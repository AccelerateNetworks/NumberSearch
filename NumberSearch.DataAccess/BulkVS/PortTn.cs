using Flurl.Http;

using Serilog;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct PortTn
    (
        [property: JsonPropertyName("Order Details")]
        OrderDetails OrderDetails,
        [property: JsonPropertyName("End User Info")]
        EndUserInfo EndUserInfo,
        [property: JsonPropertyName("TN List")]
        TNList[] TNList,
        PortTnAttachment[] Attachments,
        PortTnNote[] Notes
    )
    {
        public static async Task<PortTn> GetAsync(ReadOnlyMemory<char> orderId, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string orderIdParameter = $"?OrderId={orderId}";
            string route = $"{baseUrl}{endpoint}{orderIdParameter}";

            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<PortTn>();
                //var x = await route.WithBasicAuth(username.ToString(), password.ToString()).GetStringAsync();
                //return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error("[Porting] [BulkVS] {Message} for BulkVS orderId {Id}.", ex.Message, orderId);
                var x = await ex.GetResponseStringAsync();
                Log.Error(x);
                return new();
            }
        }

        public static async Task<TNList[]> GetAllAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<TNList[]>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[Porting] [BulkVS] {Message} for BulkVS port requests.", ex.Message);
                Log.Error(await ex.GetResponseStringAsync());
                return [];
            }
        }
    }

    public readonly record struct OrderDetails
    (
        string OrderId,
        string ReferenceId,
        [property: JsonPropertyName("Losing Carrier")]
        string LosingCarrier,
        string Tier,
        [property: JsonPropertyName("Trunk Group")]
        string TrunkGroup,
        string BTN
    );

    public readonly record struct EndUserInfo
    (
        string Name,
        [property: JsonPropertyName("Authorized Contact")]
        string Contact,
        string Address,
        string City,
        string State,
        string Zip,
        [property: JsonPropertyName("Account Number")]
        string AccountNumber,
        string Pin
    );

    public readonly record struct TNList
    (
        string OrderId,
        string ReferenceId,
        string TN,
        [property: JsonPropertyName("LNP Status")]
        string LNPStatus,
        string RDD,
        string Reason
    );

    public readonly record struct PortTnAttachment(string Filename, string Date);

    public readonly record struct PortTnNote(string Date, string Author, string Note);

    public record PortTNNote(string[] Attachments, string Note)
    {
        public async Task PostAsync(string vendorOrderId, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string orderIdParameter = $"?OrderId={vendorOrderId}";
            string route = $"{baseUrl}{endpoint}{orderIdParameter}";

            await route.WithBasicAuth(username, password).PostMultipartAsync(mp => mp.AddString("Note", Note));
        }
    }

    public record PortTnRequest
    {
        public string ReferenceId { get; set; } = string.Empty;
        [JsonPropertyName("TN List")]
        public string[] TNList { get; set; } = [];
        public string BTN { get; set; } = string.Empty;
        [JsonPropertyName("Subscriber Type")]
        public string SubscriberType { get; set; } = string.Empty;
        [JsonPropertyName("Account Number")]
        public string AccountNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        [JsonPropertyName("Street Number")]
        public string StreetNumber { get; set; } = string.Empty;
        [JsonPropertyName("Street Name")]
        public string StreetName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string RDD { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        [JsonPropertyName("Portout Pin")]
        public string PortoutPin { get; set; } = string.Empty;
        [JsonPropertyName("Trunk Group")]
        public string TrunkGroup { get; set; } = string.Empty;
        public string Lidb { get; set; } = string.Empty;
        public bool Sms { get; set; }
        public bool Mms { get; set; }
        [JsonPropertyName("Sign Loa")]
        public bool SignLoa { get; set; }
        public string Notify { get; set; } = string.Empty;

        public async Task<PortTNResponse> PutAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "portTn";
            string route = $"{baseUrl}{endpoint}";

            return await route.WithBasicAuth(username, password).PutJsonAsync(this).ReceiveJson<PortTNResponse>();
        }

        public readonly record struct PortTNResponse
        (
            string OrderId,
            string CustOrderId,
            string RDD,
            string Description,
            string Code,
            [property: JsonPropertyName("TN Groups")]
            string[] TnGroups
        );
    }
}