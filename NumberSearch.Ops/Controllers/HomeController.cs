﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using BulkVS.BulkVS;

using FirstCom;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.Ops.Models;

namespace NumberSearch.Ops.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;
        private readonly string _username;
        private readonly string _password;


        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _username = config.GetConnectionString("PComNetUsername");
            _password = config.GetConnectionString("PComNetPassword");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }


        [Authorize]
        public async Task<IActionResult> Orders()
        {
            // Show all orders
            var orders = await Order.GetAllAsync(_postgresql);

            return View("Orders", orders.OrderByDescending(x => x.DateSubmitted));
        }

        [Authorize]
        public async Task<IActionResult> NumberOrders()
        {
            // Show all orders
            var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql);

            return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
        }

        [Authorize]
        public async Task<IActionResult> PortRequests()
        {
            // Show all orders
            var portRequests = await PortRequest.GetAllAsync(_postgresql);

            return View("PortRequests", portRequests.OrderByDescending(x => x.DateSubmitted));
        }

        [Authorize]
        public async Task<IActionResult> Tests(string testName, string npa, string nxx, string dialedNumber)
        {
            if (testName == "DIDInventorySearchAsync" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx) || !string.IsNullOrWhiteSpace(dialedNumber)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                dialedNumber ??= string.Empty;

                var results = await NpaNxxFirstPointCom.GetAsync(npa, nxx, dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    NPA = npa,
                    NXX = nxx,
                    DialedNumber = dialedNumber,
                    PhoneNumbers = results
                });
            }

            if (testName == "DIDOrderAsync" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var results = await FirstPointComOrderPhoneNumber.PostAsync(dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumberOrder = results
                });
            }


            return View("Tests");
        }


        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [Authorize]
        [Route("Numbers/{Query}")]
        [Route("Numbers/")]
        public async Task<IActionResult> Numbers(string query, int page = 1)
        {
            // Fail fast
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Numbers");
            }

            // Clean up the query.
            query = query?.Trim();

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
                    converted.Add(LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1')
            {
                converted.Remove('1');
            }

            var results = await PhoneNumber.PaginatedSearchAsync(new string(converted.ToArray()), page, _postgresql).ConfigureAwait(false);
            var count = await PhoneNumber.NumberOfResultsInQuery(new string(converted.ToArray()), _postgresql).ConfigureAwait(false);

            return View("Numbers", new SearchResults
            {
                CleanQuery = new string(converted.ToArray()),
                NumberOfResults = count,
                Page = page,
                PhoneNumbers = results.ToArray(),
                Query = query
            });
        }

        public static char LetterToKeypadDigit(char letter)
        {
            // Map the chars to their keypad numerical values.
            switch (letter)
            {
                case '+':
                    return '0';
                case 'a':
                case 'b':
                case 'c':
                    return '2';
                case 'd':
                case 'e':
                case 'f':
                    return '3';
                case 'g':
                case 'h':
                case 'i':
                    return '4';
                case 'j':
                case 'k':
                case 'l':
                    return '5';
                case 'm':
                case 'n':
                case 'o':
                    return '6';
                case 'p':
                case 'q':
                case 'r':
                case 's':
                    return '7';
                case 't':
                case 'u':
                case 'v':
                    return '8';
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                    return '9';
                default:
                    // The digit 1 isn't mapped to any chars on a phone keypad.
                    // If the char isn't mapped to anything, respect it's existence by mapping it to a wildcard.
                    return '*';
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}