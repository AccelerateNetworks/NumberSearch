using BulkVS;
using BulkVS.BulkVS;

using FirstCom;

using MailKit.Security;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;
using MimeKit.Text;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleMesssage;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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

        public CartController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            var checkCallFlow = int.TryParse(_configuration.GetConnectionString("CallFlow"), out _CallFlow);
            var checkChannelGroup = int.TryParse(_configuration.GetConnectionString("ChannelGroup"), out _ChannelGroup);
            _apiKey = config.GetConnectionString("BulkVSAPIKEY");
            _apiSecret = config.GetConnectionString("BulkVSAPISecret");
            _fpcusername = config.GetConnectionString("PComNetUsername");
            _fpcpassword = config.GetConnectionString("PComNetPassword");
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
        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber, string Query)
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
                }
            }
            else
            {
                // Sadly its gone. And the user needs to pick a different number.
                return RedirectToAction("Index", "Search", new { Query, Failed = phoneNumber.DialedNumber });
            }

            if (!purchasable)
            {
                // Sadly its gone. And the user needs to pick a different number.
                return RedirectToAction("Index", "Search", new { Query, Failed = phoneNumber.DialedNumber });
            }

            var checkAdd = cart.AddPhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                return RedirectToAction("Index", "Search", new { Query });
            }
            else
            {
                // TODO: Tell the user about the failure
                return RedirectToAction("Index", "Search", new { Query, Failed = phoneNumber.DialedNumber });
            }
        }

        [Route("Cart/PortPhoneNumber/{dialedPhoneNumber}")]
        public async Task<IActionResult> PortPhoneNumberAsync(string dialedPhoneNumber)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var portedPhoneNumber = await PortedPhoneNumber.GetAsync(dialedPhoneNumber, _postgresql).ConfigureAwait(false);

            if (portedPhoneNumber is null || string.IsNullOrWhiteSpace(portedPhoneNumber.PortedDialedNumber))
            {

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

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
                        IngestedFrom = "UserInput"
                    };

                    var checkSubmission = await port.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmission)
                    {
                        portedPhoneNumber = await PortedPhoneNumber.GetAsync(port.PortedDialedNumber, _postgresql).ConfigureAwait(false);
                    }
                }
            }

            var productOrder = new ProductOrder { PortedDialedNumber = portedPhoneNumber.PortedDialedNumber, Quantity = 1 };

            var checkAdd = cart.AddPortedPhoneNumber(portedPhoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                // TODO: Mark the item as sucessfully added.
                return RedirectToAction("Index", "Porting", new PortingResults
                {
                    PortedPhoneNumber = portedPhoneNumber,
                    Cart = cart
                });
            }
            else
            {
                // TODO: Tell the user about the failure
                return RedirectToAction("Index", "Porting", new PortingResults
                {
                    PortedPhoneNumber = portedPhoneNumber,
                    Cart = cart
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
                return RedirectToAction("Index", "Hardware");
            }
            else
            {
                // TODO: Tell the user about the failure
                return RedirectToAction("Index", "Hardware");
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
        public IActionResult RemovePhoneNumber(string dialedPhoneNumber)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var phoneNumber = new PhoneNumber { DialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { DialedNumber = dialedPhoneNumber };

            var checkRemove = cart.RemovePhoneNumber(phoneNumber, productOrder);
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

            return View("Order", cart);
        }

        // Show orders that have already been submitted.
        [Route("Cart/Order/{Id}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id, bool? AddPortingInfo)
        {
            if (Id != null)
            {
                var order = await Order.GetByIdAsync(Id, _postgresql).ConfigureAwait(false);
                if (order == null || order.OrderId == null || string.IsNullOrWhiteSpace(order.Email))
                {
                    return View("Index", new CartResult
                    {
                        Cart = null,
                        Message = "Couldn't find this order in our system."
                    });
                }

                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);

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
                        var phoneNumber = await PurchasedPhoneNumber.GetByDialedNumberAsync(item.DialedNumber, order.OrderId, _postgresql).ConfigureAwait(false);

                        bool checkNpa = int.TryParse(phoneNumber.DialedNumber.Substring(0, 3), out int npa);
                        bool checkNxx = int.TryParse(phoneNumber.DialedNumber.Substring(3, 3), out int nxx);
                        bool checkXxxx = int.TryParse(phoneNumber.DialedNumber.Substring(6), out int xxxx);

                        if (checkNxx && checkXxxx)
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
                        var portedPhoneNumber = await PortedPhoneNumber.GetAsync(item.PortedDialedNumber, _postgresql).ConfigureAwait(false);
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
                    PortedPhoneNumbers = portedPhoneNumbers
                };

                if (AddPortingInfo != null)
                {
                    var checkSet = cart.SetToSession(HttpContext.Session);

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "<Pending>")]
        public async Task<IActionResult> SubmitAsync(Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                var cart = Cart.GetFromSession(HttpContext.Session);

                if (cart.Order.OrderId == Guid.Empty)
                {
                    // Save to db.
                    var submittedOrder = await order.PostAsync(_postgresql).ConfigureAwait(false);

                    // Send a confirmation email.
                    if (submittedOrder)
                    {
                        var orderFromDb = await Order.GetByEmailAsync(order.Email, _postgresql).ConfigureAwait(false);
                        order = orderFromDb.FirstOrDefault();


                        // Submit the number orders.
                        var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                        var numbersToOrder = new List<string>();
                        foreach (var po in productOrders)
                        {
                            if (po.DialedNumber != null)
                            {
                                numbersToOrder.Add(po.DialedNumber);
                            }
                        }

                        foreach (var nto in cart.PhoneNumbers)
                        {
                            if (nto.IngestedFrom == "BulkVS")
                            {
                                // Buy it and save the reciept.
                                var random = new Random();
                                var pin = random.Next(100000, 99999999);
                                var executeOrder = await BulkVSOrderPhoneNumber.GetAsync(nto.DialedNumber, "SFO", "Enabled", string.Empty, "false", pin.ToString(new CultureInfo("en-US")), _apiKey, _apiSecret).ConfigureAwait(false);

                                var verifyOrder = new PurchasedPhoneNumber
                                {
                                    OrderId = order.OrderId,
                                    DateOrdered = order.DateSubmitted,
                                    DialedNumber = nto.DialedNumber,
                                    DateIngested = nto.DateIngested,
                                    IngestedFrom = nto.IngestedFrom,
                                    // Keep the raw response as a receipt.
                                    OrderResponse = string.IsNullOrWhiteSpace(executeOrder?.result?.description) ? $"faultstring: {executeOrder?.fault?.faultstring}" : $"description: {executeOrder?.result?.description}, cnamlookup: {executeOrder?.result?.entry?.cnamlookup}, dn: {executeOrder?.result?.entry?.dn}, lidb: {executeOrder?.result?.entry?.lidb}, portoutpin: {executeOrder?.result?.entry?.portoutpin}, trunkgroup: {executeOrder?.result?.entry?.trunkgroup}",
                                    // If the status code of the order comes back as 200 then it was sucessful.
                                    Completed = executeOrder.result.entry.dn.Contains(nto.DialedNumber, StringComparison.InvariantCultureIgnoreCase)
                                };

                                var checkVerifyOrder = await verifyOrder.PostAsync(_postgresql).ConfigureAwait(false);
                            }
                            else if (nto.IngestedFrom == "TeleMessage")
                            {
                                // Buy it and save the reciept.
                                var executeOrder = await DidsOrder.GetAsync(nto.DialedNumber, _CallFlow, _ChannelGroup, _teleToken).ConfigureAwait(false);

                                var verifyOrder = new PurchasedPhoneNumber
                                {
                                    OrderId = order.OrderId,
                                    DateOrdered = order.DateSubmitted,
                                    DialedNumber = nto.DialedNumber,
                                    DateIngested = nto.DateIngested,
                                    IngestedFrom = nto.IngestedFrom,
                                    // Keep the raw response as a receipt.
                                    OrderResponse = JsonSerializer.Serialize(executeOrder),
                                    // If the status code of the order comes back as 200 then it was sucessful.
                                    Completed = executeOrder.code == 200
                                };

                                var checkVerifyOrder = await verifyOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                // Set a note for these number purchases inside of Tele's system.
                                var getTeleId = await UserDidsGet.GetAsync(nto.DialedNumber, _teleToken).ConfigureAwait(false);
                                var setTeleLabel = await UserDidsNote.SetNote($"{order?.BusinessName} {order?.FirstName} {order?.LastName}", getTeleId.data.id, _teleToken).ConfigureAwait(false);
                            }
                            else if (nto.IngestedFrom == "FirstPointCom")
                            {
                                // Buy it and save the reciept.
                                var executeOrder = await FirstPointComOrderPhoneNumber.PostAsync(nto.DialedNumber, _fpcusername, _fpcpassword).ConfigureAwait(false);

                                var verifyOrder = new PurchasedPhoneNumber
                                {
                                    OrderId = order.OrderId,
                                    DateOrdered = order.DateSubmitted,
                                    DialedNumber = nto.DialedNumber,
                                    DateIngested = nto.DateIngested,
                                    IngestedFrom = nto.IngestedFrom,
                                    // Keep the raw response as a receipt.
                                    OrderResponse = JsonSerializer.Serialize(executeOrder),
                                    // If the status code of the order comes back as 200 then it was sucessful.
                                    Completed = executeOrder.code == 0
                                };

                                var checkVerifyOrder = await verifyOrder.PostAsync(_postgresql).ConfigureAwait(false);
                            }
                            else
                            {
                                // Sadly its gone. And the user needs to pick a different number.
                                return View("Index", new CartResult
                                {
                                    Cart = cart,
                                    Message = $"Please remove {nto.DialedNumber} from your cart and try again. This number is not purchasable at this time."
                                });
                            }
                        }

                        foreach (var productOrder in cart.ProductOrders)
                        {
                            productOrder.OrderId = order.OrderId;
                            var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                            if (!checkSubmitted)
                            {
                                // TODO: Maybe failout here?
                            }
                        }

                        var outboundMessage = new MimeKit.MimeMessage
                        {
                            Sender = new MimeKit.MailboxAddress("Number Search", _configuration.GetConnectionString("SmtpUsername")),
                            Subject = $"Order: {order.OrderId}"
                        };

                        // This will need to be updated when the baseURL changes.
                        var linkToOrder = $"https://acceleratenetworks.com/Cart/Order/{order.OrderId}";

                        outboundMessage.Body = new TextPart(TextFormat.Plain)
                        {
                            Text = $@"Hi {order.FirstName},
                                                                                      
Thank you for choosing Accelerate Networks!

Your order Id is: {order.OrderId} and it was submitted on {order.DateSubmitted.ToLocalTime().ToShortDateString()} at {order.DateSubmitted.ToLocalTime().ToShortTimeString()}.

You can review your order at {linkToOrder}
                                                                                      
A delivery specialist will send you a follow up email to walk you through the next steps in the process.

Thanks,

Accelerate Networks"
                        };



                        var ordersInbox = MailboxAddress.Parse(_configuration.GetConnectionString("SmtpUsername"));
                        var recipient = MailboxAddress.Parse(order.Email);
                        outboundMessage.Cc.Add(ordersInbox);
                        outboundMessage.To.Add(recipient);

                        using var smtp = new MailKit.Net.Smtp.SmtpClient();
                        smtp.MessageSent += (sender, args) => { };
                        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
                        await smtp.AuthenticateAsync(_configuration.GetConnectionString("SmtpUsername"), _configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                        await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
                        await smtp.DisconnectAsync(true).ConfigureAwait(false);

                        if (cart.PortedPhoneNumbers.Any())
                        {
                            cart.Order = order;
                            var checkSet = cart.SetToSession(HttpContext.Session);

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