using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.Controllers
{
    public class PortingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Portability(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (Query != null && Query?.Length == 10)
            {
                var dialedPhoneNumber = Query;

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    var port = new PhoneNumber
                    {
                        DialedNumber = dialedPhoneNumber,
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput"
                    };

                    return View("Index", new PortingResults
                    {
                        PhoneNumber = port,
                        Cart = cart
                    });
                }
                else
                {
                    return View("Index", new PortingResults
                    {
                        PhoneNumber = new PhoneNumber { },
                        Cart = cart
                    });
                }
            }
            else
            {
                return View("Index", new PortingResults
                {
                    PhoneNumber = new PhoneNumber { },
                    Cart = cart
                });
            }
        }
    }
}