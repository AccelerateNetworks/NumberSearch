using AccelerateNetworks.Operations;

using CsvHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.Models;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

            return View("NumberOrders", new PurchasedResult { PurchasedPhoneNumber = order, Owned = owned });
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

        using var writer = new StreamWriter(completePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(orders).ConfigureAwait(false);
        var file = new FileInfo(completePath);

        if (file.Exists)
        {
            return Redirect($"../csv/{file.Name}");
        }
        else
        {
            return View("NumberOrders", orders);
        }
    }

}