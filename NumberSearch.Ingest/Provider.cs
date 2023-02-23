using FirstCom;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Call48;
using NumberSearch.DataAccess.Peerless;
using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        public static async Task<IngestStatistics> PeerlessAsync(string[] ratecenters, string peerlessAPIKey, string connectionString)
        {
            var start = DateTime.Now;

            var numbers = await Peerless.GetValidNumbersByRateCenterAsync(ratecenters, peerlessAPIKey).ConfigureAwait(false);

            var locations = await Services.AssignRatecenterAndRegionAsync(numbers).ConfigureAwait(false);
            numbers = locations.ToArray();

            var typedNumbers = Services.AssignNumberTypes(numbers).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "Peerless";

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

        /// <summary>
        /// Ingest phone numbers from the Call48 API.
        /// </summary>
        /// <param name="username"> The username for the API. </param>
        /// <param name="password"> The password for the API. </param>
        /// <param name="states"> A list of states to ingest. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> Call48Async(string username, string password, PhoneNumbersNA.AreaCode.AreaCodesByState[] states, string connectionString)
        {
            var start = DateTime.Now;

            var numbers = new List<PhoneNumber>();

            try
            {
                var credentials = await Login.LoginAsync(username, password).ConfigureAwait(false);

                foreach (var state in states)
                {
                    Log.Information($"[Call48] Ingesting numbers for {state?.StateShort}.");

                    var ratecenters = await Ratecenter.GetRatecentersByStateAsync(state?.StateShort ?? string.Empty, credentials.data.token);

                    if (ratecenters is not null && ratecenters.data.Any())
                    {
                        foreach (var ratecenter in ratecenters.data)
                        {
                            try
                            {
                                numbers.AddRange(await Search.GetAsync(state?.StateShort ?? string.Empty, ratecenter.rate_center, credentials.data.token).ConfigureAwait(false));
                                Log.Information($"[Call48] Found {numbers.Count} Phone Numbers");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[Call48] Rate Center {ratecenter.rate_center} in State {state?.StateShort ?? string.Empty} failed @ {DateTime.Now}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"[Call48] Failed to login to Call48.");
                Log.Fatal($"[Call48] {ex.Message}");
            }

            var locations = await Services.AssignRatecenterAndRegionAsync(numbers).ConfigureAwait(false);
            numbers = locations.ToList();

            var typedNumbers = Services.AssignNumberTypes(numbers).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "Call48";

            return stats;
        }

        /// <summary>
        /// Ingests phone numbers from the TeleMessage API.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> TeliMessageAsync(Guid token, int[] areaCodes, string connectionString)
        {
            var readyToSubmit = new List<PhoneNumber>();

            var start = DateTime.Now;

            // Pass this provider a blank array if you want it to figure out what NPAs are available. 
            var npas = areaCodes.Length > 0 ? areaCodes : Array.Empty<int>();

            // Capture the NPAs.
            try
            {
                if (npas.Length > 0)
                {
                    Log.Information($"[TeliMessage] Found {npas.Length} NPAs");
                }
                else
                {
                    npas = await TeliMessage.GetValidNPAsAsync(token).ConfigureAwait(false);

                    Log.Information($"[TeliMessage] Found {npas.Length} NPAs");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                Log.Error($"[TeliMessage] No NPAs Retrived.");
            }

            // Capture the numbers using their NPA's.
            foreach (var npa in npas)
            {
                try
                {
                    var localNumbers = await TeliMessage.GetXXXXsByNPAAsync(npa, token).ConfigureAwait(false);
                    readyToSubmit.AddRange(localNumbers);

                    Log.Information($"[TeliMessage] Found {localNumbers.Length} Phone Numbers for the {npa} Area Code.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[TeliMessage] {ex.Message} {ex.InnerException}");
                    Log.Information($"[TeliMessage] Found 0 Phone Numbers for the {npa} Area Code.");
                }
            }

            // Capture the tollfree numbers.
            try
            {
                var tollfree = await DidsList.GetAllTollfreeAsync(token).ConfigureAwait(false);
                readyToSubmit.AddRange(tollfree);
                Log.Information($"[TeliMessage] Found {tollfree.Length} Tollfree Phone Numbers.");
            }
            catch (Exception ex)
            {
                Log.Error($"[TeliMessage] {ex.Message} {ex.InnerException}");
                Log.Information($"[TeliMessage] Found 0 Tollfree Phone Numbers.");
            }

            Log.Information($"[TeliMessage] Found {readyToSubmit.Count} Phone Numbers");

            var locations = await Services.AssignRatecenterAndRegionAsync(readyToSubmit).ConfigureAwait(false);
            readyToSubmit = locations.ToList();

            var typedNumbers = Services.AssignNumberTypes(readyToSubmit).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }

        public static async Task VerifyAddToCartAsync(int[] areaCodes, string numberType, string _postgresql, string _bulkVSusername,
            string _bulkVSpassword, Guid _teleToken, string _fpcusername, string _fpcpassword, string _call48Username,
            string _call48Password, string _peerlessApiKey)
        {
            foreach (var code in areaCodes)
            {
                var numbers = await PhoneNumber.GetAllByAreaCodeAsync(code, _postgresql);

                numbers = numbers.Where(x => x.NumberType == numberType);

                if (numbers is not null && numbers.Any())
                {
                    foreach (var phoneNumber in numbers)
                    {
                        // Check that the number is still avalible from the provider.
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
                        else if (phoneNumber.IngestedFrom is "TeleMessage")
                        {
                            // Verify that tele has the number.
                            try
                            {
                                var doesItStillExist = await DidsList.GetAsync(phoneNumber.NPA, phoneNumber.NXX, _teleToken).ConfigureAwait(false);
                                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information($"[TeliMessage] Found {phoneNumber.DialedNumber} in {doesItStillExist.Length} results returned for {phoneNumber.DialedNumber}.");
                                }
                                else
                                {
                                    Log.Warning($"[TeliMessage] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Length} results returned for {phoneNumber.DialedNumber}.");

                                    // Remove numbers that are unpurchasable.
                                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{ex.Message}");
                                Log.Error($"[TeliMessage] Failed to query TeliMessage for {phoneNumber?.DialedNumber}.");
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
                        else if (phoneNumber.IngestedFrom is "Call48")
                        {
                            //// Verify that Call48 has the number.
                            //try
                            //{
                            //    var credentials = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);
                            //    var results = await Search.GetLocalNumbersAsync(phoneNumber.State, string.Empty, phoneNumber.NPA.ToString(), phoneNumber.NXX.ToString(), credentials.data.token).ConfigureAwait(false);
                            //    var matchingNumber = results?.data?.result?.Where(x => x?.did_number?.Replace("-", string.Empty) == phoneNumber?.DialedNumber)?.FirstOrDefault();
                            //    if (matchingNumber != null && matchingNumber?.did_number?.Replace("-", string.Empty) == phoneNumber?.DialedNumber)
                            //    {
                            //        Log.Information($"[Call48] Found {phoneNumber?.DialedNumber} in {results?.data?.result?.Length} results returned for {phoneNumber?.NPA}, {phoneNumber?.NXX}.");
                            //    }
                            //    else
                            //    {
                            //        Log.Warning($"[Call48] Failed to find {phoneNumber?.DialedNumber} in {results?.data?.result?.Length} results returned for {phoneNumber?.NPA}, {phoneNumber?.NXX}.");

                            //        // Remove numbers that are unpurchasable.
                            //        var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                            //    }
                            //}
                            //catch (Exception ex)
                            //{
                            //    Log.Error($"{ex.Message}");
                            //    Log.Error($"[Call48] Failed to query Call48 for {phoneNumber?.DialedNumber}.");
                            //}
                        }
                        else if (phoneNumber.IngestedFrom is "Peerless")
                        {
                            // Verify that Peerless has the number.
                            try
                            {
                                var results = await DidFind.GetByDialedNumberAsync(phoneNumber.NPA.ToString("000"), phoneNumber.NXX.ToString("000"), phoneNumber.XXXX.ToString("0000"), _peerlessApiKey);
                                // Sometimes Call48 includes dashes in their numbers for no reason.
                                var matchingNumber = results?.Where(x => x?.DialedNumber == phoneNumber?.DialedNumber)?.FirstOrDefault();
                                if (matchingNumber is not null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information($"[Peerless] Found {phoneNumber.DialedNumber} in {results?.Length} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}, {phoneNumber.XXXX}.");
                                }
                                else
                                {
                                    Log.Warning($"[Peerless] Failed to find {phoneNumber.DialedNumber} in {results?.Length} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}, {phoneNumber.XXXX}.");

                                    // Remove numbers that are unpurchasable.
                                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{ex.Message}");
                                Log.Error($"[Peerless] Failed to query Peerless for {phoneNumber?.DialedNumber}.");
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
