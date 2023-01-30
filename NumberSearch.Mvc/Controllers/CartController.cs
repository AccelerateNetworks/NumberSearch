using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartController : Controller
    {
        private readonly string _postgresql;
        private readonly string _invoiceNinjaToken;
        private readonly string _emailOrders;

        public CartController(MvcConfiguration mvcConfiguration)
        {
            _postgresql = mvcConfiguration.PostgresqlProd;
            _invoiceNinjaToken = mvcConfiguration.InvoiceNinjaToken;
            _emailOrders = mvcConfiguration.EmailOrders;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(bool? emptyCart, string product, int? quantity)
        {
            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
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

                        return View("Index", new CartResult
                        {
                            Cart = cart
                        });
                    }
                    else
                    {
                        return View("Index", new CartResult
                        {
                            Cart = cart
                        });
                    }
                }
                else
                {
                    // Do nothing and return to the cart.
                    return View("Index", new CartResult
                    {
                        Cart = cart
                    });
                }
            }
            else
            {
                return View("Index", new CartResult
                {
                    Cart = cart
                });
            }
        }

        [HttpGet]
        [Route("Cart/Checkout")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckoutAsync()
        {
            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (cart is not null && cart.ProductOrders is not null && !cart.ProductOrders.Any())
            {
                return View("Index", new CartResult { Cart = cart });
            }
            else if (cart is not null && cart.Order is not null)
            {
                // Create a GUID for an order to prevent multiple order submissions from repeated button clicking.
                cart.Order.OrderId = Guid.NewGuid();

                _ = cart.SetToSession(HttpContext.Session);
            }

            return View("Order", cart);
        }

        // Show orders that have already been submitted.
        [HttpGet("Cart/Order/{Id}")]
        [HttpPost("Cart/Order/{Id}")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id, bool? AddPortingInfo)
        {
            if (Id != Guid.Empty)
            {
                var order = await Order.GetByIdAsync(Id, _postgresql).ConfigureAwait(false);
                if (order == null || string.IsNullOrWhiteSpace(order.Email))
                {
                    return View("Index", new CartResult
                    {
                        Cart = null,
                        Message = "Couldn't find this order in our system."
                    });
                }

                if (order.MergedOrderId is not null)
                {
                    return Redirect($"/Cart/Order/{order?.MergedOrderId}");
                }

                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var products = new List<Product>();
                var services = new List<Service>();
                var coupons = new List<Coupon>();
                foreach (var item in productOrders)
                {
                    if (item.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                        products.Add(product);
                    }
                    else if (item.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                        services.Add(service);
                    }
                    else if (item.CouponId is not null)
                    {
                        var coupon = await Coupon.GetByIdAsync(item.CouponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
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

                if (AddPortingInfo != null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    _ = cart.SetToSession(HttpContext.Session);

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = cart.PortedPhoneNumbers
                    });
                }
                else
                {
                    return View("Order", cart);

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
                var order = await Order.GetByIdAsync(Id, _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                if (portedPhoneNumbers.Any())
                {
                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = portedPhoneNumbers
                    });
                }
                else
                {
                    return Redirect($"/Cart/Order/{order.OrderId}");

                }

            }
            else
            {
                return Redirect($"/Cart/");
            }
        }

        [HttpPost("Cart/Submit")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitAsync(Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                await HttpContext.Session.LoadAsync().ConfigureAwait(false);
                var cart = Cart.GetFromSession(HttpContext.Session);

                // This is purely so that we can isolate the state of this call when it fails out.
                Log.Information(JsonSerializer.Serialize(cart));

                if (cart.ProductOrders is null || !cart.ProductOrders.Any())
                {
                    Log.Error("[Checkout] There are no product orders in this sessions cart. How did we get here???");
                    // Reset the session and clear the Cart.
                    HttpContext.Session.Clear();

                    return RedirectToAction("Cart", "Checkout");
                }

                if (order.OrderId != Guid.Empty)
                {
                    var orderExists = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

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
                            Log.Error($"[Checkout] Failed automatic address formating.");
                        }

                        // Fillout the address2 information from its components.
                        if (!string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                        {
                            order.Address2 = $"{order.AddressUnitType} {order.AddressUnitNumber}";
                        }

                        // Save to db.
                        var submittedOrder = await order.PostAsync(_postgresql).ConfigureAwait(false);

                        // Send a confirmation email.
                        if (submittedOrder)
                        {
                            order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
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

                                        var checkPurchaseOrder = await purchsedNumber.PostAsync(_postgresql).ConfigureAwait(false);

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

                            if (cart is not null && cart.ProductOrders is not null)
                            {
                                foreach (var productOrder in cart.ProductOrders)
                                {
                                    productOrder.OrderId = order.OrderId;

                                    if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                    {
                                        emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.DialedNumber : emailSubject;
                                    }

                                    if (productOrder.PortedPhoneNumberId is not null)
                                    {
                                        var ported = cart.PortedPhoneNumbers?.Where(x => x.PortedPhoneNumberId == productOrder.PortedPhoneNumberId).FirstOrDefault();

                                        var calculatedCost = 20;

                                        if (ported != null)
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    }

                                    if (productOrder.VerifiedPhoneNumberId is not null)
                                    {
                                        var verfied = cart.VerifiedPhoneNumbers?.Where(x => x.VerifiedPhoneNumberId == productOrder.VerifiedPhoneNumberId).FirstOrDefault();

                                        if (verfied != null)
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    }

                                    if (productOrder.ProductId != Guid.Empty)
                                    {
                                        var product = cart.Products?.Where(x => x.ProductId == productOrder.ProductId).FirstOrDefault();

                                        if (product != null)
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    }

                                    if (productOrder.ServiceId != Guid.Empty)
                                    {
                                        var service = cart.Services?.Where(x => x.ServiceId == productOrder.ServiceId).FirstOrDefault();

                                        if (service != null)
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    }

                                    // Apply coupon discounts
                                    if (productOrder.CouponId is not null)
                                    {
                                        var coupon = cart.Coupons?.Where(x => x.CouponId == productOrder.CouponId).FirstOrDefault();

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
                                                // If they have selected onsite installation this coupon removes a $60 charge.
                                                if (order.OnsiteInstallation)
                                                {
                                                    onetimeItems.Add(new Line_Items
                                                    {
                                                        product_key = coupon.Name,
                                                        notes = coupon.Description,
                                                        cost = 60 * -1,
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
                                                    var discountTo20 = cart?.PhoneNumbers is not null && cart.PhoneNumbers.Any() ? cart.PhoneNumbers.Count() * 20 :
                                                    cart?.PurchasedPhoneNumbers is not null && cart.PurchasedPhoneNumbers.Any() ? cart.PurchasedPhoneNumbers.Count() * 20 : 0;
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
                                                var servicesToDiscount = cart?.Services is not null && cart.Services.Any() ? cart?.Services?.Where(x => x.Name.Contains("5G")).ToArray() : null;
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

                                        var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    }
                                }
                            }

                            // Handle hardware installation scenarios, if hardware is in the order.
                            if (cart?.Products is not null && cart.Products.Any())
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

                            if (cart is not null && cart.PortedPhoneNumbers is not null)
                            {
                                // Associate the ported numbers with this order.
                                foreach (var portedNumber in cart.PortedPhoneNumbers)
                                {
                                    portedNumber.OrderId = order.OrderId;

                                    var checkPort = await portedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                    Log.Information($"[Checkout] Saved port request for number {portedNumber.PortedDialedNumber}.");
                                }
                            }

                            if (cart is not null && cart.VerifiedPhoneNumbers is not null)
                            {
                                // Associate the verified numbers with this order.
                                foreach (var verifiedNumber in cart.VerifiedPhoneNumbers)
                                {
                                    verifiedNumber.OrderId = order.OrderId;

                                    var checkVerified = await verifiedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                    Log.Information($"[Checkout] Saved Verified Number {verifiedNumber.VerifiedDialedNumber} to the Database.");
                                }
                            }

                            // Handle the tax information for the invoice and fall back to simplier queries if we get failures.
                            SalesTax specificTaxRate = null!;
                            try
                            {
                                // Use our own API
                                specificTaxRate = await SalesTax.GetLocalAPIAsync(order.Address, string.Empty, order.Zip).ConfigureAwait(false);
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
                                    specificTaxRate = await SalesTax.GetAsync(order.Address, order.City, order.Zip).ConfigureAwait(false);
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

                            // Create the confirmation email.
                            var confirmationEmail = new Email
                            {
                                PrimaryEmailAddress = order.Email,
                                SalesEmailAddress = order.SalesEmail,
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
                            var billingClients = await Client.GetByEmailAsync(order.Email, _invoiceNinjaToken).ConfigureAwait(false);
                            var billingClient = billingClients.data.FirstOrDefault();

                            // To get the right data into invoice ninja 5 we must first create the billing client using a unique name,
                            // and then update that billing client with the rest of the address and contact data once we have its id.
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

                                // Create the client and get its id.
                                var newClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                newBillingClient.id = newClient?.id;
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
                                client_id = billingClient.id,
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
                                    line_items = reoccuringItems.ToArray(),
                                    tax_name1 = billingTaxRate.name,
                                    tax_rate1 = billingTaxRate.rate,
                                    entity_type = "quote",
                                };

                                var hiddenReoccurringInvoice = new ReccurringInvoiceDatum
                                {
                                    client_id = billingClient.id,
                                    line_items = reoccuringItems.ToArray(),
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
                                    InvoiceDatum createNewOneTimeInvoice;
                                    InvoiceDatum createNewReoccurringInvoice;

                                    // Retry once on invoice creation failures.
                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewOneTimeInvoice is not null && createNewReoccurringInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, true).ConfigureAwait(false);
                                        var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                        var reoccurringLink = oneTimeInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                        {
                                            order.ReoccuringInvoiceLink = reoccurringLink;
                                        }

                                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                        {
                                            order.UpfrontInvoiceLink = oneTimeLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} and {createNewReoccurringInvoice.number} from Accelerate Networks";
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
                                else if (reoccurringInvoice.line_items.Any())
                                {
                                    // Submit them to the billing system.
                                    InvoiceDatum createNewReoccurringInvoice;
                                    try
                                    {
                                        // Submit them to the billing system.
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        var createNewHiddenReoccurringInvoice = await hiddenReoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                        // Submit them to the billing system.
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewReoccurringInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewReoccurringInvoice.client_id, _invoiceNinjaToken, true).ConfigureAwait(false);
                                        var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                        {
                                            order.ReoccuringInvoiceLink = reoccurringLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewReoccurringInvoice.number} from Accelerate Networks";
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
                                else if (upfrontInvoice.line_items.Any())
                                {
                                    InvoiceDatum createNewOneTimeInvoice;

                                    try
                                    {
                                        // Submit them to the billing system.
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        // Submit them to the billing system.
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewOneTimeInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, true).ConfigureAwait(false);
                                        var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                        {
                                            order.UpfrontInvoiceLink = oneTimeLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} from Accelerate Networks";
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
                            }
                            else
                            {
                                var reoccurringInvoice = new ReccurringInvoiceDatum
                                {
                                    client_id = billingClient.id,
                                    line_items = reoccuringItems.ToArray(),
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
                                    InvoiceDatum createNewOneTimeInvoice;
                                    ReccurringInvoiceDatum createNewReoccurringInvoice;

                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewOneTimeInvoice is not null && createNewReoccurringInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var oneTimeInvoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, false).ConfigureAwait(false);
                                        var recurringInvoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                        var oneTimeLink = oneTimeInvoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                        var reoccurringLink = recurringInvoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                        {
                                            order.ReoccuringInvoiceLink = reoccurringLink;
                                        }

                                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                        {
                                            order.UpfrontInvoiceLink = oneTimeLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} and {createNewReoccurringInvoice.number} from Accelerate Networks";
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
                                else if (reoccurringInvoice.line_items.Any())
                                {
                                    ReccurringInvoiceDatum createNewReoccurringInvoice;

                                    try
                                    {
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(reoccurringInvoice));
                                        createNewReoccurringInvoice = await reoccurringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewReoccurringInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewReoccurringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceReoccuringId = createNewReoccurringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var invoiceLinks = await ReccurringInvoice.GetByClientIdWithLinksAsync(createNewReoccurringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                        var reoccurringLink = invoiceLinks.Where(x => x.id == createNewReoccurringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(reoccurringLink))
                                        {
                                            order.ReoccuringInvoiceLink = reoccurringLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewReoccurringInvoice.number} from Accelerate Networks";
                                        confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href='{reoccurringLink}'>monthly service cost here</a>.
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
                                else if (upfrontInvoice.line_items.Any())
                                {
                                    InvoiceDatum createNewOneTimeInvoice;

                                    try
                                    {
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                        Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                        createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    }

                                    if (createNewOneTimeInvoice is not null)
                                    {
                                        // Update the order with the billing system's client and the two invoice Id's.
                                        order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                        order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                        var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                        var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken, false).ConfigureAwait(false);
                                        var oneTimeLink = invoiceLinks.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                        {
                                            order.UpfrontInvoiceLink = oneTimeLink;
                                        }

                                        confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.number} from Accelerate Networks";
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
                                        if (cart is not null && cart.PhoneNumbers is not null && cart.ProductOrders is not null && cart.PhoneNumbers.Count() == cart.ProductOrders.Count())
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

                                            if (cart.PhoneNumbers.Count() > 1)
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
                                    Attendees = new List<Attendee> { attendee, new Attendee { CommonName = "Accelerate Networks", Rsvp = true, Value = ourRep } },
                                    Organizer = new Organizer { CommonName = "Accelerate Networks", Value = new Uri($"mailto:{_emailOrders}") },
                                };

                                var calendar = new Ical.Net.Calendar();
                                calendar.Events.Add(e);

                                var serializer = new CalendarSerializer();
                                var icalString = serializer.SerializeToString(calendar);

                                confirmationEmail.CalendarInvite = icalString;
                            }

                            // If there are notes on the order don't send out any emails.
                            if (string.IsNullOrWhiteSpace(order.CustomerNotes))
                            {
                                // Queue up the confirmation email.
                                confirmationEmail.Completed = false;
                                var checkSave = await confirmationEmail.PostAsync(_postgresql).ConfigureAwait(false);
                            }
                            else
                            {
                                confirmationEmail.Completed = false;
                                confirmationEmail.DoNotSend = true;
                                var checkSave = await confirmationEmail.PostAsync(_postgresql).ConfigureAwait(false);
                                Log.Information($"Skipped sending out the confirmation emails for {order.OrderId} due to customer notes.");
                            }

                            // Allow the background work to commence.
                            order.BackgroundWorkCompleted = false;
                            var checkOrderUpdate = order.PutAsync(_postgresql).ConfigureAwait(false);

                            if (cart is not null && cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Any())
                            {
                                HttpContext.Session.Clear();

                                return View("Success", new OrderWithPorts
                                {
                                    Order = order,
                                    PhoneNumbers = cart.PortedPhoneNumbers
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

                if (cart is not null && cart.Order is not null && cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Any())
                {
                    return View("Success", new OrderWithPorts
                    {
                        Order = cart.Order,
                        PhoneNumbers = cart.PortedPhoneNumbers
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