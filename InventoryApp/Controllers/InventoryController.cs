using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── List: সব Inventories ────────────────────────────
        public async Task<IActionResult> Index()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Items)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(inventories);
        }

        // ─── Create: নতুন Inventory ──────────────────────────
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreateInventoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var inventory = new Inventory
            {
                Title = model.Title,
                Description = model.Description,
                IsPublic = model.IsPublic,
                OwnerId = user!.Id
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = inventory.Id });
        }

        // ─── Details: একটা Inventory দেখো ───────────────────
        public async Task<IActionResult> Details(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Fields)
                .Include(i => i.Items)
                    .ThenInclude(item => item.FieldValues)
                        .ThenInclude(fv => fv.Field)
                .Include(i => i.Items)
                    .ThenInclude(item => item.CreatedBy)
                .Include(i => i.AccessList)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = currentUser?.Id == inventory.OwnerId;
            var hasAccess = inventory.IsPublic ||
                            isOwner ||
                            isAdmin ||
                            inventory.AccessList.Any(a => a.UserId == currentUser?.Id);

            var vm = new InventoryDetailsViewModel
            {
                Inventory = inventory,
                IsOwner = isOwner,
                IsAdmin = isAdmin,
                HasWriteAccess = hasAccess && User.Identity!.IsAuthenticated
            };

            return View(vm);
        }

        // ─── Edit: Inventory Settings ────────────────────────
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin)
                return Forbid();

            var vm = new EditInventoryViewModel
            {
                Id = inventory.Id,
                Title = inventory.Title,
                Description = inventory.Description,
                IsPublic = inventory.IsPublic,
                Version = inventory.Version
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(EditInventoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin)
                return Forbid();

            // Optimistic locking check
            if (inventory.Version != model.Version)
            {
                ModelState.AddModelError("", "This inventory was modified by someone else. Please reload and try again.");
                return View(model);
            }

            inventory.Title = model.Title;
            inventory.Description = model.Description;
            inventory.IsPublic = model.IsPublic;
            inventory.Version += 1;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = inventory.Id });
        }

        // ─── Delete: Inventory মুছো ──────────────────────────
        [Authorize]
        [HttpPost]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Items)
                    .ThenInclude(item => item.FieldValues)
                .Include(i => i.Items)
                    .ThenInclude(item => item.Likes)
                .Include(i => i.Fields)
                .Include(i => i.AccessList)
                .Include(i => i.Comments)
                .Include(i => i.InventoryTags)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            // সব related data আগে delete করো
            foreach (var item in inventory.Items)
            {
                _context.ItemFieldValues.RemoveRange(item.FieldValues);
                _context.Likes.RemoveRange(item.Likes);
            }

            _context.Items.RemoveRange(inventory.Items);
            _context.InventoryFields.RemoveRange(inventory.Fields);
            _context.InventoryAccesses.RemoveRange(inventory.AccessList);
            _context.Comments.RemoveRange(inventory.Comments);
            _context.InventoryTags.RemoveRange(inventory.InventoryTags);
            _context.Inventories.Remove(inventory);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}