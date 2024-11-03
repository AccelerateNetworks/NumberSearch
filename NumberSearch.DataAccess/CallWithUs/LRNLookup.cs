using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.CallWithUs
{
    public readonly record struct LRNLookup(
        string LRN,
        string State,
        string Ratecenter,
        string LATA,
        string OCN,
        string Company,
        string Prefix_Type,
        string CLLI
        )
    {
        /// <summary>
        /// Docs: http://callwithus.com/API#lrn
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<LRNLookup> GetAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "http://lrn.callwithus.com/api/lrn/index.php";
            string apikeyParameter = $"?key={apiKey}";
            string eiParameter = $"&ei";
            string didParameter = $"&number={dialedNumber}";
            string route = $"{baseUrl}{apikeyParameter}{eiParameter}{didParameter}";

            try
            {
                var raw = await route.GetStringAsync();
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
                Log.Error(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }
}
