
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess.Models;
using NumberSearch.Ingest;

using ServiceReference;

using Xunit;

using static NumberSearch.Ingest.Program;

namespace NumberSearch.Tests
{
    public class FunctionalIngest
    {
        private readonly ITestOutputHelper output;
        private readonly Credentials pComNetCredentials;
        private readonly string bulkVSKey;
        private readonly string bulkVSSecret;
        private readonly string bulkVSUsername;
        private readonly string bulkVSPassword;
        private readonly string postgresql;
        private readonly string invoiceNinjaToken;
        private readonly IConfiguration configuration;
        private readonly IngestConfiguration ingestConfiguration;

        public FunctionalIngest(ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();

            configuration = config;

            pComNetCredentials = new Credentials
            {
                Username = config.GetConnectionString("PComNetUsername"),
                Password = config.GetConnectionString("PComNetPassword")
            };

            bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
            bulkVSUsername = config.GetConnectionString("BulkVSUsername");
            bulkVSPassword = config.GetConnectionString("BulkVSPassword");
            postgresql = config.GetConnectionString("PostgresqlProd");
            invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");

            var appConfig = new IngestConfiguration
            {
                Postgresql = string.IsNullOrWhiteSpace(config.GetConnectionString("PostgresqlProd")) ? throw new Exception("PostgresqlProd config key is blank.") : config.GetConnectionString("PostgresqlProd").AsMemory(),
                BulkVSAPIKEY = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPIKEY")) ? throw new Exception("BulkVSAPIKEY config key is blank.") : config.GetConnectionString("BulkVSAPIKEY").AsMemory(),
                BulkVSAPISecret = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSAPISecret")) ? throw new Exception("BulkVSAPISecret config key is blank.") : config.GetConnectionString("BulkVSAPISecret").AsMemory(),
                BulkVSUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSUsername")) ? throw new Exception("BulkVSUsername config key is blank.") : config.GetConnectionString("BulkVSUsername").AsMemory(),
                BulkVSPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("BulkVSPassword")) ? throw new Exception("BulkVSPassword config key is blank.") : config.GetConnectionString("BulkVSPassword").AsMemory(),
                PComNetUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetUsername")) ? throw new Exception("PComNetUsername config key is blank.") : config.GetConnectionString("PComNetUsername").AsMemory(),
                PComNetPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("PComNetPassword")) ? throw new Exception("PComNetPassword config key is blank.") : config.GetConnectionString("PComNetPassword").AsMemory(),
                SmtpUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpUsername")) ? throw new Exception("SmtpUsername config key is blank.") : config.GetConnectionString("SmtpUsername").AsMemory(),
                SmtpPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("SmtpPassword")) ? throw new Exception("SmtpPassword config key is blank.") : config.GetConnectionString("SmtpPassword").AsMemory(),
                EmailOrders = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailOrders")) ? throw new Exception("EmailOrders config key is blank.") : config.GetConnectionString("EmailOrders").AsMemory(),
                EmailDan = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailDan")) ? throw new Exception("EmailDan config key is blank.") : config.GetConnectionString("EmailDan").AsMemory(),
                EmailTom = string.IsNullOrWhiteSpace(config.GetConnectionString("EmailTom")) ? throw new Exception("EmailTom config key is blank.") : config.GetConnectionString("EmailTom").AsMemory(), InvoiceNinjaToken = string.IsNullOrWhiteSpace(config.GetConnectionString("InvoiceNinjaToken")) ? throw new Exception("InvoiceNinjaToken config key is blank.") : config.GetConnectionString("InvoiceNinjaToken").AsMemory(),
                MessagingUsername = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingUsername")) ? throw new Exception("MessagingUsername config key is blank.") : config.GetConnectionString("MessagingUsername").AsMemory(),
                MessagingPassword = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingPassword")) ? throw new Exception("MessagingPassword config key is blank.") : config.GetConnectionString("MessagingPassword").AsMemory(),
                MessagingURL = string.IsNullOrWhiteSpace(config.GetConnectionString("MessagingURL")) ? throw new Exception("MessagingURL config key is blank.") : config.GetConnectionString("MessagingURL").AsMemory()
            };
            ingestConfiguration = appConfig;
        }

        //[Fact]
        //public async Task TestBulkVSPortStatusUpdates()
        //{
        //    await Provider.BulkVSPriorityAsync(TimeSpan.FromMinutes(10), ingestConfiguration);
        //}

        //[Fact]
        //public async Task TestFirstPointComIngest()
        //{
        //    TimeSpan cycle = TimeSpan.FromMinutes(10);
        //    await Provider.FirstPointComPriorityAsync(cycle, ingestConfiguration);

        //    output.WriteLine("Done");
        //}

        //[Fact]
        //public async Task TestCheckForQuoteConversionsAsync()
        //{
        //    await Orders.CheckForQuoteConversionsAsync(ingestConfiguration.Postgresql, ingestConfiguration.InvoiceNinjaToken, ingestConfiguration.SmtpUsername, ingestConfiguration.SmtpPassword);
        //}

        //[Fact]
        //public async Task TestOwnedNumbersIngestAsync()
        //{
        //    await Owned.IngestAsync(ingestConfiguration);

        //    output.WriteLine("Done");
        //}

        //[Fact]
        //public async Task VerifySPIDDailyAsync()
        //{
        //    var results = await Owned.VerifyServiceProvidersAsync(ingestConfiguration.BulkVSAPIKEY, ingestConfiguration.Postgresql);
        //}

        //[Fact]
        //public async Task UnassignedAsync()
        //{
        //    _ = await Owned.OfferUnassignedNumberForSaleAsync(ingestConfiguration.Postgresql);
        //}  

        //[Fact]
        //public async Task DailyBreifingEmailToDan()
        //{
        //    var emptyChanges = Array.Empty<Owned.SMSRouteChange>();
        //    var checkRun = await Orders.DailyBriefingEmailAsync(emptyChanges, ingestConfiguration);
        //}

        //[Fact]
        //public async Task OwnedNumers()
        //{
        //    await Owned.OwnedDailyAsync(ingestConfiguration);
        //}

        //[Fact]
        //public async Task TestFusionPBXDataUpdateAsync()
        //{
        //    await Owned.MatchOwnedNumbersToFusionPBXAsync(ingestConfiguration.Postgresql, ingestConfiguration.FusionPBXUsername, ingestConfiguration.FusionPBXPassword);
        //}

        //[Fact]
        //public async Task TestSMSRoutingAsync()
        //{
        //    await Owned.VerifySMSRoutingAsync(ingestConfiguration.Postgresql, ingestConfiguration.PComNetUsername, ingestConfiguration.PComNetPassword);
        //}

        //[Fact]
        //public async Task TestE911RegistrationIngestAsync()
        //{
        //    await Owned.VerifyEmergencyInformationAsync(ingestConfiguration.Postgresql, ingestConfiguration.BulkVSUsername, ingestConfiguration.BulkVSPassword);
        //}

        //[Fact]
        //public async Task TestIncompleteOrderRemindersAsync()
        //{
        //    var results = await Orders.IncompleteOrderRemindersAsync(postgresql);
        //    output.WriteLine($"Active Orders: {results.Count()}");
        //}

        [Fact]
        public void CategorizeNumbers()
        {
            // Arrange
            List<PhoneNumber> numbers = [
                new() { DialedNumber = "6666666666" },
                new() { DialedNumber = "2666666666" },
                new() { DialedNumber = "2166666666" },
                new() { DialedNumber = "3216666666" },
                new() { DialedNumber = "4321666666" },
                new() { DialedNumber = "5432166666" },
                new() { DialedNumber = "7543216666" },
                new() { DialedNumber = "8754321666" },
                new() { DialedNumber = "9875432166" },
            ];

            ReadOnlySpan<PhoneNumber> set = [.. numbers];
            // Act
            var results = Services.AssignNumberTypes(ref set);

            // Assert
            Assert.NotEmpty(results.ToArray());
            foreach (var result in results.ToArray())
            {
                Assert.False(string.IsNullOrWhiteSpace(result.NumberType));
                output.WriteLine(result.DialedNumber);
                output.WriteLine(result.NumberType);
            }
        }

        //This takes 3 minutes to run.

        //[Fact]
        // public async Task OwnedNumbersAsync()
        // {
        //     //Arrange

        //     //Act
        //     await Owned.IngestAsync(ingestConfiguration).ConfigureAwait(false);

        //     //Assert
        // }

        //[Fact]
        //public async Task UpdateBulkVSPortRequestStatusesAsync()
        //{
        //    // Update the statuses of all the active port requests with BulkVS.
        //    await PortRequests.UpdateStatusesBulkVSAsync(configuration);
        //}

 
    }
}