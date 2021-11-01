using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{

    public class UserDidsGet
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public TeliNumberDetailsData data { get; set; }

        public class TeliNumberDetailsData
        {
            public string id { get; set; }
            public string user_id { get; set; }
            public string call_flow_id { get; set; }
            public string channel_group_id { get; set; }
            public string voicemail_inbox_id { get; set; }
            public string number { get; set; }
            public string country_code { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public string number_type { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string xmpp_enabled { get; set; }
            public string slacksms_team { get; set; }
            public string cnam { get; set; }
            public string e911 { get; set; }
            public string note { get; set; }
            public string forced_call_flow { get; set; }
            public string sms_post_url { get; set; }
            public string call_post_url { get; set; }
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
                Log.Warning($"{await ex.GetResponseStringAsync()}");
                return null;
            }
        }
    }
}
