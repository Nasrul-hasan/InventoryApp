namespace InventoryApp.Models
{
    public class SearchResultViewModel
    {
        public string Query { get; set; } = "";
        public List<Inventory> Inventories { get; set; } = new();
        public List<Item> Items { get; set; } = new();

        public int TotalResults => Inventories.Count + Items.Count;
    }
}