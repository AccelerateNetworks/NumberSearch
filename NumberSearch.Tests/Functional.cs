using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using NumberSearch.Mvc;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Serilog;

namespace NumberSearch.Tests
{
    public class Functional : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly Guid token;
        private readonly ITestOutputHelper output;
        private readonly IConfiguration configuration;
        protected readonly HttpClient _client;

        public Functional(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();

            configuration = config;

            token = Guid.Parse(config.GetConnectionString("TeleAPI"));

            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Services")]
        [InlineData("/Hardware")]
        [InlineData("/Search")]
        [InlineData("/About")]
        [InlineData("/Features")]
        [InlineData("/Support")]
        [InlineData("/Lookup")]
        [InlineData("/Privacy")]
        [InlineData("/Cart")]
        public async Task GetDynamicPagesAsync(string url)
        {
            // Arrange
            var response = await _client.GetAsync(url);

            // Act
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Seattle", stringResponse);
            output.WriteLine(stringResponse);
        }
    }
}
