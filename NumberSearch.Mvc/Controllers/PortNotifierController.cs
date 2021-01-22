using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PortNotifierController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string _postgresql;
        private readonly string _data247username;
        private readonly string _data247password;
        private readonly string _bulkVSAPIKey;

        public PortNotifierController(IConfiguration config)
        {
            configuration = config;
            _postgresql = configuration.GetConnectionString("PostgresqlProd");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
            _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(string Query)
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            // Clean up the query.
            Query = Query?.Trim();

            if (Query is null || Query.Length == 0)
            {
                return View("Index");
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

                        var numberName = await LIDBLookup.GetAsync(dialedPhoneNumber, _data247username, _data247password).ConfigureAwait(false);

                        checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;

                        var checkLong = long.TryParse(checkNumber.activation, out var timeInSeconds);

                        var verify = new VerifiedPhoneNumber
                        {
                            VerifiedDialedNumber = checkNumber.tn.Substring(1),
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
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
                            Activation = checkNumber.activation,
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
                            VerifiedDialedNumber = dialedPhoneNumber,
                            NPA = npa,
                            NXX = nxx,
                            XXXX = xxxx,
                            City = "Unknown City",
                            DateIngested = DateTime.Now,
                            IngestedFrom = "UserInput"
                        };

                        Log.Error($"[VerifedNumber] Failed to find number {dialedPhoneNumber}");
                        Log.Error(ex.Message);
                        Log.Error(ex.InnerException.Message);

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
