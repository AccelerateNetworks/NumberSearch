using BulkVS;

using FirstCom;

using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleMesssage;

using ServiceReference;

using System;
using System.Collections.Generic;
using System.Linq;
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

        public Integration(ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();

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
        }

        [Fact]
        public async Task GetAllBillingClientsAsync()
        {
            // Act
            var result = await Client.GetAllClientsAsync(invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task GetAllBillingInvoicesAsync()
        {
            // Act
            var result = await Invoice.GetAllAsync(invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task GetBillingInvoiceByIdAsync()
        {
            // Act
            var result = await Invoice.GetByIdAsync(1661, invoiceNinjaToken);

            // Assert        
            Assert.NotNull(result);
            Assert.Equal(1661, result.id);
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task GetBillingTaxRatesAsync()
        {
            // Act
            var result = await TaxRate.GetAllAsync(invoiceNinjaToken).ConfigureAwait(false);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result.data);
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(client));

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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(testClient));


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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

        //    result.invoice_items.FirstOrDefault().notes = "Updated";

        //    var updateTest = await result.PutAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    // Assert        
        //    Assert.NotNull(updateTest);
        //    Assert.Equal(result.invoice_items.FirstOrDefault().notes, updateTest.invoice_items.FirstOrDefault().notes);
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(updateTest));

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
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

        //    result.contacts.FirstOrDefault().first_name = "IntegrationTest";

        //    var updateResult = await result.PutAsync(invoiceNinjaToken).ConfigureAwait(false);

        //    Assert.NotNull(result);
        //    Assert.Equal(updateResult.name, result.name);
        //    Assert.Equal(updateResult.id, result.id);
        //    Assert.Equal(updateResult.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task LIDBLookupAsync()
        {
            // Arrange
            string phoneNumber = "14257808879";

            // Act
            var result = await LIDBLookup.GetAsync(phoneNumber, _data247username, _data247password).ConfigureAwait(false);

            // Assert        
            Assert.NotNull(result);
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
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
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));

            results = await EmergencyInfo.GetAsync("9365820436", token);
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
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
                output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
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

        //[Fact]
        //public async Task LocalNumberPortabilityTestAsync()
        //{
        //    // Arrange
        //    string query = "1";

        //    // Act
        //    var result = await LnpCheck.IsPortable(query, token);

        //    // Assert
        //    Assert.False(result);

        //    result = await LnpCheck.IsPortable("8662214141", token);

        //    // Assert
        //    Assert.True(result);
        //}


        [Fact]
        public async Task PComNetDIDInventorySearchAsyncTestAsync()
        {
            var DIDSearch = new DIDOrderQuery
            {
                DID = "12062670472",
                NPA = "206",
                NXX = "267",
                RateCenter = "SEATTLE"
            };
            var ReturnAmount = 100;

            var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(pComNetCredentials, DIDSearch, ReturnAmount);

            Assert.NotNull(result);
            Assert.NotEmpty(result.DIDOrder);

            foreach (var x in result.DIDOrder)
            {
                output.WriteLine(x.DID);
            }
        }

        [Fact]
        public async Task FirstComGetPhoneNumbersTestAsync()
        {
            var results = await NpaNxxFirstPointCom.GetAsync("206", string.Empty, string.Empty, pComNetCredentials.Username, pComNetCredentials.Password);

            Assert.NotNull(results);
            Assert.NotEmpty(results);
            int count = 0;

            foreach (var result in results.ToArray())
            {
                output.WriteLine(result.DialedNumber);
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                Assert.True(result.XXXX > 1);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
        }

        [Fact]
        public async Task BulkVSNpaNxxGetAsyncTestAsync()
        {
            // Arrange
            var npanxx = "206279";

            // Act
            var results = await NpaNxxBulkVS.GetAsync(npanxx, bulkVSKey, bulkVSSecret);

            // Assert
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results.ToArray())
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                Assert.True(result.XXXX > 1);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
        }

        //[Fact]
        //public async Task BulkVSRESTNpaNxxGetAsyncTestAsync()
        //{
        //    // Arrange
        //    var npa = 206;

        //    // Act
        //    var results = await DataAccess.BulkVS.OrderTn.GetAsync(npa, bulkVSUsername, bulkVSPassword);

        //    // Assert
        //    Assert.NotNull(results);
        //    int count = 0;
        //    foreach (var result in results.ToArray())
        //    {
        //        Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
        //        count++;
        //    }
        //    output.WriteLine($"{count} Results Reviewed");
        //}

        [Fact]
        public async Task BulkVSLrnLookupAsync()
        {
            // Arrange
            var number = "2068588757";

            // Act
            var result = await LrnBulkCnam.GetAsync(number, bulkVSKey).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrWhiteSpace(result.spid));
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task BulkVSNpaNxxGetAsyncBadInputTestAsync()
        {
            // Arrange
            var npanxx = "999999";

            // Act
            var results = await NpaNxxBulkVS.GetAsync(npanxx, bulkVSKey, bulkVSSecret);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.ToArray().Length == 0);
        }

        [Fact]
        public async Task BulkVSNpaGetAsyncTestAsync()
        {
            // Arrange
            var npa = "206";

            // Act
            var results = await NpaBulkVS.GetAsync(npa, bulkVSKey, bulkVSSecret);

            // Assert
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results.ToArray())
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                Assert.True(result.XXXX > 1);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
        }

        [Fact]
        public async Task BulkVSGetAllOwnedNumbersAsync()
        {
            // Arrange

            // Act
            var results = await BulkVSOwnedPhoneNumbers.GetAllAsync(bulkVSKey, bulkVSSecret).ConfigureAwait(false);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        //[Fact]
        //public async Task TelePhoneNumbersTestAsync()
        //{
        //    // Arrange
        //    var query = "206";

        //    // Act
        //    var results = await DidsList.GetAsync(query, token);

        //    // Assert
        //    Assert.NotNull(results);
        //    int count = 0;
        //    foreach (var result in results.ToArray())
        //    {
        //        Assert.True(result.NPA > 99);
        //        Assert.True(result.NXX > 99);
        //        Assert.True(result.XXXX > 0);
        //        Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
        //        Assert.False(string.IsNullOrWhiteSpace(result.City));
        //        Assert.False(string.IsNullOrWhiteSpace(result.State));
        //        Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
        //        count++;
        //    }
        //    output.WriteLine($"{count} Results Reviewed");
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

        //[Fact]
        //public async Task TeleNoteTestAsync()
        //{
        //    // Arrange
        //    var number = "2068588757";

        //    // Act
        //    var results = await UserDidsGet.GetAsync(number, token).ConfigureAwait(false);

        //    Assert.NotNull(results);
        //    output.WriteLine(results.data.id);

        //    var note = $"This is a test note.";

        //    var setNote = await UserDidsNote.SetNote(note, results.data.id, token).ConfigureAwait(false);

        //    Assert.NotNull(setNote);
        //    Assert.True(setNote.code == 200);
        //    output.WriteLine(setNote.data);
        //}


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
            var results = await PhoneNumber.GetAllAsync(conn);
            var number = results.OrderBy(x => x.DialedNumber).LastOrDefault();
            number.IngestedFrom = "IntegrationTest";
            number.XXXX++;
            number.DialedNumber = $"{number.NPA}{number.NXX}{number.XXXX}";
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
        public async Task CheckIfNumberExistsTestAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.GetAllAsync(conn);
            var existingNumbers = await PhoneNumber.GetAllAsync(conn);
            var dict = existingNumbers.ToDictionary(x => x.DialedNumber, x => x);

            foreach (var result in results.ToArray())
            {
                var check = result.ExistsInDb(dict);
                Assert.True(check);
            }
            var badExample = results.OrderBy(x => x.DialedNumber).LastOrDefault();
            var newXXXX = badExample.XXXX + 1;
            badExample.DialedNumber = $"{badExample.NPA}{badExample.NXX}{newXXXX}";
            var badCheck = badExample.ExistsInDb(dict);
            Assert.False(badCheck);
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
            var conn = postgresql;

            var result = await PhoneNumber.GetCountByProvider("BulkVS", conn);

            Assert.True(result > 0);
            output.WriteLine(result.ToString());
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
                Completed = true,
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

                var checkDelete = await item.DeleteAsync(postgresql).ConfigureAwait(false);
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
                Assert.False(string.IsNullOrWhiteSpace(result.Address));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.Zip));
                Assert.False(string.IsNullOrWhiteSpace(result.Email));
                Assert.True(result.DateSubmitted > new DateTime(2019, 1, 1));
            }
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

        //[Fact]
        //public async Task GetRawSaleTaxInfoAsync()
        //{
        //    string address = "LINDERSON WAY SW";
        //    string city = string.Empty;
        //    string zip = "98501";

        //    var result = await SalesTax.GetAsync(address, city, zip).ConfigureAwait(false);

        //    Assert.NotNull(result);
        //    Assert.True(result.rate1 > 0.0M);
        //    output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        //}

        [Fact]
        public async Task PostOrderAsync()
        {
            var conn = postgresql;

            var orders = await Order.GetAllAsync(conn).ConfigureAwait(false);

            var selectedOrders = orders.Where(x => (x.OrderId != null) && (x.OrderId != Guid.Empty)).FirstOrDefault();
            Assert.False(selectedOrders is null);

            var result = await Order.GetByIdAsync(selectedOrders.OrderId, conn);

            result.FirstName = "Integration";
            result.LastName = "Test";

            var response = await result.PostAsync(conn);
            Assert.True(response);

            // Clean up.
            var fromDb = await Order.GetByEmailAsync(result.Email, conn).ConfigureAwait(false);

            var newOrderFromDb = fromDb.Where(x => x.FirstName == result.FirstName && x.LastName == result.LastName).FirstOrDefault();
            Assert.False(newOrderFromDb is null);

            var checkDelete = await newOrderFromDb.DeleteAsync(conn).ConfigureAwait(false);
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