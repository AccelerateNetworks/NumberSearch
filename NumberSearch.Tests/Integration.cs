using Microsoft.Extensions.Configuration;
using NumberSearch.Mvc.Models;
using System;
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

        public Integration(ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            configuration = config;

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
    }
}
