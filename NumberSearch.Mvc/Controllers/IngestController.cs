using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;
using NumberSearch.Mvc.Models;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IngestController(MvcConfiguration mvcConfiguration) : Controller
    {
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;

        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        //[OutputCache(Duration = 30)]
        public async Task<IActionResult> IndexAsync()
        {
            var ingests = await IngestStatistics.GetAllAsync(_postgresql);
            var fpc = await IngestStatistics.GetAllFirstComAsync(_postgresql);
            var fpcPriority = await IngestStatistics.GetAllFirstComPriorityAsync(_postgresql);
            var bulkVS = await IngestStatistics.GetAllBulkVSAsync(_postgresql);
            var bulkVSPriority = await IngestStatistics.GetAllBulkVSPriorityAsync(_postgresql);
            var currentState = await PhoneNumber.GetCountAllProvider(_postgresql);
            var numberTypeCounts = await PhoneNumber.GetCountAllNumberType(_postgresql);
            var numbersByAreaCode = await PhoneNumber.GetCountAllAreaCode(_postgresql);
            var npasOwned = await PhoneNumber.GetCountAllAreaCodeOwnedNumber(_postgresql);
            var npasBulkVS = await PhoneNumber.GetCountAllAreaCodeBulkVS(_postgresql);
            var npasFPC = await PhoneNumber.GetCountAllAreaCodeFPC(_postgresql);

            List<PhoneNumber.CountNPA> priority = [];
            foreach (var code in AreaCode.Priority)
            {
                var match = numbersByAreaCode.FirstOrDefault(x => x.NPA == $"{code}");
                priority.Add(new PhoneNumber.CountNPA { NPA = $"{code}", Count = match?.Count ?? 0 });
            }

            var total = 0;
            foreach (var item in numberTypeCounts)
            {
                total += item.Count;
            }

            return View("Index", new IngestResults
            {
                Ingests = [..ingests],
                FirstPointComIngest = [..fpc],
                FirstPointComPriorityIngest = [..fpcPriority],
                BulkVSIngest = [..bulkVS],
                BulkVSPriorityIngest = [..bulkVSPriority],
                CurrentState = currentState,
                AreaCodes = numbersByAreaCode,
                PriorityAreaCodes = [.. priority],
                OwnedAreaCodes = [.. npasOwned],
                BulkVSAreaCodes = [.. npasBulkVS],
                FirstPointComAreaCodes = [.. npasFPC],
                TotalPhoneNumbers = total,
                TotalExecutiveNumbers = numberTypeCounts.Where(x => x.NumberType == "Executive").Select(x => x.Count).FirstOrDefault(),
                TotalPremiumNumbers = numberTypeCounts.Where(x => x.NumberType == "Premium").Select(x => x.Count).FirstOrDefault(),
                TotalStandardNumbers = numberTypeCounts.Where(x => x.NumberType == "Standard").Select(x => x.Count).FirstOrDefault(),
                TotalTollFreeNumbers = numberTypeCounts.Where(x => x.NumberType == "Tollfree").Select(x => x.Count).FirstOrDefault(),
            });
        }

        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        [OutputCache(Duration = 30)]
        public async Task<IActionResult> SalesDashboardAsync()
        {
            var orders = await Order.GetAllAsync(_postgresql);

            return View("Sales", new SalesDashboard
            {
                Orders = orders.ToArray(),
            });
        }
    }
}