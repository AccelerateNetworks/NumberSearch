using Flurl.Http;

using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct OrderTn
    (
        string TN,
        [property: JsonPropertyName("Rate Center")]
        string RateCenter,
        string State,
        [property: JsonPropertyName("Per Minute Rate")]
        string PerMinuteRate,
        string Mrc,
        string Nrc
    )
    {
        public static async ValueTask<OrderTn[]> GetRawAsync(int npa, int nxx, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string npaParameter = $"?Npa={npa:000}";
            string nxxParameter = PhoneNumbersNA.AreaCode.ValidNXX(nxx) ? string.Empty : $"&Nxx={nxx:000}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{nxxParameter}";
            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<OrderTn[]>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [BulkVS] No results found for area code {npa}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return [];
            }
        }

        public static async Task<PhoneNumber[]> GetAsync(int inNpa, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            OrderTn[] results = await GetRawAsync(inNpa, default, username, password);
            List<PhoneNumber> output = [];

            // Bail out early if something is wrong.
            if (results.Length is 0) { return []; }

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

        public static async Task<PhoneNumber[]> GetAsync(int inNpa, int inNxx, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            OrderTn[] results = await GetRawAsync(inNpa, inNxx, username, password);
            List<PhoneNumber> output = [];

            // Bail out early if something is wrong.
            if (results.Length is 0) { return []; }

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

    public readonly record struct OrderTnRequestBody
    (
        string TN,
        string Lidb,
        [property: JsonPropertyName("Portout Pin")]
        string PortoutPin,
        [property: JsonPropertyName("Trunk Group")]
        string TrunkGroup,
        bool Sms,
        bool Mms
    )
    {
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
                var response = await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveString();
                OrderTnResponseBody weatherForecast =
                System.Text.Json.JsonSerializer.Deserialize<OrderTnResponseBody>(response);
                weatherForecast = weatherForecast with { RawResponse = response };
                return weatherForecast;
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

    public readonly record struct OrderTnResponseBody
    (
        string TN,
        string Status,
        string Lidb,
        [property: JsonPropertyName("Portout Pin")]
        string PortoutPin,
        OrderTnRouting Routing,
        OrderTnMessaging Messaging,
        [property: JsonPropertyName("TN Details")]
        OrderTnTNDetails TNDetails,
        OrderTnFailed Failed,
        string RawResponse
    );

    public readonly record struct OrderTnRouting
    (
        [property: JsonPropertyName("Trunk Group")]
        string TrunkGroup,
        [property: JsonPropertyName("Custom URI")]
        string CustomURI,
        [property: JsonPropertyName("Call Forward")]
        object CallForward
    );

    public readonly record struct OrderTnMessaging(bool Sms,bool Mms);

    public readonly record struct OrderTnTNDetails
    (
        [property: JsonPropertyName("Rate Center")]
        string RateCenter,
        string State,
        string Tier,
        bool Cnam,
        [property: JsonPropertyName("Activation Date")]
        string ActivationDate
    );

    public readonly record struct OrderTnFailed(string TN,string Status,string Code,string Description); 
}