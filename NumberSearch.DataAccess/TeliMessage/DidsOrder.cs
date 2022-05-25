using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    /// <summary>
    /// Models https://apidocs.teleapi.net/api/order-phone-numbers/order-single-number
    /// </summary>
    public class DidsOrder
    {
        public int code { get; set; }
        public string status { get; set; }
        public TeliOrderResponse data { get; set; }

        /// <summary>
        /// Submit an order to purchase an available phone number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="callFlow"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<DidsOrder> GetAsync(string dialedNumber, int callFlow, int channelGroup, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/order";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&number={dialedNumber}";
            string callFlowParameter = $"&call_flow={callFlow}";
            string channelGroupParameter = $"&channel_group={channelGroup}";
            string cnamParameter = $"&cnam=enabled";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}{callFlowParameter}{channelGroupParameter}{cnamParameter}";
            return await route.GetJsonAsync<DidsOrder>().ConfigureAwait(false);
        }
    }

    public class TeliOrderResponse
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
        public string sms_enabled { get; set; }
        public string cnam { get; set; }
    }
}
