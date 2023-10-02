using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class PortTn
    {
        [JsonPropertyName("Order Details")]
        [JsonProperty("Order Details")]
        public OrderDetails OrderDetails { get; set; } = new();
        [JsonPropertyName("End User Info")]
        [JsonProperty("End User Info")]
        public EndUserInfo EndUserInfo { get; set; } = new();
        [JsonPropertyName("TN List")]
        [JsonProperty("TN List")]
        public TNList[] TNList { get; set; } = Array.Empty<TNList>();
        public PortTnAttachment[] Attachments { get; set; } = Array.Empty<PortTnAttachment>();
        public PortTnNote[] Notes { get; set; } = Array.Empty<PortTnNote>();


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
                Log.Error($"[Porting] [BulkVS] {ex.Message} for BulkVS orderId {orderId}.");
                var x = await ex.GetResponseStringAsync();
                Log.Error(x);
                return new();
            }
        }

        public static async Task<TNList[]> GetAllAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<TNList[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Porting] [BulkVS] {ex.Message} for BulkVS port requests.");
                Log.Error(await ex.GetResponseStringAsync());
                return Array.Empty<TNList>();
            }
        }

    }

    public class OrderDetails
    {
        public string OrderId { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        [JsonPropertyName("Losing Carrier")]
        [JsonProperty("Losing Carrier")]
        public string LosingCarrier { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; } = string.Empty;
        public string BTN { get; set; } = string.Empty;
    }

    public class EndUserInfo
    {
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("Authorized Contact")]
        [JsonProperty("Authorized Contact")]
        public string Contact { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        [JsonPropertyName("Account Number")]
        [JsonProperty("Account Number")]
        public string AccountNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class TNList
    {
        public string OrderId { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        public string TN { get; set; } = string.Empty;
        [JsonPropertyName("LNP Status")]
        [JsonProperty("LNP Status")]
        public string LNPStatus { get; set; } = string.Empty;
        public string RDD { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class PortTnAttachment
    {
        public string Filename { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public class PortTnNote
    {
        public string Date { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class PortTNNote
    {
        public string[] Attachments { get; set; } = Array.Empty<string>();
        public string Note { get; set; } = string.Empty;

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
        public string ReferenceId { get; set; } = string.Empty;
        [JsonPropertyName("TN List")]
        [JsonProperty("TN List")]
        public string[] TNList { get; set; } = Array.Empty<string>();
        public string BTN { get; set; } = string.Empty;
        [JsonPropertyName("Subscriber Type")]
        [JsonProperty("Subscriber Type")]
        public string SubscriberType { get; set; } = string.Empty;
        [JsonPropertyName("Account Number")]
        [JsonProperty("Account Number")]
        public string AccountNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        [JsonPropertyName("Street Number")]
        [JsonProperty("Street Number")]
        public string StreetNumber { get; set; } = string.Empty;
        [JsonPropertyName("Street Name")]
        [JsonProperty("Street Name")]
        public string StreetName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string RDD { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        [JsonPropertyName("Portout Pin")]
        [JsonProperty("Portout Pin")]
        public string PortoutPin { get; set; } = string.Empty;
        [JsonPropertyName("Trunk Group")]
        [JsonProperty("Trunk Group")]
        public string TrunkGroup { get; set; } = string.Empty;
        public string Lidb { get; set; } = string.Empty;
        public bool Sms { get; set; }
        public bool Mms { get; set; }
        [JsonPropertyName("Sign Loa")]
        [JsonProperty("Sign Loa")]
        public bool SignLoa { get; set; }
        public string Notify { get; set; } = string.Empty;

        public async Task<PortTNResponse> PutAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";

            return await route.WithBasicAuth(username, password).PutJsonAsync(this).ReceiveJson<PortTNResponse>();
        }

        public class PortTNResponse
        {
            public string OrderId { get; set; } = string.Empty;
            public string CustOrderId { get; set; } = string.Empty;
            public string RDD { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            [JsonPropertyName("TN Groups")]
            [JsonProperty("TN Groups")]
            public string[] TnGroups { get; set; } = Array.Empty<string>();
        }
    }
}