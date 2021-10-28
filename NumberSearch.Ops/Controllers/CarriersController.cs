using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.EFModels;
using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class CarriersController : Controller
    {
        private readonly numberSearchContext _context;

        public CarriersController(numberSearchContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("/Carriers")]
        // GET: CarriersController
        public async Task<IActionResult> Index()
        {
            return View(await _context.Carriers.ToListAsync());
        }

        [Authorize]
        [HttpGet("/Carriers/Details/{id}")]
        // GET: CarriersController/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Carriers
                .FirstOrDefaultAsync(m => m.CarrierId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [Authorize]
        [HttpGet("/Carriers/Create")]
        // GET: CarriersController/Create
        public ActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpGet("/Carriers/FromLookup/{lookupId}")]
        // GET: CarriersController/Create
        public async Task<IActionResult> CreateFromLookup(Guid lookupId)
        {
            var lookup = await _context.PhoneNumberLookups.Where(x => x.PhoneNumberLookupId == lookupId).FirstOrDefaultAsync();

            if (lookup is not null)
            {
                return View("Create", new Carrier
                {
                    Lec = lookup.Lec,
                    Spid = lookup.Spid,
                    Lectype = lookup.Lectype,
                    Ocn = lookup.Ocn
                });
            }
            else
            {
                return View("Create");
            }
        }

        // POST: CarriersController/Create
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Carriers/Create")]
        public async Task<IActionResult> Create([Bind("CarrierId,Ocn,Lec,Lectype,Spid,Name,Type,Ratecenter,Color,LogoLink,LastUpdated")] Carrier carrier)
        {
            if (ModelState.IsValid)
            {
                carrier.CarrierId = Guid.NewGuid();
                carrier.LastUpdated = DateTime.Now;
                _context.Add(carrier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(carrier);
        }

        [Authorize]
        [HttpGet("/Carriers/Edit/{id}")]
        // GET: CarriersController/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carrier = await _context.Carriers.FindAsync(id);
            var lookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == carrier.Ocn).ToListAsync();

            if (carrier == null)
            {
                return NotFound();
            }
            return View(new EditCarrier { Carrier = carrier, Lookups = lookups });
        }

        // POST: CarriersController/Edit/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Carriers/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, [Bind("CarrierId,Ocn,Lec,Lectype,Spid,Name,Type,Ratecenter,Color,LogoLink,LastUpdated")] Carrier carrier)
        {
            if (id != carrier.CarrierId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    carrier.LastUpdated = DateTime.Now;
                    _context.Update(carrier);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarrierExists(carrier.CarrierId))
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
            return View(carrier);
        }

        [Authorize]
        [HttpGet("/Carriers/Delete/{id}")]
        // GET: CarriersController/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Carriers
                .FirstOrDefaultAsync(m => m.CarrierId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: CarriersController/Delete/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Carriers/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _context.Carriers.FindAsync(id);
            _context.Carriers.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarrierExists(Guid id)
        {
            return _context.Carriers.Any(e => e.CarrierId == id);
        }
    }
}
