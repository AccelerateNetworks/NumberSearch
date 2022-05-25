using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class UserDidsList
    {
        public int code { get; set; }
        public string status { get; set; }
        public UserDid[] data { get; set; }

        public class UserDid
        {
            public string id { get; set; }
            public string number { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public int tier { get; set; }
            public string call_flow_id { get; set; }
            public string channel_group_id { get; set; }
            public string cnam { get; set; }
            public string country_code { get; set; }
            public string delivery_notification_url { get; set; }
            public E911 e911 { get; set; }
            public bool faxable { get; set; }
            public string note { get; set; }
            public string sms_enabled { get; set; }
            public string slacksms_team { get; set; }
            public string sms_post_url { get; set; }
            public string user_id { get; set; }
            public string voicemail_inbox_id { get; set; }
            public bool xmpp_enabled { get; set; }
            public string forced_call_forward { get; set; }
            public bool straight_to_voicemail { get; set; }
        }

        public class E911
        {
            public int e911 { get; set; }
            public string alert_group { get; set; }
            public string e911_id { get; set; }
            public string group_id { get; set; }
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
