using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using static NumberSearch.Ingest.Owned;

namespace NumberSearch.Ingest
{
    public class Program
    {

        public readonly record struct IngestConfiguration(
            ReadOnlyMemory<char> PComNetUsername,
            ReadOnlyMemory<char> PComNetPassword,
            ReadOnlyMemory<char> BulkVSAPIKEY,
            ReadOnlyMemory<char> BulkVSAPISecret,
            ReadOnlyMemory<char> BulkVSUsername,
            ReadOnlyMemory<char> BulkVSPassword,
            ReadOnlyMemory<char> Postgresql,
            ReadOnlyMemory<char> PostgresqlProd,
            ReadOnlyMemory<char> SmtpUsername,
            ReadOnlyMemory<char> SmtpPassword,
            ReadOnlyMemory<char> InvoiceNinjaToken,
            ReadOnlyMemory<char> EmailOrders,
            ReadOnlyMemory<char> EmailDan,
            ReadOnlyMemory<char> EmailTom,
            ReadOnlyMemory<char> FusionPBXUsername,
            ReadOnlyMemory<char> FusionPBXPassword,
            ReadOnlyMemory<char> MessagingUsername,
            ReadOnlyMemory<char> MessagingPassword,
            ReadOnlyMemory<char> MessagingURL
        );

        public static async Task Main()
        {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
            .Build();

            var appConfig = new IngestConfiguration()
            {
                PComNetUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetUsername")) ? throw new Exception("PComNetUsername config key is blank.") : config.GetConnectionString("PComNetUsername").AsMemory(),
                PComNetPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetPassword")) ? throw new Exception("PComNetPassword config key is blank.") : config.GetConnectionString("PComNetPassword").AsMemory(),
                BulkVSAPIKEY = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPIKEY")) ? throw new Exception("BulkVSAPIKEY config key is blank.") : config.GetConnectionString("BulkVSAPIKEY").AsMemory(),
                BulkVSAPISecret = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPISecret")) ? throw new Exception("BulkVSAPISecret config key is blank.") : config.GetConnectionString("BulkVSAPISecret").AsMemory(),
                BulkVSUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSUsername")) ? throw new Exception("BulkVSUsername config key is blank.") : config.GetConnectionString("BulkVSUsername").AsMemory(),
                BulkVSPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSPassword")) ? throw new Exception("BulkVSPassword config key is blank.") : config.GetConnectionString("BulkVSPassword").AsMemory(),
                Postgresql = string.IsNullOrWhiteSpace(config.GetConnectionString("PostgresqlProd")) ? throw new Exception("PostgresqlProd config key is blank.") : config.GetConnectionString("PostgresqlProd").AsMemory(),
                SmtpUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpUsername")) ? throw new Exception("SmtpUsername config key is blank.") : config.GetConnectionString("SmtpUsername").AsMemory(),
                SmtpPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpPassword")) ? throw new Exception("SmtpPassword config key is blank.") : config.GetConnectionString("SmtpPassword").AsMemory(),
                InvoiceNinjaToken = string.IsNullOrWhiteSpace(config.GetConnectionString("InvoiceNinjaToken")) ? throw new Exception("InvoiceNinjaToken config key is blank.") : config.GetConnectionString("InvoiceNinjaToken").AsMemory(),
                EmailOrders = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailOrders")) ? throw new Exception("EmailOrders config key is blank.") : config.GetConnectionString("EmailOrders").AsMemory(),
                EmailDan = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailDan")) ? throw new Exception("EmailDan config key is blank.") : config.GetConnectionString("EmailDan").AsMemory(),
                EmailTom = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailTom")) ? throw new Exception("EmailTom config key is blank.") : config.GetConnectionString("EmailTom").AsMemory(),
                FusionPBXUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("FusionPBXUsername")) ? throw new Exception("FusionPBXUsername config key is blank.") : config.GetConnectionString("FusionPBXUsername").AsMemory(),
                FusionPBXPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("FusionPBXPassword")) ? throw new Exception("FusionPBXPassword config key is blank.") : config.GetConnectionString("FusionPBXPassword").AsMemory(),
                MessagingUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingUsername")) ? throw new Exception("MessagingUsername config key is blank.") : config.GetConnectionString("MessagingUsername").AsMemory(),
                MessagingPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingPassword")) ? throw new Exception("MessagingPassword config key is blank.") : config.GetConnectionString("MessagingPassword").AsMemory(),
                MessagingURL = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingURL")) ? throw new Exception("MessagingURL config key is blank.") : config.GetConnectionString("MessagingURL").AsMemory()
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

