
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Call48;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.LCGuide;
using NumberSearch.DataAccess.Models;
using NumberSearch.DataAccess.TeleDynamics;
using NumberSearch.DataAccess.TeleMesssage;

using ServiceReference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace NumberSearch.Tests
{
    public class Integration
    {
        private readonly Guid token;
        private readonly ITestOutputHelper output;
        private readonly Credentials pComNetCredentials;
        private readonly string bulkVSKey;
        private readonly string bulkVSSecret;
        private readonly string bulkVSUsername;
        private readonly string bulkVSPassword;
        private readonly string postgresql;
        private readonly string peerlessAPIKey;
        private readonly string invoiceNinjaToken;
        private readonly string _data247username;
        private readonly string _data247password;
        private readonly string _teleDynamicsUsername;
        private readonly string _teleDynamicsPassword;
        private readonly string _call48Username;
        private readonly string _call48Password;
        private readonly IConfiguration configuration;

        public Integration(ITestOutputHelper output)
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
            token = Guid.Parse(config.GetConnectionString("TeleAPI"));
            postgresql = config.GetConnectionString("PostgresqlProd");
            peerlessAPIKey = config.GetConnectionString("PeerlessAPIKey");
            invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
            _teleDynamicsUsername = config.GetConnectionString("TeleDynamicsUsername");
            _teleDynamicsPassword = config.GetConnectionString("TeleDynamicsPassword");
            _call48Username = config.GetConnectionString("Call48Username");
            _call48Password = config.GetConnectionString("Call48Password");
        }

        [Fact]
        public async Task GetAllBillingClientsAsync()
        {
            // Act
            var result = await Client.GetAllClientsAsync(invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetAllBillingInvoicesAsync()
        {
            // Act
            var result = await Invoice.GetAllAsync(invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetBillingInvoiceByIdAsync()
        {
            // Act
            var result = await Invoice.GetByIdAsync(1661, invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.Equal(1661, result.id);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetBillingTaxRatesAsync()
        {
            // Act
            var result = await TaxRate.GetAllAsync(invoiceNinjaToken).ConfigureAwait(false);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        // There no way to clean up these new entries after we create them, so we don't want to do it all the time.
        //[Fact]
        //public async Task CreateBillingTaxRateAsync()
        //{
        //    // Arrange
        //    var taxRate = new TaxRateDatum
        //    {
        //        name = "IntegrationTest",
        //        rate = 10.1M
        //    };

        //    // Act
        //    var result = await taxRate.PostAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(result);
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        //}

        [Fact]
        public async Task GetAllBillingClientsByEmailAsync()
        {
            // Act
            var result = await Client.GetByEmailAsync("mary@dcigi.com", invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetAllBillingClientsByIdAsync()
        {
            // Act
            var result = await Client.GetByIdWithInoviceLinksAsync(15, invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.invoices.LastOrDefault().invitations.FirstOrDefault().link));
            output.WriteLine(result.invoices.LastOrDefault().invitations.FirstOrDefault().link);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        //[Fact]
        //public async Task CreateAndSendAnInvoiceAsync()
        //{
        //    // Act
        //    var clients = await Client.GetByEmailAsync("thomas.ryan@outlook.com", invoiceNinjaToken);
        //    var client = clients.data.FirstOrDefault();

        //    // Assert        
        //    Assert.NotNull(client);
        //    Assert.NotEmpty(clients.data);
        //    output.WriteLine(JsonSerializer.Serialize(client));

        //    var testInvoice = new Invoice_Items[] {
        //        new Invoice_Items {
        //            product_key = "IntegrationTest",
        //            notes = "IntegrationTest",
        //            cost = 10,
        //            qty = 1
        //        }
        //    };

        //    var testCreate = new InvoiceDatum
        //    {
        //        id = client.id,
        //        invoice_items = testInvoice
        //    };

        //    // Act
        //    var result = await testCreate.PostAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.Equal(result.invoice_items.FirstOrDefault().notes, testCreate.invoice_items.FirstOrDefault().notes);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    var checkSend = await result.SendInvoiceAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.True(checkSend);
        //}

        [Fact]
        public async Task GetBillingClientByIdAsync()
        {
            // Act
            var result = await Client.GetByIdAsync(96, invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.Equal(96, result.id);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        // Diabled to prevent the deployment pipeline from churning the billing system on every commit.
        //[Fact]
        //public async Task CreateBillingClientByIdAsync()
        //{
        //    // Arrange
        //    var testCreate = new ClientDatum
        //    {
        //        name = "IntegrationTest",
        //        contacts = new ClientContact[] {
        //            new ClientContact {
        //                email = "integrationTest@example.com"
        //            }
        //        }
        //    };

        //    // Act
        //    var result = await testCreate.PostAsync(invoiceNinjaToken);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.Equal(testCreate.name, result.name);
        //    Assert.Equal(testCreate.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    var checkDelete = await result.DeleteAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(checkDelete);
        //    Assert.True(checkDelete.is_deleted);
        //}

        //[Fact]
        //public async Task CreateUpdateAndDeleteBillingInvoiceByClientByIdAsync()
        //{

        //    // Arrange
        //    var testCreateClient = new ClientDatum
        //    {
        //        name = "IntegrationTest",
        //        contacts = new ClientContact[] {
        //            new ClientContact {
        //                email = "integrationTest@example.com"
        //            }
        //        }
        //    };

        //    // Act
        //    var testClient = await testCreateClient.PostAsync(invoiceNinjaToken);

        //    // Assert        
        //    Assert.NotNull(testClient);
        //    Assert.Equal(testCreateClient.name, testClient.name);
        //    Assert.Equal(testCreateClient.contacts.FirstOrDefault().email, testClient.contacts.FirstOrDefault().email);
        //    output.WriteLine(JsonSerializer.Serialize(testClient));


        //    var testInvoice = new Invoice_Items[] {
        //        new Invoice_Items {
        //            product_key = "IntegrationTest",
        //            notes = "IntegrationTest",
        //            cost = 10,
        //            qty = 1
        //        }
        //    };

        //    var testCreate = new InvoiceDatum
        //    {
        //        id = testClient.id,
        //        invoice_items = testInvoice
        //    };

        //    // Act
        //    var result = await testCreate.PostAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.Equal(result.invoice_items.FirstOrDefault().notes, testCreate.invoice_items.FirstOrDefault().notes);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    result.invoice_items.FirstOrDefault().notes = "Updated";

        //    var updateTest = await result.PutAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(updateTest);
        //    Assert.Equal(result.invoice_items.FirstOrDefault().notes, updateTest.invoice_items.FirstOrDefault().notes);
        //    output.WriteLine(JsonSerializer.Serialize(updateTest));

        //    var deleteTest = await updateTest.DeleteAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(deleteTest);
        //    Assert.True(deleteTest.is_deleted);

        //    var checkDelete = await testClient.DeleteAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(checkDelete);
        //    Assert.True(checkDelete.is_deleted);
        //}

        //[Fact]
        //public async Task CreateAndUpdateBillingClientByIdAsync()
        //{
        //    // Arrange
        //    var testCreate = new ClientDatum
        //    {
        //        name = "IntegrationTest",
        //        contacts = new ClientContact[] {
        //            new ClientContact {
        //                email = "integrationTest@example.com"
        //            }
        //        }
        //    };

        //    // Act
        //    var result = await testCreate.PostAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.Equal(testCreate.name, result.name);
        //    Assert.Equal(testCreate.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    result.contacts.FirstOrDefault().first_name = "IntegrationTest";

        //    var updateResult = await result.PutAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(result);
        //    Assert.Equal(updateResult.name, result.name);
        //    Assert.Equal(updateResult.id, result.id);
        //    Assert.Equal(updateResult.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    var checkDelete = await updateResult.DeleteAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(checkDelete);
        //    Assert.True(checkDelete.is_deleted);
        //}

        [Fact]
        public async Task GetBillingClientByClientIdAsync()
        {
            // Act
            var result = await Client.GetByClientIdAsync("tf5ohehkafg4xcdrmufsqob4jd6lwymt", invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task LRNLookupTestAsync()
        {
            // Arrange
            string phoneNumber = "2065579450";

            // Act
            var result = await LrnLookup.GetAsync(phoneNumber, token);

            // Assert        
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.status));
            Assert.True(result.code == 200);
            Assert.False(string.IsNullOrWhiteSpace(result.data.lrn));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task LookupPageWithBadNumbersAsync()
        {
            // Arrange
            string phoneNumber1 = "2065552121";
            string phoneNumber2 = "5253747761";
            string phoneNumber3 = "8886409088";

            // Act
            var lookup = new NumberSearch.Mvc.Controllers.LookupController(configuration);
            var result = await lookup.VerifyPortablityAsync(phoneNumber1);

            // Assert        
            Assert.NotNull(result);
            Assert.False(result.Portable);
            output.WriteLine(JsonSerializer.Serialize(result));

            result = await lookup.VerifyPortablityAsync(phoneNumber2);

            // Assert        
            Assert.NotNull(result);
            Assert.False(result.Portable);
            output.WriteLine(JsonSerializer.Serialize(result));

            result = await lookup.VerifyPortablityAsync(phoneNumber3);

            // Assert        
            Assert.NotNull(result);
            Assert.False(result.Portable);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        // We are no longer using data 24/7 for cnam lookups.
        //[Fact]
        //public async Task LIDBLookupAsync()
        //{
        //    // Arrange
        //    string phoneNumber = "14257808879";

        //    // Act
        //    var result = await LIDBLookup.GetAsync(phoneNumber, _data247username, _data247password).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(result);
        //    output.WriteLine(JsonSerializer.Serialize(result));
        //}

        [Fact]
        public async Task RateCenterLookupAsync()
        {
            // Arrange
            string npa = "425";
            string nxx = "780";

            // Act
            var result = await RateCenterLookup.GetAsync(npa, nxx).ConfigureAwait(false);

            // Assert        
            Assert.NotNull(result);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BadRateCenterLookupAsync()
        {
            // Arrange
            string npa = "844";
            string nxx = "646";

            // Act
            var result = await RateCenterLookup.GetAsync(npa, nxx).ConfigureAwait(false);

            // Assert        
            Assert.Null(result);
        }

        [Fact]
        public async Task TeleNPAsTestAsync()
        {
            // Arrange

            // Act
            var results = await DidsNpas.GetAsync(token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            Assert.True(results.code == 200);
            foreach (var result in results.data)
            {
                Assert.False(string.IsNullOrWhiteSpace(result));
            }
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeleGetAllOwnedNumbersAsync()
        {
            // Arrange

            // Act
            var results = await UserDidsList.GetAllAsync(token).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            Assert.True(results.code == 200);
            foreach (var result in results.data)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.number));
            }
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeleGetAllTollfreeAsync()
        {
            // Arrange

            // Act
            var results = await DidsList.GetAllTollfreeAsync(token).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            foreach (var result in results)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
            }
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeliEnableCNAMAsync()
        {
            // Arrange

            // Act
            var results = await UserDidsCnamEnable.GetAsync("9292233014", token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            Assert.True(results.code == 200);
            Assert.True(results.CnamEnabled());
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeliLookupEmergencyInfoAsync()
        {
            // Arrange

            // Act
            var results = await EmergencyInfo.GetAsync("2062011205", token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            output.WriteLine(JsonSerializer.Serialize(results));

            results = await EmergencyInfo.GetAsync("9365820436", token);
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeliPortRequestStatusAsync()
        {
            // Arrange
            var portRequests = new string[] { "3939", "5030", "4951", "4936" };

            foreach (var request in portRequests)
            {
                // Act
                var results = await LnpGet.GetAsync(request, token).ConfigureAwait(false);

                // Assert
                Assert.NotNull(results);
                Assert.False(string.IsNullOrWhiteSpace(results.status));
                Assert.True(results.code == 200);
                output.WriteLine(JsonSerializer.Serialize(results));
            }
        }


        // Teli doesn't have a way to delete these so only want to test it when required.
        //[Fact]
        //public async Task TeliPortRequestAsync()
        //{
        //    // Arrange
        //    var orderId = Guid.Parse("4f6dad94-2442-42fa-a3eb-cf2aa9fe2324");
        //    var fromDb = await PortRequest.GetByOrderIdAsync(orderId, postgresql).ConfigureAwait(false);
        //    var numbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId, postgresql).ConfigureAwait(false);

        //    // Act
        //    var results = await LnpCreate.GetAsync(fromDb, numbers, token).ConfigureAwait(false);

        //    // Assert
        //    Assert.NotNull(results);
        //    Assert.False(string.IsNullOrWhiteSpace(results.status));
        //    Assert.True(results.code == 200);
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
        //}

        //[Fact]
        //public async Task PeerlessNPATestAsync()
        //{
        //    // Arrange
        //    string npa = "206";

        //    // Act
        //    var results = await DidFind.GetRawAsync(npa, peerlessAPIKey).ConfigureAwait(false);

        //    // Assert
        //    Assert.NotNull(results);
        //    Assert.NotEmpty(results);
        //    foreach (var result in results)
        //    {
        //        Assert.False(string.IsNullOrWhiteSpace(result.did));
        //        Assert.False(string.IsNullOrWhiteSpace(result.category));
        //    }
        //}

        //[Fact]
        //public async Task PeerlessGetPhoneNumbersTestAsync()
        //{
        //    // Arrange
        //    string npa = "206";

        //    // Act
        //    var results = await DidFind.GetAsync(npa, peerlessAPIKey).ConfigureAwait(false);

        //    // Assert
        //    Assert.NotNull(results);
        //    Assert.NotEmpty(results);
        //    int count = 0;

        //    foreach (var result in results.ToArray())
        //    {
        //        output.WriteLine(result.DialedNumber);
        //        Assert.True(result.NPA > 99);
        //        Assert.True(result.NXX > 99);
        //        Assert.True(result.XXXX > 1);
        //        Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
        //        Assert.False(string.IsNullOrWhiteSpace(result.City));
        //        Assert.False(string.IsNullOrWhiteSpace(result.State));
        //        Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
        //        count++;
        //    }
        //    output.WriteLine($"{count} Results Reviewed");
        //}

        //[Fact]
        //public async Task TeleNXXsTestAsync()
        //{
        //    // Arrange
        //    string npa = "206";

        //    // Act
        //    var results = await DidsNxxs.GetAsync(npa, token);

        //    // Assert
        //    Assert.NotNull(results);
        //    Assert.False(string.IsNullOrWhiteSpace(results.status));
        //    Assert.True(results.code == 200);
        //    foreach (var result in results.data)
        //    {
        //        Assert.False(string.IsNullOrWhiteSpace(result));
        //    }
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
        //}

        //[Fact]
        //public async Task LocalNumberTestAsync()
        //{
        //    // Arrange
        //    string query = "20645";

        //    // Act
        //    var results = await DidsList.GetRawAsync(query, token);

        //    // Assert
        //    Assert.NotNull(results);
        //    Assert.False(string.IsNullOrWhiteSpace(results.status));
        //    Assert.True(results.code == 200);
        //    foreach (var result in results.data.dids)
        //    {
        //        Assert.False(string.IsNullOrWhiteSpace(result.number));
        //    }
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
        //}

        [Fact]
        public async Task LocalNumberPortabilityRawTestAsync()
        {
            // Arrange
            string query = "1";

            // Act
            var results = await LnpCheck.GetRawAsync(query, token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results));
            output.WriteLine(results);

            results = await LnpCheck.GetRawAsync("8662214141", token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results));
            output.WriteLine(results);
        }

        [Fact]
        public async Task LocalNumberPortabilityTestAsync()
        {
            // Arrange
            string query = "1";

            // Act
            var result = await LnpCheck.IsPortable(query, token);

            // Assert
            Assert.False(result);

            result = await LnpCheck.IsPortable("8662214141", token);

            // Assert
            Assert.True(result);
        }


        //[Fact]
        //public async Task PComNetDIDInventorySearchAsyncTestAsync()
        //{
        //    var DIDSearch = new DIDOrderQuery
        //    {
        //        DID = "12062670472",
        //        NPA = "206",
        //        NXX = "267",
        //        RateCenter = "SEATTLE"
        //    };
        //    var ReturnAmount = 100;

        //    var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

        //    var result = await client.DIDInventorySearchAsync(pComNetCredentials, DIDSearch, ReturnAmount);

        //    Assert.NotNull(result);
        //    Assert.NotEmpty(result.DIDOrder);

        //    foreach (var x in result.DIDOrder)
        //    {
        //        output.WriteLine(x.DID);
        //    }
        //}

        //[Fact]
        //public async Task FirstComGetPhoneNumbersTestAsync()
        //{
        //    var results = await NpaNxxFirstPointCom.GetAsync("206", string.Empty, string.Empty, pComNetCredentials.Username, pComNetCredentials.Password);

        //    Assert.NotNull(results);
        //    Assert.NotEmpty(results);
        //    int count = 0;

        //    foreach (var result in results.ToArray())
        //    {
        //        output.WriteLine(result.DialedNumber);
        //        Assert.True(result.NPA > 99);
        //        Assert.True(result.NXX > 99);
        //        Assert.True(result.XXXX > 1);
        //        Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
        //        Assert.False(string.IsNullOrWhiteSpace(result.City));
        //        Assert.False(string.IsNullOrWhiteSpace(result.State));
        //        Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
        //        count++;
        //    }
        //    output.WriteLine($"{count} Results Reviewed");
        //}

        [Fact]
        public async Task BulkVSRESTNpaNxxGetAsyncTestAsync()
        {
            // Arrange
            var npa = 206;

            // Act
            var results = await OrderTn.GetAsync(npa, bulkVSUsername, bulkVSPassword);

            // Assert
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results.ToArray())
            {
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
            output.WriteLine(JsonSerializer.Serialize(results.FirstOrDefault()));
        }

        [Fact]
        public async Task BulkVSRESTOrderPostAsyncTestAsync()
        {
            // Arrange
            var order = new OrderTnRequestBody
            {
                TN = "13109060901",
                Lidb = "Accelerate Networks",
                PortoutPin = "3591344",
                TrunkGroup = "Primary",
                Sms = true,
                Mms = false
            };

            // Act
            var results = await order.PostAsync(bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }


        [Fact]
        public async Task BulkVSCnameLookupAsync()
        {
            // Arrange
            var number = "2064083008";

            // Act
            var result = await CnamBulkVs.GetAsync(number, bulkVSKey).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrWhiteSpace(result.name));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSLrnLookupAsync()
        {
            // Arrange
            var number = "2064083008";

            // Act
            var result = await LrnBulkCnam.GetAsync(number, bulkVSKey).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrWhiteSpace(result.spid));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSRESTGetAllOwnedNumbersAsync()
        {
            // Arrange

            // Act
            var results = await TnRecord.GetAsync(bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task BulkVSRESTValidatePortabilityAsync()
        {
            // Arrange
            var portedNumber = "8605530426";
            // Act
            var results = await ValidatePortability.GetAsync(portedNumber, bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeleDynamicsProductCheckQuantityAsync()
        {
            // Arrange
            var partNumber = "yea-sip-t54w";
            // Act
            var results = await VendorProduct.GetAsync(partNumber, _teleDynamicsUsername, _teleDynamicsPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task BulkVSRESTGetAllOwnedNumbersAsOwnedAsync()
        {
            // Arrange

            // Act
            var results = await TnRecord.GetOwnedAsync(bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task BulkVSRESTGetAllPortRequestsAsync()
        {
            // Arrange

            // Act
            var results = await PortTn.GetAllAsync(bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        // Disabled so as not to mess up this specific order in the vendor's system.
        //[Fact]
        //public async Task BulkVSRESTAddANoteAsync()
        //{
        //    // Arrange
        //    var note = new PortTNNote
        //    {
        //        Note = "This is a test note submitted via the API."
        //    };

        //    // Act
        //    await note.PostAsync("1638158", bulkVSUsername, bulkVSPassword);
        //}

        [Fact]
        public async Task BulkVSRESTGetPortRequestsAsync()
        {
            // Arrange

            // Act
            var results = await PortTn.GetAllAsync(bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var result = await PortTn.GetAsync(results.FirstOrDefault().OrderId, bulkVSUsername, bulkVSPassword).ConfigureAwait(false);

            Assert.NotNull(result);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task Call48LoginTestAsync()
        {
            // Act
            var result = await Login.LoginAsync(_call48Username, _call48Password);

            Assert.NotNull(result);
            Assert.NotNull(result.data.token);
            Assert.True(result.code == 200);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task Call48LocalNumberLookupTestAsync()
        {
            // Act
            var result = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);

            var results = await Search.GetLocalNumbersAsync("WA", string.Empty, "425", string.Empty, result.data.token).ConfigureAwait(false);

            Assert.NotEmpty(results.data.result);
            output.WriteLine(results.data.result.Length.ToString());
            foreach (var number in results.data.result)
            {
                Assert.False(string.IsNullOrWhiteSpace(number.did));
                Assert.False(string.IsNullOrWhiteSpace(number.number));
                Assert.False(string.IsNullOrWhiteSpace(number.npa));
                Assert.False(string.IsNullOrWhiteSpace(number.nxx));
                Assert.False(string.IsNullOrWhiteSpace(number.xxxx));
                Assert.True(number.type == "local");
                Assert.True(number.state == "WA");
                Assert.False(string.IsNullOrWhiteSpace(number.ratecenter));
                Assert.False(string.IsNullOrWhiteSpace(number.locData));
                output.WriteLine(JsonSerializer.Serialize(number));
            }

        }

        [Fact]
        public async Task Call48GetNumbersTestAsync()
        {
            // Act
            var cred = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);

            var results = await Search.GetAsync("WA", 425, cred.data.token).ConfigureAwait(false);

            Assert.NotEmpty(results);
            output.WriteLine(results.Count().ToString());
            foreach (var result in results)
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                // XXXX can be 0001 which as an int is 1.
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                // Reenabled these after June 2020 starts.
                //Assert.False(string.IsNullOrWhiteSpace(result.City));
                //Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                output.WriteLine(JsonSerializer.Serialize(result));
            }
        }

        [Fact]
        public async Task Call48GetRatecentersTestAsync()
        {
            // Act
            var cred = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);

            var results = await Ratecenter.GetAllRatecentersAsync(AreaCode.States, cred.data.token).ConfigureAwait(false);

            Assert.NotEmpty(results);
            output.WriteLine(results.Count().ToString());
            foreach (var result in results)
            {
                Assert.NotEmpty(result.Ratecenters);
                output.WriteLine(JsonSerializer.Serialize(result));
            }

        }

        // Disabled so as not to purchase a bunch of numbers by accident.
        //[Fact]
        //public async Task Call48PurchaseLocalNumberTestAsync()
        //{
        //    // Act
        //    var cred = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);

        //    var results = await Search.GetAsync("WA", 206, cred.data.token).ConfigureAwait(false);

        //    Assert.NotEmpty(results);
        //    output.WriteLine(results.Count().ToString());
        //    var number = results.FirstOrDefault();

        //    var checkExist = await Search.GetLocalNumbersAsync(string.Empty, number.State, number.NPA.ToString(), number.NXX.ToString(), cred.data.token).ConfigureAwait(false);

        //    var numberToPurchase = checkExist.data.result.Where(x => x.did.Replace("-", string.Empty) == number.DialedNumber).FirstOrDefault();
        //    output.WriteLine(JsonSerializer.Serialize(numberToPurchase));

        //    var purchaseOrder = await Purchase.PurchasePhoneNumberAsync(checkExist.data.loc, numberToPurchase, cred.data.token).ConfigureAwait(false);
        //    output.WriteLine(JsonSerializer.Serialize(purchaseOrder));

        //    Assert.NotNull(purchaseOrder);
        //    Assert.True(purchaseOrder.code == 200);
        //}

        [Fact]
        public async Task TeleNumberDetailsTestAsync()
        {
            // Arrange
            var number = "2068588757";

            // Act
            var results = await UserDidsGet.GetAsync(number, token);

            Assert.NotNull(results);
            Assert.True(results.code == 200);
            output.WriteLine(results.code.ToString());
        }

        [Fact]
        public async Task TeleNoteTestAsync()
        {
            // Arrange
            var number = "2068588757";

            // Act
            var results = await UserDidsGet.GetAsync(number, token).ConfigureAwait(false);

            Assert.NotNull(results);
            output.WriteLine(results.data.id);

            var note = $"This is a test note.";

            var setNote = await UserDidsNote.SetNote(note, results.data.id, token).ConfigureAwait(false);

            Assert.NotNull(setNote);
            Assert.True(setNote.code == 200);
            output.WriteLine(setNote.data);
        }


        [Fact]
        public async Task GetPhoneNumbersAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.GetAllAsync(conn);
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results)
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                // XXXX can be 0001 which as an int is 1.
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                // Reenabled these after June 2020 starts.
                //Assert.False(string.IsNullOrWhiteSpace(result.City));
                //Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
        }

        [Fact]
        public async Task GetAllPhoneNumbersAsStringsAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.GetAllNumbersAsync(conn);
            Assert.NotNull(results);
            output.WriteLine($"{results.Count()} Results Reviewed");
        }

        [Fact]
        public async Task GetPhoneNumbersByQueryAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.SearchAsync("*", conn);
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results)
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                // XXXX can be 0001 which as an int is 1.
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                //Assert.False(string.IsNullOrWhiteSpace(result.City));
                //Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
        }

        [Fact]
        public async Task GetPhoneNumbersByQueryPaginatedAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.SequentialPaginatedSearchAsync("*", 1, conn);
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results)
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                // XXXX can be 0001 which as an int is 1.
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
            Assert.True(count > 0);
        }

        [Fact]
        public async Task GetPhoneNumbersByLocationPaginatedAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.LocationPaginatedSearchAsync("*", 1, conn);
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results)
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                // XXXX can be 0001 which as an int is 1.
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
            Assert.True(count > 0);
        }

        [Fact]
        // This was deleting the database everytime it ran.
        // Now it only delete stale numbers that haven't been updated in the last 3 days.
        public async Task DeleteOldPhoneNumberAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.DeleteOld(DateTime.Now.AddDays(-3), conn).ConfigureAwait(false);
            Assert.NotNull(results);
            output.WriteLine($"{results.Removed} Numbers Removed.");
        }

        [Fact]
        public async Task DeleteOldPhoneNumbersByProviderAsync()
        {
            var conn = postgresql;
            var cycle = DateTime.Now.AddHours(1) - DateTime.Now;
            var provider = "Test";
            var results = await PhoneNumber.DeleteOldByProvider(DateTime.Now, cycle, provider, conn).ConfigureAwait(false);
            Assert.NotNull(results);
            output.WriteLine($"{results.Removed} Numbers Removed.");
        }

        [Fact]
        public async Task PostPhoneNumberAsync()
        {
            var conn = postgresql;
            var number = new PhoneNumber
            {
                DialedNumber = "1111111111",
                NPA = 111,
                NXX = 111,
                XXXX = 1111,
                City = "Test",
                DateIngested = DateTime.Now,
                IngestedFrom = "IntegrationTest",
                NumberType = "Standard",
                Purchased = false,
                State = "WA"
            };

            var existing = await PhoneNumber.GetAsync(number.DialedNumber, postgresql).ConfigureAwait(false);

            if (existing is not null && existing?.DialedNumber?.Length == 10)
            {
                var checkDeleteNumber = existing.DeleteAsync(postgresql).ConfigureAwait(false);
            }

            var response = await number.PostAsync(conn);
            Assert.True(response);

            // Clean up.
            // We need the Guid so we have to get a copy of the new record from the DB before we can delete it.
            var fromDb = await PhoneNumber.GetAsync(number.DialedNumber, conn).ConfigureAwait(false);

            var checkDelete = await fromDb.DeleteAsync(conn);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PhoneNumberGetSingleTestAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.GetAllAsync(conn);
            var number = results.OrderBy(x => x.DialedNumber).LastOrDefault();
            var result = await PhoneNumber.GetAsync(number.DialedNumber, conn);
            Assert.True(number.DialedNumber == result.DialedNumber);
            Assert.True(result.NPA > 99);
            Assert.True(result.NXX > 99);
            Assert.True(result.XXXX > 1);
            Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
            Assert.False(string.IsNullOrWhiteSpace(result.City));
            Assert.False(string.IsNullOrWhiteSpace(result.State));
            Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
        }

        [Fact]
        public async Task PostIngestCyclesAsync()
        {
            var cycle = new IngestCycle
            {
                CycleTime = DateTime.Now.AddHours(12) - DateTime.Now,
                IngestedFrom = "IntegrationTest",
                LastUpdate = DateTime.Now,
                Enabled = true,
                RunNow = true
            };

            var result = await cycle.PostAsync(postgresql).ConfigureAwait(false);
            Assert.True(result);

            var results = await IngestCycle.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var test = results.Where(x => x.IngestedFrom == cycle.IngestedFrom).FirstOrDefault();
            Assert.NotNull(test);

            test.CycleTime = DateTime.Now.AddHours(1) - DateTime.Now;
            var checkUpdate = await test.PutAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkUpdate);

            results = await IngestCycle.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var update = results.Where(x => x.IngestedFrom == cycle.IngestedFrom).FirstOrDefault();
            Assert.NotNull(update);
            //Assert.Equal(test.CycleTime, update.CycleTime);

            var checkDelete = await update.DeleteAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PostEndOfRunStatsAsync()
        {
            var conn = postgresql;
            var stats = new IngestStatistics
            {
                NumbersRetrived = 1,
                FailedToIngest = 1,
                IngestedNew = 1,
                Unchanged = 1,
                UpdatedExisting = 1,
                Removed = 1,
                IngestedFrom = "Test",
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now
            };

            var check = await stats.PostAsync(conn);

            Assert.True(check);

            // Clean up.
            var fromDb = await IngestStatistics.GetLastIngestAsync(stats.IngestedFrom, conn).ConfigureAwait(false);

            var checkDelete = await fromDb.DeleteAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetNumberOfResultsInQueryAsync()
        {
            var query = "*";
            var conn = postgresql;

            var result = await PhoneNumber.NumberOfResultsInQuery(query, conn);

            Assert.True(result > 0);
        }

        [Fact]
        public async Task GetCountPhoneNumbersAsync()
        {
            var result = await PhoneNumber.GetCountByProvider("BulkVS", postgresql);

            Assert.True(result > 0);
            output.WriteLine(result.ToString());
        }

        [Fact]
        public async Task GetVerifiedNumbersAsync()
        {
            var results = await VerifiedPhoneNumber.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            results = await VerifiedPhoneNumber.GetByOrderIdAsync(results.FirstOrDefault().OrderId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task PostPutDeleteVerifiedNumbersAsync()
        {
            var testNumber = new VerifiedPhoneNumber
            {
                VerifiedPhoneNumberId = Guid.NewGuid(),
                VerifiedDialedNumber = "2068588757",
                NPA = 206,
                NXX = 858,
                XXXX = 8757,
                ServiceProfileIdentifier = "test",
                Activation = "test",
                City = "test",
                DateIngested = DateTime.Now,
                DateToExpire = DateTime.Now,
                LocalAccessTransportArea = "test",
                IngestedFrom = "test",
                Jurisdiction = "test",
                LastPorted = DateTime.Now,
                LIDBName = "test",
                Local = "test",
                LocalExchangeCarrier = "test",
                LocalExchangeCarrierType = "test",
                LocalRoutingNumber = "test",
                NumberType = "test",
                OperatingCompanyNumber = "test",
                OrderId = Guid.NewGuid(),
                Province = "test",
                RateCenter = "test",
                Wireless = true
            };

            var checkPost = await testNumber.PostAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkPost);

            var checkSave = await VerifiedPhoneNumber.GetByOrderIdAsync(testNumber.OrderId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            Assert.True(checkSave.FirstOrDefault().VerifiedDialedNumber == testNumber.VerifiedDialedNumber);

            testNumber.LocalAccessTransportArea = "testtest";
            var checkPut = await testNumber.PutAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkPut);

            checkSave = await VerifiedPhoneNumber.GetByOrderIdAsync(testNumber.OrderId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            Assert.True(checkSave.FirstOrDefault().LocalAccessTransportArea == testNumber.LocalAccessTransportArea);

            var checkDelete = await testNumber.DeleteAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetAllPortedPhoneNumbersAsync()
        {
            var results = await PortedPhoneNumber.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results.LastOrDefault()));

            var order = results.LastOrDefault();
            results = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            //results = await PortedPhoneNumber.GetByPortRequestIdAsync(order.PortRequestId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            //Assert.NotNull(results);
            //Assert.NotEmpty(results);
            //output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));

            results = await PortedPhoneNumber.GetByDialedNumberAsync(order.PortedDialedNumber, postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            var result = await PortedPhoneNumber.GetByIdAsync(order.PortedPhoneNumberId, postgresql).ConfigureAwait(false);
            Assert.NotNull(result);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PostPutDeletePortedPhoneNumberAsync()
        {
            var ported = new PortedPhoneNumber
            {
                PortedPhoneNumberId = Guid.NewGuid(),
                PortedDialedNumber = "2068588757",
                NPA = 206,
                NXX = 858,
                XXXX = 8757,
                State = "test",
                RequestStatus = "test",
                City = "test",
                DateFirmOrderCommitment = DateTime.Now,
                DateIngested = DateTime.Now,
                ExternalPortRequestId = "test",
                IngestedFrom = "test",
                OrderId = Guid.NewGuid(),
                PortRequestId = Guid.NewGuid(),
                Wireless = false
            };

            var checkPost = await ported.PostAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkPost);

            var verifyPost = await PortedPhoneNumber.GetByOrderIdAsync(ported.OrderId ?? Guid.Empty, postgresql).ConfigureAwait(false);
            Assert.NotNull(verifyPost);
            Assert.NotEmpty(verifyPost);
            var verified = verifyPost.FirstOrDefault();
            Assert.True(ported.OrderId == verified.OrderId);

            verified.ExternalPortRequestId = "testtest";
            var checkPut = await verified.PutAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkPut);

            var verifyPut = await PortedPhoneNumber.GetByIdAsync(verified.PortedPhoneNumberId, postgresql).ConfigureAwait(false);
            Assert.NotNull(verifyPut);
            Assert.True(verifyPut.ExternalPortRequestId == verified.ExternalPortRequestId);

            var checkDelete = await verifyPut.DeleteAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetAllPortRequestsAsync()
        {
            var results = await PortRequest.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task GetPortRequestByOrderIdAsync()
        {
            var results = await PortRequest.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var order = await PortRequest.GetByOrderIdAsync(results.FirstOrDefault().OrderId, postgresql).ConfigureAwait(false);
            output.WriteLine(JsonSerializer.Serialize(order));
        }

        [Fact]
        public async Task GetAllSentEmailsAsync()
        {
            var results = await Email.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task GetSentEmailByIdAsync()
        {
            var results = await Email.GetAllAsync(postgresql).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var email = results.FirstOrDefault();

            var result = await Email.GetAsync(email.EmailId, postgresql).ConfigureAwait(false);
            Assert.NotNull(result);
            Assert.Equal(email.EmailId, result.EmailId);
        }

        [Fact]
        public async Task PostSentEmailAsync()
        {
            var email = new Email
            {
                PrimaryEmailAddress = "test@test.com",
                Subject = "Test",
                DateSent = DateTime.Now,
                CarbonCopy = "Test",
                Completed = false,
                MessageBody = "This is an integration test.",
                OrderId = Guid.NewGuid()
            };

            var checkSubmit = await email.PostAsync(postgresql).ConfigureAwait(false);
            Assert.True(checkSubmit);

            // Clean up
            var fromDb = await Email.GetByOrderAsync(email.OrderId, postgresql).ConfigureAwait(false);
            Assert.NotNull(fromDb);
            Assert.NotEmpty(fromDb);
            foreach (var item in fromDb)
            {
                Assert.Equal(email.PrimaryEmailAddress, item.PrimaryEmailAddress);
                Assert.Equal(email.Subject, item.Subject);
                Assert.Equal(email.CarbonCopy, item.CarbonCopy);
                Assert.Equal(email.MessageBody, item.MessageBody);

                item.DateSent = DateTime.Now;
                item.Completed = true;

                var checkUpdate = await item.PutAsync(postgresql).ConfigureAwait(false);
                Assert.True(checkUpdate);

                var updatedDb = await Email.GetAsync(item.EmailId, postgresql).ConfigureAwait(false);
                Assert.NotNull(updatedDb);
                Assert.Equal(updatedDb.PrimaryEmailAddress, item.PrimaryEmailAddress);
                Assert.Equal(updatedDb.Subject, item.Subject);
                Assert.Equal(updatedDb.CarbonCopy, item.CarbonCopy);
                Assert.Equal(updatedDb.MessageBody, item.MessageBody);
                Assert.Equal(updatedDb.Completed, item.Completed);
                Assert.True(updatedDb.Completed);

                var checkDelete = await updatedDb.DeleteAsync(postgresql).ConfigureAwait(false);
                Assert.True(checkDelete);
            }
        }

        [Fact]
        public async Task GetOrderAsync()
        {
            var conn = postgresql;

            var results = await Order.GetAllAsync(conn).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            foreach (var result in results)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.OrderId.ToString()));
                Assert.False(string.IsNullOrWhiteSpace(result.FirstName));
                Assert.False(string.IsNullOrWhiteSpace(result.LastName));
                //Assert.False(string.IsNullOrWhiteSpace(result.Address));
                //Assert.False(string.IsNullOrWhiteSpace(result.City));
                //Assert.False(string.IsNullOrWhiteSpace(result.State));
                //Assert.False(string.IsNullOrWhiteSpace(result.Zip));
                Assert.False(string.IsNullOrWhiteSpace(result.Email));
                Assert.True(result.DateSubmitted > new DateTime(2019, 1, 1));
            }
        }


        [Fact]
        public async Task GetOrderByBackGroundworkNotCompletedAsync()
        {
            var conn = postgresql;

            var results = await Order.GetByBackGroundworkNotCompletedAsync(conn).ConfigureAwait(false);

            Assert.NotNull(results);
        }

        //[Fact]
        //public async Task GetSaleTaxForOrderAsync()
        //{
        //    var conn = postgresql;

        //    var results = await Order.GetAllAsync(conn).ConfigureAwait(false);

        //    Assert.NotNull(results);
        //    Assert.NotEmpty(results);

        //    var order = results.FirstOrDefault();

        //    var taxrate = await SalesTax.GetTaxRateAsync(order.Address, order.City, order.Zip).ConfigureAwait(false);

        //    Assert.True(taxrate > 0.0M);
        //    output.WriteLine($"Taxrate: {taxrate}");
        //}

        [Fact]
        public async Task GetRawSaleTaxInfoAsync()
        {
            string address = "6300 linderson way";
            string city = string.Empty;
            string zip = "98501";

            var result = await SalesTax.GetLocalAPIAsync(address, city, zip).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.rate1 > 0.0M);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PostOrderAsync()
        {
            var conn = postgresql;

            var orders = await Order.GetAllAsync(conn).ConfigureAwait(false);

            var selectedOrders = orders.Where(x => x.OrderId != Guid.Empty).FirstOrDefault();
            Assert.False(selectedOrders is null);

            var result = await Order.GetByIdAsync(selectedOrders.OrderId, conn);

            result.FirstName = "Integration";
            result.LastName = "Test";
            result.OrderId = Guid.NewGuid();

            var response = await result.PostAsync(conn);
            Assert.True(response);

            // Clean up.
            var fromDb = await Order.GetByIdAsync(result.OrderId, conn);

            Assert.False(fromDb is null);

            var checkDelete = await fromDb.DeleteAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PostProductOrderByProductIdAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };

            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDb = await ProductOrder.GetAsync(itemToOrder.OrderId, conn).ConfigureAwait(false);
            Assert.NotNull(fromDb);
            Assert.NotEmpty(fromDb);

            var checkDelete = await fromDb.FirstOrDefault().DeleteByOrderAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }


        [Fact]
        public async Task PostProductOrderByDialedNumberAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                OrderId = Guid.NewGuid(),
                DialedNumber = "8605530426",
                Quantity = 1
            };

            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDb = await ProductOrder.GetAsync(itemToOrder.OrderId, conn).ConfigureAwait(false);
            Assert.NotNull(fromDb);
            Assert.NotEmpty(fromDb);

            var checkDelete = await fromDb.FirstOrDefault().DeleteByOrderAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PostPurchasedPhoneNumberAsync()
        {
            var conn = postgresql;

            var itemToOrder = new PurchasedPhoneNumber
            {
                OrderId = Guid.NewGuid(),
                DialedNumber = "8605530426",
                DateIngested = DateTime.Now.AddDays(-1),
                DateOrdered = DateTime.Now,
                IngestedFrom = "IntegrationTest",
                OrderResponse = "\"code\":200,",
                Completed = true
            };

            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDb = await PurchasedPhoneNumber.GetByDialedNumberAndOrderIdAsync(itemToOrder.DialedNumber, itemToOrder.OrderId, conn).ConfigureAwait(false);
            Assert.NotNull(fromDb);

            var checkDelete = await fromDb.DeleteAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PostEmergencyInfoAsync()
        {
            var conn = postgresql;

            var info = new EmergencyInformation
            {
                Address = "123 Sesemy Street",
                AlertGroup = "123",
                City = "Seattle",
                CreatedDate = DateTime.Now,
                DateIngested = DateTime.Now,
                DialedNumber = "1231231234",
                FullName = "Big Bird",
                IngestedFrom = "IntegrationTest",
                ModifyDate = DateTime.Now,
                Note = "This is an integration test.",
                State = "WA",
                TeliId = "1231231",
                UnitNumber = "123",
                UnitType = "Condo",
                Zip = "99999"
            };

            var result = await info.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDbResults = await EmergencyInformation.GetAllAsync(conn).ConfigureAwait(false);
            Assert.NotNull(fromDbResults);
            Assert.NotEmpty(fromDbResults);
            var fromDb = fromDbResults.Where(x => x.TeliId == info.TeliId).FirstOrDefault();

            var checkDelete = await fromDb.DeleteAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetProductOrderAsync()
        {
            // Arrange
            var conn = postgresql;

            // Create the order.
            var itemToOrder = new ProductOrder
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };


            var result = await itemToOrder.PostAsync(conn).ConfigureAwait(false);
            Assert.True(result);

            // Act
            var results = await ProductOrder.GetAsync(itemToOrder.OrderId, conn).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            foreach (var order in results)
            {
                Assert.Equal(order.OrderId, itemToOrder.OrderId);
            }

            // Clean up.
            var checkDelete = await results.FirstOrDefault().DeleteByOrderAsync(conn).ConfigureAwait(false);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task OwnedPhoneNumberCRUDAsync()
        {
            // Arrange
            var conn = postgresql;
            var ownedPhoneNumber = new OwnedPhoneNumber
            {
                DialedNumber = "8605530426",
                Active = true,
                BillingClientId = "1",
                DateIngested = DateTime.Now,
                IngestedFrom = "IntegrationTest",
                OwnedBy = "IntegrationTest",
                Notes = "CoolNote",
                SPID = "0000",
                SPIDName = "IntegrationTest",
                LIDBCNAM = "Test Test"
            };

            // Act
            var checkCreate = await ownedPhoneNumber.PostAsync(postgresql).ConfigureAwait(false);

            Assert.True(checkCreate);

            var results = await OwnedPhoneNumber.GetAllAsync(postgresql).ConfigureAwait(false);
            var fromDb = results.Where(x => x.IngestedFrom == ownedPhoneNumber.IngestedFrom).FirstOrDefault();

            Assert.NotNull(fromDb);
            fromDb.Notes = "IntegrationTest";

            var checkUpdate = await fromDb.PutAsync(postgresql).ConfigureAwait(false);

            Assert.True(checkUpdate);

            results = await OwnedPhoneNumber.GetAllAsync(postgresql).ConfigureAwait(false);
            var fromDbAgain = results.Where(x => x.IngestedFrom == ownedPhoneNumber.IngestedFrom).FirstOrDefault();

            // Assert
            Assert.NotNull(fromDb);
            Assert.Equal(fromDb.IngestedFrom, fromDbAgain.IngestedFrom);
            Assert.Equal(fromDb.Notes, fromDbAgain.Notes);
            Assert.Equal(fromDb.OwnedPhoneNumberId, fromDbAgain.OwnedPhoneNumberId);

            // Clean up
            var checkDelete = await fromDbAgain.DeleteAsync(postgresql).ConfigureAwait(false);

            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetProductAsync()
        {
            var conn = postgresql;

            var results = await Product.GetAsync("GXP2140", conn);

            Assert.NotNull(results);
        }

        [Fact]
        public async Task GetAllIngestsAsync()
        {
            var conn = postgresql;

            var results = await IngestStatistics.GetAllAsync(conn);

            Assert.NotNull(results);
        }

        [Fact]
        public async Task GetLastIngestAsync()
        {
            var conn = postgresql;

            var results = await IngestStatistics.GetLastIngestAsync("TeleMessage", conn);

            Assert.NotNull(results);
        }

        [Fact]
        public async Task GetLockAsync()
        {
            var conn = postgresql;

            // Prevent another run from starting while this is still going.
            var lockingStats = new IngestStatistics
            {
                IngestedFrom = "Test",
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

            var checkLock = await lockingStats.PostAsync(conn).ConfigureAwait(false);

            Assert.True(checkLock);

            var results = await IngestStatistics.GetLockAsync("Test", conn).ConfigureAwait(false);

            Assert.NotNull(results);

            var checkRemoveLock = await results.DeleteAsync(conn).ConfigureAwait(false);

            Assert.True(checkRemoveLock);
        }


        //[Fact]
        //public async Task ServiceMigrationScriptAsync()
        //{
        //    var conn = postgresql;

        //    var services = new List<Service>
        //    {
        //        new Service
        //        {
        //            Name = "E911 Connectivity Fee",
        //            Description = "E911 service is required each line or seat by the FCC.",
        //            Price = 3
        //        },
        //        new Service
        //        {
        //            Name = "Web Texting",
        //            Description = "Text from any browser.",
        //            Price = 10
        //        },
        //        new Service
        //        {
        //            Name = "LTE Backup",
        //            Description = "Always stay online",
        //            Price = 30
        //        },
        //        new Service
        //        {
        //            Name = "Toll Free Number",
        //            Description = "Free for clients to call",
        //            Price = 10
        //        },
        //        new Service
        //        {
        //            Name = "Fax Server",
        //            Description = "Send and Recieve Faxes",
        //            Price = 10
        //        },
        //        new Service
        //        {
        //            Name = "Standard Lines",
        //            Description = "Supports 1 concurrent call per Line",
        //            Price = 35
        //        },
        //        new Service
        //        {
        //            Name = "Concurrent Seats",
        //            Description = "Supports 1 concurrent call per Seat",
        //            Price = 20
        //        },
        //        new Service
        //        {
        //            Name = "LTE Internet",
        //            Description = "Cellular internet",
        //            Price = 70
        //        },
        //        new Service
        //        {
        //            Name = "5G Internet",
        //            Description = "The latest in cellular internet",
        //            Price = 80
        //        }
        //    };

        //    foreach (var service in services)
        //    {
        //        var checkSubmit = await service.PostAsync(conn);

        //        Assert.True(checkSubmit);
        //    }
        //}

        //[Fact]
        //public async Task ProductMigrationScriptAsync()
        //{
        //    var conn = postgresql;

        //    var products = new List<Product>
        //    {
        //        new Product
        //        {
        //            Name = "GRP2615",
        //            Description = "Bringing Bluetooth 5 & Wireless support to the table, the GRP2615 is a high performing, versatile phone that is at home on any desk! Supports Power over Ethernet.",
        //            Price = 160,
        //            Image = "/images/scaled/GRP2615-front-400.jpg"
        //        },
        //        new Product
        //        {
        //            Name = "GRP2613",
        //            Description = "The GRP2613 is able to serve diverse needs as business evolves given its 6 programmable keys, Power over Ethernet, headset support and color screen.",
        //            Price = 80,
        //            Image = "/images/scaled/GRP2613front-400.jpg"
        //        },
        //        new Product
        //        {
        //            Name = "GRP2612",
        //            Description = "The GRP2612 is a workhorse! Offering a color screen, HD voice and 4 programmable keys, it is a great value.",
        //            Price = 65,
        //            Image = "/images/scaled/GRP2612front-400.jpg"
        //        },
        //        new Product
        //        {
        //            Name = "GRP2612 WiFi",
        //            Description = "Need a phone where only WiFi exists? The GRP2612W has you covered with powerful WiFi 5 (aka 802.11AC) support built in. Plug into power and dial away!",
        //            Price = 85,
        //            Image = "/images/scaled/GRP2612front-400.jpg"
        //        },
        //        new Product
        //        {
        //            Name = "DP720",
        //            Description = "Need a solution that can serve your workplace without cables or daily charging? DP720 Cordless handsets are easy to use and flexible!",
        //            Price = 85,
        //            Image = "/images/scaled/DP750_720_combination_3-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "GXP2160",
        //            Description = "Need to know the status of your coworkers at a glance? With 24 programmable keys on the GXP2160 monitoring every phone is a breeze!",
        //            Price = 100,
        //            Image = "/images/scaled/gxp2160-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "GXP2170",
        //            Description = "Offering 12 programmable keys, Bluetooth, HD Voice, Gigabit and predictive dialing, the GXP2170 is able to bring your business together!",
        //            Price = 90,
        //            Image = "/images/scaled/gxp2170-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "DP722",
        //            Description = "Lighter, thinner and in silver? What is not to love about the DP722! The same great cordless phone, but with 20% less weight.",
        //            Price = 95,
        //            Image = "/images/scaled/DP722-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "HT802",
        //            Description = "Keep your classic analog devices humming along with the HT802 Analog Adapter. Supports 2 active calls (1 on each port) and HD Voice.",
        //            Price = 40,
        //            Image = "/images/scaled/ht802-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "GXP2140",
        //            Description = "Need a great phone at a reasonable price? The GXP2140 has you covered with Bluetooth, a 4.3\" color screen, gigabit & PoE!",
        //            Price = 70,
        //            Image = "/images/scaled/gxp2140-400.png"
        //        },
        //        new Product
        //        {
        //            Name = "Polycom Soundstation IP 7000",
        //            Description = "Light up your conference room with the vibrant sound quality provided by the Polycom Soundstation IP 7000! Supports 3 way calling, HD Voice & Power over Ethernet.",
        //            Price = 300,
        //            Image = "/images/scaled/11315553-a8da-4a45-b632-b8ced825957dscaled.jpg"
        //        },
        //        new Product
        //        {
        //            Name = "Snom PA1",
        //            Description = "Enable paging over speaker systems with the Snom PA1. The built in amp enables small scale deployments, while the numerous outputs can tie into existing amplifiers.",
        //            Price = 150,
        //            Image = "/images/scaled/SNO-PA1image1scaled.jpg"
        //        }
        //    };

        //    // Get existing products from the Db.
        //    var existing = await Product.GetAllAsync(postgresql);

        //    foreach (var product in products)
        //    {
        //        var match = existing.Where(x => x.Name == product.Name).FirstOrDefault();

        //        if (match != null && !string.IsNullOrWhiteSpace(match.Name))
        //        {
        //            // Update the existing products.
        //            match.Image = product.Image;
        //            match.Price = product.Price;
        //            match.Description = product.Description;
        //            var checkUpdate = await match.PutAsync(postgresql);
        //            Assert.True(checkUpdate);
        //        }
        //        else
        //        {
        //            // Add the new product to the Db.
        //            var checkSubmit = await product.PostAsync(conn);

        //            Assert.True(checkSubmit);
        //        }
        //    }
        //}
    }
}