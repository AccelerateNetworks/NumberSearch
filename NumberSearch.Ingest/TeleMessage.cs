using NumberSearch.DataAccess;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class TeleMessage
    {
        /// <summary>
        /// Ingests phone number from the TeleMessage API.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(Guid token, string connectionString)
        {
            var readyToSubmit = new ConcurrentDictionary<string, PhoneNumber>();

            var start = DateTime.Now;

            var npas = await GetValidNPAsAsync(token);

            Console.WriteLine($"Found {npas.Length} NPAs");

            foreach (var npa in npas)
            {
                var nxxs = await GetValidNXXsAsync(npa, token);

                Console.WriteLine($"Found {nxxs.Length} NXXs");

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
                                var localNumbers = await GetValidXXXXsAsync(npa, nxx, token);
                                foreach (var num in localNumbers)
                                {
                                    // TODO: Maybe do something with this check varible?
                                    var check = readyToSubmit.TryAdd(num.DialedNumber, num);
                                }
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
                    Console.WriteLine($"Found {count} Phone Numbers");
                }
            }

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
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }

        /// <summary>
        /// Gets a list of valid area codes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int[]> GetValidNPAsAsync(Guid token)
        {
            var results = await TeleNPA.GetAsync(token);

            if (!(results.status == "Success") && !(results.code == 200))
            {
                return new int[] { };
            }

            var valid = new List<int>();
            foreach (var npa in results?.data?.ToArray())
            {
                // Valid NPAs are only 3 chars long.
                if (npa.Length == 3)
                {
                    var check = int.TryParse(npa, out int outNpa);

                    if (check && outNpa > 99)
                    {
                        valid.Add(outNpa);
                    }
                }
            }

            return valid.ToArray();
        }

        /// <summary>
        /// gets a list of valid NXX's from a given area code.
        /// </summary>
        /// <param name="npa"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int[]> GetValidNXXsAsync(int npa, Guid token)
        {
            var results = await TeleNXX.GetAsync($"{npa}", token);

            var vaild = new List<int>();

            // Verify that we got a good response.
            if ((results.status == "success") && (results.code == 200))
            {
                foreach (var result in results?.data?.ToArray())
                {
                    // Valid NXXs are only 3 chars long.
                    if (result.Length == 3)
                    {
                        bool check = int.TryParse(result, out int nxx);

                        if (check && nxx > 99)
                        {
                            vaild.Add(nxx);
                        }
                    }
                }
            }

            return vaild.ToArray();
        }

        /// <summary>
        /// Gets a list of valid XXXX's for a given NXX.
        /// </summary>
        /// <param name="npa"> The area code. </param>
        /// <param name="nxx"> The NXX. </param>
        /// <param name="token"> The TeleMessage auth token. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidXXXXsAsync(int npa, int nxx, Guid token)
        {
            var vaild = new List<PhoneNumber>();

            try
            {
                var results = await LocalNumberTeleMessage.GetAsync($"{npa}{nxx}****", token);

                foreach (var result in results?.ToArray())
                {
                    if (result.XXXX > 1)
                    {
                        vaild.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NXX code {nxx} failed @ {DateTime.Now}: {ex.Message}");
            }

            return vaild.ToArray();
        }
    }
}
