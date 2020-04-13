using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("40f816f3-0a65-4523-a9be-4bbef0716720")
            .Build();

            var teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            var postgresSQL = config.GetConnectionString("PostgresqlProd");
            var bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            var bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
            var username = config.GetConnectionString("PComNetUsername");
            var password = config.GetConnectionString("PComNetPassword");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"NumberSearch.Ingest\\{DateTime.Now:yyyyMMdd}.txt")
                .CreateLogger();

            var start = DateTime.Now;

            // Ingest all avablie phones numbers from the BulkVs API.
            Log.Information("Ingesting data from BulkVS");
            var BulkVSStats = await BulkVS.IngestPhoneNumbersAsync(bulkVSKey, bulkVSSecret, postgresSQL);
            //var BulkVSStats = new IngestStatistics { };

            if (await BulkVSStats.PostAsync(postgresSQL))
            {
                Log.Information("Ingest logged to the database.");
            }
            else
            {
                Log.Error("Failed to log this ingest.");
            }

            // Ingest all avalible numbers in the FirstCom API.
            Log.Information("Ingesting data from FirstCom");
            var FirstComStats = await FirstCom.IngestPhoneNumbersAsync(username, password, postgresSQL);
            //var FirstComStats = new IngestStatistics { };

            if (await FirstComStats.PostAsync(postgresSQL))
            {
                Log.Information("Ingest logged to the database.");
            }
            else
            {
                Log.Error("Failed to log this ingest.");
            }

            // Ingest all avalible numbers from the TeleAPI.
            Log.Information("Ingesting data from TeleAPI");
            var teleStats = await TeleMessage.IngestPhoneNumbersAsync(teleToken, postgresSQL);
            //var teleStats = new IngestStatistics { };

            if (await teleStats.PostAsync(postgresSQL))
            {
                Log.Information("Ingest logged to the database.");
            }
            else
            {
                Log.Error("Failed to log this ingest.");
            }

            // Remove all of the old numbers from the database.
            Log.Information("Removing old numbers from the database.");
            var cleanUp = await PhoneNumber.DeleteOld(start, postgresSQL);
            //var cleanUp = new IngestStatistics { };

            if (await cleanUp.PostAsync(postgresSQL))
            {
                Log.Information("Old numbers removed from the database.");
            }
            else
            {
                Log.Fatal("Failed to remove old numbers from the database.");
            }

            // Add an extra minute so that the finish time of all stages isn't the same as the finish time of the last stage to run.
            var end = DateTime.Now.AddMinutes(1);

            var combinedStats = new IngestStatistics
            {
                NumbersRetrived = teleStats.NumbersRetrived + BulkVSStats.NumbersRetrived + FirstComStats.NumbersRetrived,
                FailedToIngest = teleStats.FailedToIngest + BulkVSStats.FailedToIngest + FirstComStats.FailedToIngest,
                IngestedNew = teleStats.IngestedNew + BulkVSStats.IngestedNew + FirstComStats.IngestedNew,
                UpdatedExisting = teleStats.UpdatedExisting + BulkVSStats.UpdatedExisting + FirstComStats.UpdatedExisting,
                Unchanged = teleStats.Unchanged + BulkVSStats.Unchanged + FirstComStats.Unchanged,
                Removed = cleanUp.Removed,
                IngestedFrom = "All",
                StartDate = start,
                EndDate = end
            };

            var check = await combinedStats.PostAsync(postgresSQL);

            if (check)
            {
                Log.Information("Stats saved to the database.");
            }
            else
            {
                Log.Error("Failed to save the stats to the database.");
            }

            var diff = end - start;

            Log.Information($"Numbers Retrived: {combinedStats.NumbersRetrived}");
            Log.Information($"Numbers Ingested New: {combinedStats.IngestedNew}");
            Log.Information($"Numbers Updated Existing: {combinedStats.UpdatedExisting}");
            Log.Information($"Numbers Unchanged: {combinedStats.Unchanged}");
            Log.Information($"Numbers Removed: {combinedStats.Removed}");
            Log.Information($"Numbers Failed To Ingest: {combinedStats.FailedToIngest}");
            Log.Information($"Start: {start.ToLongTimeString()} End: {end.ToLongTimeString()} Elapsed: {diff.TotalMinutes} Minutes");

            Log.CloseAndFlush();
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
                var existingNumbers = await PhoneNumber.GetAllAsync(connectionString);
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
                Log.Information($"Found {inserts.Count} new Phone Numbers to Insert.");
                Log.Information($"Found {updates.Count} existing Phone Numbers to Update.");
            }

            // Execute these API requests in parallel.
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

            var results = new List<Task>();
            var count = 0;

            foreach (var update in updates.Values.ToArray())
            {
                // Wait for an open slot in the semaphore before grabbing another thread from the threadpool.
                await semaphore.WaitAsync();
                if (count % 100 == 0)
                {
                    Log.Information($"Updated {count} of {updates.Count} Phone Numbers.");
                }
                results.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await update.PutAsync(connectionString);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
                count++;
            }

            await Task.WhenAll(results).ConfigureAwait(false);

            Log.Information($"Updated {updates.Count} Phone Numbers");

            var listInserts = inserts.Values.ToList();

            var groups = SplitList(listInserts);

            foreach (var group in groups?.ToArray())
            {
                try
                {
                    var check = await PhoneNumber.BulkPostAsync(group, connectionString);

                    if (check) { stats.IngestedNew += group.Count; };

                    Log.Information($"{stats.IngestedNew} of {listInserts.Count} submitted to the database.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to submit a batch of PhoneNumbers to the database. Exception: {ex.Message}");
                    count = 0;
                    foreach (var number in group)
                    {
                        Log.Error($"{count}. {number.DialedNumber}, {number.IngestedFrom}");
                        count++;
                    }
                }
            }

            return stats;
        }
    }
}