            Log.Information("[Heartbeat] Ingest scheduling loop is starting. {ProcessorCount} threads detected.", Environment.ProcessorCount);
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

                        var bulkVS = await Provider.BulkVSPriorityAsync(priorityCycle, appConfig);
                        var firstPointCom = await Provider.FirstPointComPriorityAsync(priorityCycle, appConfig);
                        // Verify that all the Executive numbers are still purchasable for the priority area codes.
                        await Provider.VerifyAddToCartAsync(AreaCode.Priority, "Executive".AsMemory(), appConfig.Postgresql, appConfig.BulkVSUsername, appConfig.BulkVSPassword,
                            appConfig.PComNetUsername, appConfig.PComNetPassword);
                        await Owned.MatchOwnedNumbersToFusionPBXAsync(appConfig.Postgresql, appConfig.FusionPBXUsername, appConfig.FusionPBXPassword);
                        await Orders.CheckForQuoteConversionsAsync(appConfig.Postgresql, appConfig.InvoiceNinjaToken, appConfig.SmtpUsername, appConfig.SmtpPassword);
                        await Orders.CheckForInvoicePaymentAsync(appConfig.Postgresql, appConfig.InvoiceNinjaToken, appConfig.SmtpUsername, appConfig.SmtpPassword);
                    }

                    if (bulkVSTimer.Elapsed >= bulkVSCycle)
                    {
                        bulkVSTimer.Restart();

                        var bulkVS = await Provider.BulkVSCompleteAsync(bulkVSCycle, appConfig);
                    }

                    if (fpcTimer.Elapsed >= fpcCycle)
                    {
                        fpcTimer.Restart();

                        var firstPointCom = await Provider.FirstPointComCompleteAsync(fpcCycle, appConfig);

                        // Offer unassigned phone numbers we own for purchase on the website.
                        _ = await OfferUnassignedNumberForSaleAsync(appConfig.Postgresql);

                    }

                    if (dailyTimer.Elapsed >= dailyCycle || DateTime.Now == DateTime.Today.AddDays(1).AddSeconds(-1))
                    {
                        dailyTimer.Restart();

                        await Owned.OwnedDailyAsync(appConfig);

                        // Verify SMS routing with Endstream.
                        SMSRouteChange[] smsRouteChanges = await VerifySMSRoutingAsync(appConfig.Postgresql, appConfig.PComNetUsername, appConfig.PComNetPassword);

                        var email = await Orders.EmailDailyAsync(smsRouteChanges, appConfig);
                    }

                    Log.Information("[Heartbeat] Priorty Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", priorityTimer.ElapsedMilliseconds, priorityCycle.TotalMilliseconds, (priorityTimer.ElapsedMilliseconds / priorityCycle.TotalMilliseconds));
                    Log.Information("[Heartbeat] BulkVS Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", bulkVSTimer.ElapsedMilliseconds, bulkVSCycle.TotalMilliseconds, (bulkVSTimer.ElapsedMilliseconds / bulkVSCycle.TotalMilliseconds));
                    Log.Information("[Heartbeat] Cycle complete. Daily Timer {Elapsed:000} ms of {Limit:000} ms. ({percentP:P2})", dailyTimer.ElapsedMilliseconds, dailyCycle.TotalMilliseconds, (dailyTimer.ElapsedMilliseconds / dailyCycle.TotalMilliseconds));

                    // Limit this to 1 request every 10 seconds to the database.
                    await Task.Delay(10000);
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
                    PrimaryEmailAddress = appConfig.EmailTom.ToString(),
                    CarbonCopy = appConfig.EmailDan.ToString(),
                    DateSent = DateTime.Now,
                    Subject = $"[Ingest] App is down.",
                    MessageBody = $"Something has gone wrong and the ingest app is down at {DateTime.Now}. Please capture the logs and then restart or redeploy the ingest application to restore service.",
                    OrderId = new Guid(),
                    Completed = true
                };

                var checkSend = await notificationEmail.SendEmailAsync(appConfig.SmtpUsername.ToString(), appConfig.SmtpPassword.ToString());
                var checkSave = await notificationEmail.PostAsync(appConfig.Postgresql.ToString());

                // Save the log.
                await Log.CloseAndFlushAsync();
            }
        }
    }
}