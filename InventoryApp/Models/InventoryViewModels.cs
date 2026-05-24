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

        public string? Tags { get; set; } // comma separated
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
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

        public string? Tags { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class InventoryDetailsViewModel
    {
        public Inventory Inventory { get; set; } = null!;
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasWriteAccess { get; set; }
    }
    public class InventoryStatsViewModel
    {
        public int TotalItems { get; set; }
        public int TotalLikes { get; set; }
        public List<FieldStats> FieldStats { get; set; } = new();
    }

    public class FieldStats
    {
        public string FieldTitle { get; set; } = "";
        public FieldType FieldType { get; set; }

        //   For numeric fields
        public double? Average { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }

        // for string fields
        public string? MostUsedValue { get; set; }
        public int MostUsedCount { get; set; }
    }
    // AutoSaveViewModel is used to store the auto-saved data for an inventory when a user is editing it.
    public class AutoSaveViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Version { get; set; }
    }
}