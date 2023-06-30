using AccelerateNetworks.Operations;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NuGet.Packaging;

using NumberSearch.Ops.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class OwnedNumbersController : Controller
{
    private readonly numberSearchContext _context;
    private readonly OpsConfig _config;

    public OwnedNumbersController(numberSearchContext context, OpsConfig opsConfig)
    {
        _context = context;
        _config = opsConfig;
    }

    public class ClientRegistration
    {
        [Key]
        public Guid ClientRegistrationId { get; set; } = Guid.NewGuid();
        [DataType(DataType.PhoneNumber)]
        public string AsDialed { get; set; } = string.Empty;
        [DataType(DataType.Url)]
        public string CallbackUrl { get; set; } = string.Empty;
        [DataType(DataType.DateTime)]
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;
    }

    [Authorize]
    [Route("/Home/OwnedNumbers")]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    public async Task<IActionResult> OwnedNumbers(string dialedNumber)
    {
        if (!string.IsNullOrWhiteSpace(dialedNumber))
        {
            var owned = await _context.OwnedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            if (owned is not null && owned.DialedNumber == dialedNumber)
            {
                var orderIds = new List<Guid>();
                var localPortedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == dialedNumber).ToArrayAsync();
                var localPurchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == dialedNumber).ToArrayAsync();
                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

                // Get the orderIds for all the related orders.
                var portedOrders = localPortedNumbers.Where(x => x.OrderId.HasValue && x.OrderId != Guid.Empty).Select(x => x.OrderId.Value).ToList();
                orderIds.AddRange(portedOrders);
                var purchasedOrders = localPurchasedNumbers.Where(x => x.OrderId != Guid.Empty).Select(x => x.OrderId).ToList();
                orderIds.AddRange(purchasedOrders);

                var relatedOrders = new List<Order>();

                foreach (var id in orderIds.Distinct())
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
                    if (order is not null && order.OrderId == id)
                    {
                        relatedOrders.Add(order);
                    }
                }

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    PurchasedPhoneNumbers = localPurchasedNumbers,
                    PortedPhoneNumbers = localPortedNumbers,
                    Owned = owned,
                    EmergencyInformation = e911 ?? new(),
                    RelatedOrders = relatedOrders.ToArray(),
                });
            }

        }

        // Show all orders
        var ownedNumbers = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
        var portedNumbers = await _context.PortedPhoneNumbers.ToArrayAsync();
        var purchasedNumbers = await _context.PurchasedPhoneNumbers.ToArrayAsync();
        var e911s = await _context.EmergencyInformation.ToArrayAsync();
        var registeredNumbers = await "https://sms.callpipe.com/client/all"
            .WithOAuthBearerToken(_config.MessagingAPIJWT)
            .GetJsonAsync<ClientRegistration[]>();

        var viewOrders = new List<OwnedNumberResult>();
        foreach (var ownedNumber in ownedNumbers)
        {
            viewOrders.Add(new OwnedNumberResult
            {
                EmergencyInformation = e911s.FirstOrDefault(x => x.DialedNumber == ownedNumber.DialedNumber) ?? new(),
                Owned = ownedNumber,
                PortedPhoneNumbers = portedNumbers.Where(x => x.PortedDialedNumber == ownedNumber.DialedNumber).ToArray(),
                PurchasedPhoneNumbers = purchasedNumbers.Where(x => x.DialedNumber == ownedNumber.DialedNumber).ToArray(),
                ClientRegistration = registeredNumbers.FirstOrDefault(x => x.AsDialed == ownedNumber.DialedNumber) ?? new()
            });
        }

        return View("OwnedNumbers", viewOrders.ToArray());
    }

    [Authorize]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OwnedNumberUpdate(OwnedNumberResult number)
    {
        if (number is null)
        {
            return Redirect("/Home/OwnedNumbers");
        }
        else
        {
            var existing = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.Owned.DialedNumber);
            if (existing is not null)
            {
                existing.Notes = number.Owned.Notes;
                existing.OwnedBy = number.Owned.OwnedBy;
                existing.BillingClientId = number.Owned.BillingClientId;
                existing.Active = number.Owned.Active;

                var orderToUpdate = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.Owned.DialedNumber);
                await _context.SaveChangesAsync();
            }

            var portedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == number.Owned.DialedNumber).ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == number.Owned.DialedNumber).ToArrayAsync();
            return View("OwnedNumberEdit", new OwnedNumberResult { PurchasedPhoneNumbers = purchasedNumbers, PortedPhoneNumbers = portedNumbers, Owned = existing });
        }
    }
}