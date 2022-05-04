using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
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
            .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
            .Build();

            var teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            var postgresSQL = config.GetConnectionString("PostgresqlProd");
            var bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            var bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
            var bulkVSusername = config.GetConnectionString("BulkVSUsername");
            var bulkVSpassword = config.GetConnectionString("BulkVSPassword");
            var username = config.GetConnectionString("PComNetUsername");
            var password = config.GetConnectionString("PComNetPassword");
            var peerlessApiKey = config.GetConnectionString("PeerlessAPIKey");
            var call48Username = config.GetConnectionString("Call48Username");
            var call48Password = config.GetConnectionString("Call48Password");
            var smtpUsername = config.GetConnectionString("SmtpUsername");
            var smtpPassword = config.GetConnectionString("SmtpPassword");
            var emailOrders = config.GetConnectionString("EmailOrders");
            var emailDan = config.GetConnectionString("EmailDan");
            var emailTom = config.GetConnectionString("EmailTom");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    $"{DateTime.Now:yyyyMMdd}_NumberSearch.Ingest.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: new TimeSpan(1800000)
                )
                .CreateLogger();

            Log.Information($"[Heartbeat] Ingest scheduling loop is starting. {Environment.ProcessorCount} threads detected.");

            // Priority ingest timers.
            var bulkVSPriortyTimer = new Stopwatch();
            var firstPointComPriortyTimer = new Stopwatch();
            var teleMessagePriortyTimer = new Stopwatch();
            var call48PriortyTimer = new Stopwatch();
            var peerlessPriortyTimer = new Stopwatch();
            var orderUpdatesTimer = new Stopwatch();
            // 20 Minutes in miliseconds
            var priortyIngestCycleTime = 1200000;

            try
            {
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
                    var call48Cycle = cycles.Where(x => x.IngestedFrom == "Call48").FirstOrDefault();
                    var ownedNumbersCycle = cycles.Where(x => x.IngestedFrom == "OwnedNumbers").FirstOrDefault();

                    // Ingest phone numbers from BulkVS.
                    if (bulkVSCycle is not null && (bulkVSCycle.Enabled || bulkVSCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("BulkVS", postgresSQL).ConfigureAwait(false);

                        // If the last ingest was run to recently do nothing.
                        if (lastRun is not null && (lastRun.StartDate < (start - bulkVSCycle.CycleTime) || bulkVSCycle.RunNow))
                        {
                            Log.Information($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

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

                            // Ingest all avablie phones numbers from the BulkVs API.
                            Log.Information("Ingesting data from BulkVS");
                            var BulkVSStats = await Provider.BulkVSAsync(bulkVSusername, bulkVSpassword, PhoneNumbersNA.AreaCode.All, postgresSQL).ConfigureAwait(false);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("BulkVS", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("[BulkVS] Removing old numbers from the database.");
                            var bulkVSCleanUp = await PhoneNumber.DeleteOldByProvider(start, bulkVSCycle!.CycleTime, "BulkVS", postgresSQL).ConfigureAwait(false);

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
                                Log.Information($"[BulkVS] Completed the ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[BulkVS] Failed to completed the ingest process {DateTime.Now}.");
                            }

                            if (bulkVSCycle.RunNow)
                            {
                                bulkVSCycle.RunNow = false;
                                var checkRunNow = bulkVSCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                            }

                        }

                        // Priority ingest.
                        if (lastRun != null && ((bulkVSPriortyTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!bulkVSPriortyTimer.IsRunning)))
                        {
                            if (!bulkVSPriortyTimer.IsRunning)
                            {
                                bulkVSPriortyTimer.Start();
                            }

                            // Restart the one hour timer.
                            bulkVSPriortyTimer.Restart();

                            Log.Information($"[BulkVS] Priority ingest started at {DateTime.Now}.");

                            // Ingest priority phones numbers from the BulkVs API.
                            Log.Information("[BulkVS] Ingesting priority data from BulkVS.");
                            var BulkVSStats = await Provider.BulkVSAsync(bulkVSusername, bulkVSpassword, AreaCode.Priority, postgresSQL).ConfigureAwait(false);

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
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, postgresSQL).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                            {
                                Log.Information($"[BulkVS] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[BulkVS] Failed to completed the priority ingest process {DateTime.Now}.");
                            }

                            Log.Information($"[BulkVS] [PortRequests] Priority ingest of Port Request statuses started at {DateTime.Now}.");

                            // Update the statuses of all the active port requests with BulkVS.
                            await PortRequests.UpdateStatusesBulkVSAsync(config);
                        }
                    }

                    // Ingest phone numbers from FirstPointCom.
                    if (firstPointComCycle is not null && (firstPointComCycle.Enabled || firstPointComCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("FirstPointCom", postgresSQL).ConfigureAwait(false);

                        if (lastRun is not null && (lastRun.StartDate < (start - firstPointComCycle.CycleTime) || firstPointComCycle.RunNow))
                        {
                            Log.Information($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

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

                            // Ingest all avalible numbers in the FirsPointtCom API.
                            Log.Information("[FirstPointCom] Ingesting data from FirstPointCom");
                            var FirstPointComStats = await Provider.FirstPointComAsync(username, password, PhoneNumbersNA.AreaCode.All, postgresSQL).ConfigureAwait(false);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("FirstPointCom", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("[FirstPointCom] Removing old FirstPointCom numbers from the database.");
                            var firstPointComCleanUp = await PhoneNumber.DeleteOldByProvider(start, firstPointComCycle!.CycleTime, "FirstPointCom", postgresSQL).ConfigureAwait(false);

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
                                Log.Information($"[FirstPointCom] Completed the FirstPointCom ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[FirstPointCom] Failed to completed the FirstPointCom ingest process {DateTime.Now}.");
                            }

                            if (firstPointComCycle.RunNow)
                            {
                                firstPointComCycle.RunNow = false;
                                var checkRunNow = firstPointComCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                            }
                        }

                        // Priority ingest.
                        if (lastRun != null && ((firstPointComPriortyTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!firstPointComPriortyTimer.IsRunning)))
                        {
                            if (!firstPointComPriortyTimer.IsRunning)
                            {
                                firstPointComPriortyTimer.Start();
                            }

                            // Restart the one hour timer.
                            firstPointComPriortyTimer.Restart();

                            Log.Debug($"[FirstPointCom] Priority ingest started at {DateTime.Now}");


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

                            // Remove stale priority numbers
                            foreach (var code in AreaCode.Priority)
                            {
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, postgresSQL).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                            {
                                Log.Information($"[FirstPointCom] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[FirstPointCom] Failed to completed the priority ingest process {DateTime.Now}.");
                            }
                        }
                    }

                    // Ingest phone numbers from TeleMessage.
                    if (teleMessageCycle is not null && (teleMessageCycle.Enabled || teleMessageCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("TeleMessage", postgresSQL).ConfigureAwait(false);

                        if (lastRun is not null && (lastRun.StartDate < (start - teleMessageCycle.CycleTime) || teleMessageCycle.RunNow))
                        {
                            Log.Information($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

                            Log.Information($"[TeliMessage] Cycle time is {teleMessageCycle?.CycleTime}");
                            Log.Information($"[TeliMessage] Enabled is {teleMessageCycle?.Enabled}");

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

                            // Ingest all avalible numbers from the TeleMessage.
                            Log.Information("Ingesting data from TeliMessage");
                            var teleStats = new IngestStatistics
                            {
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now,
                                FailedToIngest = 0,
                                IngestedFrom = "TeleMessage",
                                IngestedNew = 0,
                                Lock = false,
                                NumbersRetrived = 0,
                                Removed = 0,
                                Unchanged = 0,
                                UpdatedExisting = 0,
                                Priority = true
                            };

                            try
                            {
                                teleStats = await Provider.TeliMessageAsync(teleToken, Array.Empty<int>(), postgresSQL).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Log.Fatal("[TeliMessage] Failed to completed the priority ingest process.");
                                Log.Fatal($"[TeliMessage] {ex.Message} {ex.InnerException}");
                            }

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("TeleMessage", postgresSQL).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            // Remove Telemessage records at half the rate that we ingest them by doubling the delete cycle.
                            Log.Information("[TeliMessage] Removing old TeleMessage numbers from the database.");
                            var teleMessageCleanUp = await PhoneNumber.DeleteOldByProvider(start, teleMessageCycle!.CycleTime, "TeleMessage", postgresSQL).ConfigureAwait(false);

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
                                Log.Information($"[TeliMessage] Completed the TeleMessage ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[TeliMessage] Failed to completed the TeleMessage ingest process {DateTime.Now}.");
                            }

                            if (teleMessageCycle.RunNow)
                            {
                                teleMessageCycle.RunNow = false;
                                var checkRunNow = teleMessageCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                            }
                        }

                        // Priority ingest.
                        if (lastRun != null && ((teleMessagePriortyTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!teleMessagePriortyTimer.IsRunning)))
                        {
                            if (!teleMessagePriortyTimer.IsRunning)
                            {
                                teleMessagePriortyTimer.Start();
                            }

                            // Restart the one hour timer.
                            teleMessagePriortyTimer.Restart();

                            Log.Debug($"[TeliMessage] Priority ingest started at {DateTime.Now}.");

                            // Ingest all avalible numbers from the TeleMessage.
                            Log.Information("[TeliMessage] Ingesting priority data from TeleMessage");
                            var teleStats = new IngestStatistics
                            {
                                StartDate = DateTime.Now,
                                EndDate = DateTime.Now,
                                FailedToIngest = 0,
                                IngestedFrom = "TeleMessage",
                                IngestedNew = 0,
                                Lock = false,
                                NumbersRetrived = 0,
                                Removed = 0,
                                Unchanged = 0,
                                UpdatedExisting = 0,
                                Priority = true
                            };
                            try
                            {
                                teleStats = await Provider.TeliMessageAsync(teleToken, AreaCode.Priority, postgresSQL).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Log.Fatal("[TeliMessage] Failed to completed the priority ingest process.");
                                Log.Fatal($"[TeliMessage] {ex.Message} {ex.InnerException}");
                            }

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

                            // Remove stale priority numbers
                            foreach (var code in AreaCode.Priority)
                            {
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, postgresSQL).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                            {
                                Log.Information($"[TeliMessage] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[TeliMessage] Failed to completed the priority ingest process {DateTime.Now}.");
                            }

                            // Update the statuses of all active TeliMessage port request.
                            await PortRequests.UpdateStatusesTeliMessageAsync(config);
                        }
                    }

                    // Ingest phone numbers from Call48.
                    if (call48Cycle != null && (call48Cycle.Enabled || call48Cycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("Call48", postgresSQL).ConfigureAwait(false);

                        // If the last ingest was run to recently do nothing.
                        // Disabled because we don't get that many numbers back from them anymore.
                        //if (lastRun != null && (lastRun.StartDate < (start - call48Cycle.CycleTime) || call48Cycle.RunNow))
                        //{
                        //    Log.Information($"Last Run of {lastRun?.IngestedFrom} started at {lastRun?.StartDate} and ended at {lastRun?.EndDate}");

                        //    Log.Information($"[Call48] Cycle time is {call48Cycle?.CycleTime}");
                        //    Log.Information($"[Call48] Enabled is {call48Cycle?.Enabled}");

                        //    // Prevent another run from starting while this is still going.
                        //    var lockingStats = new IngestStatistics
                        //    {
                        //        IngestedFrom = "Call48",
                        //        StartDate = DateTime.Now,
                        //        EndDate = DateTime.Now,
                        //        IngestedNew = 0,
                        //        FailedToIngest = 0,
                        //        NumbersRetrived = 0,
                        //        Removed = 0,
                        //        Unchanged = 0,
                        //        UpdatedExisting = 0,
                        //        Lock = true
                        //    };

                        //    var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                        //    // Ingest all avablie phones numbers from the BulkVs API.
                        //    Log.Information("Ingesting data from Call48");
                        //    var call48Stats = await Provider.Call48Async(call48Username, call48Password, AreaCode.States, postgresSQL).ConfigureAwait(false);

                        //    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                        //    var lockEntry = await IngestStatistics.GetLockAsync("Call48", postgresSQL).ConfigureAwait(false);
                        //    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                        //    // Remove all of the old numbers from the database.
                        //    Log.Information("[Call48] Removing old numbers from the database.");
                        //    var call48CleanUp = await PhoneNumber.DeleteOldByProvider(start, call48Cycle.CycleTime, "Call48", postgresSQL).ConfigureAwait(false);

                        //    var combined = new IngestStatistics
                        //    {
                        //        StartDate = call48Stats.StartDate,
                        //        EndDate = call48CleanUp.EndDate,
                        //        FailedToIngest = call48Stats.FailedToIngest,
                        //        IngestedFrom = call48Stats.IngestedFrom,
                        //        IngestedNew = call48Stats.IngestedNew,
                        //        Lock = false,
                        //        NumbersRetrived = call48Stats.NumbersRetrived,
                        //        Removed = call48CleanUp.Removed,
                        //        Unchanged = call48Stats.Unchanged,
                        //        UpdatedExisting = call48Stats.UpdatedExisting,
                        //        Priority = false
                        //    };

                        //    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                        //    {
                        //        Log.Information($"[Call48] Completed the ingest process {DateTime.Now}.");
                        //    }
                        //    else
                        //    {
                        //        Log.Fatal($"[Call48] Failed to completed the ingest process {DateTime.Now}.");
                        //    }

                        //    if (call48Cycle.RunNow)
                        //    {
                        //        call48Cycle.RunNow = false;
                        //        var checkRunNow = call48Cycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        //    }
                        //}

                        // Priority ingest.
                        if (lastRun != null && ((call48PriortyTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!call48PriortyTimer.IsRunning)))
                        {
                            if (!call48PriortyTimer.IsRunning)
                            {
                                call48PriortyTimer.Start();
                            }

                            // Restart the 20 minute timer.
                            call48PriortyTimer.Restart();

                            Log.Information($"[Call48] Priority ingest started at {DateTime.Now}.");

                            // Ingest priority phones numbers from the Call48 API.
                            Log.Information("[Call48] Ingesting priority data from Call48.");
                            var call48Stats = await Provider.Call48Async(call48Username, call48Password, PhoneNumbersNA.AreaCode.States.ToArray().Where(x => x.State == "Oregon" || x.State == "Washington").ToArray(), postgresSQL).ConfigureAwait(false);

                            var combined = new IngestStatistics
                            {
                                StartDate = call48Stats.StartDate,
                                EndDate = DateTime.Now,
                                FailedToIngest = call48Stats.FailedToIngest,
                                IngestedFrom = call48Stats.IngestedFrom,
                                IngestedNew = call48Stats.IngestedNew,
                                Lock = false,
                                NumbersRetrived = call48Stats.NumbersRetrived,
                                Removed = 0,
                                Unchanged = call48Stats.Unchanged,
                                UpdatedExisting = call48Stats.UpdatedExisting,
                                Priority = true
                            };

                            // Remove stale priority numbers
                            foreach (var code in AreaCode.Priority)
                            {
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, postgresSQL).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                            {
                                Log.Information($"[Call48] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[Call48] Failed to completed the priority ingest process {DateTime.Now}.");
                            }
                        }
                    }


                    // Ingest phone numbers from Peerless.
                    if (peerlessCycle != null && (peerlessCycle.Enabled || peerlessCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("Peerless", postgresSQL).ConfigureAwait(false);

                        //if (lastRun != null && (lastRun.StartDate < (start - peerlessCycle.CycleTime) || peerlessCycle.RunNow))
                        //{
                        //    Log.Information($"[Peerless] Last Run of {lastRun.IngestedFrom} started at {lastRun.StartDate} and ended at {lastRun.EndDate}");

                        //    Log.Information($"[Peerless] Cycle time is {peerlessCycle?.CycleTime}");
                        //    Log.Information($"[Peerless] Enabled is {peerlessCycle?.Enabled}");

                        //    // Prevent another run from starting while this is still going.
                        //    var lockingStats = new IngestStatistics
                        //    {
                        //        IngestedFrom = "Peerless",
                        //        StartDate = DateTime.Now,
                        //        EndDate = DateTime.Now,
                        //        IngestedNew = 0,
                        //        FailedToIngest = 0,
                        //        NumbersRetrived = 0,
                        //        Removed = 0,
                        //        Unchanged = 0,
                        //        UpdatedExisting = 0,
                        //        Lock = true
                        //    };

                        //    var checkLock = await lockingStats.PostAsync(postgresSQL).ConfigureAwait(false);

                        //    // Ingest all avalible numbers from the TeleMessage.
                        //    Log.Information("[Peerless] Ingesting data from Peerless");
                        //    var peerlessStats = await Provider.PeerlessAsync(PhoneNumbersNA.AreaCode.All, peerlessApiKey, postgresSQL).ConfigureAwait(false);

                        //    // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                        //    var lockEntry = await IngestStatistics.GetLockAsync("Peerless", postgresSQL).ConfigureAwait(false);
                        //    var checkRemoveLock = await lockEntry.DeleteAsync(postgresSQL).ConfigureAwait(false);

                        //    // Remove all of the old numbers from the database.
                        //    Log.Information("[Peerless] Removing old Peerless numbers from the database.");
                        //    var peerlessCleanUp = await PhoneNumber.DeleteOldByProvider(start, peerlessCycle.CycleTime, "Peerless", postgresSQL).ConfigureAwait(false);

                        //    var combined = new IngestStatistics
                        //    {
                        //        StartDate = peerlessStats.StartDate,
                        //        EndDate = peerlessCleanUp.EndDate,
                        //        FailedToIngest = peerlessStats.FailedToIngest,
                        //        IngestedFrom = peerlessStats.IngestedFrom,
                        //        IngestedNew = peerlessStats.IngestedNew,
                        //        Lock = false,
                        //        NumbersRetrived = peerlessStats.NumbersRetrived,
                        //        Removed = peerlessCleanUp.Removed,
                        //        Unchanged = peerlessStats.Unchanged,
                        //        UpdatedExisting = peerlessStats.UpdatedExisting,
                        //        Priority = false
                        //    };

                        //    if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                        //    {
                        //        Log.Information("[Peerless] Completed the Peerless ingest process.");
                        //    }
                        //    else
                        //    {
                        //        Log.Fatal("[Peerless] Failed to completed the Peerless ingest process.");
                        //    }

                        //    if (peerlessCycle.RunNow)
                        //    {
                        //        peerlessCycle.RunNow = false;
                        //        var checkRunNow = peerlessCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                        //    }
                        //}

                        // Priority ingest.
                        if (lastRun != null && ((peerlessPriortyTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!peerlessPriortyTimer.IsRunning)))
                        {
                            if (!peerlessPriortyTimer.IsRunning)
                            {
                                peerlessPriortyTimer.Start();
                            }

                            // Restart the one hour timer.
                            peerlessPriortyTimer.Restart();

                            Log.Information($"[Peerless] Priority ingest started at {DateTime.Now}.");

                            // Ingest priority numbers from the TeleMessage.
                            Log.Information("[Peerless] Ingesting priority data from Peerless");
                            var peerlessStats = await Provider.PeerlessAsync(AreaCode.Priority, peerlessApiKey, postgresSQL).ConfigureAwait(false);

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

                            // Remove stale priority numbers
                            foreach (var code in AreaCode.Priority)
                            {
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, postgresSQL).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(postgresSQL).ConfigureAwait(false))
                            {
                                Log.Information("[Peerless] Completed the priority ingest process.");
                            }
                            else
                            {
                                Log.Fatal("[Peerless] Failed to complete the priority ingest process.");
                            }
                        }
                    }

                    // Ingest all the phone numbers we own.
                    if (ownedNumbersCycle is not null && (ownedNumbersCycle.Enabled || ownedNumbersCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("OwnedNumbers", postgresSQL).ConfigureAwait(false);

                        if (lastRun is not null && (lastRun.StartDate < (start - ownedNumbersCycle.CycleTime) || ownedNumbersCycle.RunNow))
                        {
                            Log.Debug($"Last Run of {lastRun.IngestedFrom} started at {lastRun.StartDate} and ended at {lastRun.EndDate}");

                            Log.Information($"[OwnedNumbers] Cycle time is {ownedNumbersCycle?.CycleTime}");
                            Log.Information($"[OwnedNumbers] Enabled is {ownedNumbersCycle?.Enabled}");

                            // Prevent another run from starting while this is still going.
                            var lockingStats = new IngestStatistics
                            {
                                IngestedFrom = "OwnedNumbers",
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

                            await Owned.IngestAsync(config);

                            if (ownedNumbersCycle!.RunNow)
                            {
                                ownedNumbersCycle.RunNow = false;
                                var checkRunNow = ownedNumbersCycle.PutAsync(postgresSQL).ConfigureAwait(false);
                            }
                        }
                    }

                    // Update orders from the billing system.
                    if (((orderUpdatesTimer.ElapsedMilliseconds >= priortyIngestCycleTime) || (!orderUpdatesTimer.IsRunning)))
                    {
                        if (!orderUpdatesTimer.IsRunning)
                        {
                            orderUpdatesTimer.Start();
                        }

                        // Restart the one hour timer.
                        orderUpdatesTimer.Restart();


                        try
                        {
                            await Orders.UpdateOrdersAsync(config);

                            // Verify that all the Executive numbers are still purchasable for the priority area codes.
                            await Provider.VerifyAddToCartAsync(AreaCode.Priority, "Executive", postgresSQL, bulkVSusername, bulkVSpassword,
                                teleToken, username, password, call48Username, call48Password, peerlessApiKey);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.StackTrace);
                        }
                    }

                    Log.Information("[Heartbeat] Cycle complete.");

                    // Limit this to 1 request every 10 seconds to the database.
                    await Task.Delay(10000).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex?.Message);
                Log.Fatal(ex?.StackTrace);
                Log.Fatal(ex?.InnerException?.Message);
                Log.Fatal(ex?.InnerException?.StackTrace);
            }
            finally
            {
                // Hopefully we never get here.
                Log.Fatal("[Heartbeat] This is a complete application failure. We've broken out of the infinte loop.");

                // Notify someone that there's been a failure.
                var notificationEmail = new Email
                {
                    PrimaryEmailAddress = emailTom,
                    CarbonCopy = emailDan,
                    DateSent = DateTime.Now,
                    Subject = $"[Ingest] App is down.",
                    MessageBody = $"Something has gone wrong and the ingest app is down at {DateTime.Now}. Please capture the logs and then restart or redeploy the ingest application to restore service.",
                    OrderId = new Guid(),
                    Completed = true
                };

                var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
                var checkSave = await notificationEmail.PostAsync(postgresSQL).ConfigureAwait(false);

                // Save the log.
                Log.CloseAndFlush();
            }
        }
    }
}