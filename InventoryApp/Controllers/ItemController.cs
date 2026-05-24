using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ItemController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─── Create Item ─────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Create(int inventoryId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .Include(i => i.AccessList)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;
            var hasAccess = inventory.IsPublic || isOwner || isAdmin ||
                            inventory.AccessList.Any(a => a.UserId == user?.Id);

            if (!hasAccess) return Forbid();

            var vm = new CreateItemViewModel
            {
                InventoryId = inventoryId,
                InventoryTitle = inventory.Title,
                CustomId = GenerateSimpleId(),
                Fields = inventory.Fields.OrderBy(f => f.Order).ToList()
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(int inventoryId, string customId,
            Dictionary<int, string> fieldValues)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .Include(i => i.AccessList)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;
            var hasAccess = inventory.IsPublic || isOwner || isAdmin ||
                            inventory.AccessList.Any(a => a.UserId == user?.Id);

            if (!hasAccess) return Forbid();

            // CustomId duplicate check
            var exists = await _context.Items
                .AnyAsync(i => i.InventoryId == inventoryId && i.CustomId == customId);

            if (exists)
            {
                TempData["Error"] = $"Custom ID '{customId}' already exists in this inventory.";
                return RedirectToAction("Create", new { inventoryId });
            }

            var item = new Item
            {
                InventoryId = inventoryId,
                CustomId = customId,
                CreatedById = user!.Id
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Field values save
            foreach (var field in inventory.Fields)
            {
                var value = fieldValues.ContainsKey(field.Id)
                    ? fieldValues[field.Id]
                    : "";

                _context.ItemFieldValues.Add(new ItemFieldValue
                {
                    ItemId = item.Id,
                    FieldId = field.Id,
                    Value = value
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Inventory", new { id = inventoryId });
        }

        // ─── Item Details ─────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items
                .Include(i => i.Inventory)
                    .ThenInclude(inv => inv.Fields)
                .Include(i => i.FieldValues)
                    .ThenInclude(fv => fv.Field)
                .Include(i => i.CreatedBy)
                .Include(i => i.Likes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == item.Inventory.OwnerId;
            var hasAccess = item.Inventory.IsPublic || isOwner || isAdmin ||
                            await _context.InventoryAccesses
                                .AnyAsync(a => a.InventoryId == item.InventoryId
                                            && a.UserId == user!.Id);

            var userLiked = user != null &&
                            item.Likes.Any(l => l.UserId == user.Id);

            var vm = new ItemDetailsViewModel
            {
                Item = item,
                IsOwner = isOwner,
                IsAdmin = isAdmin,
                HasWriteAccess = hasAccess && User.Identity!.IsAuthenticated,
                UserLiked = userLiked,
                LikeCount = item.Likes.Count
            };

            return View(vm);
        }

        // ─── Edit Item ────────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Items
                .Include(i => i.Inventory)
                    .ThenInclude(inv => inv.Fields)
                .Include(i => i.FieldValues)
                .Include(i => i.Inventory)
                    .ThenInclude(inv => inv.AccessList)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == item.Inventory.OwnerId;
            var hasAccess = item.Inventory.IsPublic || isOwner || isAdmin ||
                            item.Inventory.AccessList.Any(a => a.UserId == user?.Id);

            if (!hasAccess) return Forbid();

            var vm = new EditItemViewModel
            {
                Id = item.Id,
                CustomId = item.CustomId,
                InventoryId = item.InventoryId,
                InventoryTitle = item.Inventory.Title,
                Version = item.Version,
                Fields = item.Inventory.Fields.OrderBy(f => f.Order).ToList(),
                FieldValues = item.FieldValues
                    .ToDictionary(fv => fv.FieldId, fv => fv.Value ?? "")
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string customId,
            int version, Dictionary<int, string> fieldValues)
        {
            var item = await _context.Items
                .Include(i => i.Inventory)
                    .ThenInclude(inv => inv.AccessList)
                .Include(i => i.FieldValues)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == item.Inventory.OwnerId;
            var hasAccess = item.Inventory.IsPublic || isOwner || isAdmin ||
                            item.Inventory.AccessList.Any(a => a.UserId == user?.Id);

            if (!hasAccess) return Forbid();

            // Optimistic locking
            if (item.Version != version)
            {
                TempData["Error"] = "This item was modified by someone else. Please reload.";
                return RedirectToAction("Edit", new { id });
            }

            // CustomId duplicate check (excluding self)
            var exists = await _context.Items
                .AnyAsync(i => i.InventoryId == item.InventoryId
                            && i.CustomId == customId
                            && i.Id != id);
            if (exists)
            {
                TempData["Error"] = $"Custom ID '{customId}' already exists.";
                return RedirectToAction("Edit", new { id });
            }

            item.CustomId = customId;
            item.Version += 1;

             // update field values
            foreach (var fv in item.FieldValues)
            {
                if (fieldValues.ContainsKey(fv.FieldId))
                    fv.Value = fieldValues[fv.FieldId];
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.FieldValues) // fix delete issue by including related field values
                .Include(i => i.Likes)       // fix delete issue by including related likes
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == item.Inventory.OwnerId;
            var hasAccess = item.Inventory.IsPublic || isOwner || isAdmin ||
                            await _context.InventoryAccesses
                                .AnyAsync(a => a.InventoryId == item.InventoryId
                                            && a.UserId == user!.Id);

            if (!hasAccess) return Forbid();

            var inventoryId = item.InventoryId;

            // firstly,save related data delete
            _context.ItemFieldValues.RemoveRange(item.FieldValues);
            _context.Likes.RemoveRange(item.Likes);

            // than item delete
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Inventory", new { id = inventoryId });
        }

        // ─── Like / Unlike ────────────────────────────────────
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.ItemId == id && l.UserId == user!.Id);

            if (like == null)
            {
                _context.Likes.Add(new Like
                {
                    ItemId = id,
                    UserId = user!.Id
                });
            }
            else
            {
                _context.Likes.Remove(like);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        // ─── Helper: Simple ID Generator ─────────────────────
        private string GenerateSimpleId()
        {
            return "ITEM-" + DateTime.UtcNow.ToString("yyyyMMdd") +
                   "-" + new Random().Next(100, 999);
        }
    }
}