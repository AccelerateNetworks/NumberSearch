using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class UserDidsList
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public UserDid[] data { get; set; } = Array.Empty<UserDid>();

        public class UserDid
        {
            public string id { get; set; } = string.Empty;
            public string number { get; set; } = string.Empty;
            public string npa { get; set; } = string.Empty;
            public string nxx { get; set; } = string.Empty;
            public string xxxx { get; set; } = string.Empty;
            public string state { get; set; } = string.Empty;
            public string ratecenter { get; set; } = string.Empty;
            public int tier { get; set; }
            public string call_flow_id { get; set; } = string.Empty;
            public string channel_group_id { get; set; } = string.Empty;
            public string cnam { get; set; } = string.Empty;
            public string country_code { get; set; } = string.Empty;
            public string delivery_notification_url { get; set; } = string.Empty;
            public E911 e911 { get; set; } = new();
            public bool faxable { get; set; }
            public string note { get; set; } = string.Empty;
            public string sms_enabled { get; set; } = string.Empty;
            public string slacksms_team { get; set; } = string.Empty;
            public string sms_post_url { get; set; } = string.Empty;
            public string user_id { get; set; } = string.Empty;
            public string voicemail_inbox_id { get; set; } = string.Empty;
            public bool xmpp_enabled { get; set; }
            public string forced_call_forward { get; set; } = string.Empty;
            public bool straight_to_voicemail { get; set; }
        }

        public class E911
        {
            public int e911 { get; set; }
            public string alert_group { get; set; } = string.Empty;
            public string e911_id { get; set; } = string.Empty;
            public string group_id { get; set; } = string.Empty;
        }

        public static async Task<UserDidsList> GetAllAsync(Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/list";
            string tokenParameter = $"?token={token}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}";
            return await route.GetJsonAsync<UserDidsList>().ConfigureAwait(false);
        }
    }
}
