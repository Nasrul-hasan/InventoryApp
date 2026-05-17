using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    [Authorize]
    public class FieldController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FieldController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── Field List for an Inventory ─────────────────────
        public async Task<IActionResult> Index(int inventoryId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            ViewBag.Inventory = inventory;
            return View(inventory.Fields.OrderBy(f => f.Order).ToList());
        }

        // ─── Add Field ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Add(int inventoryId, string title,
            string? description, FieldType fieldType, bool showInTable)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            // Field type limit check (max 3 of each type)
            var typeCount = inventory.Fields.Count(f => f.FieldType == fieldType);
            if (typeCount >= 3)
            {
                TempData["Error"] = $"Maximum 3 fields of type '{fieldType}' allowed.";
                return RedirectToAction("Index", new { inventoryId });
            }

            var maxOrder = inventory.Fields.Any()
                ? inventory.Fields.Max(f => f.Order)
                : 0;

            var field = new InventoryField
            {
                Title = title,
                Description = description,
                FieldType = fieldType,
                ShowInTable = showInTable,
                InventoryId = inventoryId,
                Order = maxOrder + 1
            };

            _context.InventoryFields.Add(field);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId });
        }

        // ─── Delete Field ────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var field = await _context.InventoryFields
                .Include(f => f.Inventory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == field.Inventory!.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            _context.InventoryFields.Remove(field);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId = field.InventoryId });
        }

        // ─── Toggle ShowInTable ──────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleShow(int id)
        {
            var field = await _context.InventoryFields
                .Include(f => f.Inventory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null) return NotFound();

            field.ShowInTable = !field.ShowInTable;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId = field.InventoryId });
        }
    }
}