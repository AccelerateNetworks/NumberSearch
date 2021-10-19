using Flurl.Http;

using NumberSearch.DataAccess.TeliMessage;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class EmergencyInfo
    {
        public int code { get; set; }
        public string status { get; set; }
        public EmergencyInfoDetail data { get; set; }

        public class EmergencyInfoDetail
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
            public string alert_group { get; set; }
            public string did_note { get; set; }
        }

        public static async Task<EmergencyInfo> GetAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "911/info";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&did_number={number}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}";
            try
            {
                var result = await route.GetJsonAsync<EmergencyInfo>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException)
            {
                var result = await route.GetJsonAsync<TeliError>().ConfigureAwait(false);

                return new EmergencyInfo
                {
                    code = result.code,
                    status = result.status,
                    data = new EmergencyInfoDetail
                    {
                        did_note = result.data
                    }
                };
            }
        }
    }
}
