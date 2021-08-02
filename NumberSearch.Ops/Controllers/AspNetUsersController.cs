using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.EFModels;

namespace NumberSearch.Ops.Controllers
{
    public class AspNetUsersController : Controller
    {
        private readonly numberSearchContext _context;

        public AspNetUsersController(numberSearchContext context)
        {
            _context = context;
        }

        // GET: AspNetUsers
        [Authorize]
        [HttpGet("AspNetUsers")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.AspNetUsers.ToListAsync());
        }

        // GET: AspNetUsers/Details/5
        [Authorize]
        [HttpGet("AspNetUsers/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aspNetUser = await _context.AspNetUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (aspNetUser == null)
            {
                return NotFound();
            }

            return View(aspNetUser);
        }

        // GET: AspNetUsers/Create
        [Authorize]
        [HttpGet("AspNetUsers/Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: AspNetUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("AspNetUsers/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount")] AspNetUser aspNetUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(aspNetUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(aspNetUser);
        }

        // GET: AspNetUsers/Edit/5
        [Authorize]
        [HttpGet("AspNetUsers/Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aspNetUser = await _context.AspNetUsers.FindAsync(id);
            if (aspNetUser == null)
            {
                return NotFound();
            }
            return View(aspNetUser);
        }

        // POST: AspNetUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("AspNetUsers/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount")] AspNetUser aspNetUser)
        {
            if (id != aspNetUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(aspNetUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AspNetUserExists(aspNetUser.Id))
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
            return View(aspNetUser);
        }

        // GET: AspNetUsers/Delete/5
        [Authorize]
        [HttpGet("AspNetUsers/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aspNetUser = await _context.AspNetUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (aspNetUser == null)
            {
                return NotFound();
            }

            return View(aspNetUser);
        }

        // POST: AspNetUsers/Delete/5
        [Authorize]
        [HttpPost("AspNetUsers/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var aspNetUser = await _context.AspNetUsers.FindAsync(id);
            _context.AspNetUsers.Remove(aspNetUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AspNetUserExists(string id)
        {
            return _context.AspNetUsers.Any(e => e.Id == id);
        }
    }
}
