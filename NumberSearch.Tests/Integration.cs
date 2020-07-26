using BulkVS;

using FirstCom;

using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Peerless;
using NumberSearch.DataAccess.TeleMesssage;

using ServiceReference;

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
        private readonly string peerlessAPIKey;

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
            peerlessAPIKey = config.GetConnectionString("PeerlessAPIKey");
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

        [Fact]
        public async Task TeleNXXsTestAsync()
        {
            // Arrange
            string npa = "206";

            // Act
            var results = await DidsNxxs.GetAsync(npa, token);

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
            var results = await DidsList.GetRawAsync(query, token);

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
        public async Task TelePhoneNumbersTestAsync()
        {
            // Arrange
            var query = "206";

            // Act
            var results = await DidsList.GetAsync(query, token);

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
            var results = await PhoneNumber.PaginatedSearchAsync("*", 1, conn);
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
        public async Task DeleteOldPhoneNumberAsync()
        {
            var conn = postgresql;
            var results = await PhoneNumber.DeleteOld(DateTime.Now, conn).ConfigureAwait(false);
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

        [Fact]
        public async Task PostOrderAsync()
        {
            var conn = postgresql;

            var orders = await Order.GetAllAsync(conn).ConfigureAwait(false);

            var selectedOrders = orders.Where(x => (x.OrderId != null) && (x.OrderId != Guid.Empty)).FirstOrDefault();

            Assert.False(selectedOrders is null);

            var result = await Order.GetByIdAsync(selectedOrders.OrderId, conn);

            var response = await result.PostAsync(conn);

            Assert.True(response);
        }

        [Fact]
        public async Task PostProductOrderByProductIdAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                OrderId = new Guid("799cc220-5931-46d8-9a21-03ce523e8ec2"),
                ProductId = new Guid("799cc220-5931-46d8-9a21-03ce523e8ec3"),
                Quantity = 1
            };


            var result = await itemToOrder.PostAsync(conn);

            Assert.True(result);
        }


        [Fact]
        public async Task PostProductOrderByDialedNumberAsync()
        {
            var conn = postgresql;

            var itemToOrder = new ProductOrder
            {
                OrderId = new Guid("799cc220-5931-46d8-9a21-03ce523e8ec2"),
                DialedNumber = "8605530426",
                Quantity = 1
            };

            var result = await itemToOrder.PostAsync(conn);

            Assert.True(result);
        }

        [Fact]
        public async Task PostPurchasedPhoneNumberAsync()
        {
            var conn = postgresql;

            var itemToOrder = new PurchasedPhoneNumber
            {
                OrderId = new Guid("799cc220-5931-46d8-9a21-03ce523e8ec2"),
                DialedNumber = "8605530426",
                DateIngested = DateTime.Now.AddDays(-1),
                DateOrdered = DateTime.Now,
                IngestedFrom = "Test",
                OrderResponse = "\"code\":200,",
                Completed = true
            };

            var result = await itemToOrder.PostAsync(conn);

            Assert.True(result);
        }

        [Fact]
        public async Task GetProductOrderAsync()
        {
            var conn = postgresql;

            var results = await ProductOrder.GetAsync(new Guid("799cc220-5931-46d8-9a21-03ce523e8ec2"), conn);

            Assert.NotNull(results);
            Assert.NotEmpty(results);
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