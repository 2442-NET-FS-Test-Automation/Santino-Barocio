using KitchenFulfillment.Data;
using KitchenFulfillment.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitchenFulfillment.Api.Fulfillment;

public interface ISeeder
{
    IReadOnlyList<int> SeedOrders(int n, bool expedited);
    IReadOnlyList<int> SeedMixedOrders(int expeditedCount, int normalCount);
    IReadOnlyList<int> ResetAndCreateOrders(int n);
}

public class Seeder : ISeeder
{
    private readonly IDbContextFactory<KitchenDbContext> _dbFactory;
    private static readonly Random _rng = new();

    // IDs of customers and menuItems that exist in the seed
    private static readonly int[] CustomerIds = { 1, 2, 3 };
    private static readonly int[] MenuItemIds = { 1, 2, 3, 4, 5 };

    public Seeder(IDbContextFactory<KitchenDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Generates N orders of the same type (all expedited or all normal).
    /// Distributes across varied customers and menu items.
    /// </summary>
    public IReadOnlyList<int> SeedOrders(int count, bool expedited)
    {
        using var db = _dbFactory.CreateDbContext();
        var orders = new List<Order>();

        for (int i = 0; i < count; i++)
        {
            orders.Add(CreateSyntheticOrder(
                expedited ? OrderPriority.Expedited : OrderPriority.Normal));
        }

        db.Orders.AddRange(orders);
        db.SaveChanges();

        return orders.Select(o => o.Id).ToList();
    }

    /// <summary>
    /// Generates a MIXED wave: expeditedCount expedited + normalCount normal, all shuffled together.
    /// The spec requires sending "a mixed expedited/normal wave" to demonstrate expedited-first.
    /// </summary>
    public IReadOnlyList<int> SeedMixedOrders(int expeditedCount, int normalCount)
    {
        using var db = _dbFactory.CreateDbContext();
        var orders = new List<Order>();

        for (int i = 0; i < expeditedCount; i++)
            orders.Add(CreateSyntheticOrder(OrderPriority.Expedited));

        for (int i = 0; i < normalCount; i++)
            orders.Add(CreateSyntheticOrder(OrderPriority.Normal));

        db.Orders.AddRange(orders);
        db.SaveChanges();

        return orders.Select(o => o.Id).ToList();
    }

    public IReadOnlyList<int> ResetAndCreateOrders(int count)
    {
        using var db = _dbFactory.CreateDbContext();
        
        // Clean previous orders for a clean benchmark
        db.FulfillmentEvents.RemoveRange(db.FulfillmentEvents);
        db.OrderLines.RemoveRange(db.OrderLines);
        db.Orders.RemoveRange(db.Orders);

        // Restore inventory for the Benchmark (Target requirement)
        var items = db.InventoryItems.ToList();
        foreach (var item in items) item.QuantityOnHand = 100;
        
        db.SaveChanges();

        return SeedOrders(count, false);
    }

    /// <summary>
    /// Creates a varied synthetic order — distributes across customers, tables, and menu items.
    /// </summary>
    private Order CreateSyntheticOrder(OrderPriority priority)
    {
        return new Order
        {
            CustomerId = CustomerIds[_rng.Next(CustomerIds.Length)],
            DiningTableId = _rng.Next(1, 3), // table 1 or 2
            EmployeeId = _rng.Next(1, 3),    // employee 1 or 2
            Priority = priority,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderLines = new List<OrderLine>
            {
                new OrderLine
                {
                    MenuItemId = MenuItemIds[_rng.Next(MenuItemIds.Length)],
                    Quantity = 1
                }
            }
        };
    }
}