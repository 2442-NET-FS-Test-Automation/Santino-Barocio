using KitchenFulfillment.Data.Entities;

namespace KitchenFulfillment.Api.Fulfillment;

/// <summary>
/// Factory for creating orders — validates the kind (Normal/Expedited) and rejects unknown types
/// in its default arm, and validates that each SKU is not empty.
/// </summary>
public class OrderFactory
{
    public Order CreateOrder(string kind, int customerId, int tableId, IEnumerable<(string Sku, int Qty)> lines)
    {
        // Validate kind with switch — default arm throws exception for unknown kind
        OrderPriority priority = kind.ToLowerInvariant() switch
        {
            "normal"    => OrderPriority.Normal,
            "expedited" => OrderPriority.Expedited,
            _           => throw new ArgumentException($"Unknown order type: '{kind}'. Use 'Normal' or 'Expedited'.", nameof(kind))
        };

        var order = new Order
        {
            CustomerId = customerId,
            DiningTableId = tableId,
            EmployeeId = 1, 
            Priority = priority,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Sku))
            {
                throw new UnknownSkuException("N/A");
            }

            order.OrderLines.Add(new OrderLine
            {
                Quantity = line.Qty
                // Note: The MenuItem relationship is resolved in the service,
                // here we only store the quantity in memory.
            });
        }

        return order;
    }
}