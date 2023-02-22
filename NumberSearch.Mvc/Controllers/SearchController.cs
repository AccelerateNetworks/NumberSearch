using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SearchController : Controller
    {
        private readonly string _postgresql;
        private readonly MvcConfiguration _configuration;

        public SearchController(MvcConfiguration mvcConfiguration)
        {
            _configuration = mvcConfiguration;
            _postgresql = mvcConfiguration.PostgresqlProd;
        }

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [HttpGet("Search/")]
        [HttpGet("Search/{Query}")]
        [HttpPost("Search/")]
        [HttpPost("Search/{Query}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync(string query, string city, string failed, string view, int page = 1)
        {
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

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            var cleanedQuery = new string(converted.ToArray());

            // Short curcit area code searches.
            if (cleanedQuery.Length == 3 && cleanedQuery.Equals(query, System.StringComparison.InvariantCultureIgnoreCase))
            {
                var checkConvert = int.TryParse(cleanedQuery, out var code);

                if (checkConvert && PhoneNumbersNA.AreaCode.All.Contains(code))
                {
                    cleanedQuery += "*******";
                }
            }

            // If there's a city provided we need to use a more specific results count query.
            var count = (string.IsNullOrWhiteSpace(city))
                ? await PhoneNumber.NumberOfResultsInQuery(cleanedQuery, _postgresql).ConfigureAwait(false)
                : await PhoneNumber.NumberOfResultsInQueryWithCity(cleanedQuery, city, _postgresql).ConfigureAwait(false);

            // Handle out of range page values.
            page = page < 1 ? 1 : page;
            page = page * 50 > count ? (count / 50) + 1 : page;

            IEnumerable<PhoneNumber> results;

            // Select a view for the data.
            if (!string.IsNullOrWhiteSpace(view) && view == "Recommended")
            {
                results = await PhoneNumber.RecommendedPaginatedSearchAsync(cleanedQuery, page, _postgresql).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(view) && view == "Sequential")
            {
                results = await PhoneNumber.SequentialPaginatedSearchAsync(cleanedQuery, page, _postgresql).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(view) && view == "Location")
            {
                // If a city is provided then we need to filter our results down to just that city.
                if (!string.IsNullOrWhiteSpace(city))
                {
                    results = await PhoneNumber.LocationByCityPaginatedSearchAsync(cleanedQuery, city, page, _postgresql).ConfigureAwait(false);
                }
                else
                {
                    results = await PhoneNumber.LocationPaginatedSearchAsync(cleanedQuery, page, _postgresql).ConfigureAwait(false);
                }
            }
            else
            {
                results = await PhoneNumber.RecommendedPaginatedSearchAsync(cleanedQuery, page, _postgresql).ConfigureAwait(false);
            }

            var cart = Cart.GetFromSession(HttpContext.Session);

            // The query is a complete phone number and we have no results, perhaps they mean to port it?
            if (cleanedQuery.Length == 10 && !cleanedQuery.Contains('*') && !results.Any())
            {
                var lookup = new LookupController(_configuration);

                var port = await lookup.VerifyPortablityAsync(cleanedQuery);

                if (port.Portable)
                {
                    return View("Porting", new PortingResults
                    {
                        PortedPhoneNumber = port,
                        Cart = cart,
                        Query = query,
                        Message = port.Wireless ? "This wireless phone number can be ported to our network!" : "This phone number can be ported to our network!"
                    });
                }
                else
                {

                    return View("Porting", new PortingResults
                    {
                        PortedPhoneNumber = port,
                        Cart = cart,
                        Query = query,
                        Message = port.Wireless ? "This wireless phone number can likely be ported to our network!" : "This phone number can likely be ported to our network!"
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(view) && view == "Location")
            {
                var cities = await PhoneNumber.CitiesInQueryAsync(cleanedQuery, _postgresql).ConfigureAwait(false);

                return View("Index", new SearchResults
                {
                    CleanQuery = cleanedQuery,
                    NumberOfResults = count,
                    Page = page,
                    View = !string.IsNullOrWhiteSpace(view) ? view : "Location",
                    Message = !string.IsNullOrWhiteSpace(failed) ? $"{failed} is not purchasable at this time." : string.Empty,
                    AlertType = "alert-warning",
                    City = city,
                    Cities = cities.ToArray(),
                    PhoneNumbers = results.ToArray(),
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
                PhoneNumbers = results.ToArray(),
                City = city,
                Query = query,
                Cart = cart
            });
        }
    }
}
