namespace InventoryApp.Models
{
    public enum FieldType
    {
        SingleLineText,
        MultiLineText,
        Numeric,
        DocumentLink,
        TrueFalse
    }

    public class InventoryField
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public FieldType FieldType { get; set; }
        public bool ShowInTable { get; set; } = true;
        public int Order { get; set; } = 0;

        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }

        public ICollection<ItemFieldValue> Values { get; set; } = new List<ItemFieldValue>();
    }
}