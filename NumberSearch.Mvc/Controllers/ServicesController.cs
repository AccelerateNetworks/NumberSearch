﻿using System;
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
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync()
        {
            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", cart);
        }
    }
}