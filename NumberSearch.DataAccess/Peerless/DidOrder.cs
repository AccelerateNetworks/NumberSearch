using Flurl.Http;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class DidOrder
    {
        public string did { get; set; }
        public string category { get; set; }

        public static async Task<DidOrder> PostAsync(string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/order";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{apiKeyParameter}";

            var order = new OrderNumbers
            {

            };
            var request = new DidOrderRequest
            {
                customer_name = "",
            };

            return await route.PostJsonAsync(request).ReceiveJson<DidOrder>();
        }
    }

    public class DidOrderRequest
    {
        public string customer_name { get; set; }
        public OrderNumbers[] order_numbers { get; set; }
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