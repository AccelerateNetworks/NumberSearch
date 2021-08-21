using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class NewClientController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public NewClientController(IConfiguration config)
        {
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        [HttpGet("Cart/Order/{orderId}/NewClient")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync(Guid orderId)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql);

            var form = new NewClientResult
            {
                Order = order,
                NewClient = new NewClient()
            };

            return View("Index", form);
        }


        [HttpPost("Cart/Order/{orderId}/NewClient")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult SubmitNewClient(Guid orderId, NewClient newClient)
        {
            return View();
        }
    }
}
