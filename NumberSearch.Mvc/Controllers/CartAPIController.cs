using FirstCom;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [Route("Cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fpcusername;
        private readonly string _fpcpassword;
        private readonly string _invoiceNinjaToken;
        private readonly string _emailOrders;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _data247username;
        private readonly string _data247password;

        public CartAPIController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _apiKey = config.GetConnectionString("BulkVSAPIKEY");
            _apiSecret = config.GetConnectionString("BulkVSAPISecret");
            _bulkVSusername = config.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = config.GetConnectionString("BulkVSPassword");
            _fpcusername = config.GetConnectionString("PComNetUsername");
            _fpcpassword = config.GetConnectionString("PComNetPassword");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _emailOrders = config.GetConnectionString("EmailOrders");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
        }

        // GET: api/Games/5
        [HttpGet("PhoneNumber/Add/{dialedPhoneNumber}")]
        public async Task<IActionResult> BuyPhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder { DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

            var purchasable = false;

            // Check that the number is still avalible from the provider.
            if (phoneNumber.IngestedFrom == "BulkVS")
            {
                var npanxx = $"{phoneNumber.NPA}{phoneNumber.NXX}";
                var doesItStillExist = await OrderTn.GetAsync(phoneNumber.NPA, phoneNumber.NXX, _bulkVSusername, _bulkVSpassword).ConfigureAwait(false);
                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[BulkVS] Found {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {npanxx}.");
                }
                else
                {
                    Log.Warning($"[BulkVS] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {npanxx}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }

            }
            else if (phoneNumber.IngestedFrom == "TeleMessage")
            {
                // Verify that tele has the number.
                var doesItStillExist = await DidsList.GetAsync(phoneNumber.DialedNumber, _teleToken).ConfigureAwait(false);
                var checkIfExists = doesItStillExist.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[TeleMessage] Found {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {phoneNumber.DialedNumber}.");
                }
                else
                {
                    Log.Warning($"[TeleMessage] Failed to find {phoneNumber.DialedNumber} in {doesItStillExist.Count()} results returned for {phoneNumber.DialedNumber}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }

            }
            else if (phoneNumber.IngestedFrom == "FirstPointCom")
            {
                // Verify that tele has the number.
                var results = await NpaNxxFirstPointCom.GetAsync(phoneNumber.NPA.ToString(new CultureInfo("en-US")), phoneNumber.NXX.ToString(new CultureInfo("en-US")), string.Empty, _fpcusername, _fpcpassword).ConfigureAwait(false);
                var matchingNumber = results.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[FirstPointCom] Found {phoneNumber.DialedNumber} in {results.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");
                }
                else
                {
                    Log.Warning($"[FirstPointCom] Failed to find {phoneNumber.DialedNumber} in {results.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }
            }
            else
            {
                // Remove numbers that are unpurchasable.
                var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                // Sadly its gone. And the user needs to pick a different number.
                return BadRequest($"{dialedPhoneNumber} is no longer available.");
            }

            // Prevent a duplicate order.
            if (phoneNumber.Purchased)
            {
                purchasable = false;
            }

            if (!purchasable)
            {
                // Remove numbers that are unpurchasable.
                var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                // Sadly its gone. And the user needs to pick a different number.
                return BadRequest($"{dialedPhoneNumber} is no longer available.");
            }

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkAdd = cart.AddPhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok(dialedPhoneNumber);
        }

        [HttpGet("PortedPhoneNumber/Add/{dialedPhoneNumber}")]
        public async Task<IActionResult> PortPhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var portedPhoneNumber = new PortedPhoneNumber();

            if (!string.IsNullOrWhiteSpace(dialedPhoneNumber) && dialedPhoneNumber.Length == 10)
            {
                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                // Determine if the number is a wireless number.
                var lrnLookup = await LrnBulkCnam.GetAsync(dialedPhoneNumber, _apiKey).ConfigureAwait(false);

                bool wireless = false;

                switch (lrnLookup.lectype)
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

                Log.Information($"[AddToCart] {dialedPhoneNumber} has an OCN Type of {lrnLookup.lectype}.");

                if (checkNpa && checkNxx && checkXxxx)
                {
                    portedPhoneNumber = new PortedPhoneNumber
                    {
                        PortedPhoneNumberId = Guid.NewGuid(),
                        PortedDialedNumber = dialedPhoneNumber,
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        City = lrnLookup?.city,
                        State = lrnLookup?.province,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "UserInput",
                        Wireless = wireless
                    };
                }
                else
                {
                    return BadRequest($"Failed to add {dialedPhoneNumber} to your cart.");
                }
            }

            var cart = Cart.GetFromSession(HttpContext.Session);

            // Prevent the user from adding ported numbers that are both wireless and not wireless to the same order.
            if (cart.PortedPhoneNumbers.Any())
            {
                var wirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == true);
                var nonWirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == false);

                if (wirelessCount > 0 && !portedPhoneNumber.Wireless)
                {
                    // Tell the user about the failure
                    return BadRequest("This phone number cannot be added to an order that already has wireless numbers in it. Please create a separate order for non-wireless numbers.");
                }

                if (nonWirelessCount > 0 && portedPhoneNumber.Wireless)
                {
                    // Tell the user about the failure
                    return BadRequest("This wireless phone number cannot be added to an order that already has non-wireless numbers in it. Please create a separate order for wireless numbers.");
                }
            }

            var productOrder = new ProductOrder { PortedDialedNumber = portedPhoneNumber?.PortedDialedNumber, PortedPhoneNumberId = portedPhoneNumber.PortedPhoneNumberId, Quantity = 1 };

            var checkAdd = cart.AddPortedPhoneNumber(portedPhoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            if (checkAdd && checkSet)
            {
                return Ok(portedPhoneNumber.Wireless ? $"Sucessfully added wireless phone number {portedPhoneNumber.PortedDialedNumber} to your cart!" : $"Sucessfully added {dialedPhoneNumber} to your cart!");
            }
            else
            {
                return BadRequest($"Failed to add {dialedPhoneNumber} to your cart.");
            }
        }

        [HttpGet("VerifiedPhoneNumber/Add/{dialedPhoneNumber}")]
        public async Task<IActionResult> VerifyPhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!string.IsNullOrWhiteSpace(dialedPhoneNumber) && dialedPhoneNumber.Length == 10)
            {
                bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    try
                    {
                        // Determine if the number is a wireless number.
                        var checkNumber = await LrnBulkCnam.GetAsync(dialedPhoneNumber, _apiKey).ConfigureAwait(false);

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

                        var verifiedPhoneNumber = new VerifiedPhoneNumber
                        {
                            VerifiedPhoneNumberId = Guid.NewGuid(),
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

                        var productOrder = new ProductOrder { VerifiedPhoneNumberId = verifiedPhoneNumber.VerifiedPhoneNumberId, Quantity = 1 };

                        var cart = Cart.GetFromSession(HttpContext.Session);
                        var checkAdd = cart.AddVerifiedPhoneNumber(verifiedPhoneNumber, productOrder);
                        var checkSet = cart.SetToSession(HttpContext.Session);

                        if (checkAdd && checkSet)
                        {
                            return Ok(dialedPhoneNumber);
                        }
                        else
                        {
                            return BadRequest($"Failed to verify phone number {dialedPhoneNumber}. :(");
                        }
                    }
                    catch
                    {
                        return BadRequest($"Failed to verify phone number {dialedPhoneNumber}. :(");
                    }
                }
                else
                {
                    return BadRequest($"Failed to verify phone number {dialedPhoneNumber}. :(");
                }
            }
            else
            {
                return BadRequest($"Failed to verify phone number {dialedPhoneNumber}. :(");
            }
        }

        [HttpGet("Product/Add/{productId}")]
        public async Task<IActionResult> BuyProductAsync([FromRoute] Guid productId, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await Product.GetAsync(productId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ProductId = product.ProductId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkAdd = cart.AddProduct(product, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok();
        }

        [HttpGet("Service/Add/{serviceId}")]
        public async Task<IActionResult> BuyServiceAsync([FromRoute] Guid serviceId, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = await Service.GetAsync(serviceId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ServiceId = service.ServiceId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkAdd = cart.AddService(service, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok();
        }

        [HttpGet("PhoneNumber/Remove/{dialedPhoneNumber}")]
        public async Task<IActionResult> RemovePhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var phoneNumber = new PhoneNumber { DialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { DialedNumber = dialedPhoneNumber };

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkRemove = cart.RemovePhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok(dialedPhoneNumber);
        }

        [HttpGet("PortedPhoneNumber/Remove/{dialedPhoneNumber}")]
        public async Task<IActionResult> RemovePortedPhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = Cart.GetFromSession(HttpContext.Session);

            var portedPhoneNumber = cart.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == dialedPhoneNumber).FirstOrDefault();

            if (portedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders.Where(x => x.PortedPhoneNumberId == portedPhoneNumber.PortedPhoneNumberId).FirstOrDefault();

                if (productOrder is not null)
                {
                    var checkRemove = cart.RemovePortedPhoneNumber(portedPhoneNumber, productOrder);
                    var checkSet = cart.SetToSession(HttpContext.Session);

                    return Ok(dialedPhoneNumber);
                }
                else
                {
                    // TODO: Tell the user about the failure.
                    return NotFound(dialedPhoneNumber);
                }
            }
            else
            {
                // TODO: Tell the user about the failure.
                return NotFound(dialedPhoneNumber);
            }
        }

        [HttpGet("VerifiedPhoneNumber/Remove/{dialedPhoneNumber}")]
        public async Task<IActionResult> RemoveVerifiedPhoneNumberAsync([FromRoute] string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = Cart.GetFromSession(HttpContext.Session);

            var verifedPhoneNumber = cart.VerifiedPhoneNumbers.Where(x => x.VerifiedDialedNumber == dialedPhoneNumber).FirstOrDefault();
            if (verifedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders.Where(x => x.VerifiedPhoneNumberId == verifedPhoneNumber.VerifiedPhoneNumberId).FirstOrDefault();
                if (productOrder is not null)
                {
                    var checkRemove = cart.RemoveVerifiedPhoneNumber(verifedPhoneNumber, productOrder);
                    var checkSet = cart.SetToSession(HttpContext.Session);

                    return Ok(dialedPhoneNumber);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("Product/Remove/{productId}")]
        public async Task<IActionResult> RemoveProductAsync([FromRoute] Guid productId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Product { ProductId = productId };
            var productOrder = new ProductOrder { ProductId = productId };

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkRemove = cart.RemoveProduct(product, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok();
        }

        [HttpGet("Service/Remove/{serviceId}")]
        public async Task<IActionResult> RemoveServiceAsync([FromRoute] Guid serviceId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = new Service { ServiceId = serviceId };
            var productOrder = new ProductOrder { ServiceId = serviceId };

            var cart = Cart.GetFromSession(HttpContext.Session);
            var checkRemove = cart.RemoveService(service, productOrder);
            var checkSet = cart.SetToSession(HttpContext.Session);

            return Ok();
        }
    }
}