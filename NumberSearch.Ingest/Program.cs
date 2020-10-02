using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var smtpUsername = config.GetConnectionString("SmtpUsername");
            var smtpPassword = config.GetConnectionString("SmtpPassword");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.Console())
                .WriteTo.Async(a => a.Debug())
                .WriteTo.Async(a => a.File(
                    $"{DateTime.Now:yyyyMMdd}_NumberSearch.Ingest.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: new TimeSpan(1800000))
                )
                .CreateLogger();

            Log.Information($"[Heartbeat] Ingest scheduling loop is starting. {Environment.ProcessorCount} threads detected.");

            var tasks = new List<Task<IngestStatistics>>();

            // Priority ingest timers.
            var bulkVSPriortyTimer = new Stopwatch();
            var firstPointComPriortyTimer = new Stopwatch();
            var teleMessagePriortyTimer = new Stopwatch();
            var peerlessPriortyTimer = new Stopwatch();
            var ownedPhoneNumbers = new Stopwatch();

            // To infinity and beyond.
            while (true)
            {
                var start = DateTime.Now;

                // Get the configuration for each of the ingest providers.
                var cycles = await IngestCycle.GetAllAsync(postgresSQL).ConfigureAwait(false);
                var bulkVSCycle = cycles.Where(x => x.IngestedFrom == "BulkVS").FirstOrDefault();
                var firstPointComCycle = cycles.Where(x => x.IngestedFrom == "FirstPointCom").FirstOrDefault();
                var teleMessageCycle = cycles.Where(x => x.IngestedFrom == "TeleMessage").FirstOrDefault();
                var peerlessCycle = cycles.Where(x => x.IngestedFrom == "Peerless").FirstOrDefault();
                var ownedNumbersCycle = cycles.Where(x => x.IngestedFrom == "OwnedNumbers").FirstOrDefault();

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
                                    var BulkVSStats = await Provider.BulkVSAsync(bulkVSKey, bulkVSSecret, AreaCode.All, postgresSQL).ConfigureAwait(false);

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
                                        UpdatedExisting = BulkVSStats.UpdatedExisting,
                                        Priority = false
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
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

                    // Priority ingest.
                    if (lastRun != null && ((bulkVSPriortyTimer.ElapsedMilliseconds >= 3600000) || (!bulkVSPriortyTimer.IsRunning)))
                    {
                        if (!bulkVSPriortyTimer.IsRunning)
                        {
                            bulkVSPriortyTimer.Start();
                        }

                        // Restart the one hour timer.
                        bulkVSPriortyTimer.Restart();

                        Log.Debug($"[BulkVS] Priority ingest started at {DateTime.Now}.");

                        tasks.Add(
                                Task.Run(async () =>
                                {
                                    // Ingest priority phones numbers from the BulkVs API.
                                    Log.Information("[BulkVS] Ingesting priority data from BulkVS.");
                                    var BulkVSStats = await Provider.BulkVSAsync(bulkVSKey, bulkVSSecret, AreaCode.Priority, postgresSQL).ConfigureAwait(false);

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

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                                    {
                                        Log.Information("[BulkVS] Completed the priority ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("[BulkVS] Failed to completed the priority ingest process.");
                                    }

                                    return combined;
                                })
                            );

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
                                    var FirstPointComStats = await Provider.FirstPointComAsync(username, password, AreaCode.All, postgresSQL).ConfigureAwait(false);

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
                                        UpdatedExisting = FirstPointComStats.UpdatedExisting,
                                        Priority = false
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
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

                    // Priority ingest.
                    if (lastRun != null && ((firstPointComPriortyTimer.ElapsedMilliseconds >= 3600000) || (!firstPointComPriortyTimer.IsRunning)))
                    {
                        if (!firstPointComPriortyTimer.IsRunning)
                        {
                            firstPointComPriortyTimer.Start();
                        }

                        // Restart the one hour timer.
                        firstPointComPriortyTimer.Restart();

                        Log.Debug($"[FirstPointCom] Priority ingest started at {DateTime.Now}");

                        tasks.Add(
                                Task.Run(async () =>
                                {
                                    // Ingest priority numbers in the FirsPointCom API.
                                    Log.Information("[FirstPointCom] Ingesting priority data from FirstPointCom");
                                    var FirstPointComStats = await Provider.FirstPointComAsync(username, password, AreaCode.Priority, postgresSQL).ConfigureAwait(false);

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

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                                    {
                                        Log.Information("[FirstPointCom] Completed the priority ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("[FirstPointCom] Failed to completed the priority ingest process.");
                                    }

                                    return combined;
                                })
                            );
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
                                    var teleStats = await Provider.TeleMessageAsync(teleToken, new int[] { }, postgresSQL).ConfigureAwait(false);

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
                                        UpdatedExisting = teleStats.UpdatedExisting,
                                        Priority = false
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
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

                    // Priority ingest.
                    if (lastRun != null && ((teleMessagePriortyTimer.ElapsedMilliseconds >= 3600000) || (!teleMessagePriortyTimer.IsRunning)))
                    {
                        if (!teleMessagePriortyTimer.IsRunning)
                        {
                            teleMessagePriortyTimer.Start();
                        }

                        // Restart the one hour timer.
                        teleMessagePriortyTimer.Restart();

                        Log.Debug($"[TeleMessage] Priority ingest started at {DateTime.Now}.");

                        tasks.Add(
                                Task.Run(async () =>
                                {
                                    // Ingest all avalible numbers from the TeleMessage.
                                    Log.Information("[TeleMessage] Ingesting priority data from TeleMessage");
                                    var teleStats = await Provider.TeleMessageAsync(teleToken, AreaCode.Priority, postgresSQL).ConfigureAwait(false);

                                    var combined = new IngestStatistics
                                    {
                                        StartDate = teleStats.StartDate,
                                        EndDate = DateTime.Now,
                                        FailedToIngest = teleStats.FailedToIngest,
                                        IngestedFrom = teleStats.IngestedFrom,
                                        IngestedNew = teleStats.IngestedNew,
                                        Lock = false,
                                        NumbersRetrived = teleStats.NumbersRetrived,
                                        Removed = 0,
                                        Unchanged = teleStats.Unchanged,
                                        UpdatedExisting = teleStats.UpdatedExisting,
                                        Priority = true
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                                    {
                                        Log.Information("[TeleMessage] Completed the priority ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("[TeleMessage] Failed to completed the priority ingest process.");
                                    }

                                    return combined;
                                })
                            );
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
                                    var peerlessStats = await Peerless.IngestPhoneNumbersAsync(peerlessApiKey, AreaCode.All, postgresSQL).ConfigureAwait(false);

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
                                        UpdatedExisting = peerlessStats.UpdatedExisting,
                                        Priority = false
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
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

                    // Priority ingest.
                    if (lastRun != null && ((peerlessPriortyTimer.ElapsedMilliseconds >= 3600000) || (!peerlessPriortyTimer.IsRunning)))
                    {
                        if (!peerlessPriortyTimer.IsRunning)
                        {
                            peerlessPriortyTimer.Start();
                        }

                        // Restart the one hour timer.
                        peerlessPriortyTimer.Restart();

                        Log.Debug($"[Peerless] Priority ingest started at {DateTime.Now}.");

                        tasks.Add(
                                Task.Run(async () =>
                                {
                                    // Ingest priority numbers from the TeleMessage.
                                    Log.Information("[Peerless] Ingesting priority data from Peerless");
                                    var peerlessStats = await Peerless.IngestPhoneNumbersAsync(peerlessApiKey, AreaCode.Priority, postgresSQL).ConfigureAwait(false);

                                    var combined = new IngestStatistics
                                    {
                                        StartDate = peerlessStats.StartDate,
                                        EndDate = DateTime.Now,
                                        FailedToIngest = peerlessStats.FailedToIngest,
                                        IngestedFrom = peerlessStats.IngestedFrom,
                                        IngestedNew = peerlessStats.IngestedNew,
                                        Lock = false,
                                        NumbersRetrived = peerlessStats.NumbersRetrived,
                                        Removed = 0,
                                        Unchanged = peerlessStats.Unchanged,
                                        UpdatedExisting = peerlessStats.UpdatedExisting,
                                        Priority = true
                                    };

                                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                                    {
                                        Log.Information("[Peerless] Completed the priority ingest process.");
                                    }
                                    else
                                    {
                                        Log.Fatal("[Peerless] Failed to complete the priority ingest process.");
                                    }

                                    return combined;
                                })
                            );
                    }
                }

                // Ingest all the phone numbers we own.
                //if (ownedNumbersCycle != null && (ownedNumbersCycle.Enabled || ownedNumbersCycle.RunNow))
                //{
                //    var lastRun = await IngestStatistics.GetLastIngestAsync("OwnedNumbers", postgresSQL).ConfigureAwait(false);

                //    if (lastRun != null && (lastRun.StartDate < (start - ownedNumbersCycle.CycleTime) || ownedNumbersCycle.RunNow))
                //    {
                //        Log.Debug($"Last Run of {lastRun.IngestedFrom} started at {lastRun.StartDate} and ended at {lastRun.EndDate}");

                //        Log.Information($"[OwnedNumbers] Cycle time is {ownedNumbersCycle?.CycleTime}");
                //        Log.Information($"[OwnedNumbers] Enabled is {ownedNumbersCycle?.Enabled}");

                //        // Prevent another run from starting while this is still going.
                //        var lockingStats = new IngestStatistics
                //        {
                //            IngestedFrom = "OwnedNumbers",
                //            StartDate = DateTime.Now,
                //            EndDate = DateTime.Now,
                //            IngestedNew = 0,
                //            FailedToIngest = 0,
                //            NumbersRetrived = 0,
                //            Removed = 0,
                //            Unchanged = 0,
                //            UpdatedExisting = 0,
                //            Lock = true
                //        };

                //        var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                //        tasks.Add(
                //                Task.Run(async () =>
                //                {
                //                    // Ingest all owned numbers from the providers.
                //                    Log.Information("[OwnedNumbers] Ingesting data from OwnedNumbers.");
                //                    var firstComNumbers = await Owned.FirstPointComAsync(username, password).ConfigureAwait(false);
                //                    var teleMessageNumbers = await Owned.TeleMessageAsync(teleToken).ConfigureAwait(false);
                //                    var bulkVSNumbers = await BulkVS.BulkVSOwnedPhoneNumbers.GetAllAsync(bulkVSKey, bulkVSSecret).ConfigureAwait(false);
                //                    var allNumbers = new List<OwnedPhoneNumber>();

                //                    if (firstComNumbers != null)
                //                    {
                //                        allNumbers.AddRange(firstComNumbers);
                //                    };

                //                    if (teleMessageNumbers != null)
                //                    {
                //                        allNumbers.AddRange(teleMessageNumbers);
                //                    };

                //                    if (bulkVSNumbers != null)
                //                    {
                //                        allNumbers.AddRange(bulkVSNumbers);
                //                    };

                //                    Log.Information($"[OwnedNumbers] Submitting {allNumbers.Count} numbers to the database.");

                //                    var ownedNumberStats = await Owned.SubmitOwnedNumbersAsync(allNumbers, postgresSQL).ConfigureAwait(false);

                //                    // Look for LRN changes.
                //                    Log.Information("[OwnedNumbers] Looking for LRN changes on owned numbers.");
                //                    var changedNumbers = await Owned.VerifyServiceProvidersAsync(teleToken, bulkVSKey, postgresSQL).ConfigureAwait(false);

                //                    if (changedNumbers != null && changedNumbers.Any())
                //                    {
                //                        Log.Information($"[OwnedNumbers] Emailing out a notification that {changedNumbers.Count()} numbers LRN updates.");
                //                        var checkSend = await Owned.SendPortingNotificationEmailAsync(changedNumbers, smtpUsername, smtpPassword, postgresSQL).ConfigureAwait(false);
                //                    }

                //                    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                //                    var lockEntry = await IngestStatistics.GetLockAsync("OwnedNumbers", postgresSQL).ConfigureAwait(false);
                //                    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                //                    // Remove all of the old numbers from the database.
                //                    Log.Information("[OwnedNumbers] Marking numbers that failed to reingest as inactive in the database.");
                //                    // TODO: Mark old owned numbers as in active.

                //                    var combined = new IngestStatistics
                //                    {
                //                        StartDate = ownedNumberStats.StartDate,
                //                        EndDate = DateTime.Now,
                //                        FailedToIngest = ownedNumberStats.FailedToIngest,
                //                        IngestedFrom = ownedNumberStats.IngestedFrom,
                //                        IngestedNew = ownedNumberStats.IngestedNew,
                //                        Lock = false,
                //                        NumbersRetrived = ownedNumberStats.NumbersRetrived,
                //                        Removed = 0,
                //                        Unchanged = ownedNumberStats.Unchanged,
                //                        UpdatedExisting = ownedNumberStats.UpdatedExisting,
                //                        Priority = false
                //                    };

                //                    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                //                    {
                //                        Log.Information("[OwnedNumbers] Completed the ingest process.");
                //                    }
                //                    else
                //                    {
                //                        Log.Fatal("[OwnedNumbers] Failed to completed the ingest process.");
                //                    }

                //                    return combined;
                //                })
                //            );

                //        if (ownedNumbersCycle.RunNow)
                //        {
                //            ownedNumbersCycle.RunNow = false;
                //            var checkRunNow = ownedNumbersCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                //        }
                //    }
                //}

                Log.Information("[Heartbeat] Cycle complete.");

                // Limit this to 1 request every 10 seconds to the database.
                await Task.Delay(10000).ConfigureAwait(false);
            }
        }
    }
}