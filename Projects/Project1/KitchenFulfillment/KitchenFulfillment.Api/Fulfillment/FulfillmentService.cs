using KitchenFulfillment.Data;
using KitchenFulfillment.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Concurrent;

namespace KitchenFulfillment.Api.Fulfillment;

// 1. Required return types
public enum FulfillmentResult
{
    Fulfilled,
    Backordered,
    Failed
}

// BurstResult with fulfilled vs backordered breakdown
public record BurstResult(int Fulfilled, int Backordered);

// 2. The service interface
public interface IFulfillmentService
{
    int ResolveProductId(string sku);
    Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
    Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct);
}

// 3. The service implementation
public class FulfillmentService : IFulfillmentService
{
    private readonly IDbContextFactory<KitchenDbContext> _dbFactory;
    private readonly BurstPlanner _planner;

    // ConcurrentDictionary for O(1) lookups of SKU → MenuItemId
    // Preloaded in constructor — avoids DB hits on each resolution
    private readonly ConcurrentDictionary<string, int> _skuToProductId;

    public FulfillmentService(IDbContextFactory<KitchenDbContext> dbFactory, BurstPlanner planner)
    {
        _dbFactory = dbFactory;
        _planner = planner;

        // Preload SKU → Id dictionary for O(1) lookups
        using var db = _dbFactory.CreateDbContext();
        _skuToProductId = new ConcurrentDictionary<string, int>(
            db.MenuItems.ToDictionary(m => m.Sku, m => m.Id)
        );
    }

    /// <summary>
    /// Resolves an SKU to its MenuItemId using the ConcurrentDictionary (O(1) lookup).
    /// Throws UnknownSkuException if the SKU does not exist.
    /// </summary>
    public int ResolveProductId(string sku)
    {
        if (_skuToProductId.TryGetValue(sku, out int id))
            return id;

        throw new UnknownSkuException(sku);
    }

    public async Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct)
    {
        // Uses PriorityQueue to process expedited-first
        var queue = await _planner.PlanBurstAsync(orderIds);
        var tasks = new List<Task<FulfillmentResult>>();

        while (queue.TryDequeue(out int orderId, out int priority))
        {
            tasks.Add(FulfillOneAsync(orderId, ct));
        }

        // Task.WhenAll — concurrent burst, each task gets its own DbContext
        var results = await Task.WhenAll(tasks);

        // Returns breakdown fulfilled vs backordered
        return new BurstResult(
            Fulfilled: results.Count(r => r == FulfillmentResult.Fulfilled),
            Backordered: results.Count(r => r == FulfillmentResult.Backordered || r == FulfillmentResult.Failed)
        );
    }

    /// <summary>
    /// Fulfills a single order within its own DbContext and transaction.
    /// Handles DbUpdateConcurrencyException with bounded retry (max 3 attempts).
    /// Each attempt gets a fresh DbContext to avoid dirty state in the change tracker.
    /// </summary>
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        int maxRetries = 50;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            // Each attempt gets its own fresh DbContext (not thread-safe — one per order)
            // This prevents the change tracker from dragging dirty state from a failed attempt
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            try
            {
                var order = await db.Orders
                    .Include(o => o.OrderLines)
                        .ThenInclude(ol => ol.MenuItem) // For structured logging with Sku
                    .FirstOrDefaultAsync(o => o.Id == orderId, ct);

                if (order == null || order.Status != OrderStatus.Pending) 
                    return FulfillmentResult.Failed;

                bool canFulfill = true;

                foreach (var line in order.OrderLines)
                {
                    var inventory = await db.InventoryItems.FirstOrDefaultAsync(i => i.MenuItemId == line.MenuItemId, ct);

                    if (inventory == null || inventory.QuantityOnHand < line.Quantity)
                    {
                        canFulfill = false;
                        // Structured logging with Sku and Quantity (spec: "structured fields — order id, product, quantity")
                        Log.Warning("Backordered {OrderId}: insufficient stock for {Sku} (requested {Qty}, on-hand {OnHand})",
                            orderId, line.MenuItem?.Sku ?? "unknown", line.Quantity, inventory?.QuantityOnHand ?? 0);
                        break;
                    }

                    // Decrement protected by RowVersion (optimistic concurrency)
                    inventory.QuantityOnHand -= line.Quantity;
                }

                if (canFulfill)
                {
                    order.Status = OrderStatus.Fulfilled;
                    order.CompletedAt = DateTime.UtcNow;
                    LogEvent(db, order, "Fulfilled", "Order completed successfully.");

                    // Serilog structured logging — with Sku and Quantity per line
                    foreach (var line in order.OrderLines)
                    {
                        Log.Information("Fulfilled {OrderId}: {Sku} x{Qty}",
                            orderId, line.MenuItem?.Sku ?? "unknown", line.Quantity);
                    }
                }
                else
                {
                    order.Status = OrderStatus.Backordered;
                    order.CompletedAt = DateTime.UtcNow;
                    LogEvent(db, order, "Backordered", "Insufficient inventory.");
                }

                // SaveChangesAsync — here SQL Server verifies the RowVersion
                // One transaction per order (atomicity — ACID)
                await db.SaveChangesAsync(ct);
                
                return canFulfill ? FulfillmentResult.Fulfilled : FulfillmentResult.Backordered;
            }
            catch (DbUpdateConcurrencyException)
            {
                Log.Warning("Concurrency conflict on order {OrderId}, attempt {Attempt}/{Max}", 
                    orderId, attempt + 1, maxRetries);

                // The DbContext is disposed when leaving the using block — the next
                // iteration of the for loop creates a fresh one with clean data from the DB
                if (attempt + 1 >= maxRetries)
                {
                    // Final failed attempt — backorder in a clean context
                    await using var cleanDb = await _dbFactory.CreateDbContextAsync(ct);
                    var failedOrder = await cleanDb.Orders.FindAsync(new object[] { orderId }, ct);
                    if (failedOrder != null)
                    {
                        failedOrder.Status = OrderStatus.Backordered;
                        failedOrder.CompletedAt = DateTime.UtcNow;
                        LogEvent(cleanDb, failedOrder, "Failed", "Concurrency retry limit exceeded.");
                        await cleanDb.SaveChangesAsync(ct);
                    }

                    Log.Error("Order {OrderId} failed after {MaxRetries} concurrency retries", orderId, maxRetries);
                    return FulfillmentResult.Failed;
                }
            }
        }
        return FulfillmentResult.Failed;
    }

    /// <summary>
    /// Records a fulfillment event in the FulfillmentEvents table (audit trail).
    /// </summary>
    private void LogEvent(KitchenDbContext db, Order order, string type, string message)
    {
        db.FulfillmentEvents.Add(new FulfillmentEvent
        {
            OrderId = order.Id,
            Type = type,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
}