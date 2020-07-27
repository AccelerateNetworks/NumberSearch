using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BulkVS;

namespace NumberSearch.Ingest
{
    public class Program
    {
        public static async Task Main()
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
            var peerlessApiKey = config.GetConnectionString("PeerlessAPIKey");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"NumberSearch.Ingest\\{DateTime.Now:yyyyMMdd}.txt")
                .CreateLogger();

            var start = DateTime.Now;

            var bulkVSCycle = DateTime.Now.AddHours(config.GetValue<int>("IngestProviders:BulkVS")) - DateTime.Now;
            var firstComCycle = DateTime.Now.AddHours(config.GetValue<int>("IngestProviders:FirstCom")) - DateTime.Now;
            var teleMessageCycle = DateTime.Now.AddHours(config.GetValue<int>("IngestProviders:TeleMessage")) - DateTime.Now;
            var peerlessCycle = DateTime.Now.AddHours(config.GetValue<int>("IngestProviders:Peerless")) - DateTime.Now;

            var tasks = new List<Task<IngestStatistics>>();

            var lastRun = await IngestStatistics.GetLastIngestAsync("BulkVS", postgresSQL).ConfigureAwait(false);

            // If the last ingest was run to recently do nothing.
            if (lastRun.StartDate < (DateTime.Now - bulkVSCycle))
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

                var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                tasks.Add(
                        Task.Run(async () =>
                        {
                            // Ingest all avablie phones numbers from the BulkVs API.
                            Log.Information("Ingesting data from BulkVS");
                            var BulkVSStats = await MainBulkVS.IngestPhoneNumbersAsync(bulkVSKey, bulkVSSecret, postgresSQL).ConfigureAwait(false);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("BulkVS", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("Removing old BulkVS numbers from the database.");
                            var bulkVSCleanUp = await PhoneNumber.DeleteOldByProvider(start, bulkVSCycle, "BulkVS", postgresSQL).ConfigureAwait(false);

                            var combined = new IngestStatistics
                            {
                                StartDate = BulkVSStats.StartDate,
                                EndDate = bulkVSCleanUp.EndDate,
                                FailedToIngest = BulkVSStats.FailedToIngest,
                                IngestedFrom = BulkVSStats.IngestedFrom,
                                IngestedNew = BulkVSStats.IngestedNew,
                                Lock = false,
                                NumbersRetrived = BulkVSStats.NumbersRetrived,
                                Removed = bulkVSCleanUp.Removed,
                                Unchanged = BulkVSStats.Unchanged,
                                UpdatedExisting = BulkVSStats.UpdatedExisting
                            };

                            if (await combined.PostAsync(postgresSQL))
                            {
                                Log.Information("Completed the BulkVS ingest process.");
                            }
                            else
                            {
                                Log.Fatal("Failed to completed the BulkVS ingest process.");
                            }

                            return combined;
                        })
                    );

            }
            else
            {
                Log.Information("Ingesting BulkVS skipped.");
            }

            lastRun = await IngestStatistics.GetLastIngestAsync("FirstCom", postgresSQL).ConfigureAwait(false);

