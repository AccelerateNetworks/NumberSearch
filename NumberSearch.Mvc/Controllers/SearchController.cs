using FirstCom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class SearchController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly Guid token;

        private readonly Credentials pComNetCredentials;
        private static IEnumerable<PhoneNumber> pComNetSearchResults;
        private static IEnumerable<PhoneNumber> teleSearchResults;
        private static IEnumerable<PhoneNumber> bulkVSSearchResults;
        private List<PhoneNumber> phoneNumbers = new List<PhoneNumber>();
        private readonly string bulkVSKey;
        private readonly string bulkVSSecret;

        public SearchController(IConfiguration config)
        {
            configuration = config;
            token = Guid.Parse(configuration.GetConnectionString("TeleAPI"));
            pComNetCredentials = new Credentials
            {
                Username = config.GetConnectionString("PComNetUsername"),
                Password = config.GetConnectionString("PComNetPassword")
            };
            bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
        }

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        public async Task<IActionResult> Index(string query)
        {
            // Fail fast
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Index");
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

            // This service can't handle partial queries or queries containing wildcards.
            if (!converted.Contains('*'))
            {
                switch (converted.Count)
                {
                    case 10:
                        // Supply the whole number if it exists.
                        pComNetSearchResults = await NpaNxxFirstPointCom
                            .GetAsync(string.Empty, string.Empty, new string(converted.ToArray()), pComNetCredentials.Username, pComNetCredentials.Password);
                        break;
                    case 6:
                        // Supply the first six of the number if it exists.
                        pComNetSearchResults = await NpaNxxFirstPointCom
                            .GetAsync(new string(converted.ToArray()).Substring(0, 3), new string(converted.ToArray()).Substring(4), string.Empty, pComNetCredentials.Username, pComNetCredentials.Password);
                        break;
                    case 3:
                        // Supply the area code if it exists.
                        pComNetSearchResults = await NpaNxxFirstPointCom
                            .GetAsync(new string(converted.ToArray()), string.Empty, string.Empty, pComNetCredentials.Username, pComNetCredentials.Password);
                        bulkVSSearchResults = await NpaNxxBulkVS.GetAsync(new string(converted.ToArray()), bulkVSKey, bulkVSSecret);
                        break;
                }
            }

            // Submit the parsed query to remote API.
            teleSearchResults = await LocalNumberTeleMessage.GetAsync(query, token);

            foreach (var item in teleSearchResults)
            {
                phoneNumbers.Add(item);
            }

            // If we got results from PCom, add them to our Tele search results.
            foreach (var item in pComNetSearchResults)
            {
                phoneNumbers.Add(item);
            }

            // If we got results from BulkVS add them to our Tele search results.
            foreach (var item in bulkVSSearchResults)
            {
                phoneNumbers.Add(item);
            }

            return View("Index", new SearchResults
            {
                CleanQuery = new string(converted.ToArray()),
                PhoneNumbers = phoneNumbers.ToArray(),
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
    }
}
