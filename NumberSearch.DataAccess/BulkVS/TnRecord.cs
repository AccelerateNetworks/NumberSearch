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
        public string TN { get; set; }
        public string Status { get; set; }
        public string Lidb { get; set; }
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; }
        public TnRecordRouting Routing { get; set; }
        public TnRecordMessaging Messaging { get; set; }
        [JsonPropertyName("TN Details")]
        [JsonProperty("TN Details")]
        public TnRecordTNDetails TNDetails { get; set; }

        public class TnRecordRouting
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

        public class TnRecordMessaging
        {
            public bool Sms { get; set; }
            public bool Mms { get; set; }
        }

        public class TnRecordTNDetails
        {
            [JsonPropertyName("Rate Center")]
            [JsonProperty("Rate Center")]
            public string RateCenter { get; set; }
            public string State { get; set; }
            public string Tier { get; set; }
            public bool Cnam { get; set; }
            [JsonPropertyName("Activation Date")]
            [JsonProperty("Activation Date")]
            public string ActivationDate { get; set; }
        }

        public static async Task<IEnumerable<TnRecord>> GetRawAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "tnRecord";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<IEnumerable<TnRecord>>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [OwnedNumbers] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string username, string password)
        {
            var results = await GetRawAsync(username, password).ConfigureAwait(false);

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
                        City = string.IsNullOrWhiteSpace(item.TNDetails.RateCenter) ? "Unknown City" : item.TNDetails.RateCenter,
                        State = string.IsNullOrWhiteSpace(item.TNDetails.State) ? "Unknown State" : item.TNDetails.State,
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

        public static async Task<IEnumerable<OwnedPhoneNumber>> GetOwnedAsync(string username, string password)
        {
            var results = await GetRawAsync(username, password).ConfigureAwait(false);

            var output = new List<OwnedPhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return output;
            }

            foreach (var item in results?.ToArray())
            {

                var checkParsed = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParsed)
                {
                    output.Add(new OwnedPhoneNumber
                    {
                        DialedNumber = phoneNumber.DialedNumber,
                        IngestedFrom = "BulkVS",
                        Active = true,
                        DateIngested = DateTime.Now
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
}