using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class MenuCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}