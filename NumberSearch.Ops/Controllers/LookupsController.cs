using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccelerateNetworks.Operations;
using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LookupsController : Controller
    {
        private readonly numberSearchContext _context;

        public LookupsController(numberSearchContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("/Lookups")]
        // GET: CarriersController
        public async Task<IActionResult> Index()
        {
            // Match lookups to existing carriers.
            //var carriers = await _context.Carriers.ToListAsync();
            //var lookups = await _context.PhoneNumberLookups.ToListAsync();

            //foreach (var item in lookups)
            //{
            //    var carrier = carriers.Where(x => x.Ocn == item.Ocn).FirstOrDefault();

            //    if (carrier is not null)
            //    {
            //        item.CarrierId = carrier.CarrierId;
            //        _context.Update(item);
            //    }
            //}

            //await _context.SaveChangesAsync();

            return View(await _context.PhoneNumberLookups.Where(x => x.CarrierId == null && !string.IsNullOrWhiteSpace(x.Ocn)).ToListAsync());
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

            var product = await _context.PhoneNumberLookups
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
            return View(new CreateLookup { Carriers = await _context.Carriers.ToArrayAsync() });
        }

        // POST: CarriersController/Create
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Lookups/Create")]
        public async Task<IActionResult> Create([Bind("PhoneNumberLookupId,DialedNumber,Lrn,Ocn,Lata,City,Ratecenter,State,Jurisdiction,Local,Lec,Lectype,Spid,Lidbname,LastPorted,IngestedFrom,DateIngested,CarrierId")] PhoneNumberLookup lookup)
        {
            if (ModelState.IsValid)
            {
                lookup.PhoneNumberLookupId = Guid.NewGuid();
                lookup.DateIngested = DateTime.Now;
                _context.Add(lookup);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lookup);
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

            var product = await _context.PhoneNumberLookups.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(new CreateLookup { Lookup = product, Carriers = await _context.Carriers.ToArrayAsync() });
        }

        // POST: CarriersController/Edit/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Lookups/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, [Bind("PhoneNumberLookupId,DialedNumber,Lrn,Ocn,Lata,City,Ratecenter,State,Jurisdiction,Local,Lec,Lectype,Spid,Lidbname,LastPorted,IngestedFrom,DateIngested,CarrierId")] PhoneNumberLookup lookup)
        {
            //if (id != lookup.CarrierId)
            //{
            //    return NotFound();
            //}

            if (ModelState.IsValid)
            {
                try
                {
                    lookup.DateIngested = DateTime.Now;
                    _context.Update(lookup);
                    await _context.SaveChangesAsync();
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
            return View(lookup);
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

            var product = await _context.PhoneNumberLookups
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
            var product = await _context.PhoneNumberLookups.FindAsync(id);
            if (product is not null)
            {
                _context.PhoneNumberLookups.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PhoneNumberLookupExists(Guid id)
        {
            return _context.PhoneNumberLookups.Any(e => e.PhoneNumberLookupId == id);
        }
    }
}
