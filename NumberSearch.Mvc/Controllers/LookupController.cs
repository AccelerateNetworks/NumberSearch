using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.TeliMessage;
using NumberSearch.Mvc.Models;

using PhoneNumbersNA;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LookupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly string _bulkVSKey;
        private readonly string _callWithUsAPIkey;

        public LookupController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            _callWithUsAPIkey = config.GetConnectionString("CallWithUsAPIKEY");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(string dialedNumber)
        {
            // Lookup numbers in bulk
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var numberCanidates = dialedNumber.Trim().Replace(") ", "", StringComparison.CurrentCultureIgnoreCase).Replace("\r\n", " ", StringComparison.CurrentCultureIgnoreCase).Split(" ");

                var parsedNumbers = new List<string>();

                foreach (var query in numberCanidates)
                {
                    // Parse the query.
                    var converted = new List<char>();
                    foreach (var letter in query)
                    {
                        // Allow digits.
                        if (char.IsDigit(letter))
                        {
                            converted.Add(letter);
                        }
                        // Drop everything else.
                    }

                    // Drop leading 1's to improve the copy/paste experiance.
                    if (converted.Count >= 10 && converted[0] == '1')
                    {
                        converted.Remove('1');
                    }

                    // Only if its a perfect number do we want to query for it.
                    if (converted.Count == 10)
                    {
                        parsedNumbers.Add(new string(converted.ToArray()));
                    }
                }

                var results = await Task.WhenAll(parsedNumbers.Select(InvestigateAsync));

                return View("Index", new LookupResults
                {
                    DialedNumber = dialedNumber,
                    Lookups = results
                });
            }
            else
            {
                return View("Index");
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> BulkPortAsync(string dialedNumber)
        {
            // Add portable numbers to cart in bulk
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var parsedNumbers = dialedNumber.ExtractDialedNumbers();

                if (!parsedNumbers.Any())
                {
                    return View("Index", new LookupResults
                    {
                        Message = "No dialed phone numbers found. Please try a different query. 🥺👉👈"
                    });
                }

                var cart = Cart.GetFromSession(HttpContext.Session);

                var results = await VerifyPortablityInBulkAsync(parsedNumbers.ToArray());

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

        public async Task<PortedPhoneNumber> VerifyPortablityAsync(string number)
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number, out var phoneNumber);

            if (checkParse && phoneNumber is not null)
            {
                try
                {
                    var portable = await LnpCheck.IsPortableAsync(phoneNumber.DialedNumber, _teleToken).ConfigureAwait(false);

                    // Fail fast
                    if (portable is not true)
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                        return new PortedPhoneNumber
                        {
                            PortedDialedNumber = phoneNumber.DialedNumber,
                            Portable = false
                        };
                    }

                    // Lookup the number.
                    var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _bulkVSKey).ConfigureAwait(false);

                    // Determine if the number is a wireless number.
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

                    var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber, _bulkVSKey);
                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName?.name;

                    Log.Information($"[Portability] {number} is Portable.");

                    var portableNumber = new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = phoneNumber.DialedNumber,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = checkNumber.city,
                        State = checkNumber.province,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless,
                        LrnLookup = checkNumber,
                        Portable = true
                    };

                    return portableNumber;
                }
                catch (Exception ex)
                {
                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");
                    Log.Fatal($"[Portability] {ex.Message}");
                    Log.Fatal($"[Portability] {ex.InnerException}");

                    return new PortedPhoneNumber
                    {
                        PortedDialedNumber = phoneNumber.DialedNumber,
                        Portable = false
                    };
                }
            }
            else
            {
                Log.Information($"[Portability] {number} is not Portable. Failed NPA, NXX, XXXX parsing.");

                return new PortedPhoneNumber
                {
                    PortedDialedNumber = number,
                    Portable = false
                };
            }
        }

        public async Task<PortedPhoneNumber[]> VerifyPortablityInBulkAsync(string[] numbers)
        {
            var numbersAndPortability = await LnpCheck.IsBulkPortableAsync(numbers, _teleToken).ConfigureAwait(false);

            return await Task.WhenAll(numbersAndPortability.Select(x => VerifyCnamLibdAsync(x.dialedNumber, x.Portable)));
        }

        public async Task<PortedPhoneNumber> VerifyCnamLibdAsync(string dialedNumber, bool portable)
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);

            if (checkParse && phoneNumber is not null)
            {
                try
                {
                    // Fail fast
                    if (portable is not true)
                    {
                        Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                        return new PortedPhoneNumber
                        {
                            PortedDialedNumber = phoneNumber.DialedNumber,
                            Portable = false
                        };
                    }

                    LrnBulkCnam checkNumber;

                    // Lookup the number.
                    if (phoneNumber.Type is NumberType.Canada)
                    {
                        var canada = await DataAccess.CallWithUs.LRNLookup.GetAsync(phoneNumber.DialedNumber, _callWithUsAPIkey).ConfigureAwait(false);

                        checkNumber = new LrnBulkCnam
                        {
                            lata = canada.LATA,
                            lrn = canada.LRN,
                            jurisdiction = canada.State,
                            ocn = canada.OCN,
                            ratecenter = canada.Ratecenter,
                            tn = $"1{phoneNumber.DialedNumber}",
                            lec = canada.Company,
                            lectype = canada.Prefix_Type,
                            city = canada.Ratecenter,
                            province = canada.State
                        };
                    }
                    else
                    {
                        checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _bulkVSKey).ConfigureAwait(false);
                    }

                    // Determine if the number is a wireless number.
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

                    var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber, _bulkVSKey);
                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName?.name;

                    // Log the lookup to the db.
                    var lookup = new PhoneNumberLookup(checkNumber);
                    var carrier = await Carrier.GetByOCNAsync(lookup.OCN, _postgresql).ConfigureAwait(false);
                    if (carrier is not null)
                    {
                        lookup.CarrierId = carrier.CarrierId;
                    }
                    var checkLog = await lookup.PostAsync(_postgresql).ConfigureAwait(false);

                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");

                    var portableNumber = new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = phoneNumber.DialedNumber,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = checkNumber.city,
                        State = checkNumber.province,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless,
                        LrnLookup = checkNumber,
                        Carrier = carrier,
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
                        PortedDialedNumber = dialedNumber,
                        Portable = false
                    };
                }
            }
            else
            {
                Log.Information($"[Portability] {dialedNumber} is not Portable. Failed NPA, NXX, XXXX parsing.");

                return new PortedPhoneNumber
                {
                    PortedDialedNumber = dialedNumber,
                    Portable = false
                };
            }
        }

        public async Task<LrnBulkCnam> InvestigateAsync(string number)
        {
            LrnBulkCnam checkNumber;

            if (number.IsCanadian())
            {
                var canada = await DataAccess.CallWithUs.LRNLookup.GetAsync(number, _callWithUsAPIkey).ConfigureAwait(false);

                checkNumber = new LrnBulkCnam
                {
                    lata = canada.LATA,
                    lrn = canada.LRN,
                    jurisdiction = canada.State,
                    ocn = canada.OCN,
                    ratecenter = canada.Ratecenter,
                    tn = number,
                    lec = canada.Company,
                    lectype = canada.Prefix_Type,
                };
            }
            else
            {
                checkNumber = await LrnBulkCnam.GetAsync(number, _bulkVSKey).ConfigureAwait(false);
            }

            var numberName = await CnamBulkVs.GetAsync(number, _bulkVSKey);
            checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName?.name;

            // Log the lookup to the db.
            var lookup = new PhoneNumberLookup(checkNumber);
            var carrier = await Carrier.GetByOCNAsync(lookup.OCN, _postgresql).ConfigureAwait(false);
            if (carrier is not null)
            {
                lookup.CarrierId = carrier.CarrierId;
            }
            _ = await lookup.PostAsync(_postgresql).ConfigureAwait(false);

            return checkNumber;
        }
    }
}