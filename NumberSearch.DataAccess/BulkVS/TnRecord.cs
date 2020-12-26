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

                return new List<TnRecord>() { };
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
                if (item.TN.Length == 10)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(0, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(6, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new PhoneNumber
                        {
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
                            DialedNumber = item.TN,
                            City = string.IsNullOrWhiteSpace(item.TNDetails.RateCenter) ? "Unknown City" : item.TNDetails.RateCenter,
                            State = string.IsNullOrWhiteSpace(item.TNDetails.State) ? "Unknown State" : item.TNDetails.State,
                            DateIngested = DateTime.Now,
                            IngestedFrom = "BulkVS"
                        });
                    }
                }
                else if (item.TN.Length == 11)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(1, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(4, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(7, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new PhoneNumber
                        {
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
                            DialedNumber = item.TN,
                            City = string.IsNullOrWhiteSpace(item.TNDetails.RateCenter) ? "Unknown City" : item.TNDetails.RateCenter,
                            State = string.IsNullOrWhiteSpace(item.TNDetails.State) ? "Unknown State" : item.TNDetails.State,
                            DateIngested = DateTime.Now,
                            IngestedFrom = "BulkVS"
                        });
                    }
                }
                else
                {
                    Log.Warning($"[Ingest] [BulkVS] Failed to parse {item.TN}. Passed neither the 10 or 11 char checks.");
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
                if (item.TN.Length == 10)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(0, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(6, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new OwnedPhoneNumber
                        {
                            DialedNumber = item.TN,
                            IngestedFrom = "BulkVS",
                            Active = true,
                            DateIngested = DateTime.Now
                        });
                    }
                }
                else if (item.TN.Length == 11)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(1, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(4, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(7, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new OwnedPhoneNumber
                        {
                            DialedNumber = item.TN.Substring(1),
                            IngestedFrom = "BulkVS",
                            Active = true,
                            DateIngested = DateTime.Now
                        });
                    }
                }
                else
                {
                    Log.Warning($"[Ingest] [BulkVS] Failed to parse {item.TN}. Passed neither the 10 or 11 char checks.");
                }
            }
            return output;
        }
    }
}