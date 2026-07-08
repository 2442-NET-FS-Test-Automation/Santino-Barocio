using Library.Data.Entities;

namespace Library.Data;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku);
    Task<InventoryItem> AddInventoryItemAsync(string sku, string name, decimal price, int quantity);
    Task<bool> RemoveBySkuAsync(string sku);
    }