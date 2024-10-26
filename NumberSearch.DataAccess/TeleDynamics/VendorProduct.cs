using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleDynamics
{
    public class VendorProduct
    {
        public string PartNumber { get; set; } = string.Empty;
        public string ResrouceURL { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
        public double MSRP { get; set; }
        public double MAP { get; set; }
        public bool SpecialOrder { get; set; }
        public int Quantity { get; set; }
        public string UPC { get; set; } = string.Empty;

        public static async Task<VendorProduct> GetAsync(string partNumber, string username, string password)
        {
            string baseUrl = "https://tdapi.teledynamics.com/api/v1/product/";
            string checkQuantityParameter = $"/CheckQuantity";
            string route = $"{baseUrl}{partNumber}{checkQuantityParameter}";

            return await route.WithBasicAuth(username, password).GetJsonAsync<VendorProduct>().ConfigureAwait(false);
        }

        public static async Task<Dictionary<string, VendorProduct>> GetAllAsync(string username, string password)
        {
            string[] manufacturers = ["Yealink", "Snom", "Grandstream"];

            List<VendorProduct> products = [];

            await Parallel.ForEachAsync(manufacturers, async (manufacturer, token) =>
                {
                    string baseUrl = "https://tdapi.teledynamics.com/api/v1/product/manufacturer/";
                    string checkQuantityParameter = $"/CheckQuantity";
                    string route = $"{baseUrl}{manufacturer}{checkQuantityParameter}";
                    var results = await route.WithBasicAuth(username, password).GetJsonAsync<List<VendorProduct>>(cancellationToken: token).ConfigureAwait(false);
                    products.AddRange(results);
                });

            return products.ToDictionary(x => x.PartNumber);
        }
    }
}
