using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.TeleMesssage;
using NumberSearch.Mvc.Models;

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
        private readonly string _data247username;
        private readonly string _data247password;

        public LookupController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
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

                var results = new List<LrnBulkCnam>();

                foreach (var number in parsedNumbers)
                {
                    var checkNumber = await LrnBulkCnam.GetAsync(number, _bulkVSKey).ConfigureAwait(false);

                    //checkNumber.data.spid = bulkResult.spid;
                    //checkNumber.data.spid_name = bulkResult.lec;
                    //checkNumber.data.port_date = bulkResult.activation;

                    var numberName = new LIDBLookup();
                    try
                    {
                        numberName = await LIDBLookup.GetAsync(number, _data247username, _data247password).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Lookups] Failed to get LIBDName from Data 24/7 for number {number}.");
                        Log.Error(ex.Message);
                        Log.Error(ex.InnerException.ToString());
                    }

                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;
                    results.Add(checkNumber);
                }

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

                var cart = Cart.GetFromSession(HttpContext.Session);

                var portableNumbers = new List<PortedPhoneNumber>();
                var notPortable = new List<string>();
                var wirelessPortable = new List<PortedPhoneNumber>();

                foreach (var number in parsedNumbers)
                {
                    bool checkNpa = int.TryParse(number.Substring(0, 3), out int npa);
                    bool checkNxx = int.TryParse(number.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(number.Substring(6, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        try
                        {
                            var portable = await LnpCheck.IsPortable(number, _teleToken).ConfigureAwait(false);

                            // Lookup the number.
                            var checkNumber = await LrnBulkCnam.GetAsync(number, _bulkVSKey).ConfigureAwait(false);

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

                            var numberName = new LIDBLookup();
                            try
                            {
                                numberName = await LIDBLookup.GetAsync(number, _data247username, _data247password).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[Lookups] Failed to get LIBDName from Data 24/7 for number {number}.");
                                Log.Error(ex.Message);
                                Log.Error(ex.InnerException.ToString());
                            }

                            checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;

                            if (portable)
                            {
                                Log.Information($"[Portability] {number} is Portable.");

                                var portableNumber = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = number,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    City = "Unknown City",
                                    State = "Unknown State",
                                    DateIngested = DateTime.Now,
                                    IngestedFrom = "UserInput",
                                    Wireless = wireless,
                                    LrnLookup = checkNumber
                                };

                                // Separate wireless numbers out from the rest.
                                if (portableNumber.Wireless)
                                {
                                    wirelessPortable.Add(portableNumber);
                                }
                                else
                                {
                                    portableNumbers.Add(portableNumber);
                                }
                            }
                            else
                            {
                                Log.Information($"[Portability] {number} is not Portable.");

                                notPortable.Add(number);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"[Portability] {number} is not Portable.");
                            Log.Fatal($"[Portability] {ex.Message}");
                            Log.Fatal($"[Portability] {ex.InnerException}");

                            notPortable.Add(number);
                        }
                    }
                }

                // Only non-wireless numbers.
                if (!wirelessPortable.Any() && portableNumbers.Any())
                {
                    foreach (var portableNumber in portableNumbers)
                    {
                        var productOrder = new ProductOrder { PortedDialedNumber = portableNumber.PortedDialedNumber, Quantity = 1 };

                        var checkAdd = cart.AddPortedPhoneNumber(portableNumber, productOrder);
                    }

                    var checkSet = cart.SetToSession(HttpContext.Session);
                }
                // Only wireless numbers.
                else if (wirelessPortable.Any() && !portableNumbers.Any())
                {
                    foreach (var portableNumber in wirelessPortable)
                    {
                        var productOrder = new ProductOrder { PortedDialedNumber = portableNumber.PortedDialedNumber, Quantity = 1 };

                        var checkAdd = cart.AddPortedPhoneNumber(portableNumber, productOrder);
                    }

                    var checkSet = cart.SetToSession(HttpContext.Session);
                }

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
    }
}
