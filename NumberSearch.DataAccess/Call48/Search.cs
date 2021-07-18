using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Call48
{
    public class Search
    {
        public int code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public Result[] result { get; set; }
            public string loc { get; set; }
            public string setup { get; set; }
            public string monthly { get; set; }
        }

        public class Result
        {
            public string did { get; set; }
            public string number { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public bool fax { get; set; }
            public int tier { get; set; }
            public string type { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string locData { get; set; }
        }



        /// <summary>
        /// Get local numbers.
        /// </summary>
        /// <param name="ratecenter"></param>
        /// <param name="state"> Required. </param>
        /// <param name="npa"></param>
        /// <param name="nxx"></param>
        /// <param name="limit"></param>
        /// <param name="token"> An auth token from the Login endpoint. </param>
        /// <returns></returns>
        public static async Task<Search> GetLocalNumbersAsync(string ratecenter, string state, string npa, string nxx, string token)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"search";
            string typeParameter = $"?type=local";
            string stateParameter = $"&state={state}";
            string ratecenterParameter = string.IsNullOrWhiteSpace(ratecenter) ? string.Empty : $"&ratecenter={ratecenter}";
            string npaParameter = string.IsNullOrWhiteSpace(npa) ? string.Empty : $"&npa={npa}";
            string nxxParameter = string.IsNullOrWhiteSpace(nxx) ? string.Empty : $"&nxx={nxx}";
            // Limit values above 5000 return an error from the API.
            string limitParameter = $"&limit=5000";

            string route = $"{baseUrl}{endPointName}{typeParameter}{stateParameter}{ratecenterParameter}{npaParameter}{nxxParameter}{limitParameter}";

            try
            {
                var result = await route.WithHeader("Authorization", token).GetJsonAsync<Search>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                throw;
            }
        }
    }
}
