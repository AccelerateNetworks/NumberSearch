using FirstCom;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Call48;
using NumberSearch.DataAccess.TeliMesssage;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    Log.Information($"[Call48] Ingesting numbers for {state.StateShort}.");

                    foreach (var code in state.AreaCodes)
                    {
                        try
                        {
                            numbers.AddRange(await Search.GetAsync(state.StateShort, code, credentials.data.token).ConfigureAwait(false));
                            Log.Information($"[Call48] Found {numbers.Count} Phone Numbers");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[Call48] Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"[Call48] Failed to login to Call48.");
                Log.Fatal($"[Call48] {ex.Message}");
            }

            // This is the ratecenters method, which is disabled because the Call48 API is drastically slower when we use it.
            //var statesAndRatecenters = await Ratecenter.GetAllRatecentersAsync(states, credentials.data.token).ConfigureAwait(false);

            //foreach (var state in statesAndRatecenters)
            //{
            //    Log.Information($"[Call48] Ingesting numbers for {state.ShortState}.");

            //    foreach (var ratecenter in state.Ratecenters)
            //    {
            //        try
            //        {
            //            numbers.AddRange(await Search.GetAsync(state.ShortState, ratecenter, credentials.data.token).ConfigureAwait(false));
            //            Log.Information($"[Call48] Found {numbers.Count} Phone Numbers");
            //        }
            //        catch (Exception ex)
            //        {
            //            Log.Error($"[Call48] Ratecenter {ratecenter}, {state.ShortState} failed @ {DateTime.Now}: {ex.Message}");
            //        }
            //    }
            //}

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
                Log.Information($"[TeliMessage] Found {tollfree.Count()} Tollfree Phone Numbers.");
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
            stats.IngestedFrom = "TeliMessage";

            return stats;
        }

        /// <summary>
        /// Ingests phone numbers from the TeleMessage API using a multithreaded method.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> TeliMessageConcurrentAsync(Guid token, int[] areaCodes, string connectionString)
        {
            var readyToSubmit = new ConcurrentDictionary<string, PhoneNumber>();

            var start = DateTime.Now;

            // Pass this provider a blank array if you want it to figure out what NPAs are available. 
            var npas = areaCodes.Length > 0 ? areaCodes : Array.Empty<int>();

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

            foreach (var npa in npas)
            {
                var nxxs = Array.Empty<int>();

                try
                {
                    nxxs = await TeliMessage.GetValidNXXsAsync(npa, token).ConfigureAwait(false);

                    Log.Information($"[TeliMessage] Found {nxxs.Length} NXXs for NPA {npa}");
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message}");
                    Log.Error($"[TeliMessage] No NXXs Retrived for NPA {npa}.");
                }

                if (nxxs.Length > 1)
                {
                    // Execute these API requests in parallel.
                    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

                    var results = new List<Task<int>>();

                    foreach (var nxx in nxxs)
                    {
                        // Wait for an open slot in the semaphore before grabbing another thread from the threadpool.
                        await semaphore.WaitAsync().ConfigureAwait(false);
                        results.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var localNumbers = await TeliMessage.GetValidXXXXsAsync(npa, nxx, token).ConfigureAwait(false);
                                foreach (var num in localNumbers)
                                {
                                    // TODO: Maybe do something with this check varible?
                                    var check = readyToSubmit.TryAdd(num.DialedNumber, num);
                                }

                                Log.Information($"[TeliMessage] Found {localNumbers.Length} Phone Numbers for {npa}-{nxx}-xxxx");

                                return localNumbers.Length;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }
                    var complete = await Task.WhenAll(results).ConfigureAwait(false);

                    // Total the numbers retrived.
                    int count = 0;
                    foreach (var xxxx in complete)
                    {
                        count += xxxx;
                    }
                    Log.Information($"[TeliMessage] Found {count} Phone Numbers");
                }
            }

            // Pull just the objects out of the concurrent data structure.
            var numbersReady = new List<PhoneNumber>();
            foreach (var number in readyToSubmit)
            {
                numbersReady.Add(number.Value);
            }

            var typedNumbers = Services.AssignNumberTypes(numbersReady).ToArray();

            var stats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }
    }
}
