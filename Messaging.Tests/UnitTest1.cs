using Microsoft.AspNetCore.Mvc.Testing;

namespace Messaging.Tests
{
    public class Functional : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _httpClient;
        public Functional(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _httpClient = factory.CreateClient();
        }

        [Fact]
        public async Task CorrectlyFormattedButInvalidMessage()
        {
            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "Your Lyft code is 12345"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers. {\\\"origtime\\\":\\\"2022-04-17 03:48:00\\\",\\\"msisdn\\\":\\\"15555551212\\\",\\\"to\\\":\\\"\\\",\\\"sessionid\\\":\\\"tLMOYTAmIFiQvBE6X1g\\\",\\\"timezone\\\":\\\"EST\\\",\\\"message\\\":\\\"Your Lyft code is 12345\\\",\\\"api_version\\\":0,\\\"serversecret\\\":\\\"sekrethere\\\"}\"", message);
        }

        [Fact]
        public async Task BadToken()
        {
            string route = "/api/inbound/1pcom";
            string token = "thisIsNotAValidToken";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "Your Lyft code is 12345"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.Unauthorized);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("", message);
        }

        [Fact]
        public async Task IncompleteRequest()
        {
            string route = "/api/inbound/1pcom";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(string.Empty, string.Empty),
                });

            var response = await _httpClient.PostAsync($"{route}", stringContent);

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CorrectlyFormattedMessageWithBadCallbackUrl()
        {
            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589312"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "Hello, this is 1stPoint SMS :D"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"Failed to forward the message to the client's callback url.\"", message);
        }
    }
}