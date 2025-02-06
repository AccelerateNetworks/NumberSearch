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
    public class PortingController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly string _bulkVSAPIUsername = mvcConfiguration.BulkVSUsername;
        private readonly string _bulkVSAPIPassword = mvcConfiguration.BulkVSPassword;
        private readonly string _azureStorage = mvcConfiguration.AzureStorageAccount;
        private readonly string _SmtpUsername = mvcConfiguration.SmtpUsername;

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

            if (checkParse)
            {
                try
                {
                    var lookup = new LookupController(mvcConfiguration);
                    var portable = await lookup.VerifyPortabilityAsync(Query);

                    if (portable is not null && portable.Portable)
                    {
                        Log.Information("[Portability] {DialedNumber} is Portable.", phoneNumber.DialedNumber);


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
                        Log.Information("[Portability] {DialedNumber} is not Portable.", phoneNumber.DialedNumber);

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
                    Log.Fatal("[Portability] {Message}", ex.Message);

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

            if (checkParse)
            {
                var portable = await ValidatePortability.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSAPIUsername.AsMemory(), _bulkVSAPIPassword.AsMemory());

                if (portable.Portable)
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
    }
}