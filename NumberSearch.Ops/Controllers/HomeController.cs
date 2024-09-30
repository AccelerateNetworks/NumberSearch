using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NumberSearch.Ops.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;
[ApiExplorerSettings(IgnoreApi = true)]

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _postgresql;
    private readonly string _emailUsername;
    private readonly string _emailPassword;
    private readonly numberSearchContext _context;

    public HomeController(
        IConfiguration config,
        numberSearchContext context)
    {
        _configuration = config;
        _emailUsername = config.GetConnectionString("SmtpUsername") ?? string.Empty;
        _emailPassword = config.GetConnectionString("SmtpPassword") ?? string.Empty;
        _postgresql = _configuration.GetConnectionString("PostgresqlProd") ?? string.Empty;
        _context = context;
    }

    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    [Route("/Home/NumbersToVerify")]
    [Route("/Home/NumbersToVerify/{orderId}")]
    public async Task<IActionResult> NumbersToVerify(Guid? orderId)
    {
        if (orderId.HasValue)
        {
            var orders = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == orderId).AsNoTracking().ToListAsync();

            if (orders is not null && orders.Count != 0)
            {
                foreach (var order in orders)
                {
                    // Update the product orders here.
                }
            }

            return View("NumbersToVerify", orders);
        }
        else
        {
            // Show all orders
            var orders = await _context.VerifiedPhoneNumbers.OrderByDescending(x => x.DateToExpire).AsNoTracking().ToListAsync();

            return View("NumbersToVerify", orders);
        }
    }

    [Authorize]
    [Route("/Home/E911")]
    [Route("/Home/E911/{dialedNumber}")]
    public async Task<IActionResult> AllEmergencyInformation(string dialedNumber)
    {
        if (string.IsNullOrWhiteSpace(dialedNumber))
        {
            // Show all orders
            var info = await _context.EmergencyInformation.OrderByDescending(x => x.DateIngested).AsNoTracking().ToListAsync();
            return View("E911", info);
        }
        else
        {
            var info = await _context.EmergencyInformation.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            return View("E911Edit", info);
        }
    }

    [Authorize]
    [HttpGet("/Home/Shipment/")]
    [HttpGet("/Home/Shipment/{ProductShipmentId}")]
    public async Task<IActionResult> ShipmentsAsync(Guid? ProductShipmentId)
    {
        if (ProductShipmentId is null || !ProductShipmentId.HasValue)
        {
            var products = await _context.Products.AsNoTracking().ToArrayAsync();
            var shipments = await _context.ProductShipments.AsNoTracking().ToArrayAsync();

            return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
        }
        else
        {
            var products = await _context.Products.ToArrayAsync();
            var checkExists = await _context.ProductShipments.AsNoTracking().FirstOrDefaultAsync(x => x.ProductShipmentId == ProductShipmentId);

            if (checkExists is not null)
            {
                return View("Shipments", new InventoryResult { Products = products, ProductShipments = [checkExists], Shipment = checkExists });
            }
            else
            {
                var shipments = await _context.ProductShipments.AsNoTracking().ToArrayAsync();

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
            }
        }
    }

    [Authorize]
    [Route("/Home/Shipment")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShipmentCreate(ProductShipment shipment)
    {
        if (shipment.ProductId == Guid.Empty)
        {
            return Redirect("/Home/Shipments");
        }
        else
        {
            shipment.DateCreated = DateTime.Now;
            var products = await _context.Products.ToArrayAsync();
            var checkExists = await _context.ProductShipments.FirstOrDefaultAsync(x => x.ProductShipmentId == shipment.ProductShipmentId);

            if (checkExists is null)
            {
                if (string.IsNullOrWhiteSpace(shipment.Name))
                {
                    shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault()?.Name;
                }
                _context.ProductShipments.Add(shipment);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(shipment.Name))
                {
                    shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault()?.Name;
                }
                _context.ProductShipments.Add(checkExists);
                await _context.SaveChangesAsync();
            }

            var shipments = await _context.ProductShipments.ToArrayAsync();

            // Update all product inventory counts when a shipment is added or updated.
            foreach (var product in products)
            {
                var relatedShipments = shipments.Where(x => x.ProductId == product.ProductId);
                var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                product.QuantityAvailable = instockItems - assignedItems;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
        }
    }

    [Authorize]
    [Route("/Home/Shipment/{productShipmentId}/Delete")]
    public async Task<IActionResult> ProductShipmentDelete(Guid productShipmentId)
    {
        if (productShipmentId == Guid.Empty)
        {
            return Redirect("/Home/Shipment");
        }
        else
        {
            var order = await _context.ProductShipments.FirstOrDefaultAsync(x => x.ProductShipmentId == productShipmentId);
            if (order is not null && order.ProductShipmentId == productShipmentId)
            {
                _context.ProductShipments.Remove(order);
                await _context.SaveChangesAsync();
            }

            return Redirect("/Home/Shipment");
        }
    }

    [Authorize]
    [HttpGet("/Home/Product/")]
    [HttpGet("/Home/Product/{ProductId}")]
    public async Task<IActionResult> ProductsAsync(Guid? ProductId)
    {
        if (ProductId is null || !ProductId.HasValue)
        {
            var products = await _context.Products.AsNoTracking().ToArrayAsync();

            return View("Products", new InventoryResult { Products = products });
        }
        else
        {
            var products = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == ProductId);

            return View("Products", new InventoryResult { Products = [products ?? new()], Product = products ?? new() });
        }
    }

    [Authorize]
    [Route("/Home/Product")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(Product product)
    {
        var checkExists = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == product.ProductId);

        if (checkExists is null)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Entry(checkExists).CurrentValues.SetValues(product);
            await _context.SaveChangesAsync();
        }

        var products = await _context.Products.ToArrayAsync();
        var shipments = await _context.ProductShipments.ToArrayAsync();

        return View("Products", new InventoryResult { Products = products, ProductShipments = shipments });
    }

    [Authorize]
    [Route("/Home/Product/{productId}/Delete")]
    public async Task<IActionResult> ProductDelete(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            return Redirect("/Home/Product");
        }
        else
        {
            var order = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == productId);

            if (order is not null && order.ProductId == productId)
            {
                _context.Products.Remove(order);
                await _context.SaveChangesAsync();
            }

            return Redirect("/Home/Product");
        }
    }

    [Authorize]
    [HttpGet]
    [Route("/Home/Coupons")]
    [Route("/Home/Coupons/{couponId}")]
    public async Task<IActionResult> Coupons(Guid? couponId)
    {
        if (couponId is null)
        {
            var results = await _context.Coupons.ToArrayAsync();

            return View("Coupons", new CouponResult { Coupons = results });
        }
        else
        {
            // Show all orders
            var result = await _context.Coupons.Where(x => x.CouponId == couponId).FirstOrDefaultAsync();

            return View("Coupons", new CouponResult { Coupon = result ?? new(), Coupons = [result ?? new Coupon()] });
        }
    }

    [Authorize]
    [HttpGet]
    [Route("/Home/Coupons/{couponId}/Delete")]
    public async Task<IActionResult> DeleteCouponAsync(Guid? couponId)
    {
        if (couponId is null)
        {
            var results = await _context.Coupons.ToArrayAsync();

            return View("Coupons", new CouponResult { Coupons = results });
        }
        else
        {
            var result = await _context.Coupons.FirstOrDefaultAsync(x => x.CouponId == couponId);

            if (result is not null)
            {
                _context.Coupons.Remove(result);
                await _context.SaveChangesAsync();
            }

            var results = await _context.Coupons.ToArrayAsync();

            return View("Coupons", new CouponResult { Coupons = results });
        }
    }


    [Authorize]
    [Route("/Home/Coupon")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CouponCreate(Coupon coupon)
    {
        var checkExists = await _context.Coupons.Where(x => x.Name == coupon.Name).FirstOrDefaultAsync();

        if (checkExists is null)
        {
            coupon.CouponId = Guid.NewGuid();
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
        }
        else
        {
            coupon.CouponId = checkExists.CouponId;
            _context.Entry(checkExists).CurrentValues.SetValues(coupon);
            await _context.SaveChangesAsync();
        }

        var coupons = await _context.Coupons.AsNoTracking().ToArrayAsync();

        return View("Coupons", new CouponResult { Coupons = coupons });
    }


    [Authorize]
    [Route("/Home/Emails")]
    [Route("/Home/Emails/{orderId}")]
    public async Task<IActionResult> Emails(Guid? orderId)
    {
        var response = new EmailResult();
        try
        {
            if (orderId is not null && orderId.HasValue)
            {
                response.Emails = await _context.SentEmails.OrderByDescending(x => x.DateSent).Where(x => x.OrderId == orderId).AsNoTracking().ToArrayAsync();
            }
            else
            {
                response.Emails = await _context.SentEmails.OrderByDescending(x => x.DateSent).Take(100).AsNoTracking().ToArrayAsync();
            }
        }
        catch (Exception ex)
        {
            response.Message = $"❌ Failed to find emails for {orderId}. {ex?.Message} {ex?.InnerException} {ex?.StackTrace}";
        }
        return View("Emails", response);
    }

    [Authorize]
    [HttpGet("/Home/Emails/{orderId}/Resend/{emailId}")]
    public async Task<IActionResult> ResendEmails(Guid orderId, Guid emailId)
    {
        var response = new EmailResult();
        try
        {
            var email = await _context.SentEmails.FirstOrDefaultAsync(x => x.EmailId == emailId);

            if (email is not null)
            {
                email.DoNotSend = false;
                email.Completed = false;

                // Directly send the email.
                NumberSearch.DataAccess.Email toSend = new()
                {
                    EmailId = email.EmailId,
                    OrderId = email.OrderId,
                    PrimaryEmailAddress = email.PrimaryEmailAddress,
                    SalesEmailAddress = email.SalesEmailAddress ?? string.Empty,
                    CarbonCopy = email.CarbonCopy,
                    Subject = email.Subject,
                    MessageBody = email.MessageBody,
                    DateSent = DateTime.Now,
                    Completed = email.Completed,
                    CalendarInvite = email.CalendarInvite ?? string.Empty,
                    DoNotSend = email.DoNotSend
                };

                var checkSending = await toSend.SendEmailAsync(_emailUsername, _emailPassword);

                if (checkSending)
                {
                    email.Completed = true;
                    email.DateSent = DateTime.Now;
                    await _context.SaveChangesAsync();
                    response.Message = $"✔️ Sent out EmailId {email.EmailId} to {email.PrimaryEmailAddress}.";
                    response.AlertType = "alert-success";
                }
                else
                {
                    response.Message = $"❌ Failed to send out EmailId {email.EmailId}.";
                }
            }
            else
            {
                response.Message = $"❌ Couldn't find and email with an EmailId of {emailId} for OrderId {orderId}.";
                response.AlertType = "alert-danger";
            }

            if (email?.OrderId is not null)
            {
                response.Emails = await _context.SentEmails.Where(x => x.OrderId == email.OrderId).ToArrayAsync();
            }
        }
        catch (Exception ex)
        {
            response.Message = $"❌ Couldn't find and email with an EmailId of {emailId} for OrderId {orderId}. {ex?.Message} {ex?.InnerException} {ex?.StackTrace}";
            response.AlertType = "alert-danger";
        }

        return View("Emails", response);
    }

    [Authorize]
    public async Task<IActionResult> Ingests(int cycle, string ingestedFrom, string enabled, string runNow)
    {
        if (cycle > 0 && cycle < 24 && !string.IsNullOrWhiteSpace(ingestedFrom) && (enabled == "Enabled" || enabled == "Disabled"))
        {
            var update = await _context.IngestCycles.FirstOrDefaultAsync(x => x.IngestedFrom != null && x.IngestedFrom.Contains(ingestedFrom));

            if (update is not null)
            {
                var ingestToUpdate = await _context.IngestCycles.FirstOrDefaultAsync(x => x.IngestCycleId == update.IngestCycleId);

                update.CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now;
                update.Enabled = enabled == "Enabled";
                update.RunNow = runNow == "true";
                update.LastUpdate = DateTime.Now;

                _context.Entry(ingestToUpdate!).CurrentValues.SetValues(update);
                await _context.SaveChangesAsync();
            }
            // Create new
            else
            {
                update = new IngestCycle
                {
                    CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now,
                    IngestedFrom = ingestedFrom,
                    Enabled = enabled == "Enabled",
                    RunNow = runNow == "true",
                    LastUpdate = DateTime.Now
                };

                _context.Entry(update).CurrentValues.SetValues(update);
                await _context.SaveChangesAsync();
            }
        }

        var ingests = await _context.IngestCycles.AsNoTracking().ToListAsync();

        return View("IngestConfiguration", ingests);
    }

    /// <summary>
    /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
    /// </summary>
    /// <param name="query"> A complete or partial phone number. </param>
    /// <returns> A view of nothing, or the result of the query. </returns>
    [Authorize]
    [Route("/Numbers/{Query}")]
    [Route("/Numbers/")]
    public async Task<IActionResult> Numbers(string query, int page = 1)
    {
        // Fail fast
        if (string.IsNullOrWhiteSpace(query))
        {
            return View("Numbers");
        }

        // Clean up the query.
        query = query.Trim();

        // Parse the query.
        var converted = new List<char>();
        foreach (var letter in query)
        {
            // Allow digits.
            if (char.IsDigit(letter))
            {
                converted.Add(letter);
            }
            // Allow stars.
            else if (letter == '*')
            {
                converted.Add(letter);
            }
            // Convert letters to digits.
            else if (char.IsLetter(letter))
            {
                converted.Add(LetterToKeypadDigit(letter));
            }
            // Drop everything else.
        }

        // Drop leading 1's to improve the copy/paste experiance.
        if (converted[0] == '1' && converted.Count >= 10)
        {
            converted.Remove('1');
        }

        var count = await DataAccess.PhoneNumber.NumberOfResultsInQuery(new string(converted.ToArray()), _postgresql).ConfigureAwait(false);

        var results = await DataAccess.PhoneNumber.RecommendedPaginatedSearchAsync(new string(converted.ToArray()), page, _postgresql).ConfigureAwait(false);

        return View("Numbers", new SearchResults
        {
            CleanQuery = new string(converted.ToArray()),
            NumberOfResults = count,
            Page = page,
            PhoneNumbers = results.ToArray(),
            Query = query
        });
    }

    public static char LetterToKeypadDigit(char letter)
    {
        // Map the chars to their keypad numerical values.
        return letter switch
        {
            '+' => '0',
            'a' or 'b' or 'c' => '2',
            'd' or 'e' or 'f' => '3',
            'g' or 'h' or 'i' => '4',
            'j' or 'k' or 'l' => '5',
            'm' or 'n' or 'o' => '6',
            'p' or 'q' or 'r' or 's' => '7',
            't' or 'u' or 'v' => '8',
            'w' or 'x' or 'y' or 'z' => '9',
            _ => '*',// The digit 1 isn't mapped to any chars on a phone keypad.
                     // If the char isn't mapped to anything, respect it's existence by mapping it to a wildcard.
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

