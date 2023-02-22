using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class ValidatePortability
    {
        public string TN { get; set; } = string.Empty;
        public bool Portable { get; set; }
        [JsonPropertyName("Losing Carrier")]
        [JsonProperty("Losing Carrier")]
        public string LosingCarrier { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        [JsonPropertyName("Rate Center")]
        [JsonProperty("Rate Center")]
        public string RateCenter { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        [JsonPropertyName("Per Minute Rate")]
        [JsonProperty("Per Minute Rate")]
        public string PerMinuteRate { get; set; } = string.Empty;
        public string Mrc { get; set; } = string.Empty;
        public string Nrc { get; set; } = string.Empty;

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
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }
}