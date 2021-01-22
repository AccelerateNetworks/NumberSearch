using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;

using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HardwareController : Controller
    {
        private readonly IConfiguration configuration;

        public HardwareController(IConfiguration config)
        {
            configuration = config;
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Products = products });
        }
    }
}