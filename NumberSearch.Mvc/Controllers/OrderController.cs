using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;
using System;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class OrderController : Controller
    {
        private readonly IConfiguration configuration;

        public OrderController(IConfiguration config)
        {
            configuration = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        public async Task<IActionResult> IndexAsync(string query)
        {
            if (query != null && query.Length == 10)
            {
                foreach (var c in query)
                {
                    var check = int.TryParse(c.ToString(), out int i);
                    if (!check)
                    {
                        // Redirect back to the search page. 
                        return RedirectToAction("Index", "Search");
                    };
                }

                var result = await PhoneNumber.GetAsync(query, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Query the owner of the number to make sure it's still avalible for purchase.
                switch (result.IngestedFrom)
                {
                    case "TeleMessage":
                        break;
                    case "BulkVS":
                        break;
                    case "FirstCom":
                        break;
                }

                var model = new PhoneNumberOrderInfo
                {
                    number = result,
                    detail = new PhoneNumberDetail { }
                };

                return View("Index", model);
            }
            else
            {
                return Redirect("/Search");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderAsync([Bind("DialedNumber,FirstName,LastName,Email,Address,Address2,Country,State,Zip")] PhoneNumberOrder order)
        {
            if (order != null && order.DialedNumber.Length == 10 && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                // TODO: Save to db.
                var submittedOrder = await order.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                if (submittedOrder)
                {

                }
                // Email customer
                // Email fufillment
                return View("Success", order);
            }

            return RedirectToAction("Index", "Search");
        }
    }
}