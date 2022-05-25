using Flurl.Http;

using Serilog;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.CallWithUs
{
    public class LRNLookup
    {
        public string LRN { get; set; }
        public string State { get; set; }
        public string Ratecenter { get; set; }
        public string LATA { get; set; }
        public string OCN { get;set; }
        public string Company { get; set; }
        public string Prefix_Type { get; set; }
        public string CLLI { get; set; }

        /// <summary>
        /// Docs: http://callwithus.com/API#lrn
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<LRNLookup> GetAsync(string dialedNumber, string apiKey)
        {
            string baseUrl = "http://lrn.callwithus.com/api/lrn/index.php";
            string apikeyParameter = $"?key={apiKey}";
            string eiParameter = $"&ei";
            string didParameter = $"&number={dialedNumber}";
            string route = $"{baseUrl}{apikeyParameter}{eiParameter}{didParameter}";

            try
            {
                var raw = await route.GetStringAsync().ConfigureAwait(false);
                var chunks = raw.Split(',');
                return new LRNLookup
                {
                    LRN = chunks[0],
                    State = chunks[1],
                    Ratecenter = chunks[2],
                    LATA = chunks[3],
                    OCN = chunks[4],
                    Company = chunks[5].Replace("\u0022", string.Empty),
                    Prefix_Type = chunks[6],
                    CLLI = chunks[7].Replace("\n", string.Empty),
                };
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }
    }
}
