using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
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
            var ownedNumbers = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
            var portedNumbers = await _context.PortedPhoneNumbers.ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.ToArrayAsync();
            var e911s = await _context.EmergencyInformation.ToArrayAsync();
            var viewOrders = new List<OwnedNumberResult>();
            foreach (var ownedNumber in ownedNumbers)
            {
                viewOrders.Add(new OwnedNumberResult { EmergencyInformation = e911s.FirstOrDefault(x => x.DialedNumber == ownedNumber.DialedNumber) ?? new(), Owned = ownedNumber, PortedPhoneNumbers = portedNumbers.Where(x => x.PortedDialedNumber == ownedNumber.DialedNumber).ToArray(), PurchasedPhoneNumbers = purchasedNumbers.Where(x => x.DialedNumber == ownedNumber.DialedNumber).ToArray() });
            }

            return View("OwnedNumbers", viewOrders.ToArray());
        }
        else
        {
            var order = await _context.OwnedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            if (order is not null && order.DialedNumber == dialedNumber)
            {
                var portedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == dialedNumber).ToArrayAsync();
                var purchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == dialedNumber).ToArrayAsync();
                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
                return View("OwnedNumberEdit", new OwnedNumberResult { PurchasedPhoneNumbers = purchasedNumbers, PortedPhoneNumbers = portedNumbers, Owned = order, EmergencyInformation = e911 ?? new() });
            }
            else
            {
                // Show all orders
                var ownedNumbers = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
                var portedNumbers = await _context.PortedPhoneNumbers.ToArrayAsync();
                var purchasedNumbers = await _context.PurchasedPhoneNumbers.ToArrayAsync();
                var e911s = await _context.EmergencyInformation.ToArrayAsync();
                var viewOrders = new List<OwnedNumberResult>();
                foreach (var ownedNumber in ownedNumbers)
                {
                    viewOrders.Add(new OwnedNumberResult { EmergencyInformation = e911s.FirstOrDefault(x => x.DialedNumber == ownedNumber.DialedNumber) ?? new(), Owned = ownedNumber, PortedPhoneNumbers = portedNumbers.Where(x => x.PortedDialedNumber == ownedNumber.DialedNumber).ToArray(), PurchasedPhoneNumbers = purchasedNumbers.Where(x => x.DialedNumber == ownedNumber.DialedNumber).ToArray() });
                }

                return View("OwnedNumbers", viewOrders.ToArray());
            }
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

            var portedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == number.DialedNumber).ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == number.DialedNumber).ToArrayAsync();
            return View("OwnedNumberEdit", new OwnedNumberResult { PurchasedPhoneNumbers = purchasedNumbers, PortedPhoneNumbers = portedNumbers, Owned = order });
        }
    }
}