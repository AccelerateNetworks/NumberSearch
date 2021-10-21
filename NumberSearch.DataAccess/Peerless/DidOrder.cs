using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class DidOrder
    {
        public string order_status { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public string description { get; set; }

        public static async Task<DidOrder> GetOrderStatusByIdAsync(string customerName, string orderId, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/order";
            string customerNameParameter = $"?customer_name={customerName}";
            string orderIdParameter = $"&order={orderId}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{customerNameParameter}{orderIdParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<DidOrder>().ConfigureAwait(false);
        }
    }

    public class DidOrderRequest
    {
        public string customer_name { get; set; }
        public OrderNumbers[] order_numbers { get; set; }

        public async Task<DidOrder> PostAsync(string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/order";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{apiKeyParameter}";

            return await route.PostJsonAsync(this).ReceiveJson<DidOrder>();
        }
    }
    public class OrderNumbers
    {
        public string did { get; set; }
        public string connection_type { get; set; }
        public string trunk_name { get; set; }
        public string extension_id { get; set; }
        public bool cnam_delivery { get; set; }
        public bool cnam_storage { get; set; }
        public string cnam_storage_name { get; set; }
        public bool e911 { get; set; }
        public object address { get; set; }
        public object directory_listing { get; set; }
    }
}