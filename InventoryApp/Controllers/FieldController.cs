using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    // Only logged-in users can access field management
    [Authorize]
    public class FieldController : Controller
    {
        // Database context for inventory and field data
        private readonly ApplicationDbContext _context;
        // Identity service for getting current user information
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor Dependency Injection
        public FieldController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── Field List for an Inventory ─────────────────────
        // Display all custom fields for a specific inventory
        public async Task<IActionResult> Index(int inventoryId)
        {
            // Load inventory with related fields
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);
            // Return 404 if inventory does not exist
            if (inventory == null) return NotFound();
            // Get current user and check permission
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            // Only inventory owner or admin can manage fields
            if (!isOwner && !isAdmin) return Forbid();

            // Pass inventory data to the view
            ViewBag.Inventory = inventory;
            // Show fields ordered by display order
            return View(inventory.Fields.OrderBy(f => f.Order).ToList());
        }

        // ─── Add Field ───────────────────────────────────────
        [HttpPost]
        // Add a new custom field to an inventory
        public async Task<IActionResult> Add(int inventoryId, string title,
            string? description, FieldType fieldType, bool showInTable)
        {
            // Load inventory with existing fields
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            // Check whether current user is owner or admin
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

            // Find last field order number
            var maxOrder = inventory.Fields.Any()
                ? inventory.Fields.Max(f => f.Order)
                : 0;

            // Create new inventory field
            var field = new InventoryField
            {
                Title = title,
                Description = description,
                FieldType = fieldType,
                ShowInTable = showInTable,
                InventoryId = inventoryId,
                Order = maxOrder + 1
            };
            // Save field to database
            _context.InventoryFields.Add(field);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId });
        }

        // ─── Delete Field ────────────────────────────────────
        [HttpPost]
        // Delete an existing custom field
        public async Task<IActionResult> Delete(int id)
        {
            // Load field with its parent inventory
            var field = await _context.InventoryFields
                .Include(f => f.Inventory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null) return NotFound();
            // Check whether current user is owner or admin
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == field.Inventory!.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            // Remove field from database
            _context.InventoryFields.Remove(field);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId = field.InventoryId });
        }

        // ─── Toggle ShowInTable ──────────────────────────────
        [HttpPost]
        // Show or hide a field in inventory table view
        public async Task<IActionResult> ToggleShow(int id)
        {
            // Find selected field with related inventory
            var field = await _context.InventoryFields
                .Include(f => f.Inventory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null) return NotFound();

            // Toggle field visibility in table
            field.ShowInTable = !field.ShowInTable;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { inventoryId = field.InventoryId });
        }
    }
}