using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccelerateNetworks.Operations;
using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;
using NumberSearch.DataAccess;
using System.Threading;

namespace NumberSearch.Ops.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LookupsController(numberSearchContext context) : Controller
    {
        [Authorize]
        [HttpGet("/Lookups")]
        // GET: CarriersController
        public async Task<IActionResult> Index()
        {
            return View(await context.PhoneNumberLookups.Where(x => x.CarrierId == null && !string.IsNullOrWhiteSpace(x.Ocn)).Take(100).ToListAsync());
        }

        [Authorize]
        [HttpGet("/Lookups/Details/{id}")]
        // GET: CarriersController/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await context.PhoneNumberLookups
                .FirstOrDefaultAsync(m => m.PhoneNumberLookupId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [Authorize]
        [HttpGet("/Lookups/Create")]
        // GET: CarriersController/Create
        public async Task<IActionResult> Create()
        {
            return View(new CreateLookup { Carriers = await context.Carriers.ToArrayAsync() });
        }

        // POST: CarriersController/Create
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Lookups/Create")]
        public async Task<IActionResult> Create([Bind("PhoneNumberLookupId,DialedNumber,Lrn,Ocn,Lata,City,Ratecenter,State,Jurisdiction,Local,Lec,Lectype,Spid,Lidbname,LastPorted,IngestedFrom,DateIngested,CarrierId")] AccelerateNetworks.Operations.PhoneNumberLookup lookup)
        {
            if (ModelState.IsValid)
            {
                lookup.PhoneNumberLookupId = Guid.NewGuid();
                lookup.DateIngested = DateTime.Now;
                context.Add(lookup);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lookup);
        }

        [Authorize]
        [HttpGet("/Lookups/OCNMatchLookups")]
        public async Task<IActionResult> OCNMatchLookups()
        {
            // Match lookups to existing carriers.
            var carriers = await context.Carriers.ToListAsync();

            var lookupsWithoutCarriers = await context.PhoneNumberLookups.Where(x => x.CarrierId == null || x.CarrierId == Guid.Empty).ToArrayAsync();

            foreach (var item in carriers)
            {
                var lookups = lookupsWithoutCarriers.Where(x => x.Ocn == item.Ocn && x.CarrierId != item.CarrierId);

                foreach (var look in lookups)
                {
                    if (look.CarrierId != item.CarrierId)
                    {
                        look.CarrierId = item.CarrierId;
                    }
                }
            }
            await context.SaveChangesAsync();

            return View("Index", await context.PhoneNumberLookups.Where(x => x.CarrierId == null && !string.IsNullOrWhiteSpace(x.Ocn)).Take(100).ToListAsync());
        }

        [Authorize]
        [HttpGet("/Lookups/CarrierMatchesLookupOCN")]
        public async Task<IActionResult> CarrierMatchesLookupOCN()
        {
            var carriers = await context.Carriers.ToListAsync();

            foreach (var carrier in carriers)
            {
                // If the OCNs don't match then this is not the right Carrier for the lookup.
                var lookups = await context.PhoneNumberLookups.Where(x => x.CarrierId == carrier.CarrierId && x.Ocn != carrier.Ocn).ToArrayAsync();

                foreach (var lookup in lookups)
                {
                    lookup.CarrierId = null;

                    // Let find the right Carrier based on the OCN of the lookup, if we can.
                    var ocnMatch = carriers.FirstOrDefault(x => x.Ocn == lookup.Ocn);

                    if (ocnMatch is not null && ocnMatch.Ocn == lookup.Ocn)
                    {
                        lookup.CarrierId = ocnMatch.CarrierId;
                    }
                }
            }
            await context.SaveChangesAsync();

            return View("Index", await context.PhoneNumberLookups.Where(x => x.CarrierId == null && !string.IsNullOrWhiteSpace(x.Ocn)).Take(100).ToListAsync());
        }

        [Authorize]
        [HttpGet("/Lookups/Edit/{id}")]
        // GET: CarriersController/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await context.PhoneNumberLookups.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(new CreateLookup { Lookup = product, Carriers = await context.Carriers.ToArrayAsync() });
        }

        // POST: CarriersController/Edit/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Lookups/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, [Bind("PhoneNumberLookupId,DialedNumber,Lrn,Ocn,Lata,City,Ratecenter,State,Jurisdiction,Local,Lec,Lectype,Spid,Lidbname,LastPorted,IngestedFrom,DateIngested,CarrierId")] AccelerateNetworks.Operations.PhoneNumberLookup lookup)
        {
            if (id != lookup.PhoneNumberLookupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    lookup.DateIngested = DateTime.Now;
                    context.Update(lookup);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhoneNumberLookupExists(lookup.PhoneNumberLookupId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(new CreateLookup { Lookup = lookup, Carriers = await context.Carriers.ToArrayAsync() });
        }

        [Authorize]
        [HttpGet("/Lookups/Delete/{id}")]
        // GET: CarriersController/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await context.PhoneNumberLookups
                .FirstOrDefaultAsync(m => m.PhoneNumberLookupId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: CarriersController/Delete/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Lookups/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await context.PhoneNumberLookups.FindAsync(id);
            if (product is not null)
            {
                context.PhoneNumberLookups.Remove(product);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PhoneNumberLookupExists(Guid id)
        {
            return context.PhoneNumberLookups.Any(e => e.PhoneNumberLookupId == id);
        }
    }
}
