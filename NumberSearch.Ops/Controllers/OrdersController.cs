using AccelerateNetworks.Operations;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeliMessage;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;
public class OrdersController : Controller
{
    private readonly Guid _teleToken;
    private readonly string _invoiceNinjaToken;
    private readonly numberSearchContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public OrdersController(OpsConfig opsConfig,
        numberSearchContext context,
        UserManager<IdentityUser> userManager)
    {
        _invoiceNinjaToken = opsConfig.InvoiceNinjaToken;
        _teleToken = Guid.Parse(opsConfig.TeleAPI);
        _context = context;
        _userManager = userManager;
    }

    [Authorize]
    [Route("/")]
    [Route("/Home/Order/")]
    [Route("/Home/Order/{orderId}")]
    public async Task<IActionResult> Orders(Guid? orderId)
    {
        if (orderId is null)
        {
            // Show all orders
            var orders = new List<Order>();
            var portRequests = await _context.PortRequests.AsNoTracking().ToListAsync();
            var productOrders = await _context.ProductOrders.AsNoTracking().ToListAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToListAsync();
            var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToListAsync();
            var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToListAsync();
            var products = await _context.Products.AsNoTracking().ToListAsync();
            var services = await _context.Services.AsNoTracking().ToListAsync();
            var pairs = new List<OrderProducts>();

            // Show only the relevant Orders to a Sales rep.
            if (User.IsInRole("Sales"))
            {
                var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);

                if (user is not null)
                {
                    orders = await _context.Orders
                        .Where(x => (x.Quote != true) && (x.SalesEmail == user.Email))
                        .OrderByDescending(x => x.DateSubmitted)
                        .Take(100)
                        .AsNoTracking()
                        .ToListAsync();
                }
                else
                {
                    orders = await _context.Orders
                        .Where(x => x.Quote != true)
                        .OrderByDescending(x => x.DateSubmitted)
                        .Take(100)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
            else
            {
                orders = await _context.Orders
                    .Where(x => x.Quote != true)
                    .OrderByDescending(x => x.DateSubmitted)
                    .Take(100)
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
                Orders = pairs,
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

                return View("OrderEdit", new EditOrderResult { Order = order, PortRequest = portRequest, ProductItems = productItems, Cart = cart });
            }
        }
    }

    [Authorize]
    [Route("/Home/Quotes/")]
    public async Task<IActionResult> Quotes()
    {
        // Show all orders
        var orders = new List<Order>();
        var portRequests = await _context.PortRequests.AsNoTracking().ToListAsync();
        var productOrders = await _context.ProductOrders.AsNoTracking().ToListAsync();
        var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToListAsync();
        var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToListAsync();
        var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToListAsync();
        var products = await _context.Products.AsNoTracking().ToListAsync();
        var services = await _context.Services.AsNoTracking().ToListAsync();
        var pairs = new List<OrderProducts>();

        // Show only the relevant Orders to a Sales rep.
        if (User.IsInRole("Sales"))
        {
            var user = await _userManager.FindByNameAsync(User?.Identity?.Name);

            if (user is not null)
            {
                orders = await _context.Orders
                    .Where(x => x.Quote && (x.SalesEmail == user.Email))
                    .OrderByDescending(x => x.DateSubmitted)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                orders = await _context.Orders.Where(x => x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .Take(100)
                    .ToListAsync();
            }
        }
        else
        {
            orders = await _context.Orders.Where(x => x.Quote)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .Take(100)
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
            Orders = pairs,
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
    public async Task<IActionResult> OrderUpdate(Order order)
    {
        if (order is null)
        {
            return Redirect("/Home/Order");
        }

        var existingOrder = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

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
                Log.Error($"[Checkout] Failed automatic address formating.");
            }

            // Fillout the address2 information from its components.
            if (order is not null && !string.IsNullOrWhiteSpace(order?.AddressUnitNumber))
            {
                order.Address2 = $"{order.AddressUnitType} {order.AddressUnitNumber}";
            }

            if (order is not null && existingOrder is not null)
            {
                order.DateSubmitted = existingOrder.DateSubmitted;
                _context.Entry(existingOrder).CurrentValues.SetValues(order);
                await _context.SaveChangesAsync();

                var productOrders = await _context.ProductOrders.AsNoTracking().Where(x => x.OrderId == order.OrderId).ToListAsync();
                var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().Where(x => x.OrderId == order.OrderId).ToListAsync();
                var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().Where(x => x.OrderId == order.OrderId).ToListAsync();
                var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().Where(x => x.OrderId == order.OrderId).ToListAsync();
                var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();

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

                return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = productItems, Cart = cart, Message = "Order updated successfully! 😘", AlertType = "alert-success" });
            }
            else
            {
                return Redirect("/Home/Order");
            }
        }
        catch (Exception ex)
        {
            var productOrders = await _context.ProductOrders.Where(x => x.OrderId == order.OrderId).ToListAsync();
            var purchasedPhoneNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();
            var verifiedPhoneNumbers = await _context.VerifiedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();
            var portedPhoneNumbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();
            var productItems = await _context.ProductItems.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();

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

            return View("OrderEdit", new EditOrderResult { Order = order, ProductItems = productItems, Cart = cart, Message = $"Failed to update this order! 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
        }
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
                    // Register the number with Teli for E911 service.
                    if (phoneNumber is not null && checkParse)
                    {
                        var existingRegistration = await NumberSearch.DataAccess.EmergencyInfo.GetAsync(phoneNumber.DialedNumber, _teleToken);

                        if (existingRegistration.code == 200)
                        {
                            // This number is already registered with Teli.
                            Log.Information($"[RegisterE911] E911 Service number {existingRegistration.data.did_number} is already register with Teli.");

                            // Save the number to the order.
                            if (!string.IsNullOrWhiteSpace(serviceNumber) && existingRegistration.data.did_number == phoneNumber.DialedNumber)
                            {
                                order.E911ServiceNumber = phoneNumber.DialedNumber;
                                Log.Information($"[RegisterE911] E911 Service Number: {order.E911ServiceNumber}");

                                _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                await _context.SaveChangesAsync();
                            }

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

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"The currently selected phone number {existingRegistration.data.did_number} is already registered for E911 service. 🤔", AlertType = "alert-warning" });
                        }
                        else
                        {
                            // Register the number with Teli for E911 service.
                            var checkAddressValid = await NumberSearch.DataAccess.EmergencyInfo.ValidateAddressAsync(order.Address, order.City, order.State, order.Zip, _teleToken);

                            if (checkAddressValid.code == 200)
                            {
                                // With a valid address we can now register the address to the phone number selected for this account.
                                var checkNumber = await UserDidsGet.GetAsync(phoneNumber.DialedNumber, _teleToken);
                                if (checkNumber is null)
                                {
                                    // Check if the number can be registered as an offnet number with Teli.
                                    var checkOffnet = await DidsOffnet.VerifyCapabilityAsync(phoneNumber.DialedNumber, _teleToken);
                                    if (checkOffnet.code == 200)
                                    {
                                        // Insert the offnet number.
                                        var offnetInsertJob = await DidsOffnet.SubmitNumberAsync(phoneNumber.DialedNumber, _teleToken);

                                        // Check the status of the job until it completes.
                                        if (!string.IsNullOrWhiteSpace(offnetInsertJob?.data?.jobid))
                                        {
                                            var checkJobStatus = await DidsOffnet.StatusSubmitNumberAsync(offnetInsertJob.data.jobid, _teleToken);

                                            while (checkJobStatus.status != "success")
                                            {
                                                await Task.Delay(1000);
                                                checkJobStatus = await DidsOffnet.StatusSubmitNumberAsync(offnetInsertJob.data.jobid, _teleToken);

                                                // Bail out if the job has failed.
                                                if (checkJobStatus.code == 500)
                                                {
                                                    // Number cannot be register with Teli as an offnet number.
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

                                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and the job to register it failed. {checkJobStatus?.data}", AlertType = "alert-danger" });
                                                }
                                            }

                                            // If the job completed successfully get the did_id.
                                            var didDetails = await DidsGet.GetAsync(phoneNumber.DialedNumber, _teleToken);
                                            if (didDetails?.code == 200 && !string.IsNullOrWhiteSpace(didDetails?.data?.id))
                                            {
                                                var fullName = $"{order.FirstName} {order.LastName}";
                                                NumberSearch.DataAccess.EmergencyInfo? E911Request = null;
                                                // Request E911 service for this number at the previously validated address.
                                                if (string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                                                {
                                                    E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(didDetails.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);

                                                }
                                                else
                                                {
                                                    E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(didDetails.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                                }

                                                if (E911Request is not null && E911Request?.code == 200)
                                                {
                                                    // Congrats we did it!
                                                    // Save the number to the order.
                                                    if (!string.IsNullOrWhiteSpace(serviceNumber))
                                                    {
                                                        order.E911ServiceNumber = serviceNumber;
                                                        Log.Information($"[RegisterE911] E911 Service Number: {order.E911ServiceNumber}");

                                                        _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                                        await _context.SaveChangesAsync();
                                                    }

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
                                                    // Failed to get the did_id.
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

                                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we failed to register an address against its did_id. {E911Request?.error}", AlertType = "alert-danger" });
                                                }
                                            }
                                            else
                                            {
                                                // Failed to get the did_id.
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

                                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we could not lookup it's did_id. {didDetails?.ErrorData}", AlertType = "alert-danger" });
                                            }

                                        }
                                        else
                                        {
                                            // Number cannot be register with Teli as an offnet number.
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

                                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and cannot be registered. {offnetInsertJob?.error}", AlertType = "alert-danger" });
                                        }
                                    }
                                    else
                                    {
                                        // Number cannot be register with Teli as an offnet number.
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

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and cannot be registered. {checkOffnet?.error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Get the did_id and use it to register the number.
                                    if (checkNumber?.code == 200 && !string.IsNullOrWhiteSpace(checkNumber?.data?.id))
                                    {
                                        var fullName = $"{order.FirstName} {order.LastName}";
                                        NumberSearch.DataAccess.EmergencyInfo? E911Request = null;
                                        // Request E911 service for this number at the previously validated address.
                                        if (string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                                        {
                                            E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            }

                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            }
                                        }
                                        else
                                        {
                                            E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            }

                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await NumberSearch.DataAccess.EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            }
                                        }

                                        if (E911Request is not null && E911Request?.code == 200)
                                        {
                                            // Congrats we did it!
                                            // Save the number to the order.
                                            if (!string.IsNullOrWhiteSpace(serviceNumber))
                                            {
                                                order.E911ServiceNumber = serviceNumber;
                                                Log.Information($"[RegisterE911] E911 Service Number: {order.E911ServiceNumber}");

                                                _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                                await _context.SaveChangesAsync();
                                            }

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
                                            // Failed to get the did_id.
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

                                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we failed to register an address against its did_id. {E911Request?.error}", AlertType = "alert-danger" });
                                        }
                                    }
                                    else
                                    {
                                        // Failed to get the did_id.
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

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we could not lookup it's did_id. {checkNumber?.status}", AlertType = "alert-danger" });
                                    }
                                }
                            }
                            else
                            {
                                // Address is invalid, inform the user.
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

                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the Address associated with this Order is invalid. {checkAddressValid?.data}", AlertType = "alert-danger" });
                            }
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
    public async Task<IActionResult> CreateProductItemsFromOrder(Guid? orderId)
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
                                // Create the product items here to track the serial numbers and condition of the hardware.
                                for (var i = 0; i < item.Quantity; i++)
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
                                product_key = nto.DialedNumber,
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
                                        product_key = product.Name,
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
                                        product_key = service.Name,
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
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = totalPortingCost * -1,
                                            quantity = 1
                                        });
                                    }
                                    else if (coupon.Name == "Install")
                                    {

                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = 60 * -1,
                                            quantity = 1
                                        });
                                    }
                                    else if (coupon.Type == "Number")
                                    {
                                        totalCost -= totalNumberPurchasingCost;
                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = totalNumberPurchasingCost * -1,
                                            quantity = 1
                                        });
                                    }
                                    else
                                    {
                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
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
                        try
                        {
                            // Use our own API
                            specificTaxRate = await NumberSearch.DataAccess.SalesTax.GetLocalAPIAsync(order.Address, string.Empty, order.Zip).ConfigureAwait(false);
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
                                specificTaxRate = await NumberSearch.DataAccess.SalesTax.GetAsync(order.Address, order.City, order.Zip).ConfigureAwait(false);
                            }
                            catch
                            {
                                Log.Fatal($"[Checkout] Failed to get the Sale Tax rate from the state's API for {order.City}, {order.Zip}.");
                            }
                        }

                        var billingTaxRate = new TaxRateDatum();

                        if (specificTaxRate is not null && specificTaxRate.rate is not null)
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

                        // Create a billing client and send out an invoice.
                        var billingClients = await Client.GetByEmailAsync(order.Email, _invoiceNinjaToken).ConfigureAwait(false);
                        var billingClient = billingClients.data.FirstOrDefault();

                        if (billingClient is null)
                        {
                            // Create a new client in the billing system.
                            var newBillingClient = new ClientDatum
                            {
                                name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                contacts = new ClientContact[] {
                                        new ClientContact {
                                            email = order.Email,
                                            first_name = order.FirstName,
                                            last_name = order.LastName
                                        }
                                    },
                                address1 = order.Address,
                                address2 = order.Address2,
                                city = order.City,
                                state = order.State,
                                postal_code = order.Zip
                            };

                            var newClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            newBillingClient.id = newClient.id;
                            newBillingClient.contacts.FirstOrDefault().id = newClient?.contacts?.FirstOrDefault()?.id;
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
                            tax_rate1 = billingTaxRate.rate
                        };

                        // If they want just a Quote, create a quote in the billing system, not an invoice.
                        if (order.Quote)
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

                            // Submit them to the billing system if they have items.
                            if (upfrontInvoice.line_items.Any() && reoccurringInvoice.line_items.Any())
                            {
                                InvoiceDatum createNewOneTimeInvoice = null;
                                InvoiceDatum createNewReoccurringInvoice = null;

                                // TODO: These need to be quotes, not invoices.

                                try
                                {
                                    createNewOneTimeInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                }
                                catch (FlurlHttpException ex)
                                {
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(JsonSerializer.Serialize(error));
                                    Log.Warning("[Checkout] Failed to find existing onetime invoice in the billing system.");
                                }

                                try
                                {
                                    createNewReoccurringInvoice = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                }
                                catch (FlurlHttpException ex)
                                {
                                    var error = await ex.GetResponseStringAsync();
                                    Log.Fatal(JsonSerializer.Serialize(error));
                                    Log.Warning("[Checkout] Failed to find existing reoccurring invoice in the billing system.");
                                }

                                // If it doesn't exist create it, otherwise update it.
                                if (createNewOneTimeInvoice is null)
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
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
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    }
                                }

                                if (createNewReoccurringInvoice is null)
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
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
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    }
                                }

                                if (createNewOneTimeInvoice is not null && createNewReoccurringInvoice is not null)
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);

                                    _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                                    await _context.SaveChangesAsync();

                                    var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, order.Quote).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                    var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccurringLink;
                                    }

                                    if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                    {
                                        order.UpfrontInvoiceLink = oneTimeLink;
                                    }
                                }
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (reoccurringInvoice.line_items.Any())
                            {
                                // Submit them to the billing system.
                                InvoiceDatum createNewReoccurringInvoice = null;

                                try
                                {
                                    createNewReoccurringInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                }

                                if (createNewReoccurringInvoice is null)
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
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
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                    }
                                }

                                if (createNewReoccurringInvoice is not null)
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
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (upfrontInvoice.line_items.Any())
                            {
                                InvoiceDatum createNewOneTimeInvoice = null;

                                try
                                {
                                    createNewOneTimeInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                }

                                // If it doesn't exist create it, otherwise update it.
                                if (createNewOneTimeInvoice is null)
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
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
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    }
                                }

                                if (createNewOneTimeInvoice is not null)
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
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }

                            _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                            await _context.SaveChangesAsync();

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully deleted the existing Invoices and created new Invoices for this quote! 🥳", AlertType = "alert-success" });
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

                            // Submit them to the billing system if they have items.
                            if (upfrontInvoice.line_items.Any() && reoccurringInvoice.line_items.Any())
                            {
                                InvoiceDatum createNewOneTimeInvoice = null;
                                ReccurringInvoiceDatum createNewReoccurringInvoice = null;

                                try
                                {
                                    createNewOneTimeInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                    createNewReoccurringInvoice = await ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                }

                                if (createNewOneTimeInvoice is null)
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        createNewOneTimeInvoice = await createNewOneTimeInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                    }
                                }

                                if (createNewReoccurringInvoice is null)
                                {
                                    try
                                    {
                                        // Create
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        // Update
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                    }
                                }

                                if (createNewOneTimeInvoice is not null && createNewReoccurringInvoice is not null)
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
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (reoccurringInvoice.line_items.Any())
                            {
                                ReccurringInvoiceDatum createNewReoccurringInvoice = null;

                                try
                                {
                                    createNewReoccurringInvoice = await ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _invoiceNinjaToken);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                }

                                if (createNewReoccurringInvoice is null)
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewReoccurringInvoice.line_items = reoccurringInvoice.line_items;
                                        createNewReoccurringInvoice = await createNewReoccurringInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewReoccurringInvoice));
                                    }
                                }

                                if (createNewReoccurringInvoice is not null)
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
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (upfrontInvoice.line_items.Any())
                            {
                                InvoiceDatum createNewOneTimeInvoice = null;

                                try
                                {
                                    createNewOneTimeInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, _invoiceNinjaToken);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to find existing invoices in the billing system.");
                                }

                                if (createNewOneTimeInvoice is null)
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        createNewOneTimeInvoice.line_items = upfrontInvoice.line_items;
                                        createNewOneTimeInvoice = await upfrontInvoice.PutAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(ex?.Message ?? "No Message found.");
                                        Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
                                        Log.Fatal(JsonSerializer.Serialize(createNewOneTimeInvoice));
                                    }
                                }

                                if (createNewOneTimeInvoice is not null)
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
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }

                            _context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                            await _context.SaveChangesAsync();

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully deleted the existing Invoices and created new Invoices for this order! 🥳", AlertType = "alert-success" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                    }
                }
                else
                {
                    return View("OrderEdit", new EditOrderResult { Order = order, Message = $"Failed to regenerate the invoices for {order.OrderId}. Either the order could not be found or there are no Product Orders assocated with this Order. 🤔", AlertType = "alert-danger" });
                }
            }
        }
    }
}