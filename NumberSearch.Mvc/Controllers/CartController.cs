using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using Newtonsoft.Json;
using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

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
            var session = HttpContext.Session;
            if (session.TryGetValue("cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);
                var items = JsonConvert.DeserializeObject<List<PhoneNumber>>(entries);

                var cart = new Cart
                {
                    Items = items
                };

                return View("Index", cart);
            }
            else
            {
                return View();
            }
        }

        [Route("Cart/Buy/{dialedPhoneNumber}")]
        public async Task<IActionResult> BuyAsync(string dialedPhoneNumber, string Query)
        {
            var session = HttpContext.Session;
            if (session.TryGetValue("cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);
                var items = JsonConvert.DeserializeObject<List<PhoneNumber>>(entries);

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
                    items.Add(phoneNumber);
                }

                var cart = JsonConvert.SerializeObject(items);

                session.Set("cart", Encoding.ASCII.GetBytes(cart));

                return RedirectToAction("Index", "Search", new { Query });
            }
            else
            {
                var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                var cart = JsonConvert.SerializeObject(new List<PhoneNumber> { phoneNumber });

                session.Set("cart", Encoding.ASCII.GetBytes(cart));

                return RedirectToAction("Index", "Search", new { Query });
            }
        }

        [Route("Cart/Remove/{dialedPhoneNumber}")]
        public IActionResult Remove(string dialedPhoneNumber)
        {
            var session = HttpContext.Session;
            if (session.TryGetValue("cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);
                var items = JsonConvert.DeserializeObject<List<PhoneNumber>>(entries);
                var itemToRemove = new PhoneNumber();

                foreach (var item in items)
                {
                    if (item.DialedNumber == dialedPhoneNumber)
                    {
                        itemToRemove = item;
                    }
                }

                items.Remove(itemToRemove);

                var cart = JsonConvert.SerializeObject(items);

                session.Set("cart", Encoding.ASCII.GetBytes(cart));
            }

            return RedirectToAction("Index");
        }

        [Route("Cart/Checkout")]
        public IActionResult Checkout()
        {
            var session = HttpContext.Session;

            if (session.TryGetValue("cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);
                var items = JsonConvert.DeserializeObject<List<PhoneNumber>>(entries);

                return View("Order", new Cart { Items = items });
            }
            else
            {
                return RedirectToAction("Cart", "Index");
            }
        }

        // Show orders that have already been submitted.
        [Route("Cart/Order/{Id}")]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id)
        {
            if (Id != null)
            {
                var orders = await Order.GetAsync(Id, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                var order = orders.FirstOrDefault();
                var products = await ProductOrder.GetAsync(order.Id, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                var output = new ExistingOrder
                {
                    Order = order,
                    Items = products
                };

                return View("Existing", output);
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

                var session = HttpContext.Session;

                if (session.TryGetValue("cart", out var cookie))
                {
                    var entries = Encoding.ASCII.GetString(cookie);
                    var items = JsonConvert.DeserializeObject<List<PhoneNumber>>(entries);

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
                                DialedNumber = item.DialedNumber,
                                Quantity = 1
                            };

                            var checkSubmitted = await productOrdered.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
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

                    return View("Success", order);
                }
                else
                {
                    return RedirectToAction("Cart", "Index");
                }
            }
            else
            {
                return RedirectToAction("Cart", "Checkout");
            }
        }
    }
}