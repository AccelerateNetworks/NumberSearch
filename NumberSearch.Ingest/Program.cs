using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                .WriteTo.Async(a => a.Console())
                .WriteTo.Async(a => a.Debug())
                .WriteTo.Async(a => a.File($"NumberSearch.Ingest_{DateTime.Now:yyyyMMdd}.txt"))
                .CreateLogger();

            Log.Information("[Heartbeat] Ingest scheduling loop is starting.");

            var tasks = new List<Task<IngestStatistics>>();

            while (true)
            {
                var start = DateTime.Now;

                // Get the configuration for each of the ingest providers.
                var cycles = await IngestCycle.GetAllAsync(postgresSQL).ConfigureAwait(false);
                var bulkVSCycle = cycles.Where(x => x.IngestedFrom == "BulkVS").FirstOrDefault();
                var firstPointComCycle = cycles.Where(x => x.IngestedFrom == "FirstPointCom").FirstOrDefault();
                var teleMessageCycle = cycles.Where(x => x.IngestedFrom == "TeleMessage").FirstOrDefault();
                var peerlessCycle = cycles.Where(x => x.IngestedFrom == "Peerless").FirstOrDefault();

                // Ingest phone numbers from BulkVS.
                if (bulkVSCycle != null && (bulkVSCycle.Enabled || bulkVSCycle.RunNow))
                {
                    var lastRun = await IngestStatistics.GetLastIngestAsync("BulkVS", postgresSQL).ConfigureAwait(false);

                    // If the last ingest was run to recently do nothing.
                    if (lastRun != null && (lastRun.StartDate < (start - bulkVSCycle.CycleTime) || bulkVSCycle.RunNow))
                    {
                        Log.Debug($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

                        Log.Information($"[BulkVS] Cycle time is {bulkVSCycle?.CycleTime}");
                        Log.Information($"[BulkVS] Enabled is {bulkVSCycle?.Enabled}");

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
                                    var BulkVSStats = await Provider.BulkVSAsync(bulkVSKey, bulkVSSecret, postgresSQL).ConfigureAwait(false);

                                    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                                    var lockEntry = await IngestStatistics.GetLockAsync("BulkVS", postgresSQL).ConfigureAwait(false);
                                    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                                    // Remove all of the old numbers from the database.
                                    Log.Information("[BulkVS] Removing old numbers from the database.");
                                    var bulkVSCleanUp = await PhoneNumber.DeleteOldByProvider(start, bulkVSCycle.CycleTime, "BulkVS", postgresSQL).ConfigureAwait(false);

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
                                        Log.Information("[BulkVS] Completed the ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("[BulkVS] Failed to completed the ingest process.");
                                    }

                                    return combined;
                                })
                            );

                        if (bulkVSCycle.RunNow)
                        {
                            bulkVSCycle.RunNow = false;
                            var checkRunNow = bulkVSCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        }

                    }
                    else
                    {
                        Log.Information("[BulkVS] Ingest skipped.");
                    }
                }

                // Ingest phone numbers from FirstPointCom.
                if (firstPointComCycle != null && (firstPointComCycle.Enabled || firstPointComCycle.RunNow))
                {
                    var lastRun = await IngestStatistics.GetLastIngestAsync("FirstPointCom", postgresSQL).ConfigureAwait(false);

                    if (lastRun != null && (lastRun.StartDate < (start - firstPointComCycle.CycleTime) || firstPointComCycle.RunNow))
                    {
                        Log.Debug($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

                        Log.Information($"[FirstPointCom] Cycle time is {firstPointComCycle?.CycleTime}");
                        Log.Information($"[FirstPointCom] Enabled is {firstPointComCycle?.Enabled}");

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

                        var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                        tasks.Add(
                                Task.Run(async () =>
                                {
                                    // Ingest all avalible numbers in the FirsPointtCom API.
                                    Log.Information("Ingesting data from FirstPointCom");
                                    var FirstPointComStats = await Provider.FirstPointComAsync(username, password, postgresSQL);

                                    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                                    var lockEntry = await IngestStatistics.GetLockAsync("FirstPointCom", postgresSQL).ConfigureAwait(false);
                                    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                                    // Remove all of the old numbers from the database.
                                    Log.Information("Removing old FirstPointCom numbers from the database.");
                                    var firstPointComCleanUp = await PhoneNumber.DeleteOldByProvider(start, firstPointComCycle.CycleTime, "FirstPointCom", postgresSQL).ConfigureAwait(false);

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
                                        UpdatedExisting = FirstPointComStats.UpdatedExisting
                                    };

                                    if (await combined.PostAsync(postgresSQL))
                                    {
                                        Log.Information("Completed the FirstPointCom ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("Failed to completed the FirstPointCom ingest process.");
                                    }

                                    return combined;
                                })
                            );

                        if (firstPointComCycle.RunNow)
                        {
                            firstPointComCycle.RunNow = false;
                            var checkRunNow = firstPointComCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Log.Information("[FirstPointCom] Ingest skipped.");
                    }
                }

                // Ingest phone numbers from TeleMessage.
                if (teleMessageCycle != null && (teleMessageCycle.Enabled || teleMessageCycle.RunNow))
                {
                    var lastRun = await IngestStatistics.GetLastIngestAsync("TeleMessage", postgresSQL).ConfigureAwait(false);

                    if (lastRun != null && (lastRun.StartDate < (start - teleMessageCycle.CycleTime) || teleMessageCycle.RunNow))
                    {
                        Log.Debug($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

                        Log.Information($"[TeleMessage] Cycle time is {teleMessageCycle?.CycleTime}");
                        Log.Information($"[TeleMessage] Enabled is {teleMessageCycle?.Enabled}");

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
                                    var teleStats = await Provider.TeleMessageAsync(teleToken, postgresSQL);

                                    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                                    var lockEntry = await IngestStatistics.GetLockAsync("TeleMessage", postgresSQL).ConfigureAwait(false);
                                    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                                    // Remove all of the old numbers from the database.
                                    // Remove Telemessage records at half the rate that we ingest them by doubling the delete cycle.
                                    Log.Information("Removing old TeleMessage numbers from the database.");
                                    var teleMessageCleanUp = await PhoneNumber.DeleteOldByProvider(start, teleMessageCycle.CycleTime, "TeleMessage", postgresSQL).ConfigureAwait(false);

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

                        if (teleMessageCycle.RunNow)
                        {
                            teleMessageCycle.RunNow = false;
                            var checkRunNow = teleMessageCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Log.Information("[TeleMessage] Ingest skipped.");
                    }
                }

                // Ingest phone numbers from Peerless.
                if (peerlessCycle != null && (peerlessCycle.Enabled || peerlessCycle.RunNow))
                {
                    var lastRun = await IngestStatistics.GetLastIngestAsync("Peerless", postgresSQL).ConfigureAwait(false);

                    if (lastRun != null && (lastRun.StartDate < (start - peerlessCycle.CycleTime) || peerlessCycle.RunNow))
                    {
                        Log.Debug($"Last Run of {lastRun.IngestedFrom} started at {lastRun.StartDate} and ended at {lastRun.EndDate}");

                        Log.Information($"[Peerless] Cycle time is {peerlessCycle?.CycleTime}");
                        Log.Information($"[Peerless] Enabled is {peerlessCycle?.Enabled}");

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
                                    Log.Information("[Peerless] Removing old Peerless numbers from the database.");
                                    var peerlessCleanUp = await PhoneNumber.DeleteOldByProvider(start, peerlessCycle.CycleTime, "Peerless", postgresSQL).ConfigureAwait(false);

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

                        if (peerlessCycle.RunNow)
                        {
                            peerlessCycle.RunNow = false;
                            var checkRunNow = peerlessCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Log.Information("[Peerless] Ingest skipped.");
                    }
                }

                Log.Information("[Heartbeat] Cycle complete.");

                // Limit this to 1 request every 10 seconds to the database.
                await Task.Delay(10000);
            }
        }
    }
}