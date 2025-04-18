﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RolesController(RoleManager<IdentityRole> roleManager) : Controller
    {
        [Authorize]
        [HttpGet("Roles")]
        public async Task<IActionResult> Index()
        {
            return View(await roleManager.Roles.ToListAsync());
        }

        [Authorize]
        [HttpGet("Roles/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost("Roles/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Name)
        {
            if (Name != null)
            {
                await roleManager.CreateAsync(new IdentityRole(Name.Trim()));
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet("Roles/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await roleManager.FindByIdAsync(id);
            return View(role);
        }

        [Authorize]
        [HttpPost("Roles/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            await roleManager.DeleteAsync(role ?? new());
            return RedirectToAction(nameof(Index));
        }
    }
}
