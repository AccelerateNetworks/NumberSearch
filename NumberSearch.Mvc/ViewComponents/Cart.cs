using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.ViewComponents
{
    public class Cart : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var session = HttpContext.Session;
            if (session.TryGetValue("Cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);

                // TODO: Replace the use of Newtonsoft.Json here with System.Text.Json for better performance.
                var items = JsonConvert.DeserializeObject<List<ProductOrder>>(entries);

                return View(items);
            }
            else
            {
                return View(new List<ProductOrder> { });
            }
        }
    }
}