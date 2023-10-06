using AccelerateNetworks.Operations;

using Amazon.S3.Model;

using Azure;

using FirstCom;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.EntityFrameworkCore;

using Models;

using NumberSearch.Ops.Models;

using Serilog;

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
            var stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            //bool refresh = false;
            //foreach (var number in stats)
            //{
            //    if (number.RegisteredUpstream is false && number.AsDialed is not "Total")
            //    {
            //        bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.AsDialed, out var phoneNumber);
            //        if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            //        {
            //            _ = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetStringAsync();
            //            refresh = true;
            //        }
            //    }
            //}

            //if (refresh)
            //{
            //    stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            //}

            return View(new MessagingResult { ClientRegistrations = stats.OrderByDescending(x => x.DateRegistered).ToArray(), Owned = ownedNumbers });
        }

        [Authorize]
        [Route("/Messaging/Failed")]
        public async Task<IActionResult> FailedMessagesAsync()
        {
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Failed", new MessagingResult { FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray(), Owned = ownedNumbers });
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
            var stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { ClientRegistrations = stats.OrderByDescending(x => x.DateRegistered).ToArray(), Owned = ownedNumbers, Message = message, AlertType = alertType });
        }

        [Authorize]
        [Route("/Messaging/Reregister")]
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
                }
                catch (Exception ex)
                {
                    result.Message = $"{ex.Message} {ex.StackTrace}";
                    result.AlertType = "alert-danger";
                }
            }

            // Refresh the status
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = stats.OrderByDescending(x => x.DateRegistered).ToArray();
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        [Authorize]
        [Route("/Messaging/ToEmail")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MessagingToEmailAsync([Bind("DialedNumber,Email")] ToEmailRequest toEmail)
        {
            var result = new MessagingResult { AlertType = "alert-success" };

            if (string.IsNullOrWhiteSpace(toEmail?.Email) || !toEmail.Email.Contains('@'))
            {
                result.Message = $"❌ Email address is invalid. {toEmail?.Email}";
                result.AlertType = "alert-danger";

                var stats1 = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
                var ownedNumbers1 = await _context.OwnedPhoneNumbers.ToArrayAsync();
                result.ClientRegistrations = stats1.OrderByDescending(x => x.DateRegistered).ToArray();
                result.Owned = ownedNumbers1;
                return View("Index", result);
            }

            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(toEmail.DialedNumber, out var phoneNumber);
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                bool registeredUpstream = false;
                string upstreamStatusDescription = string.Empty;
                string dialedNumber = phoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode ? $"1{phoneNumber.DialedNumber}" : phoneNumber.DialedNumber;

                try
                {
                    // Verify that this number is routed through our upstream provider.
                    var checkRouted = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber, _config.PComNetUsername, _config.PComNetPassword);
                    Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
                    registeredUpstream = checkRouted.QueryResult.code is 0 && checkRouted.epid is 265;
                    upstreamStatusDescription = checkRouted.QueryResult.text;
                    if (checkRouted.QueryResult.code is not 0 || checkRouted.epid is not 265)
                    {
                        // Enabled routing and set the EPID if the number is not already routed.
                        var enableSMS = await FirstPointComSMS.EnableSMSByDialedNumberAsync(dialedNumber, _config.PComNetUsername, _config.PComNetPassword);
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(enableSMS));
                        var setRouting = await FirstPointComSMS.RouteSMSToEPIDByDialedNumberAsync(dialedNumber, 265, _config.PComNetUsername, _config.PComNetPassword);
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(setRouting));
                        var checkRoutedAgain = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber, _config.PComNetUsername, _config.PComNetPassword);
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
                        registeredUpstream = checkRouted.QueryResult.code is 0 && checkRouted.epid is 265;
                        upstreamStatusDescription = checkRouted.QueryResult.text;
                        result.Message = $"Attempted to set and enable SMS routing for {dialedNumber}. SMS Enabled? {enableSMS.text} Routing Set? {setRouting.text} SMS Routed? {checkRoutedAgain.QueryResult.text}";
                    }
                    else
                    {
                        try
                        {
                            var response = await FirstCom.FirstPointComSMS.SMSToEmailByDialedNumberAsync(dialedNumber, toEmail.Email, _config.PComNetUsername, _config.PComNetPassword);
                            result.Message = $"✔️ Reregistration complete! {response.text} This number is routed for SMS service with our upstream vendor: {checkRouted.QueryResult.text}";
                        }
                        catch (FlurlHttpException ex)
                        {
                            result.ToEmail = new() { DialedNumber = dialedNumber, Email = toEmail?.Email ?? string.Empty };
                            result.Message = $"❓Please register this number for service. {checkRouted.QueryResult.text} {await ex.GetResponseStringAsync()} ";
                            result.AlertType = "alert-warning";
                        }
                        catch (Exception ex)
                        {
                            result.Message = $"{ex.Message} {ex.StackTrace}";
                            result.AlertType = "alert-danger";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace ?? "No stack trace found.");
                    result.Message = $"❌ Failed to enabled messaging service through EndStream for this dialed number. Please email dan@acceleratenetworks.com to report this outage. {ex.Message}";
                    result.AlertType = "alert-danger";
                    var stats2 = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
                    var ownedNumbers2 = await _context.OwnedPhoneNumbers.ToArrayAsync();
                    result.ClientRegistrations = stats2.OrderByDescending(x => x.DateRegistered).ToArray();
                    result.Owned = ownedNumbers2;
                    return View("Index", result);
                }
            }
            else
            {
                result.Message = $"❌ Dialed number is invalid. {toEmail.DialedNumber}";
                result.AlertType = "alert-danger";
            }
            // Refresh the status
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = stats.OrderByDescending(x => x.DateRegistered).ToArray();
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        [Authorize]
        [HttpPost]
        [Route("/Messaging/Register")]
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
            // Refresh the status
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = await $"{_baseUrl}client/all".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { ClientRegistrations = stats.OrderByDescending(x => x.DateRegistered).ToArray(), Owned = ownedNumbers, Message = message, AlertType = alertType });
        }
    }
}
