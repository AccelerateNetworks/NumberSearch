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
        public string status { get; set; } = string.Empty;
        public TeliOrderResponse data { get; set; } = new();

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
        public string sms_enabled { get; set; } = string.Empty;
        public string cnam { get; set; } = string.Empty;
    }
}
