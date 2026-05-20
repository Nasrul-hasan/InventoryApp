using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Latest 10 inventories
            var latest = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Items)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Top 5 most popular (most items)
            var top = await _context.Inventories
                .Include(i => i.Owner)
                .Include(i => i.Items)
                .OrderByDescending(i => i.Items.Count)
                .Take(5)
                .ToListAsync();

            // All tags
            var tags = await _context.Tags
                .Include(t => t.InventoryTags)
                .Where(t => t.InventoryTags.Any())
                .OrderByDescending(t => t.InventoryTags.Count)
                .Take(30)
                .ToListAsync();

            var vm = new HomeViewModel
            {
                LatestInventories = latest,
                TopInventories = top,
                PopularTags = tags
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}