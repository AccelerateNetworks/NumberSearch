using Microsoft.AspNetCore.Mvc;

using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IngestController : Controller
    {
        private readonly string _postgresql;

        public IngestController(MvcConfiguration mvcConfiguration)
        {
            _postgresql = mvcConfiguration.PostgresqlProd;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            var ingests = await IngestStatistics.GetAllAsync(_postgresql).ConfigureAwait(false);
            var currentState = await PhoneNumber.GetCountAllProvider(_postgresql).ConfigureAwait(false);
            var numberTypeCounts = await PhoneNumber.GetCountAllNumberType(_postgresql).ConfigureAwait(false);
            var numbersByAreaCode = await PhoneNumber.GetCountAllAreaCode(_postgresql).ConfigureAwait(false);
            var total = 0;
            foreach (var item in numberTypeCounts)
            {
                total += item.Count;
            }

            return View("Index", new IngestResults
            {
                Ingests = ingests,
                CurrentState = currentState,
                AreaCodes = numbersByAreaCode,
                TotalPhoneNumbers = total,
                TotalExecutiveNumbers = numberTypeCounts.Where(x => x.NumberType == "Executive").Select(x => x.Count).FirstOrDefault(),
                TotalPremiumNumbers = numberTypeCounts.Where(x => x.NumberType == "Premium").Select(x => x.Count).FirstOrDefault(),
                TotalStandardNumbers = numberTypeCounts.Where(x => x.NumberType == "Standard").Select(x => x.Count).FirstOrDefault(),
                TotalTollFreeNumbers = numberTypeCounts.Where(x => x.NumberType == "Tollfree").Select(x => x.Count).FirstOrDefault(),
            });
        }
    }
}