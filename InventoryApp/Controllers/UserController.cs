using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── Personal Page ───────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            // নিজের inventories
            var ownedInventories = await _context.Inventories
                .Include(i => i.Items)
                .Where(i => i.OwnerId == user!.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Write access আছে এমন inventories
            var accessInventories = await _context.InventoryAccesses
                .Include(a => a.Inventory)
                    .ThenInclude(i => i!.Items)
                .Include(a => a.Inventory)
                    .ThenInclude(i => i!.Owner)
                .Where(a => a.UserId == user!.Id)
                .OrderByDescending(a => a.Inventory!.CreatedAt)
                .ToListAsync();

            var vm = new UserProfileViewModel
            {
                User = user!,
                OwnedInventories = ownedInventories,
                AccessInventories = accessInventories
                    .Select(a => a.Inventory!)
                    .ToList()
            };

            return View(vm);
        }

        // ─── Public Profile ──────────────────────────────────
        public async Task<IActionResult> Public(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var inventories = await _context.Inventories
                .Include(i => i.Items)
                .Where(i => i.OwnerId == id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var vm = new UserProfileViewModel
            {
                User = user,
                OwnedInventories = inventories,
                AccessInventories = new List<Inventory>()
            };

            return View(vm);
        }
    }
}