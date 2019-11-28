using Microsoft.AspNetCore.Mvc;

namespace NumberSearch.Mvc.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}