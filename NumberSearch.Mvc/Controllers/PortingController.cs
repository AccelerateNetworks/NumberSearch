using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class PortingController : Controller
    {
        private readonly IConfiguration configuration;

        public PortingController(IConfiguration config)
        {
            configuration = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CheckPortabilityAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (Query != null && Query?.Length == 10)
            {
                var dialedPhoneNumber = Query;

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    var teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));

                    try
                    {
                        var portable = await LnpCheck.IsPortable(dialedPhoneNumber, teleToken).ConfigureAwait(false);

                        if (portable)
                        {
                            Log.Information($"[Portability] {dialedPhoneNumber} is Portable.");

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

                            return View("Index", new PortingResults
                            {
                                PortedPhoneNumber = port,
                                Cart = cart,
                                Message = "This phone number can be ported to our network!"
                            });
                        }
                        else
                        {
                            Log.Information($"[Portability] {dialedPhoneNumber} is not Portable.");

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

                            return View("Index", new PortingResults
                            {
                                PortedPhoneNumber = port,
                                Cart = cart,
                                Message = "This phone number can likely be ported to our network!"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal($"[Portability] {ex.Message}");

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

                        return View("Index", new PortingResults
                        {
                            PortedPhoneNumber = port,
                            Cart = cart,
                            Message = "This phone number can likely be ported to our network!"
                        });
                    }
                }
                else
                {
                    return View("Index", new PortingResults
                    {
                        PortedPhoneNumber = new PortedPhoneNumber { },
                        Cart = cart
                    });
                }
            }
            else
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Cart = cart
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestPortAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (Query != null && Query?.Length == 10)
            {
                var dialedPhoneNumber = Query;

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    var teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));

                    var portable = await LnpCheck.IsPortable(dialedPhoneNumber, teleToken).ConfigureAwait(false);

                    if (portable)
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

                        return View("Index", new PortingResults
                        {
                            PortedPhoneNumber = port,
                            Cart = cart
                        });
                    }
                    else
                    {
                        return View("Index", new PortingResults
                        {
                            PortedPhoneNumber = new PortedPhoneNumber { },
                            Cart = cart
                        });
                    }
                }
                else
                {
                    return View("Index", new PortingResults
                    {
                        PortedPhoneNumber = new PortedPhoneNumber { },
                        Cart = cart
                    });
                }
            }
            else
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Cart = cart
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPortingInformationAsync(PortRequest portRequest)
        {
            // http://www.mimekit.net/docs/html/Creating-Messages.htm
            var multipart = new Multipart("mixed");
            using var stream = new System.IO.MemoryStream();

            if (portRequest.BillImage != null && portRequest.BillImage.Length > 0)
            {
                await portRequest.BillImage.CopyToAsync(stream);

                var fileExtension = portRequest.BillImage.FileName.Split('.').LastOrDefault();

                // Yeet the image into an email attachment.
                var attachment = new MimePart("image", fileExtension)
                {
                    Content = new MimeContent(stream, ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(portRequest.BillImage.FileName)
                };

                multipart.Add(attachment);
            }

            var order = await Order.GetByIdAsync(portRequest.OrderId, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            // Save the rest of the data to the DB.
            var checkPortRequest = await portRequest.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            if (checkPortRequest)
            {
                // Send out the confirmation email.
                var confirmationEmail = new Email
                {
                    PrimaryEmailAddress = order.Email,
                    CarbonCopy = configuration.GetConnectionString("SmtpUsername"),
                    MessageBody = $@"Hi {order.FirstName},
                                                                                      
Thank you for choosing Accelerate Networks!

Your order Id is: {order.OrderId} and you have successfully added additional Porting information to accelerate the process.

You can review your order at https://acceleratenetworks.com/Cart/Order/{order.OrderId}
                                                                                      
A delivery specialist will send you a follow up email to walk you through the next steps in the process.

Thanks,

Accelerate Networks",
                    OrderId = order.OrderId,
                    Subject = $"Order: {order.OrderId} Added Porting Information",
                    Multipart = multipart
                };

                var checkSend = await confirmationEmail.SendEmailAsync(configuration.GetConnectionString("SmtpUsername"), configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
                var checkSave = await confirmationEmail.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

                if (checkSend && checkSave)
                {
                    Log.Information($"Sucessfully sent out the confirmation emails for {order.OrderId}.");
                }
                else
                {
                    Log.Fatal($"Failed to sent out the confirmation emails for {order.OrderId}.");
                }

                // Reset the session and clear the Cart.
                HttpContext.Session.Clear();

                return View("Success", new OrderWithPorts
                {
                    Order = order,
                    PortRequest = portRequest
                });
            }
            else
            {
                return RedirectToAction("Cart", "Order", portRequest.OrderId);
            }
        }
    }
}