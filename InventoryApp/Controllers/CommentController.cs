using InventoryApp.Data;
using InventoryApp.Models;
using InventoryApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace InventoryApp.Controllers
{
    // only authenticated users can access this controller
    [Authorize]
    public class CommentController : Controller
    {
        // Database context for accessing applicationo data
        private readonly ApplicationDbContext _context;
        // ASP.NET identity service for user management
        private readonly UserManager<ApplicationUser> _userManager;
        // SignalR hub context for real-time communication
        private readonly IHubContext<CommentHub> _hubContext;

        // Constructor to inject dependencies
        public CommentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<CommentHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }
        // handle comment submission 
        [HttpPost]
        public async Task<IActionResult> Add(int inventoryId, string content)
        {
            //Prevent empty or whitespace-only comments 
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Details", "Inventory", new { id = inventoryId });

            // get currenty logged in user
            var user = await _userManager.GetUserAsync(User);

            // create new comment object
            var comment = new Comment
            {
                InventoryId = inventoryId,
                UserId = user!.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            // Save comment to database
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Send real-time comment update to connected clients
            // using SignalR group based on inventory ID
            await _hubContext.Clients.Group(inventoryId.ToString())
                .SendAsync("ReceiveComment", new
                {
                    // Show display name if available, otherwise email
                    userName = user.DisplayName ?? user.Email,
                    // Comment message content
                    content = content,
                    // Format comment creation time
                    createdAt = comment.CreatedAt.ToString("dd MMM yyyy HH:mm")
                });

            return Redirect($"/Inventory/Details/{inventoryId}#discussion");
        }
    }
}