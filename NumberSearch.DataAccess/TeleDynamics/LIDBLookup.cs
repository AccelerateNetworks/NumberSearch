using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleDynamics
{
    public class VendorProduct
    {
        public string PartNumber { get; set; }
        public string ResrouceURL { get; set; }
        public string Manufacturer { get; set; }
        public string ProductName { get; set; }
        public double UnitPrice { get; set; }
        public double MSRP { get; set; }
        public double MAP { get; set; }
        public bool SpecialOrder { get; set; }
        public int Quantity { get; set; }
        public string UPC { get; set; }

        public static async Task<VendorProduct> GetAsync(string partNumber, string username, string password)
        {
            string baseUrl = "https://tdapi-sandbox.teledynamics.com/api/v1/product/";
            string checkQuantityParameter = $"/CheckQuantity";
            string route = $"{baseUrl}{partNumber}{checkQuantityParameter}";

            return await route.WithBasicAuth(username, password).GetJsonAsync<VendorProduct>().ConfigureAwait(false);
        }
    }
}
