using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.Ops.Models;

namespace NumberSearch.Ops.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }


        [Authorize]
        public async Task<IActionResult> Orders()
        {
            // Show all orders
            var orders = await Order.GetAllAsync(_postgresql);

            return View("Orders", orders);
        }

        [Authorize]
        public async Task<IActionResult> PortRequests()
        {
            // Show all orders
            var portRequests = await PortRequest.GetAllAsync(_postgresql);

            return View("PortRequests", portRequests);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
