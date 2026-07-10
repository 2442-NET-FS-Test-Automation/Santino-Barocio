using KitchenFulfillment.Data;
using KitchenFulfillment.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitchenFulfillment.Api.Fulfillment;

public class BurstPlanner
{
    private readonly IDbContextFactory<KitchenDbContext> _dbFactory;

    public BurstPlanner(IDbContextFactory<KitchenDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PriorityQueue<int, int>> PlanBurstAsync(IEnumerable<int> orderIds)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        
        // PriorityQueue requires an element (the order ID) and a priority (int)
        // In C#, the lowest number has the highest priority when dequeuing.
        var queue = new PriorityQueue<int, int>();

        var orders = await db.Orders
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Priority })
            .ToListAsync();

        foreach (var order in orders)
        {
            // Assign 0 to Expedited (dequeued first) and 1 to Normal (dequeued after)
            int priorityLevel = order.Priority == OrderPriority.Expedited ? 0 : 1;
            queue.Enqueue(order.Id, priorityLevel);
        }

        return queue;
    }
}