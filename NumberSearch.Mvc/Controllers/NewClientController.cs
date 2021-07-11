using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class NewClientController : Controller
    {
        [HttpGet("Cart/Order/{orderId}")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public IActionResult Index(Guid orderId)
        {
            return View();
        }


        [HttpPost("Cart/Order/{orderId}/NewClient")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult SubmitNewClientAsync(Guid orderId, NewClient newClient)
        {
            return View();
        }
    }
}
