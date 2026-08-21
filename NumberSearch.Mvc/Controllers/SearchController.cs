using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Models;
using NumberSearch.Mvc.Models;

using Serilog;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SearchController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly MvcConfiguration _configuration = mvcConfiguration;

        public const string SearchLeadSessionKey = "SearchLeadId";
        public const string SearchLeadBypassSessionKey = "SearchLeadBypass";
        public const string VisitorBlockedSessionKey = "VisitorBlocked";
        public const string VisitorEmailSessionKey = "VisitorEmail";
        public const string VisitorPhoneSessionKey = "VisitorPhone";

        // Kept out of source control - see ConnectionStrings:BlockedEmails / BlockedPhoneNumbers in user-secrets.
        private readonly string[] _blockedEmails = mvcConfiguration.BlockedEmails
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        private readonly string[] _blockedPhoneNumbers = [.. mvcConfiguration.BlockedPhoneNumbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToTenDigits)
            .Where(x => !string.IsNullOrWhiteSpace(x))];

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query for available phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <param name="city"></param>
        /// <param name="failed"></param>
        /// <param name="view"></param>
        /// <param name="page"></param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [HttpGet("Search")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SearchAsync(string query, string city, string failed, string view, string token, int page = 1)
        {
            await HttpContext.Session.LoadAsync();

            // Let us link a client or a partner straight to a set of results without asking them to introduce themselves.
            if (IsBypassToken(token))
            {
                HttpContext.Session.SetString(SearchLeadBypassSessionKey, bool.TrueString);
                Log.Information("[Search] A visitor used the bypass token to skip the introduction.");
            }

            // We haven't met yet, so block the page until we have a way to follow up with them.
            if (!HasIntroduced())
            {
                return View("Index", new SearchResults
                {
                    Query = query ?? string.Empty,
                    City = city ?? string.Empty,
                    View = string.IsNullOrWhiteSpace(view) ? "Recommended" : view,
                    Page = page < 1 ? 1 : page,
                    ShowIntroduction = true,
                    Introduction = new SearchLeadForm
                    {
                        Query = query ?? string.Empty,
                        City = city ?? string.Empty,
                        View = string.IsNullOrWhiteSpace(view) ? "Recommended" : view,
                        Page = page < 1 ? 1 : page
                    },
                    Cart = Cart.GetFromSession(HttpContext.Session)
                });
            }

            // Bypass-token visitors are trusted staff/partner links; they skip abuse evaluation entirely.
            var isBypassSession = string.Equals(HttpContext.Session.GetString(SearchLeadBypassSessionKey), bool.TrueString, StringComparison.OrdinalIgnoreCase);

            if (!isBypassSession)
            {
                var blockedFlag = HttpContext.Session.GetString(VisitorBlockedSessionKey);

                // A session established before this check existed has no cached decision yet - evaluate once now and cache it.
                if (blockedFlag is null)
                {
                    var searchLeadId = Guid.TryParse(HttpContext.Session.GetString(SearchLeadSessionKey), out var existingLeadId) ? existingLeadId : (Guid?)null;
                    var existingLead = searchLeadId is not null ? await SearchLead.GetAsync(searchLeadId.Value, _postgresql) : null;
                    var email = existingLead?.Email ?? string.Empty;
                    var phone = existingLead?.ContactPhoneNumber ?? string.Empty;

                    var (blocklistBlocked, blocklistReason) = EvaluateBlocklist(email, phone);
                    var reverseDns = blocklistBlocked ? string.Empty : await GetReverseDnsAsync(GetRemoteIpAddress(HttpContext));
                    var njBlocked = !blocklistBlocked && HostnameIndicatesNewJersey(reverseDns);
                    var blocked = blocklistBlocked || njBlocked;

                    HttpContext.Session.SetString(VisitorBlockedSessionKey, blocked.ToString());
                    HttpContext.Session.SetString(VisitorEmailSessionKey, email);
                    HttpContext.Session.SetString(VisitorPhoneSessionKey, phone);
                    blockedFlag = blocked.ToString();

                    if (blocked)
                    {
                        Log.Warning("[Search] Denied a previously-established session for {Email} / {ContactPhoneNumber} from {IpAddress} ({ReverseDns}): {Reason}",
                            email, phone, GetRemoteIpAddress(HttpContext), reverseDns, blocklistBlocked ? blocklistReason : $"New Jersey rDNS: {reverseDns}");
                    }
                }

                if (string.Equals(blockedFlag, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return View("Index", new SearchResults { Blocked = true, Cart = Cart.GetFromSession(HttpContext.Session) });
                }
            }

            // Fail fast
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Index");
            }

            // Clean up the query.
            query = query.Trim().ToLowerInvariant();

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
                    converted.Add(PhoneNumbersNA.PhoneNumber.LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experience.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            var cleanedQuery = new string([.. converted]);

            // Short circuit area code searches.
            if (cleanedQuery.Length == 3 && cleanedQuery.Equals(query, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var checkConvert = int.TryParse(cleanedQuery, out var code);

                if (checkConvert && PhoneNumbersNA.AreaCodes.All.Contains(code))
                {
                    cleanedQuery += "*******";
                }
            }

            // A query for one of the numbers a known bad actor keeps hitting is itself a signal, regardless of session history.
            if (_blockedPhoneNumbers.Contains(NormalizeToTenDigits(cleanedQuery)))
            {
                HttpContext.Session.SetString(VisitorBlockedSessionKey, bool.TrueString);
                Log.Warning("[Search] Denied service after a blocked-number query: {Query} from {IpAddress}", cleanedQuery, GetRemoteIpAddress(HttpContext));
                return View("Index", new SearchResults { Blocked = true, Cart = Cart.GetFromSession(HttpContext.Session) });
            }

            // Log every query for abuse review: number, timestamp, email/phone on file, user agent, and IP.
            var searchQueryLog = new SearchQuery
            {
                SearchLeadId = Guid.TryParse(HttpContext.Session.GetString(SearchLeadSessionKey), out var loggingLeadId) ? loggingLeadId : null,
                SessionId = HttpContext.Session.Id,
                Query = cleanedQuery,
                Email = HttpContext.Session.GetString(VisitorEmailSessionKey) ?? string.Empty,
                ContactPhoneNumber = HttpContext.Session.GetString(VisitorPhoneSessionKey) ?? string.Empty,
                IpAddress = GetRemoteIpAddress(HttpContext)?.ToString() ?? string.Empty,
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };
            _ = await searchQueryLog.PostAsync(_postgresql);

            // If there's a city provided we need to use a more specific results count query.
            int count = string.IsNullOrWhiteSpace(city)
                ? await PhoneNumber.NumberOfResultsInQuery(cleanedQuery, _postgresql)
                : await PhoneNumber.NumberOfResultsInQueryWithCity(cleanedQuery, city, _postgresql);

            // Handle out of range page values.
            page = page < 1 ? 1 : page;
            page = page * 50 > count ? (count / 50) + 1 : page;

            IEnumerable<PhoneNumber> results;

            // Select a view for the data.
            if (!string.IsNullOrWhiteSpace(view) && view == "Recommended")
            {
                results = await PhoneNumber.RecommendedPaginatedSearchAsync(cleanedQuery, page, _postgresql);
            }
            else if (!string.IsNullOrWhiteSpace(view) && view == "Sequential")
            {
                results = await PhoneNumber.SequentialPaginatedSearchAsync(cleanedQuery, page, _postgresql);
            }
            else if (!string.IsNullOrWhiteSpace(view) && view == "Location")
            {
                // If a city is provided then we need to filter our results down to just that city.
                if (!string.IsNullOrWhiteSpace(city))
                {
                    results = await PhoneNumber.LocationByCityPaginatedSearchAsync(cleanedQuery, city, page, _postgresql);
                }
                else
                {
                    results = await PhoneNumber.LocationPaginatedSearchAsync(cleanedQuery, page, _postgresql);
                }
            }
            else
            {
                results = await PhoneNumber.RecommendedPaginatedSearchAsync(cleanedQuery, page, _postgresql);
            }

            Cart cart = Cart.GetFromSession(HttpContext.Session);

            // The query is a complete phone number and we have no results, perhaps they mean to port it?
            if (cleanedQuery.Length == 10 && !cleanedQuery.Contains('*') && !results.Any())
            {
                var lookup = new LookupController(_configuration);

                var port = await lookup.VerifyPortabilityAsync(cleanedQuery);

                if (port.Portable)
                {
                    var productOrder = new ProductOrder { ProductOrderId = Guid.NewGuid(), PortedDialedNumber = port.PortedDialedNumber, PortedPhoneNumberId = port.PortedPhoneNumberId, Quantity = 1 };
                    var local = port;
                    _ = cart.AddPortedPhoneNumber(ref local, ref productOrder);
                    _ = cart.SetToSession(HttpContext.Session);

                    TempData["CartMessage"] = port.Wireless
                        ? $"✅ {port.PortedDialedNumber} is a wireless number that can be ported to our network, and has been added to your cart!"
                        : $"✅ {port.PortedDialedNumber} can be ported to our network, and has been added to your cart!";
                    TempData["CartAlertType"] = "alert-success";

                    return RedirectToAction("Index", "Cart");
                }
                else
                {
                    return View("Index", new SearchResults
                    {
                        Query = query,
                        CleanQuery = cleanedQuery,
                        City = city ?? string.Empty,
                        View = !string.IsNullOrWhiteSpace(view) ? view : "Recommended",
                        Page = page,
                        Cart = cart,
                        Message = port.Wireless ? $"❌ {cleanedQuery} is a wireless number that cannot be ported to our network!" : $"❌ {cleanedQuery} cannot be ported to our network!",
                        AlertType = "alert-danger"
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(view) && view == "Location")
            {
                var cities = await PhoneNumber.CitiesInQueryAsync(cleanedQuery, _postgresql);

                return View("Index", new SearchResults
                {
                    CleanQuery = cleanedQuery,
                    NumberOfResults = count,
                    Page = page,
                    View = !string.IsNullOrWhiteSpace(view) ? view : "Location",
                    Message = !string.IsNullOrWhiteSpace(failed) ? $"{failed} is not purchasable at this time." : string.Empty,
                    AlertType = "alert-warning",
                    City = city,
                    Cities = [.. cities],
                    PhoneNumbers = [.. results],
                    Query = query,
                    Cart = cart
                });
            }

            return View("Index", new SearchResults
            {
                CleanQuery = cleanedQuery,
                NumberOfResults = count,
                Page = page,
                View = !string.IsNullOrWhiteSpace(view) ? view : "Recommended",
                Message = !string.IsNullOrWhiteSpace(failed) ? $"{failed} is not purchasable at this time." : string.Empty,
                PhoneNumbers = [.. results],
                City = city,
                Query = query,
                Cart = cart
            });
        }

        [HttpPost("Search/Introduction")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IntroductionAsync(SearchLeadForm? introduction)
        {
            await HttpContext.Session.LoadAsync();

            introduction ??= new SearchLeadForm();
            introduction.View = string.IsNullOrWhiteSpace(introduction.View) ? "Recommended" : introduction.View;
            introduction.Page = introduction.Page < 1 ? 1 : introduction.Page;

            if (string.IsNullOrWhiteSpace(introduction.Name))
            {
                return Introduction(introduction, "🙋 Please tell us your name.");
            }

            if (string.IsNullOrWhiteSpace(introduction.Email))
            {
                return Introduction(introduction, "💌 Please supply an email address so that we can send you your options.");
            }

            if (string.IsNullOrWhiteSpace(introduction.ContactPhoneNumber))
            {
                return Introduction(introduction, "📞 Please supply a phone number so that we can reach you.");
            }

            // Validate the email address the same way the checkout page does.
            string emailDomain;

            try
            {
                var emailValidation = await CartController.VerifyEmailByAddressAsync(introduction.Email.AsMemory());
                emailDomain = emailValidation.EmailDomain.Host;

                if (emailValidation.MxRecordExists)
                {
                    Log.Information("[Search] Email address {Email} has a valid domain: {Host}.", introduction.Email, emailDomain);
                }
                else
                {
                    Log.Error("[Search] Email address {Email} has an invalid domain: {Host}.", introduction.Email, emailDomain);
                    introduction.Email = string.Empty;
                    return Introduction(introduction, $"💀 The email server at {emailDomain} didn't have an MX record. Please supply a valid email address.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Search] Email address {Email} has an invalid domain. {Message}", introduction.Email, ex.Message);
                var invalid = introduction.Email;
                introduction.Email = string.Empty;
                return Introduction(introduction, $"💀 The email server at {invalid} didn't have an MX record. Please supply a valid email address.");
            }

            // Validate the contact phone number the same way the checkout page does.
            var checkParsed = PhoneNumbersNA.PhoneNumber.TryParse(introduction.ContactPhoneNumber, out var contact);

            if (checkParsed is false)
            {
                Log.Error("[Search] The contact phone number is not a dialable North American phone number.");
                introduction.ContactPhoneNumber = string.Empty;
                return Introduction(introduction, "💀 That phone number is not a dialable North American phone number.");
            }

            try
            {
                var checkPortable = await ValidatePortability.GetAsync(contact.DialedNumber.AsMemory(), _configuration.BulkVSUsername.AsMemory(), _configuration.BulkVSPassword.AsMemory());

                if (string.IsNullOrWhiteSpace(checkPortable.TN) || checkPortable.Portable is false)
                {
                    Log.Error("[Search] The contact phone number is not a dialable North American phone number.");
                    introduction.ContactPhoneNumber = string.Empty;
                    return Introduction(introduction, "💀 That phone number is not a dialable North American phone number.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Search] The contact phone number is not a dialable North American phone number. {Message}", ex.Message);
                introduction.ContactPhoneNumber = string.Empty;
                return Introduction(introduction, "💀 That phone number is not a dialable North American phone number.");
            }

            var remoteIp = GetRemoteIpAddress(HttpContext);
            var (blocklistBlocked, blocklistReason) = EvaluateBlocklist(introduction.Email.Trim(), contact.DialedNumber);
            var reverseDns = blocklistBlocked ? string.Empty : await GetReverseDnsAsync(remoteIp);
            var njBlocked = !blocklistBlocked && HostnameIndicatesNewJersey(reverseDns);
            var blocked = blocklistBlocked || njBlocked;
            var blockReason = blocklistBlocked ? blocklistReason : njBlocked ? $"New Jersey rDNS: {reverseDns}" : string.Empty;

            SearchLead lead = new()
            {
                SearchLeadId = Guid.NewGuid(),
                SessionId = HttpContext.Session.Id,
                Name = introduction.Name.Trim(),
                ContactPhoneNumber = contact.DialedNumber,
                Email = introduction.Email.Trim(),
                EmailDomain = emailDomain,
                MxRecordExists = true,
                ContactPhoneNumberPortable = true,
                Query = introduction.Query ?? string.Empty,
                IpAddress = remoteIp?.ToString() ?? string.Empty,
                ReverseDns = reverseDns,
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                Referrer = HttpContext.Request.Headers.Referer.ToString(),
                Blocked = blocked,
                BlockReason = blockReason,
                DateSubmitted = DateTime.Now
            };

            if (await lead.PostAsync(_postgresql))
            {
                Log.Information("[Search] Saved lead {SearchLeadId} for {Email} at {ContactPhoneNumber}.", lead.SearchLeadId, lead.Email, lead.ContactPhoneNumber);
            }
            else
            {
                // Don't hold the visitor hostage over a failed insert, but make sure we hear about it.
                Log.Error("[Search] Failed to save lead for {Email} at {ContactPhoneNumber}.", lead.Email, lead.ContactPhoneNumber);
            }

            if (blocked)
            {
                Log.Warning("[Search] Denied service to {Email} / {ContactPhoneNumber} from {IpAddress} ({ReverseDns}): {BlockReason}",
                    lead.Email, lead.ContactPhoneNumber, lead.IpAddress, lead.ReverseDns, lead.BlockReason);
            }

            HttpContext.Session.SetString(SearchLeadSessionKey, lead.SearchLeadId.ToString());
            HttpContext.Session.SetString(VisitorBlockedSessionKey, blocked.ToString());
            HttpContext.Session.SetString(VisitorEmailSessionKey, lead.Email);
            HttpContext.Session.SetString(VisitorPhoneSessionKey, lead.ContactPhoneNumber);

            return RedirectToAction("Search", "Search", new
            {
                query = introduction.Query,
                city = introduction.City,
                view = introduction.View,
                page = introduction.Page
            });
        }

        private ViewResult Introduction(SearchLeadForm introduction, string message)
            => View("Index", new SearchResults
            {
                Query = introduction.Query ?? string.Empty,
                City = introduction.City ?? string.Empty,
                View = introduction.View,
                Page = introduction.Page,
                ShowIntroduction = true,
                IntroductionMessage = message,
                Introduction = introduction,
                Cart = Cart.GetFromSession(HttpContext.Session)
            });

        private bool HasIntroduced()
        {
            if (string.Equals(HttpContext.Session.GetString(SearchLeadBypassSessionKey), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Guid.TryParse(HttpContext.Session.GetString(SearchLeadSessionKey), out var searchLeadId) && searchLeadId != Guid.Empty;
        }

        private bool IsBypassToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_configuration.SearchToken))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(_configuration.SearchToken));
        }

        private static IPAddress? GetRemoteIpAddress(HttpContext httpContext)
        {
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                foreach (var candidate in forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (IPAddress.TryParse(candidate.Trim(), out var address)
                        && address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                    {
                        return address;
                    }
                }
            }

            return httpContext.Connection.RemoteIpAddress;
        }

        private (bool blocked, string reason) EvaluateBlocklist(string email, string phoneDialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(email) && _blockedEmails.Contains(email.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return (true, $"Blocked email: {email}");
            }

            var normalizedPhone = NormalizeToTenDigits(phoneDialedNumber);

            if (!string.IsNullOrWhiteSpace(normalizedPhone) && _blockedPhoneNumbers.Contains(normalizedPhone))
            {
                return (true, $"Blocked phone number: {normalizedPhone}");
            }

            return (false, string.Empty);
        }

        private static string NormalizeToTenDigits(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var digits = new string([.. input.Where(char.IsDigit)]);

            return digits.Length > 10 ? digits[^10..] : digits;
        }

        private static async Task<string> GetReverseDnsAsync(IPAddress? ip)
        {
            if (ip is null)
            {
                return string.Empty;
            }

            try
            {
                var entry = await Dns.GetHostEntryAsync(ip).WaitAsync(TimeSpan.FromSeconds(2));
                return entry.HostName ?? string.Empty;
            }
            catch
            {
                // DNS is unreliable by nature - a failed or slow lookup just means we can't evaluate the NJ heuristic for this visitor.
                return string.Empty;
            }
        }

        // ISP rDNS conventions typically encode the state as its own dot/hyphen-delimited token (e.g. "*.nj.comcast.net", "*-nj-*.verizon.net").
        // A plain substring match on "nj" would false-positive on unrelated hostnames (e.g. "ninja"), so this requires it to stand alone as a token.
        // This is still a heuristic, not a precise geolocation - rDNS naming isn't standardized across ISPs.
        private static bool HostnameIndicatesNewJersey(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return false;
            }

            var tokens = hostname.Split(['.', '-'], StringSplitOptions.RemoveEmptyEntries);

            return tokens.Any(t => t.Equals("nj", StringComparison.OrdinalIgnoreCase));
        }
    }
}
