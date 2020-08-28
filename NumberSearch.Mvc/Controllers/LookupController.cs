using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class LookupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;

        public LookupController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        public async Task<IActionResult> IndexAsync(string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var numberCanidates = dialedNumber.Trim().Split(" ");

                var parsedNumbers = new List<string>();

                foreach (var query in numberCanidates)
                {
                    // Parse the query.
                    var converted = new List<char>();
                    foreach (var letter in query)
                    {
                        // Allow digits.
                        if (char.IsDigit(letter))
                        {
                            converted.Add(letter);
                        }
                        // Drop everything else.
                    }

                    // Drop leading 1's to improve the copy/paste experiance.
                    if (converted.Count >= 10 && converted[0] == '1')
                    {
                        converted.Remove('1');
                    }

                    // Only if its a perfect number do we want to query for it.
                    if (converted.Count == 10)
                    {
                        parsedNumbers.Add(new string(converted.ToArray()));
                    }
                }

                var results = new List<LrnLookup>();

                foreach (var number in parsedNumbers)
                {
                    var checkNumber = await LrnLookup.GetAsync(number, _teleToken).ConfigureAwait(false);
                    checkNumber.data.DialedNumber = number;
                    results.Add(checkNumber);
                }

                return View("Index", new LookupResults
                {
                    DialedNumber = dialedNumber,
                    Lookups = results
                });
            }
            else
            {
                return View("Index");
            }
        }
    }
}
