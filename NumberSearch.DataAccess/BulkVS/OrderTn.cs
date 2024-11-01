using Flurl.Http;

using Newtonsoft.Json;

using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class OrderTn
    {
        public string TN { get; set; } = string.Empty;
        [JsonPropertyName("Rate Center")]
        [JsonProperty("Rate Center")]
        public string RateCenter { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        [JsonPropertyName("Per Minute Rate")]
        [JsonProperty("Per Minute Rate")]
        public string PerMinuteRate { get; set; } = string.Empty;
        public string Mrc { get; set; } = string.Empty;
        public string Nrc { get; set; } = string.Empty;

        public static async Task<OrderTn[]> GetRawAsync(string npa, string nxx, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string npaParameter = $"?Npa={npa}";
            string nxxParameter = string.IsNullOrWhiteSpace(nxx) ? string.Empty : $"&Nxx={nxx}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{nxxParameter}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<OrderTn[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [BulkVS] No results found for area code {npa}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return [];
            }
        }

        public static async Task<PhoneNumber[]> GetAsync(int inNpa, string username, string password)
        {
            var results = await GetRawAsync(inNpa.ToString("000"), string.Empty, username, password).ConfigureAwait(false);
            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results is null || results.Length == 0)
            {
                return [];
            }

            foreach (var item in results)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParse && phoneNumber is not null)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
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
            return [.. output];
        }

        public static async Task<PhoneNumber[]> GetAsync(int inNpa, int inNxx, string username, string password)
        {
            var results = await GetRawAsync(inNpa.ToString("000"), inNxx.ToString("000"), username, password).ConfigureAwait(false);
            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results is null || results.Length == 0)
            {
                return [];
            }

            foreach (var item in results)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParse && phoneNumber is not null)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
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
            return [.. output];
        }
    }

    public class OrderTnRequestBody
    {
        public string TN { get; set; } = string.Empty;
        public string Lidb { get; set; } = string.Empty;
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; } = string.Empty;
        [JsonProperty("Trunk Group")]
        [JsonPropertyName("Trunk Group")]
        public string TrunkGroup { get; set; } = string.Empty;
        public bool Sms { get; set; }
        public bool Mms { get; set; }

        /// <summary>
        /// Submit an order for a phone number to BulkVS.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// example JSON responses:
        //        We already own the number:
        //{
        //    "TN": "12062752462",
        //    "Status": "Failed",
        //    "Code": "7511",
        //    "Description": "Telephone Number already provisioned to your account"
        //}
        //Successful purchase:
        //{
        //    "TN": "14255475185",
        //    "Status": "Active",
        //    "Lidb": "Accelerate Networks",
        //    "Portout Pin": "3591344",
        //    "ReferenceID": "",
        //    "Routing": {
        //        "Trunk Group": "",
        //        "Custom URI": null,
        //        "Call Forward": null
        //    },
        //    "Messaging": {
        //        "Sms": true,
        //        "Mms": false
        //    },
        //    "TN Details": {
        //    "Rate Center": "RENTON",
        //        "State": "WA",
        //        "Tier": "0",
        //        "Cnam": true,
        //        "Activation Date": "2024-10-26 22:06:28"
        //    }
        //}
        public async Task<OrderTnResponseBody> PostAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                var response = await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveString().ConfigureAwait(false);
                OrderTnResponseBody weatherForecast =
                System.Text.Json.JsonSerializer.Deserialize<OrderTnResponseBody>(response) ?? new();
                weatherForecast.RawResponse = response;
                return weatherForecast ?? new();
            }
            catch (FlurlHttpException ex)
            {
                Log.Error($"[Ingest] [BulkVS] Failed to order {TN}.");
                var x = await ex.GetResponseStringAsync();
                Log.Error(x);
                var error = await ex.GetResponseJsonAsync<OrderTnFailed>();

                return new OrderTnResponseBody
                {
                    TN = TN,
                    Failed = error,
                    RawResponse = x
                };
            }
        }
    }

    public class OrderTnResponseBody
    {
        public string TN { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Lidb { get; set; } = string.Empty;
        [JsonPropertyName("Portout Pin")]
        public string PortoutPin { get; set; } = string.Empty;
        public OrderTnRouting Routing { get; set; } = new();
        public OrderTnMessaging Messaging { get; set; } = new();
        [JsonPropertyName("TN Details")]
        [JsonProperty("TN Details")]
        public OrderTnTNDetails TNDetails { get; set; } = new();
        public OrderTnFailed Failed { get; set; } = new();
        public string RawResponse { get; set; } = string.Empty;
    }

    public class OrderTnRouting
    {
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; } = string.Empty;
        [JsonPropertyName("Custom URI")]
        [JsonProperty("Custom URI")]
        public string CustomURI { get; set; } = string.Empty;
        [JsonPropertyName("Call Forward")]
        [JsonProperty("Call Forward")]
        public object CallForward { get; set; } = new();
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
        public string RateCenter { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public bool Cnam { get; set; }
        [JsonPropertyName("Activation Date")]
        [JsonProperty("Activation Date")]
        public string ActivationDate { get; set; } = string.Empty;
    }

    public class OrderTnFailed
    {
        public string TN { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}