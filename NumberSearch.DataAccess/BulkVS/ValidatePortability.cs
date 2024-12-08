using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct ValidatePortability
    (
        string TN,
        bool Portable,
        [property: JsonPropertyName("Losing Carrier")]
        string LosingCarrier,
        string Tier,
        [property: JsonPropertyName("Rate Center")]
        string RateCenter,
        string State,
        [property: JsonPropertyName("Per Minute Rate")]
        string PerMinuteRate,
        string Mrc,
        string Nrc
    )
    {
        public static async Task<ValidatePortability> GetAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "validatePortability";
            string numberParameter = $"?Number={dialedNumber}";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<ValidatePortability>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[Portability] [BulkVS] No results found for number {dialedNumber}.", dialedNumber);
                var result = await ex.GetResponseStringAsync();
                Log.Warning(result);
                return new()
                {
                    TN = $"1{dialedNumber}",
                    Portable = false,
                };
            }
        }
    }
}