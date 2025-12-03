using FirstCom.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.Mvc.Models;

using PhoneNumbersNA;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Mvc.Controllers
{
    [ApiController]
    public class CartAPIController(MvcConfiguration mvcConfiguration) : ControllerBase
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DialedNumber"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="DateIngested"></param>
        /// <param name="Wireless"></param>
        /// <param name="Portable"></param>
        /// <param name="LastPorted"></param>
        /// <param name="SPID"></param>
        /// <param name="LATA"></param>
        /// <param name="LEC"></param>
        /// <param name="LECType"></param>
        /// <param name="LIDBName"></param>
        /// <param name="LRN"></param>
        /// <param name="OCN"></param>
        /// <param name="CarrierName"></param>
        /// <param name="CarrierLogoLink"></param>
        /// <param name="CarrierColor"></param>
        /// <param name="CarrierType"></param>
        public readonly record struct BulkLookupResult(string DialedNumber, string City, string State, DateTime DateIngested, bool Wireless, bool Portable, DateTime LastPorted, string SPID, string LATA, string LEC, string LECType, string LIDBName, string LRN, string OCN, string CarrierName, string CarrierLogoLink, string CarrierColor, string CarrierType);

        /// <summary>
        /// Get detailed information about a list of North American phone numbers.
        /// </summary>
        /// <param name="token">To get an API token please contact Accelerate Networks at 206-858-8757</param>
        /// <param name="dialedNumber">One or more valid phone number.</param>
        /// <returns>Detailed information on a per phone number basis.</returns>
        /// <response code="200">A list of phone number details. </response>
        /// <response code="400">No dialed phone numbers found. Please try a different query. 🥺👉👈</response>
        /// <response code="400">Token is invalid. Please supply the correct token in your request or contact support@acceleratenetworks.com for help.</response>
        [HttpGet("Number/Search/Bulk")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [OutputCache(Duration = 0)]
        [Produces<BulkLookupResult[]>]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> NumberSearchBulkAsync([Required] string token, [Required] string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(token) && token == "Memorable8142024")
            {
                // Add portable numbers to cart in bulk
                if (!string.IsNullOrWhiteSpace(dialedNumber))
                {
                    var parsedNumbers = dialedNumber.ExtractDialedNumbers().ToArray();

                    if (parsedNumbers.Length == 0)
                    {
                        return BadRequest("No dialed phone numbers found. Please try a different query. 🥺👉👈");
                    }

                    var results = new ConcurrentBag<PortedPhoneNumber>();
                    await Parallel.ForEachAsync(parsedNumbers, async (number, token) =>
                    {
                        var lookup = new LookupController(mvcConfiguration);
                        var result = await lookup.VerifyPortabilityAsync(number);
                        results.Add(result);
                    });

                    var lookups = new List<BulkLookupResult>(results.Count);
                    foreach(var number in results)
                    {
                        lookups.Add(new BulkLookupResult(number.PortedDialedNumber, number.City, number.State, number.DateIngested, number.Wireless, number.Portable, number.LrnLookup.LastPorted, number.LrnLookup.SPID, number.LrnLookup.LATA, number.LrnLookup.LEC, number.LrnLookup.LECType, number.LrnLookup.LIDBName, number.LrnLookup.LRN, number.LrnLookup.OCN, number.Carrier.Name, number.Carrier.LogoLink, number.Carrier.Color, number.Carrier.Type));
                    }

                    return Ok(lookups.ToArray());
                }
                else
                {
                    return BadRequest("No dialed phone numbers found. Please try a different query. 🥺👉👈");
                }
            }
            else
            {
                return BadRequest("Token is invalid. Please supply the correct token in your request or contact support@acceleratenetworks.com for help.");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/ExtensionRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientExtensionRegistrationAsync([FromRoute] Guid id, [FromBody] ExtensionRegistration registration)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await ExtensionRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.ExtensionRegistrationId == registration.ExtensionRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    registration.ExtensionRegistrationId = Guid.NewGuid();
                    registration.DateUpdated = DateTime.Now;
                    registration.NewClientId = id;

                    if (await registration.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/ExtensionRegistration/{extRegId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientExtensionRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid extRegId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await ExtensionRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.ExtensionRegistrationId == extRegId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An extension Registration with and Id of {extRegId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/NumberDescription")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientNumberDescriptionAsync([FromRoute] Guid id, [FromBody] NumberDescription description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await NumberDescription.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.NumberDescriptionId == description.NumberDescriptionId).FirstOrDefault();

                if (existing is null)
                {
                    description.NumberDescriptionId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    if (await description.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/NumberDescription/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientNumberDescriptionAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await NumberDescription.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.NumberDescriptionId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"A number description with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/IntercomRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientIntercomRegistrationAsync([FromRoute] Guid id, [FromBody] IntercomRegistration description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await IntercomRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.IntercomRegistrationId == description.IntercomRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    description.IntercomRegistrationId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    if (await description.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/IntercomRegistration/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientIntercomRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await IntercomRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.IntercomRegistrationId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Intercom Registration with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/SpeedDialKey")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientSpeedDialKeyAsync([FromRoute] Guid id, [FromBody] SpeedDialKey description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await SpeedDialKey.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.SpeedDialKeyId == description.SpeedDialKeyId).FirstOrDefault();

                if (existing is null)
                {
                    description.SpeedDialKeyId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    if (await description.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/SpeedDialKey/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientSpeedDialKeyAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await SpeedDialKey.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.SpeedDialKeyId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Speed Dial Key with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/FollowMeRegistration")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientFollowMeRegistrationAsync([FromRoute] Guid id, [FromBody] FollowMeRegistration description)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await FollowMeRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.FollowMeRegistrationId == description.FollowMeRegistrationId).FirstOrDefault();

                if (existing is null)
                {
                    description.FollowMeRegistrationId = Guid.NewGuid();
                    description.DateUpdated = DateTime.Now;
                    description.NewClientId = id;

                    if (await description.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/FollowMeRegistration/{numDesId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientFollowMeRegistrationAsync([FromRoute] Guid id, [FromRoute] Guid numDesId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await FollowMeRegistration.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.FollowMeRegistrationId == numDesId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Follow Me Registration with and Id of {numDesId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("Add/NewClient/{id}/PhoneMenuOption")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AddNewClientPhoneMenuOptionAsync([FromRoute] Guid id, [FromBody] PhoneMenuOption option)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await PhoneMenuOption.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.PhoneMenuOptionId == option.PhoneMenuOptionId).FirstOrDefault();

                if (existing is null)
                {
                    option.PhoneMenuOptionId = Guid.NewGuid();
                    option.DateUpdated = DateTime.Now;
                    option.NewClientId = id;

                    if (await option.PostAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("Remove/NewClient/{id}/PhoneMenuOption/{menuOptId}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RemoveNewClientPhoneMenuOptionAsync([FromRoute] Guid id, [FromRoute] Guid menuOptId)
        {
            var newClient = await NewClient.GetAsync(id, _postgresql);

            if (newClient is not null && newClient.NewClientId != Guid.Empty)
            {
                var extregs = await PhoneMenuOption.GetByNewClientAsync(id, _postgresql);

                var existing = extregs?.AsValueEnumerable().Where(x => x.PhoneMenuOptionId == menuOptId).FirstOrDefault();

                if (existing is null)
                {
                    return BadRequest($"An Phone Menu Option with and Id of {menuOptId} does not exist in the database.");
                }
                else
                {
                    if (await existing.DeleteAsync(_postgresql))
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

        [ApiExplorerSettings(IgnoreApi = true)]
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

            var cart = new CartServices(mvcConfiguration, HttpContext);

            switch (type)
            {
                case "PhoneNumber":
                    return await cart.BuyPhoneNumberAsync(id);
                case "PortedPhoneNumber":
                    return await cart.PortPhoneNumberAsync(id);
                case "VerifiedPhoneNumber":
                    return await cart.VerifyPhoneNumberAsync(id);
                case "Product":
                    var checkProduct = Guid.TryParse(id, out var productId);
                    if (checkProduct)
                    {
                        return await cart.BuyProductAsync(productId, quantity);
                    }
                    else
                    {
                        return BadRequest($"Product Id {id} can't be parsed as a GUID.");
                    }
                case "Service":
                    var checkService = Guid.TryParse(id, out var serviceId);
                    if (checkService)
                    {
                        return await cart.BuyServiceAsync(serviceId, quantity);
                    }
                    else
                    {
                        return BadRequest($"Service Id {id} can't be parsed as a GUID.");
                    }
                case "Coupon":
                    return await cart.AddCouponAsync(id, quantity);
                default:
                    return NotFound(ModelState);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
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

            var cart = new CartServices(mvcConfiguration, HttpContext);

            switch (type)
            {
                case "PhoneNumber":
                    return await cart.RemovePhoneNumberAsync(id);
                case "PortedPhoneNumber":
                    return await cart.RemovePortedPhoneNumberAsync(id);
                case "VerifiedPhoneNumber":
                    return await cart.RemoveVerifiedPhoneNumberAsync(id);
                case "Product":
                    var checkProduct = Guid.TryParse(id, out var productId);
                    if (checkProduct)
                    {
                        return await cart.RemoveProductAsync(productId);
                    }
                    else
                    {
                        return BadRequest($"Product Id {id} can't be parsed as a GUID.");
                    }
                case "Service":
                    var checkService = Guid.TryParse(id, out var serviceId);
                    if (checkService)
                    {
                        return await cart.RemoveServiceAsync(serviceId);
                    }
                    else
                    {
                        return BadRequest($"Service Id {id} can't be parsed as a GUID.");
                    }
                case "Coupon":
                    var checkCoupon = Guid.TryParse(id, out var couponId);
                    if (checkCoupon)
                    {
                        return await cart.RemoveCouponAsync(couponId);
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
    public class CartServices(MvcConfiguration mvcConfiguration, HttpContext httpContext) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly string _apiKey = mvcConfiguration.BulkVSAPIKEY;
        private readonly string _fpcusername = mvcConfiguration.PComNetUsername;
        private readonly string _fpcpassword = mvcConfiguration.PComNetPassword;
        private readonly string _bulkVSusername = mvcConfiguration.BulkVSUsername;
        private readonly string _bulkVSpassword = mvcConfiguration.BulkVSPassword;

        public async Task<IActionResult> BuyPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var phoneNumber = await DataAccess.Models.PhoneNumber.GetAsync(dialedPhoneNumber, _postgresql);
            var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), DialedNumber = phoneNumber.DialedNumber, Quantity = 1 };

            // Check that the number is still avalible from the provider.
            if (phoneNumber.IngestedFrom == "BulkVS")
            {
                var npanxx = $"{phoneNumber.NPA}{phoneNumber.NXX}";
                var doesItStillExist = await OrderTn.GetAsync(phoneNumber.NPA, phoneNumber.NXX, 0, _bulkVSusername.AsMemory(), _bulkVSpassword.AsMemory());
                var checkIfExists = doesItStillExist.AsValueEnumerable().Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (checkIfExists != null && checkIfExists?.DialedNumber == phoneNumber.DialedNumber)
                {
                    Log.Information("[BulkVS] Found {DialedNumber} in {Length} results returned for {npanxx}.", phoneNumber.DialedNumber, doesItStillExist.Length, npanxx);
                }
                else
                {
                    Log.Warning("[BulkVS] Failed to find {DialedNumber} in {Length} results returned for {npanxx}.", phoneNumber.DialedNumber, doesItStillExist.Length, npanxx);

                    var purchaseOrder = new PurchasedPhoneNumber
                    {
                        Completed = false,
                        DateIngested = phoneNumber.DateIngested,
                        DateOrdered = DateTime.Now,
                        DialedNumber = phoneNumber.DialedNumber,
                        IngestedFrom = phoneNumber.IngestedFrom,
                        NPA = phoneNumber.NPA,
                        NumberType = phoneNumber.NumberType,
                        NXX = phoneNumber.NXX,
                        PIN = "",
                        OrderId = Guid.NewGuid(),
                        OrderResponse = "Failed to add to Cart.",
                        PurchasedPhoneNumberId = Guid.NewGuid(),
                        XXXX = phoneNumber.XXXX
                    };

                    _ = purchaseOrder.PostAsync(_postgresql);

                    // Remove numbers that are unpurchasable.
                    _ = await phoneNumber.DeleteAsync(_postgresql);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }

            }
            else if (phoneNumber.IngestedFrom == "FirstPointCom")
            {
                // Verify that tele has the number.
                var results = await FirstPointCom.GetPhoneNumbersByNpaNxxAsync(phoneNumber.NPA, phoneNumber.NXX, string.Empty.AsMemory(), _fpcusername.AsMemory(), _fpcpassword.AsMemory());
                var matchingNumber = results.AsValueEnumerable().Where(x => x.DialedNumber == phoneNumber.DialedNumber).FirstOrDefault();
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    Log.Information("[FirstPointCom] Found {DialedNumber} in {Length} results returned for {NPA}, {NXX}.", phoneNumber.DialedNumber, results.Length, phoneNumber.NPA, phoneNumber.NXX);
                }
                else
                {
                    Log.Warning("[FirstPointCom] Failed to find {DialedNumber} in {Length} results returned for {NPA}, {NXX}.", phoneNumber.DialedNumber, results.Length, phoneNumber.NPA, phoneNumber.NXX);

                    var purchaseOrder = new PurchasedPhoneNumber
                    {
                        Completed = false,
                        DateIngested = phoneNumber.DateIngested,
                        DateOrdered = DateTime.Now,
                        DialedNumber = phoneNumber.DialedNumber,
                        IngestedFrom = phoneNumber.IngestedFrom,
                        NPA = phoneNumber.NPA,
                        NumberType = phoneNumber.NumberType,
                        NXX = phoneNumber.NXX,
                        PIN = "",
                        OrderId = Guid.NewGuid(),
                        OrderResponse = "Failed to add to Cart.",
                        PurchasedPhoneNumberId = Guid.NewGuid(),
                        XXXX = phoneNumber.XXXX
                    };

                    _ = purchaseOrder.PostAsync(_postgresql);

                    // Remove numbers that are unpurchasable.
                    _ = await phoneNumber.DeleteAsync(_postgresql);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }
            }
            else if (phoneNumber.IngestedFrom == "OwnedNumber")
            {
                // Verify that we still have the number.
                var matchingNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(phoneNumber.DialedNumber, _postgresql);
                if (matchingNumber != null && matchingNumber?.DialedNumber == phoneNumber.DialedNumber)
                {
                    Log.Information("[OwnedNumber] Found {DialedNumber}.", phoneNumber.DialedNumber);
                }
                else
                {
                    Log.Warning("[OwnedNumber] Failed to find {DialedNumber}.", phoneNumber.DialedNumber);

                    var purchaseOrder = new PurchasedPhoneNumber
                    {
                        Completed = false,
                        DateIngested = phoneNumber.DateIngested,
                        DateOrdered = DateTime.Now,
                        DialedNumber = phoneNumber.DialedNumber,
                        IngestedFrom = phoneNumber.IngestedFrom,
                        NPA = phoneNumber.NPA,
                        NumberType = phoneNumber.NumberType,
                        NXX = phoneNumber.NXX,
                        PIN = "",
                        OrderId = Guid.NewGuid(),
                        OrderResponse = "Failed to add to Cart.",
                        PurchasedPhoneNumberId = Guid.NewGuid(),
                        XXXX = phoneNumber.XXXX
                    };

                    _ = purchaseOrder.PostAsync(_postgresql);

                    // Remove numbers that are unpurchasable.
                    _ = await phoneNumber.DeleteAsync(_postgresql);

                    // Sadly its gone. And the user needs to pick a different number.
                    return BadRequest($"{dialedPhoneNumber} is no longer available.");
                }
            }
            else
            {
                var purchaseOrder = new PurchasedPhoneNumber
                {
                    Completed = false,
                    DateIngested = phoneNumber.DateIngested,
                    DateOrdered = DateTime.Now,
                    DialedNumber = phoneNumber.DialedNumber,
                    IngestedFrom = phoneNumber.IngestedFrom,
                    NPA = phoneNumber.NPA,
                    NumberType = phoneNumber.NumberType,
                    NXX = phoneNumber.NXX,
                    PIN = "",
                    OrderId = Guid.NewGuid(),
                    OrderResponse = "Failed to add to Cart.",
                    PurchasedPhoneNumberId = Guid.NewGuid(),
                    XXXX = phoneNumber.XXXX
                };

                _ = purchaseOrder.PostAsync(_postgresql);

                // Remove numbers that are unpurchasable.
                _ = await phoneNumber.DeleteAsync(_postgresql);

                // Sadly its gone. And the user needs to pick a different number.
                return BadRequest($"{dialedPhoneNumber} is no longer available.");
            }

            // Prevent a duplicate order.
            if (phoneNumber.Purchased)
            {
                // Remove numbers that are unpurchasable.
                _ = await phoneNumber.DeleteAsync(_postgresql);

                // Sadly its gone. And the user needs to pick a different number.
                return BadRequest($"{dialedPhoneNumber} is no longer available.");
            }

            await httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(httpContext.Session);
            var checkAdd = cart.AddPhoneNumber(ref phoneNumber, ref productOrder);
            var checkSet = cart.SetToSession(httpContext.Session);

            return Ok(dialedPhoneNumber);
        }

        public async Task<IActionResult> PortPhoneNumberAsync(string dialedPhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var portedPhoneNumber = new PortedPhoneNumber();

            if (PhoneNumbersNA.PhoneNumber.TryParse(dialedPhoneNumber, out var phoneNumber))
            {
                // Determine if the number is a wireless number.
                var lrnLookup = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber.AsMemory(), _apiKey.AsMemory());

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

                Log.Information("[AddToCart] {DialedNumber} has an OCN Type of {lectype}.", phoneNumber.DialedNumber, lrnLookup.lectype);

                portedPhoneNumber = new PortedPhoneNumber
                {
                    PortedPhoneNumberId = Guid.NewGuid(),
                    PortedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                    NPA = phoneNumber.NPA,
                    NXX = phoneNumber.NXX,
                    XXXX = phoneNumber.XXXX,
                    City = lrnLookup.city ?? string.Empty,
                    State = lrnLookup.province ?? string.Empty,
                    DateIngested = DateTime.Now,
                    IngestedFrom = "UserInput",
                    Wireless = wireless
                };

            }
            else
            {
                return BadRequest($"Failed to add {dialedPhoneNumber} to your cart.");
            }

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            // Prevent the user from adding ported numbers that are both wireless and not wireless to the same order.
            if (cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Count != 0)
            {
                var wirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == true);
                var nonWirelessCount = cart.PortedPhoneNumbers.Count(x => x.Wireless == false);

                if (wirelessCount > 0 && portedPhoneNumber.Wireless)
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

            var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = portedPhoneNumber.PortedDialedNumber, PortedPhoneNumberId = portedPhoneNumber?.PortedPhoneNumberId, Quantity = 1 };

            if (cart.AddPortedPhoneNumber(ref portedPhoneNumber!, ref productOrder) && cart.SetToSession(httpContext.Session))
            {
                return Ok(portedPhoneNumber!.PortedDialedNumber);
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

            if (PhoneNumbersNA.PhoneNumber.TryParse(dialedPhoneNumber, out var phoneNumber))
            {
                try
                {
                    // Determine if the number is a wireless number.
                    var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber.AsMemory(), _apiKey.AsMemory());

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

                    var checkLong = long.TryParse(checkNumber.activation.ToString(), out var timeInSeconds);

                    var verifiedPhoneNumber = new VerifiedPhoneNumber
                    {
                        VerifiedPhoneNumberId = Guid.NewGuid(),
                        VerifiedDialedNumber = phoneNumber.DialedNumber ?? string.Empty,
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
                        Activation = checkNumber.activation?.ToString() ?? string.Empty,
                        LIDBName = checkNumber.LIDBName,
                        LastPorted = checkLong ? new DateTime(1970, 1, 1).AddSeconds(timeInSeconds) : DateTime.Now,
                        DateToExpire = DateTime.Now.AddYears(1)
                    };

                    var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), VerifiedPhoneNumberId = verifiedPhoneNumber.VerifiedPhoneNumberId, Quantity = 1 };

                    await httpContext.Session.LoadAsync();
                    var cart = Cart.GetFromSession(httpContext.Session);

                    if (cart.AddVerifiedPhoneNumber(ref verifiedPhoneNumber, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            var product = await Product.GetByIdAsync(productId, _postgresql);
            if (product is not null)
            {
                var productOrder = new ProductOrder
                {
                    ProductOrderId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Quantity = Quantity > 0 ? Quantity : 1
                };

                await httpContext.Session.LoadAsync();
                var cart = Cart.GetFromSession(httpContext.Session);
                if (cart.AddProduct(ref product, ref productOrder) && cart.SetToSession(httpContext.Session))
                {
                    return Ok(productId.ToString());
                }
            }
            return BadRequest($"Failed to purchase product {productId}.");
        }

        public async Task<IActionResult> BuyServiceAsync(Guid serviceId, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = await Service.GetAsync(serviceId, _postgresql);
            if (service is not null)
            {
                var productOrder = new ProductOrder
                {
                    ProductOrderId = Guid.NewGuid(),
                    ServiceId = service.ServiceId,
                    Quantity = Quantity > 0 ? Quantity : 1
                };

                await httpContext.Session.LoadAsync();
                var cart = Cart.GetFromSession(httpContext.Session);
                var checkAdd = cart.AddService(ref service, ref productOrder);

                var stdSeat = new Guid("16e2c639-445b-4ae6-9925-07300318206b");
                var concurrentSeat = new Guid("48eb4627-8692-4a3b-8be1-be64bbeea534");

                // Add required E911 fees to the order for seats and lines.
                if (service.ServiceId == concurrentSeat || service.ServiceId == stdSeat)
                {
                    var e911Id = new Guid("1b3ae0e0-e308-4f99-88e1-b9c220bc02d5");
                    var e911fee = await Service.GetAsync(e911Id, _postgresql);
                    if (e911fee is not null)
                    {
                        var e911ProductOrder = cart.ProductOrders?.AsValueEnumerable().Where(x => x.ServiceId == e911Id).FirstOrDefault();
                        // If there are already E911 fees in the cart.
                        if (e911ProductOrder is not null)
                        {
                            // See how many total lines and seats there are.
                            if (productOrder.Quantity != e911ProductOrder.Quantity)
                            {
                                var totalE911FeeItems = 0;

                                var lines = cart.Services?.AsValueEnumerable().Where(x => x.ServiceId == stdSeat).FirstOrDefault();

                                if (lines is not null)
                                {
                                    var lineQuantity = cart.ProductOrders?.AsValueEnumerable().Where(x => x.ServiceId == lines.ServiceId).FirstOrDefault();
                                    totalE911FeeItems += lineQuantity?.Quantity ?? 0;
                                }

                                var seats = cart.Services?.AsValueEnumerable().Where(x => x.ServiceId == concurrentSeat).FirstOrDefault();

                                if (seats is not null)
                                {
                                    var seatsQuantity = cart.ProductOrders?.AsValueEnumerable().Where(x => x.ServiceId == seats.ServiceId).FirstOrDefault();
                                    totalE911FeeItems += seatsQuantity?.Quantity ?? 0;
                                }

                                e911ProductOrder = new ProductOrder
                                {
                                    ProductOrderId = Guid.NewGuid(),
                                    ServiceId = e911Id,
                                    Quantity = totalE911FeeItems > 0 ? totalE911FeeItems : 1
                                };
                                checkAdd = cart.AddService(ref e911fee, ref e911ProductOrder);
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
                            checkAdd = cart.AddService(ref e911fee, ref e911ProductOrder);
                        }
                    }

                }
                _ = cart.SetToSession(httpContext.Session);
            }

            return Ok(serviceId.ToString());
        }

        public async Task<IActionResult> AddCouponAsync(string couponName, int Quantity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Remove leading and trailing whitespace and convert to lowercase.
            var input = couponName.Trim().ToLowerInvariant();

            // Drop everything that's not a letter or number.
            input = new string([.. input.AsValueEnumerable().Where(c => char.IsLetterOrDigit(c))]);

            var coupons = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

            var coupon = coupons.FirstOrDefault(x => x.Name.Replace(" ", string.Empty).Contains(input, StringComparison.InvariantCultureIgnoreCase));

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

                await httpContext.Session.LoadAsync();
                var cart = Cart.GetFromSession(httpContext.Session);

                if (coupon.Type == "Install" && cart.Products is not null && cart.Products.Count != 0)
                {
                    _ = cart.AddCoupon(ref coupon, ref productOrder);
                    _ = cart.SetToSession(httpContext.Session);

                    return Ok(couponName.ToString());
                }
                else if (coupon.Type == "Port" && cart.PortedPhoneNumbers is not null && cart.PortedPhoneNumbers.Count != 0)
                {
                    _ = cart.AddCoupon(ref coupon, ref productOrder);
                    _ = cart.SetToSession(httpContext.Session);

                    return Ok(couponName.ToString());
                }
                else if (coupon.Type == "Number" && cart.PhoneNumbers is not null && cart.PhoneNumbers.Count != 0)
                {
                    _ = cart.AddCoupon(ref coupon, ref productOrder);
                    _ = cart.SetToSession(httpContext.Session);

                    return Ok(couponName.ToString());
                }
                else if (coupon.Type == "Service" && cart.Services is not null && cart.Services.Count != 0)
                {
                    _ = cart.AddCoupon(ref coupon, ref productOrder);
                    _ = cart.SetToSession(httpContext.Session);

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

            var phoneNumber = new DataAccess.Models.PhoneNumber { DialedNumber = dialedPhoneNumber };
            var productOrder = new ProductOrder { DialedNumber = dialedPhoneNumber };

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            if (cart.RemovePhoneNumber(ref phoneNumber, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            var portedPhoneNumber = cart.PortedPhoneNumbers?.AsValueEnumerable().Where(x => x.PortedDialedNumber == dialedPhoneNumber).FirstOrDefault();

            if (portedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders?.AsValueEnumerable().Where(x => x.PortedPhoneNumberId == portedPhoneNumber.PortedPhoneNumberId).FirstOrDefault();

                productOrder ??= new ProductOrder { PortedDialedNumber = portedPhoneNumber.PortedDialedNumber ?? string.Empty, PortedPhoneNumberId = portedPhoneNumber?.PortedPhoneNumberId, Quantity = 1 };

                if (cart.RemovePortedPhoneNumber(ref portedPhoneNumber!, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            var verifedPhoneNumber = cart.VerifiedPhoneNumbers?.AsValueEnumerable().Where(x => x.VerifiedDialedNumber == dialedPhoneNumber).FirstOrDefault();
            if (verifedPhoneNumber is not null)
            {
                var productOrder = cart.ProductOrders?.AsValueEnumerable().Where(x => x.VerifiedPhoneNumberId == verifedPhoneNumber.VerifiedPhoneNumberId).FirstOrDefault();
                productOrder ??= new ProductOrder { VerifiedPhoneNumberId = verifedPhoneNumber.VerifiedPhoneNumberId, Quantity = 1 };

                if (cart.RemoveVerifiedPhoneNumber(ref verifedPhoneNumber, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            await httpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(httpContext.Session);

            if (cart.RemoveProduct(ref product, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            if (cart.RemoveService(ref service, ref productOrder) && cart.SetToSession(httpContext.Session))
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

            await httpContext.Session.LoadAsync();
            var cart = Cart.GetFromSession(httpContext.Session);

            if (cart.RemoveCoupon(ref coupon, ref productOrder) && cart.SetToSession(httpContext.Session))
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