using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class PortTn
    {
        [JsonPropertyName("Order Details")]
        [JsonProperty("Order Details")]
        public OrderDetails OrderDetails { get; set; }
        [JsonPropertyName("End User Info")]
        [JsonProperty("End User Info")]
        public EndUserInfo EndUserInfo { get; set; }
        [JsonPropertyName("TN List")]
        [JsonProperty("TN List")]
        public TNList[] TNList { get; set; }
        public PortTnAttachment[] Attachments { get; set; }
        public PortTnNote[] Notes { get; set; }


        public static async Task<PortTn> GetAsync(string orderId, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string orderIdParameter = $"?OrderId={orderId}";
            string route = $"{baseUrl}{endpoint}{orderIdParameter}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<PortTn>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Porting] [BulkVS] {ex.Message} for BulkVS orderId {orderId}.");
                Log.Warning($"[Porting] [BulkVS] {await ex.GetResponseStringAsync()}.");
                return null;
            }
        }

        public static async Task<IEnumerable<TNList>> GetAllAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<IEnumerable<TNList>>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Porting] [BulkVS] {ex.Message} for BulkVS port requests.");

                return new List<TNList>();
            }
        }

    }

    public class OrderDetails
    {
        public string OrderId { get; set; }
        public string ReferenceId { get; set; }
        [JsonPropertyName("Losing Carrier")]
        [JsonProperty("Losing Carrier")]
        public string LosingCarrier { get; set; }
        public string Tier { get; set; }
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; }
        public string BTN { get; set; }
    }

    public class EndUserInfo
    {
        public string Name { get; set; }
        [JsonPropertyName("Authorized Contact")]
        [JsonProperty("Authorized Contact")]
        public string Contact { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        [JsonPropertyName("Account Number")]
        [JsonProperty("Account Number")]
        public string AccountNumber { get; set; }
        public string Pin { get; set; }
    }

    public class TNList
    {
        public string OrderId { get; set; }
        public string ReferenceId { get; set; }
        public string TN { get; set; }
        [JsonPropertyName("LNP Status")]
        [JsonProperty("LNP Status")]
        public string LNPStatus { get; set; }
        public string RDD { get; set; }
        public string Reason { get; set; }
    }

    public class PortTnAttachment
    {
        public string Filename { get; set; }
        public string Date { get; set; }
    }

    public class PortTnNote
    {
        public string Date { get; set; }
        public string Author { get; set; }
        public string Note { get; set; }
    }

    public class PortTNNote
    {
        public string[] Attachments { get; set; }
        public string Note { get; set; }

        public async Task PostAsync(string vendorOrderId, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string orderIdParameter = $"?OrderId={vendorOrderId}";
            string route = $"{baseUrl}{endpoint}{orderIdParameter}";

            await route.WithBasicAuth(username, password).PostMultipartAsync(mp => mp.AddString("Note", Note));
        }
    }

    public class PortTnRequest
    {
        public string ReferenceId { get; set; }
        [JsonPropertyName("TN List")]
        [JsonProperty("TN List")]
        public string[] TNList { get; set; }
        public string BTN { get; set; }
        [JsonPropertyName("Subscriber Type")]
        [JsonProperty("Subscriber Type")]
        public string SubscriberType { get; set; }
        [JsonPropertyName("Account Number")]
        [JsonProperty("Account Number")]
        public string AccountNumber { get; set; }
        public string Pin { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        [JsonPropertyName("Street Number")]
        [JsonProperty("Street Number")]
        public string StreetNumber { get; set; }
        [JsonPropertyName("Street Name")]
        [JsonProperty("Street Name")]
        public string StreetName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string RDD { get; set; }
        public string Time { get; set; }
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; }
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; }
        public string Lidb { get; set; }
        public bool Sms { get; set; }
        public bool Mms { get; set; }
        [JsonPropertyName("Sign Loa")]
        [JsonProperty("Sign Loa")]
        public bool SignLoa { get; set; }
        public string Notify { get; set; }

        public async Task<PortTNResponse> PutAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.WithBasicAuth(username, password).PutJsonAsync(this).ReceiveJson<PortTNResponse>();
            }
            catch (FlurlHttpException ex)
            {
                var response = await ex.GetResponseStringAsync();
                Log.Error(response);
                return null;
            }
        }

        public class PortTNResponse
        {
            public string OrderId { get; set; }
            public string CustOrderId { get; set; }
            public string RDD { get; set; }
            public string Description { get; set; }
            public string Code { get; set; }
            [JsonPropertyName("TN Groups")]
            [JsonProperty("TN Groups")]
            public string[] TnGroups { get; set; }
        }
    }
}