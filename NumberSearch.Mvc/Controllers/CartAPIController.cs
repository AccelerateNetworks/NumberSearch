using FirstCom;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [Route("api")]
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
        [HttpGet("add/{dialedPhoneNumber}")]
        public async Task<IActionResult> AddToCartAsync([FromRoute] string dialedPhoneNumber)
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

        [HttpGet("remove/{dialedPhoneNumber}")]
        public async Task<IActionResult> RemoveFromCartAsync([FromRoute] string dialedPhoneNumber)
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
    }
}