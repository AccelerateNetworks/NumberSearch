using BulkVS;

using FirstCom;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly int _CallFlow;
        private readonly int _ChannelGroup;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fpcusername;
        private readonly string _fpcpassword;
        private readonly string _invoiceNinjaToken;
        private readonly string _emailOrders;

        public CartController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _ = int.TryParse(_configuration.GetConnectionString("CallFlow"), out _CallFlow);
            _ = int.TryParse(_configuration.GetConnectionString("ChannelGroup"), out _ChannelGroup);
            _apiKey = config.GetConnectionString("BulkVSAPIKEY");
            _apiSecret = config.GetConnectionString("BulkVSAPISecret");
            _fpcusername = config.GetConnectionString("PComNetUsername");
            _fpcpassword = config.GetConnectionString("PComNetPassword");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _emailOrders = config.GetConnectionString("EmailOrders");
        }

        public IActionResult Index()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new CartResult
            {
                Cart = cart
            });
        }

        [Route("Cart/BuyPhoneNumber/{dialedPhoneNumber}")]
        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber, string Query, string View, string Page)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder { DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

            var purchasable = false;

            // Check that the number is still avalible from the provider.
            if (phoneNumber.IngestedFrom == "BulkVS")
            {
                var npanxx = $"{phoneNumber.NPA}{phoneNumber.NXX}";
                var doesItStillExist = await NpaNxxBulkVS.GetAsync(npanxx, _apiKey, _apiSecret).ConfigureAwait(false);
                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[BulkVS] Found {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {npanxx}.");
                }
                else
                {
                    Log.Warning($"[BulkVS] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {npanxx}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}#{phoneNumber.DialedNumber}");
                }

            }
            else if (phoneNumber.IngestedFrom == "TeleMessage")
            {
                // Verify that tele has the number.
                var doesItStillExist = await DidsList.GetAsync(phoneNumber.DialedNumber, _teleToken).ConfigureAwait(false);
                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[TeleMessage] Found {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {phoneNumber.DialedNumber}.");
                }
                else
                {
                    Log.Warning($"[TeleMessage] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {phoneNumber.DialedNumber}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}#{phoneNumber.DialedNumber}");
                }

            }
            else if (phoneNumber.IngestedFrom == "FirstPointCom")
            {
                // Verify that tele has the number.
                var results = await NpaNxxFirstPointCom.GetAsync(phoneNumber.NPA.ToString(new CultureInfo("en-US")), phoneNumber.NXX.ToString(new CultureInfo("en-US")), string.Empty, _fpcusername, _fpcpassword).ConfigureAwait(false);
                var matchingNumber = results.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[FirstPointCom] Found {phoneNumber.DialedNumber} in {results.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");
                }
                else
                {
                    Log.Warning($"[FirstPointCom] Failed to find {phoneNumber.DialedNumber} in {results.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}");
                }
            }
            else
            {
                // Remove numbers that are unpurchasable.
                var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                // Sadly its gone. And the user needs to pick a different number.
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}");
                //return RedirectToAction("Index", "Search", new { Query, View, Page, Failed = phoneNumber.DialedNumber });
            }

            // Check if this number has already been purchased.
            var phoneNumberOrder = await PurchasedPhoneNumber.GetByDialedNumberAsync(phoneNumber.DialedNumber, _postgresql).ConfigureAwait(false);

            // Prevent a duplicate order.
            if (phoneNumberOrder != null)
            {
                purchasable = false;
            }

            if (!purchasable)
            {
                // Remove numbers that are unpurchasable.
                var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                // Sadly its gone. And the user needs to pick a different number.
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}");
                //return RedirectToAction("Index", "Search", new { Query, View, Page, Failed = phoneNumber.DialedNumber });
            }

            var checkAdd = cart.AddPhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}#{phoneNumber.DialedNumber}");
                //return RedirectToAction("Index", "Search", new { Query, View, Page });
            }
            else
            {
                // TODO: Tell the user about the failure
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}&Failed={phoneNumber.DialedNumber}#{phoneNumber.DialedNumber}");
                //return RedirectToAction("Index", "Search", new { Query, View, Page, Failed = phoneNumber.DialedNumber });
            }
        }

        [Route("Cart/PortPhoneNumber/{dialedPhoneNumber}")]
        public async Task<IActionResult> PortPhoneNumberAsync(string dialedPhoneNumber)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var portedPhoneNumber = await PortedPhoneNumber.GetByDialedNumberAsync(dialedPhoneNumber, _postgresql).ConfigureAwait(false);

            if (portedPhoneNumber is null || string.IsNullOrWhiteSpace(portedPhoneNumber.PortedDialedNumber))
            {

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                // Determine if the number is a wireless number.
                var lrnLookup = await LrnLookup.GetAsync(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                bool wireless = false;

                switch (lrnLookup.data.ocn_type)
                {
                    case "wireless":
                        wireless = true;
                        break;
                    case "PCS":
                        wireless = true;
                        break;
                    case "P RESELLER":
                        wireless = true;
                        break;
                    case "Wireless":
                        wireless = true;
                        break;
                    case "W RESELLER":
                        wireless = true;
                        break;
                    default:
                        break;
                }

                Log.Information($"[AddToCart] {dialedPhoneNumber} has an OCN Type of {lrnLookup.data.ocn_type}.");

                if (checkNpa && checkNxx && checkXxxx)
                {
                    var port = new PortedPhoneNumber
                    {
                        PortedDialedNumber = dialedPhoneNumber,
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless
                    };

                    var checkSubmission = await port.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmission)
                    {
                        portedPhoneNumber = await PortedPhoneNumber.GetByDialedNumberAsync(port.PortedDialedNumber, _postgresql).ConfigureAwait(false);
                    }
                }
            }

            // Prevent the user from adding ported numbers that are both wireless and not wireless to the same order.
            if (cart.PortedPhoneNumbers.Any())
            {
                var wirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == true);
                var nonWirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == false);

                if (wirelessCount > 0 && !portedPhoneNumber.Wireless)
                {
                    // Tell the user about the failure
                    return View("../Porting/Index", new PortingResults
                    {
                        PortedPhoneNumber = portedPhoneNumber,
                        Cart = cart,
                        Message = "This phone number cannot be added to an order that already has wireless numbers in it. Please create a separate order for non-wireless numbers.",
                        AlertType = "alert-danger"
                    });
                }

                if (nonWirelessCount > 0 && portedPhoneNumber.Wireless)
                {
                    // Tell the user about the failure
                    return View("../Porting/Index", new PortingResults
                    {
                        PortedPhoneNumber = portedPhoneNumber,
                        Cart = cart,
                        Message = "This wireless phone number cannot be added to an order that already has non-wireless numbers in it. Please create a separate order for wireless numbers.",
                        AlertType = "alert-danger"
                    });
                }
            }

            var productOrder = new ProductOrder { PortedDialedNumber = portedPhoneNumber.PortedDialedNumber, Quantity = 1 };

            var checkAdd = cart.AddPortedPhoneNumber(portedPhoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                return View("../Porting/Index", new PortingResults
                {
                    PortedPhoneNumber = portedPhoneNumber,
                    Cart = cart,
                    Message = portedPhoneNumber.Wireless ? $"Sucessfully added wireless phone number {portedPhoneNumber.PortedDialedNumber} to your cart!" : $"Sucessfully added {dialedPhoneNumber} to your cart!",
                    AlertType = "alert-success"
                });
            }
            else
            {
                // TODO: Tell the user about the failure
                return View("../Porting/Index", new PortingResults
                {
                    PortedPhoneNumber = portedPhoneNumber,
                    Cart = cart,
                    Message = $"Failed to add {dialedPhoneNumber} to your cart.",
                    AlertType = "alert-danger"
                });
            }
        }

        [Route("Cart/BuyProduct/{productId}")]
        public async Task<IActionResult> BuyProductAsync(Guid productId, int Quantity)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var product = await Product.GetAsync(productId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ProductId = product.ProductId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            var checkAdd = cart.AddProduct(product, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                //return RedirectToAction("Index", "Hardware");
                return Redirect($"/Hardware#{product.Name}");
            }
            else
            {
                // TODO: Tell the user about the failure
                //return RedirectToAction("Index", "Hardware");
                return Redirect($"/Hardware#{product.Name}");
            }
        }

        [Route("Cart/BuyService/{serviceId}")]
        public async Task<IActionResult> BuyServiceAsync(Guid serviceId, int Quantity)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var service = await Service.GetAsync(serviceId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ServiceId = service.ServiceId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            var checkAdd = cart.AddService(service, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                return RedirectToAction("Index", "Services");
            }
            else
            {
                // TODO: Tell the user about the failure
                return RedirectToAction("Index", "Services");
            }
        }

        [Route("Cart/RemovePhoneNumber/{dialedPhoneNumber}")]
        public IActionResult RemovePhoneNumber(string dialedPhoneNumber, string Query, string View, string Page)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var phoneNumber = new PhoneNumber { DialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { DialedNumber = dialedPhoneNumber };

            var checkRemove = cart.RemovePhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkRemove && checkSet)
            {
                // TODO: Mark the item as removed.
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}#{phoneNumber.DialedNumber}");
            }
            else
            {
                // TODO: Tell the user about the failure.
                return Redirect($"/Search?Query={Query}&View={View}&Page={Page}#{phoneNumber.DialedNumber}");
            }
        }

        [Route("Cart/RemovePortedPhoneNumber/{dialedPhoneNumber}")]
        public IActionResult RemovePortedPhoneNumber(string dialedPhoneNumber)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var portedPhoneNumber = new PortedPhoneNumber { PortedDialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { PortedDialedNumber = dialedPhoneNumber };

            var checkRemove = cart.RemovePortedPhoneNumber(portedPhoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkRemove && checkSet)
            {
                // TODO: Mark the item as removed.
                return RedirectToAction("Index");
            }
            else
            {
                // TODO: Tell the user about the failure.
                return RedirectToAction("Index");
            }
        }

        [Route("Cart/RemoveProduct/{productId}")]
        public IActionResult RemoveProduct(Guid productId)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var product = new Product { ProductId = productId };
            var productOrder = new ProductOrder { ProductId = productId };

            var checkRemove = cart.RemoveProduct(product, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkRemove && checkSet)
            {
                // TODO: Mark the item as removed.
                return RedirectToAction("Index");
            }
            else
            {
                // TODO: Tell the user about the failure.
                return RedirectToAction("Index");
            }
        }

        [Route("Cart/RemoveService/{serviceId}")]
        public IActionResult RemoveService(Guid serviceId)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var service = new Service { ServiceId = serviceId };
            var productOrder = new ProductOrder { ServiceId = serviceId };

            var checkRemove = cart.RemoveService(service, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkRemove && checkSet)
            {
                // TODO: Mark the item as removed.
                return RedirectToAction("Index");
            }
            else
            {
                // TODO: Tell the user about the failure.
                return RedirectToAction("Index");
            }
        }

        [Route("Cart/Checkout")]
        public IActionResult Checkout()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            // Create a GUID for an order to prevent multiple order submissions from repeated button clicking.
            cart.Order.OrderId = Guid.NewGuid();

            var checkSet = cart.SetToSession(HttpContext.Session);

            return View("Order", cart);
        }

        // Show orders that have already been submitted.
        [Route("Cart/Order/{Id}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
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

                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var phoneNumbers = new List<PhoneNumber>();
                var portedPhoneNumbers = new List<PortedPhoneNumber>();
                var products = new List<Product>();
                var services = new List<Service>();
                foreach (var item in productOrders)
                {
                    if (item?.DialedNumber?.Length == 10)
                    {
                        var phoneNumber = purchasedPhoneNumbers.Where(x => x.DialedNumber == item?.DialedNumber).FirstOrDefault();

                        if (phoneNumber is null)
                        {
                            phoneNumber = new PurchasedPhoneNumber
                            {
                                DialedNumber = item?.DialedNumber,
                                IngestedFrom = "Not Purchased"
                            };
                        }

                        bool checkNpa = int.TryParse(phoneNumber.DialedNumber.Substring(0, 3), out int npa);
                        bool checkNxx = int.TryParse(phoneNumber.DialedNumber.Substring(3, 3), out int nxx);
                        bool checkXxxx = int.TryParse(phoneNumber.DialedNumber.Substring(6), out int xxxx);

                        if (checkNpa && checkNxx && checkXxxx)
                        {
                            phoneNumbers.Add(new PhoneNumber
                            {
                                NPA = npa,
                                NXX = nxx,
                                XXXX = xxxx,
                                DialedNumber = phoneNumber.DialedNumber,
                                IngestedFrom = phoneNumber.IngestedFrom
                            });
                        }
                    }
                    else if (item?.PortedDialedNumber?.Length == 10)
                    {
                        var portedPhoneNumber = await PortedPhoneNumber.GetByDialedNumberAsync(item.PortedDialedNumber, _postgresql).ConfigureAwait(false);
                        portedPhoneNumbers.Add(portedPhoneNumber);
                    }
                    else if (item?.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                        products.Add(product);
                    }
                    else if (item?.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                        services.Add(service);
                    }
                }

                var cart = new Cart
                {
                    Order = order,
                    ProductOrders = productOrders,
                    PhoneNumbers = phoneNumbers,
                    Products = products,
                    Services = services,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                };

                if (AddPortingInfo != null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    var checkSet = cart.SetToSession(HttpContext.Session);

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

        [Route("Cart/Submit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                var cart = Cart.GetFromSession(HttpContext.Session);

                if (order.OrderId != Guid.Empty)
                {
                    var orderExists = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    // Create a new order.
                    if (orderExists is null)
                    {
                        // Save to db.
                        var submittedOrder = await order.PostAsync(_postgresql).ConfigureAwait(false);

                        // Send a confirmation email.
                        if (submittedOrder)
                        {
                            order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                            // Submit the number orders and track the total cost.
                            var onetimeItems = new List<Invoice_Items>();
                            var reoccuringItems = new List<Invoice_Items>();
                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var totalCost = 0;


                            foreach (var nto in cart.PhoneNumbers)
                            {
                                var productOrder = cart.ProductOrders.Where(x => x.DialedNumber == nto.DialedNumber).FirstOrDefault();
                                productOrder.OrderId = order.OrderId;

                                var cost = nto.NumberType == "Executive" ? 200 : nto.NumberType == "Premium" ? 40 : nto.NumberType == "Standard" ? 20 : 20;

                                if (nto.IngestedFrom == "BulkVS")
                                {
                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                                else if (nto.IngestedFrom == "TeleMessage")
                                {

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                                else if (nto.IngestedFrom == "FirstPointCom")
                                {
                                    var checkSubmitted = productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                            }

                            // Track the number of free ports this order qualifies for.
                            var freePortedNumbers = cart.Services.Count();
                            var emailSubject = string.Empty;

                            foreach (var productOrder in cart.ProductOrders)
                            {
                                productOrder.OrderId = order.OrderId;

                                if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                {
                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.DialedNumber : emailSubject;
                                }

                                if (!string.IsNullOrWhiteSpace(productOrder.PortedDialedNumber))
                                {
                                    var ported = cart.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == productOrder.PortedDialedNumber).FirstOrDefault();

                                    // Discount one ported number for each service they purchase.
                                    var calculatedCost = 20;

                                    if (freePortedNumbers > 0)
                                    {
                                        freePortedNumbers--;
                                        calculatedCost = 0;
                                    }
                                    // If they use up all of their free ports, then charge $2 a line.
                                    else if (freePortedNumbers == 0 && cart.Services.Any())
                                    {
                                        calculatedCost = 2;
                                    }

                                    if (ported != null)
                                    {
                                        totalCost += calculatedCost;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = productOrder.PortedDialedNumber,
                                            notes = $"Phone Number to Port to our Network",
                                            cost = calculatedCost,
                                            qty = 1
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.PortedDialedNumber : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }

                                if (productOrder.ProductId != Guid.Empty)
                                {
                                    var product = cart.Products.Where(x => x.ProductId == productOrder.ProductId).FirstOrDefault();

                                    if (product != null)
                                    {
                                        totalCost += product.Price;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = product.Name,
                                            notes = $"{product.Description}",
                                            cost = product.Price,
                                            qty = productOrder.Quantity
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? product.Name : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }

                                if (productOrder.ServiceId != Guid.Empty)
                                {
                                    var service = cart.Services.Where(x => x.ServiceId == productOrder.ServiceId).FirstOrDefault();

                                    if (service != null)
                                    {
                                        totalCost += service.Price;
                                        reoccuringItems.Add(new Invoice_Items
                                        {
                                            product_key = service.Name,
                                            notes = $"{service.Description}",
                                            cost = service.Price,
                                            qty = productOrder.Quantity
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? service.Name : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }
                            }

                            // Associate the ported numbers with this order.
                            foreach (var portedNumber in cart.PortedPhoneNumbers)
                            {
                                var fromDb = await PortedPhoneNumber.GetByDialedNumberAsync(portedNumber.PortedDialedNumber, _postgresql).ConfigureAwait(false);

                                if (fromDb != null && fromDb?.PortedDialedNumber == portedNumber.PortedDialedNumber)
                                {
                                    fromDb.OrderId = order.OrderId;

                                    var checkPortUpdate = await fromDb.PutAsync(_postgresql).ConfigureAwait(false);

                                    Log.Information($"[Checkout] Set the OrderId for port request number {portedNumber.PortedDialedNumber}.");
                                }
                            }

                            // Handle the tax information for the invoice and fall back to simplier queries if we get failures.
                            SalesTax specificTaxRate = null;
                            try
                            {
                                specificTaxRate = await SalesTax.GetAsync(order.Address, order.City, order.Zip).ConfigureAwait(false);
                            }
                            catch
                            {
                                Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.Address}, {order.City}, {order.Zip}.");
                            }

                            if (specificTaxRate is null)
                            {
                                try
                                {
                                    specificTaxRate = await SalesTax.GetAsync(string.Empty, order.City, order.Zip).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.City}, {order.Zip}.");
                                }
                            }

                            if (specificTaxRate is null)
                            {
                                try
                                {
                                    specificTaxRate = await SalesTax.GetAsync(string.Empty, string.Empty, order.Zip).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.Zip}.");
                                }
                            }

                            var billingTaxRate = new TaxRateDatum();

                            if (!(specificTaxRate is null) && !(specificTaxRate.rate is null))
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
                                    }
                                };

                                billingClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            }

                            // Create the invoices for this order and submit it to the billing system.
                            var upfrontInvoice = new InvoiceDatum
                            {
                                id = billingClient.id,
                                invoice_items = onetimeItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate
                            };

                            var reoccuringInvoice = new InvoiceDatum
                            {
                                id = billingClient.id,
                                invoice_items = reoccuringItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate
                            };

                            // If they want just a Quote, create a quote in the billing system, not an invoice.
                            if (order.Quote)
                            {
                                // Mark the invoices as quotes.
                                upfrontInvoice.is_quote = true;
                                upfrontInvoice.invoice_type_id = 2;
                                reoccuringInvoice.is_quote = true;
                                reoccuringInvoice.invoice_type_id = 2;

                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.invoice_items.Any() && reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} and {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
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
                                else if (reoccuringInvoice.invoice_items.Any())
                                {
                                    // Submit them to the billing system.
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>.
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
                                else if (upfrontInvoice.invoice_items.Any())
                                {
                                    // Submit them to the billing system.
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} from Accelerate Networks";
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
                            }
                            else
                            {
                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.invoice_items.Any() && reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} and {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
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
                                else if (reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>.
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
                                else if (upfrontInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} from Accelerate Networks";
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
                                }
                            }

                            // If there are notes on the order don't send out any emails.
                            if (string.IsNullOrWhiteSpace(order.CustomerNotes))
                            {
                                // Send out the confirmation email to the customer.
                                var checkSend = await confirmationEmail.SendEmailAsync(_configuration.GetConnectionString("SmtpUsername"), _configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                                var checkSave = await confirmationEmail.PostAsync(_postgresql).ConfigureAwait(false);

                                if (checkSend && checkSave)
                                {
                                    Log.Information($"Sucessfully sent out the confirmation emails for {order.OrderId}.");
                                }
                                else
                                {
                                    Log.Fatal($"Failed to sent out the confirmation emails for {order.OrderId}.");
                                }
                            }
                            else
                            {
                                Log.Information($"Skipped sending out the confirmation emails for {order.OrderId} due to customer notes.");
                            }


                            if (cart.PortedPhoneNumbers.Any())
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

                        return Redirect($"/Cart/Order/{cart.Order.OrderId}");
                    }
                }

                if (cart.PortedPhoneNumbers.Any())
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