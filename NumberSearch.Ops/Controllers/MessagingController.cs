using AccelerateNetworks.Operations;

using Amazon.S3.Model;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.EntityFrameworkCore;

using Models;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Reflection.Metadata;
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
            bool refresh = false;
            foreach (var number in stats)
            {
                if (number.RegisteredUpstream is false && number.AsDialed is not "Total")
                {
                    bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.AsDialed, out var phoneNumber);
                    if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
                    {
                        _ = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetStringAsync();
                        refresh = true;
                    }
                }
            }

            if (refresh)
            {
                stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            }

            return View(new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers });
        }

        [Authorize]
        [Route("/Messaging/RefreshStatus")]
        public async Task<IActionResult> RefreshStatusAsync(string dialedNumber)
        {
            string message = string.Empty;
            string alertType = "alert-success";
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
                message = $"🔃 Upstream Status {refresh.UpstreamStatusDescription} for {refresh.AsDialed} routed to {refresh.CallbackUrl}";
            }
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers, Message = message, AlertType = alertType });
        }

        [Authorize]
        [Route("/Messaging/Register")]
        public async Task<IActionResult> RegisterAsync(string dialedNumber)
        {
            var result = new MessagingResult { AlertType = "alert-success" };
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                try
                {
                    var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
                    var registrationRequest = new RegistrationRequest
                    {
                        DialedNumber = refresh.AsDialed,
                        CallbackUrl = refresh.CallbackUrl,
                        ClientSecret = refresh.ClientSecret,
                    };
                    var request = await $"{_baseUrl}client/register".WithOAuthBearerToken(_messagingToken).PostJsonAsync(registrationRequest);
                    var response = await request.GetJsonAsync<RegistrationResponse>();
                    result.Message = $"✔️ Reregistration complete! {response.Message}";
                }
                catch (FlurlHttpException ex)
                {
                    result.RegistrationRequest = new RegistrationRequest { DialedNumber = dialedNumber };
                    result.Message = $"❓Please register this number for service. {await ex.GetResponseStringAsync()}";
                    result.AlertType = "alert-warning";
                } catch (Exception ex)
                {
                    result.Message = $"{ex.Message} {ex.StackTrace}";
                    result.AlertType = "alert-danger";
                }
            }
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray();
            result.FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray();
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        [Authorize]
        [Route("/Messaging/Register")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAsync([Bind("DialedNumber,CallbackUrl,ClientSecret")] RegistrationRequest registrationRequest)
        {
            string message = string.Empty;
            string alertType = "alert-success";
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(registrationRequest.DialedNumber, out var phoneNumber);
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var request = await $"{_baseUrl}client/register".WithOAuthBearerToken(_messagingToken).PostJsonAsync(registrationRequest);
                var response = await request.GetJsonAsync<RegistrationResponse>();
                message = $"✔️ Reregistration complete! {response.Message}";
            }
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers, Message = message, AlertType = alertType });
        }
    }
}
