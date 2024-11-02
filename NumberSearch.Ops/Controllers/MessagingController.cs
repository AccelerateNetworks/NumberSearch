using AccelerateNetworks.Operations;

using CsvHelper;

using FirstCom;

using Flurl.Http;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Models;

using NumberSearch.DataAccess.Twilio;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class MessagingController(numberSearchContext context, OpsConfig opsConfig) : Controller
    {
        private readonly numberSearchContext _context = context;
        private readonly OpsConfig _config = opsConfig;
        private readonly string _baseUrl = opsConfig.MessagingURL;
        private readonly string _messagingUsername = opsConfig.MessagingUsername;
        private readonly string _messagingPassword = opsConfig.MessagingPassword;

        private Task<AccessTokenResponse> GetTokenAsync()
        {
            var loginRequest = new LoginRequest()
            {
                Email = _messagingUsername,
                Password = _messagingPassword,
                TwoFactorCode = string.Empty,
                TwoFactorRecoveryCode = string.Empty
            };
            return $"{_baseUrl}login".PostJsonAsync(loginRequest).ReceiveJson<AccessTokenResponse>();
        }

        [Authorize]
        [Route("/Messaging")]
        [Route("/Messaging/Index")]
        public async Task<IActionResult> IndexAsync()
        {
            string message = string.Empty;
            var token = await GetTokenAsync();
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View(new MessagingResult { ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)], Owned = ownedNumbers, Message = message });
        }

        [Authorize]
        [Route("/Messaging/Failed")]
        public async Task<IActionResult> FailedMessagesAsync()
        {
            var token = await GetTokenAsync();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<MessageRecord[]>();
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Failed", new MessagingResult { FailedMessages = [.. failures.OrderByDescending(x => x.DateReceivedUTC)], Owned = ownedNumbers });
        }

        [Authorize]
        [Route("/Messaging/RefreshStatus")]
        public async Task<IActionResult> RefreshStatusAsync(string dialedNumber)
        {
            string message = string.Empty;
            string alertType = "alert-success";
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            var token = await GetTokenAsync();
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration>();
                message = $"🔃 Upstream Status {refresh.UpstreamStatusDescription} for {refresh.AsDialed} routed to {refresh.CallbackUrl}";
            }
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }

            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)], Owned = ownedNumbers, Message = message, AlertType = alertType });
        }

        [Authorize]
        [Route("/Messaging/Reregister")]
        public async Task<IActionResult> RegisterAsync(string dialedNumber)
        {
            string message = string.Empty;
            var result = new MessagingResult { AlertType = "alert-success" };
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            var token = await GetTokenAsync();
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                try
                {
                    var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration>();
                    var registrationRequest = new RegistrationRequest
                    {
                        DialedNumber = refresh.AsDialed,
                        CallbackUrl = refresh.CallbackUrl,
                        ClientSecret = refresh.ClientSecret,
                    };
                    var request = await $"{_baseUrl}client/register".WithOAuthBearerToken(token.AccessToken).PostJsonAsync(registrationRequest);
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
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
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
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(toEmail?.Email) || !toEmail.Email.Contains('@'))
            {
                result.Message = $"❌ Email address is invalid. {toEmail?.Email}";
                result.AlertType = "alert-danger";

                var stats1 = await $"{_baseUrl}client/all".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration[]>();
                var ownedNumbers1 = await _context.OwnedPhoneNumbers.ToArrayAsync();
                result.ClientRegistrations = [.. stats1.OrderByDescending(x => x.DateRegistered)];
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
                    var checkRouted = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber.AsMemory(), _config.PComNetUsername.AsMemory(), _config.PComNetPassword.AsMemory());
                    Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
                    registeredUpstream = checkRouted.QueryResult.code is 0;
                    upstreamStatusDescription = checkRouted.QueryResult.text;
                    if (checkRouted.QueryResult.code is not 0)
                    {
                        // Enabled routing and set the EPID if the number is not already routed.
                        var enableSMS = await FirstPointComSMS.EnableSMSByDialedNumberAsync(dialedNumber, _config.PComNetUsername, _config.PComNetPassword);
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(enableSMS));
                        var checkRoutedAgain = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber.AsMemory(), _config.PComNetUsername.AsMemory(), _config.PComNetPassword.AsMemory());
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
                        registeredUpstream = checkRouted.QueryResult.code is 0;
                        upstreamStatusDescription = checkRouted.QueryResult.text;
                        result.Message = $"❓Attempted to set and enable SMS routing for {dialedNumber}. SMS Enabled? {enableSMS.text} SMS Routed? {checkRoutedAgain.QueryResult.text} Please try again in 24 hours.";
                        result.AlertType = "alert-warning";
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
                    var stats2 = new List<ClientRegistration>();
                    var page2 = 1;
                    try
                    {
                        var pageResult = await $"{_config.MessagingURL}client/all?page={page2}"
                            .WithOAuthBearerToken(token.AccessToken)
                            .GetJsonAsync<ClientRegistration[]>();

                        while (pageResult.Length is 100)
                        {
                            stats2.AddRange(pageResult);
                            page2++;
                            pageResult = await $"{_config.MessagingURL}client/all?page={page2}"
                                .WithOAuthBearerToken(token.AccessToken)
                                .GetJsonAsync<ClientRegistration[]>();
                        }
                    }
                    catch
                    {
                        result.Message = "❌ Failed to get client registration data from sms.callpipe.com.";
                    }
                    var ownedNumbers2 = await _context.OwnedPhoneNumbers.ToArrayAsync();
                    result.ClientRegistrations = [.. stats2.OrderByDescending(x => x.DateRegistered)];
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
            // This won't work because SMSToEmail registered numbers are not registered clients.
            //if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            //{
            //    var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<ClientRegistration>();
            //}
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
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
            var token = await GetTokenAsync();
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var request = await $"{_baseUrl}client/register".WithOAuthBearerToken(token.AccessToken).PostJsonAsync(registrationRequest);
                var response = await request.GetJsonAsync<RegistrationResponse>();
                message = $"✔️ Reregistration complete! {response.Message}";
            }
            // Refresh the status
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                var refresh = await $"{_baseUrl}client?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration>();
            }
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            return View("Index", new MessagingResult { ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)], Owned = ownedNumbers, Message = message, AlertType = alertType });
        }

        [Authorize]
        [Route("/Messaging/Remove")]
        public async Task<IActionResult> RemoveAsync(string dialedNumber)
        {
            var result = new MessagingResult { AlertType = "alert-success" };
            bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
            var token = await GetTokenAsync();
            if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
            {
                try
                {
                    var request = await $"{_baseUrl}client/remove?asDialed={phoneNumber.DialedNumber}".WithOAuthBearerToken(token.AccessToken).PostAsync();
                    var response = await request.GetStringAsync();
                    result.Message = $"✔️ Registration removed! {response}";
                }
                catch (FlurlHttpException ex)
                {
                    result.Message = $"❓Failed to removed this registration. {await ex.GetResponseStringAsync()}";
                    result.AlertType = "alert-warning";
                }
                catch (Exception ex)
                {
                    result.Message = $"{ex.Message} {ex.StackTrace}";
                    result.AlertType = "alert-danger";
                }
            }
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        [Authorize]
        [Route("/Messaging/TwilioCarrier")]
        public async Task<IActionResult> TwilioCarrierAsync(string dialedNumber, bool? refreshAll)
        {
            var result = new MessagingResult { AlertType = "alert-success" };
            var token = await GetTokenAsync();

            if (refreshAll is not null && refreshAll is true)
            {
                var numbers = await $"{_baseUrl}client/all".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<ClientRegistration[]>();
                var existing = await _context.OwnedPhoneNumbers.ToDictionaryAsync(x => x.DialedNumber, x => x);

                foreach (var number in numbers)
                {
                    bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.AsDialed, out var phoneNumber);
                    bool checkOwned = existing.TryGetValue(phoneNumber.DialedNumber, out var owned);
                    if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(number.AsDialed) && checkOwned && string.IsNullOrWhiteSpace(owned?.TwilioCarrierName))
                    {
                        try
                        {
                            var response = await LineTypeIntelligenceResponse.GetByDialedNumberAsync(phoneNumber.DialedNumber.AsMemory(), _config.TwilioUsername.AsMemory(), _config.TwilioPassword.AsMemory());
                            if (!string.IsNullOrWhiteSpace(response.line_type_intelligence.carrier_name))
                            {
                                // Update the owned number record
                                if (owned is not null)
                                {
                                    owned.TwilioCarrierName = response.line_type_intelligence.carrier_name.Trim();
                                    await _context.SaveChangesAsync();
                                    result.Message += $"✔️ Refreshed Carrier Name from Twilio! {owned.TwilioCarrierName}\n";
                                }
                                else
                                {
                                    result.Message += $"❌ Could not match {phoneNumber.DialedNumber} to an Owned Phone Number. {response.line_type_intelligence.carrier_name}\n";
                                }
                            }
                            else
                            {
                                result.Message += $"❌ Twilio has no Carrier Name for this {phoneNumber.DialedNumber}.\n";
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            result.RegistrationRequest = new RegistrationRequest { DialedNumber = number.AsDialed };
                            result.Message += $"❓Failed to query Twilio. {await ex.GetResponseStringAsync()}\n";
                            result.AlertType = "alert-warning";
                        }
                        catch (Exception ex)
                        {
                            result.Message += $"{ex.Message} {ex.StackTrace}\n";
                            result.AlertType = "alert-danger";
                        }
                    }
                }
            }
            else
            {
                bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);
                if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
                {
                    try
                    {
                        var response = await LineTypeIntelligenceResponse.GetByDialedNumberAsync(phoneNumber.DialedNumber.AsMemory(), _config.TwilioUsername.AsMemory(), _config.TwilioPassword.AsMemory());
                        if (!string.IsNullOrWhiteSpace(response.line_type_intelligence.carrier_name))
                        {
                            // Update the owned number record
                            var owned = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == phoneNumber.DialedNumber);
                            if (owned is not null)
                            {
                                owned.TwilioCarrierName = response.line_type_intelligence.carrier_name.Trim();
                                await _context.SaveChangesAsync();
                                result.Message = $"✔️ Refreshed Carrier Name from Twilio! {owned.TwilioCarrierName}";
                            }
                            else
                            {
                                result.Message = $"❌ Could not match {phoneNumber.DialedNumber} to an Owned Phone Number. {response.line_type_intelligence.carrier_name}";
                            }
                        }
                        else
                        {
                            result.Message = $"❌ Twilio has no Carrier Name for this {phoneNumber.DialedNumber}.";
                        }
                    }
                    catch (FlurlHttpException ex)
                    {
                        result.RegistrationRequest = new RegistrationRequest { DialedNumber = dialedNumber };
                        result.Message = $"❓Failed to query Twilio. {await ex.GetResponseStringAsync()}";
                        result.AlertType = "alert-warning";
                    }
                    catch (Exception ex)
                    {
                        result.Message = $"{ex.Message} {ex.StackTrace}";
                        result.AlertType = "alert-danger";
                    }
                }
            }

            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        public record CSVExport(string DialedNumber, bool RegisteredUpstream, string UpstreamStatusDescription, string Carrier);

        [Authorize]
        [Route("/Messaging/ExportToCSV")]
        public async Task<IActionResult> ExportToCSV()
        {
            var result = new MessagingResult { AlertType = "alert-success" };
            var token = await GetTokenAsync();
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            var exportReady = new List<CSVExport>(stats.Count);
            try
            {
                foreach (var number in stats)
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.AsDialed, out var phoneNumber);
                    var ownedPhoneNumber = ownedNumbers.FirstOrDefault(x => x.DialedNumber == phoneNumber.DialedNumber);
                    exportReady.Add(new CSVExport(number.AsDialed, number.RegisteredUpstream, number.UpstreamStatusDescription, ownedPhoneNumber?.TwilioCarrierName ?? string.Empty));
                }

                var filePath = Path.GetFullPath(Path.Combine("wwwroot", "csv"));
                var fileName = $"MessagingUsers{DateTime.Now:yyyyMMdd}.csv";
                var completePath = Path.Combine(filePath, fileName);

                using var writer = new StreamWriter(completePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(exportReady).ConfigureAwait(false);
                var file = new FileInfo(completePath);

                if (file.Exists)
                {
                    return Redirect($"../csv/{file.Name}");
                }
                else
                {
                    result.Message = $"❓Failed to export this CSV.";
                    result.AlertType = "alert-warning";
                    result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
                    result.Owned = ownedNumbers;
                    return View("Index", result);
                }
            }
            catch (Exception ex)
            {
                result.Message = $"❓Failed to export this CSV. {ex.Message} {ex.StackTrace}";
                result.AlertType = "alert-danger";
            }

            result.Message = $"❓Failed to export this CSV.";
            result.AlertType = "alert-warning";
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
            result.Owned = ownedNumbers;
            return View("Index", result);
        }

        [Authorize]
        [Route("/Messaging/RefreshCarrier")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCarrierNamesAsync(string carrierName)
        {
            var result = new MessagingResult { AlertType = "alert-success" };

            var existing = await _context.OwnedPhoneNumbers.Where(x => x.Active && x.TwilioCarrierName == carrierName).ToArrayAsync();

            if (carrierName is "All")
            {
                existing = await _context.OwnedPhoneNumbers.Where(x => x.Active).ToArrayAsync();
            }

            foreach (var number in existing)
            {
                bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.DialedNumber, out var phoneNumber);
                if (checkParse && phoneNumber is not null)
                {
                    try
                    {
                        var response = await LineTypeIntelligenceResponse.GetByDialedNumberAsync(phoneNumber.DialedNumber.AsMemory(), _config.TwilioUsername.AsMemory(), _config.TwilioPassword.AsMemory());
                        if (!string.IsNullOrWhiteSpace(response.line_type_intelligence.carrier_name))
                        {
                            // Update the owned number record
                            result.Message += $"✔️ {phoneNumber.DialedNumber} - Old: {number.TwilioCarrierName} - New: {response.line_type_intelligence.carrier_name.Trim()}\n";
                            number.TwilioCarrierName = response.line_type_intelligence.carrier_name.Trim();
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            result.Message += $"❌ Twilio has no Carrier Name for this {phoneNumber.DialedNumber}.\n";
                        }
                    }
                    catch (FlurlHttpException ex)
                    {
                        result.Message += $"❓Failed to query Twilio. {await ex.GetResponseStringAsync()}\n";
                        result.AlertType = "alert-warning";
                    }
                    catch (Exception ex)
                    {
                        result.Message += $"{ex.Message} {ex.StackTrace}\n";
                        result.AlertType = "alert-danger";
                    }
                }
            }

            var token = await GetTokenAsync();
            var stats = new List<ClientRegistration>();
            int page = 1;
            try
            {
                var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();

                while (pageResult.Length is 100)
                {
                    stats.AddRange(pageResult);
                    page++;
                    pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                        .WithOAuthBearerToken(token.AccessToken)
                        .GetJsonAsync<ClientRegistration[]>();
                }
            }
            catch (FlurlHttpException ex)
            {
                result.Message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
            }
            var ownedNumbers = await _context.OwnedPhoneNumbers.ToArrayAsync();
            result.ClientRegistrations = [.. stats.OrderByDescending(x => x.DateRegistered)];
            result.Owned = ownedNumbers;
            result.Message = string.IsNullOrWhiteSpace(result.Message) ? $"❓Failed to query Twilio for {carrierName}" : result.Message;
            return View("Index", result);
        }
    }
}
