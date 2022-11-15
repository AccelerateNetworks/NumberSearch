using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace NumberSearch.Mvc.Controllers
{
    public class PhonesController : Controller
    {

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult GRP2615()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult GXP2170()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult GXP2135()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult T54W()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult DP730()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult DP722()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public IActionResult DP720()
        {
            return View();
        }
    }
}
