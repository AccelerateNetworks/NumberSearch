using AccelerateNetworks.Operations;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleDynamics;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;
[ApiExplorerSettings(IgnoreApi = true)]
public class OrdersController : Controller
{
    private readonly string _invoiceNinjaToken;
    private readonly numberSearchContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly OpsConfig _config;

    public OrdersController(OpsConfig opsConfig,
        numberSearchContext context,
        UserManager<IdentityUser> userManager)
    {
        _config = opsConfig;
        _invoiceNinjaToken = opsConfig.InvoiceNinjaToken;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("/Orders/Search")]
    public async Task<JsonResult> SearchOrdersAsync(string query)
    {
        // Show all orders
        var orders = new List<Order>();

        // Show only the relevant Orders to a Sales rep.
        if (User.IsInRole("Sales") && !User.IsInRole("Support"))
        {
            var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);

            if (user is not null)
            {
                orders = await _context.Orders
                    .Where(x => (x.Quote != true) && (x.SalesEmail == user.Email))
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                orders = await _context.Orders
                    .Where(x => x.Quote != true)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        else
        {
            orders = await _context.Orders
                .Where(x => x.Quote != true)
                .OrderByDescending(x => x.DateSubmitted)
                .AsNoTracking()
                .ToListAsync();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Handle GUIDs
            if (query.Length is 36 && Guid.TryParse(query, out var guidOutput))
            {
                orders = orders.Where(x => x.OrderId == guidOutput).ToList();
            }

            if (orders.Count > 1)
            {
                var searchResults = orders.Where(x => x.BusinessName != null
                && x.BusinessName.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();

                // First and Last Name
              searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.FirstName)
                                && !string.IsNullOrWhiteSpace(x.LastName)
                                && $"{x.FirstName} {x.LastName}".ToLowerInvariant().Contains(query.ToLowerInvariant())));

                // Phone Number
                searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x?.ContactPhoneNumber)
                && x.ContactPhoneNumber.Contains(query)));

                orders = searchResults.Count > 0 ? searchResults : orders;
            }
        }

      return new JsonResult(orders.Select(x => x?.BusinessName ?? $"{x?.FirstName} {x?.LastName}").Distinct().Take(5).ToArray());
    }

    [Authorize]
    [Route("/Home/Order/")]
    [Route("/Home/Order/{orderId}")]
    public async Task<IActionResult> Orders(Guid? orderId)
    {
        if (orderId is null)
        {
            // Show all orders
            var orders = new List<Order>();
            var portRequests = await _context.PortRequests.AsNoTracking().ToArrayAsync();
            var productOrders = await _context.ProductOrders.AsNoTracking().ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var products = await _context.Products.AsNoTracking().ToArrayAsync();
            var services = await _context.Services.AsNoTracking().ToArrayAsync();
            var pairs = new List<OrderProducts>();

            // Show only the relevant Orders to a Sales rep.
            if (User.IsInRole("Sales") && !User.IsInRole("Support"))
            {
                var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);

                if (user is not null)
                {
                    orders = await _context.Orders
                        .Where(x => (x.Quote != true) && (x.SalesEmail == user.Email))
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
                else
                {
                    orders = await _context.Orders
                        .Where(x => x.Quote != true)
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
            else
            {
                orders = await _context.Orders
                    .Where(x => x.Quote != true)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }

            foreach (var order in orders)
            {
                var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
                var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                pairs.Add(new OrderProducts
                {
                    Order = order,
                    PortRequest = portRequest ?? new PortRequest(),
                    ProductOrders = orderProductOrders
                });
            }

            return View("Orders", new OrderResult
            {
                Orders = pairs.ToArray(),
                Products = products,
                Services = services,
                PortedPhoneNumbers = portedPhoneNumbers,
                PurchasedPhoneNumbers = purchasedNumbers,
                VerifiedPhoneNumbers = verifiedNumbers
            });
        }
        else
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order is null)
            {
                return View("OrderEdit", new EditOrderResult { Message = "An Order with OrderId {orderId} could not be found. 😭" });
            }
            else
            {
                var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();
                var portRequest = await _context.PortRequests.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

                // Find numbers registered for E911 service manually.
                var e911Registrations = new List<EmergencyInformation>();

                foreach (var number in purchasedPhoneNumbers)
                {
                    var match = await _context.EmergencyInformation.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == number.DialedNumber);
                    if (match is not null)
                    {
                        e911Registrations.Add(match);
                    }
                }

                foreach (var number in portedPhoneNumbers)
                {
                    var match = await _context.EmergencyInformation.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == number.PortedDialedNumber);
                    if (match is not null)
                    {
                        e911Registrations.Add(match);
                    }
                }

                var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                var products = new List<Product>();
                foreach (var productId in productsToGet)
                {
                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                    if (product is not null)
                    {
                        products.Add(product);
                    }
                }

                var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                var services = new List<Service>();
                foreach (var serviceId in servicesToGet)
                {
                    var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                    if (service is not null)
                    {
                        services.Add(service);
                    }
                }

                var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                var coupons = new List<Coupon>();
                foreach (var couponId in couponsToGet)
                {
                    var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                    if (coupon is not null)
                    {
                        coupons.Add(coupon);
                    }
                }

                var cart = new Cart
                {
                    Order = order,
                    PhoneNumbers = new List<PhoneNumber>(),
                    ProductOrders = productOrders,
                    Products = products,
                    Services = services,
                    Coupons = coupons,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                };

                return View("OrderEdit", new EditOrderResult { Order = order, PortRequest = portRequest ?? new(), ProductItems = productItems, EmergencyInformation = e911Registrations.ToArray(), Cart = cart });
            }
        }
    }

    [Authorize]
    [Route("/Home/Quotes/")]
    public async Task<IActionResult> Quotes()
    {
        // Show all orders
        var orders = new List<Order>();
        var portRequests = await _context.PortRequests.AsNoTracking().ToArrayAsync();
        var productOrders = await _context.ProductOrders.AsNoTracking().ToArrayAsync();
        var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToArrayAsync();
        var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToArrayAsync();
        var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToArrayAsync();
        var products = await _context.Products.AsNoTracking().ToArrayAsync();
        var services = await _context.Services.AsNoTracking().ToArrayAsync();
        var pairs = new List<OrderProducts>();

        // Show only the relevant Orders to a Sales rep.
        if (User.IsInRole("Sales") && !User.IsInRole("Support"))
        {
            var user = await _userManager.FindByNameAsync(User?.Identity?.Name ?? string.Empty);

            if (user is not null)
            {
                orders = await _context.Orders
                    .Where(x => x.Quote && (x.SalesEmail == user.Email))
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                orders = await _context.Orders.Where(x => x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        else
        {
            orders = await _context.Orders.Where(x => x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
        }

        foreach (var order in orders)
        {
            var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
            var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

            pairs.Add(new OrderProducts
            {
                Order = order,
                PortRequest = portRequest ?? new PortRequest(),
                ProductOrders = orderProductOrders
            });
        }

        return View("Quotes", new OrderResult
        {
            Orders = pairs.ToArray(),
            Products = products,
            Services = services,
            PortedPhoneNumbers = portedPhoneNumbers,
            PurchasedPhoneNumbers = purchasedNumbers,
            VerifiedPhoneNumbers = verifiedNumbers
        });
    }

    [Authorize]
    [Route("/Home/Order/{orderId}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrderUpdate([Bind("OrderId,FirstName,LastName,Email,Address,Address2,City,State,Zip,DateSubmitted,BusinessName,CustomerNotes,BillingClientId,BillingInvoiceId,Quote,BillingInvoiceReoccuringId,SalesEmail,BackgroundWorkCompleted,Completed,InstallDate,UpfrontInvoiceLink,ReoccuringInvoiceLink,OnsiteInstallation,AddressUnitType,AddressUnitNumber,UnparsedAddress,MergedOrderId,E911ServiceNumber,DateConvertedFromQuote,DateCompleted,ContactPhoneNumber,DateUpfrontInvoicePaid")] Order order)
    {
        if (order is null)
        {
            return Redirect("/Home/Order");
        }

        var existingOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

        try
        {
            // Format the address information
            Log.Information($"[Checkout] Parsing address data from {order.Address}");
            var addressParts = order?.UnparsedAddress?.Split(", ") ?? Array.Empty<string>();
            if (order is not null && addressParts is not null && addressParts.Length > 4)
            {
                order.Address = addressParts[0] ?? string.Empty;
                order.City = addressParts[1] ?? string.Empty;
                order.State = addressParts[2] ?? string.Empty;
                order.Zip = addressParts[3] ?? string.Empty;
                Log.Information($"[Checkout] Address: {order.Address} City: {order.City} State: {order.State} Zip: {order.Zip}");
            }
            else
            {
                Log.Error($"[Checkout] Failed automatic address formatting.");
            }

            // Fillout the address2 information from its components.
            if (order is not null && !string.IsNullOrWhiteSpace(order?.AddressUnitNumber))
            {
                order.Address2 = $"{order.AddressUnitType} {order.AddressUnitNumber}";
            }

            if (order is not null && existingOrder is not null)
            {
                order.DateSubmitted = existingOrder.DateSubmitted;
                if (order.Quote is false && existingOrder.Quote is true)
                {
                    order.DateConvertedFromQuote = DateTime.Now;
                }
                if (order.Completed is true && existingOrder.Completed is false)
                {
                    order.DateCompleted = DateTime.Now;
                }

                // Get fresh invoice links
                if (!string.IsNullOrWhiteSpace(order.BillingClientId) && !string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                {
                    var recurringInvoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(order.BillingClientId, _invoiceNinjaToken).ConfigureAwait(false);
                    var reoccurringLink = recurringInvoiceLinks.Where(x => x.id == order.BillingInvoiceReoccuringId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                    {
                        order.ReoccuringInvoiceLink = reoccurringLink;
                    }
                    else
                    {
                        var quoteLinks = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                        reoccurringLink = quoteLinks?.invitations.FirstOrDefault()?.link;
                        if (!string.IsNullOrWhiteSpace(reoccurringLink))
                        {
                            order.ReoccuringInvoiceLink = reoccurringLink;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(order.BillingClientId) && !string.IsNullOrWhiteSpace(order.BillingInvoiceId))
                {
                    try
                    {
                        var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(order.BillingClientId, _invoiceNinjaToken, false).ConfigureAwait(false);
                        var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == order.BillingInvoiceId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                        {
                            order.UpfrontInvoiceLink = oneTimeLink;
                        }
                        else
                        {
                            try
                            {
                                var quoteLinks = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                oneTimeLink = quoteLinks?.invitations.FirstOrDefault()?.link;
                                if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                {
                                    order.UpfrontInvoiceLink = oneTimeLink;
                                }
                            }
                            catch (FlurlHttpException ex)
                            {
                                if (ex.StatusCode is 404)
                                {
                                    return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync(), Cart = await GetOrderEditCartAsync(order), Message = $"Failed to update this order! 😠\r\nThe BillingInvoiceId {order.BillingInvoiceId} is invalid. Please update or delete it.", AlertType = "alert-danger" });
                                }
                                else
                                {
                                    throw ex;
                                }
                            }
                        }
                    }
                    catch (FlurlHttpException ex)
                    {
                        if (ex.StatusCode is 404)
                        {
                            return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync(), Cart = await GetOrderEditCartAsync(order), Message = $"Failed to update this order! 😠\r\nThe BillingClientId {order.BillingClientId} is invalid. Please update or delete it.", AlertType = "alert-danger" });
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }

                if (order.OnsiteInstallation != existingOrder.OnsiteInstallation)
                {
                    var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).ToListAsync();
                    var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                    var products = new List<Product>();
                    foreach (var productId in productsToGet)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == productId);
                        if (product is not null)
                        {
                            products.Add(product);
                        }
                    }

                    // Add the call out charge and install estimate to the Cart
                    var onsite = await _context.Products.FindAsync(Guid.Parse("b174c76a-e067-4a6a-abcf-53b6d3a848e4"));
                    var estimate = await _context.Products.FindAsync(Guid.Parse("a032b3ba-da57-4ad3-90ec-c59a3505b075"));

                    // Sum all of the install time estimates.
                    decimal totalInstallTime = 0m;
                    foreach (var item in products)
                    {
                        var quantity = productOrders?.Where(x => x.ProductId == item.ProductId).FirstOrDefault();

                        if (item.InstallTime > 0m && quantity is not null)
                        {
                            totalInstallTime += item.InstallTime * quantity.Quantity;
                        }
                    }

                    var productOrderOnsite = new ProductOrder
                    {
                        ProductOrderId = Guid.NewGuid(),
                        ProductId = onsite?.ProductId,
                        Quantity = 1,
                        OrderId = order.OrderId,
                    };

                    var productOrderEstimate = new ProductOrder
                    {
                        ProductOrderId = Guid.NewGuid(),
                        ProductId = estimate?.ProductId,
                        Quantity = decimal.ToInt32(Math.Ceiling(totalInstallTime)),
                        OrderId = order.OrderId,
                    };

                    // Add the install charges if they're not already in the Cart.
                    var checkOnsiteExists = productOrders.FirstOrDefault(x => x.ProductId == Guid.Parse("b174c76a-e067-4a6a-abcf-53b6d3a848e4"));
                    var checkEstimateExists = productOrders.FirstOrDefault(x => x.ProductId == Guid.Parse("a032b3ba-da57-4ad3-90ec-c59a3505b075"));

                    if (order.OnsiteInstallation)
                    {
                        if (checkOnsiteExists is null && checkEstimateExists is null)
                        {
                            _context.Add(productOrderOnsite);
                            _context.Add(productOrderEstimate);
                        }
                    }
                    else
                    {
                        // Remove the install charges as this is now a remote install.
                        _context.Remove(checkOnsiteExists ?? new());
                        _context.Remove(checkEstimateExists ?? new());
                    }
                }

                // Update the existing order
                _context.Update(order);
                await _context.SaveChangesAsync();

                var cart = await GetOrderEditCartAsync(order);
                var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();
                return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = productItems, Cart = cart, Message = "Order updated successfully! 😘", AlertType = "alert-success" });
            }
            else
            {
                return Redirect("/Home/Order");
            }
        }
        catch (Exception ex)
        {
            var cart = await GetOrderEditCartAsync(order);
            var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();
            return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = productItems, Cart = cart, Message = $"Failed to update this order! 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
        }
    }

    private async Task<Cart> GetOrderEditCartAsync(Order order)
    {
        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
        var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
        var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
        var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

        var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
        var products = new List<Product>();
        foreach (var productId in productsToGet)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
            if (product is not null)
            {
                products.Add(product);
            }
        }

        var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
        var services = new List<Service>();
        foreach (var serviceId in servicesToGet)
        {
            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
            if (service is not null)
            {
                services.Add(service);
            }
        }

        var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
        var coupons = new List<Coupon>();
        foreach (var couponId in couponsToGet)
        {
            var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
            if (coupon is not null)
            {
                coupons.Add(coupon);
            }
        }

        return new Cart
        {
            Order = order,
            PhoneNumbers = new List<PhoneNumber>(),
            ProductOrders = productOrders,
            Products = products,
            Services = services,
            Coupons = coupons,
            PortedPhoneNumbers = portedPhoneNumbers,
            VerifiedPhoneNumbers = verifiedPhoneNumbers,
            PurchasedPhoneNumbers = purchasedPhoneNumbers
        };
    }

    [Authorize]
    [Route("/Home/Order/{orderId}/Delete")]
    public async Task<IActionResult> OrderDeleteAsync(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return Redirect("/Home/Order");
        }
        else
        {
            var order = await _context.Orders.Where(x => x.OrderId == Guid.Parse(orderId)).FirstOrDefaultAsync();

            if (order is not null && order.OrderId == Guid.Parse(orderId))
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return Redirect("/Home/Order");
        }
    }

    [Authorize]
    [Route("/Home/Order/{orderId}/RegisterE911")]
    public async Task<IActionResult> RegisterE911Async(string serviceNumber, string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return Redirect("/Home/Order");
        }
        else
        {
            var order = await _context.Orders.Where(x => x.OrderId == Guid.Parse(orderId)).FirstOrDefaultAsync();

            if (order is not null)
            {
                var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
                Log.Information($"{JsonSerializer.Serialize(order)}");

                if (order is not null && order.OrderId == Guid.Parse(orderId))
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(serviceNumber ?? string.Empty, out var phoneNumber);
                    // Register the number for E911 service.
                    if (phoneNumber is not null && checkParse)
                    {
                        try
                        {
                            string[] addressChunks = order.Address?.Split(" ") ?? Array.Empty<string>();
                            string withoutUnitNumber = string.Join(" ", addressChunks[1..]);
                            var checkAddress = await E911Record.ValidateAddressAsync(addressChunks[0], withoutUnitNumber, order.Address2 ?? string.Empty,
                                order.City ?? string.Empty, order.State ?? string.Empty, order.Zip ?? string.Empty, _config.BulkVSUsername,
                                _config.BulkVSPassword);

                            if (checkAddress.Status is "GEOCODED" && !string.IsNullOrWhiteSpace(checkAddress.AddressID))
                            {
                                Log.Information(JsonSerializer.Serialize(checkAddress));

                                try
                                {
                                    var response = await E911Record.PostAsync($"1{phoneNumber.DialedNumber}",
                                        string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                        checkAddress.AddressID, Array.Empty<string>(), _config.BulkVSUsername, _config.BulkVSPassword);

                                    if (response.Status is "Success")
                                    {
                                        Log.Information(JsonSerializer.Serialize(response));
                                        order.E911ServiceNumber = response.TN;
                                        var emergencyRecord = new EmergencyInformation
                                        {
                                            AddressLine1 = response.AddressLine1,
                                            AddressLine2 = response.AddressLine2,
                                            BulkVSLastModificationDate = response.LastModification,
                                            CallerName = response.CallerName,
                                            RawResponse = JsonSerializer.Serialize(response),
                                            City = response.City,
                                            DateIngested = DateTime.Now,
                                            DialedNumber = phoneNumber.DialedNumber,
                                            Sms = response.Sms.Any() ? string.Join(',', response.Sms) : string.Empty,
                                            State = response.State,
                                            EmergencyInformationId = Guid.NewGuid(),
                                            IngestedFrom = "BulkVS",
                                            ModifiedDate = DateTime.Now,
                                            Zip = response.Zip
                                        };
                                        // Save the record to our database
                                        _context.EmergencyInformation.Add(emergencyRecord);

                                        // Updated the owned number that we registered for E911 service if it exists.
                                        var owned = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == phoneNumber.DialedNumber);

                                        if (owned is not null && owned.DialedNumber == phoneNumber.DialedNumber)
                                        {
                                            owned.EmergencyInformationId = emergencyRecord.EmergencyInformationId;
                                        }

                                        await _context.SaveChangesAsync();

                                        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                                        var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                                        var products = new List<Product>();
                                        foreach (var productId in productsToGet)
                                        {
                                            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                                            if (product is not null)
                                            {
                                                products.Add(product);
                                            }
                                        }

                                        var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                                        var services = new List<Service>();
                                        foreach (var serviceId in servicesToGet)
                                        {
                                            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                                            if (service is not null)
                                            {
                                                services.Add(service);
                                            }
                                        }

                                        var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                                        var coupons = new List<Coupon>();
                                        foreach (var couponId in couponsToGet)
                                        {
                                            var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                                            if (coupon is not null)
                                            {
                                                coupons.Add(coupon);
                                            }
                                        }

                                        var cart = new Cart
                                        {
                                            Order = order,
                                            PhoneNumbers = new List<PhoneNumber>(),
                                            ProductOrders = productOrders,
                                            Products = products,
                                            Services = services,
                                            Coupons = coupons,
                                            PortedPhoneNumbers = portedPhoneNumbers,
                                            VerifiedPhoneNumbers = verifiedPhoneNumbers,
                                            PurchasedPhoneNumbers = purchasedPhoneNumbers
                                        };

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully registered {phoneNumber.DialedNumber} with E911! 🥳", AlertType = "alert-success" });
                                    }
                                    else
                                    {
                                        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                        var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                                        var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                                        var products = new List<Product>();
                                        foreach (var productId in productsToGet)
                                        {
                                            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                                            if (product is not null)
                                            {
                                                products.Add(product);
                                            }
                                        }

                                        var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                                        var services = new List<Service>();
                                        foreach (var serviceId in servicesToGet)
                                        {
                                            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                                            if (service is not null)
                                            {
                                                services.Add(service);
                                            }
                                        }

                                        var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                                        var coupons = new List<Coupon>();
                                        foreach (var couponId in couponsToGet)
                                        {
                                            var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                                            if (coupon is not null)
                                            {
                                                coupons.Add(coupon);
                                            }
                                        }

                                        var cart = new Cart
                                        {
                                            Order = order,
                                            PhoneNumbers = new List<PhoneNumber>(),
                                            ProductOrders = productOrders,
                                            Products = products,
                                            Services = services,
                                            Coupons = coupons,
                                            PortedPhoneNumbers = portedPhoneNumbers,
                                            VerifiedPhoneNumbers = verifiedPhoneNumbers,
                                            PurchasedPhoneNumbers = purchasedPhoneNumbers
                                        };

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 {JsonSerializer.Serialize(response)}", AlertType = "alert-danger" });
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                    var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                    var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                    var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                                    var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                                    var products = new List<Product>();
                                    foreach (var productId in productsToGet)
                                    {
                                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                                        if (product is not null)
                                        {
                                            products.Add(product);
                                        }
                                    }

                                    var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                                    var services = new List<Service>();
                                    foreach (var serviceId in servicesToGet)
                                    {
                                        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                                        if (service is not null)
                                        {
                                            services.Add(service);
                                        }
                                    }

                                    var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                                    var coupons = new List<Coupon>();
                                    foreach (var couponId in couponsToGet)
                                    {
                                        var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                                        if (coupon is not null)
                                        {
                                            coupons.Add(coupon);
                                        }
                                    }

                                    var cart = new Cart
                                    {
                                        Order = order,
                                        PhoneNumbers = new List<PhoneNumber>(),
                                        ProductOrders = productOrders,
                                        Products = products,
                                        Services = services,
                                        Coupons = coupons,
                                        PortedPhoneNumbers = portedPhoneNumbers,
                                        VerifiedPhoneNumbers = verifiedPhoneNumbers,
                                        PurchasedPhoneNumbers = purchasedPhoneNumbers
                                    };

                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}", AlertType = "alert-danger" });
                                }
                            }
                            else
                            {
                                var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                                var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                                var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                                var products = new List<Product>();
                                foreach (var productId in productsToGet)
                                {
                                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                                    if (product is not null)
                                    {
                                        products.Add(product);
                                    }
                                }

                                var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                                var services = new List<Service>();
                                foreach (var serviceId in servicesToGet)
                                {
                                    var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                                    if (service is not null)
                                    {
                                        services.Add(service);
                                    }
                                }

                                var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                                var coupons = new List<Coupon>();
                                foreach (var couponId in couponsToGet)
                                {
                                    var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                                    if (coupon is not null)
                                    {
                                        coupons.Add(coupon);
                                    }
                                }

                                var cart = new Cart
                                {
                                    Order = order,
                                    PhoneNumbers = new List<PhoneNumber>(),
                                    ProductOrders = productOrders,
                                    Products = products,
                                    Services = services,
                                    Coupons = coupons,
                                    PortedPhoneNumbers = portedPhoneNumbers,
                                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                                };

                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Address {order.Address} {order.Address2} {order.City} {order.State} {order.Zip} failed to validate for E911 Service. {JsonSerializer.Serialize(checkAddress)}", AlertType = "alert-danger" });
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                            var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                            var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                            var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                            var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                            var products = new List<Product>();
                            foreach (var productId in productsToGet)
                            {
                                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                                if (product is not null)
                                {
                                    products.Add(product);
                                }
                            }

                            var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                            var services = new List<Service>();
                            foreach (var serviceId in servicesToGet)
                            {
                                var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                                if (service is not null)
                                {
                                    services.Add(service);
                                }
                            }

                            var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                            var coupons = new List<Coupon>();
                            foreach (var couponId in couponsToGet)
                            {
                                var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                                if (coupon is not null)
                                {
                                    coupons.Add(coupon);
                                }
                            }

                            var cart = new Cart
                            {
                                Order = order,
                                PhoneNumbers = new List<PhoneNumber>(),
                                ProductOrders = productOrders,
                                Products = products,
                                Services = services,
                                Coupons = coupons,
                                PortedPhoneNumbers = portedPhoneNumbers,
                                VerifiedPhoneNumbers = verifiedPhoneNumbers,
                                PurchasedPhoneNumbers = purchasedPhoneNumbers
                            };

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}", AlertType = "alert-danger" });
                        }
                    }
                    else
                    {
                        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                        var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                        var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                        var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                        var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                        var products = new List<Product>();
                        foreach (var productId in productsToGet)
                        {
                            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                            if (product is not null)
                            {
                                products.Add(product);
                            }
                        }

                        var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                        var services = new List<Service>();
                        foreach (var serviceId in servicesToGet)
                        {
                            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                            if (service is not null)
                            {
                                services.Add(service);
                            }
                        }

                        var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                        var coupons = new List<Coupon>();
                        foreach (var couponId in couponsToGet)
                        {
                            var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                            if (coupon is not null)
                            {
                                coupons.Add(coupon);
                            }
                        }

                        var cart = new Cart
                        {
                            Order = order,
                            PhoneNumbers = new List<PhoneNumber>(),
                            ProductOrders = productOrders,
                            Products = products,
                            Services = services,
                            Coupons = coupons,
                            PortedPhoneNumbers = portedPhoneNumbers,
                            VerifiedPhoneNumbers = verifiedPhoneNumbers,
                            PurchasedPhoneNumbers = purchasedPhoneNumbers
                        };

                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = "Failed to register with E911! 😠 The currently selected phone number is not a valid value.", AlertType = "alert-danger" });
                    }
                }
            }

            return Redirect("/Home/Order");
        }
    }

    [Authorize]
    [Route("/Home/Order/{orderId}/Merge")]
    public async Task<IActionResult> OrderMergeAsync(Guid orderId, Guid mergeId)
    {
        if (orderId == Guid.Empty || mergeId == Guid.Empty)
        {
            return Redirect("/Home/Order");
        }
        else
        {
            var parent = await _context.Orders.Where(x => x.OrderId == orderId).FirstOrDefaultAsync();
            var child = await _context.Orders.Where(x => x.OrderId == mergeId).FirstOrDefaultAsync();

            if (parent is not null && child is not null)
            {
                var parentProductOrders = await _context.ProductOrders.Where(x => x.OrderId == parent.OrderId).ToListAsync();
                var parentPurchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).ToListAsync();
                var parentVerifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).ToListAsync();
                var parentPortedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).ToListAsync();
                var parentPortRequests = await _context.PortRequests.Where(x => x.OrderId == parent.OrderId).ToListAsync();

                try
                {
                    var productOrders = await _context.ProductOrders.Where(x => x.OrderId == child.OrderId).ToListAsync();
                    var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == child.OrderId).ToListAsync();
                    var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == child.OrderId).ToListAsync();
                    var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == child.OrderId).ToListAsync();
                    var portRequests = await _context.PortRequests.Where(x => x.OrderId == child.OrderId).ToListAsync();

                    List<Guid> duplicateIds = new();

                    foreach (var item in purchasedPhoneNumbers)
                    {
                        var duplicate = parentPurchasedPhoneNumbers?.Where(x => x?.DialedNumber == item?.DialedNumber).FirstOrDefault();
                        if (duplicate is null)
                        {
                            item.OrderId = parent.OrderId;
                            _context.PurchasedPhoneNumbers.Update(item);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Remove duplicate product orders.
                            productOrders = productOrders.Where(x => x?.DialedNumber != item?.DialedNumber).ToList();
                        }
                    }

                    foreach (var item in verifiedPhoneNumbers)
                    {
                        var duplicate = parentVerifiedPhoneNumbers.Where(x => x?.VerifiedDialedNumber == item?.VerifiedDialedNumber).FirstOrDefault();
                        if (duplicate is null)
                        {
                            item.OrderId = parent.OrderId;
                            _context.VerifiedPhoneNumbers.Update(item);
                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in portedPhoneNumbers)
                    {
                        var duplicate = portedPhoneNumbers.Where(x => x?.PortedDialedNumber == item?.PortedDialedNumber).FirstOrDefault();
                        if (duplicate is null)
                        {
                            item.OrderId = parent.OrderId;
                            _context.PortedPhoneNumbers.Update(item);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Remove duplicate product orders.
                            productOrders = productOrders.Where(x => x?.PortedDialedNumber != item?.PortedDialedNumber).ToList();
                        }
                    }

                    foreach (var item in portRequests)
                    {
                        item.OrderId = parent.OrderId;
                        _context.PortRequests.Update(item);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var item in productOrders)
                    {
                        item.OrderId = parent.OrderId;
                        _context.ProductOrders.Update(item);
                        await _context.SaveChangesAsync();
                    }

                    // Redirect requests for the child order to the parent order it was merged into.
                    child.MergedOrderId = parent.OrderId;
                    _context.Orders.Update(child);
                    await _context.SaveChangesAsync();

                    productOrders = await _context.ProductOrders.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();

                    var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                    var products = new List<Product>();
                    foreach (var productId in productsToGet)
                    {
                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                        if (product is not null)
                        {
                            products.Add(product);
                        }
                    }

                    var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                    var services = new List<Service>();
                    foreach (var serviceId in servicesToGet)
                    {
                        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                        if (service is not null)
                        {
                            services.Add(service);
                        }
                    }

                    var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                    var coupons = new List<Coupon>();
                    foreach (var couponId in couponsToGet)
                    {
                        var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                        if (coupon is not null)
                        {
                            coupons.Add(coupon);
                        }
                    }

                    var cart = new Cart
                    {
                        Order = parent,
                        PhoneNumbers = new List<PhoneNumber>(),
                        ProductOrders = productOrders,
                        Products = products,
                        Services = services,
                        Coupons = coupons,
                        PortedPhoneNumbers = portedPhoneNumbers,
                        VerifiedPhoneNumbers = verifiedPhoneNumbers,
                        PurchasedPhoneNumbers = purchasedPhoneNumbers
                    };

                    return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Successfully merged {parent.OrderId} with {child.OrderId} 🥳", AlertType = "alert-success" });
                }
                catch (Exception ex)
                {
                    var productOrders = await _context.ProductOrders.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();
                    var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == parent.OrderId).AsNoTracking().ToListAsync();

                    var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                    var products = new List<Product>();
                    foreach (var productId in productsToGet)
                    {
                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                        if (product is not null)
                        {
                            products.Add(product);
                        }
                    }

                    var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                    var services = new List<Service>();
                    foreach (var serviceId in servicesToGet)
                    {
                        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                        if (service is not null)
                        {
                            services.Add(service);
                        }
                    }

                    var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                    var coupons = new List<Coupon>();
                    foreach (var couponId in couponsToGet)
                    {
                        var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                        if (coupon is not null)
                        {
                            coupons.Add(coupon);
                        }
                    }

                    var cart = new Cart
                    {
                        Order = parent,
                        PhoneNumbers = new List<PhoneNumber>(),
                        ProductOrders = productOrders,
                        Products = products,
                        Services = services,
                        Coupons = coupons,
                        PortedPhoneNumbers = portedPhoneNumbers,
                        VerifiedPhoneNumbers = verifiedPhoneNumbers,
                        PurchasedPhoneNumbers = purchasedPhoneNumbers
                    };

                    return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Failed to merge {parent.OrderId} with {child.OrderId} 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                }
            }
            else if (parent is not null)
            {
                var productOrders = await _context.ProductOrders.Where(x => x.OrderId == orderId).AsNoTracking().ToListAsync();
                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == orderId).AsNoTracking().ToListAsync();
                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == orderId).AsNoTracking().ToListAsync();
                var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == orderId).AsNoTracking().ToListAsync();

                var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                var products = new List<Product>();
                foreach (var productId in productsToGet)
                {
                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                    if (product is not null)
                    {
                        products.Add(product);
                    }
                }

                var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                var services = new List<Service>();
                foreach (var serviceId in servicesToGet)
                {
                    var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                    if (service is not null)
                    {
                        services.Add(service);
                    }
                }

                var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                var coupons = new List<Coupon>();
                foreach (var couponId in couponsToGet)
                {
                    var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                    if (coupon is not null)
                    {
                        coupons.Add(coupon);
                    }
                }

                var cart = new Cart
                {
                    Order = parent,
                    PhoneNumbers = new List<PhoneNumber>(),
                    ProductOrders = productOrders,
                    Products = products,
                    Services = services,
                    Coupons = coupons,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                };

                return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Failed to merge {parent.OrderId} with {child?.OrderId} 😠 The second orderId does not exist.", AlertType = "alert-warning" });
            }
            else
            {
                return Redirect("/Home/Order");
            }
        }
    }

    [Authorize]
    [Route("/Order/{orderId}/CreateProductItems")]
    public async Task<IActionResult> CreateProductItemsFromOrder(Guid? orderId, string teleDynamicsOrderNumber)
    {
        if (orderId is not null && orderId != Guid.Empty)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order is null)
            {
                return View("OrderEdit", new EditOrderResult { Message = $"An order with an Order Id of {orderId} could not be found. 😭" });
            }
            else
            {
                var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                string carrier = string.Empty;
                string trackingNumber = string.Empty;
                HardwareOrder.Orderline[] orderLines = Array.Empty<HardwareOrder.Orderline>();

                if (!string.IsNullOrWhiteSpace(teleDynamicsOrderNumber))
                {
                    var teleDynamicsOrders = await HardwareOrder.SearchByPONumberAsync(teleDynamicsOrderNumber, _config.TeleDynamicsUsername, _config.TeleDynamicsPassword);
                    if (teleDynamicsOrders is not null && teleDynamicsOrders.Length > 0)
                    {
                        var matchingOrder = teleDynamicsOrders.FirstOrDefault(x => x.OrderNumber == teleDynamicsOrderNumber);
                        if (matchingOrder is not null && matchingOrder.OrderNumber == teleDynamicsOrderNumber)
                        {
                            if (!string.IsNullOrWhiteSpace(matchingOrder.PONumber))
                            {
                                var fullOrders = await HardwareOrder.GetByPONumberAsync(matchingOrder.PONumber.Trim(), _config.TeleDynamicsUsername, _config.TeleDynamicsPassword);
                                if (fullOrders is not null && fullOrders.Length > 0)
                                {
                                    var matchingFullOrder = fullOrders.FirstOrDefault(x => x.OrderNumber == teleDynamicsOrderNumber);
                                    if (matchingFullOrder is not null && matchingFullOrder.OrderNumber == teleDynamicsOrderNumber)
                                    {
                                        carrier = matchingFullOrder.Shipping.Carrier;
                                        trackingNumber = matchingFullOrder.TrackingInformation.FirstOrDefault()?.TrackingNumber ?? string.Empty;
                                        orderLines = matchingFullOrder.OrderLines;
                                    }
                                }
                            }
                        }
                    }
                }

                var products = new List<Product>();

                foreach (var item in productOrders)
                {
                    if (item is not null && item.ProductId is not null && item.ProductId != Guid.Empty)
                    {
                        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == item.ProductId);
                        if (product is not null)
                        {
                            products.Add(product);

                            // If items already exist, do not create them twice.
                            if (!productItems.Any() && item?.Quantity is not null && item.Quantity > 0)
                            {
                                var matches = orderLines.FirstOrDefault(x => x.PartNumber.Contains(product.VendorPartNumber))?.SerializationInformation;

                                // Create the product items here to track the serial numbers and condition of the hardware.
                                for (var i = 0; i < item.Quantity; i++)
                                {
                                    if (matches is not null && matches.Length == item.Quantity)
                                    {
                                        var match = matches[i];
                                        string trackingLink = string.Empty;

                                        if (!string.IsNullOrWhiteSpace(trackingNumber) && carrier is "UPS")
                                        {
                                            trackingLink = $"https://www.ups.com/track?track=yes&trackNums={trackingNumber}&loc=en_US&requester=ST/trackdetails";
                                        }
                                        else if (!string.IsNullOrWhiteSpace(trackingNumber) && carrier is "FedEx")
                                        {
                                            trackingLink = $"https://www.fedex.com/fedextrack/?trknbr={trackingNumber}";
                                        }

                                        var productItem = new ProductItem
                                        {
                                            ProductId = product.ProductId,
                                            DateCreated = DateTime.Now,
                                            DateUpdated = DateTime.Now,
                                            OrderId = order.OrderId,
                                            ProductItemId = Guid.NewGuid(),
                                            MACAddress = match.MAC ?? string.Empty,
                                            SerialNumber = match.SerialNumber ?? string.Empty,
                                            Condition = "New",
                                            ShipmentTrackingLink = string.IsNullOrWhiteSpace(trackingLink) ? $"{carrier} - {trackingNumber}" : trackingLink,
                                            Carrier = carrier,
                                            TrackingNumber = trackingNumber,
                                            ExternalOrderId = teleDynamicsOrderNumber
                                        };
                                        _context.ProductItems.Add(productItem);
                                    }
                                    else
                                    {
                                        var productItem = new ProductItem
                                        {
                                            ProductId = product.ProductId,
                                            DateCreated = DateTime.Now,
                                            DateUpdated = DateTime.Now,
                                            OrderId = order.OrderId,
                                            ProductItemId = Guid.NewGuid(),
                                        };
                                        _context.ProductItems.Add(productItem);
                                    }
                                }
                            }
                            else if (productItems.Any() && item?.Quantity is not null && item.Quantity > 0)
                            {
                                // Update them with the current tracking info
                                List<DataAccess.TeleDynamics.HardwareOrder.Serializationinformation> devices = new();
                                foreach (var line in orderLines)
                                {
                                    foreach (var device in line.SerializationInformation)
                                    {
                                        devices.Add(device);
                                    }
                                }

                                foreach (var unit in productItems)
                                {
                                    var match = devices?.Where(x => !string.IsNullOrWhiteSpace(x.MAC) && x.MAC == unit.MACAddress).FirstOrDefault();
                                    match ??= devices?.Where(x => !string.IsNullOrWhiteSpace(x.SerialNumber) && x.SerialNumber == unit.SerialNumber).FirstOrDefault();
                                    if (match is not null)
                                    {
                                        string trackingLink = string.Empty;

                                        if (!string.IsNullOrWhiteSpace(trackingNumber) && carrier is "UPS")
                                        {
                                            trackingLink = $"https://www.ups.com/track?track=yes&trackNums={trackingNumber}&loc=en_US&requester=ST/trackdetails";
                                        }
                                        else if (!string.IsNullOrWhiteSpace(trackingNumber) && carrier is "FedEx")
                                        {
                                            trackingLink = $"https://www.fedex.com/fedextrack/?trknbr={trackingNumber}";
                                        }

                                        unit.SerialNumber = match.SerialNumber;
                                        unit.MACAddress = match.MAC;
                                        unit.Carrier = carrier;
                                        unit.TrackingNumber = trackingNumber;
                                        unit.Condition = "New";
                                        unit.ShipmentTrackingLink = string.IsNullOrWhiteSpace(trackingLink) ? $"{carrier} - {trackingNumber}" : trackingLink;
                                        unit.DateUpdated = DateTime.Now;
                                        unit.ExternalOrderId = teleDynamicsOrderNumber;
                                    }
                                }
                            }
                        }
                    }
                }

                var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                var services = new List<Service>();
                foreach (var serviceId in servicesToGet)
                {
                    var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                    if (service is not null)
                    {
                        services.Add(service);
                    }
                }

                var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                var coupons = new List<Coupon>();
                foreach (var couponId in couponsToGet)
                {
                    var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                    if (coupon is not null)
                    {
                        coupons.Add(coupon);
                    }
                }

                await _context.SaveChangesAsync();

                var cart = new Cart
                {
                    Order = order,
                    PhoneNumbers = new List<PhoneNumber>(),
                    ProductOrders = productOrders,
                    Products = products,
                    Services = services,
                    Coupons = coupons,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers,
                };

                productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, ProductItems = productItems, Message = $"Created {productItems.Length} Product Items for this order. 😀", AlertType = "alert-success" });
            }
        }
        else
        {
            return Redirect("/Home/Orders");
        }
    }

    [Authorize]
    [Route("/Home/Order/{orderId}/NewInvoices")]
    public async Task<IActionResult> OrderNewInvoicesAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return Redirect("/Home/Order");
        }
        else
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order is null)
            {
                return View("OrderEdit", new EditOrderResult { Message = $"An order with an Order Id of {orderId} could not be found. 😭" });
            }
            else
            {
                var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
                var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();
                var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                var productsToGet = productOrders.Where(x => x.ProductId is not null && x.ProductId != Guid.Empty).Select(x => x.ProductId).ToArray();
                var products = new List<Product>();
                foreach (var productId in productsToGet)
                {
                    var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId);
                    if (product is not null)
                    {
                        products.Add(product);
                    }
                }

                var servicesToGet = productOrders.Where(x => x.ServiceId is not null && x.ServiceId != Guid.Empty).Select(x => x.ServiceId).ToArray();
                var services = new List<Service>();
                foreach (var serviceId in servicesToGet)
                {
                    var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceId == serviceId);
                    if (service is not null)
                    {
                        services.Add(service);
                    }
                }

                var couponsToGet = productOrders.Where(x => x.CouponId is not null && x.CouponId != Guid.Empty).Select(x => x.CouponId).ToArray();
                var coupons = new List<Coupon>();
                foreach (var couponId in couponsToGet)
                {
                    var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == couponId);
                    if (coupon is not null)
                    {
                        coupons.Add(coupon);
                    }
                }

                var cart = new Cart
                {
                    Order = order,
                    PhoneNumbers = new List<PhoneNumber>(),
                    ProductOrders = productOrders,
                    Products = products,
                    Services = services,
                    Coupons = coupons,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                };

                if (cart is not null && cart.ProductOrders.Any())
                {
                    try
                    {
                        // Submit the number orders and track the total cost.
                        var onetimeItems = new List<Line_Items>();
                        var reoccurringItems = new List<Line_Items>();
                        var totalCost = 0;

                        var totalNumberPurchasingCost = 0;

                        foreach (var nto in cart.PurchasedPhoneNumbers)
                        {
                            var cost = nto.NumberType == "Executive" ? 200 : nto.NumberType == "Premium" ? 40 : nto.NumberType == "Standard" ? 20 : 20;

                            onetimeItems.Add(new Line_Items
                            {
                                product_key = nto.DialedNumber ?? string.Empty,
                                notes = $"{nto.NumberType} Phone Number",
                                cost = cost,
                                quantity = 1
                            });

                            totalNumberPurchasingCost += cost;
                        }

                        var totalPortingCost = 0;

                        foreach (var productOrder in cart.ProductOrders)
                        {
                            productOrder.OrderId = order.OrderId;

                            if (productOrder.PortedPhoneNumberId is not null)
                            {
                                var ported = cart.PortedPhoneNumbers.Where(x => x.PortedPhoneNumberId == productOrder.PortedPhoneNumberId).FirstOrDefault();

                                var calculatedCost = 20;

                                if (ported is not null)
                                {
                                    totalCost += calculatedCost;
                                    onetimeItems.Add(new Line_Items
                                    {
                                        product_key = ported.PortedDialedNumber,
                                        notes = $"Phone Number to Port to our Network",
                                        cost = calculatedCost,
                                        quantity = 1
                                    });
                                }

                                totalPortingCost += calculatedCost;
                            }

                            if (productOrder.VerifiedPhoneNumberId is not null && productOrder.VerifiedPhoneNumberId != Guid.Empty)
                            {
                                var verified = cart.VerifiedPhoneNumbers.Where(x => x.VerifiedPhoneNumberId == productOrder.VerifiedPhoneNumberId).FirstOrDefault();

                                if (verified != null)
                                {
                                    totalCost += 10;
                                    onetimeItems.Add(new Line_Items
                                    {
                                        product_key = verified.VerifiedDialedNumber,
                                        notes = $"Phone Number to Verify Daily",
                                        cost = 10,
                                        quantity = 1
                                    });
                                }
                            }

                            if (productOrder.ProductId is not null && productOrder.ProductId != Guid.Empty)
                            {
                                var product = cart.Products.Where(x => x.ProductId == productOrder.ProductId).FirstOrDefault();

                                if (product != null)
                                {
                                    _ = int.TryParse(product.Price, out var price);
                                    totalCost += price;
                                    onetimeItems.Add(new Line_Items
                                    {
                                        product_key = product.Name ?? string.Empty,
                                        notes = $"{product.Description}",
                                        cost = price,
                                        quantity = Convert.ToInt32(productOrder.Quantity)
                                    });
                                }
                            }

                            if (productOrder.ServiceId is not null && productOrder.ServiceId != Guid.Empty)
                            {
                                var service = cart.Services.Where(x => x.ServiceId == productOrder.ServiceId).FirstOrDefault();

                                if (service != null)
                                {
                                    _ = int.TryParse(service.Price, out var price);
                                    totalCost += price;
                                    reoccurringItems.Add(new Line_Items
                                    {
                                        product_key = service.Name ?? string.Empty,
                                        notes = $"{service.Description}",
                                        cost = price,
                                        quantity = Convert.ToInt32(productOrder.Quantity)
                                    });
                                }
                            }

                            // Apply coupon discounts
                            if (productOrder.CouponId is not null && productOrder.CouponId != Guid.Empty)
                            {
                                var coupon = cart.Coupons.Where(x => x.CouponId == productOrder.CouponId).FirstOrDefault();

                                if (coupon is not null)
                                {
                                    if (coupon.Type == "Port")
                                    {

                                        totalCost -= totalPortingCost;
                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name ?? string.Empty,
                                            notes = coupon.Description ?? string.Empty,
                                            cost = totalPortingCost * -1,
                                            quantity = 1
                                        });
                                    }
                                    else if (coupon.Name == "Install")
                                    {

                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description ?? string.Empty,
                                            cost = 60 * -1,
                                            quantity = 1
                                        });
                                    }
                                    else if (coupon.Type == "Number")
                                    {
                                        totalCost -= totalNumberPurchasingCost;
                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name ?? string.Empty,
                                            notes = coupon.Description ?? string.Empty,
                                            cost = totalNumberPurchasingCost * -1,
                                            quantity = 1
                                        });
                                    }
                                    else
                                    {
                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name ?? string.Empty,
                                            notes = coupon.Description ?? string.Empty,
                                            cost = coupon.Value * -1,
                                            quantity = 1
                                        });
                                    }
                                }
                            }
                        }

                        // Handle hardware installation scenarios, if hardware is in the order.
                        if (cart.Products.Any())
                        {
                            if (order.OnsiteInstallation)
                            {
                                onetimeItems.Add(new Line_Items
                                {
                                    product_key = "Onsite Hardware Installation",
                                    notes = $"We'll come visit you and get all your phones setup.",
                                    cost = 60,
                                    quantity = 1
                                });
                            }
                            else
                            {
                                onetimeItems.Add(new Line_Items
                                {
                                    product_key = "Remote Installation",
                                    notes = $"We'll walk you through getting all your phones setup virtually.",
                                    cost = 0,
                                    quantity = 1
                                });
                            }
                        }

                        // Handle the tax information for the invoice and fall back to simpler queries if we get failures.
                        NumberSearch.DataAccess.SalesTax? specificTaxRate = null;
                        if (order.State is "WA" || order.State is "Washington")
                        {
                            try
                            {
                                // Use our own API
                                specificTaxRate = await NumberSearch.DataAccess.SalesTax.GetLocalAPIAsync(order.Address ?? string.Empty, string.Empty, order.Zip ?? string.Empty).ConfigureAwait(false);
                            }
                            catch
                            {
                                Log.Fatal($"[Checkout] Failed to get the Sale Tax rate from the local API for {order.Address}, {order.Zip}.");
                            }

                            if (specificTaxRate is null)
                            {
                                try
                                {
                                    // Fall back to using the state's API
                                    specificTaxRate = await NumberSearch.DataAccess.SalesTax.GetAsync(order.Address ?? string.Empty, order.City ?? string.Empty, order.Zip ?? string.Empty).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate from the state's API for {order.City}, {order.Zip}.");
                                }
                            }
                        }

                        var billingTaxRate = new TaxRateDatum
                        {
                            name = "None",
                            rate = 0M
                        };

                        if (specificTaxRate is not null && specificTaxRate.loccode > 0 && specificTaxRate.loccode < 15 && string.IsNullOrWhiteSpace(specificTaxRate.rate?.name) && (order.State is "WA" || order.State is "Washington"))
                        {
                            var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                            var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                            var taxRateValue = specificTaxRate.rate1 * 100M;

                            var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();
                            if (billingTaxRate is null)
                            {
                                billingTaxRate = new TaxRateDatum
                                {
                                    name = taxRateName,
                                    rate = taxRateValue
                                };

                                var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            }

                            Log.Information($"[Checkout] {billingTaxRate.name} @ {billingTaxRate.rate}.");
                        }
                        else
                        {
                            Log.Information($"[Checkout] Failed to get the Tax Rate from WA State.");
                        }

                        // Just in case things go wrong.
                        if (billingTaxRate.rate > 15 || billingTaxRate.rate < 0)
                        {
                            billingTaxRate.rate = 0;
                        }

                        // Create a billing client and send out an invoice.
                        var billingClients = await Client.GetByEmailAsync(order.Email ?? string.Empty, _invoiceNinjaToken).ConfigureAwait(false);
                        var billingClient = billingClients.data.FirstOrDefault();

                        if (billingClient is null)
                        {
                            // Create a new client in the billing system.
                            var newBillingClient = new ClientDatum
                            {
                                name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                contacts = new ClientContact[] {
                                        new ClientContact {
                                            email = order.Email ?? string.Empty,
                                            first_name = order?.FirstName ?? string.Empty,
                                            last_name = order?.LastName ?? string.Empty
                                        }
                                    },
                                address1 = order?.Address ?? string.Empty,
                                address2 = order?.Address2 ?? string.Empty,
                                city = order?.City ?? string.Empty,
                                state = order?.State ?? string.Empty,
                                postal_code = order?.Zip ?? string.Empty
                            };

                            var newClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            newBillingClient.id = newClient.id;
                            var billingClientContact = newBillingClient.contacts.FirstOrDefault();
                            var newClientContact = newClient?.contacts?.FirstOrDefault();
                            if (billingClientContact is not null && newClientContact is not null)
                            {
                                billingClientContact.id = newClientContact.id;
                            }
                            billingClient = await newBillingClient.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            Log.Information($"[Checkout] Created billing client {billingClient.name}, {billingClient.id}.");
                        }
                        else
                        {
                            Log.Information($"[Checkout] Found billing client {billingClient.name}, {billingClient.id}.");
                        }

                        // Create the invoices for this order and submit it to the billing system.
                        var upfrontInvoice = new InvoiceDatum
                        {
                            id = billingClient.id,
                            line_items = onetimeItems.ToArray(),
                            tax_name1 = billingTaxRate.name,
                            tax_rate1 = billingTaxRate.rate,
                            client_id = billingClient.id
                        };

                        // If they want just a Quote, create a quote in the billing system, not an invoice.
                        if (order is not null && order.Quote)
                        {
                            // Mark the invoices as quotes.
                            upfrontInvoice.entity_type = "quote";
                            var reoccurringInvoice = new InvoiceDatum
                            {
                                client_id = billingClient.id,
                                line_items = reoccurringItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate,
                                entity_type = "quote",
                            };

                            var hiddenReoccurringInvoice = new ReccurringInvoiceDatum
                            {
                                client_id = billingClient.id,
                                line_items = reoccurringItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate,
                                entity_type = "recurringInvoice",
                                frequency_id = "5",
                                auto_bill = "always",
                                auto_bill_enabled = false,
                            };

                            string partialMessage = string.Empty;

                            // Submit them to the billing system if they have items.
                            if (upfrontInvoice.line_items.Any() && reoccurringInvoice.line_items.Any())
                            {
                                var BillingClientId = string.Empty;
                                var BillingInvoiceId = string.Empty;
                                var BillingInvoiceReoccuringId = string.Empty;

                                var createNewOneTimeInvoice = new InvoiceDatum();

                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId))
                                    {
                                        createNewOneTimeInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Error(error);
                                    Log.Warning("[Checkout] Failed to find existing onetime invoice in the billing system.");
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing onetime invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing onetime and reoccurring invoices in the billing system 😡";
                                }

                                // If it doesn't exist create it, otherwise update it.
                                if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id) || createNewOneTimeInvoice.id != order.BillingInvoiceId)
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice?.id) && string.IsNullOrWhiteSpace(createNewOneTimeInvoice?.client_id))
                                        {
                                            BillingInvoiceId = createNewOneTimeInvoice?.id;
                                            BillingClientId = createNewOneTimeInvoice?.client_id;
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Error(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Update the existing invoice.
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        createNewOneTimeInvoice = await createNewOneTimeInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice?.id) && string.IsNullOrWhiteSpace(createNewOneTimeInvoice?.client_id))
                                        {
                                            BillingInvoiceId = createNewOneTimeInvoice?.id;
                                            BillingClientId = createNewOneTimeInvoice?.client_id;
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to update the existing invoices in the billing system.");
                                        Log.Error(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update the existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                var createNewReoccurringInvoice = new InvoiceDatum();
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                                    {
                                        createNewReoccurringInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Warning("[Checkout] Failed to find existing reoccurring invoice in the billing system.");
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Error(error);
                                    Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find the existing  reoccurring invoice in the billing system 😡 {error}", AlertType = "alert-danger" });

                                }

                                if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice?.id) && string.IsNullOrWhiteSpace(createNewReoccurringInvoice?.client_id))
                                        {
                                            BillingInvoiceReoccuringId = createNewReoccurringInvoice?.id;
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create new invoices in the billing system.");
                                        Log.Error(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create new invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Update the existing invoice.
                                    try
                                    {
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice?.id) && string.IsNullOrWhiteSpace(createNewReoccurringInvoice?.client_id))
                                        {
                                            BillingInvoiceReoccuringId = createNewReoccurringInvoice?.id;
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to update the existing invoices in the billing system.");
                                        Log.Error(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update the existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = string.IsNullOrWhiteSpace(BillingClientId) ? order.BillingClientId : BillingClientId;
                                    order.BillingInvoiceId = string.IsNullOrWhiteSpace(BillingInvoiceId) ? order.BillingInvoiceId : BillingInvoiceId;
                                    order.BillingInvoiceReoccuringId = string.IsNullOrWhiteSpace(BillingInvoiceReoccuringId) ? order.BillingInvoiceReoccuringId : BillingInvoiceReoccuringId;

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(BillingClientId ?? string.Empty, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.Where(x => x.id == BillingInvoiceId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                    var reoccurringLink = invoiceLinks.Where(x => x.id == BillingInvoiceReoccuringId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccurringLink;
                                    }

                                    if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                    {
                                        order.UpfrontInvoiceLink = oneTimeLink;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Message);
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update the order with the new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }

                            }
                            else if (reoccurringInvoice.line_items.Any())
                            {
                                var createNewReoccurringInvoice = new InvoiceDatum();
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                                    {
                                        createNewReoccurringInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(error);
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing reoccurring invoices in the billing system 😡";
                                }

                                if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Update the existing invoice.
                                    try
                                    {
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewReoccurringInvoice.client_id, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccurringLink;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Fatal(ex.Message ?? "No message found.");
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update order with the new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }
                            }
                            else if (upfrontInvoice.line_items.Any())
                            {
                                var createNewOneTimeInvoice = new InvoiceDatum();

                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId))
                                    {
                                        createNewOneTimeInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    // TODO add failure message for better UX.
                                    var message = await ex.GetResponseStringAsync();
                                    Log.Fatal(message);
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                    // Suppress this because we want this process to continue along anyway.
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 Did someone manually delete them?!? {message}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing onetime invoices in the billing system 😡";
                                }

                                // If it doesn't exist create it, otherwise update it.
                                if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        var message = await ex.GetResponseStringAsync();
                                        Log.Fatal(message);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create new invoices in the billing system 😡 {message}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Update the existing invoice.
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        createNewOneTimeInvoice = await createNewOneTimeInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        var message = await ex.GetResponseStringAsync();
                                        Log.Fatal(message);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update existing invoices in the billing system 😡 {message}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                    {
                                        order.UpfrontInvoiceLink = oneTimeLink;
                                    }

                                }
                                catch (Exception ex)
                                {
                                    // TODO add failure message for better UX.
                                    Log.Fatal(ex.Message);
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update the order with the new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }
                            }

                            _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                            await _context.SaveChangesAsync();

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Generated the invoices for this quote! 🥳 {partialMessage}", AlertType = "alert-success" });
                        }
                        else
                        {
                            var reoccurringInvoice = new ReccurringInvoiceDatum
                            {
                                client_id = billingClient.id,
                                line_items = reoccurringItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate,
                                entity_type = "recurringInvoice",
                                frequency_id = "5",
                                auto_bill = "always",
                                auto_bill_enabled = true,
                            };

                            string partialMessage = string.Empty;

                            // Submit them to the billing system if they have items.
                            if (upfrontInvoice.line_items.Any() && reoccurringInvoice.line_items.Any() && order is not null)
                            {

                                var createNewOneTimeInvoice = new InvoiceDatum();
                                var createNewReoccurringInvoice = new ReccurringInvoiceDatum();

                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId) && !string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                                    {
                                        createNewOneTimeInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                        createNewReoccurringInvoice = await ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Fatal("[Checkout] Failed to find existing the invoices in the billing system.");
                                    Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(error);
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing onetime and reoccurring invoices in the billing system 😡";
                                }

                                if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        if (createNewOneTimeInvoice.tax_name1 != upfrontInvoice.tax_name1)
                                        {
                                            createNewOneTimeInvoice.tax_name1 = upfrontInvoice.tax_name1;
                                            createNewOneTimeInvoice.tax_rate1 = upfrontInvoice.tax_rate1;
                                        }
                                        createNewOneTimeInvoice = await createNewOneTimeInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                {
                                    try
                                    {
                                        // Create
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        // Update
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        if (createNewReoccurringInvoice.tax_name1 != reoccurringInvoice.tax_name1)
                                        {
                                            createNewReoccurringInvoice.tax_name1 = reoccurringInvoice.tax_name1;
                                            createNewReoccurringInvoice.tax_rate1 = reoccurringInvoice.tax_rate1;
                                        }
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    var recurringInvoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                    var reoccurringLink = recurringInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccurringLink;
                                    }

                                    if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                    {
                                        order.UpfrontInvoiceLink = oneTimeLink;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Fatal(ex.Message);
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update the order with the new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }
                            }
                            else if (reoccurringInvoice.line_items.Any() && order is not null)
                            {
                                var createNewReoccurringInvoice = new ReccurringInvoiceDatum();

                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                                    {
                                        createNewReoccurringInvoice = await ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(error);
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing reoccurring invoice in the billing system 😡";
                                }


                                if (string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to create invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to update the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewReoccurringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccurringLink;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Fatal(ex.Message);
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update order with new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }

                            }
                            else if (upfrontInvoice.line_items.Any() && order is not null)
                            {

                                var createNewOneTimeInvoice = new InvoiceDatum();

                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId))
                                    {
                                        createNewOneTimeInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(error);
                                    //return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    partialMessage = $"Failed to find existing onetime invoices in the billing system 😡";
                                }

                                if (string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        createNewOneTimeInvoice = await upfrontInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to find existing invoices in the billing system 😡 {error}", AlertType = "alert-danger" });
                                    }
                                }

                                try
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                    {
                                        order.UpfrontInvoiceLink = oneTimeLink;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Fatal(ex.Message);
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update order with new invoices. 😡 {ex.Message}", AlertType = "alert-danger" });
                                }
                            }

                            if (order is not null)
                            {
                                _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                await _context.SaveChangesAsync();
                            }

                            return View("OrderEdit", new EditOrderResult { Order = order ?? new(), Cart = cart, Message = $"Generated the invoices for this order! 🥳 {partialMessage}", AlertType = "alert-success" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                    }
                }
                else
                {
                    return View("OrderEdit", new EditOrderResult { Order = order, Message = $"Failed to regenerate the invoices for {order.OrderId}. Either the order could not be found or there are no Product Orders associated with this Order. 🤔", AlertType = "alert-danger" });
                }
            }
        }
    }

    [Authorize]
    [Route("/Order/InstallDates")]
    public async Task<IActionResult> InstallDatesAsync()
    {
        // Show all orders
        var orders = new List<Order>();

        // Show only the relevant Orders to a Sales rep.
        if (User.IsInRole("Sales") && !User.IsInRole("Support"))
        {
            var user = await _userManager.FindByNameAsync(User?.Identity?.Name ?? string.Empty);

            if (user is not null)
            {
                orders = await _context.Orders
                    .Where(x => x.Quote && (x.SalesEmail == user.Email))
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                orders = await _context.Orders.Where(x => !x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }
        else
        {
            orders = await _context.Orders.Where(x => !x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
        }

        return View("InstallDates", orders);
    }
}