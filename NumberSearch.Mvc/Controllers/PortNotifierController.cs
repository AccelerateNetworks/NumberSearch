using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PortNotifierController : Controller
    {
        private readonly string _bulkVSAPIKey;

        public PortNotifierController(MvcConfiguration mvcConfiguration)
        {
            _bulkVSAPIKey = mvcConfiguration.BulkVSAPIKEY;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (Query is null || Query.Length == 0)
            {
                return View("Index");
            }

            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(Query, out var phoneNumber);

            if (checkParse && phoneNumber is not null)
            {
                try
                {
                    // Determine if the number is a wireless number.
                    var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber ?? string.Empty, _bulkVSAPIKey).ConfigureAwait(false);

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

                    var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber ?? string.Empty, _bulkVSAPIKey);
                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName.name;

                    var checkLong = long.TryParse(checkNumber.activation.ToString(), out var timeInSeconds);

                    var verify = new VerifiedPhoneNumber
                    {
                        VerifiedDialedNumber = checkNumber.tn[1..],
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        IngestedFrom = "BulkVS",
                        DateIngested = DateTime.Now,
                        OrderId = Guid.Empty,
                        Wireless = wireless,
                        NumberType = "Standard",
                        LocalRoutingNumber = checkNumber.lrn,
                        OperatingCompanyNumber = checkNumber.ocn,
                        City = checkNumber.city,
                        LocalAccessTransportArea = checkNumber.lata,
                        RateCenter = checkNumber.ratecenter,
                        Province = checkNumber.province,
                        Jurisdiction = checkNumber.jurisdiction,
                        Local = checkNumber.local,
                        LocalExchangeCarrier = checkNumber.lec,
                        LocalExchangeCarrierType = checkNumber.lectype,
                        ServiceProfileIdentifier = checkNumber.spid,
                        Activation = checkNumber.activation.ToString(),
                        LIDBName = checkNumber.LIDBName,
                        LastPorted = checkLong ? new DateTime(1970, 1, 1).AddSeconds(timeInSeconds) : DateTime.Now,
                        DateToExpire = DateTime.Now.AddYears(1)
                    };

                    return View("Index", new PortNotifierResults
                    {
                        VerifiedPhoneNumber = verify,
                        Cart = cart,
                        Message = wireless ? "This is wireless phone number." : "This is not a wireless phone number."
                    });

                }
                catch (Exception ex)
                {
                    var verified = new VerifiedPhoneNumber
                    {
                        VerifiedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        City = "Unknown City",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput"
                    };

                    Log.Error($"[VerifedNumber] Failed to find number {phoneNumber.DialedNumber}");
                    Log.Error(ex.Message);
                    Log.Error(ex.InnerException?.Message ?? "No innner exception found.");

                    return View("Index", new PortNotifierResults
                    {
                        VerifiedPhoneNumber = verified,
                        Cart = cart,
                        Message = ex.Message
                    });
                }
            }
            else
            {
                return View("Index", new PortNotifierResults
                {
                    VerifiedPhoneNumber = new VerifiedPhoneNumber(),
                    Cart = cart
                });
            }
        }
    }
}
