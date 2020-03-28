using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.Controllers
{
    public class OnboardingController : Controller
    {
        private readonly IConfiguration configuration;

        public OnboardingController(IConfiguration config)
        {
            configuration = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "<Pending>")]
        public async Task<IActionResult> OnboardingAsync([Bind("BusinessName,FirstName,LastName,Email,Address,Address2,Country,State,Zip,ExpensivePhoneName,EPCount,CheapPhoneName,CPCount,LinesOrSeats,Lines,Seats,ExtraPhoneNumbers,FaxServer")] ClientOnboarding order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.BusinessName) && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                // Save to db.
                var submittedOrder = await order.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Send a confirmation email.
                if (submittedOrder)
                {
                    var orderFromDb = await ClientOnboarding.GetAsync(order.BusinessName, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                    order = orderFromDb.FirstOrDefault();

                    var outboundMessage = new MimeKit.MimeMessage
                    {
                        Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                        Subject = $"Order: {order.BusinessName}"
                    };

                    outboundMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = $@"Hi {order.FirstName},
                                                                                      
We happy to have {order.BusinessName} join our network!

Your Onboarding Id is: {order.Id}.
                                                                                      
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