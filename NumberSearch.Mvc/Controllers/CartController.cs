using DnsClient;

using Flurl.Http;

using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly string _invoiceNinjaToken = mvcConfiguration.InvoiceNinjaToken;
        private readonly string _emailOrders = mvcConfiguration.EmailOrders;
        private readonly MvcConfiguration _configuration = mvcConfiguration;

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(bool? emptyCart, string product, int? quantity)
        {
            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (emptyCart.HasValue && emptyCart.Value)
            {
                // Replace the existing cart with a new, empty cart to empty it.
                cart = new Cart();
                var checkSet = cart.SetToSession(HttpContext.Session);
                cart = Cart.GetFromSession(HttpContext.Session);

                return View("Index", new CartResult
                {
                    Cart = cart
                });
            }
            else if (!string.IsNullOrWhiteSpace(product) && quantity is not null & quantity > 0)
            {
                var products = await Product.GetAllAsync(_postgresql);
                var productToUpdate = products.FirstOrDefault(x => x.Name == product);

                if (productToUpdate is not null && cart.ProductOrders is not null)
                {
                    var toRemove = cart.ProductOrders.Where(x => x.ProductId == productToUpdate.ProductId).FirstOrDefault();

                    if (toRemove is not null)
                    {
                        var checkRemove = cart.RemoveProduct(productToUpdate, toRemove);

                        toRemove.Quantity = quantity ?? toRemove.Quantity;

                        var checkAdd = cart.AddProduct(productToUpdate, toRemove);

                        var checkSet = cart.SetToSession(HttpContext.Session);
                        cart = Cart.GetFromSession(HttpContext.Session);
                    }
                }
            }

            // Check cordless phone to base station ratio
            var cordless = cart.Products.Where(x => x.Name.Contains("DP")).ToArray();
            int handsets = 0;
            int basestations = 0;

            foreach (var item in cordless)
            {
                var productOrder = cart?.ProductOrders?.FirstOrDefault(x => x.ProductId == item.ProductId);
                if (productOrder is not null)
                {
                    if (item.Name.Contains("DP750"))
                    {
                        basestations += productOrder.Quantity;
                    }
                    if (item.Name.Contains("DP752"))
                    {
                        basestations += productOrder.Quantity;
                    }
                    if (item.Name.Contains("DP720"))
                    {
                        handsets += productOrder.Quantity;
                    }
                    if (item.Name.Contains("DP722"))
                    {
                        handsets += productOrder.Quantity;
                    }
                    if (item.Name.Contains("DP730"))
                    {
                        handsets += productOrder.Quantity;
                    }
                }
            }
            // 5 to 1 ratio of handsets per base station cannot be exceeded.
            if (handsets > 0 && basestations > 0 && handsets > (basestations * 5))
            {
                return View("Index", new CartResult
                {
                    Message = "❌ The hardware in your cart does not make sense. Only 5 cordless handsets can be paired to 1 base station, please add more base stations. Call us at 206-858-8757 for help!",
                    Cart = cart ?? new()
                });
            }
            else if (handsets > 0 && basestations > 0 && basestations > handsets)
            {
                return View("Index", new CartResult
                {
                    Message = "❌ The hardware in your cart does not make sense. 5 cordless handsets can be paired to 1 base station, please order fewer base stations. Call us at 206-858-8757 for help!",
                    Cart = cart ?? new()
                });
            }
            else
            {
                return View("Index", new CartResult
                {
                    Cart = cart ?? new()
                });
            }

        }

        [HttpGet]
        [Route("Cart/Checkout")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckoutAsync()
        {
            await HttpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (cart is not null && cart.ProductOrders is not null && cart.ProductOrders.Count == 0)
            {
                return View("Index", new CartResult { Cart = cart });
            }
            else if (cart is not null && cart.Order is not null)
            {
                // Create a GUID for an order to prevent multiple order submissions from repeated button clicking.
                cart.Order.OrderId = Guid.NewGuid();

                // Default to Onsite Installation if there is hardware in the cart.
                if (cart?.Products is not null && cart.Products.Count > 0)
                {
                    cart.Order.OnsiteInstallation = true;

                    // Add the call out charge and install estimate to the Cart
                    Product onsite = await Product.GetByIdAsync(Guid.Parse("b174c76a-e067-4a6a-abcf-53b6d3a848e4"), _postgresql) ?? new();
                    Product estimate = await Product.GetByIdAsync(Guid.Parse("a032b3ba-da57-4ad3-90ec-c59a3505b075"), _postgresql) ?? new();

                    // Sum all of the install time estimates.
                    decimal totalInstallTime = 0m;
                    foreach (var item in cart.Products)
                    {
                        var quantity = cart.ProductOrders?.Where(x => x.ProductId == item.ProductId).FirstOrDefault();

                        if (item.InstallTime > 0m && quantity is not null)
                        {
                            totalInstallTime += item.InstallTime * quantity.Quantity;
                        }
                    }

                    var productOrderOnsite = new ProductOrder
                    {
                        ProductOrderId = Guid.NewGuid(),
                        ProductId = onsite.ProductId,
                        Quantity = 1
                    };

                    var productOrderEstimate = new ProductOrder
                    {
                        ProductOrderId = Guid.NewGuid(),
                        ProductId = estimate.ProductId,
                        Quantity = decimal.ToInt32(Math.Ceiling(totalInstallTime))
                    };

                    _ = cart.AddProduct(onsite, productOrderOnsite);
                    _ = cart.AddProduct(estimate, productOrderEstimate);
                }

                _ = cart?.SetToSession(HttpContext.Session);
            }

            return View("Order", new CartResult { Cart = cart ?? new() });
        }

        // Show orders that have already been submitted.
        [HttpGet("Cart/Order/{Id}")]
        [HttpPost("Cart/Order/{Id}")]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id, bool? AddPortingInfo)
        {
            if (Id != Guid.Empty)
            {
                var order = await Order.GetByIdAsync(Id, _postgresql);
                if (order == null || string.IsNullOrWhiteSpace(order.Email))
                {
                    return View("Index", new CartResult
                    {
                        Message = "Couldn't find this order in our system."
                    });
                }

                if (order.MergedOrderId is not null)
                {
                    return Redirect($"/Cart/Order/{order?.MergedOrderId}");
                }

                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql);
                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql);
                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql);
                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var products = new List<Product>();
                var services = new List<Service>();
                var coupons = new List<Coupon>();
                foreach (var item in productOrders)
                {
                    if (item.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql);
                        if (product is not null)
                        {
                            products.Add(product);
                        }
                    }
                    else if (item.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql);
                        if (service is not null)
                        {
                            services.Add(service);
                        }
                    }
                    else if (item.CouponId is not null)
                    {
                        var coupon = await Coupon.GetByIdAsync(item.CouponId ?? Guid.NewGuid(), _postgresql);
                        if (coupon is not null)
                        {
                            coupons.Add(coupon);
                        }
                    }
                }

                var shipment = await ProductItem.GetByOrderIdAsync(order.OrderId, _postgresql);

                var cart = new Cart
                {
                    Order = order,
                    PhoneNumbers = [],
                    ProductOrders = productOrders.ToList(),
                    Products = products,
                    Services = services,
                    Coupons = coupons,
                    PortedPhoneNumbers = portedPhoneNumbers.ToList(),
                    VerifiedPhoneNumbers = verifiedPhoneNumbers.ToList(),
                    PurchasedPhoneNumbers = purchasedPhoneNumbers.ToList(),
                    Shipment = shipment?.FirstOrDefault() ?? new()
                };

                if (AddPortingInfo is not null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql);

                    _ = cart.SetToSession(HttpContext.Session);

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = portRequest ?? new(),
                        PhoneNumbers = [.. cart.PortedPhoneNumbers]
                    });
                }
                else
                {
                    return View("Order", new CartResult { Cart = cart });

                }
            }
            else
            {
                return View("Order");
            }
        }

        [HttpGet]
        [Route("Cart/PortingInformation/{Id}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> PortingInformationForOrderByIdAsync(Guid Id)
        {
            if (Id != Guid.Empty)
            {
                var order = await Order.GetByIdAsync(Id, _postgresql);
                if (order is not null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql);
                    var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql);

                    if (portedPhoneNumbers.Any())
                    {
                        return View("Success", new OrderWithPorts
                        {
                            Order = order,
                            PortRequest = portRequest ?? new(),
                            PhoneNumbers = portedPhoneNumbers.ToArray()
                        });
                    }
                }
                return Redirect($"/Cart/Order/{order?.OrderId}");
            }
            else
            {
                return Redirect($"/Cart/");
            }
        }

        [HttpPost("Cart/Submit")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitAsync(CartResult input)
        {
            var order = input.Cart.Order;

            if (order is not null && !string.IsNullOrWhiteSpace(order.Email))
            {
                await HttpContext.Session.LoadAsync();
                var cart = Cart.GetFromSession(HttpContext.Session);

                cart.Order = order;

                // This is purely so that we can isolate the state of this call when it fails out.
                Log.Information(JsonSerializer.Serialize(cart));

                var emailDomain = new MailAddress(order.Email);

                try
                {
                    var lookup = new LookupClient();
                    var result = lookup.Query(emailDomain.Host, QueryType.MX);
                    var record = result.Answers.MxRecords().FirstOrDefault();
                    if (record is not null)
                    {
                        Log.Information($"[Checkout] Email address {order.Email} has a valid domain: {emailDomain.Host}.");
                    }
                    else
                    {
                        _ = cart.SetToSession(HttpContext.Session);
                        Log.Error($"[Checkout] Email address {order.Email} has an invalid domain: {emailDomain.Host}.");
                        var message = $"💀 The email server at {emailDomain.Host} didn't have an MX record. Please supply a valid email address.";
                        return View("Order", new CartResult { Message = message, Cart = cart });
                    }
                }
                catch (Exception ex)
                {
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] Email address {order.Email} has an invalid domain: {emailDomain.Host}. {ex.Message}");
                    var message = $"💀 The email server at {emailDomain.Host} didn't have an MX record. Please supply a valid email address.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }

                // Validate the install date.
                if (order.InstallDate is not null && order.InstallDate < DateTime.Now.AddDays(1))
                {
                    order.InstallDate = null;
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] The install date needs to be at least one day in the future.");
                    var message = $"💀 The install date needs to be at least one day in the future.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }

                if (order.FirstName == order.BusinessName || order.LastName == order.BusinessName)
                {
                    order.FirstName = string.Empty;
                    order.LastName = string.Empty;
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] Your business name cannot be the same as your last name or first name.");
                    var message = $"💀 Your business name cannot be the same as your last name or first name.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }

                if (!string.IsNullOrWhiteSpace(order.AddressUnitNumber) && string.IsNullOrWhiteSpace(order.AddressUnitType))
                {
                    order.AddressUnitNumber = string.Empty;
                    order.AddressUnitType = string.Empty;
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] Please set the Unit Type for the Unit Number you provided.");
                    var message = $"💀 Please set the Unit Type for the Unit Number you provided.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }

                if (order.Address == order.AddressUnitNumber)
                {
                    order.AddressUnitNumber = string.Empty;
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] The billing address and unit number cannot be the same.");
                    var message = $"💀 The billing address and unit number cannot be the same.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }

                var checkParsed = PhoneNumbersNA.PhoneNumber.TryParse(order.ContactPhoneNumber, out var contact);

                if (checkParsed is false)
                {
                    order.ContactPhoneNumber = string.Empty;
                    _ = cart.SetToSession(HttpContext.Session);
                    Log.Error($"[Checkout] The Direct phone number is not a dialable North American phone number.");
                    var message = $"💀 The Direct phone number is not a dialable North American phone number.";
                    return View("Order", new CartResult { Message = message, Cart = cart });
                }
                else
                {
                    try
                    {
                        var checkPortable = await ValidatePortability.GetAsync(contact.DialedNumber.AsMemory(), _configuration.BulkVSUsername.AsMemory(), _configuration.BulkVSPassword.AsMemory());
                        if (string.IsNullOrWhiteSpace(checkPortable.TN) || checkPortable.Portable is false)
                        {
                            order.ContactPhoneNumber = string.Empty;
                            _ = cart.SetToSession(HttpContext.Session);
                            Log.Error($"[Checkout] The contact phone number is not a dialable North American phone number.");
                            var message = $"💀 The contact phone number is not a dialable North American phone number.";
                            return View("Order", new CartResult { Message = message, Cart = cart });
                        }
                    }
                    catch (Exception ex)
                    {
                        order.ContactPhoneNumber = string.Empty;
                        _ = cart.SetToSession(HttpContext.Session);
                        Log.Error($"[Checkout] The contact phone number is not a dialable North American phone number. {ex.Message}");
                        var message = $"💀 The contact phone number is not a dialable North American phone number.";
                        return View("Order", new CartResult { Message = message, Cart = cart });
                    }
                }

                if (cart.ProductOrders is null || cart.ProductOrders.Count == 0)
                {
                    // Give the user a better error message and tell them to try again
                    // Maybe save the cart to the database when the go to the cart page or when they hit the checkout button?
                    Log.Error("[Checkout] There are no product orders in this sessions cart. How did we get here???");
                    // Reset the session and clear the Cart.
                    HttpContext.Session.Clear();

                    return View("Order", new CartResult { Message = "💀 The server disconnected and your Cart was lost. Please try it again now." });
                }

                order.DateSubmitted = DateTime.Now;

                if (order.OrderId != Guid.Empty)
                {
                    var orderExists = await Order.GetByIdAsync(order.OrderId, _postgresql);

                    // Create a new order.
                    if (orderExists is null)
                    {
                        // Prevent the background work from happening before it's queued up.
                        order.BackgroundWorkCompleted = true;

                        // Format the address information
                        Log.Information($"[Checkout] Parsing address data from {order.Address}");
                        var addressParts = order.UnparsedAddress.Split(", ");
                        if (addressParts.Length == 5)
                        {
                            order.Address = addressParts[0];
                            order.City = addressParts[1];
                            order.State = addressParts[2];
                            order.Zip = addressParts[3];
                            Log.Information($"[Checkout] Address: {order.Address} City: {order.City} State: {order.State} Zip: {order.Zip}");
                        }
                        else if (addressParts.Length == 6)
                        {
                            order.Address = addressParts[0];
                            //order.UnitTypeAndNumber = addressParts[1];
                            order.City = addressParts[2];
                            order.State = addressParts[3];
                            order.Zip = addressParts[4];
                            Log.Information($"[Checkout] Address: {order.Address} City: {order.City} State: {order.State} Zip: {order.Zip}");
                        }
                        else
                        {
                            Log.Error($"[Checkout] Failed automatic address formatting.");
                        }

                        // Fillout the address2 information from its components.
                        if (!string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                        {
                            order.Address2 = $"{order.AddressUnitType} {order.AddressUnitNumber}";
                        }

                        // Save to db.
                        var submittedOrder = await order.PostAsync(_postgresql);

                        // Send a confirmation email.
                        if (submittedOrder)
                        {
                            bool NoEmail = order.NoEmail;
                            order = await Order.GetByIdAsync(order.OrderId, _postgresql) ?? new();
                            order.NoEmail = NoEmail;

                            // Submit the number orders and track the total cost.
                            var onetimeItems = new List<Line_Items>();
                            var reoccuringItems = new List<Line_Items>();
                            var totalCost = 0;
                            var totalNumberPurchasingCost = 0;

                            // Create a single PIN for this order.
                            var random = new Random();
                            var pin = random.Next(100000, 99999999);

                            if (cart is not null && cart.PhoneNumbers is not null)
                            {
                                foreach (var nto in cart.PhoneNumbers)
                                {
                                    var productOrder = cart.ProductOrders.Where(x => x.DialedNumber == nto.DialedNumber).FirstOrDefault();
                                    var numberToBePurchased = cart.PhoneNumbers.Where(x => x.DialedNumber == nto.DialedNumber).FirstOrDefault();

                                    if (productOrder is not null && numberToBePurchased is not null)
                                    {
                                        productOrder.OrderId = order.OrderId;

                                        var cost = nto.NumberType == "Executive" ? 200 : nto.NumberType == "Premium" ? 40 : nto.NumberType == "Standard" ? 20 : 20;

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                        var purchsedNumber = new PurchasedPhoneNumber
                                        {
                                            Completed = false,
                                            DateIngested = numberToBePurchased.DateIngested,
                                            DateOrdered = DateTime.Now,
                                            NPA = numberToBePurchased.NPA,
                                            NXX = numberToBePurchased.NXX,
                                            XXXX = numberToBePurchased.XXXX,
                                            DialedNumber = numberToBePurchased.DialedNumber,
                                            IngestedFrom = numberToBePurchased.IngestedFrom,
                                            NumberType = numberToBePurchased.NumberType,
                                            OrderId = order.OrderId,
                                            OrderResponse = string.Empty,
                                            PIN = pin.ToString()
                                        };

                                        var checkPurchaseOrder = await purchsedNumber.PostAsync(_postgresql);

                                        totalNumberPurchasingCost += cost;

                                        onetimeItems.Add(new Line_Items
                                        {
                                            product_key = nto.DialedNumber,
                                            notes = $"{nto.NumberType} Phone Number",
                                            cost = cost,
                                            quantity = 1
                                        });
                                    }
                                }
                            }

                            totalCost += totalNumberPurchasingCost;

                            var totalPortingCost = 0;
                            var emailSubject = string.Empty;

                            if (cart is not null && cart?.ProductOrders is not null)
                            {
                                if (cart.Products is not null && cart.Products.Count > 0)
                                {
                                    // Add the call out charge and install estimate to the Cart
                                    Product onsite = await Product.GetByIdAsync(Guid.Parse("b174c76a-e067-4a6a-abcf-53b6d3a848e4"), _postgresql) ?? new();
                                    Product estimate = await Product.GetByIdAsync(Guid.Parse("a032b3ba-da57-4ad3-90ec-c59a3505b075"), _postgresql) ?? new();

                                    // Sum all of the install time estimates.
                                    decimal totalInstallTime = 0m;
                                    foreach (var item in cart.Products)
                                    {
                                        var quantity = cart.ProductOrders?.Where(x => x.ProductId == item.ProductId).FirstOrDefault();

                                        if (item.InstallTime > 0m && quantity is not null)
                                        {
                                            totalInstallTime += item.InstallTime * quantity.Quantity;
                                        }
                                    }

                                    var productOrderOnsite = new ProductOrder
                                    {
                                        ProductOrderId = Guid.NewGuid(),
                                        ProductId = onsite.ProductId,
                                        Quantity = 1
                                    };

                                    var productOrderEstimate = new ProductOrder
                                    {
                                        ProductOrderId = Guid.NewGuid(),
                                        ProductId = estimate.ProductId,
                                        Quantity = decimal.ToInt32(Math.Ceiling(totalInstallTime))
                                    };

                                    if (order.OnsiteInstallation)
                                    {
                                        // Add the install charges if they're not already in the Cart.
                                        var checkOnsiteExists = cart.Products.FirstOrDefault(x => x.ProductId == Guid.Parse("b174c76a-e067-4a6a-abcf-53b6d3a848e4"));
                                        var checkEstimateExists = cart.Products.FirstOrDefault(x => x.ProductId == Guid.Parse("a032b3ba-da57-4ad3-90ec-c59a3505b075"));

                                        if (checkOnsiteExists is null && checkEstimateExists is null)
                                        {
                                            _ = cart.AddProduct(onsite, productOrderOnsite);
                                            _ = cart.AddProduct(estimate, productOrderEstimate);
                                        }
                                    }
                                    else
                                    {
                                        // Remove the install charges as this is now a remote install.
                                        _ = cart.RemoveProduct(onsite, productOrderOnsite);
                                        _ = cart.RemoveProduct(estimate, productOrderEstimate);
                                    }
                                }

                                foreach (var productOrder in cart.ProductOrders!)
                                {
                                    productOrder.OrderId = order.OrderId;

                                    if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                    {
                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.DialedNumber : emailSubject;
                                    }

                                    if (productOrder.PortedPhoneNumberId is not null)
                                    {
                                        var ported = cart?.PortedPhoneNumbers?.Where(x => x.PortedPhoneNumberId == productOrder.PortedPhoneNumberId).FirstOrDefault();

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

                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.PortedDialedNumber : emailSubject;

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                    }

                                    if (productOrder.VerifiedPhoneNumberId is not null)
                                    {
                                        var verfied = cart?.VerifiedPhoneNumbers?.Where(x => x.VerifiedPhoneNumberId == productOrder.VerifiedPhoneNumberId).FirstOrDefault();

                                        if (verfied is not null)
                                        {
                                            totalCost += 10;
                                            onetimeItems.Add(new Line_Items
                                            {
                                                product_key = verfied.VerifiedDialedNumber,
                                                notes = $"Phone Number to Verify Daily",
                                                cost = 10,
                                                quantity = 1
                                            });
                                        }

                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? verfied?.VerifiedDialedNumber : emailSubject;

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                    }

                                    if (productOrder.ProductId != Guid.Empty)
                                    {
                                        var product = cart?.Products?.Where(x => x.ProductId == productOrder.ProductId).FirstOrDefault();

                                        if (product is not null)
                                        {
                                            totalCost += product.Price;
                                            onetimeItems.Add(new Line_Items
                                            {
                                                product_key = product.Name,
                                                notes = $"{product.Description}",
                                                cost = product.Price,
                                                quantity = productOrder.Quantity
                                            });
                                        }

                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? product?.Name : emailSubject;

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                    }

                                    if (productOrder.ServiceId != Guid.Empty)
                                    {
                                        var service = cart?.Services?.Where(x => x.ServiceId == productOrder.ServiceId).FirstOrDefault();

                                        if (service is not null)
                                        {
                                            totalCost += service.Price;
                                            reoccuringItems.Add(new Line_Items
                                            {
                                                product_key = service.Name,
                                                notes = $"{service.Description}",
                                                cost = service.Price,
                                                quantity = productOrder.Quantity
                                            });
                                        }

                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? service?.Name : emailSubject;

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                    }

                                    // Apply coupon discounts
                                    if (productOrder.CouponId is not null)
                                    {
                                        var coupon = cart?.Coupons?.Where(x => x.CouponId == productOrder.CouponId).FirstOrDefault();

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
                                            else if (coupon.Type == "Install")
                                            {
                                                // If they have selected onsite installation this coupon removes a $75 charge.
                                                if (order.OnsiteInstallation)
                                                {
                                                    onetimeItems.Add(new Line_Items
                                                    {
                                                        product_key = coupon.Name,
                                                        notes = coupon.Description,
                                                        cost = 75 * -1,
                                                        quantity = 1
                                                    });
                                                }
                                                else
                                                {
                                                    onetimeItems.Add(new Line_Items
                                                    {
                                                        product_key = coupon.Name,
                                                        notes = coupon.Description,
                                                        cost = 0,
                                                        quantity = 1
                                                    });
                                                }
                                            }
                                            else if (coupon.Type == "Number")
                                            {
                                                if (coupon.Name.Contains("20"))
                                                {
                                                    var discountTo20 = cart?.PhoneNumbers is not null && cart.PhoneNumbers.Count != 0 ? cart.PhoneNumbers.Count * 20 :
                                                    cart?.PurchasedPhoneNumbers is not null && cart.PurchasedPhoneNumbers.Count != 0 ? cart.PurchasedPhoneNumbers.Count * 20 : 0;
                                                    totalCost -= totalNumberPurchasingCost - discountTo20;
                                                    onetimeItems.Add(new Line_Items
                                                    {
                                                        product_key = coupon.Name,
                                                        notes = coupon.Description,
                                                        cost = (totalNumberPurchasingCost - discountTo20) * -1,
                                                        quantity = 1
                                                    });
                                                }
                                                else
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
                                            }
                                            else if (coupon.Type == "Service")
                                            {
                                                var servicesToDiscount = cart?.Services is not null && cart.Services.Count != 0 ? cart?.Services?.Where(x => x.Name.Contains("5G")).ToArray() : null;
                                                if (servicesToDiscount is not null)
                                                {
                                                    var partnerDiscount = 0;
                                                    foreach (var service in servicesToDiscount)
                                                    {
                                                        var productOrderToDiscount = cart?.ProductOrders?.FirstOrDefault(x => x.ServiceId == service.ServiceId);
                                                        if (productOrderToDiscount is not null)
                                                        {
                                                            partnerDiscount += productOrderToDiscount.Quantity * 10;
                                                        }
                                                    }
                                                    totalCost -= partnerDiscount;
                                                    reoccuringItems.Add(new Line_Items
                                                    {
                                                        product_key = coupon.Name,
                                                        notes = coupon.Description,
                                                        cost = partnerDiscount * -1,
                                                        quantity = 1
                                                    });
                                                }
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql);
                                    }
                                }
                            }

                            // Handle hardware installation scenarios, if hardware is in the order.
                            if (cart?.Products is not null && cart.Products.Count != 0)
                            {
                                if (!order.OnsiteInstallation)
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

                            if (cart is not null && cart.PortedPhoneNumbers is not null)
                            {
                                // Associate the ported numbers with this order.
                                foreach (var portedNumber in cart.PortedPhoneNumbers)
                                {
                                    portedNumber.OrderId = order.OrderId;

                                    var checkPort = await portedNumber.PostAsync(_postgresql);

                                    Log.Information($"[Checkout] Saved port request for number {portedNumber.PortedDialedNumber}.");
                                }
                            }

                            if (cart is not null && cart.VerifiedPhoneNumbers is not null)
                            {
                                // Associate the verified numbers with this order.
                                foreach (var verifiedNumber in cart.VerifiedPhoneNumbers)
                                {
                                    verifiedNumber.OrderId = order.OrderId;

                                    var checkVerified = await verifiedNumber.PostAsync(_postgresql);

                                    Log.Information($"[Checkout] Saved Verified Number {verifiedNumber.VerifiedDialedNumber} to the Database.");
                                }
                            }

                            // Handle the tax information for the invoice and fall back to simpler queries if we get failures.
                            SalesTax specificTaxRate = null!;
                            if (order.State is "WA" || order.State is "Washington")
                            {
                                try
                                {
                                    // Use our own API
                                    specificTaxRate = await SalesTax.GetLocalAPIAsync(order.Address.AsMemory(), string.Empty.AsMemory(), order.Zip.AsMemory());
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
                                        specificTaxRate = await SalesTax.GetAsync(order.Address.AsMemory(), order.City.AsMemory(), order.Zip.AsMemory());
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

                            // Validation rules to prevent impossible tax rates.
                            if (specificTaxRate is not null && specificTaxRate.rate1 > 0 && specificTaxRate.rate1 < 0.15M && !string.IsNullOrWhiteSpace(specificTaxRate.rate?.name) && (order.State is "WA" || order.State is "Washington"))
                            {
                                var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                                var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                                var taxRateValue = specificTaxRate.rate1 * 100M;

                                var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken.AsMemory());
                                billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();
                                if (string.IsNullOrWhiteSpace(billingTaxRate.name))
                                {
                                    billingTaxRate = new TaxRateDatum
                                    {
                                        name = taxRateName,
                                        rate = taxRateValue
                                    };

                                    var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken.AsMemory());
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
                                billingTaxRate = billingTaxRate with { rate = 0 };
                            }

                            // Create the confirmation email.
                            var confirmationEmail = new DataAccess.Models.Email
                            {
                                PrimaryEmailAddress = order.Email,
                                SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                CarbonCopy = _emailOrders,
                                MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thank you for choosing Accelerate Networks! 
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Thanks,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)",
                                OrderId = order.OrderId,
                                Subject = $"Order confirmation for {emailSubject}"
                            };

                            // Create a billing client and send out an invoice.
                            var billingClients = await Client.GetByEmailAsync(order.Email, _invoiceNinjaToken);
                            var billingClient = billingClients.data.FirstOrDefault();

                            // To get the right data into invoice ninja 5 we must first create the billing client using a unique name,
                            // and then update that billing client with the rest of the address and contact data once we have its id.
                            if (string.IsNullOrWhiteSpace(billingClient.id))
                            {
                                // Create a new client in the billing system.
                                var newBillingClient = new ClientDatum
                                {
                                    name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                    contacts = [
                                        new() {
                                            email = order.Email,
                                            first_name = order.FirstName,
                                            last_name = order.LastName
                                        }
                                    ],
                                    address1 = order.Address,
                                    address2 = order.Address2,
                                    city = order.City,
                                    state = order.State,
                                    postal_code = order.Zip
                                };

                                // Create the client and get its id.
                                var newClient = await newBillingClient.PostAsync(_invoiceNinjaToken.AsMemory());
                                newBillingClient = newBillingClient with { id = newClient.id };
                                var billingClientContact = newBillingClient.contacts.FirstOrDefault();
                                var clientContact = newClient.contacts.FirstOrDefault();
                                if (!string.IsNullOrWhiteSpace(clientContact.id))
                                {
                                    billingClientContact = billingClientContact with { id = clientContact.id };
                                }
                                billingClient = await newBillingClient.PutAsync(_invoiceNinjaToken.AsMemory());
                                Log.Information($"[Checkout] Created billing client {billingClient.name}, {billingClient.id}.");
                            }
                            else
                            {
                                Log.Information($"[Checkout] Found billing client {billingClient.name}, {billingClient.id}.");
                            }

                            // Create the invoices for this order and submit it to the billing system.
                            var upfrontInvoice = new InvoiceDatum
                            {
                                client_id = billingClient.id,
                                line_items = [.. onetimeItems],
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate
                            };

                            // If they want just a Quote, create a quote in the billing system, not an invoice.
                            if (order.Quote)
                            {
                                // Mark the invoices as quotes.
                                upfrontInvoice = upfrontInvoice with { entity_type = "quote" };
                                var reoccurringInvoice = new InvoiceDatum
                                {
                                    client_id = billingClient.id,
                                    line_items = [.. reoccuringItems],
                                    tax_name1 = billingTaxRate.name,
                                    tax_rate1 = billingTaxRate.rate,
                                    entity_type = "quote",
                                };

                                var hiddenReoccurringInvoice = new ReccurringInvoiceDatum
                                {
                                    client_id = billingClient.id,
                                    line_items = [.. reoccuringItems],
                                    tax_name1 = billingTaxRate.name,
                                    tax_rate1 = billingTaxRate.rate,
                                    entity_type = "recurringInvoice",
                                    frequency_id = "5",
                                    auto_bill = "opt_out",
                                    auto_bill_enabled = false,
                                    next_send_date = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss"),
                                };

                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.line_items.Length != 0 && reoccurringInvoice.line_items.Length != 0)
                                {
                                    // Retry once on invoice creation failures.
                                    try
                                    {
                                        var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken);
                                        var createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken.AsMemory());

                                        if (!string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id) && !string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, true);
                                            var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                            var reoccurringLink = oneTimeInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                            {
                                                order.ReoccuringInvoiceLink = reoccurringLink;
                                            }

                                            if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                            {
                                                order.UpfrontInvoiceLink = oneTimeLink;
                                            }

                                            confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} and {createNewReoccurringInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href='{reoccurringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }

                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system.");
                                        Log.Fatal(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                    }
                                }
                                else if (reoccurringInvoice.line_items.Length != 0)
                                {
                                    try
                                    {
                                        // Submit them to the billing system.
                                        var createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken.AsMemory());

                                        if (!string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewReoccurringInvoice.client_id, _invoiceNinjaToken, true);
                                            var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                            {
                                                order.ReoccuringInvoiceLink = reoccurringLink;
                                            }

                                            confirmationEmail.Subject = $"Quote {createNewReoccurringInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href='{reoccurringLink}'>monthly service cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                    }
                                }
                                else if (upfrontInvoice.line_items.Length != 0)
                                {
                                    try
                                    {
                                        // Submit them to the billing system.
                                        var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken);

                                        if (!string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, true);
                                            var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                            {
                                                order.UpfrontInvoiceLink = oneTimeLink;
                                            }

                                            confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />     
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal(error);
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    }
                                }
                            }
                            else
                            {
                                var reoccurringInvoice = new ReccurringInvoiceDatum
                                {
                                    client_id = billingClient.id,
                                    line_items = [.. reoccuringItems],
                                    tax_name1 = billingTaxRate.name,
                                    tax_rate1 = billingTaxRate.rate,
                                    entity_type = "recurringInvoice",
                                    frequency_id = "5",
                                    auto_bill = "opt_out",
                                    auto_bill_enabled = true,
                                    next_send_date = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss"),
                                    status_id = "2",
                                };


                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.line_items.Length != 0 && reoccurringInvoice.line_items.Length != 0)
                                {
                                    try
                                    {
                                        var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken);
                                        var createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken.AsMemory());

                                        if (!string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, false);
                                            var recurringInvoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken);
                                            var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                            var reoccurringLink = recurringInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                            {
                                                order.ReoccuringInvoiceLink = reoccurringLink;
                                            }

                                            if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                            {
                                                order.UpfrontInvoiceLink = oneTimeLink;
                                            }

                                            confirmationEmail.Subject = $"Order {createNewOneTimeInvoice.number} and {createNewReoccurringInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href='{reoccurringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                    }


                                }
                                else if (reoccurringInvoice.line_items.Length != 0)
                                {
                                    // Bill upfront for the first month of reoccurring service so that we can get their payment information on file.
                                    upfrontInvoice = upfrontInvoice with { line_items = reoccurringInvoice.line_items };

                                    try
                                    {
                                        var createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken.AsMemory());
                                        var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken);
                                        if (!string.IsNullOrWhiteSpace(createNewReoccurringInvoice.id) && !string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, false);
                                            var recurringInvoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken);
                                            var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                            var reoccurringLink = recurringInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                            {
                                                order.ReoccuringInvoiceLink = reoccurringLink;
                                            }

                                            if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                            {
                                                order.UpfrontInvoiceLink = oneTimeLink;
                                            }

                                            confirmationEmail.Subject = $"Order {createNewOneTimeInvoice.number} and {createNewReoccurringInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href='{reoccurringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }

                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(error);
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                    }
                                }
                                else if (upfrontInvoice.line_items.Length != 0)
                                {
                                    try
                                    {
                                        var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken);

                                        if (!string.IsNullOrWhiteSpace(createNewOneTimeInvoice.id))
                                        {
                                            // Update the order with the billing system's client and the two invoice Id's.
                                            order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                            order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                            var checkQuoteUpdate = await order.PutAsync(_postgresql);

                                            var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, false);
                                            var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                            if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                            {
                                                order.UpfrontInvoiceLink = oneTimeLink;
                                            }

                                            confirmationEmail.Subject = $"Order {createNewOneTimeInvoice.number} - Accelerate Networks";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                                            // This order is just for purchasing phone numbers.
                                            if (cart is not null && cart.PhoneNumbers is not null && cart.ProductOrders is not null && cart.PhoneNumbers.Count == cart.ProductOrders.Count)
                                            {
                                                var formattedNumbers = string.Empty;

                                                foreach (var item in cart.PhoneNumbers)
                                                {
                                                    if (item.IngestedFrom == "BulkVS")
                                                    {
                                                        formattedNumbers += $"{item.DialedNumber} <strong>PIN: {pin}</strong><br />";
                                                    }
                                                    else
                                                    {
                                                        formattedNumbers += $"{item.DialedNumber}<br />";
                                                    }
                                                }

                                                if (cart.PhoneNumbers.Count > 1)
                                                {
                                                    confirmationEmail.Subject = $"Order for {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber} is complete!";
                                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />  
The order for {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber} is ready, let us know if you would like this number to forward to another phone number immediately.
<br />
<br />  
To port these numbers to another provider, please pay <a href='{oneTimeLink}'>this invoice</a> and submit a port out request with your new provider using the following information:
<br />
<br />  
Account number: {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber}
<br />  
Business Name: {order.BusinessName}
<br />  
Authorized Contact: {order.FirstName} {order.LastName}
<br />  
Address: {order.Address} {order.Address2} {order.City}, {order.State} {order.Zip}
<br />  
<br />  
{formattedNumbers}
<br />  
<a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>Original order</a>
<br />  
<br />  
If you need anything please let us know!
<br />  
<br /> 
Sincerely,
<br /> 
Accelerate Networks
<br /> 
206-858-8757 (call/text)";
                                                }
                                                else
                                                {

                                                    confirmationEmail.Subject = $"Order for {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber} is complete!";
                                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />  
The order for {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber} is ready, let us know if you would like this number to forward to another phone number immediately.
<br />
<br />  
To port this number to another provider, please pay <a href='{oneTimeLink}'>this invoice</a> and submit a port out request with your new provider using the following information:
<br />
<br />  
Account number: {cart.PhoneNumbers.FirstOrDefault()?.DialedNumber}
<br />  
Business Name: {order.BusinessName}
<br />  
Authorized Contact: {order.FirstName} {order.LastName}
<br />  
Address: {order.Address} {order.Address2} {order.City}, {order.State} {order.Zip}
<br />  
PIN: {pin}
<br />  
<br />  
<a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>Original order</a>
<br />  
<br />  
If you need anything please let us know!
<br />  
<br /> 
Sincerely,
<br /> 
Accelerate Networks
<br /> 
206-858-8757 (call/text)";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                        }
                                    }
                                    catch (FlurlHttpException ex)
                                    {
                                        var error = await ex.GetResponseStringAsync();
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(error);
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    }
                                }
                            }

                            // Create a calendar invite for the install date.
                            if (order.InstallDate > DateTime.Now)
                            {
                                var end = order.InstallDate.GetValueOrDefault().AddHours(3);

                                var attendee = new Attendee
                                {
                                    CommonName = order.FirstName + " " + order.LastName,
                                    Rsvp = true,
                                    Value = new Uri($"mailto:{order.Email}")
                                };

                                var ourRep = string.IsNullOrWhiteSpace(order.SalesEmail) ? new Uri($"mailto:{_emailOrders}") : new Uri($"mailto:{order.SalesEmail}");

                                var e = new CalendarEvent
                                {
                                    Start = new CalDateTime(order.InstallDate.GetValueOrDefault()),
                                    End = new CalDateTime(end),
                                    Summary = "Accelerate Networks Phone Install",
                                    Attendees = [attendee, new() { CommonName = "Accelerate Networks", Rsvp = true, Value = ourRep }],
                                    Organizer = new Organizer { CommonName = "Accelerate Networks", Value = new Uri($"mailto:{_emailOrders}") },
                                };

                                var calendar = new Ical.Net.Calendar();
                                calendar.Events.Add(e);

                                var serializer = new CalendarSerializer();
                                var icalString = serializer.SerializeToString(calendar);

                                confirmationEmail.CalendarInvite = icalString;
                            }

                            // Suppress the confirmation emails.
                            if (order.NoEmail)
                            {
                                confirmationEmail.Completed = false;
                                confirmationEmail.PrimaryEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? _emailOrders : order.SalesEmail;
                                var checkSave = await confirmationEmail.PostAsync(_postgresql);
                                Log.Information($"Suppressed sending out the confirmation emails for {order.OrderId}.");
                            }
                            else
                            {
                                // Queue up the confirmation email.
                                confirmationEmail.Completed = false;
                                var checkSave = await confirmationEmail.PostAsync(_postgresql);
                                Log.Information($"Sent out the confirmation emails for {order.OrderId}.");
                            }

                            // Allow the background work to commence.
                            order.BackgroundWorkCompleted = false;
                            var checkOrderUpdate = order.PutAsync(_postgresql);

                            if (cart is not null && cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Count != 0)
                            {
                                HttpContext.Session.Clear();

                                return View("Success", new OrderWithPorts
                                {
                                    Order = order,
                                    PhoneNumbers = [.. cart.PortedPhoneNumbers]
                                });
                            }
                            else
                            {
                                // Reset the session and clear the Cart.
                                HttpContext.Session.Clear();

                                return View("Success", new OrderWithPorts
                                {
                                    Order = order
                                });
                            }
                        }
                    }
                    // Display an existing order.
                    else
                    {
                        // Reset the session and clear the Cart.
                        HttpContext.Session.Clear();

                        return Redirect($"/Cart/Order/{cart?.Order?.OrderId}");
                    }
                }

                if (cart is not null && cart.Order is not null && cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Count != 0)
                {
                    return View("Success", new OrderWithPorts
                    {
                        Order = cart.Order,
                        PhoneNumbers = [.. cart.PortedPhoneNumbers]
                    });
                }
                else
                {
                    return RedirectToAction("Cart", "Checkout");
                }
            }
            else
            {
                return RedirectToAction("Cart", "Checkout");
            }
        }
    }
}