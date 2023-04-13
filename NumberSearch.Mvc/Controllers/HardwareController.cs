using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleDynamics;
using NumberSearch.Mvc.Models;

using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HardwareController : Controller
    {
        private readonly string _teleDynamicsUsername;
        private readonly string _teleDynamicsPassword;
        private readonly string _postgresql;

        public HardwareController(MvcConfiguration mvcConfiguration)
        {
            _teleDynamicsUsername = mvcConfiguration.TeleDynamicsUsername;
            _teleDynamicsPassword = mvcConfiguration.TeleDynamicsPassword;
            _postgresql = mvcConfiguration.PostgresqlProd;
        }

        [HttpGet("Hardware")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);

            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Phones = products.Where(x => x.Type is not "Accessory").ToArray() });
        }

        [HttpGet("Accessories")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAlternateAsync()
        {
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);

            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Accessories = products.Where(x => x.Type is "Accessory").ToArray() });
        }

        [HttpGet("Hardware/{product}")]
        [HttpGet("Accessories/{product}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SpecificProductAsync(string product)
        {
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            var productItems = products.Where(x => x.Name.ToLowerInvariant().Replace(" ", string.Empty) == product.ToLowerInvariant().Replace(" ", string.Empty));

            // If no matching products are found bump them back to the list of all hardware.
            if (!productItems.Any())
            {
                return Redirect($"/Hardware/");
            }

            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Item", new HardwareResult { Cart = cart, Product = productItems.FirstOrDefault() ?? new() });
        }

        [HttpGet("Hardware/PartnerPriceList")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public async Task<IActionResult> PartnerPriceListAsync()
        {

            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            // If this throws an exception we want it to break the page so a bug can get filed and we can investigate.
            var lookup = await VendorProduct.GetAllAsync(_teleDynamicsUsername, _teleDynamicsPassword).ConfigureAwait(false);

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

            return View("PartnerPriceList", new HardwareResult { Phones = products.Where(x => !string.IsNullOrWhiteSpace(x.VendorPartNumber)).OrderBy(x => x.VendorPartNumber).ToArray() });
        }
    }
}