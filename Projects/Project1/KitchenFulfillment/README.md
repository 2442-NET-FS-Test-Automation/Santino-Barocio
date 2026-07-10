# KitchenFulfillment — Order Fulfillment Service

## Domain

**Restaurant Kitchen** — A backend service that processes bursts of kitchen orders against a limited inventory of prepared portions. Menu items are the scarce resource; orders compete for the available portions.

| Spec Concept      | My Domain |
|---------------    |-----------|
| Product           | `MenuItem`|
| Order             | `Order` (Kitchen order) |
| The scarce thing  | Prepared portions (`QuantityOnHand`) |
| Customer          | `Customer` (Diner) |

**Decision:** Orders are **multi-line** — an order can request several dishes at once (e.g., 1 Lasagna + 2 Pizzas). Each `OrderLine` has its own `MenuItemId` and `Quantity`.

---

## Technique → Type/Endpoint Map

### EF Core
| Technique                                     | Type / File | Endpoint |
|---------                                      |---------------|----------|
| Code-first model (Data Annotations)           | `Customer.cs` (`[Required]`, `[MaxLength]`), `MenuItem.cs` (`[Required]`, `[MaxLength]`) | — |
| Code-first model (Fluent API)                 | `KitchenDbContext.OnModelCreating` — `IsRowVersion()`, `HasIndex().IsUnique()`, 1:1 relationship, `HasColumnType("decimal(10,2)")`, `HasMaxLength(256)` | — |
| `DbContext` registered in DI                  | `Program.cs` — `AddDbContextFactory<KitchenDbContext>` | All |
| `DbSet<T>` + change tracking + `SaveChanges`  | `KitchenDbContext.cs` — 9 DbSets | All |
| Migrations + seed                             | `Migrations/` folder + `HasData` in `OnModelCreating` | `POST /seed` |
| `RowVersion` concurrency token                | `InventoryItem.RowVersion` + `.IsRowVersion()` | `POST /orders/burst` |

### Minimal API
| Technique             | Endpoint |
|---------              |----------|
| `MapGet` / `MapPost`  | All endpoints in `Program.cs` |
| Model binding (route) | `GET /orders/{id}`, `GET /reports/rank-of/{units}` |
| Model binding (query) | `POST /orders/burst?n=50&expedited=true` |
| Model binding (body)  | `POST /orders` — `OrderRequest` record |
| Status 200            | `GET /inventory`, `GET /verify/no-oversell`, all reports |
| Status 201            | `POST /orders` — order created successfully |
| Status 202            | `POST /orders/burst` — burst accepted, processing in background |
| Status 400            | `POST /orders` — unknown SKU or invalid kind |
| Status 404            | `GET /orders/{id}` — order not found |

### SQL (through EF)
| Technique                         | Detail |
|---------                          |---------|
| 3NF + FKs                         | Customers, MenuItems, InventoryItems, Orders, OrderLines, FulfillmentEvents — all tables in 3NF with explicit FKs |
| Non-key indexes                   | `MenuItem.Sku` (unique — fast lookups by code), `Order.Status` (filtering orders by status), `Customer.Email` (unique — business constraint) |
| One fulfillment = one transaction | Each `FulfillOneAsync` calls `SaveChangesAsync` once — stock decrement + status change + audit event land atomically |

### ACID/Isolation Reasoning
Each order is fulfilled within **a single transaction** (the EF Core `SaveChangesAsync`). This guarantees:
- **Atomicity**: If the stock decrement fails, the order is not marked as Fulfilled.
- **Consistency**: The RowVersion prevents two orders from decrementing the same stock simultaneously.
- **Isolation**: Each order has its own `DbContext` (from the factory) — change trackers do not cross paths.
- **Durability**: `SaveChangesAsync` persists to SQL Server; only then is the result returned.

The effective isolation level is **Read Committed** (the default in SQL Server). The RowVersion adds optimistic protection: if another thread modified the row, it's detected on `SaveChanges` and retried with fresh data.

### Multithreading
| Technique                                                | Type / File |
|---------                                                 |---------------|
| Concurrent burst (`Task.WhenAll`)                        | `FulfillmentService.FulfillBurstAsync` |
| Per-order `DbContext`                                    | `FulfillmentService.FulfillOneAsync` — `_dbFactory.CreateDbContextAsync` |
| Oversell race solved (EF optimistic concurrency + retry) | `FulfillmentService.FulfillOneAsync` — catch `DbUpdateConcurrencyException`, `ReloadAsync`, bounded retry |
| `ConcurrentDictionary`                                   | `FulfillmentService._skuToProductId` — lookup SKU → MenuItemId in O(1) |
| `CancellationToken` shutdown                             | `FulfillOneAsync` receives `ct`, calls `ThrowIfCancellationRequested`, passes it to all queries |
| Sequential-vs-parallel benchmark                         | `POST /benchmark` — resets stock between runs, returns both times + speedup factor |

