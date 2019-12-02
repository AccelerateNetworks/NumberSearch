using DIDManagement;
using Flurl.Http;
using Flurl.Http.Xml;
using Microsoft.Extensions.Configuration;
using NumberSearch.Mvc.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
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

            token = Guid.Parse(config.GetConnectionString("TeleAPI"));
        }

        [Fact]
        public async Task LRNLookupTest()
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
        public async Task LocalNumberTest()
        {
            // Arrange
            string query = "20645";

            // Act
            var results = await LocalNumber.GetAsync(query, token);

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
        public async Task PComNetDIDInventorySearchAsyncTest()
        {
            var request = new DIDManagement.DIDInventorySearchRequest
            {
                Auth = pComNetCredentials,
                DIDSearch = new DIDManagement.DIDOrderQuery
                {
                    DID = string.Empty,
                    NPA = "202",
                    NXX = string.Empty,
                    RateCenter = string.Empty
                },
                ReturnAmount = 1000
            };

            var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(request);

            foreach (var x in result.DIDInventorySearchResult.DIDOrder)
            {
                output.WriteLine(x.DID);
            }
        }
    }
}
