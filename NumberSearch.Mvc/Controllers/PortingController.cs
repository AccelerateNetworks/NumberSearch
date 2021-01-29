using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using MimeKit;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PortingController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string _postgresql;
        private readonly Guid _teleToken;
        private readonly string _data247username;
        private readonly string _data247password;
        private readonly string _bulkVSAPIKey;
        private readonly string _azureStorage;

        public PortingController(IConfiguration config)
        {
            configuration = config;
            _postgresql = configuration.GetConnectionString("PostgresqlProd");
            _teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
            _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
            _azureStorage = config.GetConnectionString("AzureStorageAccount");
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
                        var checkNumber = await LrnBulkCnam.GetAsync(dialedPhoneNumber, _bulkVSAPIKey).ConfigureAwait(false);

                        bool wireless = false;

                        switch (checkNumber.lectype)
                        {
                            case "WIRELESS":
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

                        var numberName = new LIDBLookup();
                        try
                        {
                            numberName = await LIDBLookup.GetAsync(dialedPhoneNumber, _data247username, _data247password).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[Lookups] Failed to get LIBDName from Data 24/7 for number {dialedPhoneNumber}.");
                            Log.Error(ex.Message);
                            Log.Error(ex.InnerException.ToString());
                        }

                        checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;

                        if (portable)
                        {
                            Log.Information($"[Portability] {dialedPhoneNumber} is Portable.");

                            var port = new PortedPhoneNumber
                            {
                                PortedDialedNumber = dialedPhoneNumber,
                                NPA = npa,
                                NXX = nxx,
                                XXXX = xxxx,
                                City = checkNumber?.city,
                                State = checkNumber?.province,
                                DateIngested = DateTime.Now,
                                IngestedFrom = "UserInput",
                                Wireless = wireless,
                                LrnLookup = checkNumber
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
                                City = checkNumber?.city,
                                State = checkNumber?.province,
                                DateIngested = DateTime.Now,
                                IngestedFrom = "UserInput",
                                Wireless = wireless,
                                LrnLookup = checkNumber
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddPortingInformationAsync(PortRequest portRequest)
        {
            var order = await Order.GetByIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

            // Prevent duplicate submissions of port requests.
            if (order is not null && order.OrderId != Guid.Empty)
            {
                var existing = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                if (existing is not null && existing.OrderId != Guid.Empty && existing.OrderId == order.OrderId)
                {
                    // Reset the session and clear the Cart.
                    HttpContext.Session.Clear();

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = existing
                    });
                }
            }

            var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

            // http://www.mimekit.net/docs/html/Creating-Messages.htm
            var multipart = new Multipart("mixed");
            using var stream = new System.IO.MemoryStream();

            if (portRequest.BillImage != null && portRequest.BillImage.Length > 0)
            {
                try
                {
                    await portRequest.BillImage.CopyToAsync(stream).ConfigureAwait(false);

                    //var root = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    //var sink = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"));

                    //// Verify that the sink directory exists, and create it otherwise.
                    //if (!Directory.Exists(sink))
                    //{
                    //    Directory.CreateDirectory(sink);
                    //}

                    var fileExtension = Path.GetExtension(portRequest.BillImage.FileName);
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    //var fileStreamPath = Path.Combine(sink, fileName);

                    //// You have to rewind the MemoryStream before copying
                    //stream.Seek(0, SeekOrigin.Begin);

                    //using (FileStream fs = new FileStream(fileStreamPath, FileMode.OpenOrCreate))
                    //{
                    //    await stream.CopyToAsync(fs).ConfigureAwait(false);
                    //    await fs.FlushAsync().ConfigureAwait(false);
                    //    Log.Information($"[Port Request] Saved the bill image file to: {fileStreamPath}");
                    //}

                    // Create a BlobServiceClient object which will be used to create a container client
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_azureStorage);

                    //Create a unique name for the container
                    string containerName = portRequest.OrderId.ToString();

                    // Create the container and return a container client object
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();

                    // Get a reference to a blob
                    BlobClient blobClient = containerClient.GetBlobClient(fileName);

                    // Open the file and upload its data
                    // You have to rewind the MemoryStream before copying
                    stream.Seek(0, SeekOrigin.Begin);
                    await blobClient.UploadAsync(stream, true);

                    portRequest.BillImagePath = fileName;

                    // Yeet the image into an email attachment.
                    //var attachment = new MimePart("image", fileExtension)
                    //{
                    //    Content = new MimeContent(stream, ContentEncoding.Default),
                    //    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    //    ContentTransferEncoding = ContentEncoding.Base64,
                    //    FileName = Path.GetFileName(portRequest.BillImage.FileName)
                    //};

                    //multipart.Add(attachment);
                    Log.Information($"[Port Request] BlobContainer: {containerClient.Name} BlobClient: {blobClient.Name}");
                    Log.Information("[Port Request] Successfuly saved the bill image to the server and attached it to the confirmation email.");
                }
                catch (Exception ex)
                {
                    Log.Fatal("[Port Request] Failed to save the bill image to the server and attach it to the confirmation email.");
                    Log.Fatal($"[Port Request] {ex.Message}");
                    Log.Fatal($"[Port Request] {ex.InnerException}");
                }
            }

            // Save the rest of the data to the DB.
            var checkPortRequest = await portRequest.PostAsync(_postgresql).ConfigureAwait(false);

            if (checkPortRequest)
            {
                // Associate the ported numbers with their porting information.
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                string formattedNumbers = string.Empty;

                foreach (var number in portedNumbers)
                {
                    number.PortRequestId = portRequest.PortRequestId;
                    var checkPortUpdate = await number.PutAsync(_postgresql).ConfigureAwait(false);
                    formattedNumbers += $"<br />{number?.PortedDialedNumber}";
                }

                // Send out the confirmation email.
                var confirmationEmail = new Email
                {
                    PrimaryEmailAddress = order.Email,
                    CarbonCopy = configuration.GetConnectionString("SmtpUsername"),
                    MessageBody = $@"Hi {order.FirstName},
<br />
<br />
Thanks for adding porting information to your order!
<br />
<br />
Feel free to <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>review the order here</a>, and let us know if you have any questions.
<br />
<br />
Numbers tied to this port request:
{formattedNumbers}
<br />
<br />
Sincerely,
<br />
Accelerate Networks
<br />
206-858-8757 (call or text)",
                    OrderId = order.OrderId,
                    Subject = $"Porting information added for {portedNumbers.FirstOrDefault().PortedDialedNumber}",
                    Multipart = multipart
                };

                var checkSave = await confirmationEmail.PostAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

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