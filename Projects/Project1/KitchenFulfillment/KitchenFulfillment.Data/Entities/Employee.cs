using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Role { get; set; } = default!; // Ej. "Waiter", "Chef"
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}