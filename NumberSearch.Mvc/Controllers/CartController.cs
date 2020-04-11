using MailKit.Security;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;
using MimeKit.Text;

using Newtonsoft.Json;

using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IActionResult> IndexAsync()
        {
            var items = Cookie.Get(HttpContext.Session);

            // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
            // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
            var phoneNumbers = new List<PhoneNumber>();
            var products = new List<Product>();
            foreach (var item in items)
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
                PhoneNumbers = phoneNumbers,
                Products = products
            };

            return View("Index", cart);

        }

        [Route("Cart/BuyPhoneNumber/{dialedPhoneNumber}")]
        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber, string Query)
        {
            var items = Cookie.Get(HttpContext.Session).ToList();

            var notInCart = true;
            foreach (var item in items)
            {
                if (item.DialedNumber == dialedPhoneNumber)
                {
                    notInCart = false;
                }
            }

            if (notInCart)
            {
                var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                items.Add(new ProductOrder { DialedNumber = phoneNumber.DialedNumber, Quantity = 1 });
            }

            var checkCookie = Cookie.Set(HttpContext.Session, items);

            return RedirectToAction("Index", "Search", new { Query });
        }

        [Route("Cart/RemovePhoneNumber/{dialedPhoneNumber}")]
        public IActionResult RemovePhoneNumber(string dialedPhoneNumber)
        {
            var items = Cookie.Get(HttpContext.Session).ToList();

            var itemToRemove = new ProductOrder();

            foreach (var item in items)
            {
                if (item.DialedNumber == dialedPhoneNumber)
                {
                    itemToRemove = item;
                }
            }

            items.Remove(itemToRemove);

            var checkCookie = Cookie.Set(HttpContext.Session, items);

            return RedirectToAction("Index");
        }

        [Route("Cart/BuyProduct/{productId}")]
        public async Task<IActionResult> BuyProductAsync(Guid productId, int quantity)
        {
            var items = Cookie.Get(HttpContext.Session).ToList();

            var notInCart = true;
            foreach (var item in items)
            {
                if (item.ProductId == productId)
                {
                    notInCart = false;
                }
            }

            if (notInCart)
            {
                var product = await Product.GetAsync(productId, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                items.Add(new ProductOrder
                {
                    ProductId = product.ProductId,
                    Quantity = quantity > 0 ? quantity : 1
                });
            }

            var checkCookie = Cookie.Set(HttpContext.Session, items);

            return RedirectToAction("Hardware", "Home");

        }

        [Route("Cart/RemoveProduct/{productId}")]
        public IActionResult RemoveProduct(Guid productId)
        {
            var items = Cookie.Get(HttpContext.Session).ToList();

            var itemToRemove = new ProductOrder();

            foreach (var item in items)
            {
                if (item.ProductId == productId)
                {
                    itemToRemove = item;
                }
            }

            items.Remove(itemToRemove);

            var checkCookie = Cookie.Set(HttpContext.Session, items);

            return RedirectToAction("Hardware", "Home");
        }

        [Route("Cart/Checkout")]
        public async Task<IActionResult> CheckoutAsync()
        {
            var items = Cookie.Get(HttpContext.Session);

            // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
            // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
            var phoneNumbers = new List<PhoneNumber>();
            var products = new List<Product>();
            foreach (var item in items)
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
                PhoneNumbers = phoneNumbers,
                Products = products
            };

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
                var items = await ProductOrder.GetAsync(order.Id, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var phoneNumbers = new List<PhoneNumber>();
                var products = new List<Product>();
                foreach (var item in items)
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
                    PhoneNumbers = phoneNumbers,
                    Products = products
                };

                return View("Existing", cart);
            }
            else
            {
                return View("Existing");
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

                var items = Cookie.Get(HttpContext.Session);

                // Save to db.
                var submittedOrder = await order.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Send a confirmation email.
                if (submittedOrder)
                {
                    var orderFromDb = await Order.GetAsync(order.Email, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                    order = orderFromDb.FirstOrDefault();

                    foreach (var item in items)
                    {
                        var productOrdered = new ProductOrder
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId != null ? item.ProductId : Guid.Empty,
                            DialedNumber = item?.DialedNumber?.Length > 0 ? item?.DialedNumber : string.Empty,
                            Quantity = item.Quantity > 0 ? item.Quantity : 1
                        };

                        var checkSubmitted = await productOrdered.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                        if (!checkSubmitted)
                        {
                            // TODO: Maybe failout here?
                        }
                    }

                    var outboundMessage = new MimeKit.MimeMessage
                    {
                        Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                        Subject = $"Order: {order.Id}"
                    };

                    outboundMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = $@"Hi {order.FirstName},
                                                                                      
Thank you for choosing Accelerate Networks!

Your order Id is: {order.Id} and it was submitted on {order.DateSubmitted.ToLocalTime().ToShortDateString()} at {order.DateSubmitted.ToLocalTime().ToShortTimeString()}.

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

                // Reset the cookie.
                var checkCookie = Cookie.Set(HttpContext.Session, new List<ProductOrder>());

                return View("Success", order);
            }
            else
            {
                return RedirectToAction("Cart", "Checkout");
            }
        }
    }
}