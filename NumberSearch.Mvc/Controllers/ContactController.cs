
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;

using Serilog;

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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "<Pending>")]
        public async Task<IActionResult> ContactAsync([Bind("BusinessName,RoleTitle,FirstName,LastName,Email,PhoneNumber")] ContactForm contact)
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

                    // Send out the confirmation email.
                    var confirmationEmail = new Email
                    {
                        PrimaryEmailAddress = contact.Email,
                        CarbonCopy = configuration.GetConnectionString("SmtpUsername"),
                        MessageBody = $@"Hi {contact.FirstName},
                                                                                      
Thank you for contacting Accelerate Networks!
                                                                                      
A technical specialist will reach out to chat.

Thanks,

Accelerate Networks",
                        OrderId = contact.Id,
                        Subject = $"Thank you for Contacting Accelerate Networks"
                    };

                    var checkSend = await confirmationEmail.SendEmailAsync(configuration.GetConnectionString("SmtpUsername"), configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                    var checkSave = await confirmationEmail.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                    if (checkSend && checkSave)
                    {
                        Log.Information($"Sucessfully sent out the confirmation emails for {contact.Id}.");
                    }
                    else
                    {
                        Log.Fatal($"Failed to sent out the confirmation emails for {contact.Id}.");
                    }
                }

                return View("Success", contact);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}