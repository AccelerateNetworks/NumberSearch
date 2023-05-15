using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Models;

using System.Net.Http.Json;

using Xunit.Priority;

namespace Messaging.Tests
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class Functional : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public Functional(WebApplicationFactory<Program> factory)
        {
            _httpClient = factory.CreateClient();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();
        }


        [Fact, Priority(-1)]
        public async Task GetAValidJWTAsync()
        {
            var authRequest = new AuthRequest
            {
                Email = _configuration.GetConnectionString("OpsUsername") ?? string.Empty,
                Password = _configuration.GetConnectionString("OpsPassword") ?? string.Empty,
            };

            var response = await _httpClient.PostAsJsonAsync("/login", authRequest);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var authCredentials = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.NotNull(authCredentials);
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", authCredentials.Token);
        }

        // This is a helper method so that we don't have the share the JWT between all of the tests.
        private async Task<HttpClient> GetHttpClientWithValidBearerTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_httpClient.DefaultRequestHeaders.Authorization?.Parameter))
            {
                var authRequest = new AuthRequest
                {
                    Email = _configuration.GetConnectionString("OpsUsername") ?? string.Empty,
                    Password = _configuration.GetConnectionString("OpsPassword") ?? string.Empty,
                };

                var response = await _httpClient.PostAsJsonAsync("/login", authRequest);
                var authCredentials = await response.Content.ReadFromJsonAsync<AuthResponse>();
                _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", authCredentials?.Token ?? string.Empty);
            }
            return _httpClient;
        }

        [Fact]
        public async Task RegisterAClientAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/swagger/index.html", ClientSecret = "thisisatest", DialedNumber = "12063333341" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);
            Assert.Equal("2063333341", data.DialedNumber);
            Assert.Equal("https://sms.callpipe.com/swagger/index.html", data.CallbackUrl);
            Assert.True(string.IsNullOrWhiteSpace(data.Message));

            // Verify that the newly registered client exists.
            response = await _httpClient.GetAsync("/client?asDialed=2063333341");
            var client = await response.Content.ReadFromJsonAsync<ClientRegistration>();
            Assert.NotNull(client);
            Assert.Equal("2063333341", client.AsDialed);
            Assert.Equal("https://sms.callpipe.com/swagger/index.html", data.CallbackUrl);
        }

        [Fact]
        public async Task GetAllClientsAsync()
        {
            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var response = await _client.GetAsync("/client/all");
            var clients = await response.Content.ReadFromJsonAsync<ClientRegistration[]>();
            Assert.NotNull(clients);
            Assert.NotEmpty(clients);
        }

        [Fact]
        public async Task GetAllMessagesAsync()
        {
            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var response = await _client.GetAsync("/message/all");
            var clients = await response.Content.ReadFromJsonAsync<MessageRecord[]>();
            Assert.NotNull(clients);
            Assert.NotEmpty(clients);
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
            Assert.Equal("\"MSISDN 15555551212 could not be parsed as valid NANP (North American Numbering Plan) number. origtime:2022-04-17 03:48:00, ,msisdn:15555551212, ,to:14445556543, ,sessionid:tLMOYTAmIFiQvBE6X1g, ,timezone:EST, ,message:Your Lyft code is 12345, ,api_version:0.5, ,serversecret:sekrethere, \"", message);
        }

        [Fact]
        public async Task SendSMSMessageAsync()
        {

            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var message = new SendMessageRequest { MediaURLs = Array.Empty<string>(), Message = "This is an SMS Message test.", MSISDN = "12068589313", To = "12068589312" };
            var response = await _client.PostAsJsonAsync("/message/send?test=true", message);
            var details = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
            Assert.NotNull(details);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(details.MessageSent);

        }

        [Fact]
        public async Task MessageSendingTestAsync()
        {
            var stringContent = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("username", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("password", "EST"),
                    new KeyValuePair<string, string>("messagebody", "Your Lyft code is 12345"),

                });
            var response = await _httpClient.PostAsync("/message/send/test", stringContent);
            Assert.False(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task SendSMSGroupMessageAsync()
        {

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
        public async Task SMSGroupMessage()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forwarding", ClientSecret = "thisisatest", DialedNumber = "12068589313" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "12068589312"),
                    new KeyValuePair<string, string>("to", "12068589313,12068589313,15036622288,97167"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "screams in javascript"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was recieved and forwarded to the client.\"", message);
        }

        [Fact]
        public async Task CorrectlyFormattedMessageForUnregisteredClientAsync()
        {
            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589311"),
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
            Assert.Equal("\"12068589311 is not registered as a client.\"", message);
        }

        [Fact]
        public async Task GroupSMSMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forwarding", ClientSecret = "thisisatest", DialedNumber = "12068589312" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2022-04-17 03:48:00"),
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589312,12068589310"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "Hello, this is 1stPoint SMS :D"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was recieved and forwarded to the client.\"", message);
        }

        [Fact]      
        public async Task CenturyLinkBillingMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forwarding", ClientSecret = "thisisatest", DialedNumber = "12068589310" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2023-04-28 19:14:10"),
                    new KeyValuePair<string, string>("msisdn", "67378"),
                    new KeyValuePair<string, string>("to", "12068589310"),
                    new KeyValuePair<string, string>("sessionid", "tLMOYTAmIFiQvBE6X1g"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "CenturyLink: Payment to be applied on 05/05/2023. Text HELP for help, NOREMINDER to stop pay alerts, STOP to stop all alerts. Msg&data rates may apply."),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "sekrethere"),

                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is not System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was recieved and forwarded to the client.\"", message);
        }

        //[Fact]
        //public async Task SendMessageRequestParsingAsync()
        //{
        //    var request = new SendMessageRequest
        //    {
        //        MSISDN = "42278",
        //        To = "2063332262",
        //        Message = "Test Message",
        //    };

        //    var check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("42278", request.MSISDN);
        //        Assert.Equal("12063332262", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }

        //    request = new SendMessageRequest
        //    {
        //        MSISDN = "142278",
        //        To = "12063332262",
        //        Message = "Test Message",
        //    };

        //    check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("42278", request.MSISDN);
        //        Assert.Equal("12063332262", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }

        //    request = new SendMessageRequest
        //    {
        //        MSISDN = "42278",
        //        To = "12063332262",
        //        Message = "Test Message",
        //    };

        //    check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("42278", request.MSISDN);
        //        Assert.Equal("12063332262", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }

        //    request = new SendMessageRequest
        //    {
        //        MSISDN = "142278",
        //        To = "2063332262",
        //        Message = "Test Message",
        //    };

        //    check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("42278", request.MSISDN);
        //        Assert.Equal("12063332262", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }

        //    request = new SendMessageRequest
        //    {
        //        MSISDN = "12063332262",
        //        To = "42278",
        //        Message = "Test Message",
        //    };

        //    check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("12063332262", request.MSISDN);
        //        Assert.Equal("42278", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }

        //    request = new SendMessageRequest
        //    {
        //        MSISDN = "2063332262",
        //        To = "142278",
        //        Message = "Test Message",
        //    };

        //    check = request.RegularizeAndValidate();

        //    if (check)
        //    {
        //        Assert.Equal("12063332262", request.MSISDN);
        //        Assert.Equal("42278", request.To);
        //        Assert.Equal("Test Message", request.Message);
        //    }
        //}

        [Fact]
        public async Task MMSMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forwarding", ClientSecret = "thisisatest", DialedNumber = "12066320575" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/1pcom/inbound/MMS";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2023-05-04 18:06:00"),
                    new KeyValuePair<string, string>("msisdn", "12066320575"),
                    new KeyValuePair<string, string>("to", "12066320575"),
                    new KeyValuePair<string, string>("sessionid", "redo 18:08:25:027ffa64b9fe9824a4c98c73d016264ad92"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "{\r\n\"authkey\":\"2870e0d1-8dfa-4b31-91e9-6d0fc72de19d\",\r\n\"encoding\":\"native\",\r\n\"files\":\"part-001.jpg,part-002.txt,\",\r\n\"recip\":\"12066320575,\",\r\n\"url\":\"https://mmsc01.1pcom.net/MMS_Pickup?msgid=ffa64b9fe9824a4c98c73d016264ad92\"\r\n}"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "Sek3628"),
                    new KeyValuePair<string, string>("remote", "12066320575"),
                    new KeyValuePair<string, string>("host", "12066320575"),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was recieved and forwarded to the client.\"", message);
        }

        [Fact]
        public async Task GroupMMSMessageAsync()
        {
            string route = "/1pcom/inbound/MMS";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("origtime", "2023-05-08 14:50:00"),
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589310"),
                    new KeyValuePair<string, string>("sessionid", "2ee9b7b8a1db41d590a9fcabbec08b63"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "{\r\n\"authkey\":\"7071e405-3cb8-43ac-acae-6c06987ede02\",\r\n\"encoding\":\"native\",\r\n\"files\":\"part-001.txt,\",\r\n\"recip\":\"12068589310,\",\r\n\"url\":\"https://mmsc01.1pcom.net/MMS_Pickup?msgid=2ee9b7b8a1db41d590a9fcabbec08b63\"\r\n}"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("serversecret", "Sek3628"),
                    new KeyValuePair<string, string>("remote", "12065579450"),
                    new KeyValuePair<string, string>("host", "12068589310"),
                    new KeyValuePair<string, string>("FullRecipientList", ", 12067696361"),
                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is not System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was recieved and forwarded to the client.\"", message);
        }

        //[Fact]
        //public async Task SaveFileToDigitalOceanSpacesAsync()
        //{
        //    using var fileStream = new FileStream("", FileMode.Open, FileAccess.Read);
        //    string fileName = "appsettings.json";
        //    var _awsAccessKey = "";
        //    var _awsSecretKey = "";
        //    var bucketName = "MessagingAPIIntegrationTest";
        //    var config = new AmazonS3Config
        //    {
        //        ServiceURL = ""
        //    };
        //    var client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, config);
        //    var bucket = await client.PutBucketAsync(bucketName);
        //    var fileUtil = new TransferUtility(client);
        //    var fileRequest = new TransferUtilityUploadRequest
        //    {
        //        BucketName = bucketName,
        //        InputStream = fileStream,
        //        StorageClass = S3StorageClass.Standard,
        //        Key = fileName,
        //        CannedACL = S3CannedACL.PublicRead,
        //    };
        //    await fileUtil.UploadAsync(fileRequest);
        //    string mediaURL = $"{config.ServiceURL}{bucketName}/{fileRequest.Key}";
        //    Assert.False(string.IsNullOrWhiteSpace(mediaURL));
        //    var httpClient = new HttpClient();
        //    var response = await httpClient.GetByteArrayAsync(mediaURL);
        //    Assert.NotNull(response);
        //    await File.WriteAllBytesAsync(fileRequest.Key, response);
        //    var text = File.ReadAllText(fileRequest.Key);
        //    Assert.False(string.IsNullOrWhiteSpace(text));
        //    // Clean up
        //    File.Delete(fileRequest.Key);
        //}
    }
}