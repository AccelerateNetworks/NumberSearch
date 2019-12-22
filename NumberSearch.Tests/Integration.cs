
using FirstCom;
using Microsoft.Extensions.Configuration;
using NumberSearch.DataAccess;
using System;
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
        private readonly IConfiguration configuration;
        private readonly Credentials pComNetCredentials;
        private readonly string bulkVSKey;
        private readonly string bulkVSSecret;
        private readonly string postgresql;

        public Integration(ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("40f816f3-0a65-4523-a9be-4bbef0716720")
                .Build();

            configuration = config;

            pComNetCredentials = new Credentials
            {
                Username = config.GetConnectionString("PComNetUsername"),
                Password = config.GetConnectionString("PComNetPassword")
            };

            bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");

            token = Guid.Parse(config.GetConnectionString("TeleAPI"));

            postgresql = config.GetConnectionString("PostgresqlProd");
        }

        [Fact]
        public async Task LRNLookupTestAsync()
        {
            // Arrange
            string phoneNumber = "2065579450";

            // Act
            var result = await LRNLookup.GetAsync(phoneNumber, token);

            // Assert        
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.status));
            Assert.True(result.code == 200);
            Assert.False(string.IsNullOrWhiteSpace(result.data.lrn));
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task TeleNPAsTestAsync()
        {
            // Arrange

            // Act
            var results = await TeleNPA.GetAsync(token);

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
        public async Task TeleNXXsTestAsync()
        {
            // Arrange
            string npa = "206";

            // Act
            var results = await TeleNXX.GetAsync(npa, token);

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
        public async Task LocalNumberTestAsync()
        {
            // Arrange
            string query = "20645";

            // Act
            var results = await LocalNumberTeleMessage.GetRawAsync(query, token);

            // Assert
            Assert.NotNull(results);
            Assert.False(string.IsNullOrWhiteSpace(results.status));
            Assert.True(results.code == 200);
            foreach (var result in results.data.dids)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.number));
            }
            output.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(results));
        }

        [Fact]
        public async Task PComNetDIDInventorySearchAsyncTestAsync()
        {
            var DIDSearch = new DIDOrderQuery
            {
                DID = "12062092139",
                NPA = "206",
                NXX = "209",
                RateCenter = "SEATTLE"
            };
            var ReturnAmount = 100;

            var client = new FirstCom.DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

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
        public async Task BulkVSNpaNxxGetAsyncTest()
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

        [Fact]
        public async Task BulkVSNpaNxxGetAsyncBadInputTest()
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
        public async Task BulkVSNpaGetAsyncTest()
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
        public async Task TelePhoneNumbersTestAsync()
        {
            // Arrange
            var query = "206";

            // Act
            var results = await LocalNumberTeleMessage.GetAsync(query, token);

            // Assert
            Assert.NotNull(results);
            int count = 0;
            foreach (var result in results.ToArray())
            {
                Assert.True(result.NPA > 99);
                Assert.True(result.NXX > 99);
                Assert.True(result.XXXX > 0);
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
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
                Assert.False(string.IsNullOrWhiteSpace(result.City));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.IngestedFrom));
                count++;
            }
            output.WriteLine($"{count} Results Reviewed");
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
        public async Task PostEndOfRunStats()
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
        }

        [Fact]
        public async Task GetOrderAsync()
        {
            var conn = postgresql;

            var results = await PhoneNumberOrder.GetAsync("2062344356", conn);

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            foreach (var result in results)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.Id.ToString()));
                Assert.False(string.IsNullOrWhiteSpace(result.FirstName));
                Assert.False(string.IsNullOrWhiteSpace(result.LastName));
                Assert.False(string.IsNullOrWhiteSpace(result.Address));
                Assert.False(string.IsNullOrWhiteSpace(result.Country));
                Assert.False(string.IsNullOrWhiteSpace(result.State));
                Assert.False(string.IsNullOrWhiteSpace(result.Zip));
                Assert.False(string.IsNullOrWhiteSpace(result.DialedNumber));
                Assert.False(string.IsNullOrWhiteSpace(result.Email));
                Assert.True(result.DateSubmitted > new DateTime(2019, 1, 1));
            }
        }

        [Fact]
        public async Task PostOrderAsync()
        {
            var conn = postgresql;

            var results = await PhoneNumberOrder.GetAsync("2062344356", conn);

            var order = results.FirstOrDefault();
            var response = await order.PostAsync(conn);

            Assert.True(response);
        }
    }
}