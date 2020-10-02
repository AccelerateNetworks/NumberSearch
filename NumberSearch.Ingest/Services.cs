using NumberSearch.DataAccess;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Services
    {
        /// <summary>
        /// Assign a NumberType to a number based on the number of repeating digits in the number.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static IEnumerable<PhoneNumber> AssignNumberTypes(IEnumerable<PhoneNumber> numbers)
        {
            // NumberTypes
            var Executive = "Executive";
            var Premium = "Premium";
            var Standard = "Standard";

            // Bail early if there's no data.
            if (numbers is null || !numbers.Any()) { return numbers; }

            // Assign a Type based on number of repeating digits.
            foreach (var number in numbers)
            {
                // https://stackoverflow.com/questions/39472429/count-all-character-occurrences-in-a-string-c-sharp
                var counts = number.DialedNumber.GroupBy(c => c).Select(c => new { Char = c.Key, Count = c.Count() });

                var count = 0;
                foreach (var c in counts)
                {
                    count = c.Count > count ? c.Count : count;
                }

                if (count == 1)
                {
                    number.NumberType = Standard;
                }

                if (count == 2)
                {
                    number.NumberType = Standard;
                }

                if (count == 3)
                {
                    number.NumberType = Standard;
                }

                if (count == 4)
                {
                    number.NumberType = Premium;
                }

                if (count == 5)
                {
                    number.NumberType = Executive;
                }

                if (count == 6)
                {
                    number.NumberType = Executive;
                }

                if (count == 7)
                {
                    number.NumberType = Executive;
                }

                if (count == 8)
                {
                    number.NumberType = Executive;
                }

                if (count == 9)
                {
                    number.NumberType = Executive;
                }

                if (count == 10)
                {
                    number.NumberType = Executive;
                }
            }

            return numbers;
        }

        /// <summary>
        /// Split the list of accounts in insert into smaller lists so that they can be submitted in bulk to the database in reasonably sized chunks.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations"></param>
        /// <param name="nSize"> The maximum number of items in the smaller lists. </param>
        /// <returns></returns>
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 100)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        /// <summary>
        /// Submit the ingested Phone numbers to the database in bulk to minimize the number of commands that have to be sent.
        /// </summary>
        /// <param name="numbers"> A list of phone numbers. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> SubmitPhoneNumbersAsync(PhoneNumber[] numbers, string connectionString)
        {
            var stats = new IngestStatistics();

            var inserts = new Dictionary<string, PhoneNumber>();
            var updates = new Dictionary<string, PhoneNumber>();

            if (numbers.Length > 0)
            {
                var existingNumbers = await PhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);
                var dict = existingNumbers.ToDictionary(x => x.DialedNumber, x => x);
                // Submit the batch to the remote database.
                foreach (var number in numbers)
                {
                    // Check if it already exists.
                    var inDb = number.ExistsInDb(dict);

                    if (inDb)
                    {
                        var check = updates.TryAdd(number.DialedNumber, number);

                        if (check)
                        {
                            stats.NumbersRetrived++;
                            stats.UpdatedExisting++;
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.FailedToIngest++;
                        }

                    }
                    else
                    {
                        // If it doesn't exist then add it.
                        var check = inserts.TryAdd(number.DialedNumber, number);

                        // When the API returns duplicate numbers.
                        if (check)
                        {
                            stats.NumbersRetrived++;
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.FailedToIngest++;
                        }
                    }
                }
                Log.Information($"Found {inserts?.Count} new Phone Numbers to Insert.");
                Log.Information($"Found {updates?.Count} existing Phone Numbers to Update.");
            }

            // Execute these API requests in parallel.
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

            var results = new List<Task>();
            var count = 0;

            foreach (var update in updates.Values.ToArray())
            {
                // Wait for an open slot in the semaphore before grabbing another thread from the threadpool.
                await semaphore.WaitAsync().ConfigureAwait(false);
                if (count % 100 == 0)
                {
                    Log.Information($"Updated {count} of {updates?.Count} Phone Numbers.");
                }
                results.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await update.PutAsync(connectionString).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
                count++;
            }

            await Task.WhenAll(results).ConfigureAwait(false);

            Log.Information($"Updated {updates?.Count} Phone Numbers");

            var listInserts = inserts.Values.ToList();

            var groups = SplitList(listInserts);

            foreach (var group in groups?.ToArray())
            {
                try
                {
                    var check = await PhoneNumber.BulkPostAsync(group, connectionString).ConfigureAwait(false);

                    if (check) { stats.IngestedNew += group.Count; };

                    Log.Information($"{stats?.IngestedNew} of {listInserts?.Count} submitted to the database.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to submit a batch of PhoneNumbers to the database. Exception: {ex?.Message}");
                    count = 0;
                    foreach (var number in group)
                    {
                        Log.Error($"{count}. {number?.DialedNumber}, {number?.IngestedFrom}");
                        count++;
                    }
                }
            }

            return stats;
        }
    }
}
