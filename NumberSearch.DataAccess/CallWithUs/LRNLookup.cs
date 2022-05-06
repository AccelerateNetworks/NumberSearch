using Flurl.Http;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.CallWithUs
{
    public class LRNLookup
    {

        public static async Task<LRNLookup> GetAsync(string dialedNumber, string apiKey)
        {
            string baseUrl = "http://lrn.callwithus.com/api/lrn/index.php";
            string apikeyParameter = $"?key={apiKey}";
            string eiParameter = $"&ei";
            string didParameter = $"&number={dialedNumber}";
            string route = $"{baseUrl}{apikeyParameter}{eiParameter}{didParameter}";

            try
            {
                return await route.GetJsonAsync<LRNLookup>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }
    }
}
