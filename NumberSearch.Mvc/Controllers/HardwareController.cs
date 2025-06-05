using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleDynamics;
using NumberSearch.Mvc.Models;

using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HardwareController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _teleDynamicsUsername = mvcConfiguration.TeleDynamicsUsername;
        private readonly string _teleDynamicsPassword = mvcConfiguration.TeleDynamicsPassword;
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;

        [HttpGet("Hardware")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(_postgresql);

            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Phones = [.. products.AsValueEnumerable().Where(x => x.Type is not "Accessory")] });
        }

        [HttpGet("Accessories")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAlternateAsync()
        {
            var products = await Product.GetAllAsync(_postgresql);

            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Accessories = [.. products.AsValueEnumerable().Where(x => x.Type is "Accessory")] });
        }

        [HttpGet("Hardware/{product}")]
        [HttpGet("Accessories/{product}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SpecificProductAsync(string product)
        {
            var products = await Product.GetAllAsync(_postgresql);
            var productItems = products.Where(x => x.Name.ToLowerInvariant().Replace(" ", string.Empty) == product.ToLowerInvariant().Replace(" ", string.Empty));

            // If no matching products are found bump them back to the list of all hardware.
            if (!productItems.Any())
            {
                return Redirect($"/Hardware/");
            }

            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Item", new HardwareResult { Cart = cart, Product = productItems.AsValueEnumerable().FirstOrDefault() ?? new() });
        }

        [HttpGet("Hardware/PartnerPriceList")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 3600)]
        public async Task<IActionResult> PartnerPriceListAsync()
        {

            var products = await Product.GetAllAsync(_postgresql);
            // If this throws an exception we want it to break the page so a bug can get filed and we can investigate.
            var lookup = await VendorProduct.GetAllAsync(_teleDynamicsUsername, _teleDynamicsPassword);

            foreach (var product in products)
            {
                if (!string.IsNullOrWhiteSpace(product?.VendorPartNumber))
                {
                    var checkLookup = lookup.TryGetValue(product.VendorPartNumber, out var localVendor);
                    if (checkLookup && localVendor is not null)
                    {
                        product.Vendor = localVendor;
                    }
                }
            }

            return View("PartnerPriceList", new HardwareResult { Phones = [.. products.AsValueEnumerable().Where(x => !string.IsNullOrWhiteSpace(x.VendorPartNumber)).OrderBy(x => x.VendorPartNumber)] });
        }
    }
}