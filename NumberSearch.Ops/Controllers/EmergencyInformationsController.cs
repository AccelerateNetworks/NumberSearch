using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Controllers
{
    public class EmergencyInformationsController : Controller
    {
        private readonly numberSearchContext _context;

        public EmergencyInformationsController(numberSearchContext context)
        {
            _context = context;
        }

        // GET: EmergencyInformations
        public async Task<IActionResult> Index()
        {
              return _context.EmergencyInformation.OrderBy(x => x.DateIngested) != null ? 
                          View(await _context.EmergencyInformation.ToListAsync()) :
                          Problem("Entity set 'numberSearchContext.EmergencyInformations'  is null.");
        }

        // GET: EmergencyInformations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await _context.EmergencyInformation
                .FirstOrDefaultAsync(m => m.EmergencyInformationId == id);
            if (emergencyInformation == null)
            {
                return NotFound();
            }

            return View(emergencyInformation);
        }

        // GET: EmergencyInformations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EmergencyInformations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmergencyInformationId,DialedNumber,IngestedFrom,DateIngested,TeliId,FullName,Address,City,State,Zip,UnitType,UnitNumber,CreatedDate,ModifyDate,AlertGroup,Note")] EmergencyInformation emergencyInformation)
        {
            if (ModelState.IsValid)
            {
                emergencyInformation.EmergencyInformationId = Guid.NewGuid();
                _context.Add(emergencyInformation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(emergencyInformation);
        }

        // GET: EmergencyInformations/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await _context.EmergencyInformation.FindAsync(id);
            if (emergencyInformation == null)
            {
                return NotFound();
            }
            return View(emergencyInformation);
        }

        // POST: EmergencyInformations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("EmergencyInformationId,DialedNumber,IngestedFrom,DateIngested,TeliId,FullName,Address,City,State,Zip,UnitType,UnitNumber,CreatedDate,ModifyDate,AlertGroup,Note")] EmergencyInformation emergencyInformation)
        {
            if (id != emergencyInformation.EmergencyInformationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(emergencyInformation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmergencyInformationExists(emergencyInformation.EmergencyInformationId))
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
            return View(emergencyInformation);
        }

        // GET: EmergencyInformations/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await _context.EmergencyInformation
                .FirstOrDefaultAsync(m => m.EmergencyInformationId == id);
            if (emergencyInformation == null)
            {
                return NotFound();
            }

            return View(emergencyInformation);
        }

        // POST: EmergencyInformations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.EmergencyInformation == null)
            {
                return Problem("Entity set 'numberSearchContext.EmergencyInformations'  is null.");
            }
            var emergencyInformation = await _context.EmergencyInformation.FindAsync(id);
            if (emergencyInformation != null)
            {
                _context.EmergencyInformation.Remove(emergencyInformation);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmergencyInformationExists(Guid id)
        {
          return (_context.EmergencyInformation?.Any(e => e.EmergencyInformationId == id)).GetValueOrDefault();
        }
    }
}
