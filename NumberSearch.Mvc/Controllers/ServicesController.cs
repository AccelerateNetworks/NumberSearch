using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ServicesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", cart);
        }
    }
}