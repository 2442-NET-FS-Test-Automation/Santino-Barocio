using Microsoft.EntityFrameworkCore;
using KitchenFulfillment.Data;
using KitchenFulfillment.Data.Entities;
using KitchenFulfillment.Data.Repositories;
using KitchenFulfillment.Api;
using KitchenFulfillment.Api.Fulfillment;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// ==========================================
// 1. SERILOG BOOTSTRAP (Graceful stop pattern)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Base level
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Reduces noise from ASP.NET
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/fulfillment-log.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Kitchen Fulfillment API...");
    
    // ==========================================
    // BUILDER AREA
    // ==========================================
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // Replace logger with Serilog

    // 2. Connection string read from appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // 3. DbContext Factory registration for thread-safe operations
    builder.Services.AddDbContextFactory<KitchenDbContext>(options =>
        options.UseSqlServer(connectionString));

    // 4. Service registration (DI container — like Spring Beans)
    builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
    builder.Services.AddScoped<IFulfillmentRepository, FulfillmentRepository>();
    builder.Services.AddScoped<ISeeder, Seeder>();
    builder.Services.AddScoped<BurstPlanner>();
    builder.Services.AddScoped<OrderFactory>();

    // 5. Swagger for interactive API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ==========================================
    // APP AREA
    // ==========================================
    var app = builder.Build();

    // Swagger middleware
    app.UseSwagger();
    app.UseSwaggerUI();

    // ==============================================================================================================================
    //                                              OPERATOR ENDPOINTS (Minimal API)
    // ==============================================================================================================================

    app.MapGet("/", () => "Kitchen Fulfillment API is running!");

    // POST /seed - Reset stock to 20 units (repeatable, non-additive) — Status 200
    app.MapPost("/seed", async ([FromServices] IFulfillmentRepository repo) =>
    {
        await repo.ResetStockAsync(20);
        Log.Information("Stock reset to 20 units per item");
        return Results.Ok("Stock reseted to 20 units per item");
    });

    // GET /inventory - Consult current stock — Status 200
    app.MapGet("/inventory", async ([FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();
        var items = await db.InventoryItems.Include(i => i.MenuItem)
            .Select(i => new { i.MenuItem.Sku, i.MenuItem.Name, i.QuantityOnHand })
            .ToListAsync();
        return Results.Ok(items);
    });

    // POST /orders - Create a single order with Factory — Status 201 / 400 / 404
    // Demonstrates: Factory pattern, custom exception caught specific-before-base, status codes 201/400
    app.MapPost("/orders", async (OrderRequest req, OrderFactory factory,
        IFulfillmentService fSvc, IDbContextFactory<KitchenDbContext> dbf, CancellationToken ct) =>
    {
        try
        {
            // Factory creates the order — rejects unknown kind (default arm)
            Order newOrder = factory.CreateOrder(req.Kind, req.CustomerId, req.TableId,
                req.Lines.Select(l => (l.Sku, l.Qty)));

            // Resolve SKU → MenuItemId using ConcurrentDictionary (O(1) lookup)
            foreach (var (line, reqLine) in newOrder.OrderLines.Zip(req.Lines))
            {
                line.MenuItemId = fSvc.ResolveProductId(reqLine.Sku);
            }

            await using var db = await dbf.CreateDbContextAsync(ct);
            db.Orders.Add(newOrder);
            await db.SaveChangesAsync(ct);

            Log.Information("Created order {OrderId} for customer {CustomerId}", newOrder.Id, req.CustomerId);
            return Results.Created($"/orders/{newOrder.Id}", new { newOrder.Id });
        }
        // Specific-before-base exception handling — custom exception first, then general
        catch (UnknownSkuException ex)
        {
            Log.Warning("Rejected order: unknown SKU {Sku}", ex.Sku);
            return Results.BadRequest(new { error = ex.Message, sku = ex.Sku });
        }
        catch (ArgumentException ex)
        {
            Log.Warning("Rejected order: invalid kind — {Message}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error creating order");
            return Results.Problem("Internal error creating order.");
        }
    });

    // POST /orders/burst - Fire massive asynchronous burst without blocking the API — Status 202
    app.MapPost("/orders/burst", (
        [FromServices] ISeeder seeder,
        [FromServices] IServiceScopeFactory scopes, 
        IHostApplicationLifetime lifetime,
        int? n, 
        bool? expedited, 
        int normalCount = 0,
        int expeditedCount = 0) =>
    {
        IReadOnlyList<int> orderIds;
        // Supports legacy call (?n=10&expedited=true) or new mixed (?normalCount=5&expeditedCount=5)
        if (n.HasValue && expedited.HasValue)
        {
            orderIds = seeder.SeedOrders(n.Value, expedited.Value);
        }
        else
        {
            orderIds = seeder.SeedMixedOrders(expeditedCount, normalCount);
            n = normalCount + expeditedCount;
        }

        var appStopping = lifetime.ApplicationStopping;

        // Fire and Forget background task
        // Burst runs in a background Task so API stays responsive
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();
                await service.FulfillBurstAsync(orderIds, appStopping);
            }
            catch (Exception ex)
            {
                // Task fire-and-forget — exceptions would die silently without this
                Log.Error(ex, "Burst fulfillment failed");
            }
        }, appStopping);

        return Results.Accepted(value: new { message = $"Burst of {n} orders in progress.", orderCount = n });
    });

    // GET /verify/no-oversell — P3 proof endpoint
    // Passes P3: No oversell, units fulfilled == units depleted, no pending orders.
    app.MapGet("/verify/no-oversell", async (int? initialStockTotal, [FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        var rows = await db.InventoryItems.Include(i => i.MenuItem).ToListAsync();

        // If caller doesn't pass initialStockTotal, infer it from what /seed set.
        // Seed sets all items to the same qty, so we can read the max on-hand + fulfilled.
        int stockTotal = initialStockTotal ?? rows.Count * 20;
        
        // Requirement 1: no on-hand quantity is negative
        var negative = rows.Where(i => i.QuantityOnHand < 0).ToList();
        
        // Requirement 2: units fulfilled == units depleted
        var currentStockTotal = rows.Sum(i => i.QuantityOnHand);
        var unitsDepleted = stockTotal - currentStockTotal;

        var unitsFulfilled = await db.OrderLines
            .Where(ol => ol.Order.Status == OrderStatus.Fulfilled)
            .SumAsync(ol => ol.Quantity);

        // Requirement 3: every order is terminal (nothing Pending)
        var pendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

        return Results.Ok(new
        {
            isMathCorrect = unitsDepleted == unitsFulfilled,
            anyNegative = negative.Any(),
            anyPending = pendingOrders > 0,
            unitsDepleted,
            unitsFulfilled,
            onHand = rows.Select(i => new { i.MenuItem.Sku, i.QuantityOnHand })
        });
    });

    // GET /reports/by-completion - Completed orders ordered by timestamp — Status 200
    // Demonstrates that expedited orders tend to complete first
    app.MapGet("/reports/by-completion", async ([FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        var completedOrders = await db.Orders
            .Where(o => o.Status == OrderStatus.Fulfilled)
            .OrderBy(o => o.CompletedAt)
            .Select(o => new { o.Id, o.Priority, o.CompletedAt })
            .ToListAsync();

        return Results.Ok(completedOrders);
    });

    // POST /benchmark - Compare sequential vs parallel speed by rebuilding stock — Status 200
    app.MapPost("/benchmark", async (
        int n, 
        [FromServices] IFulfillmentService fs, 
        [FromServices] ISeeder seeder, 
        CancellationToken ct) =>
    {
        // 1. Sequential run
        var idsSequential = seeder.ResetAndCreateOrders(n);
        var stopwatch1 = Stopwatch.StartNew();

        foreach (var id in idsSequential)
        {
            await fs.FulfillOneAsync(id, ct);
        }
        stopwatch1.Stop();

        // 2. Concurrent run in parallel
        var idsParallel = seeder.ResetAndCreateOrders(n);
        var stopwatch2 = Stopwatch.StartNew();
        
        await fs.FulfillBurstAsync(idsParallel, ct);
        stopwatch2.Stop();

        var result = new
        {
            sequentialMs = stopwatch1.ElapsedMilliseconds,
            concurrentMs = stopwatch2.ElapsedMilliseconds,
            speedup = stopwatch2.ElapsedMilliseconds > 0
                ? (double)stopwatch1.ElapsedMilliseconds / stopwatch2.ElapsedMilliseconds
                : 0
        };

        Log.Information("Benchmark: Sequential={SequentialMs}ms, Concurrent={ConcurrentMs}ms, Speedup={Speedup:F2}x",
            result.sequentialMs, result.concurrentMs, result.speedup);

        return Results.Ok(result);
    });

   // GET /reports/top-products - Top products by units sold (LINQ grouping/aggregation) — Status 200
    app.MapGet("/reports/top-products", async ([FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        // LINQ with Join + GroupBy + Aggregation — not a raw table dump
        var topProducts = await db.FulfillmentEvents
            .Where(e => e.Type == "Fulfilled")
            .Join(db.OrderLines, e => e.OrderId, l => l.OrderId, (e, l) => l)
            .Join(db.MenuItems, l => l.MenuItemId, m => m.Id, (l, m) => new { m.Sku, m.Name, l.Quantity })
            .GroupBy(x => new { x.Sku, x.Name })
            .Select(g => new { g.Key.Sku, g.Key.Name, UnitsSold = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.UnitsSold)
            .ToListAsync();

        return Results.Ok(topProducts);
    });

    // GET /reports/top-customers - Top customers by order volume — Status 200
    app.MapGet("/reports/top-customers", async ([FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        var topCustomers = await db.Orders
            .Where(o => o.Status == OrderStatus.Fulfilled)
            .GroupBy(o => new { o.CustomerId, o.Customer.Name })
            .Select(g => new { g.Key.CustomerId, g.Key.Name, OrdersFulfilled = g.Count() })
            .OrderByDescending(x => x.OrdersFulfilled)
            .ToListAsync();

        return Results.Ok(topCustomers);
    });

    // GET /reports/
    //  - Fulfillment vs backorder rate — Status 200
    app.MapGet("/reports/fulfillment-rate", async ([FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        var total = await db.Orders.CountAsync();
        var fulfilled = await db.Orders.CountAsync(o => o.Status == OrderStatus.Fulfilled);
        var backordered = await db.Orders.CountAsync(o => o.Status == OrderStatus.Backordered);
        var pending = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

        return Results.Ok(new
        {
            total,
            fulfilled,
            backordered,
            pending,
            fulfillmentRate = total > 0 ? (double)fulfilled / total * 100 : 0,
            backorderRate = total > 0 ? (double)backordered / total * 100 : 0
        });
    });

    // GET /reports/rank-of/{units} - Binary search on sorted report — Status 200
    // DSA: sorted array + Array.BinarySearch to find rank of a value
    app.MapGet("/reports/rank-of/{units:int}", async (int units, [FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();

        // Build descending sorted array of units sold per product
        var unitDesc = await db.FulfillmentEvents
            .Where(e => e.Type == "Fulfilled")
            .Join(db.OrderLines, e => e.OrderId, l => l.OrderId, (e, l) => l)
            .GroupBy(l => l.MenuItemId)
            .Select(g => g.Sum(l => l.Quantity))
            .OrderByDescending(u => u)
            .ToArrayAsync();

        // Binary Search on sorted DESC array — O(log n)
        // Inverted comparer because the array is in descending order
        var index = Array.BinarySearch(unitDesc, units, Comparer<int>.Create((a, b) => b.CompareTo(a)));

        return Results.Ok(new
        {
            units,
            rank = index >= 0 ? index + 1 : -1,
            totalProducts = unitDesc.Length,
            distribution = unitDesc
        });
    });

    // GET /orders/{id} - Find an order by ID — Status 200 / 404
    // Demonstrates deep relational querying with multiple Includes and ThenInclude
    app.MapGet("/orders/{id:int}", async (int id, [FromServices] IDbContextFactory<KitchenDbContext> factory) =>
    {
        await using var db = await factory.CreateDbContextAsync();
        
        // Use of multiple navigation properties to pull the complete aggregate root
        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.DiningTable)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
                    .ThenInclude(m => m.MenuCategory)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return Results.NotFound(new { error = $"Order with ID {id} not found." });

        // Projection mapping all relations to an anonymous object
        return Results.Ok(new 
        { 
            order.Id, 
            order.Priority, 
            order.Status, 
            order.CreatedAt, 
            order.CompletedAt,
            Customer = order.Customer.Name,
            Table = order.DiningTable.TableNumber,
            Waiter = order.Employee.Name,
            Lines = order.OrderLines.Select(l => new 
            {
                l.Quantity,
                Product = l.MenuItem.Name,
                Category = l.MenuItem.MenuCategory.Name
            })
        });
    });


    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The service failed unexpectedly or could not start");
}
finally
{
    Log.Information("Shutting down Kitchen Fulfillment API...");
    // Serilog — flush and clean close on shutdown (graceful stop)
    Log.CloseAndFlush();
}

// ==============================================================================================================================
// Records for binding request body
// ==============================================================================================================================
public record OrderLineRequest(string Sku, int Qty);
public record OrderRequest(string Kind, int CustomerId, int TableId, List<OrderLineRequest> Lines);