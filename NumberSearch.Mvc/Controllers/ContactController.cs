using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class ContactController : Controller
    {
        public async Task<IActionResult> IndexAsync()
        {
            return View();
        }
    }
}