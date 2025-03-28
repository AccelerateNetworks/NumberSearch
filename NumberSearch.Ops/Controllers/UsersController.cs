﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {

        // GET: AspNetUsers
        [Authorize]
        [HttpGet("Users")]
        public async Task<IActionResult> Index()
        {
            var users = await userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (IdentityUser user in users)
            {
                var thisViewModel = new UserRolesViewModel
                {
                    User = user,
                    Roles = await userManager.GetRolesAsync(user)
                };
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }

        public class UserRolesViewModel
        {
            public IdentityUser User { get; set; } = null!;
            public IEnumerable<string> Roles { get; set; } = null!;
        }

        public class ManageUserRolesViewModel
        {
            public string RoleId { get; set; } = null!;
            public string RoleName { get; set; } = null!;
            public bool Selected { get; set; }
        }

        [Authorize]
        [HttpGet("Users/Manage")]
        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            var roles = await roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                };
                if (await userManager.IsInRoleAsync(user, role.Name ?? string.Empty))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("Users/Manage")]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet("Users/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [Authorize]
        [HttpPost("Users/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            await userManager.DeleteAsync(user ?? new());
            return RedirectToAction(nameof(Index));
        }
    }
}
