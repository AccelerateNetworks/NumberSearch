using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class DidsGet
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public ResponseData data { get; set; } = new();
        public string ErrorData { get; set; } = string.Empty;

        public class ResponseData
        {
            public string id { get; set; } = string.Empty;
            public string user_id { get; set; } = string.Empty;
            public string call_flow_id { get; set; } = string.Empty;
            public string channel_group_id { get; set; } = string.Empty;
            public object voicemail_inbox_id { get; set; } = new();
            public string number { get; set; } = string.Empty;
            public string country_code { get; set; } = string.Empty;
            public string npa { get; set; } = string.Empty;
            public string nxx { get; set; } = string.Empty;
            public string xxxx { get; set; } = string.Empty;
            public string number_type { get; set; } = string.Empty;
            public string state { get; set; } = string.Empty;
            public string ratecenter { get; set; } = string.Empty;
            public string xmpp_enabled { get; set; } = string.Empty;
            public object slacksms_team { get; set; } = new();
            public string cnam { get; set; } = string.Empty;
            public E911 e911 { get; set; } = new();
            public string note { get; set; } = string.Empty;
            public object forced_call_flow { get; set; } = new();
            public string sms_post_url { get; set; } = string.Empty;
            public string call_post_url { get; set; } = string.Empty;
        }

        public class E911
        {
            public string id { get; set; } = string.Empty;
            public string did_id { get; set; } = string.Empty;
            public string did_number { get; set; } = string.Empty;
            public string full_name { get; set; } = string.Empty;
            public string address { get; set; } = string.Empty;
            public string city { get; set; } = string.Empty;
            public string state { get; set; } = string.Empty;
            public string zip { get; set; } = string.Empty;
            public string unit_type { get; set; } = string.Empty;
            public string unit_number { get; set; } = string.Empty;
            public string create_dt { get; set; } = string.Empty;
            public string modify_dt { get; set; } = string.Empty;
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
                Log.Error($"{await ex.GetResponseStringAsync()}");
                return new DidsGet { code = 500, status = "error", ErrorData = await ex.GetResponseStringAsync() };
            }
        }
    }
}
