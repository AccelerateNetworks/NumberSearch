
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.FusionPBX;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.LCGuide;
using NumberSearch.DataAccess.Models;
using NumberSearch.DataAccess.TeleDynamics;
using NumberSearch.Mvc.Controllers;
using NumberSearch.Mvc.Models;

using ServiceReference1;

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
        private readonly ITestOutputHelper output;
        private readonly Credentials pComNetCredentials;
        private readonly string bulkVSKey;
        private readonly string bulkVSUsername;
        private readonly string bulkVSPassword;
        private readonly string postgresql;
        private readonly string invoiceNinjaToken;
        private readonly string _teleDynamicsUsername;
        private readonly string _teleDynamicsPassword;
        private readonly MvcConfiguration _configuration;

        public Integration(ITestOutputHelper output)
        {
            this.output = output;


            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();
            MvcConfiguration mvcConfiguration = new();
            config.Bind("ConnectionStrings", mvcConfiguration);
            _configuration = mvcConfiguration;

            pComNetCredentials = new Credentials
            {
                Username = config.GetConnectionString("PComNetUsername"),
                Password = config.GetConnectionString("PComNetPassword")
            };

            bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            bulkVSUsername = config.GetConnectionString("BulkVSUsername");
            bulkVSPassword = config.GetConnectionString("BulkVSPassword");
            postgresql = config.GetConnectionString("PostgresqlProd");
            invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _teleDynamicsUsername = config.GetConnectionString("TeleDynamicsUsername");
            _teleDynamicsPassword = config.GetConnectionString("TeleDynamicsPassword");
            //_call48Username = config.GetConnectionString("Call48Username");
            //_call48Password = config.GetConnectionString("Call48Password");
            //_callWithUsAPIkey = config.GetConnectionString("CallWithUsAPIKEY");
            //_peerlessApiKey = config.GetConnectionString("PeerlessAPIKey");
        }

        // Takes 6 seconds to run
        //[Fact]
        //public async Task VerifyAllPriorityNumbersArePurchasableAsync()
        //{
        //    // Act
        //    var result = await PhoneNumber.GetAllByAreaCodeAsync(541, postgresql);
        //    output.WriteLine($"{result.Count()} numbers found in NumberSearch for NPA 541.");

        //    var byNXX = result.GroupBy(x => x.NXX).ToArray();
        //    var NXXs = byNXX.Select(x => x.Key).ToArray();
        //    output.WriteLine($"{byNXX.Length} NXX groups found in NPA 541.");
        //    output.WriteLine(JsonSerializer.Serialize(NXXs));

        //    int purchaseableNumbersTotal = 0;
        //    int unpurchasableNumbersTotal = 0;

        //    // Avoid calling the API for every number by grouping them by the NXX, which is the smallest unit we can ask the API for.
        //    foreach (var nxxGroup in byNXX)
        //    {
        //        int purchaseableNumbers = 0;
        //        int unpurchasableNumbers = 0;

        //        int NPA = nxxGroup.FirstOrDefault().NPA;
        //        int NXX = nxxGroup.FirstOrDefault().NXX;
        //        var doesItStillExist = await OrderTn.GetAsync(NPA, NXX, bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

        //        foreach (var phoneNumber in nxxGroup)
        //        {
        //            var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
        //            if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
        //            {
        //                purchaseableNumbers++;
        //            }
        //            else
        //            {
        //                unpurchasableNumbers++;
        //                if (unpurchasableNumbers is 1)
        //                {
        //                    output.WriteLine($"Unpurchasable Example: {JsonSerializer.Serialize(phoneNumber)}");
        //                }
        //            }
        //        }

        //        purchaseableNumbersTotal += purchaseableNumbers;
        //        unpurchasableNumbersTotal += unpurchasableNumbers;

        //        output.WriteLine($"{doesItStillExist.Length} numbers found in BulkVS for {NPA}, {NXX}. {nxxGroup.Count()} found in NumbersSearch. A difference of {doesItStillExist.Length - nxxGroup.Count()} numbers.");
        //        output.WriteLine($"{purchaseableNumbers} from NumberSearch were still purchaseable in BulkVS. {unpurchasableNumbers} were not purchasable.");
        //    }

        //    output.WriteLine($"Of {result.Count()} numbers found in NumberSearch for NPA 541; {purchaseableNumbersTotal} were still purchaseable in BulkVS and {unpurchasableNumbersTotal} were not purchasable.");
        //}

        [Fact]
        public async Task GetAllBillingClientsAsync()
        {
            // Act
            var result = await Client.GetAllClientsAsync(invoiceNinjaToken);

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().id));
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
            Assert.NotEmpty(result);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetBillingInvoiceByIdAsync()
        {
            // Act
            var result = await Invoice.GetByIdAsync("olejnL4ejN", invoiceNinjaToken);

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.id));
            Assert.Equal("olejnL4ejN", result.id);
            Assert.True(result.balance > 0);
            output.WriteLine(JsonSerializer.Serialize(result));

            // Act
            result = await Invoice.GetByIdAsync("oQeZZ5vepZ", invoiceNinjaToken);

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.id));
            Assert.Equal("oQeZZ5vepZ", result.id);
            Assert.Equal(0, result.balance);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetBillingInvoiceByClientIdAsync()
        {
            // Act
            var result = await Invoice.GetByClientIdWithInoviceLinksAsync("7N1aM6ObWm", invoiceNinjaToken, false);

            // Assert        
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            var element = result.FirstOrDefault();
            Assert.Equal("7N1aM6ObWm", element.client_id);
            Assert.False(string.IsNullOrWhiteSpace(result.LastOrDefault().invitations.FirstOrDefault().link));
            output.WriteLine(result.LastOrDefault().invitations.FirstOrDefault().link);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetBillingTaxRatesAsync()
        {
            // Act
            var result = await DataAccess.InvoiceNinja.TaxRate.GetAllAsync(invoiceNinjaToken.AsMemory());

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().id));
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().name));
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
        //    var result = await taxRate.PostAsync(invoiceNinjaToken);

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
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().id));
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().contacts.FirstOrDefault().id));
            Assert.False(string.IsNullOrWhiteSpace(result.data.FirstOrDefault().contacts.FirstOrDefault().email));
            Assert.NotEmpty(result.data);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        //[Fact]
        //public async Task SetAllHardwareInstallTimesTo15Minutes()
        //{
        //    // Act
        //    var result = await Product.GetAllAsync(postgresql);

        //    foreach (var item in result)
        //    {
        //        item.InstallTime = 0.25m;
        //        var checkUpdate = await item.PutAsync(postgresql);
        //        Assert.True(checkUpdate);
        //    }
        //}

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
        //    var result = await testCreate.PostAsync(invoiceNinjaToken);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.Equal(result.invoice_items.FirstOrDefault().notes, testCreate.invoice_items.FirstOrDefault().notes);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    var checkSend = await result.SendInvoiceAsync(invoiceNinjaToken);

        //    Assert.True(checkSend);
        //}

        [Fact]
        public async Task GetBillingClientByIdAsync()
        {
            // Act
            var result = await Client.GetByIdAsync("q9wdLRXajP", invoiceNinjaToken);

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.id));
            Assert.Equal("q9wdLRXajP", result.id);
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

        //    var checkDelete = await result.DeleteAsync(invoiceNinjaToken);

        //    Assert.NotNull(checkDelete);
        //    Assert.True(checkDelete.is_deleted);
        //}

        [Fact]
        public async Task CreateUpdateAndDeleteBillingInvoiceByClientByIdAsync()
        {

            // Arrange
            var testCreateClient = new ClientDatum
            {
                name = "IntegrationTest",
                contacts = [
                    new ClientContact {
                        email = "integrationTest@example.com"
                    }
                ]
            };

            // Act
            var testClient = await testCreateClient.PostAsync(invoiceNinjaToken.AsMemory());

            // Assert        
            Assert.Equal(testCreateClient.name, testClient.name);
            Assert.Equal(testCreateClient.contacts.FirstOrDefault().email, testClient.contacts.FirstOrDefault().email);
            output.WriteLine(JsonSerializer.Serialize(testClient));

            var existingTaxRates = await DataAccess.InvoiceNinja.TaxRate.GetAllAsync(invoiceNinjaToken.AsMemory());
            var rate = existingTaxRates.data.LastOrDefault();

            var testInvoice = new Line_Items[] {
                new() {
                    product_key = "IntegrationTest",
                    notes = "IntegrationTest",
                    cost = 10,
                    quantity = 1
                }
            };

            var testCreate = new InvoiceDatum
            {
                client_id = testClient.id,
                line_items = testInvoice,
                tax_name1 = rate.name,
                tax_rate1 = rate.rate,
            };

            // Act
            var result = await testCreate.PostAsync(invoiceNinjaToken);

            // Assert        
            Assert.Equal(result.line_items.FirstOrDefault().notes, testCreate.line_items.FirstOrDefault().notes);
            output.WriteLine(JsonSerializer.Serialize(result));

            var item = result.line_items.FirstOrDefault();
            result.line_items[0] = item with { notes = "Updated" };

            var updateTest = await result.PutAsync(invoiceNinjaToken);

            // Assert        
            Assert.Equal(result.line_items.FirstOrDefault().notes, updateTest.line_items.FirstOrDefault().notes);
            output.WriteLine(JsonSerializer.Serialize(updateTest));

            var deleteTest = await updateTest.DeleteAsync(invoiceNinjaToken);

            Assert.True(deleteTest.is_deleted);

            var checkDelete = await testClient.DeleteAsync(invoiceNinjaToken.AsMemory());

            Assert.True(checkDelete.is_deleted);
        }

        [Fact]
        public async Task CreateAndUpdateBillingClientByIdAsync()
        {
            // Arrange
            var testCreate = new ClientDatum
            {
                name = "IntegrationTest",
                contacts = [
                    new ClientContact {
                        email = "integrationTest@example.com"
                    }
                ]
            };

            // Act
            var result = await testCreate.PostAsync(invoiceNinjaToken.AsMemory());

            // Assert        
            Assert.Equal(testCreate.name, result.name);
            Assert.Equal(testCreate.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
            output.WriteLine(JsonSerializer.Serialize(result));

            var item = result.contacts.FirstOrDefault();
            result.contacts[0] = item with { first_name = "IntegrationTest" };

            var updateResult = await result.PutAsync(invoiceNinjaToken.AsMemory());

            Assert.Equal(updateResult.name, result.name);
            Assert.Equal(updateResult.id, result.id);
            Assert.Equal(updateResult.contacts.FirstOrDefault().email, result.contacts.FirstOrDefault().email);
            output.WriteLine(JsonSerializer.Serialize(result));

            var checkDelete = await updateResult.DeleteAsync(invoiceNinjaToken.AsMemory());

            Assert.True(checkDelete.is_deleted);
        }

        // Disabled because it costs money
        //[Fact]
        // public async Task LRNLookupTestAsync()
        // {
        //     // Arrange
        //     string phoneNumber = "2065579450";

        //     // Act
        //     var result = await LrnLookup.GetAsync(phoneNumber, token);

        //     // Assert        
        //     Assert.NotNull(result);
        //     Assert.False(string.IsNullOrWhiteSpace(result.status));
        //     Assert.True(result.code == 200);
        //     Assert.False(string.IsNullOrWhiteSpace(result.data.lrn));
        //     output.WriteLine(JsonSerializer.Serialize(result));
        // }

        // Disabled because it costs money
        //[Fact]
        //public async Task CarrierLookupTestAsync()
        //{
        //    // Arrange
        //    string phoneNumber = "2065579450";

        //    // Act
        //    var result = await LineTypeIntelligenceResponse.GetByDialedNumberAsync(phoneNumber.AsMemory(), _configuration.TwilioUsername.AsMemory(), _configuration.TwilioPassword.AsMemory());

        //    // Assert        
        //    Assert.False(string.IsNullOrWhiteSpace(result.line_type_intelligence.carrier_name));
        //    output.WriteLine(JsonSerializer.Serialize(result));
        //}

        //[Fact]
        //public async Task LookupPageWithBadNumbersAsync()
        //{
        //    // Arrange
        //    string phoneNumber1 = "2065552121";
        //    string phoneNumber2 = "5253747761";
        //    //string phoneNumber3 = "8886409088";

        //    // Act
        //    var lookup = new NumberSearch.Mvc.Controllers.LookupController(_configuration);
        //    var result = await lookup.VerifyPortabilityAsync(phoneNumber1);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.False(result.Portable);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    result = await lookup.VerifyPortabilityAsync(phoneNumber2);

        //    // Assert        
        //    Assert.NotNull(result);
        //    Assert.False(result.Portable);
        //    output.WriteLine(JsonSerializer.Serialize(result));

        //    //result = await lookup.VerifyPortablityAsync(phoneNumber3);

        //    //// Assert        
        //    //Assert.NotNull(result);
        //    //Assert.False(result.Portable);
        //    //output.WriteLine(JsonSerializer.Serialize(result));
        //}

        // We are no longer using data 24/7 for cnam lookups.
        //[Fact]
        //public async Task LIDBLookupAsync()
        //{
        //    // Arrange
        //    string phoneNumber = "14257808879";

        //    // Act
        //    var result = await LIDBLookup.GetAsync(phoneNumber, _data247username, _data247password);

        //    // Assert        
        //    Assert.NotNull(result);
        //    output.WriteLine(JsonSerializer.Serialize(result));
        //}

        [Fact]
        public async Task RateCenterLookupAsync()
        {
            // Arrange
            int npa = 425;
            int nxx = 780;

            // Act
            var result = await RateCenterLookup.GetAsync(npa, nxx);

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.Region.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(result.RateCenter.ToString()));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BadRateCenterLookupAsync()
        {
            // Arrange
            int npa = 844;
            int nxx = 646;

            // Act
            var result = await RateCenterLookup.GetAsync(npa, nxx);

            // Assert        
            Assert.True(string.IsNullOrWhiteSpace(result.Region.ToString()));
            Assert.True(string.IsNullOrWhiteSpace(result.RateCenter.ToString()));
            output.WriteLine(JsonSerializer.Serialize(result));

        }


        [Fact]
        public async Task GetDestinationDetailsAsync()
        {
            // Arrange

            // Act
            var result = await DestinationDetails.GetByDialedNumberAsync("4254541206".AsMemory(), _configuration.FusionPBXConnectionString.AsMemory());

            // Assert        
            Assert.True(result.destination_enabled);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task GetDomainDetailsAsync()
        {
            // Arrange

            // Act
            var result = await DomainDetails.GetByDomainIdAsync(new Guid("f86cace8-9d5c-47df-b084-48e6cb58a95d"), _configuration.FusionPBXConnectionString.AsMemory());

            // Assert        
            Assert.False(string.IsNullOrWhiteSpace(result.domain_name));
            output.WriteLine(JsonSerializer.Serialize(result));

        }

        //[Fact]
        //public async Task CallWithUsLRNLookupAsync()
        //{
        //    // Arrange
        //    string canadaNumber = "6042400507";

        //    // Act
        //    var result = await DataAccess.CallWithUs.LRNLookup.GetAsync(canadaNumber, _callWithUsAPIkey);

        //    // Assert        
        //    Assert.NotNull(result);
        //    output.WriteLine(JsonSerializer.Serialize(result));
        //}

        [Fact]
        public async Task PComNetDIDInventorySearchAsyncTestAsync()
        {
            var DIDSearch = new DIDOrderQuery
            {
                DID = string.Empty,
                NPA = string.Empty,
                NXX = string.Empty,
                RateCenter = string.Empty
            };
            var ReturnAmount = 100;

            var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(pComNetCredentials, DIDSearch, ReturnAmount);

            Assert.NotEmpty(result.DIDOrder);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        //[Fact]
        //public async Task FirstComGetPhoneNumbersTestAsync()
        //{
        //    var results = await FirstCom.NpaNxxFirstPointCom.GetAsync("206", string.Empty, string.Empty, pComNetCredentials.Username, pComNetCredentials.Password);

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
        public async Task FirstComGetRoutingTestAsync()
        {
            var result = await FirstCom.Models.FirstPointCom.GetSMSRoutingByDialedNumberAsync("12069574634".AsMemory(), pComNetCredentials.Username.AsMemory(), pComNetCredentials.Password.AsMemory());

            Assert.True(result.QueryResult.text is "OK");
            Assert.True(result.QueryResult.code is 0);
            Assert.True(result.epid is 265);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task FirstComGetSMSEnableTestAsync()
        {
            var result = await FirstCom.Models.FirstPointCom.EnableSMSByDialedNumberAsync("12069574634".AsMemory(), pComNetCredentials.Username.AsMemory(), pComNetCredentials.Password.AsMemory());

            Assert.True(result.code is not 0);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task FirstComGetRouteSMSToEPIDTestAsync()
        {
            var result = await FirstCom.Models.FirstPointCom.RouteSMSToEPIDByDialedNumberAsync("12069574634".AsMemory(), 265, pComNetCredentials.Username.AsMemory(), pComNetCredentials.Password.AsMemory());

            Assert.True(result.text is "Routed OK!");
            Assert.True(result.code is 0);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSRESTNpaNxxGetAsyncTestAsync()
        {
            // Arrange
            var npa = 206;

            // Act
            var results = await OrderTn.GetAsync(npa, bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

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

        //[Fact]
        //public async Task BulkVSRESTOrderPostAsyncTestAsync()
        //{
        //    // Arrange
        //    var order = new OrderTnRequestBody
        //    {
        //        TN = "14255475245",
        //        Lidb = "Accelerate Networks",
        //        PortoutPin = "3591344",
        //        TrunkGroup = "Primary",
        //        Sms = true,
        //        Mms = false
        //    };

        //    // Act
        //    var results = await order.PostAsync(bulkVSUsername, bulkVSPassword);

        //    // Assert
        //    Assert.NotNull(results);
        //    output.WriteLine(JsonSerializer.Serialize(results));
        //}


        [Fact]
        public async Task BulkVSCnameLookupAsync()
        {
            // Arrange
            var number = "2064083008";

            // Act
            var result = await CnamBulkVs.GetAsync(number.AsMemory(), bulkVSKey.AsMemory());

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result.number));
            Assert.False(string.IsNullOrWhiteSpace(result.name));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSLrnLookupAsync()
        {
            // Arrange
            var number = "4252008183";

            // Act
            var result = await LrnBulkCnam.GetAsync(number.AsMemory(), bulkVSKey.AsMemory());

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result.spid));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSLrnLookupTollfreeAsync()
        {
            // Arrange
            var number = "8662122226";

            // Act
            var result = await LrnBulkCnam.GetAsync(number.AsMemory(), bulkVSKey.AsMemory());

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result.jurisdiction));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task NumberLookupPostAsync()
        {
            // Arrange
            var number = "2064083008";

            // Act
            var result = await LrnBulkCnam.GetAsync(number.AsMemory(), bulkVSKey.AsMemory());

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result.tn));
            Assert.False(string.IsNullOrWhiteSpace(result.spid));
            output.WriteLine(JsonSerializer.Serialize(result));

            result = result with { LIDBName = "IntegrationTest" };

            var lookup = new PhoneNumberLookup(result);
            var checkPost = await lookup.PostAsync(postgresql);

            Assert.True(checkPost);
        }

        [Fact]
        public async Task GetNumberLookupAsync()
        {
            // Arrange
            var number = "12067696361";

            // Act
            var result = await PhoneNumberLookup.GetByDialedNumberAsync(number, postgresql);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.SPID));
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task BulkVSRESTGetAllOwnedNumbersAsync()
        {
            // Arrange

            // Act
            var results = await TnRecord.GetAsync(bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

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
            var results = await ValidatePortability.GetAsync(portedNumber.AsMemory(), bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.True(results.Portable);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task TeleDynamicsProductCheckQuantityAsync()
        {
            // Arrange
            var partNumber = "yea-sip-t54w";
            // Act
            var results = await VendorProduct.GetAsync(partNumber, _teleDynamicsUsername, _teleDynamicsPassword);

            // Assert
            Assert.NotNull(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task BulkVSRESTGetAllOwnedNumbersAsOwnedAsync()
        {
            // Arrange

            // Act
            var results = await TnRecord.GetOwnedAsync(bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results.FirstOrDefault()));
            output.WriteLine($"{results.Length} Owned Numbers from BulkVS");
        }

        [Fact]
        public async Task BulkVSRESTGetAllPortRequestsAsync()
        {
            // Arrange

            // Act
            var results = await PortTn.GetAllAsync(bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task BulkVSRESTValidateAddressAsync()
        {
            // Arrange
            // Act
            var results = await E911Record.ValidateAddressAsync("", "", "", "", "", "", bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(results.Status));
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task BulkVSRESTGetExistingRecordAsync()
        {
            // Arrange
            // Act
            var results = await E911Record.GetAsync("14257787400".AsMemory(), bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.NotNull(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task BulkVSRESTProvisionAsync()
        {
            // Arrange
            // Act
            var results = await E911Record.PostAsync("", "", "", [], bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(results.Status));
            output.WriteLine(JsonSerializer.Serialize(results));
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
            var results = await PortTn.GetAllAsync(bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var result = await PortTn.GetAsync("1642300".AsMemory(), bulkVSUsername.AsMemory(), bulkVSPassword.AsMemory());

            Assert.NotEmpty(result.TNList);
            foreach (var item in result.TNList)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.LNPStatus));
                Assert.False(string.IsNullOrWhiteSpace(item.RDD));
                Assert.False(string.IsNullOrWhiteSpace(item.TN));
            }
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        //[Fact]
        //public async Task Call48LoginTestAsync()
        //{
        //    // Act
        //    var result = await Login.LoginAsync(_call48Username, _call48Password);

        //    Assert.NotNull(result);
        //    Assert.NotNull(result.data.token);
        //    Assert.True(result.code == 200);
        //    output.WriteLine(JsonSerializer.Serialize(result));
        //}

        //[Fact]
        //public async Task Call48LocalNumberLookupTestAsync()
        //{
        //    // Act
        //    var result = await Login.LoginAsync(_call48Username, _call48Password);

        //    var results = await Search.GetLocalNumbersAsync("WA", string.Empty, "206", string.Empty, result.data.token);

        //    if (results is null || !results.data.result.Any())
        //    {
        //        results = await Search.GetLocalNumbersAsync("WA", string.Empty, "425", string.Empty, result.data.token);
        //    }

        //    Assert.NotEmpty(results.data.result);
        //    output.WriteLine(results.data.result.Length.ToString());
        //    foreach (var number in results.data.result)
        //    {
        //        Assert.False(string.IsNullOrWhiteSpace(number.number));
        //        Assert.True(number.state == "WA");
        //        Assert.False(string.IsNullOrWhiteSpace(number.ratecenter));
        //        output.WriteLine(JsonSerializer.Serialize(number));
        //    }

        //}

        //[Fact]
        //public async Task Call48GetNumbersTestAsync()
        //{
        //    // Act
        //    var cred = await Login.LoginAsync(_call48Username, _call48Password);

        //    var results = await Search.GetAsync("OR", 541, cred.data.token);

        //    if (results is null || !results.Any())
        //    {
        //        results = await Search.GetAsync("WA", 425, cred.data.token);
        //    }

        //    Assert.NotEmpty(results);
        //    output.WriteLine(results.Count().ToString());
        //    foreach (var result in results)
        //    {
        //        Assert.True(result.NPA > 99);
        //        Assert.True(result.NXX > 99);
        //        // XXXX can be 0001 which as an int is 1.
        //        Assert.True(result.XXXX > 0);
        //        Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
        //        // Reenabled these after June 2020 starts.
        //        //Assert.False(string.IsNullOrWhiteSpace(result.City));
        //        //Assert.False(string.IsNullOrWhiteSpace(result.State));
        //        Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
        //        output.WriteLine(JsonSerializer.Serialize(result));
        //    }
        //}

        //[Fact]
        //public async Task Call48GetRatecentersTestAsync()
        //{
        //    // Act
        //    var cred = await Login.LoginAsync(_call48Username, _call48Password);

        //    var results = await Ratecenter.GetAllRatecentersAsync(PhoneNumbersNA.AreaCode.States.ToArray(), cred.data.token);

        //    Assert.NotEmpty(results);
        //    output.WriteLine(results.Length.ToString());
        //    foreach (var result in results)
        //    {
        //        Assert.NotEmpty(result.Ratecenters);
        //        output.WriteLine(JsonSerializer.Serialize(result));
        //    }

        //}

        // Disabled so as not to purchase a bunch of numbers by accident.
        //[Fact]
        //public async Task Call48PurchaseLocalNumberTestAsync()
        //{
        //    // Act
        //    var cred = await Login.LoginAsync(_call48Username, _call48Password);

        //    var results = await Search.GetAsync("WA", 206, cred.data.token);

        //    Assert.NotEmpty(results);
        //    output.WriteLine(results.Count().ToString());
        //    var number = results.FirstOrDefault();

        //    var checkExist = await Search.GetLocalNumbersAsync(string.Empty, number.State, number.NPA.ToString(), number.NXX.ToString(), cred.data.token);

        //    var numberToPurchase = checkExist.data.result.Where(x => x.did.Replace("-", string.Empty) == number.DialedNumber).FirstOrDefault();
        //    output.WriteLine(JsonSerializer.Serialize(numberToPurchase));

        //    var purchaseOrder = await Purchase.PurchasePhoneNumberAsync(checkExist.data.loc, numberToPurchase, cred.data.token);
        //    output.WriteLine(JsonSerializer.Serialize(purchaseOrder));

        //    Assert.NotNull(purchaseOrder);
        //    Assert.True(purchaseOrder.code == 200);
        //}

        [Fact]
        public async Task GetCarrierByOCNAsync()
        {
            // Arrange
            var ocn = "4324";

            // Act
            var results = await Carrier.GetByOCNAsync(ocn, postgresql);

            Assert.NotNull(results);
            Assert.True(results.Ocn == ocn);
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
            var results = await PhoneNumber.SearchAsync("206", conn);
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
            var results = await PhoneNumber.DeleteOld(DateTime.Now.AddDays(-3), conn);
            Assert.NotNull(results);
            output.WriteLine($"{results.Removed} Numbers Removed.");
        }

        [Fact]
        public async Task DeleteLogsAsync()
        {
            var conn = postgresql;
            var results = await Logs.DeleteOld(DateTime.Now, conn);
            Assert.NotNull(results);
            output.WriteLine($"{results.Removed} log entries removed.");
        }

        [Fact]
        public async Task DeleteOldPhoneNumbersByProviderAsync()
        {
            var conn = postgresql;
            var cycle = DateTime.Now.AddHours(1) - DateTime.Now;
            var provider = "Test";
            var results = await PhoneNumber.DeleteOldByProvider(DateTime.Now, cycle, provider, conn);
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

            var existing = await PhoneNumber.GetAsync(number.DialedNumber, postgresql);

            if (existing is not null && existing?.DialedNumber?.Length == 10)
            {
                _ = existing.DeleteAsync(postgresql);
            }

            var response = await number.PostAsync(conn);
            Assert.True(response);

            // Clean up.
            // We need the Guid so we have to get a copy of the new record from the DB before we can delete it.
            var fromDb = await PhoneNumber.GetAsync(number.DialedNumber, conn);

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

            var result = await cycle.PostAsync(postgresql);
            Assert.True(result);

            var results = await IngestCycle.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var test = results.Where(x => x.IngestedFrom == cycle.IngestedFrom).FirstOrDefault();
            Assert.NotNull(test);

            test.CycleTime = DateTime.Now.AddHours(1) - DateTime.Now;
            var checkUpdate = await test.PutAsync(postgresql);
            Assert.True(checkUpdate);

            results = await IngestCycle.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var update = results.Where(x => x.IngestedFrom == cycle.IngestedFrom).FirstOrDefault();
            Assert.NotNull(update);
            //Assert.Equal(test.CycleTime, update.CycleTime);

            var checkDelete = await update.DeleteAsync(postgresql);
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
            var fromDb = await IngestStatistics.GetLastIngestAsync(stats.IngestedFrom, conn);

            var checkDelete = await fromDb.DeleteAsync(conn);
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
        public async Task GetCountAllPhoneNumbersAsync()
        {
            var result = await PhoneNumber.GetCountAllProvider(postgresql);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            foreach (var item in result)
            {
                output.WriteLine(JsonSerializer.Serialize(item));
            }
        }

        //[Fact]
        //public async Task GetPhoneNumbersByAreaCodeAsync()
        //{
        //    var result = await PhoneNumber.GetAllByAreaCodeAsync(206, postgresql);

        //    Assert.True(result.Any());
        //    output.WriteLine(result.Count().ToString());
        //}

        [Fact]
        public async Task GetVerifiedNumbersAsync()
        {
            var results = await VerifiedPhoneNumber.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            results = await VerifiedPhoneNumber.GetByOrderIdAsync(results.FirstOrDefault().OrderId ?? Guid.Empty, postgresql);
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

            var checkPost = await testNumber.PostAsync(postgresql);
            Assert.True(checkPost);

            var checkSave = await VerifiedPhoneNumber.GetByOrderIdAsync(testNumber.OrderId ?? Guid.Empty, postgresql);
            Assert.True(checkSave.FirstOrDefault().VerifiedDialedNumber == testNumber.VerifiedDialedNumber);

            testNumber.LocalAccessTransportArea = "testtest";
            var checkPut = await testNumber.PutAsync(postgresql);
            Assert.True(checkPut);

            checkSave = await VerifiedPhoneNumber.GetByOrderIdAsync(testNumber.OrderId ?? Guid.Empty, postgresql);
            Assert.True(checkSave.FirstOrDefault().LocalAccessTransportArea == testNumber.LocalAccessTransportArea);

            var checkDelete = await testNumber.DeleteAsync(postgresql);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetAllPortedPhoneNumbersAsync()
        {
            var results = await PortedPhoneNumber.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results.LastOrDefault()));

            var order = results.LastOrDefault();
            results = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId ?? Guid.Empty, postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            //results = await PortedPhoneNumber.GetByPortRequestIdAsync(order.PortRequestId ?? Guid.Empty, postgresql);
            //Assert.NotNull(results);
            //Assert.NotEmpty(results);
            //output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));

            results = await PortedPhoneNumber.GetByDialedNumberAsync(order.PortedDialedNumber, postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            results = await PortedPhoneNumber.GetByExternalIdAsync(order.ExternalPortRequestId, postgresql);
            Assert.NotNull(results);
            //Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));

            var result = await PortedPhoneNumber.GetByIdAsync(order.PortedPhoneNumberId, postgresql);
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

            var checkPost = await ported.PostAsync(postgresql);
            Assert.True(checkPost);

            var verifyPost = await PortedPhoneNumber.GetByOrderIdAsync(ported.OrderId ?? Guid.Empty, postgresql);
            Assert.NotNull(verifyPost);
            Assert.NotEmpty(verifyPost);
            var verified = verifyPost.FirstOrDefault();
            Assert.True(ported.OrderId == verified.OrderId);

            verified.ExternalPortRequestId = "testtest";
            var checkPut = await verified.PutAsync(postgresql);
            Assert.True(checkPut);

            var verifyPut = await PortedPhoneNumber.GetByIdAsync(verified.PortedPhoneNumberId, postgresql);
            Assert.NotNull(verifyPut);
            Assert.True(verifyPut.ExternalPortRequestId == verified.ExternalPortRequestId);

            var checkDelete = await verifyPut.DeleteAsync(postgresql);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetAllPortRequestsAsync()
        {
            var results = await PortRequest.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            output.WriteLine(JsonSerializer.Serialize(results));
        }

        [Fact]
        public async Task GetPortRequestByOrderIdAsync()
        {
            var results = await PortRequest.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var order = await PortRequest.GetByOrderIdAsync(results.FirstOrDefault().OrderId, postgresql);
            output.WriteLine(JsonSerializer.Serialize(order));
        }

        [Fact]
        public async Task GetPostPutDeletePortRequestByOrderIdAsync()
        {
            var results = await PortRequest.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var portRequest = await PortRequest.GetByOrderIdAsync(results.FirstOrDefault().OrderId, postgresql);
            output.WriteLine(JsonSerializer.Serialize(portRequest));
            portRequest.PortRequestId = Guid.NewGuid();
            var checkCreate = await portRequest.PostAsync(postgresql);
            Assert.True(checkCreate);
            portRequest.ProviderPIN = "1234";
            var checkUpdate = await portRequest.PutAsync(postgresql);
            Assert.True(checkUpdate);
            var checkDelete = await portRequest.DeleteByIdAsync(postgresql);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetPostPutDeleteCouponsAsync()
        {
            var results = await Coupon.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var result = await Coupon.GetByIdAsync(results.FirstOrDefault().CouponId, postgresql);
            Assert.NotNull(result);
            Assert.True(results?.FirstOrDefault()?.CouponId == result?.CouponId);
            result.CouponId = Guid.NewGuid();
            result.Name = "Test";
            result.Description = "Test";
            result.Public = false;
            var checkCreate = await result.PostAsync(postgresql);
            Assert.True(checkCreate);
            result = await Coupon.GetByIdAsync(result.CouponId, postgresql);
            Assert.NotNull(result);
            result.Name = "Test2";
            var checkUpdate = await result.PutAsync(postgresql);
            Assert.True(checkUpdate);
            var checkDelete = await result.DeleteAsync(postgresql);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetAllSentEmailsAsync()
        {
            var results = await Email.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task GetSentEmailByIdAsync()
        {
            var results = await Email.GetAllAsync(postgresql);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var email = results.FirstOrDefault();

            var result = await Email.GetAsync(email.EmailId, postgresql);
            Assert.NotNull(result);
            Assert.Equal(email.EmailId, result.EmailId);
        }

        //[Fact]
        //public async Task TestSendEmailAsync()
        //{
        //    var email = new Email()
        //    {
        //        EmailId = Guid.NewGuid(),
        //        PrimaryEmailAddress = "dan@acceleratenetworks.com",
        //        SalesEmailAddress = "dan@acceleratenetworks.com",
        //        CarbonCopy = "thomas.ryan@outlook.com",
        //        Subject = $"Integration Test Email {DateTime.Now.ToShortTimeString()}",
        //        MessageBody = "This is a test email send from the NumberSearch test suite. Please ignore, thanks!",
        //        DateSent = DateTime.UtcNow,
        //        Completed = false,
        //        DoNotSend = false,
        //    };
        //    var check = await email.SendEmailAsync(_configuration.SmtpUsername, _configuration.SmtpPassword);
        //    Assert.True(check);
        //}


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
                OrderId = Guid.NewGuid(),

            };

            var checkSubmit = await email.PostAsync(postgresql);
            Assert.True(checkSubmit);

            // Clean up
            var fromDb = await Email.GetByOrderAsync(email.OrderId, postgresql);
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

                var checkUpdate = await item.PutAsync(postgresql);
                Assert.True(checkUpdate);

                var updatedDb = await Email.GetAsync(item.EmailId, postgresql);
                Assert.NotNull(updatedDb);
                Assert.Equal(updatedDb.PrimaryEmailAddress, item.PrimaryEmailAddress);
                Assert.Equal(updatedDb.Subject, item.Subject);
                Assert.Equal(updatedDb.CarbonCopy, item.CarbonCopy);
                Assert.Equal(updatedDb.MessageBody, item.MessageBody);
                Assert.Equal(updatedDb.Completed, item.Completed);
                Assert.True(updatedDb.Completed);

                var checkDelete = await updatedDb.DeleteAsync(postgresql);
                Assert.True(checkDelete);
            }
        }

        [Fact]
        public async Task GetOrderAsync()
        {
            var conn = postgresql;

            var results = await Order.GetAllAsync(conn);

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            foreach (var result in results)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.OrderId.ToString()));
                Assert.False(string.IsNullOrWhiteSpace(result.FirstName));
                //Assert.False(string.IsNullOrWhiteSpace(result.LastName));
                //Assert.False(string.IsNullOrWhiteSpace(result.Address));
                //Assert.False(string.IsNullOrWhiteSpace(result.City));
                //Assert.False(string.IsNullOrWhiteSpace(result.State));
                //Assert.False(string.IsNullOrWhiteSpace(result.Zip));
                Assert.False(string.IsNullOrWhiteSpace(result.Email));
                Assert.True(result.DateSubmitted > new DateTime(2019, 1, 1));
            }

            var quotes = await Order.GetAllQuotesAsync(conn);

            Assert.NotNull(quotes);
            Assert.NotEmpty(quotes);
        }


        [Fact]
        public async Task GetOrderByBackGroundworkNotCompletedAsync()
        {
            var conn = postgresql;

            var results = await Order.GetByBackGroundworkNotCompletedAsync(conn);

            Assert.NotNull(results);
        }

        [Fact]
        public async Task GetRateAsync()
        {
            string address = "6300 linderson way";
            string city = string.Empty;
            string zip = "98501";

            var result = await DataAccess.TaxRate.GetSalesTaxAsync(address.AsMemory(), city.AsMemory(), zip.AsMemory());

            Assert.True(result.rate > 0.0m);
            output.WriteLine(JsonSerializer.Serialize(result));
        }

        [Fact]
        public async Task PostOrderAsync()
        {
            var conn = postgresql;

            var orders = await Order.GetAllAsync(conn);

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

            var checkDelete = await fromDb.DeleteAsync(conn);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task PostProductOrderByProductIdAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                ProductOrderId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };

            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDb = await ProductOrder.GetAsync(itemToOrder.OrderId, conn);
            Assert.NotNull(fromDb);
            Assert.NotEmpty(fromDb);

            var checkDelete = await fromDb.FirstOrDefault().DeleteByOrderAsync(conn);
            Assert.True(checkDelete);
        }


        [Fact]
        public async Task PostProductOrderByDialedNumberAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                ProductOrderId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                DialedNumber = "8605530426",
                Quantity = 1
            };

            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDb = await ProductOrder.GetAsync(itemToOrder.OrderId, conn);
            Assert.NotNull(fromDb);
            Assert.NotEmpty(fromDb);

            var checkDelete = await fromDb.FirstOrDefault().DeleteByOrderAsync(conn);
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
            var fromDb = await PurchasedPhoneNumber.GetByDialedNumberAndOrderIdAsync(itemToOrder.DialedNumber, itemToOrder.OrderId, conn);
            Assert.NotNull(fromDb);

            var checkDelete = await fromDb.DeleteAsync(conn);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task GetEmergencyInfoAsync()
        {
            var conn = postgresql;

            var results = await EmergencyInformation.GetAllAsync(conn);
            Assert.NotNull(results);
            Assert.NotEmpty(results);

            var result = await EmergencyInformation.GetByDialedNumberAsync(results.FirstOrDefault().DialedNumber, conn);
            Assert.NotNull(result);
            Assert.NotEmpty(results);
            Assert.Equal(results.FirstOrDefault().DialedNumber, result.FirstOrDefault().DialedNumber);
        }

        [Fact]
        public async Task PostPutDeleteEmergencyInfoAsync()
        {
            var conn = postgresql;

            var info = new EmergencyInformation
            {
                EmergencyInformationId = Guid.NewGuid(),
                AddressLine1 = "123 Sesame Street",
                City = "Seattle",
                BulkVSLastModificationDate = DateTime.Now,
                DateIngested = DateTime.Now,
                DialedNumber = "1231231234",
                IngestedFrom = "IntegrationTest",
                ModifiedDate = DateTime.Now,
                State = "WA",
                Zip = "99999",
                RawResponse = "Test is a test.",
                Sms = string.Empty,
                AddressLine2 = string.Empty,
                CallerName = "Test",
            };

            var result = await info.PostAsync(conn);
            Assert.True(result);

            // Clean up.
            var fromDbResult = await EmergencyInformation.GetByIdAsync(info.EmergencyInformationId, conn);
            Assert.NotNull(fromDbResult);
            Assert.True(info.IngestedFrom == fromDbResult.IngestedFrom);
            fromDbResult.CallerName = "Test2";
            var checkUpdate = await fromDbResult.PutAsync(conn);
            Assert.True(checkUpdate);
            var checkDelete = await fromDbResult.DeleteAsync(conn);
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
                ProductOrderId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };


            var result = await itemToOrder.PostAsync(conn);
            Assert.True(result);

            // Act
            var results = await ProductOrder.GetAsync(itemToOrder.OrderId, conn);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            foreach (var order in results)
            {
                Assert.Equal(order.OrderId, itemToOrder.OrderId);
            }

            // Clean up.
            var checkDelete = await results.FirstOrDefault().DeleteByOrderAsync(conn);
            Assert.True(checkDelete);
        }

        [Fact]
        public async Task OwnedPhoneNumberCRUDAsync()
        {
            // Arrange
            var conn = postgresql;
            var ownedPhoneNumber = new OwnedPhoneNumber
            {
                DialedNumber = "1115530426",
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
            var checkCreate = await ownedPhoneNumber.PostAsync(postgresql);

            Assert.True(checkCreate);

            var fromDb = await OwnedPhoneNumber.GetByDialedNumberAsync(ownedPhoneNumber.DialedNumber, conn);
            Assert.NotNull(fromDb);
            fromDb.Notes = "IntegrationTest";

            var checkUpdate = await fromDb.PutAsync(postgresql);
            Assert.True(checkUpdate);

            var fromDbAgain = await OwnedPhoneNumber.GetByDialedNumberAsync(ownedPhoneNumber.DialedNumber, conn);

            // Assert
            Assert.NotNull(fromDb);
            Assert.Equal(fromDb.IngestedFrom, fromDbAgain.IngestedFrom);
            Assert.Equal(fromDb.Notes, fromDbAgain.Notes);
            Assert.Equal(fromDb.OwnedPhoneNumberId, fromDbAgain.OwnedPhoneNumberId);

            // Clean up
            var checkDelete = await fromDbAgain.DeleteAsync(postgresql);

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

            var checkLock = await lockingStats.PostAsync(conn);

            Assert.True(checkLock);

            var results = await IngestStatistics.GetLockAsync("Test", conn);

            Assert.NotNull(results);

            var checkRemoveLock = await results.DeleteAsync(conn);

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

        [Fact]
        public async Task GetBillingTaxRateForOrder()
        {
            // Valid
            var order = new Order()
            {
                State = "WA",
                Address = "4415 31st Ave W",
                City = "Seattle",
                Zip = "98199"
            };

            var result = await CartController.GetBillingTaxRateAsync(order, _configuration.InvoiceNinjaToken.AsMemory());

            Assert.False(string.IsNullOrWhiteSpace(result.name));
            Assert.True(result.rate > 0);

            // Outofstate
            order = new Order()
            {
                State = "OR",
                Address = "1710 S Mill St Cir",
                City = "Portland",
                Zip = "97201"
            };

            result = await CartController.GetBillingTaxRateAsync(order, _configuration.InvoiceNinjaToken.AsMemory());

            Assert.False(string.IsNullOrWhiteSpace(result.name));
            Assert.Equal("None", result.name);
            Assert.True(result.rate is 0);

            //Invalid
            order = new Order()
            {
                State = "WA",
                Address = "Abc 123 u and me",
                City = "Seattle",
                Zip = "98199"
            };

            result = await CartController.GetBillingTaxRateAsync(order, _configuration.InvoiceNinjaToken.AsMemory());

            Assert.False(string.IsNullOrWhiteSpace(result.name));
            Assert.Equal("None", result.name);
            Assert.True(result.rate is 0);
        }

        [Fact]
        public async Task GetBillingClientForOrder()
        {
            // Valid
            var order = new Order()
            {
                Email = "dan@acceleratenetworks.com"
            };

            var result = await CartController.GetBillingClientForOrderAsync(order, _configuration.InvoiceNinjaToken.AsMemory());

            Assert.False(string.IsNullOrWhiteSpace(result.id));
            Assert.False(string.IsNullOrWhiteSpace(result.contacts.FirstOrDefault().email));

            // Skipping create because we don't want to pollute the DB.
        }

        [Fact]
        public async Task VerifyEmailForOrder()
        {
            var result = await CartController.VerifyEmailByAddressAsync("dan@acceleratenetworks.com".AsMemory());
            Assert.True(result.MxRecordExists);
            Assert.False(string.IsNullOrWhiteSpace(result.EmailDomain.Host));

            result = await CartController.VerifyEmailByAddressAsync("test@notavaliddomainlol.com".AsMemory());
            Assert.False(result.MxRecordExists);
            Assert.False(string.IsNullOrWhiteSpace(result.EmailDomain.Host));
        }

        [Fact]
        public void ParseAddressForOrder()
        {
            var order = new Order()
            {
                UnparsedAddress = "4415 31st Ave W, Seattle, WA, 98199, USA",
            };
            var result = CartController.ParseAddress(ref order);

            Assert.False(string.IsNullOrWhiteSpace(result.Address));
            Assert.True(string.IsNullOrWhiteSpace(result.Address2));
            Assert.False(string.IsNullOrWhiteSpace(result.City));
            Assert.False(string.IsNullOrWhiteSpace(result.State));
            Assert.False(string.IsNullOrWhiteSpace(result.Zip));
            Assert.False(string.IsNullOrWhiteSpace(result.UnparsedAddress));
        }

        [Fact]
        public async Task GetInvoiceItemsTest()
        {
            var order = new Order() { OrderId = Guid.NewGuid() };
            var cart = new Cart();
            List<Line_Items> onetimeItems = [];
            List<Line_Items> reoccuringItems = [];
            List<PurchasedPhoneNumber> purchasedPhoneNumbers = [];
            int pin = CartController.GetPortingPIN();

            // Add purchased numbers
            var matchingNumber = await PhoneNumber.GetAllNumbersAsync(postgresql);
            var number = matchingNumber.FirstOrDefault();
            var phoneNumber = await PhoneNumber.GetAsync(number, postgresql);
            var productOrderPurchased = new ProductOrder { ProductOrderId = Guid.NewGuid(), DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

            cart.AddPhoneNumber(ref phoneNumber, ref productOrderPurchased);

            // Add ported numbers
            var portedPhoneNumber = new PortedPhoneNumber
            {
                PortedPhoneNumberId = Guid.NewGuid(),
                PortedDialedNumber = "2068588757",
                NPA = 206,
                NXX = 858,
                XXXX = 8757,
                City = "Seattle",
                State = "WA",
                DateIngested = DateTime.Now,
                IngestedFrom = "UserInput",
                Wireless = false
            };

            var productOrderPort = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = portedPhoneNumber.PortedDialedNumber, PortedPhoneNumberId = portedPhoneNumber?.PortedPhoneNumberId, Quantity = 1 };

            cart.AddPortedPhoneNumber(ref portedPhoneNumber, ref productOrderPort);

            var products = await Product.GetAllAsync(postgresql);

            foreach (var item in products)
            {
                var localItem = item;
                var productOrder = new ProductOrder()
                {
                    ProductOrderId = Guid.NewGuid(),
                    ProductId = localItem.ProductId,
                    Quantity = 1
                };
                cart.AddProduct(ref localItem, ref productOrder);
            }

            var services = await Service.GetAllAsync(postgresql);

            foreach (var service in services)
            {
                var local = service;
                var productOrder = new ProductOrder
                {
                    ProductOrderId = Guid.NewGuid(),
                    ServiceId = service.ServiceId,
                    Quantity = 1
                };
                cart.AddService(ref local, ref productOrder);
            }

            var coupons = await Coupon.GetAllAsync(postgresql);

            foreach (var coupon in coupons)
            {
                var local = coupon;
                var productOrder = new ProductOrder
                {
                    ProductOrderId = Guid.NewGuid(),
                    CouponId = coupon.CouponId,
                    Quantity = 1
                };
                cart.AddCoupon(ref local, ref productOrder);
            }

            var result = CartController.GetInvoiceItemsFromProductOrders(ref order, ref cart, ref onetimeItems, ref reoccuringItems, ref pin, ref purchasedPhoneNumbers);
            Assert.True(result.TotalCost > 0);
            Assert.True(result.TotalNumberPurchasingCost > 0);
            Assert.True(result.TotalPortingCost > 0);
            Assert.False(string.IsNullOrWhiteSpace(result.EmailSubject));
        }
    }
}