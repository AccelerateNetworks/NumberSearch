using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NewClientController : Controller
    {
        private readonly string _postgresql;
        private readonly MvcConfiguration _mvcConfiguration;

        public NewClientController(MvcConfiguration mvcConfiguration)
        {
            _postgresql = mvcConfiguration.PostgresqlProd;
            _mvcConfiguration = mvcConfiguration;
        }

        [HttpGet("Cart/Order/{orderId}/NewClient")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync(Guid orderId)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql);
            var productOrders = await ProductOrder.GetAsync(orderId, _postgresql).ConfigureAwait(false);
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            var newClient = await NewClient.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
            var phoneNumbers = productOrders.Where(x => !string.IsNullOrWhiteSpace(x.DialedNumber))?.Select(x => x.DialedNumber)?.ToArray();
            var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId, _postgresql).ConfigureAwait(false);
            var portedStrippedNumbers = portedNumbers?.Select(x => x.PortedDialedNumber).ToArray();
            var allNumbers = phoneNumbers?.Concat(portedStrippedNumbers ?? Array.Empty<string>())?.ToArray();

            if (newClient is null)
            {
                newClient = new NewClient { NewClientId = Guid.NewGuid(), OrderId = order.OrderId };
                var checkCreate = await newClient.PostAsync(_postgresql).ConfigureAwait(false);

                if (checkCreate)
                {
                    newClient = await NewClient.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                }
            }

            // Gather up all the childern
            newClient.ExtensionRegistrations = await ExtensionRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
            newClient.NumberDescriptions = await NumberDescription.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
            newClient.IntercomRegistrations = await IntercomRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
            newClient.SpeedDialKeys = await SpeedDialKey.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
            newClient.FollowMeRegistrations = await FollowMeRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
            newClient.PhoneMenuOptions = await PhoneMenuOption.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

            // There is info entered make sure the users sees it.
            if (newClient.IntercomRegistrations.Any())
            {
                newClient.Intercom = true;
            }

            if (!string.IsNullOrWhiteSpace(newClient.IntercomDescription))
            {
                newClient.Intercom = true;
            }

            if (newClient.SpeedDialKeys.Any())
            {
                newClient.SpeedDial = true;
            }

            if (newClient.PhoneMenuOptions.Any())
            {
                newClient.PhoneMenu = true;
            }

            if (!string.IsNullOrWhiteSpace(newClient.TextingServiceName))
            {
                newClient.TextingService = true;
            }

            if (!string.IsNullOrWhiteSpace(newClient.OverheadPagingDescription))
            {
                newClient.OverheadPaging = true;
            }

            var form = new NewClientResult
            {
                Order = order,
                NewClient = newClient,
                ProductOrders = productOrders.ToArray(),
                Products = products.ToArray(),
                PhoneNumbers = allNumbers ?? Array.Empty<string>(),
            };

            return View("Index", form);
        }


        [HttpPost("NewClient/AddNewClientInformation")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitNewClientAsync(NewClient newClient)
        {
            var order = await Order.GetByIdAsync(newClient.OrderId, _postgresql).ConfigureAwait(false);

            if (order is not null && newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var checkNewClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

                using var stream = new System.IO.MemoryStream();

                if (newClient.BillImage != null && newClient.BillImage.Length > 0)
                {
                    try
                    {
                        await newClient.BillImage.CopyToAsync(stream).ConfigureAwait(false);

                        var fileExtension = Path.GetExtension(newClient.BillImage.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";

                        // Create a BlobServiceClient object which will be used to create a container client
                        BlobServiceClient blobServiceClient = new(_mvcConfiguration.AzureStorageAccount);

                        //Create a unique name for the container
                        string containerName = newClient.OrderId.ToString();

                        // Create the container and return a container client object
                        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                        await containerClient.CreateIfNotExistsAsync();

                        // Get a reference to a blob
                        BlobClient blobClient = containerClient.GetBlobClient(fileName);

                        // Open the file and upload its data
                        // You have to rewind the MemoryStream before copying
                        stream.Seek(0, SeekOrigin.Begin);
                        await blobClient.UploadAsync(stream, true);

                        newClient.BillImagePath = fileName;

                        Log.Information($"[New Client] BlobContainer: {containerClient.Name} BlobClient: {blobClient.Name}");
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal("[New Client] Failed to save the bill image to the server and attach it to the confirmation email.");
                        Log.Fatal($"[New Client] {ex.Message}");
                        Log.Fatal($"[New Client] {ex.InnerException}");
                    }
                }

                // If it exists update it, if not then create it.
                if (checkNewClient?.NewClientId == newClient?.NewClientId)
                {
                    var checkUpdate = await newClient!.PutAsync(_postgresql).ConfigureAwait(false);

                    if (checkUpdate)
                    {
                        newClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                    }
                }
                else
                {
                    var checkCreate = await newClient!.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkCreate)
                    {
                        newClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                    }
                }

                // Gather up all the children
                newClient.ExtensionRegistrations = await ExtensionRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.NumberDescriptions = await NumberDescription.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.IntercomRegistrations = await IntercomRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.SpeedDialKeys = await SpeedDialKey.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.FollowMeRegistrations = await FollowMeRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.PhoneMenuOptions = await PhoneMenuOption.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

                // There is info entered make sure the users sees it.
                if (newClient.IntercomRegistrations.Any())
                {
                    newClient.Intercom = true;
                }

                if (!string.IsNullOrWhiteSpace(newClient.IntercomDescription))
                {
                    newClient.Intercom = true;
                }

                if (newClient.SpeedDialKeys.Any())
                {
                    newClient.SpeedDial = true;
                }

                if (newClient.PhoneMenuOptions.Any())
                {
                    newClient.PhoneMenu = true;
                }

                if (!string.IsNullOrWhiteSpace(newClient.TextingServiceName))
                {
                    newClient.TextingService = true;
                }

                if (!string.IsNullOrWhiteSpace(newClient.OverheadPagingDescription))
                {
                    newClient.OverheadPaging = true;
                }

                var productOrders = await ProductOrder.GetAsync(newClient.OrderId, _postgresql).ConfigureAwait(false);
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var phoneNumbers = productOrders.Where(x => !string.IsNullOrWhiteSpace(x.DialedNumber))?.Select(x => x.DialedNumber)?.ToArray();
                var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(newClient.OrderId, _postgresql).ConfigureAwait(false);
                var portedStrippedNumbers = portedNumbers?.Select(x => x.PortedDialedNumber).ToArray();
                var allNumbers = phoneNumbers?.Concat(portedStrippedNumbers ?? Array.Empty<string>())?.ToArray();

                return View("Index", new NewClientResult { NewClient = newClient, Order = order, Products = products.ToArray(), ProductOrders = productOrders.ToArray(), PhoneNumbers = allNumbers ?? Array.Empty<string>() });
            }

            return View("Index");
        }
    }
}
