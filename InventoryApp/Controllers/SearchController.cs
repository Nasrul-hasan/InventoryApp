using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new SearchResultViewModel { Query = "" });

            q = q.Trim();

            // Inventories খোঁজো
            var inventories = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Items)
                .Where(i => i.Title.Contains(q) ||
                            (i.Description != null && i.Description.Contains(q)))
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Items খোঁজো
            var items = await _context.Items
                .Include(i => i.Inventory)
                .Include(i => i.FieldValues)
                    .ThenInclude(fv => fv.Field)
                .Where(i => i.CustomId.Contains(q) ||
                            i.FieldValues.Any(fv => fv.Value != null &&
                                                    fv.Value.Contains(q)))
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToListAsync();

            var vm = new SearchResultViewModel
            {
                Query = q,
                Inventories = inventories,
                Items = items
            };

            return View(vm);
        }
        // Tag autocomplete এর জন্য
        public async Task<IActionResult> Tags(string q)
        {
            var tags = await _context.Tags
                .Where(t => t.Name.StartsWith(q))
                .Select(t => t.Name)
                .Take(10)
                .ToListAsync();

            return Json(tags);
        }
    }
}