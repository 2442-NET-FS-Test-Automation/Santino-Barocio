using KitchenFulfillment.Data.Entities;

namespace KitchenFulfillment.Data.Repositories;

/// <summary>
/// Abstraction for inventory-related persistence operations.
/// Callers depend on this interface, not on EF types directly.
/// </summary>
public interface IFulfillmentRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllInventoryAsync();
    Task<InventoryItem?> GetInventoryByMenuItemIdAsync(int menuItemId);
    Task ResetStockAsync(int defaultQuantity);
    Task FulfillOrderAsync(Order order);
}