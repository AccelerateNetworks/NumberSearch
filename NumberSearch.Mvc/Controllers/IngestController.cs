using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.Controllers
{
    public class IngestController : Controller
    {
        private readonly IConfiguration configuration;

        public IngestController(IConfiguration config)
        {
            configuration = config;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var ingests = await IngestStatistics.GetAllAsync(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
            var currentState = new List<(string, int)>();
            foreach (var provider in ingests.Select(x => x.IngestedFrom).Distinct())
            {
                var count = await PhoneNumber.GetCountByProvider(provider, configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);
                currentState.Add((provider, count));
            }

            var total = await PhoneNumber.GetTotal(configuration.GetConnectionString("PostgresqlProd")).ConfigureAwait(false);

            return View("Index", new IngestResults
            {
                Ingests = ingests,
                CurrentState = currentState,
                TotalPhoneNumbers = total
            });
        }
    }
}