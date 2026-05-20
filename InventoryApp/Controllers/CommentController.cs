using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace InventoryApp.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<CommentHub> _hubContext;

        public CommentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<CommentHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int inventoryId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Details", "Inventory", new { id = inventoryId });

            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                InventoryId = inventoryId,
                UserId = user!.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(inventoryId.ToString())
                .SendAsync("ReceiveComment", new
                {
                    userName = user.DisplayName ?? user.Email,
                    content = content,
                    createdAt = comment.CreatedAt.ToString("dd MMM yyyy HH:mm")
                });

            return Redirect($"/Inventory/Details/{inventoryId}#discussion");
        }
    }
}