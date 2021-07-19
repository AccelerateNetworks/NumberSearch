using Flurl.Http;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                //var error = await ex.GetResponseJsonAsync();
                Log.Error($"[Ingest] [Call48] Failed to get numbers for {state} and NPA {npa}.");
                //Log.Error(JsonSerializer.Serialize(error));
                return null;
            }
        }

        /// <summary>
        /// Get phone number by state short code and area code from Call48.
        /// </summary>
        /// <param name="stateShort"></param>
        /// <param name="inNpa"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string stateShort, int inNpa, string token)
        {
            var results = await GetLocalNumbersAsync(string.Empty, stateShort, inNpa.ToString(), string.Empty, token).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.data.result.Any())
            {
                return output;
            }

            foreach (var item in results?.data?.result)
            {
                if (item.did.Contains("-"))
                {
                    item.did = item.did.Replace("-", string.Empty);
                    //Log.Information($"[Ingest] [Call48] Removed dashes from {item.did}.");
                }

                // If the number has at least 10 chars then it could be a valid phone number.
                // If the number starts with a 1 then it's a US number, we want to ignore internation numbers.
                if (item.did.Length == 10 || item.did.Length == 11)
                {
                    item.did = item.did.Substring(item.did.Length - 10);
                }
                else
                {
                    Log.Warning($"[Ingest] [Call48] Failed to parse {item.did}. Passed neither the 10 or 11 char checks.");
                    continue;
                }

                bool checkNpa = int.TryParse(item.npa, out int npa);
                bool checkNxx = int.TryParse(item.nxx, out int nxx);
                bool checkXxxx = int.TryParse(item.xxxx, out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        DialedNumber = item.did,
                        City = string.IsNullOrWhiteSpace(item.ratecenter) ? "Unknown City" : item.ratecenter,
                        State = string.IsNullOrWhiteSpace(item.state) ? "Unknown State" : item.state,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Call48"
                    });
                }
            }
            return output;
        }
    }
}
