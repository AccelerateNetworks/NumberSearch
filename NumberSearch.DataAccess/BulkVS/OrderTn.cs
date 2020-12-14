using Flurl.Http;

using Serilog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
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
                    bool checkNpa = int.TryParse(item.TN.Substring(0, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(6, 4), out int xxxx);

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
                            DateIngested = DateTime.Now,
                            IngestedFrom = "BulkVS"
                        });
                    }
                }
                else if (item.TN.Length == 11)
                {
                    bool checkNpa = int.TryParse(item.TN.Substring(1, 3), out int npa);
                    bool checkNxx = int.TryParse(item.TN.Substring(4, 3), out int nxx);
                    bool checkXxxx = int.TryParse(item.TN.Substring(7, 4), out int xxxx);

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
                            DateIngested = DateTime.Now,
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

    public class OrderTnRequestBody
    {
        public string TN { get; set; }
        public string Lidb { get; set; }
        [JsonPropertyName("Portout Pin")]
        public string PortoutPin { get; set; }
        [JsonPropertyName("Trunk Group")]
        public string TrunkGroup { get; set; }
        public bool Sms { get; set; }
        public bool Mms { get; set; }

        /// <summary>
        /// Submit an order for a phone number to BulkVS.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<OrderTnResponseBody> PostAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "orderTn";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveJson<OrderTnResponseBody>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Ingest] [BulkVS] Failed to order {TN}.");

                var error = await ex.GetResponseJsonAsync<OrderTnFailed>();

                return new OrderTnResponseBody
                {
                    TN = TN,
                    Failed = error
                };
            }
        }
    }

    public class OrderTnResponseBody
    {
        public string TN { get; set; }
        public string Status { get; set; }
        public string Lidb { get; set; }
        public string PortoutPin { get; set; }
        public OrderTnRouting Routing { get; set; }
        public OrderTnMessaging Messaging { get; set; }
        public OrderTnTNDetails TNDetails { get; set; }
        public OrderTnFailed Failed { get; set; }
    }

    public class OrderTnRouting
    {
        public string TrunkGroup { get; set; }
        public string CustomURI { get; set; }
        public string CallForward { get; set; }
    }

    public class OrderTnMessaging
    {
        public bool Sms { get; set; }
        public bool Mms { get; set; }
    }

    public class OrderTnTNDetails
    {
        public string RateCenter { get; set; }
        public string State { get; set; }
        public int Tier { get; set; }
        public bool Cnam { get; set; }
        public string ActivationDate { get; set; }
    }


    public class OrderTnFailed
    {
        public string TN { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

}