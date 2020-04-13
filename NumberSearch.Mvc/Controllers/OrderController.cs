using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using NumberSearch.DataAccess;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class OrderController : Controller
    {
        private readonly IConfiguration configuration;

        public OrderController(IConfiguration config)
        {
            configuration = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        public async Task<IActionResult> IndexAsync(string query)
        {
            if (query != null && query.Length == 10)
            {
                foreach (var c in query)
                {
                    var check = int.TryParse(c.ToString(), out int i);
                    if (!check)
                    {
                        // Redirect back to the search page. 
                        return RedirectToAction("Index", "Search");
                    };
                }

                var result = await PhoneNumber.GetAsync(query, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Query the owner of the number to make sure it's still avalible for purchase.
                switch (result.IngestedFrom)
                {
                    case "TeleMessage":
                        break;
                    case "BulkVS":
                        break;
                    case "FirstCom":
                        break;
                }

                //var model = new PhoneNumberOrderInfo
                //{
                //    number = result,
                //    detail = new PhoneNumberDetail { }
                //};

                return View("Index");
            }
            else
            {
                return Redirect("/Search");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "<Pending>")]
        public async Task<IActionResult> OrderAsync([Bind("DialedNumber,FirstName,LastName,Email,Address,Address2,Country,State,Zip")] Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                // Save to db.
                var submittedOrder = await order.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Send a confirmation email.
                if (submittedOrder)
                {
                    var orderFromDb = await Order.GetAsync(order.Email, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                    order = orderFromDb.FirstOrDefault();

                    var outboundMessage = new MimeKit.MimeMessage
                    {
                        Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                        Subject = $"Order: {order.OrderId}"
                    };

                    outboundMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = $@"Hi {order.FirstName},
                                                                                      
Thank you for ordering from Accelerate Networks!

Your order Id is: {order.OrderId}.
                                                                                      
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

            return RedirectToAction("Index", "Search");
        }
    }
}