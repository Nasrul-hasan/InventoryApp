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

        // ─── List ────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Items)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(inventories);
        }

        // ─── Create GET ──────────────────────────────────────
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // ─── Create POST ─────────────────────────────────────
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
                Category = model.Category,
                ImageUrl = model.ImageUrl,
                OwnerId = user!.Id
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            await HandleTags(inventory.Id, model.Tags);

            return RedirectToAction("Details", new { id = inventory.Id });
        }

        // ─── Details ─────────────────────────────────────────
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
                .Include(i => i.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = currentUser?.Id == inventory.OwnerId;
            var hasAccess = (inventory.IsPublic && User.Identity!.IsAuthenticated) ||
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

        // ─── Edit GET ────────────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.InventoryTags)
                    .ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            var currentTags = inventory.InventoryTags
                .Select(it => it.Tag?.Name)
                .Where(t => t != null)
                .ToList();

            var vm = new EditInventoryViewModel
            {
                Id = inventory.Id,
                Title = inventory.Title,
                Description = inventory.Description,
                IsPublic = inventory.IsPublic,
                Category = inventory.Category,
                ImageUrl = inventory.ImageUrl,
                Tags = string.Join(", ", currentTags),
                Version = inventory.Version
            };

            return View(vm);
        }

        // ─── Edit POST ───────────────────────────────────────
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

            if (!isOwner && !isAdmin) return Forbid();

            if (inventory.Version != model.Version)
            {
                ModelState.AddModelError("", "This inventory was modified by someone else. Please reload.");
                return View(model);
            }

            inventory.Title = model.Title;
            inventory.Description = model.Description;
            inventory.IsPublic = model.IsPublic;
            inventory.Category = model.Category;
            inventory.ImageUrl = model.ImageUrl;
            inventory.Version += 1;

            // Tags update
            var existingTags = await _context.InventoryTags
                .Where(it => it.InventoryId == inventory.Id)
                .ToListAsync();
            _context.InventoryTags.RemoveRange(existingTags);
            await _context.SaveChangesAsync();
            await HandleTags(inventory.Id, model.Tags);

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = inventory.Id });
        }

        // ─── Delete ──────────────────────────────────────────
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

        // ─── Access Management ────────────────────────────────
        [Authorize]
        public async Task<IActionResult> Access(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.AccessList)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            return View(inventory);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddAccess(int inventoryId, string email)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin) return Forbid();

            // Email দিয়ে user খোঁজো
            var targetUser = await _userManager.FindByEmailAsync(email);
            if (targetUser == null)
            {
                TempData["Error"] = $"User '{email}' not found.";
                return RedirectToAction("Access", new { id = inventoryId });
            }

            // Already has access?
            var exists = await _context.InventoryAccesses
                .AnyAsync(a => a.InventoryId == inventoryId
                            && a.UserId == targetUser.Id);

            if (!exists)
            {
                _context.InventoryAccesses.Add(new InventoryAccess
                {
                    InventoryId = inventoryId,
                    UserId = targetUser.Id
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Access", new { id = inventoryId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveAccess(int inventoryId, string userId)
        {
            var access = await _context.InventoryAccesses
                .FirstOrDefaultAsync(a => a.InventoryId == inventoryId
                                        && a.UserId == userId);

            if (access != null)
            {
                _context.InventoryAccesses.Remove(access);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Access", new { id = inventoryId });
        }

        // User search autocomplete
        public async Task<IActionResult> SearchUsers(string q)
        {
            var users = await _userManager.Users
                .Where(u => u.Email!.Contains(q) ||
                            (u.DisplayName != null && u.DisplayName.Contains(q)))
                .Select(u => new { u.Id, u.Email, u.DisplayName })
                .Take(5)
                .ToListAsync();

            return Json(users);
        }
        // ─── Statistics ───────────────────────────────────────
        public async Task<IActionResult> Stats(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Fields)
                .Include(i => i.Items)
                    .ThenInclude(item => item.FieldValues)
                .Include(i => i.Items)
                    .ThenInclude(item => item.Likes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null) return NotFound();

            var stats = new InventoryStatsViewModel
            {
                TotalItems = inventory.Items.Count,
                TotalLikes = inventory.Items.Sum(i => i.Likes.Count)
            };

            foreach (var field in inventory.Fields)
            {
                var values = inventory.Items
                    .SelectMany(item => item.FieldValues)
                    .Where(fv => fv.FieldId == field.Id && !string.IsNullOrEmpty(fv.Value))
                    .Select(fv => fv.Value!)
                    .ToList();

                var fieldStat = new FieldStats
                {
                    FieldTitle = field.Title,
                    FieldType = field.FieldType
                };

                if (field.FieldType == FieldType.Numeric && values.Any())
                {
                    var numbers = values
                        .Select(v => double.TryParse(v, out var n) ? n : (double?)null)
                        .Where(n => n.HasValue)
                        .Select(n => n!.Value)
                        .ToList();

                    if (numbers.Any())
                    {
                        fieldStat.Average = Math.Round(numbers.Average(), 2);
                        fieldStat.Min = numbers.Min();
                        fieldStat.Max = numbers.Max();
                    }
                }
                else if (field.FieldType == FieldType.SingleLineText ||
                         field.FieldType == FieldType.MultiLineText)
                {
                    var mostUsed = values
                        .GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();

                    if (mostUsed != null)
                    {
                        fieldStat.MostUsedValue = mostUsed.Key;
                        fieldStat.MostUsedCount = mostUsed.Count();
                    }
                }

                stats.FieldStats.Add(fieldStat);
            }

            ViewBag.InventoryId = id;
            ViewBag.InventoryTitle = inventory.Title;
            return View(stats);
        }

        // ─── AutoSave API ───────────────────────────────────── (for Edit page)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AutoSave([FromBody] AutoSaveViewModel model)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (inventory == null)
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isOwner = user?.Id == inventory.OwnerId;

            if (!isOwner && !isAdmin)
                return Json(new { success = false });

            if (inventory.Version != model.Version)
                return Json(new { success = false, conflict = true });

            inventory.Title = model.Title ?? inventory.Title;
            inventory.Description = model.Description;
            inventory.Version += 1;

            await _context.SaveChangesAsync();

            return Json(new { success = true, version = inventory.Version });
        }
        // ─── HandleTags Helper ───────────────────────────────
        private async Task HandleTags(int inventoryId, string? tagsString)
        {
            if (string.IsNullOrWhiteSpace(tagsString)) return;

            var tagNames = tagsString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            foreach (var tagName in tagNames)
            {
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name == tagName);

                if (tag == null)
                {
                    tag = new Tag { Name = tagName };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                _context.InventoryTags.Add(new InventoryTag
                {
                    InventoryId = inventoryId,
                    TagId = tag.Id
                });
            }

            await _context.SaveChangesAsync();
        }

    }
}