using FirstCom;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Call48;
using NumberSearch.DataAccess.Peerless;
using NumberSearch.DataAccess.TeliMesssage;

using Serilog;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public CartAPIController(IConfiguration config)
        {
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        [HttpPost("Add/NewClient/{id}/ExtensionRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientExtensionRegistrationAsync([FromRoute] Guid id, [FromBody] ExtensionRegistration registration)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await ExtensionRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.ExtensionRegistrationId == registration.ExtensionRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    registration.ExtensionRegistrationId = Guid.NewGuid();
                    registration.DateUpdated = DateTime.Now;
                    registration.NewClientId = id;

                    var checkSubmit = await registration.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(registration.ExtensionRegistrationId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the extension Registration to the database.");
                    }
                }
                else
                {
                    return BadRequest("An extension Registration with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/ExtensionRegistration/{extRegId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientExtensionRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid extRegId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await ExtensionRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.ExtensionRegistrationId == extRegId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An extension Registration with and Id of {extRegId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted extension Registration {extRegId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete extension Registration {extRegId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpPost("Add/NewClient/{id}/NumberDescription")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientNumberDescriptionAsync([FromRoute] Guid id, [FromBody] NumberDescription description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await NumberDescription.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.NumberDescriptionId == description.NumberDescriptionId).FirstOrDefault();

                if (existing is null)
                {
                    description.NumberDescriptionId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    var checkSubmit = await description.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(description.NumberDescriptionId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the number Description to the database.");
                    }
                }
                else
                {
                    return BadRequest("A number description with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/NumberDescription/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientNumberDescriptionAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await NumberDescription.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.NumberDescriptionId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"A number description with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted number description {numDesId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete number description {numDesId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpPost("Add/NewClient/{id}/IntercomRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientIntercomRegistrationAsync([FromRoute] Guid id, [FromBody] IntercomRegistration description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await IntercomRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.IntercomRegistrationId == description.IntercomRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    description.IntercomRegistrationId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    var checkSubmit = await description.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(description.IntercomRegistrationId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the Intercom Registration to the database.");
                    }
                }
                else
                {
                    return BadRequest("An Intercom Registration with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/IntercomRegistration/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientIntercomRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await IntercomRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.IntercomRegistrationId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Intercom Registration with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted Intercom Registration {numDesId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete Intercom Registration {numDesId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpPost("Add/NewClient/{id}/SpeedDialKey")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientSpeedDialKeyAsync([FromRoute] Guid id, [FromBody] SpeedDialKey description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await SpeedDialKey.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.SpeedDialKeyId == description.SpeedDialKeyId).FirstOrDefault();

                if (existing is null)
                {
                    description.SpeedDialKeyId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    var checkSubmit = await description.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(description.SpeedDialKeyId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the Speed Dial Key to the database.");
                    }
                }
                else
                {
                    return BadRequest("An Speed Dial Key with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/SpeedDialKey/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientSpeedDialKeyAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await SpeedDialKey.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.SpeedDialKeyId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Speed Dial Key with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted Speed Dial Key {numDesId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete Speed Dial Key {numDesId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpPost("Add/NewClient/{id}/FollowMeRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientFollowMeRegistrationAsync([FromRoute] Guid id, [FromBody] FollowMeRegistration description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await FollowMeRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.FollowMeRegistrationId == description.FollowMeRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    description.FollowMeRegistrationId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    var checkSubmit = await description.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(description.FollowMeRegistrationId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the Follow Me Registration to the database.");
                    }
                }
                else
                {
                    return BadRequest("An Follow Me Registration with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/FollowMeRegistration/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientFollowMeRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await FollowMeRegistration.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.FollowMeRegistrationId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Follow Me Registration with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted Follow Me Registration {numDesId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete Follow Me Registration {numDesId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }


        [HttpPost("Add/NewClient/{id}/PhoneMenuOption")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientPhoneMenuOptionAsync([FromRoute] Guid id, [FromBody] PhoneMenuOption option)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await PhoneMenuOption.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.PhoneMenuOptionId == option.PhoneMenuOptionId).FirstOrDefault();

                if (existing is null)
                {
                    option.PhoneMenuOptionId = Guid.NewGuid();
                    option.DateUpdated = DateTime.Now;
                    option.NewClientId = id;

                    var checkSubmit = await option.PostAsync(_postgresql).ConfigureAwait(false);

                    if (checkSubmit)
                    {
                        return Ok(option.PhoneMenuOptionId);
                    }
                    else
                    {
                        return BadRequest($"Failed to save the Phone Menu Option to the database.");
                    }
                }
                else
                {
                    return BadRequest("An Phone Menu Option with this Id already exists in the database.");
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Remove/NewClient/{id}/PhoneMenuOption/{menuOptId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientPhoneMenuOptionAsync([FromRoute] Guid id, [FromRoute] Guid menuOptId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql).ConfigureAwait(false);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await PhoneMenuOption.GetByNewClientAsync(id, _postgresql).ConfigureAwait(false);

                var existing = extregs?.Where(x => x.PhoneMenuOptionId == menuOptId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Phone Menu Option with and Id of {menuOptId} does not exist in the database.");
                }
                else
                {
                    var checkDelete = await existing.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        return Ok($"Deleted Phone Menu Option {menuOptId} from the database.");
                    }
                    else
                    {
                        return BadRequest($"Failed to delete Phone Menu Option {menuOptId} from the database.");
                    }
                }
            }

            return BadRequest("Couldn't find a NewClient with this id.");
        }

        [HttpGet("Cart/Add/{type}/{id}/{quantity}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddToCartAsync([FromRoute] string type, [FromRoute] string id, [FromRoute] int quantity)
        {
            if (!ModelState.IsValid && !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ModelState);
            }

            if (quantity < 1)
            {
                return BadRequest($"The product quantity is less than 1.");
            }

            var cart = new CartServices(_configuration, HttpContext);

            switch (type)
            {
                case "PhoneNumber":
                    return await cart.BuyPhoneNumberAsync(id).ConfigureAwait(false);
                case "PortedPhoneNumber":
                    return await cart.PortPhoneNumberAsync(id).ConfigureAwait(false);
                case "VerifiedPhoneNumber":
                    return await cart.VerifyPhoneNumberAsync(id).ConfigureAwait(false);
                case "Product":
                    var checkProduct = Guid.TryParse(id, out var productId);
                    if (checkProduct)
                    {
                        return await cart.BuyProductAsync(productId, quantity).ConfigureAwait(false);
                    }
                    else
                    {
                        return BadRequest($"Product Id {id} can't be parsed as a GUID.");
                    }
                case "Service":
                    var checkService = Guid.TryParse(id, out var serviceId);
                    if (checkService)
                    {
                        return await cart.BuyServiceAsync(serviceId, quantity).ConfigureAwait(false);
                    }
                    else
                    {
                        return BadRequest($"Service Id {id} can't be parsed as a GUID.");
                    }
                case "Coupon":
                    return await cart.AddCouponAsync(id, quantity).ConfigureAwait(false);
                default:
                    return NotFound(ModelState);
            }
        }

        [HttpGet("Cart/Remove/{type}/{id}/{quantity}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveFromCartAsync([FromRoute] string type, [FromRoute] string id, [FromRoute] int quantity)
        {
            if (!ModelState.IsValid && !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ModelState);
            }

            if (quantity < 1)
            {
                return BadRequest($"The product quantity is less than 1.");
            }

            var cart = new CartServices(_configuration, HttpContext);

            switch (type)
            {
                case "PhoneNumber":
                    return await cart.RemovePhoneNumberAsync(id).ConfigureAwait(false);
                case "PortedPhoneNumber":
                    return await cart.RemovePortedPhoneNumberAsync(id).ConfigureAwait(false);
                case "VerifiedPhoneNumber":
                    return await cart.RemoveVerifiedPhoneNumberAsync(id).ConfigureAwait(false);
                case "Product":
                    var checkProduct = Guid.TryParse(id, out var productId);
                    if (checkProduct)
                    {
                        return await cart.RemoveProductAsync(productId).ConfigureAwait(false);
                    }
                    else
                    {
                        return BadRequest($"Product Id {id} can't be parsed as a GUID.");
                    }
                case "Service":
                    var checkService = Guid.TryParse(id, out var serviceId);
                    if (checkService)
                    {
                        return await cart.RemoveServiceAsync(serviceId).ConfigureAwait(false);
                    }
                    else
                    {
                        return BadRequest($"Service Id {id} can't be parsed as a GUID.");
                    }
                case "Coupon":
                    var checkCoupon = Guid.TryParse(id, out var couponId);
                    if (checkCoupon)
                    {
                        return await cart.RemoveCouponAsync(couponId).ConfigureAwait(false);
                    }
                    else
                    {
                        return BadRequest($"Coupon Id {id} can't be parsed as a GUID.");
                    }
                default:
                    return NotFound(ModelState);
            }
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartServices : Controller
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
        private readonly string _call48Username;
        private readonly string _call48Password;
        private readonly string _peerlessApiKey;
        private HttpContext _httpContext;

        public CartServices(IConfiguration config, HttpContext httpContext)
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
            _call48Username = config.GetConnectionString("Call48Username");
            _call48Password = config.GetConnectionString("Call48Password");
            _peerlessApiKey = config.GetConnectionString("PeerlessAPIKey");
            _httpContext = httpContext;
        }

        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var phoneNumber = await PhoneNumber.GetAsync(dialedPhoneNumber, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

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
            else if (phoneNumber.IngestedFrom == "Call48")
            {
                // Verify that Call48 has the number.
                var credentials = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);
                var results = await Search.GetLocalNumbersAsync(phoneNumber.State, string.Empty, phoneNumber.NPA.ToString(), phoneNumber.NXX.ToString(), credentials.data.token).ConfigureAwait(false);
                var matchingNumber = results.data.result.Where(x => x.did_number.Replace("-", string.Empty) == phoneNumber.DialedNumber).FirstOrDefault();
                if (matchingNumber != null && matchingNumber?.did_number.Replace("-", string.Empty) == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[Call48] Found {phoneNumber.DialedNumber} in {results?.data?.result?.Length} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");
                }
                else
                {
                    Log.Warning($"[Call48] Failed to find {phoneNumber.DialedNumber} in {results?.data?.result?.Length} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }
            }
            else if (phoneNumber.IngestedFrom == "Peerless")
            {
                // Verify that Peerless has the number.
                var numbers = await DidFind.GetByDialedNumberAsync(phoneNumber.NPA.ToString("000"), phoneNumber.NXX.ToString("000"), phoneNumber.XXXX.ToString("0000"), _peerlessApiKey);
                // Sometimes Call48 includes dashes in their numbers for no reason.
                var matchingNumber = numbers.Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[Peerless] Found {phoneNumber.DialedNumber} in {numbers.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}, {phoneNumber.XXXX}.");
                }
                else
                {
                    Log.Warning($"[Peerless] Failed to find {phoneNumber.DialedNumber} in {numbers.Count()} results returned for {phoneNumber.NPA}, {phoneNumber.NXX}, {phoneNumber.XXXX}.");

                    // Remove numbers that are unpurchasable.
                    var checkRemove = await phoneNumber.DeleteAsync(_postgresql).ConfigureAwait(false);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }
            }
            else if (phoneNumber.IngestedFrom == "OwnedNumber")
            {
                // Verify that we still have the number.
                var matchingNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(phoneNumber.DialedNumber, _postgresql).ConfigureAwait(false);
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    purchasable = true;
                    Log.Information($"[OwnedNumber] Found {phoneNumber.DialedNumber}.");
                }
                else
                {
                    Log.Warning($"[OwnedNumber] Failed to find {phoneNumber.DialedNumber}.");

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

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkAdd = cart.AddPhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            return Ok(dialedPhoneNumber);
        }

        public async Task<IActionResult> PortPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var portedPhoneNumber = new PortedPhoneNumber();

            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedPhoneNumber, out var phoneNumber);

            if (checkParse)
            {
                // Determine if the number is a wireless number.
                var lrnLookup = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _apiKey).ConfigureAwait(false);

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

                Log.Information($"[AddToCart] {phoneNumber.DialedNumber} has an OCN Type of {lrnLookup.lectype}.");

                portedPhoneNumber = new PortedPhoneNumber
                {
                    PortedPhoneNumberId = Guid.NewGuid(),
                    PortedDialedNumber = phoneNumber.DialedNumber,
                    NPA = phoneNumber.NPA,
                    NXX = phoneNumber.NXX,
                    XXXX = phoneNumber.XXXX,
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

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);

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

            var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = portedPhoneNumber?.PortedDialedNumber, PortedPhoneNumberId = portedPhoneNumber.PortedPhoneNumberId, Quantity = 1 };

            var checkAdd = cart.AddPortedPhoneNumber(portedPhoneNumber, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkAdd && checkSet)
            {
                return Ok(portedPhoneNumber.PortedDialedNumber);
            }
            else
            {
                return BadRequest($"Failed to add {dialedPhoneNumber} to your cart.");
            }
        }

        public async Task<IActionResult> VerifyPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedPhoneNumber, out var phoneNumber);

            if (checkParse)
            {
                try
                {
                    // Determine if the number is a wireless number.
                    var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _apiKey).ConfigureAwait(false);

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

                    var checkLong = long.TryParse(checkNumber.activation, out var timeInSeconds);

                    var verifiedPhoneNumber = new VerifiedPhoneNumber
                    {
                        VerifiedPhoneNumberId = Guid.NewGuid(),
                        VerifiedDialedNumber = phoneNumber.DialedNumber,
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
                        Activation = checkNumber.activation,
                        LIDBName = checkNumber.LIDBName,
                        LastPorted = checkLong ? new DateTime(1970, 1, 1).AddSeconds(timeInSeconds) : DateTime.Now,
                        DateToExpire = DateTime.Now.AddYears(1)
                    };

                    var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), VerifiedPhoneNumberId = verifiedPhoneNumber.VerifiedPhoneNumberId, Quantity = 1 };

                    await _httpContext.Session.LoadAsync().ConfigureAwait(false);
                    var cart = Cart.GetFromSession(_httpContext.Session);
                    var checkAdd = cart.AddVerifiedPhoneNumber(verifiedPhoneNumber, productOrder);
                    var checkSet = cart.SetToSession(_httpContext.Session);

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

        public async Task<IActionResult> BuyProductAsync(Guid productId, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await Product.GetByIdAsync(productId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ProductOrderId = Guid.NewGuid(),
                ProductId = product.ProductId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkAdd = cart.AddProduct(product, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkAdd && checkSet)
            {
                return Ok(productId.ToString());
            }
            else
            {
                return BadRequest($"Failed to purchase product {productId}.");
            }
        }

        public async Task<IActionResult> BuyServiceAsync(Guid serviceId, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = await Service.GetAsync(serviceId, _postgresql).ConfigureAwait(false);
            var productOrder = new ProductOrder
            {
                ProductOrderId = Guid.NewGuid(),
                ServiceId = service.ServiceId,
                Quantity = Quantity > 0 ? Quantity : 1
            };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkAdd = cart.AddService(service, productOrder);

            var stdSeat = new Guid("16e2c639-445b-4ae6-9925-07300318206b");
            var concurrentSeat = new Guid("48eb4627-8692-4a3b-8be1-be64bbeea534");

            // Add required E911 fees to the order for seats and lines.
            if (service.ServiceId == concurrentSeat || service.ServiceId == stdSeat)
            {
                var e911Id = new Guid("1b3ae0e0-e308-4f99-88e1-b9c220bc02d5");
                var e911fee = await Service.GetAsync(e911Id, _postgresql).ConfigureAwait(false);
                var e911ProductOrder = cart.ProductOrders.Where(x => x.ServiceId == e911Id).FirstOrDefault();
                // If there are already E911 fees in the cart.
                if (e911ProductOrder is not null)
                {
                    // See how many total lines and seats there are.
                    if (productOrder.Quantity != e911ProductOrder.Quantity)
                    {
                        var totalE911FeeItems = 0;

                        var lines = cart.Services.Where(x => x.ServiceId == stdSeat).FirstOrDefault();

                        if (lines is not null)
                        {
                            var lineQuantity = cart.ProductOrders.Where(x => x.ServiceId == lines.ServiceId).FirstOrDefault();
                            totalE911FeeItems += lineQuantity.Quantity;
                        }

                        var seats = cart.Services.Where(x => x.ServiceId == concurrentSeat).FirstOrDefault();

                        if (seats is not null)
                        {
                            var seatsQuantity = cart.ProductOrders.Where(x => x.ServiceId == seats.ServiceId).FirstOrDefault();
                            totalE911FeeItems += seatsQuantity.Quantity;
                        }

                        e911ProductOrder = new ProductOrder
                        {
                            ProductOrderId = Guid.NewGuid(),
                            ServiceId = e911Id,
                            Quantity = totalE911FeeItems > 0 ? totalE911FeeItems : 1
                        };
                        checkAdd = cart.AddService(e911fee, e911ProductOrder);
                    }
                }
                else
                {
                    e911ProductOrder = new ProductOrder
                    {
                        ProductOrderId = Guid.NewGuid(),
                        ServiceId = e911Id,
                        Quantity = Quantity > 0 ? Quantity : 1
                    };
                    checkAdd = cart.AddService(e911fee, e911ProductOrder);
                }
            }

            var checkSet = cart.SetToSession(_httpContext.Session);

            return Ok(serviceId.ToString());
        }

        public async Task<IActionResult> AddCouponAsync(string couponName, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            couponName = couponName.Trim();

            var coupons = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

            var coupon = coupons.Where(x => x.Name.Replace(" ", string.Empty).ToLowerInvariant() == couponName.Replace(" ", string.Empty).ToLowerInvariant()).FirstOrDefault();

            if (coupon is null)
            {
                return BadRequest("Coupon not found.");
            }
            else
            {
                var productOrder = new ProductOrder
                {
                    ProductOrderId = Guid.NewGuid(),
                    CouponId = coupon.CouponId,
                    Quantity = Quantity > 0 ? Quantity : 1
                };

                await _httpContext.Session.LoadAsync().ConfigureAwait(false);
                var cart = Cart.GetFromSession(_httpContext.Session);

                if (coupon.Type == "Install" && cart.Products.Any())
                {
                    var checkAdd = cart.AddCoupon(coupon, productOrder);
                    var checkSet = cart.SetToSession(_httpContext.Session);

                    return Ok(couponName.ToString());
                }
                else if (coupon.Type == "Port" && cart.PortedPhoneNumbers.Any())
                {
                    var checkAdd = cart.AddCoupon(coupon, productOrder);
                    var checkSet = cart.SetToSession(_httpContext.Session);

                    return Ok(couponName.ToString());
                }
                else
                {
                    return BadRequest("This coupon cannot be applied to this order.");
                }
            }
        }

        public async Task<IActionResult> RemovePhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var phoneNumber = new PhoneNumber { DialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { DialedNumber = dialedPhoneNumber };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkRemove = cart.RemovePhoneNumber(phoneNumber, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkRemove && checkSet)
            {
                return Ok(dialedPhoneNumber);
            }
            else
            {
                return NotFound(dialedPhoneNumber);
            }
        }

        public async Task<IActionResult> RemovePortedPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);

            var portedPhoneNumber = cart.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == dialedPhoneNumber).FirstOrDefault();

            if (portedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders.Where(x => x.PortedPhoneNumberId == portedPhoneNumber.PortedPhoneNumberId).FirstOrDefault();

                var newProductOrder = new ProductOrder { PortedDialedNumber = portedPhoneNumber?.PortedDialedNumber, PortedPhoneNumberId = portedPhoneNumber.PortedPhoneNumberId, Quantity = 1 };

                var checkRemove = cart.RemovePortedPhoneNumber(portedPhoneNumber, productOrder ?? newProductOrder);
                var checkSet = cart.SetToSession(_httpContext.Session);

                if (checkRemove && checkSet)
                {
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

        public async Task<IActionResult> RemoveVerifiedPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);

            var verifedPhoneNumber = cart.VerifiedPhoneNumbers.Where(x => x.VerifiedDialedNumber == dialedPhoneNumber).FirstOrDefault();
            if (verifedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders.Where(x => x.VerifiedPhoneNumberId == verifedPhoneNumber.VerifiedPhoneNumberId).FirstOrDefault();
                var newProductOrder = new ProductOrder { VerifiedPhoneNumberId = verifedPhoneNumber.VerifiedPhoneNumberId, Quantity = 1 };

                var checkRemove = cart.RemoveVerifiedPhoneNumber(verifedPhoneNumber, productOrder ?? newProductOrder);
                var checkSet = cart.SetToSession(_httpContext.Session);

                if (checkRemove && checkSet)
                {
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

        public async Task<IActionResult> RemoveProductAsync(Guid productId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Product { ProductId = productId };
            var productOrder = new ProductOrder { ProductId = productId };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkRemove = cart.RemoveProduct(product, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkRemove && checkSet)
            {
                return Ok(productId.ToString());
            }
            else
            {
                return NotFound(productId.ToString());
            }
        }

        public async Task<IActionResult> RemoveServiceAsync(Guid serviceId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = new Service { ServiceId = serviceId };
            var productOrder = new ProductOrder { ServiceId = serviceId };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkRemove = cart.RemoveService(service, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkRemove && checkSet)
            {
                return Ok(serviceId.ToString());
            }
            else
            {
                return NotFound(serviceId.ToString());
            }
        }

        public async Task<IActionResult> RemoveCouponAsync(Guid couponId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var coupon = new Coupon { CouponId = couponId };
            var productOrder = new ProductOrder { CouponId = couponId };

            await _httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(_httpContext.Session);
            var checkRemove = cart.RemoveCoupon(coupon, productOrder);
            var checkSet = cart.SetToSession(_httpContext.Session);

            if (checkRemove && checkSet)
            {
                return Ok(couponId.ToString());
            }
            else
            {
                return NotFound(couponId.ToString());
            }
        }
    }
}