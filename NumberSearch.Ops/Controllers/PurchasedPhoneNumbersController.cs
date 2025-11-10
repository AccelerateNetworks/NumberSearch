using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using nietras.SeparatedValues;

using NumberSearch.Ops.Models;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class PurchasedPhoneNumbersController(numberSearchContext context) : Controller
{
    [Authorize]
    [Route("/Home/NumberOrders")]
    [Route("/Home/NumberOrder/{orderId}")]
    [Route("/Home/NumberOrders/{dialedNumber}")]
    public async Task<IActionResult> NumberOrders(Guid? orderId, string dialedNumber)
    {
        var owned = await context.OwnedPhoneNumbers.AsNoTracking().ToArrayAsync();

        if (orderId.HasValue)
        {
            var orders = await context.PurchasedPhoneNumbers
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.DateOrdered)
                .AsNoTracking()
                .ToArrayAsync();

            return View("NumberOrders", new PurchasedResult { PurchasedPhoneNumbers = orders, Owned = owned });
        }
        else if (string.IsNullOrWhiteSpace(dialedNumber))
        {
            // Show all orders
            var orders = await context.PurchasedPhoneNumbers.OrderByDescending(x => x.DateOrdered).AsNoTracking().ToArrayAsync();

            return View("NumberOrders", new PurchasedResult { PurchasedPhoneNumbers = orders, Owned = owned });
        }
        else
        {
            var order = await context.PurchasedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

            return View("NumberOrders", new PurchasedResult { PurchasedPhoneNumber = order ?? new(), PurchasedPhoneNumbers = [order ?? new()], Owned = owned });
        }
    }

    [Authorize]
    [Route("/Home/ExportNumberOrders")]

    public async Task<IActionResult> ExportNumberOrders()
    {
        var orders = await context.PurchasedPhoneNumbers.OrderByDescending(x => x.DateOrdered).AsNoTracking().ToListAsync();

        var filePath = Path.GetFullPath(Path.Combine("wwwroot", "csv"));
        var fileName = $"PurchasedNumbers{DateTime.Now:yyyyMMdd}.csv";
        var completePath = Path.Combine(filePath, fileName);

        using var writer = Sep.New(',').Writer().ToText();

        foreach (var item in orders)
        {
            using var row = writer.NewRow();
            row["PurchasedPhoneNumberId"].Set(item.PurchasedPhoneNumberId.ToString());
            row["OrderId"].Set(item.OrderId.ToString());
            row["DialedNumber"].Set(item.DialedNumber);
            row["IngestedFrom"].Set(item.IngestedFrom);
            row["DateIngested"].Set(item.DateIngested.ToString());
            row["DateOrdered"].Set(item.DateOrdered.ToString());
            row["OrderResponse"].Set(item.OrderResponse);
            row["Completed"].Set(item.Completed.ToString());
            row["NPA"].Set(item.NPA.ToString());
            row["NXX"].Set(item.NXX.ToString());
            row["XXXX"].Set(item.XXXX.ToString());
            row["NumberType"].Set(item.NumberType);
            row["Pin"].Set(item.Pin);
        }

        return File(Encoding.UTF8.GetBytes(writer.ToString()), "text/csv", $"PurchasedPhoneNumbers{DateTime.UtcNow}.csv");
    }
}