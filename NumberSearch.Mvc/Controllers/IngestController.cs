using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IngestController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public IngestController(IConfiguration config)
        {
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            var ingests = await IngestStatistics.GetAllAsync(_postgresql).ConfigureAwait(false);
            var currentState = new List<(string, int)>();
            foreach (var provider in ingests.Select(x => x.IngestedFrom).Distinct())
            {
                var count = await PhoneNumber.GetCountByProvider(provider, _postgresql).ConfigureAwait(false);
                currentState.Add((provider, count));
            }

            var executive = await PhoneNumber.GetCountByNumberType("Executive", _postgresql).ConfigureAwait(false);
            var premium = await PhoneNumber.GetCountByNumberType("Premium", _postgresql).ConfigureAwait(false);
            var standard = await PhoneNumber.GetCountByNumberType("Standard", _postgresql).ConfigureAwait(false);

            var total = await PhoneNumber.GetTotal(_postgresql).ConfigureAwait(false);
            var numbersByAreaCode = new List<(int, int)>();

            foreach (var code in AreaCode.Priority)
            {
                numbersByAreaCode.Add((code, await PhoneNumber.GetCountByAreaCode(code, _postgresql).ConfigureAwait(false)));
            }

            return View("Index", new IngestResults
            {
                Ingests = ingests,
                CurrentState = currentState,
                PriorityAreaCodes = numbersByAreaCode,
                TotalPhoneNumbers = total,
                TotalExecutiveNumbers = executive,
                TotalPremiumNumbers = premium,
                TotalStandardNumbers = standard
            });
        }
    }
}