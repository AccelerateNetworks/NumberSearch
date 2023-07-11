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
                TeleAPI = string.IsNullOrWhiteSpace(config.GetConnectionString("TeleAPI")) ? throw new Exception("TeliAPI config key is blank.") : Guid.Parse(config.GetConnectionString("TeleAPI") ?? string.Empty),
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
            Stopwatch priorityTimer = new();
            Stopwatch dailyTimer = new();
            TimeSpan dailyCycle = TimeSpan.FromDays(1);
            TimeSpan priortyCycle = TimeSpan.FromMinutes(20);

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

                // To infinity and beyond.
                while (true)
                {
                    var start = DateTime.Now;

                    // Priority Ingest
                    if (priorityTimer.Elapsed >= priortyCycle)
                    {
                        priorityTimer.Restart();

                        var bulkVS = await Provider.BulkVSPriorityAsync(appConfig);
                        var firstPointCom = await Provider.FirstPointComPriorityAsync(appConfig);
                        // Verify that all the Executive numbers are still purchasable for the priority area codes.
                        await Provider.VerifyAddToCartAsync(AreaCode.Priority, "Executive", appConfig.Postgresql, appConfig.BulkVSUsername, appConfig.BulkVSPassword,
                            appConfig.PComNetUsername, appConfig.PComNetPassword);
                    }

                    // Daily Ingest
                    if (dailyTimer.Elapsed >= dailyCycle)
                    {
                        dailyTimer.Restart();

                        var bulkVS = await Provider.BulkVSDailyAsync(appConfig);
                        var firstPointCom = await Provider.FirstPointComDailyAsync(appConfig);
                        await Owned.OwnedDailyAsync(appConfig);
                        var email = await Orders.EmailDailyAsync(appConfig);
                    }

                    Log.Information($"[Heartbeat] Cycle complete. Daily Timer ElapsedMilliseconds: {dailyTimer.ElapsedMilliseconds}");

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