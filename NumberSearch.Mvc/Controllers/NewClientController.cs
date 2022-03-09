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
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public NewClientController(IConfiguration config)
        {
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
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
                //NewClient = new NewClient
                //{
                //    TextingService = true,
                //    TextingServiceName = "ZipWhip",
                //    SpeedDialKeys = new SpeedDialKey[] { new SpeedDialKey { LabelOrName = "Front Desk", NumberOrExtension = "Ext 1" } },
                //    AfterHoursVoicemail = "Forward all voicemails to the front desk (Ext 1)",
                //    BusinessHours = "We are open from 8 AM to 5 PM and closed on the weekends.",
                //    CustomHoldMusic = true,
                //    HoldMusicDescription = "Please play X gonna give it to ya. Thanks",
                //    Intercom = true,
                //    IntercomRegistrations = new IntercomRegistration[] { new IntercomRegistration { ExtensionSendingIntercom = 1, ExtensionRecievingIntercom = 2 } },
                //    ExtensionRegistrations = new ExtensionRegistration[] {
                //        new ExtensionRegistration { ExtensionNumber = 103, NameOrLocation = "Jessica", Email = "jessia@exmaple.co", ModelOfPhone = "GXP2170", OutboundCallerId = "2068589310" },
                //        new ExtensionRegistration { ExtensionNumber = 202, NameOrLocation = "Jared", Email = "jared@example.co", ModelOfPhone = "Linphone", OutboundCallerId = "2068589313" },
                //        new ExtensionRegistration { ExtensionNumber = 105, NameOrLocation = "Will", Email = "will@example", ModelOfPhone = "GRP2615", OutboundCallerId = "2068588757" },
                //        new ExtensionRegistration { ExtensionNumber = 404, NameOrLocation = "Loading Dock", ModelOfPhone = "GRP2612W", OutboundCallerId = "2068588757" },
                //        new ExtensionRegistration { ExtensionNumber = 541, NameOrLocation = "Kitchen", ModelOfPhone = "GRP2612W", OutboundCallerId = "2068588757" }
                //    },
                //    PhoneMenu = true,
                //    PhonesToRingOrMenuDescription = "Please ring to a phone menu that lists our extension numbers and the dials through to the front desk.",
                //    OverheadPaging = true,
                //    OverheadPagingDescription = "Big loud sounds come from the front desk and go to the loading dock.",
                //    PhoneOfflineInstructions = "Forward the call to the front desk.",
                //    FollowMeRegistrations = new FollowMeRegistration[] {
                //        new FollowMeRegistration { NumberOrExtension = "1", CellPhoneNumber = "2068588757", UnreachablePhoneNumber = "Front Desk" }
                //    },
                //    NumberDescriptions = new NumberDescription[] {
                //        new NumberDescription { PhoneNumber = "2068589310", Description = "Ring to 103 Jessica" },
                //        new NumberDescription { PhoneNumber = "2068589313", Description = "Ring to 105 Jared" },
                //        new NumberDescription { PhoneNumber = "2068588757", Description = "Ring 103 and 105", Prefix = "Support" }
                //    },
                //    OrderId = order.OrderId,
                //    BillingClientId = order.BillingClientId,
                //    NewClientId = Guid.NewGuid()
                //}
            };

            return View("Index", form);
        }


        [HttpPost("Cart/Order/{orderId}/NewClient/")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitNewClientAsync(Guid orderId, NewClient newClient)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);

            if (order is not null && newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var checkNewClient = await NewClient.GetAsync(newClient.NewClientId, _postgresql).ConfigureAwait(false);

                // If it exist update it, if not then create it.
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
