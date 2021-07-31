using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleDynamics;

using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HardwareController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string _teleDynamicsUsername;
        private readonly string _teleDynamicsPassword;

        public HardwareController(IConfiguration config)
        {
            configuration = config;
            _teleDynamicsUsername = config.GetConnectionString("TeleDynamicsUsername");
            _teleDynamicsPassword = config.GetConnectionString("TeleDynamicsPassword");
        }

        [HttpGet("Hardware")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            var accessories = products.Where(x => x.Type == "Accessory").ToArray();

            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Phones = products.Where(x => x.Type != "Accessory").ToArray(), Accessories = accessories });
        }

        [HttpGet("Hardware/PartnerPriceList")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> PartnerPriceListAsync()
        {

            var products = await Product.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            foreach (var product in products)
            {
                if (!string.IsNullOrWhiteSpace(product?.VendorPartNumber))
                {
                    var lookup = await VendorProduct.GetAsync(product.VendorPartNumber, _teleDynamicsUsername, _teleDynamicsPassword).ConfigureAwait(false);
                    product.Vendor = lookup;
                }
            }

            return View("PartnerPriceList", new HardwareResult { Phones = products.Where(x => !string.IsNullOrWhiteSpace(x.VendorPartNumber)).OrderBy(x => x.VendorPartNumber).ToArray() });
        }
    }
}