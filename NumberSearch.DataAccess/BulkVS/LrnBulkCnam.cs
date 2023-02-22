using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class LrnBulkCnam
    {
        public string tn { get; set; } = string.Empty;
        public string lrn { get; set; } = string.Empty;
        public string ocn { get; set; } = string.Empty;
        public string lata { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string ratecenter { get; set; } = string.Empty;
        public string province { get; set; } = string.Empty;
        public string jurisdiction { get; set; } = string.Empty;
        public string local { get; set; } = string.Empty;
        public string lec { get; set; } = string.Empty;
        public string lectype { get; set; } = string.Empty;
        public string spid { get; set; } = string.Empty;
        public string activation { get; set; } = string.Empty;
        public string LIDBName { get; set; } = string.Empty;
        public DateTime LastPorted { get; set; }

        /// <summary>
        /// Get LRN lookup information for a specific dialed number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<LrnBulkCnam> GetAsync(string dialedNumber, string apiKey)
        {
            string baseUrl = "https://lrn.bulkCNAM.com/";
            string apikeyParameter = $"?id={apiKey}";
            string didParameter = $"&did={dialedNumber}";
            string aniParameter = $"&ani={dialedNumber}";
            string formatParameter = $"&format=json";
            string route = $"{baseUrl}{apikeyParameter}{didParameter}{aniParameter}{formatParameter}";

            try
            {
                var result = await route.GetJsonAsync<LrnBulkCnam>().ConfigureAwait(false);

                // Handle the last ported date.
                // https://stackoverflow.com/questions/2477712/convert-local-time-10-digit-number-to-a-readable-datetime-format
                var checkParse = long.TryParse(result.activation, out var portTime);

                if (checkParse)
                {
                    result.LastPorted = new DateTime(1970, 1, 1).AddSeconds(portTime);
                }

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
