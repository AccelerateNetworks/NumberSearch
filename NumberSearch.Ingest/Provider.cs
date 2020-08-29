using BulkVS;

using FirstCom;

using NumberSearch.DataAccess;

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

            var numbers = await FirstPointCom.GetValidNumbersByNPAAsync(username, password, areaCodes);

            var typedNumbers = Ingest.AssignNumberTypes(numbers).ToArray();

            var stats = await Ingest.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

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
        public static async Task<IngestStatistics> BulkVSAsync(string apiKey, string apiSecret, int[] areaCodes, string connectionString)
        {
            var start = DateTime.Now;

            var numbers = await MainBulkVS.GetValidNumbersByNPAAsync(apiKey, apiSecret, areaCodes);

            var typedNumbers = Ingest.AssignNumberTypes(numbers).ToArray();

            var stats = await Ingest.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "BulkVS";

            return stats;
        }

        /// <summary>
        /// Ingests phone numbers from the TeleMessage API.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> TeleMessageAsync(Guid token, int[] areaCodes, string connectionString)
        {
            var readyToSubmit = new List<PhoneNumber>();

            var start = DateTime.Now;

            // Pass this provider a blank array if you want it to figure out what NPAs are available. 
            var npas = areaCodes.Length > 0 ? areaCodes : new int[] { };

            try
            {
                if (npas.Length > 0)
                {
                    Log.Information($"[TeleMessage] Found {npas.Length} NPAs");
                }
                else
                {
                    npas = await TeleMessage.GetValidNPAsAsync(token);

                    Log.Information($"[TeleMessage] Found {npas.Length} NPAs");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                Log.Error($"[TeleMessage] No NPAs Retrived.");
            }

            foreach (var npa in npas)
            {
                var localNumbers = await TeleMessage.GetXXXXsByNPAAsync(npa, token);
                readyToSubmit.AddRange(localNumbers);

                Log.Information($"[TeleMessage] Found {localNumbers.Length} Phone Numbers for the {npa} Area Code.");
            }

            Log.Information($"[TeleMessage] Found {readyToSubmit.Count} Phone Numbers");

            var typedNumbers = Ingest.AssignNumberTypes(readyToSubmit).ToArray();

            var stats = await Ingest.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }

        /// <summary>
        /// Ingests phone numbers from the TeleMessage API using a multithreaded method.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> TeleMessageConcurrentAsync(Guid token, int[] areaCodes, string connectionString)
        {
            var readyToSubmit = new ConcurrentDictionary<string, PhoneNumber>();

            var start = DateTime.Now;

            // Pass this provider a blank array if you want it to figure out what NPAs are available. 
            var npas = areaCodes.Length > 0 ? areaCodes : new int[] { };

            try
            {
                if (npas.Length > 0)
                {
                    Log.Information($"[TeleMessage] Found {npas.Length} NPAs");
                }
                else
                {
                    npas = await TeleMessage.GetValidNPAsAsync(token);

                    Log.Information($"[TeleMessage] Found {npas.Length} NPAs");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                Log.Error($"[TeleMessage] No NPAs Retrived.");
            }

            foreach (var npa in npas)
            {
                var nxxs = new int[] { };

                try
                {
                    nxxs = await TeleMessage.GetValidNXXsAsync(npa, token);

                    Log.Information($"[TeleMessage] Found {nxxs.Length} NXXs for NPA {npa}");
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message}");
                    Log.Error($"[TeleMessage] No NXXs Retrived for NPA {npa}.");
                }

                if (nxxs.Length > 1)
                {
                    // Execute these API requests in parallel.
                    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

                    var results = new List<Task<int>>();

                    foreach (var nxx in nxxs)
                    {
                        // Wait for an open slot in the semaphore before grabbing another thread from the threadpool.
                        await semaphore.WaitAsync();
                        results.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var localNumbers = await TeleMessage.GetValidXXXXsAsync(npa, nxx, token);
                                foreach (var num in localNumbers)
                                {
                                    // TODO: Maybe do something with this check varible?
                                    var check = readyToSubmit.TryAdd(num.DialedNumber, num);
                                }

                                Log.Information($"[TeleMessage] Found {localNumbers.Length} Phone Numbers for {npa}-{nxx}-xxxx");

                                return localNumbers.Length;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }
                    var complete = await Task.WhenAll(results);

                    // Total the numbers retrived.
                    int count = 0;
                    foreach (var xxxx in complete)
                    {
                        count += xxxx;
                    }
                    Log.Information($"[TeleMessage] Found {count} Phone Numbers");
                }
            }

            // Pull just the objects out of the concurrent data structure.
            var numbersReady = new List<PhoneNumber>();
            foreach (var number in readyToSubmit)
            {
                numbersReady.Add(number.Value);
            }

            var typedNumbers = Ingest.AssignNumberTypes(numbersReady).ToArray();

            var stats = await Ingest.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }
    }
}
