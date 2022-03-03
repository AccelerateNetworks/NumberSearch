using AccelerateNetworks.Operations;

using CsvHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

public class PurchasedPhoneNumbersController : Controller
{
    private readonly numberSearchContext _context;

    public PurchasedPhoneNumbersController(numberSearchContext context)
    {
        _context = context;
    }

    [Authorize]
    [Route("/Home/NumberOrders")]
    [Route("/Home/NumberOrder/{orderId}")]
    [Route("/Home/NumberOrders/{dialedNumber}")]
    public async Task<IActionResult> NumberOrders(Guid? orderId, string dialedNumber)
    {
        if (orderId.HasValue)
        {
            var orders = await _context.PurchasedPhoneNumbers
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.DateOrdered)
                .AsNoTracking()
                .ToListAsync();

            if (orders is not null && orders.Any())
            {
                foreach (var order in orders)
                {
                    // Update the product orders here.
                }
            }

            return View("NumberOrders", orders);
        }
        else if (string.IsNullOrWhiteSpace(dialedNumber))
        {
            // Show all orders
            var orders = await _context.PurchasedPhoneNumbers.OrderByDescending(x => x.DateOrdered).AsNoTracking().ToListAsync();

            return View("NumberOrders", orders);
        }
        else
        {
            var order = await _context.PurchasedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

            return View("NumberOrders", new List<PurchasedPhoneNumber> { order ?? new() });
        }
    }

    [Authorize]
    public async Task<IActionResult> ExportNumberOrders()
    {
        var orders = await _context.PurchasedPhoneNumbers.OrderByDescending(x => x.DateOrdered).AsNoTracking().ToListAsync();

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