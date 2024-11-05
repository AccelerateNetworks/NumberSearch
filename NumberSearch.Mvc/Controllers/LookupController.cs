using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.Mvc.Models;

using PhoneNumbersNA;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableRateLimiting("lookup")]
    public class LookupController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly string _bulkVSKey = mvcConfiguration.BulkVSAPIKEY;
        private readonly string _bulkVSUsername = mvcConfiguration.BulkVSUsername;
        private readonly string _bulkVSPassword = mvcConfiguration.BulkVSPassword;
        private readonly string _callWithUsAPIkey = mvcConfiguration.CallWithUsAPIKEY;

        [HttpGet]
        [DisableRateLimiting]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
        public IActionResult Index(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                return View("Index");
            }
            else
            {
                return View("Index", new LookupResults { DialedNumber = dialedNumber });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [OutputCache(Duration = 3600, VaryByQueryKeys = ["dialedNumber"])]
        public async Task<IActionResult> BulkPortAsync([Bind("dialedNumber")] string dialedNumber)
        {
            // Add portable numbers to cart in bulk
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var parsedNumbers = dialedNumber.ExtractDialedNumbers().ToArray();

                if (parsedNumbers.Length == 0)
                {
                    return View("Index", new LookupResults
                    {
                        Message = "No dialed phone numbers found. Please try a different query. 🥺👉👈"
                    });
                }

                var cart = Cart.GetFromSession(HttpContext.Session);

                // If they have an invalid Session we don't want to waste any time running queries for them.
                if (string.IsNullOrWhiteSpace(HttpContext.Session.Id) || !HttpContext.Session.IsAvailable || cart is null)
                {
                    return View("Index");
                }

                var results = new List<PortedPhoneNumber>();
                await Parallel.ForEachAsync(parsedNumbers, async (number, token) =>
                {
                    var result = await VerifyPortabilityAsync(number);
                    results.Add(result);
                });

                var portableNumbers = results.Where(x => x.Portable && x.Wireless is false).ToArray();
                var notPortable = results.Where(x => x.Portable is false).Select(x => x.PortedDialedNumber).ToArray();

                // Separate wireless numbers out from the rest.
                var wirelessPortable = results.Where(x => x.Wireless && x.Portable).ToArray();

                // Add all the numbers to the cart.
                foreach (var portableNumber in portableNumbers)
                {
                    var portedNumber = cart.PortedPhoneNumbers?.Where(x => x.PortedDialedNumber == portableNumber.PortedDialedNumber).FirstOrDefault();

                    if (portedNumber is null)
                    {
                        var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = portableNumber.PortedDialedNumber, PortedPhoneNumberId = portableNumber.PortedPhoneNumberId, Quantity = 1 };

                        var checkAdd = cart.AddPortedPhoneNumber(portableNumber, productOrder);
                    }
                }

                foreach (var wirelessNumber in wirelessPortable)
                {
                    var portedNumber = cart.PortedPhoneNumbers?.Where(x => x.PortedDialedNumber == wirelessNumber.PortedDialedNumber).FirstOrDefault();

                    if (portedNumber is null)
                    {
                        var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = wirelessNumber.PortedDialedNumber, PortedPhoneNumberId = wirelessNumber.PortedPhoneNumberId, Quantity = 1 };

                        var checkAdd = cart.AddPortedPhoneNumber(wirelessNumber, productOrder);
                    }
                }

                var checkSet = cart.SetToSession(HttpContext.Session);

                return View("Index", new LookupResults
                {
                    DialedNumber = dialedNumber,
                    Portable = portableNumbers,
                    Wireless = wirelessPortable,
                    NotPortable = notPortable,
                    Cart = cart
                });
            }
            else
            {
                return View("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [OutputCache(Duration = 3600, VaryByQueryKeys = ["dialedNumber"])]
        public async Task<IActionResult> ToCSVAsync([Bind("dialedNumber")] string dialedNumber)
        {
            // Add portable numbers to cart in bulk
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var parsedNumbers = dialedNumber.ExtractDialedNumbers().ToArray();

                if (parsedNumbers.Length == 0)
                {
                    return View("Index", new LookupResults
                    {
                        Message = "No dialed phone numbers found. Please try a different query. 🥺👉👈"
                    });
                }

                var cart = Cart.GetFromSession(HttpContext.Session);

                // If they have an invalid Session we don't want to waste any time running queries for them.
                if (string.IsNullOrWhiteSpace(HttpContext.Session.Id) || !HttpContext.Session.IsAvailable || cart is null)
                {
                    return View("Index");
                }

                var results = new List<PortedPhoneNumber>();
                await Parallel.ForEachAsync(parsedNumbers, async (number, token) =>
                {
                    var result = await VerifyPortabilityAsync(number);
                    results.Add(result);
                });

                var portableNumbers = results.Where(x => x.Portable && x.Wireless is false).ToArray();
                var notPortable = results.Where(x => x.Portable is false).Select(x => x.PortedDialedNumber).ToArray();

                // Separate wireless numbers out from the rest.
                var wirelessPortable = results.Where(x => x.Wireless && x.Portable).ToArray();

                var builder = new StringBuilder();
                builder.AppendLine("DialedNumber,City,State,DateIngested,Wireless,Portable," +
                    "LastPorted,SPID,LATA,LEC,LECType," +
                    "LIDBName,LRN,OCN");
                foreach (var number in results)
                {
                    builder.AppendLine($"{number.PortedDialedNumber},{number.City},{number.State},{number.DateIngested},{number.Wireless},{number.Portable}," +
                        $"{number.LrnLookup.LastPorted},{number.LrnLookup.SPID},{number.LrnLookup.LATA},{number.LrnLookup.LEC.Replace(",", " ")},{number.LrnLookup.LECType}," +
                        $"{number.LrnLookup.LIDBName},{number.LrnLookup.LRN},{number.LrnLookup.OCN}");
                }

                return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"AccelerateNetworksPhoneNumbers{DateTime.Now:yyyyMMddTHHmmss}.csv");
            }
            else
            {
                return View("Index");
            }
        }

        public async Task<PortedPhoneNumber> VerifyPortabilityAsync(string dialedNumber)
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);

            if (checkParse)
            {
                try
                {
                    var portable = await ValidatePortability.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSUsername.AsMemory(), _bulkVSPassword.AsMemory());

                    // Fail fast
                    if (portable.Portable is false)
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                        return new PortedPhoneNumber
                        {
                            PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                            Portable = false
                        };
                    }

                    // This is free
                    //var checkNumber = await PhoneNumberLookup.GetByDialedNumberAsync(dialedNumber.Length is 10 ? $"1{dialedNumber}" : dialedNumber, _postgresql);
                    PhoneNumberLookup checkNumber = null!;

                    bool freshQuery = false;
                    // Lookup the number.
                    if (checkNumber is null && phoneNumber.Type is NumberType.Canada)
                    {
                        // Warning this costs $
                        var canada = await DataAccess.CallWithUs.LRNLookup.GetAsync(phoneNumber.DialedNumber.AsMemory(), _callWithUsAPIkey.AsMemory());

                        checkNumber = new PhoneNumberLookup(new LrnBulkCnam
                        {
                            lata = canada.LATA ?? string.Empty,
                            lrn = canada.LRN ?? string.Empty,
                            jurisdiction = canada.State ?? string.Empty,
                            ocn = canada.OCN ?? string.Empty,
                            ratecenter = canada.Ratecenter ?? string.Empty,
                            tn = $"1{phoneNumber.DialedNumber}",
                            lec = canada.Company ?? string.Empty,
                            lectype = canada.Prefix_Type ?? string.Empty,
                            city = canada.Ratecenter ?? string.Empty,
                            province = canada.State ?? string.Empty,
                        })
                        {
                            LosingCarrier = portable.LosingCarrier ?? string.Empty
                        };

                        // Warning this costs $$$$
                        var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSKey.AsMemory());
                        checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName.name) ? string.Empty : numberName.name ?? string.Empty;
                        freshQuery = true;
                    }
                    else if (checkNumber is null && phoneNumber.Type is NumberType.Tollfree)
                    {
                        checkNumber = new PhoneNumberLookup()
                        {
                            DialedNumber = portable.TN ?? $"1{phoneNumber.DialedNumber}",
                            Ratecenter = portable.RateCenter ?? string.Empty,
                            State = portable.State ?? string.Empty,
                            LosingCarrier = portable.LosingCarrier ?? string.Empty,
                        };
                    }
                    else if (checkNumber is null)
                    {
                        // Warning this costs $$$$
                        var result = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSKey.AsMemory());
                        checkNumber = new PhoneNumberLookup(result)
                        {
                            LosingCarrier = portable.LosingCarrier ?? string.Empty
                        };

                        // Warning this costs $$$$
                        var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSKey.AsMemory());
                        checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName.name) ? string.Empty : numberName.name ?? string.Empty;
                        freshQuery = true;
                    }

                    // Determine if the number is a wireless number.
                    bool wireless = false;

                    switch (checkNumber.LECType)
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

                    // Find the carrier info
                    var carrier = await Carrier.GetByOCNAsync(checkNumber.OCN, _postgresql).ConfigureAwait(false);
                    if (carrier is not null)
                    {
                        checkNumber.CarrierId = carrier.CarrierId;
                    }

                    // Log the lookup to the db if it's new.
                    if (freshQuery)
                    {
                        var checkLog = await checkNumber.PostAsync(_postgresql).ConfigureAwait(false);
                    }

                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");

                    var portableNumber = new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = checkNumber.City,
                        State = checkNumber.State,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless,
                        LrnLookup = checkNumber,
                        Carrier = carrier ?? new(),
                        Portable = true
                    };

                    return portableNumber;
                }
                catch (Exception ex)
                {
                    Log.Information($"[Portability] {dialedNumber} is not Portable.");
                    Log.Fatal($"[Portability] {ex.Message}");
                    Log.Fatal($"[Portability] {ex.InnerException}");

                    return new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = phoneNumber.DialedNumber,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        Portable = false
                    };
                }
            }
            else
            {
                Log.Information($"[Portability] {dialedNumber} is not Portable. Failed NPA, NXX, XXXX parsing.");

                return new PortedPhoneNumber
                {
                    PortedPhoneNumberId = Guid.NewGuid(),
                    PortedDialedNumber = dialedNumber,
                    Portable = false
                };
            }
        }
    }
}