using Microsoft.AspNetCore.Mvc;

using NumberSearch.Mvc.Models;

using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ServicesController : Controller
    {
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);
            return View("Index", new ServicesResult { Cart = cart, Type = string.Empty });
        }


        [HttpGet("Internet")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> InternetAsync()
        {
            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);
            return View("Index", new ServicesResult { Cart = cart, Type = "Internet" });
        }


        [HttpGet("Additional")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AdditionalAsync()
        {
            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);
            return View("Index", new ServicesResult { Cart = cart, Type = "Additional" });
        }
    }
}