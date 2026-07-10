using System;
using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    
    // Relationship with DiningTable (1:N)
    public int DiningTableId { get; set; }
    public DiningTable DiningTable { get; set; } = default!;

    // Relationship with Employee/Waiter (1:N)
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public OrderPriority Priority { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}