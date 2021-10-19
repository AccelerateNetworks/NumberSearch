using Flurl.Http;

using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{
    public class LnpGet
    {
        public int code { get; set; }
        public string status { get; set; }
        public LnpGetResponse data { get; set; }

        public class LnpGetResponse
        {
            public string id { get; set; }
            public string user_id { get; set; }
            public string ticket_id { get; set; }
            public string btn { get; set; }
            public string location_type { get; set; }
            public string business_contact { get; set; }
            public string business_name { get; set; }
            public object first_name { get; set; }
            public object last_name { get; set; }
            public string account_number { get; set; }
            public string service_address { get; set; }
            public string service_city { get; set; }
            public string service_state { get; set; }
            public string service_zip { get; set; }
            public string partial_port { get; set; }
            public object partial_port_details { get; set; }
            public string wireless_number { get; set; }
            public object wireless_pin { get; set; }
            public string caller_id { get; set; }
            public object foc_date { get; set; }
            public object bill_s3_filename { get; set; }
            public string request_status { get; set; }
            public string create_dt { get; set; }
            public string modify_dt { get; set; }
            public string[] numbers { get; set; }
            public NumbersData[] numbers_data { get; set; }
        }

        public class NumbersData
        {
            public string lnp_request_id { get; set; }
            public string number { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string foc_date { get; set; }
            public string request_status { get; set; }
        }

        public static async Task<LnpGet> GetAsync(string requestId, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "lnp/get";
            string tokenParameter = $"?token={token}";
            string requestIdParameter = $"&request_id={requestId}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{requestIdParameter}";
            try
            {
                var result = await url.GetJsonAsync<LnpGet>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException)
            {
                var result = await url.GetJsonAsync<TeliError>().ConfigureAwait(false);

                Log.Fatal($"[TeliMessage] {JsonSerializer.Serialize(result)}");

                return new LnpGet
                {
                    code = result.code,
                    status = result.status,
                    data = new LnpGetResponse()
                };
            }
        }
    }
}