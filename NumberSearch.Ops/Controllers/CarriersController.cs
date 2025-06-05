using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Ops.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CarriersController(numberSearchContext context) : Controller
    {
        private readonly numberSearchContext _context = context;

        [Authorize]
        [HttpGet("/Carriers")]
        // GET: CarriersController
        public async Task<IActionResult> Index()
        {
            return View(await _context.Carriers.OrderBy(x => x.Name).ToListAsync());
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
            var lookup = await _context.PhoneNumberLookups.FirstOrDefaultAsync(x => x.PhoneNumberLookupId == lookupId);

            if (lookup is not null)
            {
                var relatedLookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == lookup.Ocn).Take(100).ToArrayAsync();
                if (string.IsNullOrWhiteSpace(lookup.Lec))
                {
                    return View("Create", new CreateCarrier
                    {
                        Lookups = relatedLookups,
                        Carrier = new()
                        {
                            Lec = lookup.Lec,
                            Spid = lookup.Spid,
                            Lectype = lookup.Lectype,
                            Ocn = lookup.Ocn
                        }
                    });
                }
                else
                {
                    string[] carrierQuery = lookup.Lec.Split(' ');
                    string query = carrierQuery[0].ToLowerInvariant();
                    var relatedCarriers = await _context.Carriers.ToArrayAsync();
                    relatedCarriers = [.. relatedCarriers.AsValueEnumerable().Where(x => x.Lec != null && x.Lec.Contains(query, StringComparison.InvariantCultureIgnoreCase))];
                    if (relatedCarriers.Length != 0)
                    {
                        return View("Create", new CreateCarrier
                        {
                            Lookups = relatedLookups,
                            Carriers = relatedCarriers,
                            Carrier = new()
                            {
                                Lec = lookup.Lec,
                                Spid = lookup.Spid,
                                Lectype = lookup.Lectype,
                                Ocn = lookup.Ocn,
                                Color = relatedCarriers?.AsValueEnumerable().FirstOrDefault()?.Color,
                                LogoLink = relatedCarriers?.AsValueEnumerable().FirstOrDefault()?.LogoLink,
                                Name = relatedCarriers?.AsValueEnumerable().FirstOrDefault()?.Name,
                                Type = relatedCarriers?.AsValueEnumerable().FirstOrDefault()?.Type,
                            }
                        });
                    }
                    else
                    {
                        relatedCarriers = await _context.Carriers.ToArrayAsync();

                        return View("Create", new CreateCarrier
                        {
                            Lookups = relatedLookups,
                            Carriers = relatedCarriers,
                            Carrier = new()
                            {
                                Lec = lookup.Lec,
                                Spid = lookup.Spid,
                                Lectype = lookup.Lectype,
                                Ocn = lookup.Ocn
                            }
                        });
                    }
                }
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

                // Updated all the lookups related to this OCN.
                var relatedLookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == carrier.Ocn).ToListAsync();
                foreach (var relatedLookup in relatedLookups)
                {
                    relatedLookup.CarrierId = carrier.CarrierId;
                }
                _context.UpdateRange(relatedLookups);
                await _context.SaveChangesAsync();
                return View("Edit", new EditCarrier { Carrier = carrier, Lookups = [.. relatedLookups], Message = $"Created a new Carrier for OCN {carrier.Ocn}!" });
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

            if (carrier is null)
            {
                return NotFound();
            }
            else
            {
                var lookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == carrier.Ocn).ToArrayAsync();
                return View(new EditCarrier { Carrier = carrier, Lookups = lookups });
            }
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Carriers/Edit/")]
        public Task<IActionResult> Edit([Bind("CarrierId,Ocn,Lec,Lectype,Spid,Name,Type,Ratecenter,Color,LogoLink,LastUpdated")] Carrier carrier)
        {
            return Edit(carrier.CarrierId, carrier);
        }


        // POST: CarriersController/Edit/5
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("/Carriers/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, [Bind("CarrierId,Ocn,Lec,Lectype,Spid,Name,Type,Ratecenter,Color,LogoLink,LastUpdated")] Carrier carrier)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    carrier.LastUpdated = DateTime.Now;
                    _context.Update(carrier);

                    // Updated all the lookups related to this OCN.
                    var relatedLookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == carrier.Ocn).ToListAsync();
                    foreach (var relatedLookup in relatedLookups)
                    {
                        relatedLookup.CarrierId = carrier.CarrierId;
                    }
                    _context.UpdateRange(relatedLookups);
                    await _context.SaveChangesAsync();

                    return View("Edit", new EditCarrier { Carrier = carrier, Lookups = [.. relatedLookups], Message = $"Saved your changes to OCN {carrier.Ocn}!" });

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
            }
            return View("Edit", new EditCarrier { Carrier = carrier, Lookups = await _context.PhoneNumberLookups.Where(x => x.Ocn == carrier.Ocn).ToArrayAsync(), Message = $"Failed to save your changes to OCN {carrier.Ocn}!" });
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
            if (product is not null)
            {
                _context.Carriers.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CarrierExists(Guid id)
        {
            return _context.Carriers.Any(e => e.CarrierId == id);
        }
    }
}
