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
    public class ContactController : Controller
    {
        private readonly IConfiguration configuration;

        public ContactController(IConfiguration config)
        {
            configuration = config;
        }

        public async Task<IActionResult> IndexAsync()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "<Pending>")]
        public async Task<IActionResult> ContactAsync([Bind("FirstName,LastName,Email,PhoneNumber")] ContactForm contact)
        {
            if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
            {
                contact.DateSubmitted = DateTime.Now;

                // Save to db.
                var submittedOrder = await contact.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                // Send a confirmation email.
                if (submittedOrder)
                {
                    var orderFromDb = await ContactForm.GetAsync(contact.Email, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                    contact = orderFromDb.FirstOrDefault();

                    var outboundMessage = new MimeKit.MimeMessage
                    {
                        Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                        Subject = $"Thank you for Contacting Accelerate Networks"
                    };

                    outboundMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = $@"Hi {contact.FirstName},
                                                                                      
Thank you for contacting Accelerate Networks!

Your contact Id is: {contact.Id}.
                                                                                      
A technical specialist will send you a follow up email to walk you through the next steps in the process.

Thanks,

Accelerate Networks"
                    };

                    outboundMessage.Cc.Add(new MailboxAddress(configuration.GetConnectionString("SmtpUsername")));
                    outboundMessage.To.Add(new MailboxAddress($"{contact.Email}"));

                    using var smtp = new MailKit.Net.Smtp.SmtpClient();
                    smtp.MessageSent += (sender, args) => { };
                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
                    await smtp.AuthenticateAsync(configuration.GetConnectionString("SmtpUsername"), configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                    await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
                    await smtp.DisconnectAsync(true).ConfigureAwait(false);
                }

                return View("Success", contact);
            }

            return RedirectToAction("Index", "Search");
        }
    }
}