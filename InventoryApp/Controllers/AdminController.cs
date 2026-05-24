using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ─── User List ───────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName ?? "",
                    Email = user.Email ?? "",
                    IsBlocked = user.IsBlocked,
                    IsAdmin = roles.Contains("Admin"),
                    CreatedAt = user.CreatedAt
                });
            }

            return View(userViewModels);
        }

        // ─── Block User ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Block(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = true;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Index");
        }

        // ─── Unblock User ────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Unblock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = false;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Index");
        }

        // ─── Delete User ─────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Index");
        }

        // ─── Make Admin ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            return RedirectToAction("Index");
        }

        // ─── Remove Admin ────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                // admin can remove themselves as well (as per requirement)
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            return RedirectToAction("Index");
        }
    }
}