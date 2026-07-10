using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class DiningTable
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}