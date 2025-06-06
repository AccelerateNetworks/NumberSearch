﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using NumberSearch.DataAccess.Models;
using NumberSearch.Mvc.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SearchController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly MvcConfiguration _configuration = mvcConfiguration;

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <param name="city"></param>
        /// <param name="failed"></param>
        /// <param name="view"></param>
        /// <param name="page"></param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [HttpGet("Search")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [OutputCache(Duration = 1, VaryByQueryKeys = ["query", "page", "view"])]
        public async Task<IActionResult> SearchAsync(string query, string city, string failed, string view, int page = 1)
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
                        Message = port.Wireless ? "❌ This wireless phone number cannot be ported to our network!" : "❌ This phone number cannot be ported to our network!",
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
    }
}
