using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using CsvHelper;

using FirstCom;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeliMesssage;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;
        private readonly string _username;
        private readonly string _password;
        private readonly Guid _teleToken;
        private readonly string _bulkVSAPIKey;
        private readonly string _bulkVSAPISecret;
        private readonly string _invoiceNinjaToken;
        private readonly string _data247username;
        private readonly string _data247password;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _emailOrders;
        private readonly string _azureStorage;
        private readonly string _emailUsername;
        private readonly string _emailPassword;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            IConfiguration config,
            UserManager<IdentityUser> userManager)
        {
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _username = config.GetConnectionString("PComNetUsername");
            _password = config.GetConnectionString("PComNetPassword");
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
            _bulkVSAPISecret = config.GetConnectionString("BulkVSAPISecret");
            _bulkVSusername = config.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = config.GetConnectionString("BulkVSPassword");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
            _emailOrders = config.GetConnectionString("EmailOrders");
            _azureStorage = config.GetConnectionString("AzureStorageAccount");
            _emailUsername = config.GetConnectionString("SmtpUsername");
            _emailPassword = config.GetConnectionString("SmtpPassword");

            _userManager = userManager;
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

        public async Task<PortedPhoneNumber> VerifyPortablityAsync(string number)
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number, out var phoneNumber);

            if (checkParse)
            {
                try
                {
                    var portable = await LnpCheck.IsPortableAsync(phoneNumber.DialedNumber, _teleToken).ConfigureAwait(false);

                    // Fail fast
                    if (portable is not true)
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                        return new PortedPhoneNumber
                        {
                            PortedDialedNumber = number,
                            Portable = false
                        };
                    }

                    // Lookup the number.
                    var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _bulkVSAPIKey).ConfigureAwait(false);

                    // Determine if the number is a wireless number.
                    bool wireless = false;

                    switch (checkNumber.lectype)
                    {
                        case "WIRELESS":
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

                    var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber, _bulkVSAPIKey);
                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName?.name;

                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");

                    var portableNumber = new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = phoneNumber.DialedNumber,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = checkNumber.city,
                        State = checkNumber.province,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless,
                        LrnLookup = checkNumber,
                        Portable = true
                    };

                    return portableNumber;
                }
                catch (Exception ex)
                {
                    Log.Information($"[Portability] {number} is not Portable.");
                    Log.Fatal($"[Portability] {ex.Message}");
                    Log.Fatal($"[Portability] {ex.InnerException}");

                    return new PortedPhoneNumber
                    {
                        PortedDialedNumber = number,
                        Portable = false
                    };
                }
            }
            else
            {
                Log.Information($"[Portability] {number} is not Portable. Failed NPA, NXX, XXXX parsing.");

                return new PortedPhoneNumber
                {
                    PortedDialedNumber = number,
                    Portable = false
                };
            }
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
                var orders = await Order.GetAllAsync(_postgresql).ConfigureAwait(false);
                var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);
                var productOrders = await ProductOrder.GetAllAsync(_postgresql).ConfigureAwait(false);
                var purchasedNumbers = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                var verifiedNumbers = await VerifiedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                var portedPhoneNumbers = await PortedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var services = await Service.GetAllAsync(_postgresql).ConfigureAwait(false);
                var pairs = new List<OrderProducts>();

                // Show only the relevant Orders to a Sales rep.
                if (User.IsInRole("Sales"))
                {
                    var user = await _userManager.FindByNameAsync(User.Identity.Name);

                    if (user is not null)
                    {
                        orders = orders.Where(x => (x.Quote is not true) && (x.SalesEmail == user.Email));
                    }
                    else
                    {
                        orders = orders.Where(x => x.Quote is not true);
                    }
                }
                else
                {
                    orders = orders.Where(x => x.Quote is not true);
                }

                // Hide merged orders
                //orders = orders.Where(x => x.MergedOrderId is null);

                foreach (var order in orders)
                {
                    var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
                    var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                    pairs.Add(new OrderProducts
                    {
                        Order = order,
                        PortRequest = portRequest,
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
                var order = await Order.GetByIdAsync(orderId ?? new Guid(), _postgresql).ConfigureAwait(false);
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
                    if (item?.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                        products.Add(product);
                    }
                    else if (item?.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                        services.Add(service);
                    }
                    else if (item?.CouponId is not null)
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

                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart });
            }
        }

        [Authorize]
        [Route("/Home/Quotes/")]
        public async Task<IActionResult> Quotes()
        {
            // Show all orders
            var orders = await Order.GetAllAsync(_postgresql).ConfigureAwait(false);
            var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);
            var productOrders = await ProductOrder.GetAllAsync(_postgresql).ConfigureAwait(false);
            var purchasedNumbers = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
            var verifiedNumbers = await VerifiedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
            var portedPhoneNumbers = await PortedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            var services = await Service.GetAllAsync(_postgresql).ConfigureAwait(false);
            var pairs = new List<OrderProducts>();

            // Show only the relevant Orders to a Sales rep.
            if (User.IsInRole("Sales"))
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);

                if (user is not null)
                {
                    orders = orders.Where(x => (x.Quote) && (x.SalesEmail == user.Email));
                }
                else
                {
                    orders = orders.Where(x => x.Quote);
                }
            }
            else
            {
                orders = orders.Where(x => x.Quote);
            }

            foreach (var order in orders)
            {
                var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
                var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                pairs.Add(new OrderProducts
                {
                    Order = order,
                    PortRequest = portRequest,
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
            else
            {
                var existingOrder = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                try
                {
                    // Format the address information
                    Log.Information($"[Checkout] Parsing address data from {order.Address}");
                    var addressParts = order.UnparsedAddress.Split(", ");
                    if (addressParts.Length > 4)
                    {
                        order.Address = addressParts[0];
                        order.City = addressParts[1];
                        order.State = addressParts[2];
                        order.Zip = addressParts[3];
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

                    order.BillingClientId = existingOrder?.BillingClientId;
                    order.BillingInvoiceId = existingOrder?.BillingInvoiceId;
                    order.BillingInvoiceReoccuringId = existingOrder?.BillingInvoiceReoccuringId;
                    order.DateSubmitted = existingOrder.DateSubmitted;

                    var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                    order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
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
                        if (item?.ProductId != Guid.Empty)
                        {
                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                            products.Add(product);
                        }
                        else if (item?.ServiceId != Guid.Empty)
                        {
                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                            services.Add(service);
                        }
                        else if (item?.CouponId is not null)
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


                    if (checkUpdate)
                    {
                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = "Order updated successfully! 😘", AlertType = "alert-success" });
                    }
                    else
                    {
                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = "Failed to update this order! 😠", AlertType = "alert-danger" });
                    }
                }
                catch (Exception ex)
                {
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
                        if (item?.ProductId != Guid.Empty)
                        {
                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                            products.Add(product);
                        }
                        else if (item?.ServiceId != Guid.Empty)
                        {
                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                            services.Add(service);
                        }
                        else if (item?.CouponId is not null)
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

                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to update this order! 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                }
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
                var order = await Order.GetByIdAsync(Guid.Parse(orderId), _postgresql).ConfigureAwait(false);

                if (order is not null && order.OrderId == Guid.Parse(orderId))
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
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
                var order = await Order.GetByIdAsync(Guid.Parse(orderId), _postgresql).ConfigureAwait(false);

                if (order is not null && order.OrderId == Guid.Parse(orderId))
                {
                    if (!string.IsNullOrWhiteSpace(serviceNumber))
                    {
                        order.E911ServiceNumber = serviceNumber;
                        var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);
                        if (!checkUpdate)
                        {
                            // The order failed to update with the new e911 service number.
                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                            var products = new List<Product>();
                            var services = new List<Service>();
                            var coupons = new List<Coupon>();
                            foreach (var item in productOrders)
                            {
                                if (item?.ProductId != Guid.Empty)
                                {
                                    var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                    products.Add(product);
                                }
                                else if (item?.ServiceId != Guid.Empty)
                                {
                                    var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                    services.Add(service);
                                }
                                else if (item?.CouponId is not null)
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

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"The currently selected phone number {serviceNumber} could not be added to the order. 🤔", AlertType = "alert-warning" });
                        }
                    }

                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(order.E911ServiceNumber, out var phoneNumber);
                    // Register the number with Teli for E911 service.
                    if (checkParse)
                    {
                        var existingRegistration = await EmergencyInfo.GetAsync(phoneNumber.DialedNumber, _teleToken);

                        if (existingRegistration.code == 200)
                        {
                            // This number is already registered with Teli.
                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                            var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                            var products = new List<Product>();
                            var services = new List<Service>();
                            var coupons = new List<Coupon>();
                            foreach (var item in productOrders)
                            {
                                if (item?.ProductId != Guid.Empty)
                                {
                                    var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                    products.Add(product);
                                }
                                else if (item?.ServiceId != Guid.Empty)
                                {
                                    var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                    services.Add(service);
                                }
                                else if (item?.CouponId is not null)
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

                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"The currently selected phone number {phoneNumber.DialedNumber} is already registered for E911 service. 🤔", AlertType = "alert-warning" });
                        }
                        else
                        {
                            // Register the number with Teli for E911 service.
                            var checkAddressValid = await EmergencyInfo.ValidateAddressAsync(order.Address, order.City, order.State, order.Zip, _teleToken);

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
                                                    var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                                    var products = new List<Product>();
                                                    var services = new List<Service>();
                                                    var coupons = new List<Coupon>();
                                                    foreach (var item in productOrders)
                                                    {
                                                        if (item?.ProductId != Guid.Empty)
                                                        {
                                                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                            products.Add(product);
                                                        }
                                                        else if (item?.ServiceId != Guid.Empty)
                                                        {
                                                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                            services.Add(service);
                                                        }
                                                        else if (item?.CouponId is not null)
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

                                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and the job to register it failed. {checkJobStatus?.data}", AlertType = "alert-danger" });
                                                }
                                            }

                                            // If the job completed successfully get the did_id.
                                            var didDetails = await DidsGet.GetAsync(phoneNumber.DialedNumber, _teleToken);
                                            if (didDetails?.code == 200 && !string.IsNullOrWhiteSpace(didDetails?.data?.id))
                                            {
                                                var fullName = $"{order.FirstName} {order.LastName}";
                                                EmergencyInfo E911Request = null;
                                                // Request E911 service for this number at the previously validated address.
                                                if (string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                                                {
                                                    E911Request = await EmergencyInfo.CreateE911RecordAsync(didDetails.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);

                                                }
                                                else
                                                {
                                                    E911Request = await EmergencyInfo.CreateE911RecordAsync(didDetails.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                                }

                                                if (E911Request is not null && E911Request?.code == 200)
                                                {
                                                    // Congrats we did it!
                                                    var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                                    var products = new List<Product>();
                                                    var services = new List<Service>();
                                                    var coupons = new List<Coupon>();
                                                    foreach (var item in productOrders)
                                                    {
                                                        if (item?.ProductId != Guid.Empty)
                                                        {
                                                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                            products.Add(product);
                                                        }
                                                        else if (item?.ServiceId != Guid.Empty)
                                                        {
                                                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                            services.Add(service);
                                                        }
                                                        else if (item?.CouponId is not null)
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

                                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully registered {phoneNumber.DialedNumber} with E911! 🥳", AlertType = "alert-success" });
                                                }
                                                else
                                                {
                                                    // Failed to get the did_id.
                                                    var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                    var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                                    var products = new List<Product>();
                                                    var services = new List<Service>();
                                                    var coupons = new List<Coupon>();
                                                    foreach (var item in productOrders)
                                                    {
                                                        if (item?.ProductId != Guid.Empty)
                                                        {
                                                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                            products.Add(product);
                                                        }
                                                        else if (item?.ServiceId != Guid.Empty)
                                                        {
                                                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                            services.Add(service);
                                                        }
                                                        else if (item?.CouponId is not null)
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

                                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we failed to register an address against its did_id. {E911Request?.error}", AlertType = "alert-danger" });
                                                }
                                            }
                                            else
                                            {
                                                // Failed to get the did_id.
                                                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                                var products = new List<Product>();
                                                var services = new List<Service>();
                                                var coupons = new List<Coupon>();
                                                foreach (var item in productOrders)
                                                {
                                                    if (item?.ProductId != Guid.Empty)
                                                    {
                                                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                        products.Add(product);
                                                    }
                                                    else if (item?.ServiceId != Guid.Empty)
                                                    {
                                                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                        services.Add(service);
                                                    }
                                                    else if (item?.CouponId is not null)
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

                                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we could not lookup it's did_id. {didDetails?.ErrorData}", AlertType = "alert-danger" });
                                            }

                                        }
                                        else
                                        {
                                            // Number cannot be register with Teli as an offnet number.
                                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                            var products = new List<Product>();
                                            var services = new List<Service>();
                                            var coupons = new List<Coupon>();
                                            foreach (var item in productOrders)
                                            {
                                                if (item?.ProductId != Guid.Empty)
                                                {
                                                    var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                    products.Add(product);
                                                }
                                                else if (item?.ServiceId != Guid.Empty)
                                                {
                                                    var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                    services.Add(service);
                                                }
                                                else if (item?.CouponId is not null)
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

                                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and cannot be registered. {offnetInsertJob?.error}", AlertType = "alert-danger" });
                                        }
                                    }
                                    else
                                    {
                                        // Number cannot be register with Teli as an offnet number.
                                        var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                        var products = new List<Product>();
                                        var services = new List<Service>();
                                        var coupons = new List<Coupon>();
                                        foreach (var item in productOrders)
                                        {
                                            if (item?.ProductId != Guid.Empty)
                                            {
                                                var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                products.Add(product);
                                            }
                                            else if (item?.ServiceId != Guid.Empty)
                                            {
                                                var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                services.Add(service);
                                            }
                                            else if (item?.CouponId is not null)
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

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and cannot be registered. {checkOffnet?.error}", AlertType = "alert-danger" });
                                    }
                                }
                                else
                                {
                                    // Get the did_id and use it to register the number.
                                    if (checkNumber?.code == 200 && !string.IsNullOrWhiteSpace(checkNumber?.data?.id))
                                    {
                                        var fullName = $"{order.FirstName} {order.LastName}";
                                        EmergencyInfo E911Request = null;
                                        // Request E911 service for this number at the previously validated address.
                                        if (string.IsNullOrWhiteSpace(order.AddressUnitNumber))
                                        {
                                            E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            }

                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, string.Empty, string.Empty, _teleToken);
                                            }
                                        }
                                        else
                                        {
                                            E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            }

                                            if (E911Request is not null && E911Request?.code != 200)
                                            {
                                                // Wait and then retry the request.
                                                await Task.Delay(1000);
                                                E911Request = await EmergencyInfo.CreateE911RecordAsync(checkNumber.data.id, fullName, order.Address, order.City, order.State, order.Zip, order.AddressUnitType, order.AddressUnitNumber, _teleToken);
                                            }
                                        }

                                        if (E911Request is not null && E911Request?.code == 200)
                                        {
                                            // Congrats we did it!
                                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                            var products = new List<Product>();
                                            var services = new List<Service>();
                                            var coupons = new List<Coupon>();
                                            foreach (var item in productOrders)
                                            {
                                                if (item?.ProductId != Guid.Empty)
                                                {
                                                    var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                    products.Add(product);
                                                }
                                                else if (item?.ServiceId != Guid.Empty)
                                                {
                                                    var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                    services.Add(service);
                                                }
                                                else if (item?.CouponId is not null)
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

                                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully registered {phoneNumber.DialedNumber} with E911! 🥳", AlertType = "alert-success" });
                                        }
                                        else
                                        {
                                            // Failed to get the did_id.
                                            var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                            var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                            var products = new List<Product>();
                                            var services = new List<Service>();
                                            var coupons = new List<Coupon>();
                                            foreach (var item in productOrders)
                                            {
                                                if (item?.ProductId != Guid.Empty)
                                                {
                                                    var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                    products.Add(product);
                                                }
                                                else if (item?.ServiceId != Guid.Empty)
                                                {
                                                    var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                    services.Add(service);
                                                }
                                                else if (item?.CouponId is not null)
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

                                            return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we failed to register an address against its did_id. {E911Request?.error}", AlertType = "alert-danger" });
                                        }
                                    }
                                    else
                                    {
                                        // Failed to get the did_id.
                                        var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                        var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                        var products = new List<Product>();
                                        var services = new List<Service>();
                                        var coupons = new List<Coupon>();
                                        foreach (var item in productOrders)
                                        {
                                            if (item?.ProductId != Guid.Empty)
                                            {
                                                var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                                products.Add(product);
                                            }
                                            else if (item?.ServiceId != Guid.Empty)
                                            {
                                                var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                                services.Add(service);
                                            }
                                            else if (item?.CouponId is not null)
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

                                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the number is offnet and is registered. But we could not lookup it's did_id. {checkNumber?.status}", AlertType = "alert-danger" });
                                    }
                                }
                            }
                            else
                            {
                                // Address is invalid, inform the user.
                                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                var products = new List<Product>();
                                var services = new List<Service>();
                                var coupons = new List<Coupon>();
                                foreach (var item in productOrders)
                                {
                                    if (item?.ProductId != Guid.Empty)
                                    {
                                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                        products.Add(product);
                                    }
                                    else if (item?.ServiceId != Guid.Empty)
                                    {
                                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                        services.Add(service);
                                    }
                                    else if (item?.CouponId is not null)
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

                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to register with E911! 😠 Teli reports that the Address associated with this Order is invalid. {checkAddressValid?.data}", AlertType = "alert-danger" });
                            }
                        }
                    }
                    else
                    {
                        var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                        var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                        var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                        var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                        var products = new List<Product>();
                        var services = new List<Service>();
                        var coupons = new List<Coupon>();
                        foreach (var item in productOrders)
                        {
                            if (item?.ProductId != Guid.Empty)
                            {
                                var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                products.Add(product);
                            }
                            else if (item?.ServiceId != Guid.Empty)
                            {
                                var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                services.Add(service);
                            }
                            else if (item?.CouponId is not null)
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

                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = "Failed to register with E911! 😠 The currently selected phone number is not a valid value.", AlertType = "alert-danger" });
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
                var parent = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);
                var child = await Order.GetByIdAsync(mergeId, _postgresql).ConfigureAwait(false);

                if (parent is not null && child is not null)
                {
                    try
                    {
                        var productOrders = await ProductOrder.GetAsync(child.OrderId, _postgresql).ConfigureAwait(false);
                        var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(child.OrderId, _postgresql).ConfigureAwait(false);
                        var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(child.OrderId, _postgresql).ConfigureAwait(false);
                        var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(child.OrderId, _postgresql).ConfigureAwait(false);

                        foreach (var item in productOrders)
                        {
                            item.OrderId = parent.OrderId;
                            var checkUpdate = await item.PutAsync(_postgresql).ConfigureAwait(false);
                        }

                        foreach (var item in purchasedPhoneNumbers)
                        {
                            item.OrderId = parent.OrderId;
                            var checkUpdate = await item.PutAsync(_postgresql).ConfigureAwait(false);
                        }

                        foreach (var item in verifiedPhoneNumbers)
                        {
                            item.OrderId = parent.OrderId;
                            var checkUpdate = await item.PutAsync(_postgresql).ConfigureAwait(false);
                        }

                        foreach (var item in portedPhoneNumbers)
                        {
                            item.OrderId = parent.OrderId;
                            var checkUpdate = await item.PutAsync(_postgresql).ConfigureAwait(false);
                        }

                        // Redirect requests for the child order to the parent order it was merged into.
                        child.MergedOrderId = parent.OrderId;
                        var checkMerge = await child.PutAsync(_postgresql).ConfigureAwait(false);

                        productOrders = await ProductOrder.GetAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);

                        // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                        // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                        var products = new List<Product>();
                        var services = new List<Service>();
                        var coupons = new List<Coupon>();
                        foreach (var item in productOrders)
                        {
                            if (item?.ProductId != Guid.Empty)
                            {
                                var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                products.Add(product);
                            }
                            else if (item?.ServiceId != Guid.Empty)
                            {
                                var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                services.Add(service);
                            }
                            else if (item?.CouponId is not null)
                            {
                                var coupon = await Coupon.GetByIdAsync(item.CouponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
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

                        if (checkMerge)
                        {
                            return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Successfully merged {parent.OrderId} with {child.OrderId} 🥳", AlertType = "alert-success" });
                        }
                        else
                        {
                            return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Failed to merge {parent.OrderId} with {child.OrderId} 😠 \r\n Please file a bug report on Github for this issue.", AlertType = "alert-danger" });
                        }
                    }
                    catch (Exception ex)
                    {
                        var productOrders = await ProductOrder.GetAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                        var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);

                        // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                        // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                        var products = new List<Product>();
                        var services = new List<Service>();
                        var coupons = new List<Coupon>();
                        foreach (var item in productOrders)
                        {
                            if (item?.ProductId != Guid.Empty)
                            {
                                var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                                products.Add(product);
                            }
                            else if (item?.ServiceId != Guid.Empty)
                            {
                                var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                                services.Add(service);
                            }
                            else if (item?.CouponId is not null)
                            {
                                var coupon = await Coupon.GetByIdAsync(item.CouponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
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
                else
                {
                    var productOrders = await ProductOrder.GetAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                    var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                    var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);
                    var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(parent.OrderId, _postgresql).ConfigureAwait(false);

                    // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                    // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                    var products = new List<Product>();
                    var services = new List<Service>();
                    var coupons = new List<Coupon>();
                    foreach (var item in productOrders)
                    {
                        if (item?.ProductId != Guid.Empty)
                        {
                            var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                            products.Add(product);
                        }
                        else if (item?.ServiceId != Guid.Empty)
                        {
                            var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                            services.Add(service);
                        }
                        else if (item?.CouponId is not null)
                        {
                            var coupon = await Coupon.GetByIdAsync(item.CouponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
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

                    return View("OrderEdit", new EditOrderResult { Order = parent, Cart = cart, Message = $"Failed to merge {parent.OrderId} with {child.OrderId} 😠 The second orderId does not exist.", AlertType = "alert-warning" });
                }
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
                var order = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);
                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var products = new List<Product>();
                var services = new List<Service>();
                var coupons = new List<Coupon>();

                foreach (var item in productOrders)
                {
                    if (item?.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetByIdAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                        products.Add(product);
                    }
                    else if (item?.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                        services.Add(service);
                    }
                    else if (item?.CouponId is not null)
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

                if (order is not null && cart.ProductOrders.Any())
                {
                    try
                    {
                        // Submit the number orders and track the total cost.
                        var onetimeItems = new List<Invoice_Items>();
                        var reoccuringItems = new List<Invoice_Items>();
                        var totalCost = 0;

                        // Delete the old invoices in the billing system.
                        if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId))
                        {
                            var checkParse = int.TryParse(order.BillingInvoiceId, out var oneTimeId);

                            if (checkParse)
                            {
                                try
                                {
                                    var existingOneTimeInvoice = await Invoice.GetByIdAsync(oneTimeId, _invoiceNinjaToken);
                                    var checkDelete = await existingOneTimeInvoice.DeleteAsync(_invoiceNinjaToken);
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Error($"[Regenerate Invoices] Failed to delete Invoice Id: {oneTimeId} for OrderId: {order.OrderId}");
                                    Log.Error(await ex.GetResponseStringAsync());
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 😠\r\n{await ex.GetResponseStringAsync()}\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                        {
                            var checkParse = int.TryParse(order.BillingInvoiceReoccuringId, out var reoccuringId);

                            if (checkParse)
                            {
                                try
                                {
                                    var existingReoccuringInvoice = await Invoice.GetByIdAsync(reoccuringId, _invoiceNinjaToken);
                                    var checkDelete = await existingReoccuringInvoice.DeleteAsync(_invoiceNinjaToken);
                                }
                                catch (FlurlHttpException ex)
                                {
                                    Log.Error($"[Regenerate Invoices] Failed to delete Invoice Id: {reoccuringId} for OrderId: {order.OrderId}");
                                    Log.Error(await ex.GetResponseStringAsync());
                                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 😠\r\n{await ex.GetResponseStringAsync()}\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                                }
                            }
                        }

                        foreach (var nto in cart.PurchasedPhoneNumbers)
                        {
                            var cost = nto.NumberType == "Executive" ? 200 : nto.NumberType == "Premium" ? 40 : nto.NumberType == "Standard" ? 20 : 20;

                            onetimeItems.Add(new Invoice_Items
                            {
                                product_key = nto.DialedNumber,
                                notes = $"{nto.NumberType} Phone Number",
                                cost = cost,
                                qty = 1
                            });
                        }

                        var totalPortingCost = 0;

                        foreach (var productOrder in cart.ProductOrders)
                        {
                            productOrder.OrderId = order.OrderId;

                            if (productOrder.PortedPhoneNumberId is not null)
                            {
                                var ported = cart.PortedPhoneNumbers.Where(x => x.PortedPhoneNumberId == productOrder.PortedPhoneNumberId).FirstOrDefault();

                                var calculatedCost = 20;

                                if (ported != null)
                                {
                                    totalCost += calculatedCost;
                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = ported.PortedDialedNumber,
                                        notes = $"Phone Number to Port to our Network",
                                        cost = calculatedCost,
                                        qty = 1
                                    });
                                }

                                totalPortingCost += calculatedCost;
                            }

                            if (productOrder.VerifiedPhoneNumberId is not null)
                            {
                                var verfied = cart.VerifiedPhoneNumbers.Where(x => x.VerifiedPhoneNumberId == productOrder.VerifiedPhoneNumberId).FirstOrDefault();

                                if (verfied != null)
                                {
                                    totalCost += 10;
                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = verfied.VerifiedDialedNumber,
                                        notes = $"Phone Number to Verify Daily",
                                        cost = 10,
                                        qty = 1
                                    });
                                }
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
                            }

                            // Apply coupon discounts
                            if (productOrder.CouponId is not null)
                            {
                                var coupon = cart.Coupons.Where(x => x.CouponId == productOrder.CouponId).FirstOrDefault();

                                if (coupon is not null)
                                {
                                    if (coupon.Type == "Port")
                                    {

                                        totalCost -= totalPortingCost;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = totalPortingCost * -1,
                                            qty = 1
                                        });
                                    }
                                    else if (coupon.Name == "Install")
                                    {

                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = 60 * -1,
                                            qty = 1
                                        });
                                    }
                                    else
                                    {
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = coupon.Name,
                                            notes = coupon.Description,
                                            cost = coupon.Value * -1,
                                            qty = 1
                                        });
                                    }
                                }
                            }
                        }

                        // Handle hardware installation senarios, if hardware is in the order.
                        if (cart?.Products is not null && cart.Products.Any())
                        {
                            if (order.OnsiteInstallation)
                            {
                                onetimeItems.Add(new Invoice_Items
                                {
                                    product_key = "Onsite Hardware Installation",
                                    notes = $"We'll come visit you and get all your phones setup.",
                                    cost = 60,
                                    qty = 1
                                });
                            }
                            else
                            {
                                onetimeItems.Add(new Invoice_Items
                                {
                                    product_key = "Remote Installation",
                                    notes = $"We'll walk you through getting all your phones setup virtually.",
                                    cost = 0,
                                    qty = 1
                                });
                            }
                        }

                        // Handle the tax information for the invoice and fall back to simplier queries if we get failures.
                        SalesTax specificTaxRate = null;
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

                            billingClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
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
                            invoice_items = onetimeItems.ToArray(),
                            tax_name1 = billingTaxRate.name,
                            tax_rate1 = billingTaxRate.rate
                        };

                        var reoccuringInvoice = new InvoiceDatum
                        {
                            id = billingClient.id,
                            invoice_items = reoccuringItems.ToArray(),
                            tax_name1 = billingTaxRate.name,
                            tax_rate1 = billingTaxRate.rate,
                            is_recurring = true,
                            frequency_id = 4
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
                                InvoiceDatum createNewOneTimeInvoice;
                                InvoiceDatum createNewReoccuringInvoice;

                                // Retry once on invoice creation failures.
                                try
                                {
                                    createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                    Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    Log.Fatal(JsonSerializer.Serialize(reoccuringInvoice));
                                    createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }

                                if (createNewOneTimeInvoice is not null && createNewReoccuringInvoice is not null)
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccuringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccuringLink;
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
                            else if (reoccuringInvoice.invoice_items.Any())
                            {
                                // Submit them to the billing system.
                                InvoiceDatum createNewReoccuringInvoice;
                                try
                                {
                                    // Submit them to the billing system.
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                    Log.Fatal(JsonSerializer.Serialize(reoccuringInvoice));
                                    // Submit them to the billing system.
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }

                                if (createNewReoccuringInvoice is not null)
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));

                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccuringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccuringLink;
                                    }
                                }
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (upfrontInvoice.invoice_items.Any())
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
                                    var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

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

                            var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                            if (checkQuoteUpdate)
                            {
                                order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully deleted the existing Invoices and created new Invoices for this quote! 🥳", AlertType = "alert-success" });
                            }
                        }
                        else
                        {
                            // Submit them to the billing system if they have items.
                            if (upfrontInvoice.invoice_items.Any() && reoccuringInvoice.invoice_items.Any())
                            {
                                InvoiceDatum createNewOneTimeInvoice;
                                InvoiceDatum createNewReoccuringInvoice;
                                try
                                {
                                    createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                    Log.Fatal(JsonSerializer.Serialize(upfrontInvoice));
                                    Log.Fatal(JsonSerializer.Serialize(reoccuringInvoice));
                                    createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }

                                if (createNewOneTimeInvoice is not null && createNewReoccuringInvoice is not null)
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccuringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccuringLink;
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
                            else if (reoccuringInvoice.invoice_items.Any())
                            {
                                InvoiceDatum createNewReoccuringInvoice;

                                try
                                {
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal("[Checkout] Failed to create the invoices in the billing system on the first attempt.");
                                    Log.Fatal(JsonSerializer.Serialize(reoccuringInvoice));
                                    createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }

                                if (createNewReoccuringInvoice is not null)
                                {
                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

                                    if (!string.IsNullOrWhiteSpace(reoccuringLink))
                                    {
                                        order.ReoccuringInvoiceLink = reoccuringLink;
                                    }
                                }
                                else
                                {
                                    Log.Fatal("[Checkout] Invoices were not successfully created in the billing system.");
                                }
                            }
                            else if (upfrontInvoice.invoice_items.Any())
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

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    Log.Information(JsonSerializer.Serialize(invoiceLinks));
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault()?.invitations.FirstOrDefault()?.link;

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

                            var checkInvoiceUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                            if (checkInvoiceUpdate)
                            {
                                order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Successfully deleted the existing Invoices and created new Invoices for this order! 🥳", AlertType = "alert-success" });
                            }
                        }

                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 🤔", AlertType = "alert-danger" });
                    }
                    catch (Exception ex)
                    {
                        return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. 😠\r\n{ex.Message}\r\n{ex.StackTrace}", AlertType = "alert-danger" });
                    }
                }
                else
                {
                    return View("OrderEdit", new EditOrderResult { Order = order, Cart = cart, Message = $"Failed to regenerate the invoices for {order.OrderId}. Either the order could not be found or there are no Product Orders assocated with this Order. 🤔", AlertType = "alert-danger" });
                }
            }
        }

        [Authorize]
        [Route("/Home/NumberOrders")]
        [Route("/Home/NumberOrder/{orderId}")]
        [Route("/Home/NumberOrders/{dialedNumber}")]
        public async Task<IActionResult> NumberOrders(Guid? orderId, string dialedNumber)
        {
            if (orderId.HasValue)
            {
                var orders = await PurchasedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                if (orders is not null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        // Update the product orders here.
                    }
                }

                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
            else if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
            else
            {
                var order = await PurchasedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);

                return View("NumberOrders", new List<PurchasedPhoneNumber> { order });
            }
        }

        [Authorize]
        [Route("/Home/NumbersToVerify")]
        [Route("/Home/NumbersToVerify/{orderId}")]
        public async Task<IActionResult> NumbersToVerify(Guid? orderId)
        {
            if (orderId.HasValue)
            {
                var orders = await VerifiedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                if (orders is not null && orders.Any())
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
                var orders = await VerifiedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("NumbersToVerify", orders.OrderByDescending(x => x.DateToExpire));
            }
        }

        [Authorize]
        [Route("/Home/EmergencyInformation")]
        [Route("/Home/EmergencyInformation/{dialedNumber}")]
        public async Task<IActionResult> AllEmergencyInformation(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var info = await EmergencyInformation.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("EmergencyInformation", info.OrderByDescending(x => x.DateIngested));
            }
            else
            {
                var info = await EmergencyInformation.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("EmergencyInformationEdit", info.FirstOrDefault());
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers")]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        public async Task<IActionResult> OwnedNumbers(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await OwnedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("OwnedNumbers", orders.OrderByDescending(x => x.DialedNumber));
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnedNumberUpdate(OwnedPhoneNumber number)
        {
            if (number is null)
            {
                return Redirect("/Home/OwnedNumbers");
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                order.Notes = number.Notes;
                order.OwnedBy = number.OwnedBy;
                order.BillingClientId = number.BillingClientId;
                order.Active = number.Active;
                order.SPID = order.SPID;
                order.SPIDName = order.SPIDName;

                var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        public async Task<IActionResult> ExportNumberOrders()
        {
            var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

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
                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
        }

        [Authorize]
        [Route("/Home/PortRequests")]
        [Route("/Home/PortRequests/{orderId}")]
        public async Task<IActionResult> PortRequests(Guid? orderId)
        {
            if (orderId is not null && orderId.HasValue)
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                // Show all orders
                var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("PortRequests", portRequests.OrderByDescending(x => x.DateSubmitted));
            }
        }

        [Authorize]
        [Route("/Home/BillImage/{orderId}/")]
        public async Task<FileContentResult> DownloadAsync(string orderId)
        {
            // Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(_azureStorage);

            //Create a unique name for the container
            string containerName = orderId;

            // Create the container and return a container client object
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var files = new List<BlobItem>();

            await foreach (var item in containerClient.GetBlobsAsync())
            {
                files.Add(item);
            }

            var billImage = files.FirstOrDefault();

            if (billImage is null)
            {
                //return new FileContentResult();
            }

            var blobClient = containerClient.GetBlobClient(billImage.Name);
            var download = await blobClient.DownloadAsync();

            var fileBytes = new byte[] { };

            using (var downloadFileStream = new MemoryStream())
            {
                await download.Value.Content.CopyToAsync(downloadFileStream);

                fileBytes = downloadFileStream.ToArray();
            }


            return new FileContentResult(fileBytes, download.Value.ContentType)
            {
                FileDownloadName = billImage.Name
            };
        }

        [Authorize]
        [Route("/Home/PortRequest/{orderId}/Delete")]
        public async Task<IActionResult> PortRequestDelete(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var portrequest = await PortRequest.GetByOrderIdAsync(Guid.Parse(orderId), _postgresql).ConfigureAwait(false);

                if (portrequest is not null && portrequest.OrderId == Guid.Parse(orderId))
                {
                    var checkDelete = await portrequest.DeleteAsync(_postgresql).ConfigureAwait(false);
                }

                return Redirect("/Home/PortRequests");
            }
        }

        [Authorize]
        [HttpGet("/Home/Shipment/")]
        [HttpGet("/Home/Shipment/{ProductShipmentId}")]
        public async Task<IActionResult> ShipmentsAsync(Guid? ProductShipmentId)
        {
            if (ProductShipmentId is null || !ProductShipmentId.HasValue)
            {
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
            }
            else
            {
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var checkExists = await ProductShipment.GetByIdAsync(ProductShipmentId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = new List<ProductShipment> { checkExists }, Shipment = checkExists });
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
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var checkExists = await ProductShipment.GetByIdAsync(shipment.ProductShipmentId, _postgresql).ConfigureAwait(false);

                if (checkExists is null)
                {
                    if (string.IsNullOrWhiteSpace(shipment.Name))
                    {
                        shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault().Name;
                    }
                    var checkSave = await shipment.PostAsync(_postgresql).ConfigureAwait(false);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(shipment.Name))
                    {
                        shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault().Name;
                    }

                    var checkUpdate = await shipment.PutAsync(_postgresql).ConfigureAwait(false);
                }

                var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

                // Update all product inventory counts when a shipment is added or updated.
                foreach (var product in products)
                {
                    var relatedShipments = shipments.Where(x => x.ProductId == product.ProductId);
                    var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                    var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                    product.QuantityAvailable = instockItems - assignedItems;

                    var checkStock = await product.PutAsync(_postgresql).ConfigureAwait(false);
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
                var order = await ProductShipment.GetByIdAsync(productShipmentId, _postgresql).ConfigureAwait(false);

                if (order is not null && order.ProductShipmentId == productShipmentId)
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
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
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Products", new InventoryResult { Products = products });
            }
            else
            {
                var products = await Product.GetByIdAsync(ProductId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("Products", new InventoryResult { Products = new List<Product> { products }, Product = products });
            }
        }

        [Authorize]
        [Route("/Home/Product")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductCreate(Product product)
        {
            var checkExists = await Product.GetByIdAsync(product.ProductId, _postgresql).ConfigureAwait(false);

            if (checkExists is null)
            {
                var checkSave = await product.PostAsync(_postgresql).ConfigureAwait(false);
            }
            else
            {
                var checkUpdate = await product.PutAsync(_postgresql).ConfigureAwait(false);
            }

            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

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
                var order = await Product.GetByIdAsync(productId, _postgresql).ConfigureAwait(false);

                if (order is not null && order.ProductId == productId)
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
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
                var results = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupons = results });
            }
            else
            {
                // Show all orders
                var result = await Coupon.GetByIdAsync(couponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupon = result, Coupons = new List<Coupon> { result } });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("/Home/Coupons/{couponId}/Delete")]
        public async Task<IActionResult> DeleteCouponAsync(Guid? couponId)
        {
            if (couponId is null)
            {
                var results = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupons = results });
            }
            else
            {
                // Show all orders
                var result = await Coupon.GetByIdAsync(couponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                var checkDelete = await result.DeleteAsync(_postgresql).ConfigureAwait(false);

                var results = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupons = results });
            }
        }


        [Authorize]
        [Route("/Home/Coupon")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CouponCreate(Coupon coupon)
        {
            var coupons = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

            var checkExists = coupons.Where(x => x.Name == coupon.Name).FirstOrDefault();

            if (checkExists is null)
            {
                coupon.CouponId = Guid.NewGuid();
                var checkSave = await coupon.PostAsync(_postgresql).ConfigureAwait(false);
            }
            else
            {
                coupon.CouponId = checkExists.CouponId;
                var checkUpdate = await coupon.PutAsync(_postgresql).ConfigureAwait(false);
            }

            coupons = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

            return View("Coupons", new CouponResult { Coupons = coupons });
        }

        [Authorize]
        [HttpGet]
        [Route("/Home/TaxRates")]
        [Route("/Home/TaxRates/{taxRateId}")]
        public async Task<IActionResult> TaxRates(int? taxRateId)
        {
            if (taxRateId != null && taxRateId > 0)
            {
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = new TaxRate
                    {
                        data = result.data.Where(x => x.id == taxRateId).ToArray()
                    }
                });
            }
            else
            {
                // Show all orders
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = result
                }
                );
            }
        }

        [Authorize]
        [Route("/Home/TaxRates")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaxRatesCreate(TaxRateResult location)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.Zip))
            {
                return Redirect("/Home/TaxRates");
            }
            else
            {
                try
                {
                    // Retry logic because this endpoint is sketchy.
                    var specificTaxRate = new SalesTax();
                    var retryCount = 0;

                    while (specificTaxRate?.localrate == 0M)
                    {
                        try
                        {
                            specificTaxRate = await SalesTax.GetLocalAPIAsync(location.Address, location.City, location.Zip).ConfigureAwait(false);
                        }
                        catch
                        {
                            if (retryCount > 10)
                            {
                                throw;
                            }

                            retryCount++;
                            await Task.Delay(1000);
                            // Do nothing after waiting for a bit.
                        }
                    }

                    var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                    var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                    var taxRateValue = specificTaxRate.rate1 * 100M;

                    var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);
                    var billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();
                    if (billingTaxRate is null)
                    {
                        billingTaxRate = new TaxRateDatum
                        {
                            name = taxRateName,
                            rate = taxRateValue
                        };

                        var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        return View("TaxRates", new TaxRateResult
                        {
                            Address = location.Address ?? string.Empty,
                            City = location.City ?? string.Empty,
                            Zip = location.Zip ?? string.Empty,
                            Rates = result,
                            Message = $"{taxRateName} has been created."
                        });
                    }
                    else
                    {
                        var unchanged = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        return View("TaxRates", new TaxRateResult
                        {
                            Address = location.Address ?? string.Empty,
                            City = location.City ?? string.Empty,
                            Zip = location.Zip ?? string.Empty,
                            Rates = unchanged,
                            Message = $"{taxRateName} already exists."
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {location.Address}, {location.City}, {location.Zip}.");
                    Log.Fatal(ex.Message);
                    Log.Fatal(ex.StackTrace);
                    Log.Fatal(ex.InnerException.Message);
                    Log.Fatal(ex.InnerException.StackTrace);

                    var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                    return View("TaxRates", new TaxRateResult
                    {
                        Address = location.Address ?? string.Empty,
                        City = location.City ?? string.Empty,
                        Zip = location.Zip ?? string.Empty,
                        Rates = result,
                        Message = $"Failed to create a Tax Rate for {location.Address}, {location.City}, {location.Zip}."
                    });
                }
            }
        }

        [Authorize]
        [Route("/Home/PortRequests/{orderId}/{dialedNumber}")]
        public async Task<IActionResult> RemovePortedPhoneNumber(Guid? orderId, string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var numberToRemove = numbers.Where(x => x.PortedDialedNumber == dialedNumber).FirstOrDefault();

                if (numberToRemove is not null)
                {
                    var checkDelete = await numberToRemove.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                    }

                    var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                    var productOrder = productOrders.Where(x => x.PortedPhoneNumberId == numberToRemove.PortedPhoneNumberId).FirstOrDefault();

                    if (productOrder is not null)
                    {
                        var checkDeleteProductOrder = await productOrder.DeleteByIdAsync(_postgresql).ConfigureAwait(false);
                    }
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                return Redirect("/Home/PortRequests");
            }
        }

        [Authorize]
        [HttpPost("/Home/PortRequests/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestUpdate(PortRequestResult result, Guid? orderId, string dialedNumber)
        {
            var portRequest = result?.PortRequest ?? null;

            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);

                if (checkParse)
                {
                    var port = await VerifyPortablityAsync(phoneNumber.DialedNumber);

                    if (port.Portable)
                    {
                        Log.Information($"[Portability] {port.PortedDialedNumber} is Portable.");

                        port.OrderId = order.OrderId;
                        port.PortRequestId = portRequest.PortRequestId;

                        var checkSave = await port.PostAsync(_postgresql).ConfigureAwait(false);

                        if (checkSave)
                        {
                            numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                        }

                        var productOrder = new ProductOrder
                        {
                            PortedDialedNumber = port.PortedDialedNumber,
                            PortedPhoneNumberId = port.PortedPhoneNumberId,
                            Quantity = 1,
                            CreateDate = DateTime.Now,
                            OrderId = order.OrderId,
                            ProductOrderId = Guid.NewGuid()
                        };

                        var checkProductOrder = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                        if (checkProductOrder)
                        {
                            return View("PortRequestEdit", new PortRequestResult
                            {
                                Order = order,
                                PortRequest = portRequest,
                                PhoneNumbers = numbers,
                                Message = $"Successfully added Ported Phone Number {port.PortedDialedNumber}.",
                                AlertType = "alert-success"
                            });
                        }
                        else
                        {
                            return View("PortRequestEdit", new PortRequestResult
                            {
                                Order = order,
                                PortRequest = portRequest,
                                PhoneNumbers = numbers,
                                Message = $"Failed to add Ported Phone Number {port.PortedDialedNumber}."
                            });
                        }
                    }
                    else
                    {
                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = $"Failed to add Ported Phone Number {port?.PortedDialedNumber}."
                        });
                    }
                }
                else
                {
                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = $"Failed to parse {dialedNumber} as a Phone Number."
                    });
                }

            }
            else if (portRequest is null)
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                var fromDb = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                portRequest.PortRequestId = fromDb.PortRequestId;

                // If the address has changed update it.
                if (portRequest.Address != fromDb.Address)
                {
                    // Format the address information
                    Log.Information($"[Checkout] Parsing address data from {portRequest.Address}");
                    var addressParts = portRequest.Address.Split(", ");
                    if (addressParts.Length > 4)
                    {
                        portRequest.Address = addressParts[0];
                        portRequest.City = addressParts[1];
                        portRequest.State = addressParts[2];
                        portRequest.Zip = addressParts[3];
                        Log.Information($"[Checkout] Address: {portRequest.Address} City: {portRequest.City} State: {portRequest.State} Zip: {portRequest.Zip}");
                    }
                    else
                    {
                        Log.Error($"[Checkout] Failed automatic address formating.");

                        portRequest = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                        var numbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = "Failed to update this Port Request. 😠 The address could not be parsed, please file a bug on Github.",
                            AlertType = "alert-danger"
                        });
                    }
                }

                var checkUpdate = await portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                if (checkUpdate)
                {
                    portRequest = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                    var numbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "Successfully updated this Port Request! 🥳",
                        AlertType = "alert-success"
                    });
                }
                else
                {
                    portRequest = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                    var numbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "Failed to update this Port Request. 😠",
                        AlertType = "alert-danger"
                    });
                }
            }
        }

        /// <summary>
        /// This porting method combines both the TeliMessage and BulkVS porting services.
        /// Tollfree numbers are handled by the TeliMessage port request.
        /// Local numbers are handled by BulkVS.
        /// Local numbers are broken up into separate port requests based on 
        /// the underlying carrier so that BulkVS will accept the port request.
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("/Home/PortRequestUnified/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnifiedPortRequestAsync(string OrderId)
        {
            var responseMessages = new List<string>();

            if (string.IsNullOrWhiteSpace(OrderId))
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(Guid.Parse(OrderId), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                numbers = numbers.Where(x => string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToList();

                // Prevent duplicate submissions.
                if (numbers is null || !numbers.Any())
                {
                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "All of the Numbers in the Port Request have already been submitted to a vendor."
                    });
                }

                // Handle Tollfree numbers
                var tollfreeLookup = new Dictionary<int, int>();
                foreach (var code in PhoneNumbersNA.AreaCode.TollFree)
                {
                    tollfreeLookup.Add(code, code);
                }

                var tollfreeNumbers = new List<PortedPhoneNumber>();
                var localNumbers = new List<PortedPhoneNumber>();

                // Split the tollfree numbers out from the local numbers.
                foreach (var number in numbers)
                {
                    if (tollfreeLookup.TryGetValue(number.NPA, out var _))
                    {
                        tollfreeNumbers.Add(number);
                    }
                    else
                    {
                        localNumbers.Add(number);
                    }
                }

                // Submit the tollfree numbers to TeliMessage in a port request.
                if (tollfreeNumbers.Any())
                {
                    try
                    {
                        var teliResponse = await LnpCreate.GetAsync(portRequest, numbers, _teleToken).ConfigureAwait(false);
                        if (teliResponse is not null && !string.IsNullOrWhiteSpace(teliResponse.data.id))
                        {
                            portRequest.TeliId = teliResponse.data.id;
                            portRequest.DateSubmitted = DateTime.Now;
                            portRequest.VendorSubmittedTo = "TeliMessage";
                            var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                            foreach (var number in numbers)
                            {
                                number.ExternalPortRequestId = teliResponse.data.id;
                                number.RawResponse = JsonSerializer.Serialize(teliResponse);
                                var checkUpdateId = await number.PutAsync(_postgresql).ConfigureAwait(false);
                            }

                            numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal($"[PortRequest] Failed to submit port request to Teli.");
                        Log.Error(ex.Message);
                        Log.Error(ex.StackTrace.ToString());

                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = "Failed to submit port request to Teli: " + ex.Message + " " + ex.StackTrace
                        });
                    }
                }

                // Submit the BulkVS numbers to TeliMessage in a port request.
                if (localNumbers.Any())
                {
                    try
                    {
                        // Extract the street number from the address.
                        // https://stackoverflow.com/questions/26122519/how-to-extract-address-components-from-a-string
                        Match match = Regex.Match(portRequest.Address.Trim(), @"([^\d]*)(\d*)(.*)");
                        string streetNumber = match.Groups[2].Value;

                        var lookups = new List<LrnBulkCnam>();
                        foreach (var item in localNumbers)
                        {
                            var spidCheck = await LrnBulkCnam.GetAsync(item.PortedDialedNumber, _bulkVSAPIKey).ConfigureAwait(false);
                            lookups.Add(spidCheck);
                        }

                        var checkSameSpid = lookups.Select(x => x.spid).Distinct().ToList();

                        // If there's more than one SPID for these numbers then we need to break up the list into multiple separate port requests for BulkVS.
                        if (checkSameSpid.Count > 1)
                        {
                            var portRequests = new List<PortTnRequest>();

                            foreach (var spid in checkSameSpid)
                            {
                                var localTNs = lookups.Where(x => x.spid == spid).Select(x => x.tn).ToArray();

                                var bulkVSPortRequest = new PortTnRequest
                                {
                                    ReferenceId = string.Empty,
                                    TNList = localTNs,
                                    BTN = portRequest.BillingPhone,
                                    SubscriberType = portRequest.LocationType,
                                    AccountNumber = portRequest.ProviderAccountNumber,
                                    Pin = portRequest.ProviderPIN,
                                    Name = string.IsNullOrWhiteSpace(portRequest.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                                    Contact = string.IsNullOrWhiteSpace(portRequest.BusinessContact) ? $"{portRequest.ResidentialFirstName} {portRequest.ResidentialLastName}" : portRequest.BusinessContact,
                                    StreetNumber = streetNumber,
                                    StreetName = $"{portRequest.Address.Substring(streetNumber.Length).Trim()} {portRequest.Address2}",
                                    City = portRequest.City,
                                    State = "WA",
                                    Zip = portRequest.Zip,
                                    RDD = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                                    Time = "20:00:00",
                                    PortoutPin = portRequest.ProviderPIN,
                                    TrunkGroup = "SFO",
                                    Lidb = portRequest.CallerId,
                                    Sms = false,
                                    Mms = false,
                                    SignLoa = false,
                                    Notify = _emailOrders
                                };

                                try
                                {
                                    var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                    if (bulkResponse is not null && !string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                                    {
                                        // Rename this to VendorOrderId, rather than TeliId.
                                        portRequest.TeliId = bulkResponse?.OrderId;
                                        portRequest.DateSubmitted = DateTime.Now;
                                        var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                                        foreach (var number in localTNs)
                                        {
                                            var updatedNumber = numbers.Where(x => $"1{x.PortedDialedNumber}" == number).FirstOrDefault();
                                            updatedNumber.ExternalPortRequestId = bulkResponse?.OrderId;
                                            updatedNumber.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                            var checkUpdateId = await updatedNumber.PutAsync(_postgresql).ConfigureAwait(false);
                                        }

                                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                        // Add a note to handle senarios where the requested FOC is to soon.
                                        var note = new PortTNNote
                                        {
                                            Note = "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day."
                                        };

                                        await note.PostAsync(portRequest.TeliId, _bulkVSusername, _bulkVSpassword);

                                        responseMessages.Add($"{bulkResponse.Description} - {bulkResponse.Code}");
                                    }
                                    else
                                    {
                                        Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                        return View("PortRequestEdit", new PortRequestResult
                                        {
                                            Order = order,
                                            Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
                                            PortRequest = portRequest,
                                            PhoneNumbers = numbers
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                                    Log.Error(ex.Message);
                                    Log.Error(ex.StackTrace.ToString());
                                }
                            }
                        }
                        else
                        {
                            // When there's just a single SPID for this port request.
                            var TNs = lookups.Select(x => x.tn).ToArray();

                            var bulkVSPortRequest = new PortTnRequest
                            {
                                ReferenceId = string.Empty,
                                TNList = TNs,
                                BTN = portRequest.BillingPhone,
                                SubscriberType = portRequest.LocationType,
                                AccountNumber = portRequest.ProviderAccountNumber,
                                Pin = portRequest.ProviderPIN,
                                Name = string.IsNullOrWhiteSpace(portRequest.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                                Contact = string.IsNullOrWhiteSpace(portRequest.BusinessContact) ? $"{portRequest.ResidentialFirstName} {portRequest.ResidentialLastName}" : portRequest.BusinessContact,
                                StreetNumber = streetNumber,
                                StreetName = $"{portRequest.Address.Substring(streetNumber.Length).Trim()} {portRequest?.Address2}",
                                City = portRequest.City,
                                State = "WA",
                                Zip = portRequest.Zip,
                                RDD = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                                Time = "20:00:00",
                                PortoutPin = portRequest.ProviderPIN,
                                TrunkGroup = "SFO",
                                Lidb = portRequest.CallerId,
                                Sms = false,
                                Mms = false,
                                SignLoa = false,
                                Notify = _emailOrders
                            };

                            var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);
                            Log.Information(JsonSerializer.Serialize(bulkResponse));

                            if (bulkResponse is not null && !string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                            {
                                // Rename this to VendorOrderId, rather than TeliId.
                                portRequest.TeliId = bulkResponse?.OrderId;
                                portRequest.DateSubmitted = DateTime.Now;
                                portRequest.VendorSubmittedTo = "BulkVS";
                                var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                                foreach (var number in numbers)
                                {
                                    number.ExternalPortRequestId = bulkResponse?.OrderId;
                                    number.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                    var checkUpdateId = await number.PutAsync(_postgresql).ConfigureAwait(false);
                                }

                                numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                // Add a note to handle senarios where the requested FOC is to soon.
                                var note = new PortTNNote
                                {
                                    Note = "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day."
                                };

                                await note.PostAsync(portRequest.TeliId, _bulkVSusername, _bulkVSpassword);

                                responseMessages.Add($"{bulkResponse.Description} - {bulkResponse.Code}");
                            }
                            else
                            {
                                Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                return View("PortRequestEdit", new PortRequestResult
                                {
                                    Order = order,
                                    Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
                                    PortRequest = portRequest,
                                    PhoneNumbers = numbers
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                        Log.Error(ex.Message);
                        Log.Error(ex.StackTrace.ToString());

                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = "Failed to submit port request to BulkVS: " + ex.Message + " " + ex.StackTrace
                        });
                    }
                }

                // Trigger the background processes to bring the ported numbers into Teli as offnet numbers for texting and E911 service.
                order.BackgroundWorkCompleted = false;
                var checkOrder = await order.PutAsync(_postgresql);

                if (checkOrder)
                {
                    Log.Information($"[Port Request] Updated Order {order.OrderId} to kick off the background work.");
                }
                else
                {
                    Log.Fatal($"[Port Request] Failed to update Order {order.OrderId} to kick off the background work.");
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers,
                    Message = string.Join(", ", responseMessages.ToArray())
                });
            }
        }


        [Authorize]
        public async Task<IActionResult> Tests(string testName, string npa, string nxx, string dialedNumber)
        {
            if (testName == "DIDInventorySearchAsync" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx) || !string.IsNullOrWhiteSpace(dialedNumber)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                dialedNumber ??= string.Empty;

                var results = await NpaNxxFirstPointCom.GetAsync(npa, nxx, dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    NPA = npa,
                    NXX = nxx,
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = results
                });
            }

            if (testName == "DIDOrderAsync" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var results = await FirstPointComOrderPhoneNumber.PostAsync(dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumberOrder = results
                });
            }

            if (testName == "LRNLookup" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LrnLookup.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    LRNLookup = checkNumber
                });
            }

            if (testName == "didslist" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await DidsList.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = checkNumber
                });
            }

            if (testName == "lnpcheck" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LnpCheck.GetRawAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PortabilityResponse = checkNumber.data.Values.FirstOrDefault().status
                });
            }

            if (testName == "DnSearchNpaNxx" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                var checkNPA = int.TryParse(npa, out var NPA);
                var checkNXX = int.TryParse(nxx, out var NXX);

                var checkNumber = await OrderTn.GetAsync(NPA, NXX, _bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PhoneNumbersBVS = checkNumber
                });
            }

            return View("Tests");
        }

        [Authorize]
        [Route("/Home/Emails")]
        [Route("/Home/Emails/{orderId}")]
        public async Task<IActionResult> Emails(Guid? orderId)
        {
            if (orderId != null && orderId.HasValue)
            {
                var emails = await Email.GetByOrderAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
            else
            {
                var emails = await Email.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
        }

        [Authorize]
        [HttpGet("/Home/Emails/{orderId}/Resend/{emailId}")]
        public async Task<IActionResult> ResendEmails(Guid orderId, Guid emailId)
        {
            var email = await Email.GetAsync(emailId, _postgresql).ConfigureAwait(false);

            email.DoNotSend = false;
            email.Completed = false;
            var tryUnblock = await email.PutAsync(_postgresql);

            if (tryUnblock)
            {
                var order = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);
                if (order is not null)
                {
                    order.BackgroundWorkCompleted = false;
                    var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);
                }
                else
                {
                    var checkSending = await email.SendEmailAsync(_emailUsername, _emailPassword).ConfigureAwait(false);

                    if (checkSending)
                    {
                        email.Completed = true;
                        tryUnblock = await email.PutAsync(_postgresql);
                    }
                }
            }

            var emails = await Email.GetAllAsync(_postgresql).ConfigureAwait(false);

            return View("Emails", emails);
        }

        [Authorize]
        public async Task<IActionResult> Ingests(int cycle, string ingestedFrom, string enabled, string runNow)
        {
            var ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);

            if (cycle > 0 && cycle < 24 && !string.IsNullOrWhiteSpace(ingestedFrom) && (enabled == "Enabled" || enabled == "Disabled"))
            {
                var update = ingests.Where(x => x.IngestedFrom == ingestedFrom).FirstOrDefault();

                if (update != null)
                {
                    update.CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now;
                    update.Enabled = enabled == "Enabled";
                    update.RunNow = runNow == "true";
                    update.LastUpdate = DateTime.Now;

                    var checkUpdate = await update.PutAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
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

                    var checkCreate = await update.PostAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
            }

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
            query = query?.Trim();

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

            var results = await PhoneNumber.SequentialPaginatedSearchAsync(new string(converted.ToArray()), page, _postgresql).ConfigureAwait(false);
            var count = await PhoneNumber.NumberOfResultsInQuery(new string(converted.ToArray()), _postgresql).ConfigureAwait(false);

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
}
