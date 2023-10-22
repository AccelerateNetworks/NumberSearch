using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PortingController : Controller
    {
        private readonly string _postgresql;
        private readonly string _bulkVSAPIKey;
        private readonly string _bulkVSAPIUsername;
        private readonly string _bulkVSAPIPassword;
        private readonly string _azureStorage;
        private readonly string _SmtpUsername;
        private readonly MvcConfiguration _mvcConfiguration;

        public PortingController(MvcConfiguration mvcConfiguration)
        {
            _postgresql = mvcConfiguration.PostgresqlProd;
            _bulkVSAPIKey = mvcConfiguration.BulkVSAPIKEY;
            _bulkVSAPIUsername = mvcConfiguration.BulkVSUsername;
            _bulkVSAPIPassword = mvcConfiguration.BulkVSPassword;
            _azureStorage = mvcConfiguration.AzureStorageAccount;
            _SmtpUsername = mvcConfiguration.SmtpUsername;
            _mvcConfiguration = mvcConfiguration;
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

            if (string.IsNullOrWhiteSpace(Query))
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Query = Query,
                    Cart = cart
                });
            }

            // Clean up the query.
            Query = Query.Trim().ToLowerInvariant();

            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(Query, out var phoneNumber);

            if (checkParse && phoneNumber is not null)
            {
                try
                {
                    var lookup = new LookupController(_mvcConfiguration);
                    var portable = await lookup.VerifyPortabilityAsync(Query);

                    if (portable is not null && portable.Portable)
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");


                        return View("Index", new PortingResults
                        {
                            PortedPhoneNumber = portable,
                            Cart = cart,
                            Query = Query,
                            Message = portable.Wireless ? "✔️ This wireless phone number can be ported to our network!" : "✔️ This phone number can be ported to our network!"
                        });
                    }
                    else
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                        var port = new PortedPhoneNumber
                        {
                            PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                            NPA = phoneNumber.NPA,
                            NXX = phoneNumber.NXX,
                            XXXX = phoneNumber.XXXX,
                            City = portable?.City ?? string.Empty,
                            State = portable?.State ?? string.Empty,
                            DateIngested = DateTime.Now,
                            IngestedFrom = "UserInput",
                            Wireless = false,
                            LrnLookup = portable?.LrnLookup ?? new()
                        };

                        return View("Index", new PortingResults
                        {
                            PortedPhoneNumber = port,
                            Cart = cart,
                            Query = Query,
                            Message = port.Wireless ? "❌ This wireless phone number cannot be ported to our network." : "❌ This phone number cannot be ported to our network."
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal($"[Portability] {ex.Message}");

                    var port = new PortedPhoneNumber
                    {
                        PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput"
                    };

                    return View("Index", new PortingResults
                    {
                        PortedPhoneNumber = port,
                        Cart = cart,
                        Query = Query,
                        Message = "❓ This phone number can likely be ported to our network!"
                    });
                }
            }
            else
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Message = $"❓ Did you mean to Search for purchasable numbers? {Query} isn't transferable.",
                    AlertType = "alert-warning",
                    Query = Query,
                    Cart = cart
                });
            }

        }

        [HttpPost]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RequestPortAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(Query, out var phoneNumber);

            if (checkParse && phoneNumber is not null)
            {
                var portable = await ValidatePortability.GetAsync(phoneNumber.DialedNumber ?? string.Empty, _bulkVSAPIUsername, _bulkVSAPIPassword).ConfigureAwait(false);

                if (portable is not null && portable.Portable)
                {
                    var port = new PortedPhoneNumber
                    {
                        PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput"
                    };

                    return View("Index", new PortingResults
                    {
                        PortedPhoneNumber = port,
                        Query = Query,
                        Cart = cart
                    });
                }
                else
                {
                    return View("Index", new PortingResults
                    {
                        PortedPhoneNumber = new PortedPhoneNumber { },
                        Query = Query,
                        Cart = cart
                    });
                }
            }
            else
            {
                return View("Index", new PortingResults
                {
                    PortedPhoneNumber = new PortedPhoneNumber { },
                    Query = Query,
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

            portRequest.PortRequestId = Guid.NewGuid();

            // Prevent duplicate submissions of port requests.
            if (order is not null && order.OrderId != Guid.Empty)
            {
                var existing = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                if (existing is not null && existing.OrderId != Guid.Empty && existing.OrderId == order.OrderId)
                {
                    // Update the existing port request.
                    if (portRequest.BillImage != null && portRequest.BillImage.Length > 0)
                    {
                        try
                        {
                            using var stream = new System.IO.MemoryStream();
                            await portRequest.BillImage.CopyToAsync(stream).ConfigureAwait(false);

                            var fileExtension = Path.GetExtension(portRequest.BillImage.FileName);
                            var fileName = $"{Guid.NewGuid()}{fileExtension}";

                            // Create a BlobServiceClient object which will be used to create a container client
                            BlobServiceClient blobServiceClient = new(_azureStorage);

                            //Create a unique name for the container
                            string containerName = existing.OrderId.ToString();

                            // Create the container and return a container client object
                            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                            await containerClient.CreateIfNotExistsAsync();

                            // Get a reference to a blob
                            BlobClient blobClient = containerClient.GetBlobClient(fileName);

                            // Open the file and upload its data
                            // You have to rewind the MemoryStream before copying
                            stream.Seek(0, SeekOrigin.Begin);
                            await blobClient.UploadAsync(stream, true);

                            existing.BillImagePath = fileName;

                            Log.Information($"[Port Request] BlobContainer: {containerClient.Name} BlobClient: {blobClient.Name}");
                            Log.Information("[Port Request] Successfully saved the bill image to the server and attached it to the confirmation email.");
                        }
                        catch (Exception ex)
                        {
                            Log.Fatal("[Port Request] Failed to save the bill image to the server and attach it to the confirmation email.");
                            Log.Fatal($"[Port Request] {ex.Message}");
                            Log.Fatal($"[Port Request] {ex.InnerException}");
                        }
                    }

                    // Format the address information
                    Log.Information($"[Port Request] Parsing address data from {portRequest.Address}");
                    if (portRequest is not null && !string.IsNullOrWhiteSpace(portRequest.Address))
                    {
                        existing.Address = portRequest.Address;
                        var addressParts = portRequest.Address.Split(", ");
                        if (addressParts.Length > 4)
                        {
                            existing.Address = addressParts[0];
                            existing.City = addressParts[1];
                            existing.State = addressParts[2];
                            existing.Zip = addressParts[3];
                            Log.Information($"[Port Request] Address: {existing.Address} City: {existing.City} State: {existing.State} Zip: {existing.Zip}");
                        }
                        else
                        {
                            Log.Error($"[Port Request] Failed automatic address formatting.");
                            return RedirectToAction("Cart", "Order", existing.OrderId);
                        }
                    }
                    else
                    {
                        Log.Error($"[Port Request] No address information submitted.");
                        return RedirectToAction("Cart", "Order", existing?.OrderId);
                    }

                    existing.BillingPhone = existing.BillingPhone != portRequest.BillingPhone ? portRequest.BillingPhone : existing.BillingPhone;
                    existing.BusinessContact = existing.BusinessContact != portRequest.BusinessContact ? portRequest.BusinessContact : existing.BusinessContact;
                    existing.ProviderAccountNumber = existing.ProviderAccountNumber != portRequest.ProviderAccountNumber ? portRequest.ProviderAccountNumber : existing.ProviderAccountNumber;
                    existing.BusinessName = existing.BusinessName != portRequest.BusinessName ? portRequest.BusinessName : existing.BusinessName;
                    existing.CallerId = existing.CallerId != portRequest.CallerId ? portRequest.CallerId : existing.CallerId;
                    existing.ProviderPIN = existing.ProviderPIN != portRequest.ProviderPIN ? portRequest.ProviderPIN : existing.ProviderPIN;
                    existing.PartialPort = existing.PartialPort != portRequest.PartialPort ? portRequest.PartialPort : existing.PartialPort;
                    existing.PartialPortDescription = existing.PartialPortDescription != portRequest.PartialPortDescription ? portRequest.PartialPortDescription : existing.PartialPortDescription;
                    existing.LocationType = existing.LocationType != portRequest.LocationType ? portRequest.LocationType : existing.LocationType;

                    // Save the rest of the data to the DB.
                    var checkExisting = await existing.PutAsync(_postgresql).ConfigureAwait(false);

                    if (checkExisting && order is not null)
                    {
                        // Associate the ported numbers with their porting information.
                        portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                        var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                        foreach (var number in portedNumbers)
                        {
                            number.PortRequestId = portRequest.PortRequestId;
                            var checkPortUpdate = await number.PutAsync(_postgresql).ConfigureAwait(false);
                        }
                    }

                    // Reset the session and clear the Cart.
                    HttpContext.Session.Clear();

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = existing
                    });
                }
            }

            if (portRequest.BillImage != null && portRequest.BillImage.Length > 0)
            {
                try
                {
                    using var stream = new System.IO.MemoryStream();

                    await portRequest.BillImage.CopyToAsync(stream).ConfigureAwait(false);

                    var fileExtension = Path.GetExtension(portRequest.BillImage.FileName);
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";

                    // Create a BlobServiceClient object which will be used to create a container client
                    BlobServiceClient blobServiceClient = new(_azureStorage);

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

                    Log.Information($"[Port Request] BlobContainer: {containerClient.Name} BlobClient: {blobClient.Name}");
                    Log.Information("[Port Request] Successfully saved the bill image to the server and attached it to the confirmation email.");
                }
                catch (Exception ex)
                {
                    Log.Fatal("[Port Request] Failed to save the bill image to the server and attach it to the confirmation email.");
                    Log.Fatal($"[Port Request] {ex.Message}");
                    Log.Fatal($"[Port Request] {ex.InnerException}");
                }
            }


            // Format the address information
            Log.Information($"[Port Request] Parsing address data from {portRequest.Address}");
            if (portRequest is not null && !string.IsNullOrWhiteSpace(portRequest.Address))
            {
                var addressParts = portRequest.Address.Split(", ");
                if (addressParts.Length > 4)
                {
                    portRequest.Address = addressParts[0];
                    portRequest.City = addressParts[1];
                    portRequest.State = addressParts[2];
                    portRequest.Zip = addressParts[3];
                    Log.Information($"[Port Request] Address: {portRequest.Address} City: {portRequest.City} State: {portRequest.State} Zip: {portRequest.Zip}");
                }
                else
                {
                    Log.Error($"[Port Request] Failed automatic address formatting.");
                    return RedirectToAction("Cart", "Order", portRequest.OrderId);
                }
            }
            else
            {
                Log.Error($"[Port Request] No address information submitted.");
                return RedirectToAction("Cart", "Order", portRequest?.OrderId);
            }

            // Save the rest of the data to the DB.
            var checkPortRequest = await portRequest.PostAsync(_postgresql).ConfigureAwait(false);

            if (checkPortRequest && order is not null)
            {
                // Associate the ported numbers with their porting information.
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

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
                    CarbonCopy = _SmtpUsername,
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
                    Subject = $"Porting information added for {portedNumbers.FirstOrDefault()?.PortedDialedNumber}"
                };

                var checkSave = await confirmationEmail.PostAsync(_postgresql).ConfigureAwait(false);

                // Trigger the backwork process to run again and send this email.
                order.BackgroundWorkCompleted = false;
                var checkOrder = await order.PutAsync(_postgresql).ConfigureAwait(false);

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