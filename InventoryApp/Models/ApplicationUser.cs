using Microsoft.AspNetCore.Identity;

namespace InventoryApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsBlocked { get; set; } = false;

        // এই user এর inventories
        public ICollection<Inventory> OwnedInventories { get; set; } = new List<Inventory>();
    }
}