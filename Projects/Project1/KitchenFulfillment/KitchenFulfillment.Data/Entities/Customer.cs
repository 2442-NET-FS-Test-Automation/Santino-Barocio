using System.ComponentModel.DataAnnotations;

namespace KitchenFulfillment.Data.Entities;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = default!;
}