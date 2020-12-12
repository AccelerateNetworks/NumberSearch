using Flurl.Http;

using Serilog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{

    public class OrderTn
    {
        public string TN { get; set; }
        public string RateCenter { get; set; }
        public string State { get; set; }
        public string PerMinuteRate { get; set; }
        public string Mrc { get; set; }
        public string Nrc { get; set; }


        public static async Task<IEnumerable<OrderTn>> GetRawAsync(string npa, string nxx, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string npaParameter = $"?Npa={npa}";
            string nxxParameter = string.IsNullOrWhiteSpace(nxx) ? string.Empty : $"?Nxx={nxx}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{nxxParameter}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<IEnumerable<OrderTn>>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [BulkVS] No results found for area code {npa}.");

                return new List<OrderTn>() { };
            }
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(int inNpa, string username, string password)
        {
            var results = await GetRawAsync(inNpa.ToString(), string.Empty, username, password).ConfigureAwait(false);

            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return output;
            }

            foreach (var item in results?.ToArray())
            {
                if (item.TN.Length == 10)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(0,3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(6), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new PhoneNumber
                        {
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
                            DialedNumber = item.TN,
                            City = string.IsNullOrWhiteSpace(item.RateCenter) ? "Unknown City" : item.RateCenter,
                            State = string.IsNullOrWhiteSpace(item.State) ? "Unknown State" : item.State,
                            IngestedFrom = "BulkVS"
                        });
                    }
                }
                else if (item.TN.Length == 11)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(1, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(4, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(7), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        output.Add(new PhoneNumber
                        {
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
                            DialedNumber = item.TN,
                            City = string.IsNullOrWhiteSpace(item.RateCenter) ? "Unknown City" : item.RateCenter,
                            State = string.IsNullOrWhiteSpace(item.State) ? "Unknown State" : item.State,
                            IngestedFrom = "BulkVS"
                        });
                    }
                }
                else
                {
                    Log.Warning($"[Ingest] [BulkVS] Failed to parse {item.TN}. Passed neither the 10 or 11 char checks.");
                }
            }
            return output;
        }
    }
}