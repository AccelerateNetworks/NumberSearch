using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class EmergencyInformationsController(numberSearchContext context) : Controller
    {

        // GET: EmergencyInformations
        public async Task<IActionResult> Index()
        {
              return context.EmergencyInformation.OrderBy(x => x.DateIngested) != null ? 
                          View(await context.EmergencyInformation.ToListAsync()) :
                          Problem("Entity set 'numberSearchContext.EmergencyInformations'  is null.");
        }

        // GET: EmergencyInformations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await context.EmergencyInformation
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
                context.Add(emergencyInformation);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(emergencyInformation);
        }

        // GET: EmergencyInformations/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await context.EmergencyInformation.FindAsync(id);
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
                    context.Update(emergencyInformation);
                    await context.SaveChangesAsync();
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
            if (id == null || context.EmergencyInformation == null)
            {
                return NotFound();
            }

            var emergencyInformation = await context.EmergencyInformation
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
            if (context.EmergencyInformation == null)
            {
                return Problem("Entity set 'numberSearchContext.EmergencyInformations'  is null.");
            }
            var emergencyInformation = await context.EmergencyInformation.FindAsync(id);
            if (emergencyInformation != null)
            {
                context.EmergencyInformation.Remove(emergencyInformation);
            }
            
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmergencyInformationExists(Guid id)
        {
          return (context.EmergencyInformation?.Any(e => e.EmergencyInformationId == id)).GetValueOrDefault();
        }
    }
}
