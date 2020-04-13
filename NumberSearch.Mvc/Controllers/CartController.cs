using MailKit.Security;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;
using MimeKit.Text;

using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration configuration;

        public CartController(IConfiguration config)
        {
            configuration = config;
        }

        public IActionResult Index()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", cart);
        }

        [Route("Cart/BuyPhoneNumber/{dialedPhoneNumber}")]
        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber, string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
            var productOrder = new ProductOrder { DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

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
                return RedirectToAction("Index", "Search", new { Query });
            }
        }

        [Route("Cart/BuyProduct/{productId}")]
        public async Task<IActionResult> BuyProductAsync(Guid productId, int Quantity)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var product = await Product.GetAsync(productId, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
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

        [Route("Cart/Checkout")]
        public IActionResult Checkout()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Order", cart);
        }

        // Show orders that have already been submitted.
        [Route("Cart/Order/{Id}")]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id)
        {
            if (Id != null)
            {
                var orders = await Order.GetAsync(Id, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                var order = orders.FirstOrDefault();
                var productOrders = await ProductOrder.GetAsync(order.OrderId, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var phoneNumbers = new List<PhoneNumber>();
                var products = new List<Product>();
                foreach (var item in productOrders)
                {
                    if (item?.DialedNumber?.Length == 10)
                    {
                        var phoneNumber = await PhoneNumber.GetAsync(item.DialedNumber, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                        phoneNumbers.Add(phoneNumber);
                    }
                    else
                    {
                        var product = await Product.GetAsync(item.ProductId, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                        products.Add(product);
                    }
                }

                var cart = new Cart
                {
                    Order = order,
                    ProductOrders = productOrders,
                    PhoneNumbers = phoneNumbers,
                    Products = products
                };

                return View("Order", cart);
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
        public async Task<IActionResult> SubmitAsync([Bind("FirstName,LastName,Email,Address,Address2,Country,State,Zip")] Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                var cart = Cart.GetFromSession(HttpContext.Session);

                // Save to db.
                var submittedOrder = await order.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Send a confirmation email.
                if (submittedOrder)
                {
                    var orderFromDb = await Order.GetAsync(order.Email, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                    order = orderFromDb.FirstOrDefault();

                    foreach (var productOrder in cart.ProductOrders)
                    {
                        productOrder.OrderId = order.OrderId;
                        var checkSubmitted = await productOrder.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                        if (!checkSubmitted)
                        {
                            // TODO: Maybe failout here?
                        }
                    }

                    var outboundMessage = new MimeKit.MimeMessage
                    {
                        Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                        Subject = $"Order: {order.OrderId}"
                    };

                    outboundMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = $@"Hi {order.FirstName},
                                                                                      
Thank you for choosing Accelerate Networks!

Your order Id is: {order.OrderId} and it was submitted on {order.DateSubmitted.ToLocalTime().ToShortDateString()} at {order.DateSubmitted.ToLocalTime().ToShortTimeString()}.

You can review your order at insertLinkToOrderHere
                                                                                      
A delivery specialist will send you a follow up email to walk you through the next steps in the process.

Thanks,

Accelerate Networks"
                    };

                    outboundMessage.Cc.Add(new MailboxAddress(configuration.GetConnectionString("SmtpUsername")));
                    outboundMessage.To.Add(new MailboxAddress($"{order.Email}"));

                    using var smtp = new MailKit.Net.Smtp.SmtpClient();
                    smtp.MessageSent += (sender, args) => { };
                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
                    await smtp.AuthenticateAsync(configuration.GetConnectionString("SmtpUsername"), configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                    await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
                    await smtp.DisconnectAsync(true).ConfigureAwait(false);
                }

                // Reset the session and clear the Cart.
                HttpContext.Session.Clear();

                return View("Success", order);
            }
            else
            {
                return RedirectToAction("Cart", "Checkout");
            }
        }
    }
}