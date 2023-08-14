using AccelerateNetworks.Operations;

using Flurl.Http;

using Microsoft.AspNetCore.Mvc;

using Models;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class MessagingController : Controller
    {
        private readonly numberSearchContext _context;
        private readonly OpsConfig _config;
        private readonly string _baseUrl;
        private readonly string _messagingToken;

        public MessagingController(numberSearchContext context, OpsConfig opsConfig)
        {
            _context = context;
            _config = opsConfig;
            _baseUrl = opsConfig.MessagingURL;
            _messagingToken = opsConfig.MessagingAPIJWT;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var stats = await $"{_baseUrl}client/usage".WithOAuthBearerToken(_messagingToken).GetJsonAsync<UsageSummary[]>();
            var failures = await $"{_baseUrl}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(_messagingToken).GetJsonAsync<MessageRecord[]>();

            return View(new MessagingResult { UsageSummary = stats.OrderByDescending(x => x.OutboundSMSCount).ToArray(), FailedMessages = failures.OrderByDescending(x => x.DateReceivedUTC).ToArray() });
        }
    }
}
