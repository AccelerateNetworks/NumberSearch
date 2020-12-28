using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class ValidatePortability
    {
        public string TN { get; set; }
        public bool Portable { get; set; }
        [JsonPropertyName("Losing Carrier")]
        [JsonProperty("Losing Carrier")]
        public string LosingCarrier { get; set; }
        public string Tier { get; set; }
        [JsonPropertyName("Rate Center")]
        [JsonProperty("Rate Center")]
        public string RateCenter { get; set; }
        public string State { get; set; }
        [JsonPropertyName("Per Minute Rate")]
        [JsonProperty("Per Minute Rate")]
        public string PerMinuteRate { get; set; }
        public string Mrc { get; set; }
        public string Nrc { get; set; }

        public static async Task<ValidatePortability> GetAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "validatePortability";
            string numberParameter = $"?Number={dialedNumber}";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<ValidatePortability>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Portability] [BulkVS] No results found for number {dialedNumber}.");

                return new ValidatePortability() { };
            }
        }
    }
}