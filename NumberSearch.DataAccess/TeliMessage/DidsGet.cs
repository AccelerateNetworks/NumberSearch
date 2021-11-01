using Flurl.Http;

using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{
    public class DidsGet
    {
        public int code { get; set; }
        public string status { get; set; }
        public ResponseData data { get; set; }
        public string ErrorData { get; set; }

        public class ResponseData
        {
            public string id { get; set; }
            public string user_id { get; set; }
            public string call_flow_id { get; set; }
            public string channel_group_id { get; set; }
            public object voicemail_inbox_id { get; set; }
            public string number { get; set; }
            public string country_code { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public string number_type { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string xmpp_enabled { get; set; }
            public object slacksms_team { get; set; }
            public string cnam { get; set; }
            public E911 e911 { get; set; }
            public string note { get; set; }
            public object forced_call_flow { get; set; }
            public string sms_post_url { get; set; }
            public string call_post_url { get; set; }
        }

        public class E911
        {
            public string id { get; set; }
            public string did_id { get; set; }
            public string did_number { get; set; }
            public string full_name { get; set; }
            public string address { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string zip { get; set; }
            public string unit_type { get; set; }
            public string unit_number { get; set; }
            public string create_dt { get; set; }
            public string modify_dt { get; set; }
        }

        public static async Task<DidsGet> GetAsync(string npa, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/nxxs";
            string tokenParameter = $"?token={token}";
            string availableParameter = $"&available=true";
            string npaParameter = $"&npa={npa}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{availableParameter}{npaParameter}";

            try
            {
                return await route.GetJsonAsync<DidsGet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                var error = await ex.GetResponseJsonAsync<TeliError>();
                return new DidsGet { code = error.code, status = error.status, ErrorData = error.data };
            }
        }
    }
}
