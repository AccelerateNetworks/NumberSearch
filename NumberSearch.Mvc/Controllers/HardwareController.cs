using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new HardwareResult { Cart = cart, Products = products });
        }
    }
}