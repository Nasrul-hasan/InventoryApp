namespace InventoryApp.Models
{
    public class InventoryAccess
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    }

    public class InventoryTag
    {
        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        public int TagId { get; set; }
        public Tag? Tag { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
    }

    public class Like
    {
        public int ItemId { get; set; }
        public Item? Item { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }
    }
}