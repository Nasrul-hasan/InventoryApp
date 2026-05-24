namespace InventoryApp.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;  // for optimistic locking

        // Who is the owner?
        public string OwnerId { get; set; } = "";
        public ApplicationUser? Owner { get; set; }

       // Items and fields
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<InventoryField> Fields { get; set; } = new List<InventoryField>();
        public ICollection<InventoryAccess> AccessList { get; set; } = new List<InventoryAccess>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();

        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
    }
}