            if (lastRun.StartDate < (DateTime.Now - firstComCycle))
            {
                // Prevent another run from starting while this is still going.
                var lockingStats = new IngestStatistics
                {
                    IngestedFrom = "FirstCom",
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

                var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                tasks.Add(
                        Task.Run(async () =>
                        {
                            // Ingest all avalible numbers in the FirstCom API.
                            Log.Information("Ingesting data from FirstCom");
                            var FirstComStats = await FirstCom.IngestPhoneNumbersAsync(username, password, postgresSQL);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("FirstCom", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("Removing old FirstCom numbers from the database.");
                            var firstComCleanUp = await PhoneNumber.DeleteOldByProvider(start, firstComCycle, "FirstCom", postgresSQL).ConfigureAwait(false);

                            var combined = new IngestStatistics
                            {
                                StartDate = FirstComStats.StartDate,
                                EndDate = firstComCleanUp.EndDate,
                                FailedToIngest = FirstComStats.FailedToIngest,
                                IngestedFrom = FirstComStats.IngestedFrom,
                                IngestedNew = FirstComStats.IngestedNew,
                                Lock = false,
                                NumbersRetrived = FirstComStats.NumbersRetrived,
                                Removed = firstComCleanUp.Removed,
                                Unchanged = FirstComStats.Unchanged,
                                UpdatedExisting = FirstComStats.UpdatedExisting
                            };

                            if (await combined.PostAsync(postgresSQL))
                            {
                                Log.Information("Completed the FirstCom ingest process.");
                            }
                            else
                            {
                                Log.Fatal("Failed to completed the FirstCom ingest process.");
                            }

                            return combined;
                        })
                    );
            }
            else
            {
                Log.Information("Ingesting FirstCom skipped.");
            }

            lastRun = await IngestStatistics.GetLastIngestAsync("TeleMessage", postgresSQL).ConfigureAwait(false);

            if (lastRun.StartDate < (DateTime.Now - teleMessageCycle))
            {
                // Prevent another run from starting while this is still going.
                var lockingStats = new IngestStatistics
                {
                    IngestedFrom = "TeleMessage",
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

                var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                tasks.Add(
                        Task.Run(async () =>
                        {
                            // Ingest all avalible numbers from the TeleMessage.
                            Log.Information("Ingesting data from TeleMessage");
                            var teleStats = await TeleMessage.IngestPhoneNumbersAsync(teleToken, postgresSQL);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("TeleMessage", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("Removing old TeleMessage numbers from the database.");
                            var teleMessageCleanUp = await PhoneNumber.DeleteOldByProvider(start, teleMessageCycle, "TeleMessage", postgresSQL).ConfigureAwait(false);

                            var combined = new IngestStatistics
                            {
                                StartDate = teleStats.StartDate,
                                EndDate = teleMessageCleanUp.EndDate,
                                FailedToIngest = teleStats.FailedToIngest,
                                IngestedFrom = teleStats.IngestedFrom,
                                IngestedNew = teleStats.IngestedNew,
                                Lock = false,
                                NumbersRetrived = teleStats.NumbersRetrived,
                                Removed = teleMessageCleanUp.Removed,
                                Unchanged = teleStats.Unchanged,
                                UpdatedExisting = teleStats.UpdatedExisting
                            };

                            if (await combined.PostAsync(postgresSQL))
                            {
                                Log.Information("Completed the TeleMessage ingest process.");
                            }
                            else
                            {
                                Log.Fatal("Failed to completed the TeleMessage ingest process.");
                            }

                            return combined;
                        })
                    );

            }
            else
            {
                Log.Information("Ingesting TeleMessage skipped.");
            }

            lastRun = await IngestStatistics.GetLastIngestAsync("Peerless", postgresSQL).ConfigureAwait(false);

            if (lastRun.StartDate < (DateTime.Now - peerlessCycle))
            {
                // Prevent another run from starting while this is still going.
                var lockingStats = new IngestStatistics
                {
                    IngestedFrom = "Peerless",
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

                var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                tasks.Add(
                        Task.Run(async () =>
                        {
                            // Ingest all avalible numbers from the TeleMessage.
                            Log.Information("Ingesting data from Peerless");
                            var peerlessStats = await Peerless.IngestPhoneNumbersAsync(peerlessApiKey, postgresSQL);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("Peerless", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("Removing old Peerless numbers from the database.");
                            var peerlessCleanUp = await PhoneNumber.DeleteOldByProvider(start, peerlessCycle, "Peerless", postgresSQL).ConfigureAwait(false);

                            var combined = new IngestStatistics
                            {
                                StartDate = peerlessStats.StartDate,
                                EndDate = peerlessCleanUp.EndDate,
                                FailedToIngest = peerlessStats.FailedToIngest,
                                IngestedFrom = peerlessStats.IngestedFrom,
                                IngestedNew = peerlessStats.IngestedNew,
                                Lock = false,
                                NumbersRetrived = peerlessStats.NumbersRetrived,
                                Removed = peerlessCleanUp.Removed,
                                Unchanged = peerlessStats.Unchanged,
                                UpdatedExisting = peerlessStats.UpdatedExisting
                            };

                            if (await combined.PostAsync(postgresSQL))
                            {
                                Log.Information("Completed the Peerless ingest process.");
                            }
                            else
                            {
                                Log.Fatal("Failed to completed the Peerless ingest process.");
                            }

                            return combined;
                        })
                    );

            }
            else
            {
                Log.Information("Ingesting Peerless skipped.");
            }

            //var teleStats = new IngestStatistics();
            //var BulkVSStats = new IngestStatistics();
            //var FirstComStats = new IngestStatistics();

            await Task.WhenAll(tasks);
            //foreach (var task in tasks)
            //{
            //    var result = await task;
            //    switch (result.IngestedFrom)
            //    {
            //        case "TeleMessage":
            //            teleStats = result;
            //            break;
            //        case "BulkVS":
            //            BulkVSStats = result;
            //            break;
            //        case "FirstCom":
            //            FirstComStats = result;
            //            break;
            //    }

            //}

            // This is no longer required now that each ingest deletes the number relevant to its specific provider.
            //// Remove all of the old numbers from the database.
            //Log.Information("Removing old numbers from the database.");
            //var cleanUp = await PhoneNumber.DeleteOld(start, postgresSQL).ConfigureAwait(false);
            ////var cleanUp = new IngestStatistics { };

            //if (await cleanUp.PostAsync(postgresSQL))
            //{
            //    Log.Information("Old numbers removed from the database.");
            //}
            //else
            //{
            //    Log.Fatal("Failed to remove old numbers from the database.");
            //}

            // Add an extra minute so that the finish time of all stages isn't the same as the finish time of the last stage to run.
            var end = DateTime.Now.AddMinutes(1);

            //var combinedStats = new IngestStatistics
            //{
            //    NumbersRetrived = teleStats.NumbersRetrived + BulkVSStats.NumbersRetrived + FirstComStats.NumbersRetrived,
            //    FailedToIngest = teleStats.FailedToIngest + BulkVSStats.FailedToIngest + FirstComStats.FailedToIngest,
            //    IngestedNew = teleStats.IngestedNew + BulkVSStats.IngestedNew + FirstComStats.IngestedNew,
            //    UpdatedExisting = teleStats.UpdatedExisting + BulkVSStats.UpdatedExisting + FirstComStats.UpdatedExisting,
            //    Unchanged = teleStats.Unchanged + BulkVSStats.Unchanged + FirstComStats.Unchanged,
            //    Removed = teleStats.Removed + BulkVSStats.Removed + FirstComStats.Removed,
            //    IngestedFrom = "All",
            //    StartDate = start,
            //    EndDate = end
            //};

            //var check = await combinedStats.PostAsync(postgresSQL);

            //if (check)
            //{
            //    Log.Information("Stats saved to the database.");
            //}
            //else
            //{
            //    Log.Error("Failed to save the stats to the database.");
            //}

            var diff = end - start;

            //Log.Information($"Numbers Retrived: {combinedStats.NumbersRetrived}");
            //Log.Information($"Numbers Ingested New: {combinedStats.IngestedNew}");
            //Log.Information($"Numbers Updated Existing: {combinedStats.UpdatedExisting}");
            //Log.Information($"Numbers Unchanged: {combinedStats.Unchanged}");
            //Log.Information($"Numbers Removed: {combinedStats.Removed}");
            //Log.Information($"Numbers Failed To Ingest: {combinedStats.FailedToIngest}");
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
                await semaphore.WaitAsync().ConfigureAwait(false);
                if (count % 100 == 0)
                {
                    Log.Information($"Updated {count} of {updates.Count} Phone Numbers.");
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

            Log.Information($"Updated {updates.Count} Phone Numbers");

            var listInserts = inserts.Values.ToList();

            var groups = SplitList(listInserts);

            foreach (var group in groups?.ToArray())
            {
                try
                {
                    var check = await PhoneNumber.BulkPostAsync(group, connectionString).ConfigureAwait(false);

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