using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NewClientController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly MvcConfiguration _mvcConfiguration = mvcConfiguration;

        [HttpGet("Cart/Order/{orderId}/NewClient")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync(Guid orderId)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql);
            var productOrders = await ProductOrder.GetAsync(orderId, _postgresql).ConfigureAwait(false);
            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            NewClient newClient = await NewClient.GetByOrderIdAsync(order?.OrderId ?? new(), _postgresql).ConfigureAwait(false) ?? new();
            var phoneNumbers = productOrders?.AsValueEnumerable().Where(x => !string.IsNullOrWhiteSpace(x.DialedNumber)).Select(x => x.DialedNumber).ToArray();
            var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId, _postgresql).ConfigureAwait(false);
            var portedStrippedNumbers = portedNumbers?.Select(x => x.PortedDialedNumber).ToArray();
            var allNumbers = phoneNumbers?.Concat(portedStrippedNumbers ?? [])?.ToArray();

            if (newClient is null && order?.OrderId is not null)
            {
                newClient = new NewClient { NewClientId = Guid.NewGuid(), OrderId = order.OrderId };
                var checkCreate = await newClient.PostAsync(_postgresql).ConfigureAwait(false);

                if (checkCreate)
                {
                    newClient = await NewClient.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false) ?? new();
                }
            }

            if (newClient is not null)
            {
                // Gather up all the childern
                newClient.ExtensionRegistrations = await ExtensionRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.NumberDescriptions = await NumberDescription.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.IntercomRegistrations = await IntercomRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.SpeedDialKeys = await SpeedDialKey.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.FollowMeRegistrations = await FollowMeRegistration.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);
                newClient.PhoneMenuOptions = await PhoneMenuOption.GetByNewClientAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

                // There is info entered make sure the users sees it.
                if (newClient.IntercomRegistrations.Length != 0)
                {
                    newClient.Intercom = true;
                }

                if (!string.IsNullOrWhiteSpace(newClient.IntercomDescription))
                {
                    newClient.Intercom = true;
                }

                if (newClient.SpeedDialKeys.Length != 0)
                {
                    newClient.SpeedDial = true;
                }

                if (newClient.PhoneMenuOptions.Length != 0)
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
            }

            var form = new NewClientResult
            {
                Order = order ?? new(),
                NewClient = newClient ?? new(),
                ProductOrders = [.. productOrders ?? []],
                Products = [.. products],
                PhoneNumbers = allNumbers ?? [],
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

                        Log.Information("[New Client] BlobContainer: {Container} BlobClient: {Client}", containerClient.Name, blobClient.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal("[New Client] Failed to save the bill image to the server and attach it to the confirmation email.");
                        Log.Fatal("[New Client] {Message}", ex.Message);
                        Log.Fatal("[New Client] {InnerException}", ex.InnerException);
                    }
                }

                // If it exists update it, if not then create it.
                if (checkNewClient?.NewClientId == newClient?.NewClientId)
                {
                    var checkUpdate = await newClient!.PutAsync(_postgresql).ConfigureAwait(false);

                    if (checkUpdate)
                    {
                        newClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false) ?? new();
                    }
                }
                else
                {
                    var checkCreate = await newClient!.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkCreate)
                    {
                        newClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false) ?? new();
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
                if (newClient.IntercomRegistrations.Length != 0)
                {
                    newClient.Intercom = true;
                }

                if (!string.IsNullOrWhiteSpace(newClient.IntercomDescription))
                {
                    newClient.Intercom = true;
                }

                if (newClient.SpeedDialKeys.Length != 0)
                {
                    newClient.SpeedDial = true;
                }

                if (newClient.PhoneMenuOptions.Length != 0)
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
                var phoneNumbers = productOrders?.AsValueEnumerable().Where(x => !string.IsNullOrWhiteSpace(x.DialedNumber)).Select(x => x.DialedNumber).ToArray();
                var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(newClient.OrderId, _postgresql);
                var portedStrippedNumbers = portedNumbers?.AsValueEnumerable().Select(x => x.PortedDialedNumber).ToArray();
                var allNumbers = phoneNumbers?.Concat(portedStrippedNumbers ?? [])?.ToArray();

                return View("Index", new NewClientResult { NewClient = newClient, Order = order, Products = [.. products], ProductOrders = [.. productOrders ?? []], PhoneNumbers = allNumbers ?? [] });
            }

            return View("Index");
        }
    }
}
