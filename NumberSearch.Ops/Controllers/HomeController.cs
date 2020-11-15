using BulkVS;

using CsvHelper;

using FirstCom;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleMesssage;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;
        private readonly string _username;
        private readonly string _password;
        private readonly Guid _teleToken;
        private readonly string _bulkVSAPIKey;
        private readonly string _bulkVSAPISecret;
        private readonly string _invoiceNinjaToken;
        private readonly string _data247username;
        private readonly string _data247password;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _username = config.GetConnectionString("PComNetUsername");
            _password = config.GetConnectionString("PComNetPassword");
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
            _bulkVSAPISecret = config.GetConnectionString("BulkVSAPISecret");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }


        [Authorize]
        [Route("/")]
        [Route("/Home/Order/")]
        [Route("/Home/Order/{orderId}")]
        public async Task<IActionResult> Orders(Guid? orderId)
        {
            if (orderId is null)
            {
                // Show all orders
                var orders = await Order.GetAllAsync(_postgresql).ConfigureAwait(false);
                var productOrders = await ProductOrder.GetAllAsync(_postgresql).ConfigureAwait(false);
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var services = await Service.GetAllAsync(_postgresql).ConfigureAwait(false);
                var pairs = new List<OrderProducts>();

                foreach (var order in orders)
                {
                    var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();

                    pairs.Add(new OrderProducts
                    {
                        Order = order,
                        ProductOrders = orderProductOrders
                    });
                }

                return View("Orders", new OrderResult
                {
                    Orders = pairs,
                    Products = products,
                    Services = services
                });
            }
            else
            {
                var order = await Order.GetByIdAsync(orderId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("OrderEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/Order/{orderId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderUpdate(Order order)
        {
            if (order is null)
            {
                return Redirect("/Home/Order");
            }
            else
            {
                order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                return View("OrderEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/NumberOrders")]
        [Route("/Home/NumberOrders/{dialedNumber}")]
        public async Task<IActionResult> NumberOrders(string dialedNumber)
        {

            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
            else
            {
                var order = await PurchasedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);

                return View("NumberOrders", new List<PurchasedPhoneNumber> { order });
            }
        }

        [Authorize]
        [Route("/Home/EmergencyInformation")]
        [Route("/Home/EmergencyInformation/{dialedNumber}")]
        public async Task<IActionResult> AllEmergencyInformation(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var info = await EmergencyInformation.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("EmergencyInformation", info.OrderByDescending(x => x.DateIngested));
            }
            else
            {
                var info = await EmergencyInformation.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("EmergencyInformationEdit", info.FirstOrDefault());
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers")]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        public async Task<IActionResult> OwnedNumbers(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await OwnedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("OwnedNumbers", orders.OrderByDescending(x => x.DialedNumber));
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnedNumberUpdate(OwnedPhoneNumber number)
        {
            if (number is null)
            {
                return Redirect("/Home/OwnedNumbers");
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                order.Notes = number.Notes;
                order.OwnedBy = number.OwnedBy;
                order.BillingClientId = number.BillingClientId;
                order.Active = number.Active;
                order.SPID = order.SPID;
                order.SPIDName = order.SPIDName;

                var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        public async Task<IActionResult> ExportNumberOrders()
        {
            var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

            var filePath = Path.GetFullPath(Path.Combine("wwwroot", "csv"));
            var fileName = $"PurchasedNumbers{DateTime.Now.ToString("yyyyMMdd")}.csv";
            var completePath = Path.Combine(filePath, fileName);

            using var writer = new StreamWriter(completePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(orders).ConfigureAwait(false);
            var file = new FileInfo(completePath);

            if (file.Exists)
            {
                return Redirect($"../csv/{file.Name}");
            }
            else
            {
                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
        }

        [Authorize]
        [Route("/Home/PortRequests")]
        [Route("/Home/PortRequests/{orderId}")]
        public async Task<IActionResult> PortRequests(Guid? orderId)
        {
            if (orderId is not null && orderId.HasValue)
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                // Show all orders
                var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("PortRequests", portRequests.OrderByDescending(x => x.DateSubmitted));
            }
        }

        [Authorize]
        [HttpGet]
        [Route("/Home/TaxRates")]
        [Route("/Home/TaxRates/{taxRateId}")]
        public async Task<IActionResult> TaxRates(int? taxRateId)
        {
            if (taxRateId != null && taxRateId > 0)
            {
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = new TaxRate
                    {
                        data = result.data.Where(x => x.id == taxRateId).ToArray()
                    }
                });
            }
            else
            {
                // Show all orders
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = result
                }
                );
            }
        }

        [Authorize]
        [Route("/Home/TaxRates")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaxRatesCreate(TaxRateResult location)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.Zip))
            {
                return Redirect("/Home/TaxRates");
            }
            else
            {
                try
                {
                    var specificTaxRate = await SalesTax.GetAsync(location.Address, location.City, location.Zip).ConfigureAwait(false);

                    var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                    var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                    var taxRateValue = specificTaxRate.rate1 * 100M;

                    var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);
                    var billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();
                    if (billingTaxRate is null)
                    {
                        billingTaxRate = new TaxRateDatum
                        {
                            name = taxRateName,
                            rate = taxRateValue
                        };

                        var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        return View("TaxRates", new TaxRateResult
                        {
                            Address = location.Address ?? string.Empty,
                            City = location.City ?? string.Empty,
                            Zip = location.Zip ?? string.Empty,
                            Rates = result,
                            Message = $"{taxRateName} has been created."
                        });
                    }
                    else
                    {
                        var unchanged = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                        return View("TaxRates", new TaxRateResult
                        {
                            Address = location.Address ?? string.Empty,
                            City = location.City ?? string.Empty,
                            Zip = location.Zip ?? string.Empty,
                            Rates = unchanged,
                            Message = $"{taxRateName} already exists."
                        });
                    }
                }
                catch
                {
                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {location.Address}, {location.City}, {location.Zip}.");

                    var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                    return View("TaxRates", new TaxRateResult
                    {
                        Address = location.Address ?? string.Empty,
                        City = location.City ?? string.Empty,
                        Zip = location.Zip ?? string.Empty,
                        Rates = result,
                        Message = $"Failed to create a Tax Rate for {location.Address}, {location.City}, {location.Zip}."
                    });
                }
            }
        }

        [Route("/Home/PortRequests/{orderId}/{dialedNumber}")]
        public async Task<IActionResult> RemovePortedPhoneNumber(Guid? orderId, string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var numberToRemove = numbers.Where(x => x.PortedDialedNumber == dialedNumber).FirstOrDefault();

                if (numberToRemove is not null)
                {
                    var checkDelete = await numberToRemove.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                    }
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                return Redirect("/Home/PortRequests");
            }
        }

        [Authorize]
        [Route("/Home/PortRequests/{orderId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestUpdate(PortRequestResult result, Guid? orderId, string dialedNumber)
        {
            var portRequest = result?.PortRequest ?? null;

            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var Query = dialedNumber;
                // Clean up the query.
                Query = Query?.Trim();

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
                    // This is disabled so as to avoid taking a dependancy on the Mvc project.
                    //else if (char.IsLetter(letter))
                    //{
                    //    converted.Add(SearchController.LetterToKeypadDigit(letter));
                    //}
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
                            var portable = await LnpCheck.IsPortable(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                            // Determine if the number is a wireless number.
                            var lrnLookup = await LrnLookup.GetAsync(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                            bool wireless = false;

                            switch (lrnLookup.data.ocn_type)
                            {
                                case "wireless":
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

                            // Lookup the number.
                            var checkNumber = await LrnLookup.GetAsync(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                            var numberName = await LIDBLookup.GetAsync(dialedPhoneNumber, _data247username, _data247password).ConfigureAwait(false);

                            checkNumber.data.DialedNumber = dialedPhoneNumber;
                            checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;

                            if (portable)
                            {
                                Log.Information($"[Portability] {dialedPhoneNumber} is Portable.");

                                var port = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = dialedPhoneNumber,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    City = "Unknown City",
                                    State = "Unknown State",
                                    DateIngested = DateTime.Now,
                                    IngestedFrom = "UserInput",
                                    Wireless = wireless,
                                    LrnLookup = checkNumber,
                                    OrderId = order.OrderId,
                                    PortRequestId = portRequest.PortRequestId,
                                };

                                var checkSave = await port.PostAsync(_postgresql).ConfigureAwait(false);

                                if (checkSave)
                                {
                                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                var port = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = dialedPhoneNumber,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    City = "Unknown City",
                                    State = "Unknown State",
                                    DateIngested = DateTime.Now,
                                    IngestedFrom = "UserInput",
                                    Wireless = wireless,
                                    LrnLookup = checkNumber,
                                    OrderId = order.OrderId,
                                    PortRequestId = portRequest.PortRequestId,
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Fatal($"[Portability] {ex.Message}");

                            var port = new PortedPhoneNumber
                            {
                                PortedDialedNumber = dialedPhoneNumber,
                                NPA = npa,
                                NXX = nxx,
                                XXXX = xxxx,
                                City = "Unknown City",
                                State = "Unknown State",
                                DateIngested = DateTime.Now,
                                IngestedFrom = "UserInput"
                            };
                        }
                    }
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else if (portRequest is null)
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                var fromDb = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                portRequest.PortRequestId = fromDb.PortRequestId;

                var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                portRequest = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
        }

        [Authorize]
        [Route("/Home/PortRequestsTeli/{orderId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestSendToTeli(string OrderId)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(Guid.Parse(OrderId), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                try
                {
                    var teliResponse = await LnpCreate.GetAsync(portRequest, numbers, _teleToken).ConfigureAwait(false);
                    portRequest.TeliId = teliResponse.data.id;
                    portRequest.DateSubmitted = DateTime.Now;
                    var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);
                }
                catch
                {
                    Log.Fatal($"[PortRequest] Failed to submit port request to Teli.");
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
        }

        [Authorize]
        public async Task<IActionResult> Tests(string testName, string npa, string nxx, string dialedNumber)
        {
            if (testName == "DIDInventorySearchAsync" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx) || !string.IsNullOrWhiteSpace(dialedNumber)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                dialedNumber ??= string.Empty;

                var results = await NpaNxxFirstPointCom.GetAsync(npa, nxx, dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    NPA = npa,
                    NXX = nxx,
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = results
                });
            }

            if (testName == "DIDOrderAsync" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var results = await FirstPointComOrderPhoneNumber.PostAsync(dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumberOrder = results
                });
            }

            if (testName == "LRNLookup" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LrnLookup.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    LRNLookup = checkNumber
                });
            }

            if (testName == "didslist" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await DidsList.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = checkNumber
                });
            }

            if (testName == "lnpcheck" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LnpCheck.GetRawAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PortabilityResponse = checkNumber
                });
            }

            if (testName == "DnSearchNpaNxx" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;

                var checkNumber = await NpaNxxBulkVS.GetAsync(npa + nxx, _bulkVSAPIKey, _bulkVSAPISecret).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PhoneNumbersBVS = checkNumber
                });
            }

            return View("Tests");
        }

        [Authorize]
        [Route("/Home/Emails")]
        [Route("/Home/Emails/{orderId}")]
        public async Task<IActionResult> Emails(Guid? orderId)
        {
            if (orderId != null && orderId.HasValue)
            {
                var emails = await Email.GetByOrderAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
            else
            {
                var emails = await Email.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
        }

        [Authorize]
        public async Task<IActionResult> Ingests(int cycle, string ingestedFrom, string enabled, string runNow)
        {
            var ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);

            if (cycle > 0 && cycle < 24 && !string.IsNullOrWhiteSpace(ingestedFrom) && (enabled == "Enabled" || enabled == "Disabled"))
            {
                var update = ingests.Where(x => x.IngestedFrom == ingestedFrom).FirstOrDefault();

                if (update != null)
                {
                    update.CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now;
                    update.Enabled = enabled == "Enabled";
                    update.RunNow = runNow == "true";
                    update.LastUpdate = DateTime.Now;

                    var checkUpdate = await update.PutAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
                else
                {
                    update = new IngestCycle
                    {
                        CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now,
                        IngestedFrom = ingestedFrom,
                        Enabled = enabled == "Enabled",
                        RunNow = runNow == "true",
                        LastUpdate = DateTime.Now
                    };

                    var checkCreate = await update.PostAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
            }

            return View("IngestConfiguration", ingests);
        }

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [Authorize]
        [Route("Numbers/{Query}")]
        [Route("Numbers/")]
        public async Task<IActionResult> Numbers(string query, int page = 1)
        {
            // Fail fast
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Numbers");
            }

            // Clean up the query.
            query = query?.Trim();

            // Parse the query.
            var converted = new List<char>();
            foreach (var letter in query)
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
                    converted.Add(LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            var results = await PhoneNumber.SequentialPaginatedSearchAsync(new string(converted.ToArray()), page, _postgresql).ConfigureAwait(false);
            var count = await PhoneNumber.NumberOfResultsInQuery(new string(converted.ToArray()), _postgresql).ConfigureAwait(false);

            return View("Numbers", new SearchResults
            {
                CleanQuery = new string(converted.ToArray()),
                NumberOfResults = count,
                Page = page,
                PhoneNumbers = results.ToArray(),
                Query = query
            });
        }

        public static char LetterToKeypadDigit(char letter)
        {
            // Map the chars to their keypad numerical values.
            switch (letter)
            {
                case '+':
                    return '0';
                case 'a':
                case 'b':
                case 'c':
                    return '2';
                case 'd':
                case 'e':
                case 'f':
                    return '3';
                case 'g':
                case 'h':
                case 'i':
                    return '4';
                case 'j':
                case 'k':
                case 'l':
                    return '5';
                case 'm':
                case 'n':
                case 'o':
                    return '6';
                case 'p':
                case 'q':
                case 'r':
                case 's':
                    return '7';
                case 't':
                case 'u':
                case 'v':
                    return '8';
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                    return '9';
                default:
                    // The digit 1 isn't mapped to any chars on a phone keypad.
                    // If the char isn't mapped to anything, respect it's existence by mapping it to a wildcard.
                    return '*';
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
