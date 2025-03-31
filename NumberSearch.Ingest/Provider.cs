using FirstCom.Models;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static NumberSearch.Ingest.Program;

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
        public static async Task<IngestStatistics> FirstPointComAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password, int[] areaCodes, ReadOnlyMemory<char> connectionString)
        {
            DateTime start = DateTime.Now;

            PhoneNumber[] numbers = await FirstPointCom.GetValidNumbersByNPAAsync(username, password, areaCodes);

            PhoneNumber[] locations = await Services.AssignRatecenterAndRegionAsync(numbers);

            PhoneNumber[] typedNumbers = Services.AssignNumberTypes(locations);

            IngestStatistics stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

            DateTime end = DateTime.Now;
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
        public static async Task<IngestStatistics> BulkVSAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password, int[] areaCodes, ReadOnlyMemory<char> connectionString)
        {
            var start = DateTime.Now;

            List<PhoneNumber> numbers = [];

            foreach (var code in areaCodes)
            {
                var sample = await OrderTn.GetAsync(code, username, password);
                int[] nxxs = [.. sample.Select(x => x.NXX).Distinct()];

                foreach (int nxx in nxxs)
                {
                    try
                    {
                        var results = await OrderTn.GetAsync(code, nxx, username, password);
                        numbers.AddRange(results);
                        Log.Information("[BulkVS] Found {Count} Phone Numbers for {Code}, {NXX}", results.Length, code, nxx);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[BulkVS] Area code {Code}, {NXX} failed @ {Now}: {Message}", code, nxx, DateTime.Now, ex.Message);
                    }
                }
            }

            PhoneNumber[] locations = await Services.AssignRatecenterAndRegionAsync([.. numbers]);

            PhoneNumber[] typedNumbers = Services.AssignNumberTypes(locations);

            IngestStatistics stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

            DateTime end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "BulkVS";

            return stats;
        }

        public static async Task<IngestStatistics> BulkVSCompleteAsync(TimeSpan cycle, IngestConfiguration appConfig)
        {
            // Prevent another run from starting while this is still going.
            var lockingStats = new IngestStatistics
            {
                IngestedFrom = "BulkVS",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now,
                IngestedNew = 0,
                FailedToIngest = 0,
                NumbersRetrived = 0,
                Removed = 0,
                Unchanged = 0,
                UpdatedExisting = 0,
                Lock = true
            };
            _ = await lockingStats.PostAsync(appConfig.Postgresql.ToString());

            // Ingest all available phones numbers from the BulkVs API.
            Log.Information("Ingesting data from BulkVS");
            // Breaking this into chunks to limit peak memory consumption.
            List<IngestStatistics> allStats = [];
            var groups = PhoneNumbersNA.AreaCodes.All.Chunk(50);
            foreach (var group in groups)
            {
                var BulkVSStats = await Provider.BulkVSAsync(appConfig.BulkVSUsername, appConfig.BulkVSPassword, group, appConfig.Postgresql);
                allStats.Add(BulkVSStats);
            }

            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
            var lockEntry = await IngestStatistics.GetLockAsync("BulkVS", appConfig.Postgresql.ToString());
            if (lockEntry is not null)
            {
                _ = await lockEntry.DeleteAsync(appConfig.Postgresql.ToString());
            }

            // Remove all of the old numbers from the database.
            Log.Information("[BulkVS] Removing old numbers from the database.");
            var bulkVSCleanUp = await PhoneNumber.DeleteOldByProvider(lockingStats.StartDate, cycle.Multiply(2), "BulkVS", appConfig.Postgresql.ToString());

            var combined = new IngestStatistics
            {
                StartDate = allStats.MinBy(x => x.StartDate)?.StartDate ?? DateTime.Now,
                EndDate = bulkVSCleanUp.EndDate,
                FailedToIngest = allStats.Sum(x => x.FailedToIngest),
                IngestedFrom = allStats.FirstOrDefault()?.IngestedFrom ?? "BulkVS",
                IngestedNew = allStats.Sum(x => x.IngestedNew),
                Lock = false,
                NumbersRetrived = allStats.Sum(x => x.NumbersRetrived),
                Removed = bulkVSCleanUp.Removed,
                Unchanged = allStats.Sum(x => x.Unchanged),
                UpdatedExisting = allStats.Sum(x => x.UpdatedExisting),
                Priority = false
            };

            if (await combined.PostAsync(appConfig.Postgresql.ToString()))
            {
                Log.Information("[BulkVS] Completed the ingest process {Now}.", DateTime.Now);
            }
            else
            {
                Log.Fatal("[BulkVS] Failed to completed the ingest process {Now}.", DateTime.Now);
            }

            return combined;
        }

        public static async Task<IngestStatistics> BulkVSPriorityAsync(TimeSpan cycle, IngestConfiguration appConfig)
        {
            DateTime start = DateTime.Now;
            Log.Information("[BulkVS] Priority ingest started at {Start}.", start);

            // Ingest priority phones numbers from the BulkVs API.
            Log.Information("[BulkVS] Ingesting priority data from BulkVS.");
            var BulkVSStats = await Provider.BulkVSAsync(appConfig.BulkVSUsername, appConfig.BulkVSPassword, AreaCode.Priority, appConfig.Postgresql);

            var combined = new IngestStatistics
            {
                StartDate = BulkVSStats.StartDate,
                EndDate = DateTime.Now,
                FailedToIngest = BulkVSStats.FailedToIngest,
                IngestedFrom = BulkVSStats.IngestedFrom,
                IngestedNew = BulkVSStats.IngestedNew,
                Lock = false,
                NumbersRetrived = BulkVSStats.NumbersRetrived,
                Removed = 0,
                Unchanged = BulkVSStats.Unchanged,
                UpdatedExisting = BulkVSStats.UpdatedExisting,
                Priority = true
            };

            // Remove stale priority numbers
            foreach (var code in AreaCode.Priority)
            {
                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, cycle.Multiply(2), code, "BulkVS", appConfig.Postgresql.ToString());
                combined.Removed += removedNumbers.Removed;
            }

            if (await combined.PostAsync(appConfig.Postgresql.ToString()))
            {
                Log.Information("[BulkVS] Completed the priority ingest process {Now}.", DateTime.Now);
            }
            else
            {
                Log.Fatal("[BulkVS] Failed to completed the priority ingest process {Now}.", DateTime.Now);
            }

            Log.Information("[BulkVS] [PortRequests] Priority ingest of Port Request statuses started at {Now}.", DateTime.Now);

            // Update the statuses of all the active port requests with BulkVS.
            await PortRequests.UpdateStatusesBulkVSAsync(appConfig);

            return combined;
        }

        public static async Task<IngestStatistics> FirstPointComPriorityAsync(TimeSpan cycle, IngestConfiguration appConfig)
        {
            DateTime start = DateTime.Now;
            Log.Debug("[FirstPointCom] Priority ingest started at {Start}", start);


            // Ingest priority numbers in the FirsPointCom API.
            Log.Information("[FirstPointCom] Ingesting priority data from FirstPointCom");
            var FirstPointComStats = await Provider.FirstPointComAsync(appConfig.PComNetUsername, appConfig.PComNetPassword, AreaCode.Priority, appConfig.Postgresql);

            var combined = new IngestStatistics
            {
                StartDate = FirstPointComStats.StartDate,
                EndDate = DateTime.Now,
                FailedToIngest = FirstPointComStats.FailedToIngest,
                IngestedFrom = FirstPointComStats.IngestedFrom,
                IngestedNew = FirstPointComStats.IngestedNew,
                Lock = false,
                NumbersRetrived = FirstPointComStats.NumbersRetrived,
                Removed = 0,
                Unchanged = FirstPointComStats.Unchanged,
                UpdatedExisting = FirstPointComStats.UpdatedExisting,
                Priority = true
            };

            // Remove stale priority numbers
            foreach (var code in AreaCode.Priority)
            {
                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, cycle.Multiply(2), code, "FirstPointCom", appConfig.Postgresql.ToString());
                combined.Removed += removedNumbers.Removed;
            }

            if (await combined.PostAsync(appConfig.Postgresql.ToString()))
            {
                Log.Information("[FirstPointCom] Completed the priority ingest process {Now}.", DateTime.Now);
            }
            else
            {
                Log.Fatal("[FirstPointCom] Failed to completed the priority ingest process {Now}.", DateTime.Now);
            }

            return combined;
        }

        public static async Task<IngestStatistics> FirstPointComCompleteAsync(TimeSpan cycle, IngestConfiguration appConfig)
        {
            // Prevent another run from starting while this is still going.
            var lockingStats = new IngestStatistics
            {
                IngestedFrom = "FirstPointCom",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now,
                IngestedNew = 0,
                FailedToIngest = 0,
                NumbersRetrived = 0,
                Removed = 0,
                Unchanged = 0,
                UpdatedExisting = 0,
                Lock = true
            };

            _ = await lockingStats.PostAsync(appConfig.Postgresql.ToString());

            // Ingest all available numbers in the FirsPointCom API.
            Log.Information("[FirstPointCom] Ingesting data from FirstPointCom");
            var FirstPointComStats = await Provider.FirstPointComAsync(appConfig.PComNetUsername, appConfig.PComNetPassword, PhoneNumbersNA.AreaCodes.All, appConfig.Postgresql);

            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
            var lockEntry = await IngestStatistics.GetLockAsync("FirstPointCom", appConfig.Postgresql.ToString());
            if (lockEntry is not null)
            {
                _ = await lockEntry.DeleteAsync(appConfig.Postgresql.ToString());
            }

            // Remove all of the old numbers from the database.
            Log.Information("[FirstPointCom] Removing old FirstPointCom numbers from the database.");
            var firstPointComCleanUp = await PhoneNumber.DeleteOldByProvider(lockingStats.StartDate, cycle.Multiply(2), "FirstPointCom", appConfig.Postgresql.ToString());

            var combined = new IngestStatistics
            {
                StartDate = FirstPointComStats.StartDate,
                EndDate = firstPointComCleanUp.EndDate,
                FailedToIngest = FirstPointComStats.FailedToIngest,
                IngestedFrom = FirstPointComStats.IngestedFrom,
                IngestedNew = FirstPointComStats.IngestedNew,
                Lock = false,
                NumbersRetrived = FirstPointComStats.NumbersRetrived,
                Removed = firstPointComCleanUp.Removed,
                Unchanged = FirstPointComStats.Unchanged,
                UpdatedExisting = FirstPointComStats.UpdatedExisting,
                Priority = false
            };

            if (await combined.PostAsync(appConfig.Postgresql.ToString()))
            {
                Log.Information("[FirstPointCom] Completed the FirstPointCom ingest process {Now}.", DateTime.Now);
            }
            else
            {
                Log.Fatal("[FirstPointCom] Failed to completed the FirstPointCom ingest process {Now}.", DateTime.Now);
            }
            return combined;
        }

        public static async Task VerifyAddToCartAsync(int[] areaCodes, ReadOnlyMemory<char> numberType, ReadOnlyMemory<char> _postgresql, ReadOnlyMemory<char> _bulkVSusername,
            ReadOnlyMemory<char> _bulkVSpassword, ReadOnlyMemory<char> _fpcusername, ReadOnlyMemory<char> _fpcpassword)
        {
            foreach (var code in areaCodes)
            {
                var numbersByAreaCode = await PhoneNumber.GetAllByAreaCodeAsync(code, _postgresql.ToString());

                PhoneNumber[] numbers = [.. numbersByAreaCode.Where(x => x.NumberType == numberType.ToString())];

                if (numbers is not null && numbers.Length != 0)
                {
                    foreach (var phoneNumber in numbers)
                    {
                        // Check that the number is still available from the provider.
                        if (phoneNumber.IngestedFrom is "BulkVS")
                        {
                            string npanxx = $"{phoneNumber.NPA}{phoneNumber.NXX}";
                            try
                            {
                                var doesItStillExist = await OrderTn.GetAsync(phoneNumber.NPA, phoneNumber.NXX, _bulkVSusername, _bulkVSpassword);
                                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                                if (checkIfExists is not null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information("[BulkVS] Found {DialedNumber} in {Length} results returned for {npanxx}.", phoneNumber.DialedNumber, doesItStillExist.Length, npanxx);
                                }
                                else
                                {
                                    Log.Warning("[BulkVS] Failed to find {DialedNumber} in {Length} results returned for {npanxx}.", phoneNumber.DialedNumber, doesItStillExist.Length, npanxx);

                                    // Remove numbers that are unpurchasable.
                                    _ = await phoneNumber.DeleteAsync(_postgresql.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message);
                                Log.Error("[BulkVS] Failed to query BulkVS for {DialedNumber}.", phoneNumber?.DialedNumber);
                            }

                        }
                        else if (phoneNumber.IngestedFrom is "FirstPointCom")
                        {
                            // Verify that tele has the number.
                            try
                            {
                                var results = await FirstPointCom.GetPhoneNumbersByNpaNxxAsync(phoneNumber.NPA, phoneNumber.NXX, string.Empty.AsMemory(), _fpcusername, _fpcpassword);
                                var matchingNumber = results?.Where(x => x?.DialedNumber == phoneNumber?.DialedNumber)?.FirstOrDefault();
                                if (matchingNumber is not null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    Log.Information("[FirstPointCom] Found {DialedNumber} in {Length} results returned for {NPA}, {NXX}.", phoneNumber.DialedNumber, results?.Length, phoneNumber.NPA, phoneNumber.NXX);
                                }
                                else
                                {
                                    Log.Warning("[FirstPointCom] Failed to find {DialedNumber} in {Length} results returned for {NPA}, {NXX}.", phoneNumber.DialedNumber, results?.Length, phoneNumber.NPA, phoneNumber.NXX);

                                    // Remove numbers that are unpurchasable.
                                    _ = await phoneNumber.DeleteAsync(_postgresql.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message);
                                Log.Error("[FirstPointCom] Failed to query FirstPointCom for {DialedNumber}.", phoneNumber?.DialedNumber);
                            }
                        }
                        else if (phoneNumber.IngestedFrom is "OwnedNumber")
                        {
                            // Verify that we still have the number.
                            var matchingNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(phoneNumber.DialedNumber, _postgresql.ToString());
                            if (matchingNumber is not null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                            {
                                Log.Information("[OwnedNumber] Found {DialedNumber}.", phoneNumber.DialedNumber);
                            }
                            else
                            {
                                Log.Warning("[OwnedNumber] Failed to find {DialedNumber}.", phoneNumber.DialedNumber);

                                // Remove numbers that are unpurchasable.
                                _ = await phoneNumber.DeleteAsync(_postgresql.ToString());
                            }
                        }
                        else
                        {
                            // Remove numbers that are unpurchasable.
                            _ = await phoneNumber.DeleteAsync(_postgresql.ToString());
                        }
                    }
                }
            }
        }
    }
}
