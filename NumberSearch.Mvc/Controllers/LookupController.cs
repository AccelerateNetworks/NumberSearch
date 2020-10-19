using BulkVS;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Data247;
using NumberSearch.Mvc.Models;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    public class LookupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly string _bulkVSKey;
        private readonly string _data247username;
        private readonly string _data247password;

        public LookupController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
        }

        public async Task<IActionResult> IndexAsync(string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var numberCanidates = dialedNumber.Trim().Replace(") ", "", StringComparison.CurrentCultureIgnoreCase).Replace("\r\n", " ", StringComparison.CurrentCultureIgnoreCase).Split(" ");

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
                    if (string.IsNullOrWhiteSpace(checkNumber.data.lrn))
                    {
                        var bulkResult = await LrnBulkCnam.GetAsync(number, _bulkVSKey).ConfigureAwait(false);

                        checkNumber.data.spid = bulkResult.spid;
                        checkNumber.data.spid_name = bulkResult.lec;
                        checkNumber.data.port_date = bulkResult.lrn;
                    }

                    var numberName = await LIDBLookup.GetAsync(number, _data247username, _data247password).ConfigureAwait(false);

                    checkNumber.data.DialedNumber = number;
                    checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;
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
