using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleDynamics
{
    public class HardwareOrder
    {
        public class Order
        {
            public string OrderNumber { get; set; }
            public string PONumber { get; set; }
            public string Status { get; set; }
            public int SubTotal { get; set; }
            public int OrderTotal { get; set; }
            public string BillingType { get; set; }
            public bool HoldRequest { get; set; }
            public bool IsProvisioningOrder { get; set; }
            public string ServiceProvider { get; set; }
            public string ShippingType { get; set; }
            public Shipping Shipping { get; set; }
            public Trackinginformation[] TrackingInformation { get; set; }
            public ShippingAddress ShippingAddress { get; set; }
            public EnduserAddresses EndUserAddrses { get; set; }
            public int ShipmentTypeAddressKey { get; set; }
            public Orderline[] OrderLines { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime LastChangeDate { get; set; }
            public bool SignatureRequired { get; set; }
            public bool UseTPP { get; set; }
            public string ProvisionUrl { get; set; }
            public string ServerUserName { get; set; }
            public string ServerPassword { get; set; }
            public string Notes { get; set; }
            public string JobReferenceNumber { get; set; }
            public bool CalculateShipping { get; set; }
        }

        public class Shipping
        {
            public string Carrier { get; set; }
            public string ShippingMethod { get; set; }
            public int Quote { get; set; }
        }

        public class ShippingAddress
        {
            public string Label { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
            public string Country { get; set; }
            public string StateOrProvince { get; set; }
        }

        public class EnduserAddresses
        {
            public string Label { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
            public string Country { get; set; }
            public string StateOrProvince { get; set; }
        }

        public class Trackinginformation
        {
            public string Carrier { get; set; }
            public string TrackingNumber { get; set; }
            public string Status { get; set; }
            public DateTime ShipDate { get; set; }
            public Productnumber[] ProductNumbers { get; set; }
        }

        public class Productnumber
        {
            public string PartNumber { get; set; }
            public int Quantity { get; set; }
        }

        public class Orderline
        {
            public string ProductName { get; set; }
            public string PartNumber { get; set; }
            public int Quantity { get; set; }
            public int UnitPrice { get; set; }
            public int ExtPrice { get; set; }
            public string Firmware { get; set; }
            public Serializationinformation[] SerializationInformation { get; set; }
            public bool IsBackorder { get; set; }
            public int QtyBackOrdered { get; set; }
            public bool ShouldProvision { get; set; }
        }

        public class Serializationinformation
        {
            public string MAC { get; set; }
            public string SerialNumber { get; set; }
        }

        public class Error
        {
            public string Message { get; set; }
        }

        public static async Task<Order[]> SearchByPONumberAsync(string ponumber, string username, string password)
        {
            string baseUrl = "https://tdapi-sandbox.teledynamics.com/api/v1/orders";
            string checkQuantityParameter = $"?searchCriteria={ponumber}";
            string route = $"{baseUrl}{checkQuantityParameter}";

            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<Order[]>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<Error>();
                return new Order[] { };
            }
        }

        public static async Task<Order> GetByPONumberAsync(string ponumber, string username, string password)
        {
            string baseUrl = "https://tdapi-sandbox.teledynamics.com/api/v1/orders/";
            string route = $"{baseUrl}{ponumber}";

            try
            {
                return await route.WithBasicAuth(username, password).GetJsonAsync<Order>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<Error>();
                return new Order { };
            }
        }

        public async Task<Order> PostAsync(string username, string password)
        {
            string baseUrl = "https://tdapi-sandbox.teledynamics.com/api/v1/orders/";
            string route = $"{baseUrl}";

            try
            {
                return await route.WithBasicAuth(username, password).PostJsonAsync(this).ReceiveJson<Order>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<Error>();
                return new Order { };
            }
        }
    }
}
