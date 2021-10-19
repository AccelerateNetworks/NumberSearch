using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class OrderTn
    {
        public string TN { get; set; }
        [JsonPropertyName("Rate Center")]
        [JsonProperty("Rate Center")]
        public string RateCenter { get; set; }
        public string State { get; set; }
        [JsonPropertyName("Per Minute Rate")]
        [JsonProperty("Per Minute Rate")]
        public string PerMinuteRate { get; set; }
        public string Mrc { get; set; }
        public string Nrc { get; set; }

        public static async Task<IEnumerable<OrderTn>> GetRawAsync(string npa, string nxx, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string npaParameter = $"?Npa={npa}";
            string nxxParameter = string.IsNullOrWhiteSpace(nxx) ? string.Empty : $"&Nxx={nxx}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{nxxParameter}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<IEnumerable<OrderTn>>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [BulkVS] No results found for area code {npa}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(int inNpa, string username, string password)
        {
            var results = await GetRawAsync(inNpa.ToString("000"), string.Empty, username, password).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return output;
            }

            foreach (var item in results?.ToArray())
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParse)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = string.IsNullOrWhiteSpace(item.RateCenter) ? "Unknown City" : item.RateCenter,
                        State = string.IsNullOrWhiteSpace(item.State) ? "Unknown State" : item.State,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "BulkVS"
                    });
                }
                else
                {
                    Log.Warning($"[Ingest] [BulkVS] Failed to parse {item.TN}.");
                }
            }
            return output;
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(int inNpa, int inNxx, string username, string password)
        {
            var results = await GetRawAsync(inNpa.ToString("000"), inNxx.ToString("000"), username, password).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return output;
            }

            foreach (var item in results?.ToArray())
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParse)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = string.IsNullOrWhiteSpace(item.RateCenter) ? "Unknown City" : item.RateCenter,
                        State = string.IsNullOrWhiteSpace(item.State) ? "Unknown State" : item.State,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "BulkVS"
                    });
                }
                else
                {
                    Log.Warning($"[Ingest] [BulkVS] Failed to parse {item.TN}");
                }
            }
            return output;
        }
    }

    public class OrderTnRequestBody
    {
        public string TN { get; set; }
        public string Lidb { get; set; }
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; }
        [JsonProperty("Trunk Group")]
        [JsonPropertyName("Trunk Group")]
        public string TrunkGroup { get; set; }
        public bool Sms { get; set; }
        public bool Mms { get; set; }

        /// <summary>
        /// Submit an order for a phone number to BulkVS.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<OrderTnResponseBody> PostAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveJson<OrderTnResponseBody>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Error($"[Ingest] [BulkVS] Failed to order {TN}.");
                Log.Error(await ex.GetResponseStringAsync());
                var error = await ex.GetResponseJsonAsync<OrderTnFailed>();

                return new OrderTnResponseBody
                {
                    TN = TN,
                    Failed = error
                };
            }
        }
    }

    public class OrderTnResponseBody
    {
        public string TN { get; set; }
        public string Status { get; set; }
        public string Lidb { get; set; }
        public string PortoutPin { get; set; }
        public OrderTnRouting Routing { get; set; }
        public OrderTnMessaging Messaging { get; set; }
        [JsonPropertyName("TN Details")]
        [JsonProperty("TN Details")]
        public OrderTnTNDetails TNDetails { get; set; }
        public OrderTnFailed Failed { get; set; }
    }

    public class OrderTnRouting
    {
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; }
        [JsonPropertyName("Custom URI")]
        [JsonProperty("Custom URI")]
        public string CustomURI { get; set; }
        [JsonPropertyName("Call Forward")]
        [JsonProperty("Call Forward")]
        public object CallForward { get; set; }
    }

    public class OrderTnMessaging
    {
        public bool Sms { get; set; }
        public bool Mms { get; set; }
    }

    public class OrderTnTNDetails
    {
        [JsonPropertyName("Rate Center")]
        [JsonProperty("Rate Center")]
        public string RateCenter { get; set; }
        public string State { get; set; }
        public int Tier { get; set; }
        public bool Cnam { get; set; }
        [JsonPropertyName("Activation Date")]
        [JsonProperty("Activation Date")]
        public string ActivationDate { get; set; }
    }


    public class OrderTnFailed
    {
        public string TN { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

}