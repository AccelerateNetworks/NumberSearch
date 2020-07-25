using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;
using NumberSearch.DataAccess.Peerless;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Peerless
    {
        /// <summary>
        /// Get a list of Phone Numbers from the Peerless API.
        /// </summary>
        /// <param name="apiKey"> A key for the peerless API. </param>
        /// <param name="connectionString"> A connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string apiKey, string connectionString)
        {
            var readyToSubmit = new ConcurrentDictionary<string, PhoneNumber>();

            var start = DateTime.Now;

            var npas = AreaCode.AreaCodes;

            Log.Information($"Found {npas.Length} NPAs");

            // Execute these API requests in parallel.
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

            var results = new List<Task<int>>();

            foreach (var npa in npas)
            {
                // Wait for an open slot in the semaphore before grabbing another thread from the threadpool.
                await semaphore.WaitAsync();
                results.Add(Task.Run(async () =>
                {
                    try
                    {
                        var numbers = await DidFind.GetAsync(npa.ToString(), apiKey);
                        foreach (var num in numbers)
                        {
                            // TODO: Maybe do something with this check varible?
                            var check = readyToSubmit.TryAdd(num.DialedNumber, num);
                        }
                        Log.Information($"Found {numbers.Count()} Phone Numbers");
                        return numbers.Count();
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
            Log.Information($"Found {count} Phone Numbers");

            // Pull just the objects out of the concurrent data structure.
            var numbersReady = new List<PhoneNumber>();
            foreach (var number in readyToSubmit)
            {
                numbersReady.Add(number.Value);
            }

            var stats = await Program.SubmitPhoneNumbersAsync(numbersReady.ToArray(), connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "Peerless";

            return stats;
        }

        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The firstcom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string apiKey)
        {
            var areaCodes = AreaCode.AreaCodes;

            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await DidFind.GetAsync(code.ToString(), apiKey).ConfigureAwait(false));
                    Log.Information($"Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }
    }
}