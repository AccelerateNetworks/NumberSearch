using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace NumberSearch.Mvc.Controllers
{
    public class HardwareController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}