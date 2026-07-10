using System;
using System.Collections.Generic;

namespace KitchenFulfillment.Data.Entities;

public class FulfillmentEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}