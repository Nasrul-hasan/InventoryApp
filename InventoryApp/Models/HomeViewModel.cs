namespace InventoryApp.Models
{
    public class HomeViewModel
    {
        public List<Inventory> LatestInventories { get; set; } = new();
        public List<Inventory> TopInventories { get; set; } = new();
        public List<Tag> PopularTags { get; set; } = new();
    }
}