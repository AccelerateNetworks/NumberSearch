using Flurl.Http;

using System.Threading.Tasks;

namespace BulkVS
{
    public class LrnBulkCnam
    {
        public string tn { get; set; }
        public string lrn { get; set; }
        public string ocn { get; set; }
        public string lata { get; set; }
        public string city { get; set; }
        public string ratecenter { get; set; }
        public string province { get; set; }
        public string jurisdiction { get; set; }
        public string local { get; set; }
        public string lec { get; set; }
        public string lectype { get; set; }
        public string spid { get; set; }
        public string activation { get; set; }

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
            return await route.GetJsonAsync<LrnBulkCnam>().ConfigureAwait(false);
        }
    }
}
