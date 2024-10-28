using Microsoft.AspNetCore.Mvc.Testing;

using NumberSearch.Mvc;

using System.Net.Http;

using Xunit;

namespace NumberSearch.Tests
{
    public class Functional : IClassFixture<WebApplicationFactory<Startup>>
    {
        //private readonly Guid token;
        //private readonly ITestOutputHelper output;
        //private readonly IConfiguration configuration;
        protected readonly HttpClient _client;

        // Disabled because while this works fine locally, it breaks the pipeline.
        //public Functional(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        //{
        //    this.output = output;

        //    var config = new ConfigurationBuilder()
        //        .AddJsonFile("appsettings.json")
        //        .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
        //        .Build();

        //    configuration = config;

        //    token = Guid.Parse(config.GetConnectionString("TeleAPI"));

        //    _client = factory.CreateClient();
        //}

        //[Theory]
        //[InlineData("/")]
        //[InlineData("/Services")]
        //[InlineData("/Hardware")]
        //[InlineData("/Search")]
        //[InlineData("/About")]
        //[InlineData("/Features")]
        //[InlineData("/Support")]
        //[InlineData("/Lookup")]
        //[InlineData("/Privacy")]
        //[InlineData("/Cart")]
        //public async Task GetDynamicPagesAsync(string url)
        //{
        //    // Arrange
        //    var response = await _client.GetAsync(url);

        //    // Act
        //    response.EnsureSuccessStatusCode();
        //    var stringResponse = await response.Content.ReadAsStringAsync();

        //    // Assert
        //    Assert.Contains("Seattle", stringResponse);
        //    output.WriteLine(stringResponse);
        //}

        //[Theory]
        //[InlineData("206")]
        //[InlineData("206*")]
        //[InlineData("206***")]
        //[InlineData("206*******")]
        //public async Task GetNumberSearchQueryAsync(string query)
        //{
        //    // Arrange
        //    var response = await _client.GetAsync($"/Search?Query={query}");

        //    // Act
        //    response.EnsureSuccessStatusCode();
        //    var stringResponse = await response.Content.ReadAsStringAsync();

        //    // Assert
        //    Assert.Contains("available numbers found!", stringResponse);
        //    output.WriteLine(stringResponse);
        //}

        //[Theory]
        //[InlineData("2062974300")]
        //public async Task RedirectToPortAsync(string query)
        //{
        //    // Arrange
        //    var response = await _client.GetAsync($"/Search?Query={query}");

        //    // Act
        //    response.EnsureSuccessStatusCode();
        //    var stringResponse = await response.Content.ReadAsStringAsync();

        //    // Assert
        //    Assert.Contains("This phone number can be ported to our network!", stringResponse);
        //    output.WriteLine(stringResponse);
        //}

        //[Theory]
        //[InlineData("1111111111")]
        //public async Task NothingFoundAsync(string query)
        //{
        //    // Arrange
        //    var response = await _client.GetAsync($"/Search?Query={query}");

        //    // Act
        //    response.EnsureSuccessStatusCode();
        //    var stringResponse = await response.Content.ReadAsStringAsync();

        //    // Assert
        //    Assert.Contains("No available numbers found!", stringResponse);
        //    output.WriteLine(stringResponse);
        //}
    }
}
