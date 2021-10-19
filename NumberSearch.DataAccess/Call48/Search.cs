using Flurl.Http;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Call48
{
    public class Search
    {
        public int code { get; set; }
        public string message { get; set; }
        public SearchData data { get; set; }
        public string error { get; set; }

        public class SearchData
        {
            public SearchResult[] result { get; set; }
            public string loc { get; set; }
            public string setup { get; set; }
            public string monthly { get; set; }
            public string uqid { get; set; }
            public string ttl { get; set; }
        }

        public class SearchResult
        {
            public int did_id { get; set; }
            public string did_number { get; set; }
            public string did { get; set; }
            public string number { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public int did_id_type { get; set; }
            public int state_id { get; set; }
            public string rate_center_id { get; set; }
            public int did_status_id { get; set; }
            public string ratecenter { get; set; }
            public string state { get; set; }
            public string monthly { get; set; }
            public string setup { get; set; }
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
        public static async Task<Search> GetLocalNumbersAsync(string state, string ratecenter, string npa, string nxx, string token)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"search";
            string typeParameter = $"?type=local";
            string stateParameter = $"&state={state}";
            string ratecenterParameter = string.IsNullOrWhiteSpace(ratecenter) ? string.Empty : $"&ratecenter={ratecenter}";
            string npaParameter = string.IsNullOrWhiteSpace(npa) ? string.Empty : $"&npa={npa}";
            string nxxParameter = string.IsNullOrWhiteSpace(nxx) ? string.Empty : $"&nxx={nxx}";
            // Limit values above 5000 return an error from the API.
            // A maximum of 50 records are return via the API no matter what limit value you set.
            string limitParameter = $"&limit=50";

            string route = $"{baseUrl}{endPointName}{typeParameter}{stateParameter}{ratecenterParameter}{npaParameter}{nxxParameter}{limitParameter}";

            try
            {
                var result = await route.WithHeader("Authorization", token).GetJsonAsync<Search>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error($"[Ingest] [Call48] Failed to get numbers for {state} and NPA {npa}.");
                Log.Error(await ex.GetResponseStringAsync());
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
            var results = await GetLocalNumbersAsync(stateShort, string.Empty, inNpa.ToString("000"), string.Empty, token).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.data.result.Any())
            {
                return output;
            }

            foreach (var item in results?.data?.result)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.did_number, out var phoneNumber);

                if (checkParse)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = string.IsNullOrWhiteSpace(item.ratecenter) ? "Unknown City" : item.ratecenter,
                        State = string.IsNullOrWhiteSpace(item.state) ? "Unknown State" : item.state,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Call48"
                    });
                }
                else
                {
                    Log.Warning($"[Ingest] [Call48] Failed to parse {item.did_number}");
                }
            }
            return output;
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string stateShort, string ratecenter, string token)
        {
            var results = await GetLocalNumbersAsync(stateShort, ratecenter, string.Empty, string.Empty, token).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.data.result.Any())
            {
                return output;
            }

            foreach (var item in results?.data?.result)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.did_number, out var phoneNumber);

                if (checkParse)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = string.IsNullOrWhiteSpace(item.ratecenter) ? "Unknown City" : item.ratecenter,
                        State = string.IsNullOrWhiteSpace(item.state) ? "Unknown State" : item.state,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Call48"
                    });
                }
                else
                {
                    Log.Warning($"[Ingest] [Call48] Failed to parse {item.did_number}");
                }
            }
            return output;
        }
    }
}
