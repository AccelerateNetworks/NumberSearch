using AccelerateNetworks.Operations;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Models;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class MessagingController : Controller
    {
        private readonly numberSearchContext _context;
        private readonly OpsConfig _config;
        private readonly string _baseUrl;
        private readonly string _messagingToken;

        public MessagingController(numberSearchContext context, OpsConfig opsConfig)
        {
            _context = context;
            _config = opsConfig;
            _baseUrl = opsConfig.MessagingURL;
            _messagingToken = opsConfig.MessagingAPIJWT;
        }

        [Authorize]
        [Route("/Messaging")]
        [Route("/Messaging/Index")]
        public async Task<IActionResult> IndexAsync()
        {
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View(new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers});
        }

        [Authorize]
        [Route("/Messaging/RefreshStatus")]
        public async Task<IActionResult> RefreshStatusAsync(string dialedNumber)
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers });
        }
    }
}
