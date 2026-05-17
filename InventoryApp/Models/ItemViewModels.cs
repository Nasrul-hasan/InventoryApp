namespace InventoryApp.Models
{
    public class CreateItemViewModel
    {
        public int InventoryId { get; set; }
        public string InventoryTitle { get; set; } = "";
        public string CustomId { get; set; } = "";
        public List<InventoryField> Fields { get; set; } = new();
    }

    public class EditItemViewModel
    {
        public int Id { get; set; }
        public string CustomId { get; set; } = "";
        public int InventoryId { get; set; }
        public string InventoryTitle { get; set; } = "";
        public int Version { get; set; }
        public List<InventoryField> Fields { get; set; } = new();
        public Dictionary<int, string> FieldValues { get; set; } = new();
    }

    public class ItemDetailsViewModel
    {
        public Item Item { get; set; } = null!;
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasWriteAccess { get; set; }
        public bool UserLiked { get; set; }
        public int LikeCount { get; set; }
    }
}