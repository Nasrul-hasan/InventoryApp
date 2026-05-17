namespace InventoryApp.Models
{
    public class ItemFieldValue
    {
        public int Id { get; set; }
        public string? Value { get; set; }

        public int ItemId { get; set; }
        public Item? Item { get; set; }

        public int FieldId { get; set; }
        public InventoryField? Field { get; set; }
    }
}