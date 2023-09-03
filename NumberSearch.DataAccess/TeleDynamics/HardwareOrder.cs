using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleDynamics
{
    public class HardwareOrder
    {
        public class Order
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string PONumber { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal SubTotal { get; set; }
            public decimal OrderTotal { get; set; }
            public string BillingType { get; set; } = string.Empty;
            public bool HoldRequest { get; set; }
            public bool IsProvisioningOrder { get; set; }
            public string ServiceProvider { get; set; } = string.Empty;
            public string ShippingType { get; set; } = string.Empty;
            public Shipping Shipping { get; set; } = new();
            public Trackinginformation[] TrackingInformation { get; set; } = Array.Empty<Trackinginformation>();
            public ShippingAddress ShippingAddress { get; set; } = new();
            public EnduserAddresses EndUserAddrses { get; set; } = new();
            public int ShipmentTypeAddressKey { get; set; }
            public Orderline[] OrderLines { get; set; } = Array.Empty<Orderline>();
            public DateTime CreateDate { get; set; }
            public DateTime LastChangeDate { get; set; }
            public bool SignatureRequired { get; set; }
            public bool UseTPP { get; set; }
            public string ProvisionUrl { get; set; } = string.Empty;
            public string ServerUserName { get; set; } = string.Empty;
            public string ServerPassword { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string JobReferenceNumber { get; set; } = string.Empty;
            public bool CalculateShipping { get; set; }
        }

        public class Shipping
        {
            public string Carrier { get; set; } = string.Empty;
            public string ShippingMethod { get; set; } = string.Empty;
            public decimal Quote { get; set; }
        }

        public class ShippingAddress
        {
            public string Label { get; set; } = string.Empty;
            public string Address1 { get; set; } = string.Empty;
            public string Address2 { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string ZipCode { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string StateOrProvince { get; set; } = string.Empty;
        }

        public class EnduserAddresses
        {
            public string Label { get; set; } = string.Empty;
            public string Address1 { get; set; } = string.Empty;
            public string Address2 { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string ZipCode { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string StateOrProvince { get; set; } = string.Empty;
        }

        public class Trackinginformation
        {
            public string Carrier { get; set; } = string.Empty;
            public string TrackingNumber { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime ShipDate { get; set; }
            public Productnumber[] ProductNumbers { get; set; } = Array.Empty<Productnumber>();
        }

        public class Productnumber
        {
            public string PartNumber { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        public class Orderline
        {
            public string ProductName { get; set; } = string.Empty;
            public string PartNumber { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal ExtPrice { get; set; }
            public string Firmware { get; set; } = string.Empty;
            public Serializationinformation[] SerializationInformation { get; set; } = Array.Empty<Serializationinformation>();
            public bool IsBackorder { get; set; }
            public int QtyBackOrdered { get; set; }
            public bool ShouldProvision { get; set; }
        }

        public class Serializationinformation
        {
            public string MAC { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
        }

        public static async Task<Order[]> SearchByPONumberAsync(string orderNumber, string username, string password)
        {
            string baseUrl = "https://tdapi.teledynamics.com/api/v1/orders";
            string checkQuantityParameter = $"?searchCriteria={orderNumber}";
            string route = $"{baseUrl}{checkQuantityParameter}";

            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<Order[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                Log.Error(error);
                return Array.Empty<Order>();
            }
        }

        public static async Task<Order[]> GetByPONumberAsync(string ponumber, string username, string password)
        {
            string baseUrl = "https://tdapi.teledynamics.com/api/v2/orders/";
            string route = $"{baseUrl}{ponumber},";

            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<Order[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                Log.Error(error);
                return Array.Empty<Order>();
            }
        }

        public async Task<Order> PostAsync(string username, string password)
        {
            string baseUrl = "https://tdapi.teledynamics.com/api/v1/orders/";
            string route = $"{baseUrl}";

            try
            {
                return await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveJson<Order>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                Log.Error(error);
                return new();
            }
        }
    }
}
