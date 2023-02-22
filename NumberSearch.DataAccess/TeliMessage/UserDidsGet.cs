using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{

    public class UserDidsGet
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public TeliNumberDetailsData data { get; set; } = new();

        public class TeliNumberDetailsData
        {
            public string id { get; set; } = string.Empty;
            public string user_id { get; set; } = string.Empty;
            public string call_flow_id { get; set; } = string.Empty;
            public string channel_group_id { get; set; } = string.Empty;
            public string voicemail_inbox_id { get; set; } = string.Empty;
            public string number { get; set; } = string.Empty;
            public string country_code { get; set; } = string.Empty;
            public string npa { get; set; } = string.Empty;
            public string nxx { get; set; } = string.Empty;
            public string xxxx { get; set; } = string.Empty;
            public string number_type { get; set; } = string.Empty;
            public string state { get; set; } = string.Empty;
            public string ratecenter { get; set; } = string.Empty;
            public string xmpp_enabled { get; set; } = string.Empty;
            public string slacksms_team { get; set; } = string.Empty;
            public string cnam { get; set; } = string.Empty;
            public string e911 { get; set; } = string.Empty;
            public string note { get; set; } = string.Empty;
            public string forced_call_flow { get; set; } = string.Empty;
            public string sms_post_url { get; set; } = string.Empty;
            public string call_post_url { get; set; } = string.Empty;
        }

        public static async Task<UserDidsGet> GetAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/get";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&number={number}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}";
            try
            {
                return await route.GetJsonAsync<UserDidsGet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Error($"{await ex.GetResponseStringAsync()}");
                return new();
            }
        }
    }
}
