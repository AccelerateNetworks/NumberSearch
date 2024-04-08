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
    public class TnRecord
    {
        public string TN { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Lidb { get; set; } = string.Empty;
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; } = string.Empty;
        public TnRecordRouting Routing { get; set; } = new();
        public TnRecordMessaging Messaging { get; set; } = new();
        [JsonPropertyName("TN Details")]
        [JsonProperty("TN Details")]
        public TnRecordTNDetails TNDetails { get; set; } = new();

        public class TnRecordRouting
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

        public class TnRecordMessaging
        {
            public bool Sms { get; set; }
            public bool Mms { get; set; }
        }

        public class TnRecordTNDetails
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

        public static async Task<TnRecord[]> GetRawAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "tnRecord";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<TnRecord[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [OwnedNumbers] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return [];
            }
        }

        public static async Task<TnRecord> GetByDialedNumberAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "tnRecord";
            string numberParameter = $"?Number=1{dialedNumber}";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                var results = await route.WithBasicAuth(username, password).GetJsonAsync<TnRecord[]>().ConfigureAwait(false);
                return results.FirstOrDefault() ?? new();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [OwnedNumbers] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }

        public static async Task<PhoneNumber[]> GetAsync(string username, string password)
        {
            var results = await GetRawAsync(username, password).ConfigureAwait(false);
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
                        City = string.IsNullOrWhiteSpace(item.TNDetails.RateCenter) ? "Unknown City" : item.TNDetails.RateCenter,
                        State = string.IsNullOrWhiteSpace(item.TNDetails.State) ? "Unknown State" : item.TNDetails.State,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "BulkVS"
                    });
                }
                else
                {
                    Log.Fatal($"[Ingest] [BulkVS] Failed to parse {item.TN}");
                }
            }
            return [.. output];
        }

        public static async Task<OwnedPhoneNumber[]> GetOwnedAsync(string username, string password)
        {
            var results = await GetRawAsync(username, password).ConfigureAwait(false);
            var output = new List<OwnedPhoneNumber>();

            // Bail out early if something is wrong.
            if (results is null || results.Length == 0)
            {
                return [];
            }

            foreach (var item in results)
            {
                var checkParsed = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParsed && phoneNumber is not null)
                {
                    output.Add(new OwnedPhoneNumber
                    {
                        DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
                        IngestedFrom = "BulkVS",
                        TrunkGroup = item.Routing.TrunkGroup ?? string.Empty,
                        Active = true,
                        DateIngested = DateTime.Now
                    });
                }
                else
                {
                    Log.Fatal($"[Ingest] [BulkVS] Failed to parse {item.TN}");
                }
            }
            return [.. output];
        }
    }
}