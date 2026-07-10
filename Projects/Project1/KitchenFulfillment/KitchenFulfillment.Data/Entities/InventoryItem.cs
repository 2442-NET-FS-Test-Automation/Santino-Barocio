using System;
using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int QuantityOnHand { get; set; }
    
    // Optimistic concurrency token
    public byte[] RowVersion { get; set; } = null!;
}