**Parallelism vs Concurrency:** Concurrency is the ability to handle multiple tasks _at once_ (time-slicing or interleaving). Parallelism is executing multiple tasks _simultaneously_ on different cores. Our burst uses `Task.WhenAll` which is concurrent — the runtime _can_ execute the tasks in parallel if there are cores available, which generally results in a speedup > 1x.

**Token-vs-Lock Contrast:**
- **EF Optimistic Concurrency (RowVersion)**: Does not lock the row on read — assumes there will be no conflict. On save, SQL Server checks that the `RowVersion` hasn't changed. If it changed, it throws `DbUpdateConcurrencyException` and we retry. **Best for**: low contention, many readers, few collisions.
- **`lock` / `Interlocked` (in-memory)**: Blocks access to the shared resource — only one thread enters the critical section at a time. **Best for**: shared memory state (like a `Bank` demo), high contention, fast operations that don't involve I/O.

We use RowVersion because the stock lives in **SQL Server** (not in memory) and we want to maximize concurrent throughput. A C# `lock` wouldn't protect the DB from another process or service instance.

### DSA
| Structure                             | Usage                                             | Big-O |
|-----------                            |-----                                              |-------|
| `PriorityQueue<int, int>`             | Expedited-first ordering in `BurstPlanner`        | Enqueue: O(log n), Dequeue: O(log n) |
| `ConcurrentDictionary<string, int>`   | SKU → MenuItemId lookup in `FulfillmentService`   | Lookup: Amortized O(1) |
| Sorted array + `Array.BinarySearch`   | Rank lookup in `GET /reports/rank-of/{units}`     | Sort: O(n log n) via LINQ `OrderByDescending`, Search: O(log n) |

### Patterns & Cross-cutting
| Technique                     | Type / File |
|---------                      |---------------|
| Repository behind interface   | `IFulfillmentRepository` → `FulfillmentRepository` (in `Data/Repositories/`) |
| Factory                       | `OrderFactory` — creates orders, validates kind with switch default arm |
| Serilog structured logging    | `Program.cs` config + `FulfillmentService` — `Log.Information/Warning/Error` with templates |
| Custom exception with data    | `UnknownSkuException` — carries the `Sku` property |
| Specific-before-base catch    | `POST /orders` — `catch (UnknownSkuException)` before `catch (ArgumentException)` before `catch (Exception)` |

---

## Non-Key Index Justification

| Index             | Column            | Reason |
|-------            |---------          |-------|
| Unique            | `MenuItem.Sku`    | SKUs are business identifiers — lookups by SKU are frequent (resolution in OrderFactory, lookups in reports). Unique guarantees integrity. |
| Non-unique        | `Order.Status`    | We constantly filter by status (Pending for fulfillment, Fulfilled for reports). Without an index, each query would do a full table scan. |
| Unique            | `Customer.Email`  | Business constraint — there cannot be two customers with the same email. The index speeds up uniqueness validation. |

---

## Benchmark Numbers

_(Run `POST /benchmark?n=50` to get real numbers from your machine)_

| Run           | Time (ms) | Speedup |
|-----          |-----------|---------|
| Sequential    | TBD       | — |
| Concurrent    | TBD       | TBD x |

If parallel doesn't win, the probable causes are: 
    low N (overhead of Task > savings), 
    SQL Server in Docker with a single core, 
    or high contention on the RowVersion causing many retries.

---

## Status Codes

| Code  | Endpoint                                                                              | Reason |
|------ |----------|-------|
| 200   | `GET /inventory`, `GET /verify/*`, `GET /reports/*`, `POST /seed`, `POST /benchmark`  | Successful operation, data returned |
| 201   | `POST /orders`                                                                        | Resource successfully created (the order) |
| 202   | `POST /orders/burst`                                                                  | Accepted for processing — the burst runs in the background |
| 400   | `POST /orders`                                                                        | Invalid input — unknown SKU or unrecognized kind |
| 404   | `GET /orders/{id}`                                                                    | Order with that ID does not exist |
