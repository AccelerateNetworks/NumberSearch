using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

public class OwnedNumbersController : Controller
{
    private readonly numberSearchContext _context;

    public OwnedNumbersController(numberSearchContext context)
    {
        _context = context;
    }

    [Authorize]
    [Route("/Home/OwnedNumbers")]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    public async Task<IActionResult> OwnedNumbers(string dialedNumber)
    {
        if (string.IsNullOrWhiteSpace(dialedNumber))
        {
            // Show all orders
            var orders = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
            return View("OwnedNumbers", orders);
        }
        else
        {
            var order = await _context.OwnedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            return View("OwnedNumberEdit", order);
        }
    }

    [Authorize]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OwnedNumberUpdate(OwnedPhoneNumber number)
    {
        if (number is null)
        {
            return Redirect("/Home/OwnedNumbers");
        }
        else
        {
            var order = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.DialedNumber);
            if (order is not null)
            {
                order.Notes = number.Notes;
                order.OwnedBy = number.OwnedBy;
                order.BillingClientId = number.BillingClientId;
                order.Active = number.Active;
                order.SPID = order.SPID;
                order.SPIDName = order.SPIDName;

                var orderToUpdate = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.DialedNumber);
                _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                await _context.SaveChangesAsync();
            }

            return View("OwnedNumberEdit", order);
        }
    }
}