using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NewClientController : Controller
    {
        private readonly string _postgresql;

        public NewClientController(MvcConfiguration mvcConfiguration)
        {
            _postgresql = mvcConfiguration.PostgresqlProd;
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


        [HttpPost("Cart/Order/{orderId}/NewClient/")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitNewClientAsync(Guid orderId, NewClient? newClient)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);

            if (order is not null && newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var checkNewClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

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

                var productOrders = await ProductOrder.GetAsync(orderId, _postgresql).ConfigureAwait(false);
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var phoneNumbers = productOrders.Where(x => !string.IsNullOrWhiteSpace(x.DialedNumber))?.Select(x => x.DialedNumber)?.ToArray();
                var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId, _postgresql).ConfigureAwait(false);
                var portedStrippedNumbers = portedNumbers?.Select(x => x.PortedDialedNumber).ToArray();
                var allNumbers = phoneNumbers?.Concat(portedStrippedNumbers ?? Array.Empty<string>())?.ToArray();

                return View("Index", new NewClientResult { NewClient = newClient, Order = order, Products = products.ToArray(), ProductOrders = productOrders.ToArray(), PhoneNumbers = allNumbers ?? Array.Empty<string>() });
            }

            return View("Index");
        }
    }
}
