using Microsoft.EntityFrameworkCore;
using KitchenFulfillment.Data.Entities;

namespace KitchenFulfillment.Data;

public class KitchenDbContext : DbContext
{
    public KitchenDbContext(DbContextOptions<KitchenDbContext> options) : base(options) { }

    // Base entities
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<FulfillmentEvent> FulfillmentEvents => Set<FulfillmentEvent>();

    // Additional entities
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Concurrency configuration (Fluent API — optimistic RowVersion token)
        modelBuilder.Entity<InventoryItem>()
            .Property(p => p.RowVersion)
            .IsRowVersion();

        // 2. 1:1 relationship MenuItem <-> InventoryItem 
        modelBuilder.Entity<MenuItem>()
            .HasOne<InventoryItem>()
            .WithOne(p => p.MenuItem)
            .HasForeignKey<InventoryItem>(p => p.MenuItemId);

        // 3. Price precision decimal(10,2) — Fluent API
        modelBuilder.Entity<MenuItem>()
            .Property(p => p.Price)
            .HasColumnType("decimal(10,2)");

        // 4. Performance indexes (non-key indexes)
        //    Unique SKU — for fast lookups by menu item code
        modelBuilder.Entity<MenuItem>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        //    Order.Status — to efficiently filter Pending/Fulfilled/Backordered orders
        modelBuilder.Entity<Order>()
            .HasIndex(c => c.Status);

        //    Customer.Email — unique business constraint + length for SQL Server index compatibility
        modelBuilder.Entity<Customer>()
            .Property(c => c.Email)
            .HasMaxLength(256);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // 5. Explicit 1:N relationships for additional tables
        modelBuilder.Entity<MenuItem>()
            .HasOne(m => m.MenuCategory)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.MenuCategoryId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.DiningTable)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.DiningTableId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Employee)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EmployeeId);

        // 6. Initial data seeding — categories, employees, tables, customers
        modelBuilder.Entity<MenuCategory>().HasData(
            new MenuCategory { Id = 1, Name = "Main Course" },
            new MenuCategory { Id = 2, Name = "Appetizer" }
        );

        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, Name = "Chef Gordon", Role = "Chef" },
            new Employee { Id = 2, Name = "Maria Lopez", Role = "Waiter" }
        );

        modelBuilder.Entity<DiningTable>().HasData(
            new DiningTable { Id = 1, TableNumber = 5 },
            new DiningTable { Id = 2, TableNumber = 12 }
        );

        // 3 Customers — allows top-customers to show a real ranking
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
            new Customer { Id = 2, Name = "Maria Garcia", Email = "maria.garcia@example.com" },
            new Customer { Id = 3, Name = "Carlos Ruiz", Email = "carlos.ruiz@example.com" }
        );

        // 5 Menu Items — reports for top-products are more interesting with >2 items
        modelBuilder.Entity<MenuItem>().HasData(
            new MenuItem { Id = 1, Sku = "LST-001", Name = "Lasagna", Price = 15.99m, MenuCategoryId = 1 },
            new MenuItem { Id = 2, Sku = "PPR-002", Name = "Pepperoni Pizza", Price = 12.50m, MenuCategoryId = 1 },
            new MenuItem { Id = 3, Sku = "CSR-003", Name = "Caesar Salad", Price = 9.99m, MenuCategoryId = 2 },
            new MenuItem { Id = 4, Sku = "GRL-004", Name = "Grilled Salmon", Price = 22.50m, MenuCategoryId = 1 },
            new MenuItem { Id = 5, Sku = "TCS-005", Name = "Tacos al Pastor", Price = 11.00m, MenuCategoryId = 2 }
        );

        // Base inventory — each menu item starts with 100 portions (STOCK = 500 total)
        modelBuilder.Entity<InventoryItem>().HasData(
            new InventoryItem { Id = 1, MenuItemId = 1, QuantityOnHand = 100 },
            new InventoryItem { Id = 2, MenuItemId = 2, QuantityOnHand = 100 },
            new InventoryItem { Id = 3, MenuItemId = 3, QuantityOnHand = 100 },
            new InventoryItem { Id = 4, MenuItemId = 4, QuantityOnHand = 100 },
            new InventoryItem { Id = 5, MenuItemId = 5, QuantityOnHand = 100 }
        );
    }
}