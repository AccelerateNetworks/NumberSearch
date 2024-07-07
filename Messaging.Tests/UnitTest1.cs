using Amazon.S3;
using Amazon.S3.Transfer;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Models;

using System.Net.Http.Json;
using System.Net.Mime;

using Xunit.Abstractions;
using Xunit.Priority;

namespace Messaging.Tests
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class Functional : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ITestOutputHelper _output;
        private readonly AppSettings _appSettings;


        public Functional(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _httpClient = factory.CreateClient();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("1828a61a-a56d-4b8c-b4dd-ae4309d19d44")
                .AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d")
                .Build();
            _output = output;
            AppSettings config = new();
            _configuration.Bind(config);
            _appSettings = config;
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
                var authRequest = new LoginRequest
                {
                    Email = _configuration.GetConnectionString("OpsUsername") ?? string.Empty,
                    Password = _configuration.GetConnectionString("OpsPassword") ?? string.Empty,
                };

                var response = await _httpClient.PostAsJsonAsync("/login", authRequest);
                var x = await response.Content.ReadAsStringAsync();

                var authCredentials = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
                _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", authCredentials?.AccessToken ?? string.Empty);
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
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(data.Registered);
            Assert.Equal("12063333341", data.DialedNumber);
            Assert.Equal("https://sms.callpipe.com/swagger/index.html", data.CallbackUrl);
            Assert.True(!string.IsNullOrWhiteSpace(data.Message));

            // Verify that the newly registered client exists.
            response = await _httpClient.GetAsync("/client?asDialed=12063333341");
            var client = await response.Content.ReadFromJsonAsync<ClientRegistration>();
            Assert.NotNull(client);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.Equal("12063333341", client.AsDialed);
            Assert.Equal("https://sms.callpipe.com/swagger/index.html", data.CallbackUrl);
        }

        // TODO: A Complete functional Test includes these steps:
        // 1. Login
        // 2. Register a Client
        // 3. Verify Routing
        // 4. Send Outbound SMS
        // 5. Recieve Inbound SMS
        // 6. Send Outbound MMS
        // 7. Recieve Inbound MMS

        [Fact]
        public async Task GetAllClientsAsync()
        {
            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var response = await _client.GetAsync("/client/all?page=1");
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

            var stringContent = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("message", "Your Lyft code is 12345"),
                ]);

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"To 14445556543 could not be parsed as valid NANP (North American Numbering Plan) numbers. msisdn:15555551212,to:14445556543,message:Your Lyft code is 12345\"", message);
        }

        [Fact]
        public async Task SendSMSMessageAsync()
        {
            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var message = new SendMessageRequest { MediaURLs = [], Message = "This is an SMS Message test.", MSISDN = "2068589313", To = "2068589312" };
            var response = await _client.PostAsJsonAsync("/message/send?test=true", message);
            var details = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
            Assert.NotNull(details);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(details.MessageSent);
        }

        //[Fact]
        //public async Task SendMMSMessageAsync()
        //{
        //    var _client = await GetHttpClientWithValidBearerTokenAsync();
        //    var message = new SendMessageRequest { MediaURLs = new string[] { "https://acceleratenetworks.com/images/scaled/accelerate.png" }, Message = "This is an MMS Message test.", MSISDN = "12068589313", To = "" };
        //    var response = await _client.PostAsJsonAsync("/message/send?test=true", message);
        //    var details = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
        //    Assert.NotNull(details);
        //    _output.WriteLine(await response.Content.ReadAsStringAsync());
        //    Assert.True(response.IsSuccessStatusCode);
        //    Assert.True(details.MessageSent);
        //}


        //[Fact]
        //public async Task MMSSendTestAsync()
        //{
        //    string PComNetUsername = _configuration.GetConnectionString("PComNetUsername") ?? string.Empty;
        //    string PComNetPassword = _configuration.GetConnectionString("PComNetPassword") ?? string.Empty;

        //    // Act
        //    using (var httpClient = new HttpClient())
        //    {
        //        using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://mmsc01.1pcom.net/MMS_Send"))
        //        {
        //            var multipartContent = new MultipartFormDataContent
        //            {
        //                { new StringContent(PComNetUsername), "username" },
        //                { new StringContent(PComNetPassword), "password" },
        //                { new StringContent(""), "recip" },
        //                { new StringContent("12068589313"), "ani" },
        //                { new ByteArrayContent(File.ReadAllBytes("C:\\Users\\thoma\\Downloads\\0228cc08-a403-4a12-85cf-c8bd33fdf14apart-001.SMIL")), "ufiles", "part-001.SMIL" },
        //                { new ByteArrayContent(File.ReadAllBytes("C:\\Users\\thoma\\Downloads\\0228cc08-a403-4a12-85cf-c8bd33fdf14apart-002.txt")), "ufiles", "part-002.txt" },
        //                { new ByteArrayContent(File.ReadAllBytes("C:\\Users\\thoma\\Source\\Repos\\AccelerateNetworks\\NumberSearch\\NumberSearch.Mvc\\wwwroot\\images\\scaled\\accelerate.png")), "ufiles", Path.GetFileName("C:\\Users\\thoma\\Source\\Repos\\AccelerateNetworks\\NumberSearch\\NumberSearch.Mvc\\wwwroot\\images\\scaled\\accelerate.png") }
        //            };
        //            request.Content = multipartContent;

        //            var response = await httpClient.SendAsync(request);

        //            _output.WriteLine(await response.Content.ReadAsStringAsync());
        //        }
        //    }
        //}

        [Fact]
        public async Task SendSMSGroupMessageAsync()
        {
            var _client = await GetHttpClientWithValidBearerTokenAsync();
            var message = new SendMessageRequest { MediaURLs = [], Message = "This is an SMS Group Message test.", MSISDN = "12068589313", To = "12068589312,12068589313,15036622288" };
            var response = await _client.PostAsJsonAsync("/message/send?test=true", message);
            var details = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
            Assert.NotNull(details);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(details.MessageSent);
        }

        [Fact]
        public async Task MessageSendingTestAsync()
        {
            var stringContent = new FormUrlEncodedContent(
            [
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("username", _configuration.GetConnectionString("PComNetUsername") ?? string.Empty),
                    new KeyValuePair<string, string>("password", _configuration.GetConnectionString("PComNetPassword") ?? string.Empty),
                    new KeyValuePair<string, string>("messagebody", "Your Lyft code is 12345"),

                ]);
            var response = await _httpClient.PostAsync("/message/send/test", stringContent);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task MessageForwardingTestAsync()
        {
            var toForward = new ForwardedMessage
            {
                To = "12068589313",
                From = "12068589312",
                ClientSecret = "thisisatest",
                MessageSource = MessageSource.Incoming,
                Content = "This is a test forwarded message.",
                MessageType = MessageType.SMS,
            };
            var response = await _httpClient.PostAsJsonAsync("/message/forward/test", toForward);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task BadToken()
        {
            string route = "/api/inbound/1pcom";
            string token = "thisIsNotAValidToken";

            var stringContent = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("msisdn", "15555551212"),
                    new KeyValuePair<string, string>("to", "14445556543"),
                    new KeyValuePair<string, string>("message", "Your Lyft code is 12345"),
                ]);

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.Unauthorized);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
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
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SMSGroupMessage()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forward/test", ClientSecret = "thisisatest", DialedNumber = "12068589313" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(data));
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "12068589312"),
                    new KeyValuePair<string, string>("to", "12068589313,12068589313,15036622288,97167"),
                    new KeyValuePair<string, string>("message", "screams in javascript"),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            Assert.Equal("\"The incoming message was received and forwarded to the client.\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CorrectlyFormattedMessageForUnregisteredClientAsync()
        {
            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589311"),
                    new KeyValuePair<string, string>("message", "Hello, this is 1stPoint SMS :D"),

                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"12068589311 is not registered as a client.\"", message);
        }

        [Fact]
        public async Task GroupSMSMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forward/test", ClientSecret = "thisisatest", DialedNumber = "12068589312" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "12065579450"),
                    new KeyValuePair<string, string>("to", "12068589312,12068589310"),
                    new KeyValuePair<string, string>("message", "Hello, this is 1stPoint SMS :D"),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was received and forwarded to the client.\"", message);
        }

        [Fact]
        public async Task CenturyLinkBillingMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forward/test", ClientSecret = "thisisatest", DialedNumber = "12068589310" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "67378"),
                    new KeyValuePair<string, string>("to", "12068589310"),
                    new KeyValuePair<string, string>("message", "CenturyLink: Payment to be applied on 05/05/2023. Text HELP for help, NOREMINDER to stop pay alerts, STOP to stop all alerts. Msg&data rates may apply."),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is not System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was received and forwarded to the client.\"", message);
        }

        [Fact]
        public async Task BadFromNumberAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forward/test", ClientSecret = "thisisatest", DialedNumber = "12068991741" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var x = await response.Content.ReadAsStringAsync();
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/api/inbound/1pcom";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "110000011909"),
                    new KeyValuePair<string, string>("to", "12068991741"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "Verizon:+DO+NOT+Share+this+Forgot+Password+code.+A+Verizon+representative+will+NEVER+call+you+or+text+you+for+this+code.+Code+059089."),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is not System.Net.HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task MMSMessageAsync()
        {
            var _httpClient = await GetHttpClientWithValidBearerTokenAsync();
            var registrationRequest = new RegistrationRequest() { CallbackUrl = "https://sms.callpipe.com/message/forward/test", ClientSecret = "thisisatest", DialedNumber = "12066320575" };
            var response = await _httpClient.PostAsJsonAsync("/client/register", registrationRequest);
            var data = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
            Assert.NotNull(data);
            Assert.True(data.Registered);

            string route = "/1pcom/inbound/MMS";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "12066320575"),
                    new KeyValuePair<string, string>("to", "12066320575"),
                    new KeyValuePair<string, string>("message", "{\r\n\"authkey\":\"bbdd4df0-1da0-4c5d-be9e-a94b75513c91\",\r\n\"encoding\":\"native\",\r\n\"files\":\"part-002.txt,\",\r\n\"recip\":\"12068589310,\",\r\n\"url\":\"https://mmsc01.1pcom.net/MMS_Pickup?msgid=ce808f729db2413498ef722333badb2b\"\r\n}"),
                    new KeyValuePair<string, string>("remote", "12066320575"),
                    new KeyValuePair<string, string>("host", "12066320575"),
                });

            response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is System.Net.HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was received and forwarded to the client.\"", message);
        }

        // This will break if the Amazon.S3 library is newer than version 3.7.104.25 (2023/06/01)
        // https://github.com/aws/aws-sdk-net/issues/2622
        [Fact]
        public async Task GroupMMSMessageAsync()
        {
            string route = "/1pcom/inbound/MMS";
            string token = "okereeduePeiquah3yaemohGhae0ie";

            var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("msisdn", "14402277002"),
                    new KeyValuePair<string, string>("to", "14253696177"),
                    new KeyValuePair<string, string>("timezone", "EST"),
                    new KeyValuePair<string, string>("message", "{\r\n\"authkey\":\"30c72157-6ade-46c0-b048-c120d07a0030\",\r\n\"encoding\":\"native\",\r\n\"files\":\"part-003.txt,part-001.SMIL,part-002.jpg,\",\r\n\"recip\":\"14253696177,\",\r\n\"url\":\"https://mmsc01.1pcom.net/MMS_Pickup?msgid=a4358ae3a15c4123b86eedbc3d57a7e4\"\r\n}"),
                    new KeyValuePair<string, string>("api_version", "0.5"),
                    new KeyValuePair<string, string>("FullRecipientList", ","),
                });

            var response = await _httpClient.PostAsync($"{route}?token={token}", stringContent);

            Assert.NotNull(response);
            _output.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode is not System.Net.HttpStatusCode.BadRequest);
            var message = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"The incoming message was received and forwarded to the client.\"", message);
        }

        [Fact]
        public async Task GetClientAsync()
        {
            await using var context = new MockDb().CreateDbContext();

            var result = await Endpoints.ClientByDialedNumberAsync("12063333341", _appSettings, context);

            //Assert
            Assert.IsType<Results<Ok<ClientRegistration>, BadRequest<string>, NotFound<string>>>(result);

            var okResult = (Ok<ClientRegistration>)result.Result;

            Assert.True(okResult.StatusCode is 200);
            Assert.Equal("12063333341", okResult.Value?.AsDialed);
        }

        public class MockDb : IDbContextFactory<MessagingContext>
        {
            public MessagingContext CreateDbContext()
            {
                var options = new DbContextOptionsBuilder<MessagingContext>().Options;

                return new MessagingContext(options);
            }
        }

        //[Fact]
        //public async Task SaveFileToDigitalOceanSpacesAsync()
        //{
        //    using var fileStream = new FileStream("C:\\Users\\thoma\\Source\\Repos\\AccelerateNetworks\\NumberSearch\\Messaging\\appsettings.json", FileMode.Open, FileAccess.Read);
        //    string fileName = "appsettings.json";
        //    var _awsAccessKey = "DO00Y9BM9MYP7388TQ2V";
        //    var _awsSecretKey = "QorC09arfKbBW3jzw0vb1GJZw86c2ytwFOOFvV+Vn6U";
        //    var bucketName = _configuration.GetConnectionString("BucketName") ?? string.Empty;
        //    var config = new AmazonS3Config
        //    {
        //        ServiceURL = _configuration.GetConnectionString("S3ServiceURL") ?? string.Empty,
        //        //SignatureVersion = "v4",
        //    };
        //    var client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, config);
        //    var bucket = await client.ListBucketsAsync();
        //    var fileUtil = new TransferUtility(client);
        //    var fileRequest = new TransferUtilityUploadRequest
        //    {
        //        BucketName = bucketName,
        //        InputStream = fileStream,
        //        StorageClass = S3StorageClass.Standard,
        //        PartSize = fileStream.Length,
        //        Key = fileName,
        //        CannedACL = S3CannedACL.PublicRead,
        //        //DisablePayloadSigning = true,

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