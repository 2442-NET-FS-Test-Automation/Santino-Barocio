//This class will hold the busness logic/db retry logic for fulfilling transactions

using System.Linq.Expressions;
using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Concurrent;


namespace Library.Api.Fulfillment;

//ASP.NET's builder (DI container) NEEDS us to provide 2 things when we register a service
// An interface and a croncrete implementation. These can both go in the same file.
public interface IFulfillmentService
{
    public Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
    public Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct);
    public int ResolveProductId(string sku);

}

//Im going to stcik everything about order fulfillment in this file
// Requests are either Fulfilled or Backordered - no other results possible
public enum FulfillmentResult{ Fulfilled, Backordered}

// Also going to make a record for the result of a Burst (many orders at the same time)
// records are lightweiht custom types that allow for comparison with ==
public record BurstResult(int Fulfilled, int Backordered);

public class FulfillmentService : IFulfillmentService
{

    //ASP.NET manages the creation (and destruction) of all our dependencies across our app
    // If we need a DbContext or DbContextFactory or Logger or any other dependency
    // we DO NOT instantiate one here, we ask for one via the Constructor
    private readonly IDbContextFactory<LibraryDbContext> _factory; // holds my factory

    private readonly BurstPlanner _planner; // holds my BurstPlanner object

    private readonly ConcurrentDictionary<string, int> _skuToProductId;

    // The factory in the constructor arguments list comes from the ASP.NET DI Container.
    // It will create a new instance of the factory and pass it to us when we ask for it.
    public FulfillmentService(IDbContextFactory<LibraryDbContext> factory, BurstPlanner planner)
    {
        _factory = factory;
        _planner = planner;

        //Storing skus and their product Id's so we can do O(1) lookup if we need it later
        using var db = _factory.CreateDbContext();
        _skuToProductId = new ConcurrentDictionary<string, int>(
            db.Products.ToDictionary(p => p.Sku, p=> p.Id)
        );

    }


    // Method to resolve Skus to productIds using that dictionary
    public int ResolveProductId(string sku)
    {
        try {return _skuToProductId[sku];}
        catch(KeyNotFoundException) { throw new UnkownSkuException(sku);}
    }


    // This method is going to handle fulfillment - its gonna be a bit long. which is why we didn't
    // just write all of this in Program.cs
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        // First - we need a db context
        await using var db = await _factory.CreateDbContextAsync(ct);

        //Lets grab our order from the database
        // Flow for this - a cusotmer places an order- It hits the order table - we are now fulfilling that order
        var order = await db.Orders.Include(o=>o.Lines).FirstAsync(o => o.Id == orderId);

        // Lets create that dictionary with the productId Key and the OrderId value
        // Yay for LINQ/Collections namespace!
        var requested = order.Lines.ToDictionary(l => l.ProductId, l=> l.Quantity);

        // create a flag for "can i continue fulfilling this order"
        bool canFullfill = true;

        foreach (OrderLine line in order.Lines)
        {
            // First - grab the current inventory from the db for that product
            InventoryItem inv = await db.Inventory.FirstAsync(i => i.ProductId == line.ProductId, ct);
        
            // Next - check if we can meet the order
            if(inv.CurrentStock < line.Quantity)
            {
                canFullfill = false;
                break;
            }

            inv.CurrentStock -= line.Quantity; // This write to the InventoryItem table is guarded by RowVersion

        }

        // assuming we broke out of the foreach and cannot fulfill the order
        if (!canFullfill) // checking for canFulfill == false
        {
            //We can't fulfill this order, its now Backordered
            order.Status = Status.Backordered;

            // Creating a new fulfillment event record for this transaction, setting it to backordered
            db.FulfillmentEvents.Add(new FulfillmentEvent {OrderId = orderId, Type = "Backordered"});

            await db.SaveChangesAsync(ct);
            // Log the transaction, using the Serilog structured logging syntax
            Log.Warning("Backordered {OrderId}: Insufficient stock", orderId);

            return FulfillmentResult.Backordered;
        }

        // If we make it here, we CAN fulfill that order
        order.Status = Status.Fulfilled;
        order.CompletedUtc = DateTime.UtcNow;
        db.FulfillmentEvents.Add(new FulfillmentEvent {OrderId = orderId, Type = "Fulfilled"});

        // Adding our retry save method
        if(!await SaveWithRetryAsync(db, requested, ct))// if we enter this if - we lost enough times
        {// that stock dropped this order was Backordered
            db.ChangeTracker.Clear(); // Clear change tracker
            Order staleOrder = await db.Orders.FirstAsync(o => o.Id == orderId, ct); //grab stale order from db
            staleOrder.Status = Status.Backordered;
            Log.Warning("Backordered order {orderId} after concurrency retry", orderId);
            return FulfillmentResult.Backordered;
            
        }



        Log.Information("Fulfilled order: {orderId}, {LineCount} lines");
        return FulfillmentResult.Fulfilled;

    }

    // Lets break the logic for saving with retry (via RowVersion) into its own method
    // just to help keep things straight
    private static async Task<bool> SaveWithRetryAsync(
        LibraryDbContext db, IReadOnlyDictionary<int, int> requestedByProductId, CancellationToken ct)
    {
        // This is the rowversion changetracker entry retry from yesterday
        // Lets set max retries to 3 - by wrapping everything in a loop

        for(int attempt = 0; ; attempt++)
        {
            //Our loop as written never exits - it does incremement attempt for us.
            //If we retry and fail x amount of times - we will throw an exception manually
            try
            {
                // The DbContext insidethis methos came from FulfillOneAsync - if it has chanfes
                // staged to ir - we can save them here. Its the same object.
                await db.SaveChangesAsync(ct);
                return true;
            }
            // Whe can tell our try catch how many times to handle this exception for us
            // After 3 attempts - we wont enter the catch. It bubbles up to wherever this method
            // was called
            catch (DbUpdateConcurrencyException ex) when (attempt < 3)
            {

                // Retry logic - remember that change tracker stuff?
                //entry is an EF Core Change tracker entry
                foreach(var entry in ex.Entries)
                {
                    var current = await entry.GetDatabaseValuesAsync(); // grab the current database values
                    
                    //If some other user deleted the entry out from under us... we can't save
                    // return false
                    if (current is null) return false;

                    // Set the OriginalValues bucjet on the entry to what they currently are
                    entry.OriginalValues.SetValues(current);

                    if(entry.Entity is InventoryItem inv)
                    {
                        //Grab the current total for that item's stock
                        int freshValue = current.GetValue<int>(nameof(InventoryItem.CurrentStock));
                        //Dictionary lookup against the dict we passed in
                        int desiredAmmount = requestedByProductId[inv.ProductId];

                        //Re-check on the fresh stock - don't blindly trust it
                        if(freshValue < desiredAmmount) return false;
                        inv.CurrentStock = freshValue - desiredAmmount;
                    }
                }
                
            }
        }

    }

    public async Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct)
    {



        // we are just going to piggyback off to FulfillOneAsync - no need to rewrite logic we can just call it again
        var tasks = orderIds.Select(id => FulfillOneAsync(id,ct)); // each call will get its own dbContext

        //Await here until all tasks in the collection are complete
        var results = await Task.WhenAll(tasks);

        return new BurstResult(
            Fulfilled: results.Count( r => r == FulfillmentResult.Fulfilled),
            Backordered: results.Count(r => r == FulfillmentResult.Backordered)
        );


    }

}