using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Diagnostics;
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
                Postgresql = string.IsNullOrWhiteSpace(config.GetConnectionString("PostgresqlProd")) ? throw new Exception("PostgresqlProd config key is blank.") : config.GetConnectionString("PostgresqlProd") ?? string.Empty,
                BulkVSAPIKEY = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPIKEY")) ? throw new Exception("BulkVSAPIKEY config key is blank.") : config.GetConnectionString("BulkVSAPIKEY") ?? string.Empty,
                BulkVSAPISecret = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPISecret")) ? throw new Exception("BulkVSAPISecret config key is blank.") : config.GetConnectionString("BulkVSAPISecret") ?? string.Empty,
                BulkVSUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSUsername")) ? throw new Exception("BulkVSUsername config key is blank.") : config.GetConnectionString("BulkVSUsername") ?? string.Empty,
                BulkVSPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSPassword")) ? throw new Exception("BulkVSPassword config key is blank.") : config.GetConnectionString("BulkVSPassword") ?? string.Empty,
                PComNetUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetUsername")) ? throw new Exception("PComNetUsername config key is blank.") : config.GetConnectionString("PComNetUsername") ?? string.Empty,
                PComNetPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetPassword")) ? throw new Exception("PComNetPassword config key is blank.") : config.GetConnectionString("PComNetPassword") ?? string.Empty,
                SmtpUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpUsername")) ? throw new Exception("SmtpUsername config key is blank.") : config.GetConnectionString("SmtpUsername") ?? string.Empty,
                SmtpPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpPassword")) ? throw new Exception("SmtpPassword config key is blank.") : config.GetConnectionString("SmtpPassword") ?? string.Empty,
                EmailOrders = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailOrders")) ? throw new Exception("EmailOrders config key is blank.") : config.GetConnectionString("EmailOrders") ?? string.Empty,
                EmailDan = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailDan")) ? throw new Exception("EmailDan config key is blank.") : config.GetConnectionString("EmailDan") ?? string.Empty,
                EmailTom = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailTom")) ? throw new Exception("EmailTom config key is blank.") : config.GetConnectionString("EmailTom") ?? string.Empty,
                InvoiceNinjaToken = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailTom")) ? throw new Exception("InvoiceNinjaToken config key is blank.") : config.GetConnectionString("InvoiceNinjaToken") ?? string.Empty,
            };

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Async(x => x.File(
                    $"{DateTime.Now:yyyyMMdd}_NumberSearch.Ingest.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    buffered: true
                ))
                .CreateLogger();

            Log.Information($"[Heartbeat] Ingest scheduling loop is starting. {Environment.ProcessorCount} threads detected.");
            Stopwatch priorityTimer = new();
            Stopwatch dailyTimer = new();
            Stopwatch bulkVSTimer = new();
            Stopwatch fpcTimer = new();
            TimeSpan dailyCycle = TimeSpan.FromDays(1);
            TimeSpan priorityCycle = TimeSpan.FromMinutes(10);
            TimeSpan bulkVSCycle = TimeSpan.FromHours(1);
            TimeSpan fpcCycle = TimeSpan.FromHours(3);

            try
            {
                if (!dailyTimer.IsRunning)
                {
                    dailyTimer.Start();
                }

                if (!priorityTimer.IsRunning)
                {
                    priorityTimer.Start();
                }

                if (!bulkVSTimer.IsRunning)
                {
                    bulkVSTimer.Start();
                }

                if (!fpcTimer.IsRunning)
                {
                    fpcTimer.Start();
                }

                // To infinity and beyond.
                while (true)
                {
                    var start = DateTime.Now;

                    // Priority Ingest
                    if (priorityTimer.Elapsed >= priorityCycle)
                    {
                        priorityTimer.Restart();

                        var bulkVS = await Provider.BulkVSPriorityAsync(appConfig);
                        var firstPointCom = await Provider.FirstPointComPriorityAsync(appConfig);
                        // Verify that all the Executive numbers are still purchasable for the priority area codes.
                        await Provider.VerifyAddToCartAsync(AreaCode.Priority, "Executive", appConfig.Postgresql, appConfig.BulkVSUsername, appConfig.BulkVSPassword,
                            appConfig.PComNetUsername, appConfig.PComNetPassword);
                        await Owned.MatchOwnedNumbersToFusionPBXAsync(appConfig.Postgresql, appConfig.FusionPBXUsername, appConfig.FusionPBXPassword);
                        await Orders.CheckForQuoteConversionsAsync(appConfig.Postgresql, appConfig.InvoiceNinjaToken, appConfig.SmtpUsername, appConfig.SmtpPassword);
                        await Orders.CheckForInvoicePaymentAsync(appConfig.Postgresql, appConfig.InvoiceNinjaToken, appConfig.SmtpUsername, appConfig.SmtpPassword);
                    }

                    if (bulkVSTimer.Elapsed >= bulkVSCycle)
                    {
                        bulkVSTimer.Restart();

                        var bulkVS = await Provider.BulkVSDailyAsync(appConfig);
                    }

                    if (fpcTimer.Elapsed >= fpcCycle)
                    {
                        fpcTimer.Restart();

                        var firstPointCom = await Provider.FirstPointComDailyAsync(appConfig);
                    }

                    if (dailyTimer.Elapsed >= dailyCycle || DateTime.Now == DateTime.Today.AddDays(1).AddSeconds(-1))
                    {
                        dailyTimer.Restart();

                        var smsRouteChanges = await Owned.OwnedDailyAsync(appConfig);
                        var email = await Orders.EmailDailyAsync(smsRouteChanges, appConfig);
                    }

                    Log.Information("[Heartbeat] Priorty Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", priorityTimer.ElapsedMilliseconds, priorityCycle.TotalMilliseconds, (priorityTimer.ElapsedMilliseconds / priorityCycle.TotalMilliseconds));
                    Log.Information("[Heartbeat] BulkVS Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", bulkVSTimer.ElapsedMilliseconds, bulkVSCycle.TotalMilliseconds, (bulkVSTimer.ElapsedMilliseconds / bulkVSCycle.TotalMilliseconds));
                    Log.Information("[Heartbeat] Cycle complete. Daily Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", dailyTimer.ElapsedMilliseconds, dailyCycle.TotalMilliseconds, (dailyTimer.ElapsedMilliseconds / dailyCycle.TotalMilliseconds));

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
                Log.Fatal("[Heartbeat] This is a complete application failure. We've broken out of the infinite loop.");

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
            public string InvoiceNinjaToken { get; set; } = string.Empty;
            public string Data247Username { get; set; } = string.Empty;
            public string Data247Password { get; set; } = string.Empty;
            public string EmailOrders { get; set; } = string.Empty;
            public string EmailDan { get; set; } = string.Empty;
            public string EmailTom { get; set; } = string.Empty;
            public string AzureStorageAccount { get; set; } = string.Empty;
            public string TeleDynamicsUsername { get; set; } = string.Empty;
            public string TeleDynamicsPassword { get; set; } = string.Empty;
            public string CallWithUsAPIKEY { get; set; } = string.Empty;
            public string FusionPBXUsername { get; set; } = string.Empty;
            public string FusionPBXPassword { get; set; } = string.Empty;
        }
    }
}