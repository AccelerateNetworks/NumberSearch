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

            var appConfig = new IngestConfiguration
            {
                TeleAPI = string.IsNullOrWhiteSpace(config.GetConnectionString("TeleAPI")) ? throw new Exception("TeliAPI config key is blank.") : Guid.Parse(config.GetConnectionString("TeleAPI") ?? string.Empty),
                Postgresql = string.IsNullOrWhiteSpace(config.GetConnectionString("PostgresqlProd")) ? throw new Exception("PostgresqlProd config key is blank.") : config.GetConnectionString("PostgresqlProd") ?? string.Empty,
                BulkVSAPIKEY = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPIKEY")) ? throw new Exception("BulkVSAPIKEY config key is blank.") : config.GetConnectionString("BulkVSAPIKEY") ?? string.Empty,
                BulkVSAPISecret = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPISecret")) ? throw new Exception("BulkVSAPISecret config key is blank.") : config.GetConnectionString("BulkVSAPISecret") ?? string.Empty,
                BulkVSUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSUsername")) ? throw new Exception("BulkVSUsername config key is blank.") : config.GetConnectionString("BulkVSUsername") ?? string.Empty,
                BulkVSPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSPassword")) ? throw new Exception("BulkVSPassword config key is blank.") : config.GetConnectionString("BulkVSPassword") ?? string.Empty,
                PComNetUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetUsername")) ? throw new Exception("PComNetUsername config key is blank.") : config.GetConnectionString("PComNetUsername") ?? string.Empty,
                PComNetPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetPassword")) ? throw new Exception("PComNetPassword config key is blank.") : config.GetConnectionString("PComNetPassword") ?? string.Empty,
                PeerlessAPIKey = string.IsNullOrWhiteSpace(config.GetConnectionString("PeerlessAPIKey")) ? throw new Exception("PeerlessAPIKey config key is blank.") : config.GetConnectionString("PeerlessAPIKey") ?? string.Empty,
                Call48Username = string.IsNullOrWhiteSpace(config.GetConnectionString("Call48Username")) ? throw new Exception("Call48Username config key is blank.") : config.GetConnectionString("Call48Username") ?? string.Empty,
                Call48Password = string.IsNullOrWhiteSpace(config.GetConnectionString("Call48Password")) ? throw new Exception("Call48Password config key is blank.") : config.GetConnectionString("Call48Password") ?? string.Empty,
                SmtpUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpUsername")) ? throw new Exception("SmtpUsername config key is blank.") : config.GetConnectionString("SmtpUsername") ?? string.Empty,
                SmtpPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpPassword")) ? throw new Exception("SmtpPassword config key is blank.") : config.GetConnectionString("SmtpPassword") ?? string.Empty,
                EmailOrders = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailOrders")) ? throw new Exception("EmailOrders config key is blank.") : config.GetConnectionString("EmailOrders") ?? string.Empty,
                EmailDan = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailDan")) ? throw new Exception("EmailDan config key is blank.") : config.GetConnectionString("EmailDan") ?? string.Empty,
                EmailTom = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailTom")) ? throw new Exception("EmailTom config key is blank.") : config.GetConnectionString("EmailTom") ?? string.Empty
            };

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
            var priortyIngestCycleTime = 3600000;

            try
            {
                // To infinity and beyond.
                while (true)
                {
                    var start = DateTime.Now;

                    // Get the configuration for each of the ingest providers.
                    var cycles = await IngestCycle.GetAllAsync(appConfig.Postgresql).ConfigureAwait(false);
                    var bulkVSCycle = cycles.Where(x => x.IngestedFrom == "BulkVS").FirstOrDefault();
                    var firstPointComCycle = cycles.Where(x => x.IngestedFrom == "FirstPointCom").FirstOrDefault();
                    var teleMessageCycle = cycles.Where(x => x.IngestedFrom == "TeleMessage").FirstOrDefault();
                    var peerlessCycle = cycles.Where(x => x.IngestedFrom == "Peerless").FirstOrDefault();
                    var call48Cycle = cycles.Where(x => x.IngestedFrom == "Call48").FirstOrDefault();
                    var ownedNumbersCycle = cycles.Where(x => x.IngestedFrom == "OwnedNumbers").FirstOrDefault();

                    // Ingest phone numbers from BulkVS.
                    if (bulkVSCycle is not null && (bulkVSCycle.Enabled || bulkVSCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("BulkVS", appConfig.Postgresql).ConfigureAwait(false);

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

                            var checkLock = await lockingStats.PostAsync(appConfig.Postgresql).ConfigureAwait(false);

                            // Ingest all avablie phones numbers from the BulkVs API.
                            Log.Information("Ingesting data from BulkVS");
                            var BulkVSStats = await Provider.BulkVSAsync(appConfig.BulkVSUsername, appConfig.BulkVSPassword, PhoneNumbersNA.AreaCode.All, appConfig.Postgresql).ConfigureAwait(false);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("BulkVS", appConfig.Postgresql).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(appConfig.Postgresql).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("[BulkVS] Removing old numbers from the database.");
                            var bulkVSCleanUp = await PhoneNumber.DeleteOldByProvider(start, bulkVSCycle!.CycleTime, "BulkVS", appConfig.Postgresql).ConfigureAwait(false);

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

                            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
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
                                var checkRunNow = bulkVSCycle.PutAsync(appConfig.Postgresql).ConfigureAwait(false);
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
                            var BulkVSStats = await Provider.BulkVSAsync(appConfig.BulkVSUsername, appConfig.BulkVSPassword, AreaCode.Priority, appConfig.Postgresql).ConfigureAwait(false);

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
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, appConfig.Postgresql).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
                            {
                                Log.Information($"[BulkVS] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[BulkVS] Failed to completed the priority ingest process {DateTime.Now}.");
                            }

                            Log.Information($"[BulkVS] [PortRequests] Priority ingest of Port Request statuses started at {DateTime.Now}.");

                            // Update the statuses of all the active port requests with BulkVS.
                            await PortRequests.UpdateStatusesBulkVSAsync(appConfig);
                        }
                    }

                    // Ingest phone numbers from FirstPointCom.
                    if (firstPointComCycle is not null && (firstPointComCycle.Enabled || firstPointComCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("FirstPointCom", appConfig.Postgresql).ConfigureAwait(false);

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

                            var checkLock = await lockingStats.PostAsync(appConfig.Postgresql).ConfigureAwait(false);

                            // Ingest all avalible numbers in the FirsPointtCom API.
                            Log.Information("[FirstPointCom] Ingesting data from FirstPointCom");
                            var FirstPointComStats = await Provider.FirstPointComAsync(appConfig.PComNetUsername, appConfig.PComNetPassword, PhoneNumbersNA.AreaCode.All, appConfig.Postgresql).ConfigureAwait(false);

                            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
                            var lockEntry = await IngestStatistics.GetLockAsync("FirstPointCom", appConfig.Postgresql).ConfigureAwait(false);
                            var checkRemoveLock = await lockEntry.DeleteAsync(appConfig.Postgresql).ConfigureAwait(false);

                            // Remove all of the old numbers from the database.
                            Log.Information("[FirstPointCom] Removing old FirstPointCom numbers from the database.");
                            var firstPointComCleanUp = await PhoneNumber.DeleteOldByProvider(start, firstPointComCycle!.CycleTime, "FirstPointCom", appConfig.Postgresql).ConfigureAwait(false);

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

                            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
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
                                var checkRunNow = firstPointComCycle.PutAsync(appConfig.Postgresql).ConfigureAwait(false);
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
                            var FirstPointComStats = await Provider.FirstPointComAsync(appConfig.PComNetUsername, appConfig.PComNetPassword, AreaCode.Priority, appConfig.Postgresql).ConfigureAwait(false);

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
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, appConfig.Postgresql).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
                            {
                                Log.Information($"[FirstPointCom] Completed the priority ingest process {DateTime.Now}.");
                            }
                            else
                            {
                                Log.Fatal($"[FirstPointCom] Failed to completed the priority ingest process {DateTime.Now}.");
                            }
                        }
                    }

                    // Ingest phone numbers from Peerless.
                    if (peerlessCycle != null && (peerlessCycle.Enabled || peerlessCycle.RunNow))
                    {
                        var lastRun = await IngestStatistics.GetLastIngestAsync("Peerless", appConfig.Postgresql).ConfigureAwait(false);

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
                            var peerlessStats = await Provider.PeerlessAsync(Peerless.PriorityRateCenters, appConfig.PeerlessAPIKey, appConfig.Postgresql).ConfigureAwait(false);

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
                                var removedNumbers = await PhoneNumber.DeleteOldByProviderAndAreaCode(start, new TimeSpan(priortyIngestCycleTime), code, lastRun.IngestedFrom, appConfig.Postgresql).ConfigureAwait(false);
                                combined.Removed += removedNumbers.Removed;
                            }

                            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
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
                        var lastRun = await IngestStatistics.GetLastIngestAsync("OwnedNumbers", appConfig.Postgresql).ConfigureAwait(false);

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

                            var checkLock = await lockingStats.PostAsync(appConfig.Postgresql).ConfigureAwait(false);

                            await Owned.IngestAsync(appConfig);

                            if (ownedNumbersCycle!.RunNow)
                            {
                                ownedNumbersCycle.RunNow = false;
                                var checkRunNow = ownedNumbersCycle.PutAsync(appConfig.Postgresql).ConfigureAwait(false);
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
                            // Verify that all the Executive numbers are still purchasable for the priority area codes.
                            await Provider.VerifyAddToCartAsync(AreaCode.Priority, "Executive", appConfig.Postgresql, appConfig.BulkVSUsername, appConfig.BulkVSPassword,
                                appConfig.TeleAPI, appConfig.PComNetUsername, appConfig.PComNetPassword, appConfig.Call48Username, appConfig.Call48Password, appConfig.PeerlessAPIKey);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.StackTrace ?? "No stack trace found.");
                        }
                    }

                    Log.Information("[Heartbeat] Cycle complete.");

                    // Limit this to 1 request every 10 seconds to the database.
                    await Task.Delay(10000).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "No stack trace found.");
                Log.Fatal(ex.InnerException?.Message ?? "No inner exception message found.");
                Log.Fatal(ex.InnerException?.StackTrace ?? "No inner exception stack trace found.");
            }
            finally
            {
                // Hopefully we never get here.
                Log.Fatal("[Heartbeat] This is a complete application failure. We've broken out of the infinte loop.");

                // Notify someone that there's been a failure.
                var notificationEmail = new Email
                {
                    PrimaryEmailAddress = appConfig.EmailTom,
                    CarbonCopy = appConfig.EmailDan,
                    DateSent = DateTime.Now,
                    Subject = $"[Ingest] App is down.",
                    MessageBody = $"Something has gone wrong and the ingest app is down at {DateTime.Now}. Please capture the logs and then restart or redeploy the ingest application to restore service.",
                    OrderId = new Guid(),
                    Completed = true
                };

                var checkSend = await notificationEmail.SendEmailAsync(appConfig.SmtpUsername, appConfig.SmtpPassword).ConfigureAwait(false);
                var checkSave = await notificationEmail.PostAsync(appConfig.Postgresql).ConfigureAwait(false);

                // Save the log.
                await Log.CloseAndFlushAsync();
            }
        }

        public class IngestConfiguration
        {
            public Guid TeleAPI { get; set; }
            public string CallFlow { get; set; } = string.Empty;
            public string ChannelGroup { get; set; } = string.Empty;
            public string PComNetUsername { get; set; } = string.Empty;
            public string PComNetPassword { get; set; } = string.Empty;
            public string BulkVSAPIKEY { get; set; } = string.Empty;
            public string BulkVSAPISecret { get; set; } = string.Empty;
            public string BulkVSUsername { get; set; } = string.Empty;
            public string BulkVSPassword { get; set; } = string.Empty;
            public string Postgresql { get; set; } = string.Empty;
            public string PostgresqlProd { get; set; } = string.Empty;
            public string SmtpUsername { get; set; } = string.Empty;
            public string SmtpPassword { get; set; } = string.Empty;
            public string MicrosoftClientId { get; set; } = string.Empty;
            public string MicrosoftClientSecret { get; set; } = string.Empty;
            public string PeerlessAPIKey { get; set; } = string.Empty;
            public string InvoiceNinjaToken { get; set; } = string.Empty;
            public string Data247Username { get; set; } = string.Empty;
            public string Data247Password { get; set; } = string.Empty;
            public string EmailOrders { get; set; } = string.Empty;
            public string EmailDan { get; set; } = string.Empty;
            public string EmailTom { get; set; } = string.Empty;
            public string AzureStorageAccount { get; set; } = string.Empty;
            public string TeleDynamicsUsername { get; set; } = string.Empty;
            public string TeleDynamicsPassword { get; set; } = string.Empty;
            public string Call48Username { get; set; } = string.Empty;
            public string Call48Password { get; set; } = string.Empty;
            public string CallWithUsAPIKEY { get; set; } = string.Empty;
        }
    }
}