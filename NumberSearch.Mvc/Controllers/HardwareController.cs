using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;

using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class HardwareController : Controller
    {
        private readonly IConfiguration configuration;

        public HardwareController(IConfiguration config)
        {
            configuration = config;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var products = await Product.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
            return View("Index", products);
        }
    }
}