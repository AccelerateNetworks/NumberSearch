using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class PortingController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string _postgresql;
        private readonly Guid _teleToken;

        public PortingController(IConfiguration config)
        {
            configuration = config;
            _postgresql = configuration.GetConnectionString("PostgresqlProd");
            _teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));

        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CheckPortabilityAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            // Clean up the query.
            Query = Query?.Trim();

            if (Query is null || Query.Length == 0)
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Cart = cart
                });
            }

            // Parse the query.
            var converted = new List<char>();
            foreach (var letter in Query)
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
                    converted.Add(SearchController.LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            Query = new string(converted.ToArray());

            if (Query != null && Query?.Length == 10)
            {
                var dialedPhoneNumber = Query;

                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    try
                    {
                        var portable = await LnpCheck.IsPortable(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                        // Determine if the number is a wireless number.
                        var lrnLookup = await LrnLookup.GetAsync(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                        bool wireless = false;

                        switch (lrnLookup.data.ocn_type)
                        {
                            case "wireless":
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
                                IngestedFrom = "UserInput",
                                Wireless = wireless
                            };

                            return View("Index", new PortingResults
                            {
                                PortedPhoneNumber = port,
                                Cart = cart,
                                Message = wireless ? "This wireless phone number can be ported to our network!" : "This phone number can be ported to our network!"
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
                                IngestedFrom = "UserInput",
                                Wireless = wireless
                            };

                            return View("Index", new PortingResults
                            {
                                PortedPhoneNumber = port,
                                Cart = cart,
                                Message = wireless ? "This wireless phone number can likely be ported to our network!" : "This phone number can likely be ported to our network!"
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
                    var portable = await LnpCheck.IsPortable(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPortingInformationAsync(PortRequest portRequest)
        {
            // http://www.mimekit.net/docs/html/Creating-Messages.htm
            var multipart = new Multipart("mixed");
            using var stream = new System.IO.MemoryStream();

            if (portRequest.BillImage != null && portRequest.BillImage.Length > 0)
            {
                await portRequest.BillImage.CopyToAsync(stream).ConfigureAwait(false);

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

            var order = await Order.GetByIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

            var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

            var wireless = 0;

            // Determine if its a wireless number.
            foreach (var number in portedNumbers)
            {
                var lrnLookup = await LrnLookup.GetAsync(number.PortedDialedNumber, _teleToken).ConfigureAwait(false);

                switch (lrnLookup.data.ocn_type)
                {
                    case "wireless":
                        wireless++;
                        break;
                    case "PCS":
                        wireless++;
                        break;
                    case "P RESELLER":
                        wireless++;
                        break;
                    case "Wireless":
                        wireless++;
                        break;
                    case "W RESELLER":
                        wireless++;
                        break;
                    default:
                        break;
                }
            }

            // TODO: Figure out how to handle senarios where not all of the numbers are wireless in the order.
            if (wireless == portedNumbers.Count())
            {
                portRequest.WirelessNumber = true;
            }
            else
            {
                portRequest.WirelessNumber = false;
            }

            // Save the rest of the data to the DB.
            var checkPortRequest = await portRequest.PostAsync(_postgresql).ConfigureAwait(false);

            if (checkPortRequest)
            {
                // Associate the ported numbers with their porting information.
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                foreach (var number in portedNumbers)
                {
                    number.PortRequestId = portRequest.PortRequestId;
                    var checkPortUpdate = await number.PutAsync(_postgresql).ConfigureAwait(false);
                }

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