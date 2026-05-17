using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class CreateInventoryViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Display(Name = "Public (anyone can add items)")]
        public bool IsPublic { get; set; } = false;
    }

    public class EditInventoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Display(Name = "Public (anyone can add items)")]
        public bool IsPublic { get; set; }

        public int Version { get; set; }
    }

    public class InventoryDetailsViewModel
    {
        public Inventory Inventory { get; set; } = null!;
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasWriteAccess { get; set; }
    }
}