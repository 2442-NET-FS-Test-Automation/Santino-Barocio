using KitchenFulfillment.Data;
using KitchenFulfillment.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitchenFulfillment.Data.Repositories;

/// <summary>
/// Concrete persistence layer — all DB writes for inventory and fulfillment go through here.
/// Each fulfillment call gets its own DbContext from the factory (thread-safe).
/// </summary>
public class FulfillmentRepository : IFulfillmentRepository
{
    private readonly IDbContextFactory<KitchenDbContext> _contextFactory;

    public FulfillmentRepository(IDbContextFactory<KitchenDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllInventoryAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InventoryItems.Include(i => i.MenuItem).ToListAsync();
    }

    public async Task<InventoryItem?> GetInventoryByMenuItemIdAsync(int menuItemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InventoryItems
            .Include(i => i.MenuItem)
            .FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
    }

    public async Task ResetStockAsync(int defaultQuantity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Clean previous orders for a clean scenario reset
        context.FulfillmentEvents.RemoveRange(context.FulfillmentEvents);
        context.OrderLines.RemoveRange(context.OrderLines);
        context.Orders.RemoveRange(context.Orders);

        var items = await context.InventoryItems.ToListAsync();
        foreach (var item in items)
        {
            item.QuantityOnHand = defaultQuantity;
        }

        await context.SaveChangesAsync();
    }

    public async Task FulfillOrderAsync(Order order)
    {
        int maxRetries = 3;
        int retries = 0;
        bool isSaved = false;

        // Each order gets its own isolated DbContext
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Attach the order to the new context
        context.Orders.Add(order);

        while (!isSaved && retries < maxRetries)
        {
            try
            {
                // Process each line of the order (e.g. 1 Lasagna, 2 Pizzas)
                foreach (var line in order.OrderLines)
                {
                    var inventory = await context.InventoryItems
                        .FirstOrDefaultAsync(i => i.MenuItemId == line.MenuItemId);

                    if (inventory == null || inventory.QuantityOnHand < line.Quantity)
                    {
                        order.Status = OrderStatus.Backordered;
                        LogEvent(context, order, "Backordered", $"Insufficient stock for MenuItem {line.MenuItemId}");
                        break; // Stop processing this order
                    }

                    // Decrement stock in memory
                    inventory.QuantityOnHand -= line.Quantity;
                    order.Status = OrderStatus.Fulfilled;
                    LogEvent(context, order, "Fulfilled", $"Decremented {line.Quantity} units of MenuItem {line.MenuItemId}");
                }

                // Save changes. This is where SQL Server checks the RowVersion.
                await context.SaveChangesAsync();
                isSaved = true;
                
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retries++;

                // Force reload of fresh data from the database for the next attempt
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is InventoryItem)
                    {
                        await entry.ReloadAsync();
                    }
                }
                
                // If we've reached the retry limit, cancel the order
                if (retries >= maxRetries)
                {
                    order.Status = OrderStatus.Backordered;
                    LogEvent(context, order, "Failed", "Concurrency retry limit exceeded.");
                    await context.SaveChangesAsync();
                    isSaved = true;
                }
            }
        }
    }

    private void LogEvent(KitchenDbContext context, Order order, string type, string message)
    {
        context.FulfillmentEvents.Add(new FulfillmentEvent
        {
            Order = order,
            Type = type,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
}