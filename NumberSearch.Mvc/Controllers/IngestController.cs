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
            return View("Index", ingests.ToArray());
        }
    }
}