namespace InventoryApp.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string CustomId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;

        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }

        public string CreatedById { get; set; } = "";
        public ApplicationUser? CreatedBy { get; set; }

        public ICollection<ItemFieldValue> FieldValues { get; set; } = new List<ItemFieldValue>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}