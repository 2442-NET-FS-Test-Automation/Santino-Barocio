using System.ComponentModel.DataAnnotations;

namespace KitchenFulfillment.Data.Entities;

public class MenuItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = default!;

    public decimal Price { get; set; }

    // Relationship with Category (1:N)
    public int MenuCategoryId { get; set; }
    public MenuCategory MenuCategory { get; set; } = default!;
}