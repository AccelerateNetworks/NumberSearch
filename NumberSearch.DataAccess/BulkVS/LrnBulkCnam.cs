using Flurl.Http;

using Serilog;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct LrnBulkCnam
    (
        string tn,
        string lrn,
        string ocn,
        string lata,
        string city,
        string ratecenter,
        string province,
        string jurisdiction,
        string local,
        string lec,
        string lectype,
        string spid,
        long? activation,
        string LIDBName,
        DateTime LastPorted
    )
    {
        /// <summary>
        /// Get LRN lookup information for a specific dialed number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<LrnBulkCnam> GetAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "https://lrn.bulkCNAM.com/";
            string apikeyParameter = $"?id={apiKey}";
            string didParameter = $"&did={dialedNumber}";
            string aniParameter = $"&ani={dialedNumber}";
            string formatParameter = $"&format=json";
            string route = $"{baseUrl}{apikeyParameter}{didParameter}{aniParameter}{formatParameter}";

            try
            {
                var resultData = await route.GetStringAsync();

                if (string.IsNullOrWhiteSpace(resultData)) return new();

                // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/invalid-json
                // https://github.com/AccelerateNetworks/NumberSearch/issues/449
                JsonSerializerOptions options = new()
                {
                    NumberHandling =
                        JsonNumberHandling.AllowReadingFromString |
                        JsonNumberHandling.WriteAsString
                };

                LrnBulkCnam result =
                    JsonSerializer.Deserialize<LrnBulkCnam>(resultData, options)!;

                // Handle the last ported date.
                // https://stackoverflow.com/questions/2477712/convert-local-time-10-digit-number-to-a-readable-datetime-format
                //var checkParse = long.TryParse(result.activation, out var portTime);

                //if (checkParse)
                //{
                result = result with { LastPorted = new DateTime(1970, 1, 1).AddSeconds(result.activation ?? 0) };
                //}

                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }
}
