using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PhoneNumbersNA;
using Serilog;

namespace NumberSearch.DataAccess.Call48
{
    public class Ratecenter
    {
        public int code { get; set; }
        public string error { get; set; }
        public RatecenterDatum[] data { get; set; }

        public class RatecenterDatum
        {
            public int footprint_id { get; set; }
            public string rate_center { get; set; }
        }

        public class StateRatecenter
        {
            public string ShortState { get; set; }
            public string[] Ratecenters { get; set; }
        }
        /// <summary>
        /// Get a valid security token from Call48.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<Ratecenter> GetRatecentersByStateAsync(string shortState, string token)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"ratecenter";
            string stateParameter = $"?state={shortState}";
            string route = $"{baseUrl}{endPointName}{stateParameter}";

            try
            {
                var result = await route.WithHeader("Authorization", token).GetJsonAsync<Ratecenter>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[Call48] Failed to get ratecenters.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }

        public static async Task<StateRatecenter[]> GetAllRatecentersAsync(AreaCode.AreaCodesByState[] states, string token)
        {
            var ratecenters = new List<StateRatecenter>();
            foreach (var state in states)
            {
                var results = await GetRatecentersByStateAsync(state.StateShort, token).ConfigureAwait(false);

                ratecenters.Add(new StateRatecenter
                {
                    ShortState = state.StateShort,
                    Ratecenters = results.data.Select(x => x.rate_center).ToArray()
                });
            }

            return ratecenters.ToArray();
        }
    }
}
