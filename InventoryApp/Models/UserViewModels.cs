namespace InventoryApp.Models
{
    public class UserProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<Inventory> OwnedInventories { get; set; } = new();
        public List<Inventory> AccessInventories { get; set; } = new();
    }
}