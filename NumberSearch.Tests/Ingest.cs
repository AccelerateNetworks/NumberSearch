
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Ingest;

using ServiceReference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace NumberSearch.Tests
{
    public class FunctionalIngest
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

        public FunctionalIngest(ITestOutputHelper output)
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
        }

        [Fact]
        public void CategorizeNumbers()
        {
            // Arrange
            var numbers = new List<PhoneNumber> {
                new PhoneNumber { DialedNumber = "6666666666" },
                new PhoneNumber { DialedNumber = "2666666666" },
                new PhoneNumber { DialedNumber = "2166666666" },
                new PhoneNumber { DialedNumber = "3216666666" },
                new PhoneNumber { DialedNumber = "4321666666" },
                new PhoneNumber { DialedNumber = "5432166666" },
                new PhoneNumber { DialedNumber = "7543216666" },
                new PhoneNumber { DialedNumber = "8754321666" },
                new PhoneNumber { DialedNumber = "9875432166" },
            };
            // Act
            var results = Services.AssignNumberTypes(numbers);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count() > 0);
            foreach (var result in results)
            {
                Assert.False(string.IsNullOrWhiteSpace(result.NumberType));
                output.WriteLine(result.DialedNumber);
                output.WriteLine(result.NumberType);
            }
        }

        [Fact]
        public void SplitList()
        {
            // Arrange
            var list = new List<int>();
            for (var i = 0; i < 1000; i++)
            {
                list.Add(i);
            }

            // Act
            var results = Services.SplitList(list);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count() > 0);
            output.WriteLine(list.Count.ToString());
            output.WriteLine(results.Count().ToString());
            Assert.True(list.Count % results.Count() == 0);
        }

        // This takes 3 minutes to run.
        //[Fact]
        //public async Task OwnedFirstPointCom()
        //{
        //    // Arrange

        //    // Act
        //    var results = await Owned.FirstPointComAsync(pComNetCredentials.Username, pComNetCredentials.Password).ConfigureAwait(false);

        //    // Assert
        //    Assert.NotNull(results);
        //    output.WriteLine(results.Count().ToString());
        //}

        // This test is slow too.
        //[Fact]
        //public async Task OwnedTeleMessage()
        //{
        //    // Arrange

        //    // Act
        //    var results = await Owned.TeleMessageAsync(token).ConfigureAwait(false);

        //    // Assert
        //    Assert.NotNull(results);
        //    output.WriteLine(results.Count().ToString());
        //}
    }
}