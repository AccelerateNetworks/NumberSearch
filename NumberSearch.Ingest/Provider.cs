using FirstCom;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Provider
    {
        /// <summary>
        /// Ingest phone numbers from the FirstPointCom API.
        /// </summary>
        /// <param name="username"> The FirstPointCom username. </param>
        /// <param name="password"> The FirstPointCom password. </param>
        /// <param name="connectionString"> the connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> FirstPointComAsync(string username, string password, int[] areaCodes, string connectionString)
        {
            var start = DateTime.Now;

            var numbers = await FirstPointCom.GetValidNumbersByNPAAsync(username, password, areaCodes).ConfigureAwait(false);

            var locations = await Services.AssignRatecenterAndRegionAsync(numbers).ConfigureAwait(false);
            numbers = locations.ToArray();

            var typedNumbers = Services.AssignNumberTypes(numbers).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "FirstPointCom";

            return stats;
        }

        /// <summary>
        /// Ingest phone numbers from the BulkVS API.
        /// </summary>
        /// <param name="apiKey"> The bulkVS API key. </param>
        /// <param name="apiSecret"> The bulkVS API secret. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> BulkVSAsync(string username, string password, int[] areaCodes, string connectionString)
        {
            var start = DateTime.Now;

            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await OrderTn.GetAsync(code, username, password).ConfigureAwait(false));
                    Log.Information($"[BulkVS] Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"[BulkVS] Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            var locations = await Services.AssignRatecenterAndRegionAsync(numbers).ConfigureAwait(false);
            numbers = locations.ToList();

            var typedNumbers = Services.AssignNumberTypes(numbers).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "BulkVS";

            return stats;
        }

        public static async Task VerifyAddToCartAsync(int[] areaCodes, string numberType, string _postgresql, string _bulkVSusername,
            string _bulkVSpassword, string _fpcusername, string _fpcpassword)
        {
            foreach (var code in areaCodes)
            {
                var numbers = await PhoneNumber.GetAllByAreaCodeAsync(code, _postgresql);

                numbers = numbers.Where(x => x.NumberType == numberType);

                if (numbers is not null && numbers.Any())
                {
                    foreach (var phoneNumber in numbers)
                    {
                        // Check that the number is still available from the provider.
                        if (phoneNumber.IngestedFrom is "BulkVS")
                        {
                            var npanxx = $"{phoneNumber.NPA}{phoneNumber.NXX}";
                            try
                            {
                                var doesItStillExist = await OrderTn.GetAsync(phoneNumber.NPA, phoneNumber.NXX, _bulkVSusername, _bulkVSpassword).ConfigureAwait(false);
                                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                                if (checkIfExists is not null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information($"[BulkVS] Found {phoneNumber.DialedNumber} in {doesItStillExist.Length} results returned for {npanxx}.");
                                }
                                else
                                {
                                    Log.Warning($"[BulkVS] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Length} results returned for {npanxx}.");

                                    // Remove numbers that are unpurchasable.
                                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{ex.Message}");
                                Log.Error($"[BulkVS] Failed to query BulkVS for {phoneNumber?.DialedNumber}.");
                            }

                        }
                        else if (phoneNumber.IngestedFrom is "FirstPointCom")
                        {
                            // Verify that tele has the number.
                            try
                            {
                                var results = await NpaNxxFirstPointCom.GetAsync(phoneNumber.NPA.ToString(new CultureInfo("en-US")), phoneNumber.NXX.ToString(new CultureInfo("en-US")), string.Empty, _fpcusername, _fpcpassword).ConfigureAwait(false);
                                var matchingNumber = results?.Where(x => x?.DialedNumber == phoneNumber?.DialedNumber)?.FirstOrDefault();
                                if (matchingNumber is not null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information($"[FirstPointCom] Found {phoneNumber.DialedNumber} in {results?.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");
                                }
                                else
                                {
                                    Log.Warning($"[FirstPointCom] Failed to find {phoneNumber.DialedNumber} in {results?.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");

                                    // Remove numbers that are unpurchasable.
                                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{ex.Message}");
                                Log.Error($"[FirstPointCom] Failed to query FirstPointCom for {phoneNumber?.DialedNumber}.");
                            }
                        }
                        else if (phoneNumber.IngestedFrom is "OwnedNumber")
                        {
                            // Verify that we still have the number.
                            var matchingNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(phoneNumber.DialedNumber, _postgresql).ConfigureAwait(false);
                            if (matchingNumber is not null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                            {
                                Log.Information($"[OwnedNumber] Found {phoneNumber.DialedNumber}.");
                            }
                            else
                            {
                                Log.Warning($"[OwnedNumber] Failed to find {phoneNumber.DialedNumber}.");

                                // Remove numbers that are unpurchasable.
                                var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            // Remove numbers that are unpurchasable.
                            var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}
