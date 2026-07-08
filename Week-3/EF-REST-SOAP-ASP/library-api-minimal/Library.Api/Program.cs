using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Data.Entities;
using Serilog;
using Library.Api.Fulfillment;
using System.Diagnostics;

//This is my API program.cs
// No main. We can think of it as 2 sections
// Registering things with the builder
// And then configuring things on the app
// And at the very bottom that app object that represents our entire API calls its run method


//Builder area
var builder = WebApplication.CreateBuilder(args);

// The first thing that we need is to give our builder a connection string to our database
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa;Password=S4nt1n0L!;TrustServerCertificate=true";


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()// Write to console and write to a file. Rolling interval means it will create a new file every day
    .WriteTo.File("logs/fullfilment-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Tell the builder to use Serilog for logging


//Tell the builder to use our LibraryDbContext with the connection string above
// By registering our DbContext class (or even clases, technically you use one per Database)
// We hand off the managing of creating and destroying these DbContext object to ASP.NET's
// dependency injection container. Like spring beans if you're familiar.


// ASP.NET has few different scope types
// Trasient - a new instance of the object is created every time it is requested
// Scoped - a new instance of the object is created per scope. A scope is created per HTTP request
// Singleton - a single instance of the object is created and shared across all requests
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton); // Scoped is the default for DbContext, but we can be explicit -and allow for Singleton Scope
                                                        // when needed

// We know we will need more than one libraryDbContext in one or more of these methrods. But we don't know how many
// before runtime. So we can use a DbContxt factory to create as many as we need at runtime.
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string));

//Registered our custom service with the builder
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<BurstPlanner>(); // adding out Burstplanner, will ne ised in FulfillmentService
builder.Services.AddScoped<OrderFactory>();


// Swagger stuff added to the Builder
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App area
var app = builder.Build();

//Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

// Endpoint area
app.MapGet("/", () => "Hello World!");

// Get all items from the inventory
app.MapGet("/inventory", async(LibraryDbContext db) =>
{
    // we should probably await this - may not matter because we are local
    return await db.Inventory.ToListAsync();
    
});

// Lets use LINQ - Language Integrated Query
// LINQ is a library that just lets us query collections
// The logic actually flows from SQL DQL - You can use method OR sql query syntax
// You can even save the queries themselves as C# objects if you want to

app.MapGet("/inventory/by-value", (LibraryDbContext db) =>
{
    return db.Inventory.Include(i => i.Product)
        .GroupBy(i => i.CurrentStock >= 5 ? "well-stocked" : "low") //group by just like in sql
        .Select(g => new{ tier = g.Key, count = g.Count(), units = g.Sum(i => i.CurrentStock)})
        .ToList();
});


// Any endpoints that stats with "/peek/*" are diagnostic/demo
// We are going to use them to expose things like EF Core change tracking and other
// underlying behaviors for learning. A real app would have no reason to expose HTTP endpoints
// to outside users to make this stuff observable

app.MapGet("/peel/tracking", (LibraryDbContext db) =>
{
    // Lets see the underlying EF Core change tracker
    var unchanged = db.Products.First(); // grab the first object. Read but not modified => Unchanged
    var modified = db.Products.Skip(1).First(); //queried... still Unchaged as of here

    modified.Price += 1; // state => Modified

    //When we create a new object and call the dbset's .Add() method it's state is
    // "Added" - this has not actuallu hit the database yet. But it's tracked to be added.
    db.Products.Add(new Product {Sku = "BK-TMP", Name = "Tmp", Price = 1m});

    // This bit of code is the non-production demo bit
    // We are accessing the LibraruDbContext object's change tracker to pull info
    // At most you'd debug with this
    var states = db.ChangeTracker.Entries()
        .Select(e => new { entity = e.Entity.GetType().Name, state = e.State.ToString()})
        .ToList();

    //Clearing the chnge tracker manually
    db.ChangeTracker.Clear();

    return states;

});


// Peek - Loading Strategies
app.MapGet("/peek/loading", (LibraryDbContext db) =>
{
   Product product = db.Products.First();  // grab the first product from DB table
   //Explicit loading vial Load()
   db.Entry(product).Reference(p => p.Inventory).Load(); // making another trip to the database to populate the property
});



// Lets manually go out of our way to create a conflict - obviously, don't do this in a real app
app.MapGet("/peek/conflict",(IServiceScopeFactory scopes)=>
{
    // Manually asking for scopes. Normally each endpoint method call gets its own scope tracked
    // by ASP.NET under the hood  during runtime. We can, for various reasons good and bad do this manually. 
    using var scopeA = scopes.CreateScope();
    using var scopeB = scopes.CreateScope();

    //Now, remember tha a dbContext is generated per 
    var firstDB = scopeA.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var secondDB = scopeB.ServiceProvider.GetRequiredService<LibraryDbContext>();

    //Each dbContext read from the same database BUT they tack changes independently
    // remember we gave Inventory entities a RowVersion - not just a property named RowVersion
    // but an actal OnModelCreation FluentAPI config for a RowVersion
    var firstInventory = firstDB.Inventory.First(i => i.Id == 1);
    var secondInventory = secondDB.Inventory.First(i => i.Id == 1);

    // Lets modify one AND save its changes, while just modifying the other
    firstInventory.CurrentStock --; //decrement => Modified
    firstDB.SaveChanges(); // save changes is what persists any created, deleited or modified objects

    // Calling SaveChanges() above modifies the RowVersion

    // This object, that should represent the exact same row in the DB now has a stale RowVersion
    //Before EF Tries to persist any changes, it will check the RowVersion. It won't match
    // and a excepetion will be thrown 
    secondInventory.CurrentStock --; // Rowversion still 1 - doesn't matcj DB
    try{
        secondDB.SaveChanges(); // this should fail as Rowversions don't match
    }
        catch (DbUpdateConcurrencyException ex)
    {
        // In this case we want EF to retry the UPDATE
        // Asking for the actual changetracker entry that threw this exception
        // this is EF Core specific.
        var entry = ex.Entries.Single();

        // For the entry that threw the exception - grab it's current values from the DB
        // not the object, just the values
        var current = entry.GetDatabaseValues();

        //Every entry in the change tracker tracks two sets of values
        // OriginalValues = the values of the object when it was loaded from the db
        // CurrentValues = te new modified vales we changed on the object in our app
        entry.OriginalValues.SetValues(current!);

        // Using the entry to grab the actual item - going somewhere backwards
        ((InventoryItem)entry.Entity).CurrentStock =
            current!.GetValue<int>(nameof(InventoryItem.CurrentStock)) - 1;
        
        secondDB.SaveChanges(); 
    }
   // I can send back specific codes bia methods like .Ok() with messages inside
   // others include Pronlem(), NotFound(), etc
   return Results.Ok("Conflict caught, reloaded and retried");

});


//Endpoint to reset the stock of the items in my catalog - useful dor testing and demo
// might need to hit this endpoint while we work
app.MapPost("/inventory/reset", async (LibraryDbContext db, ILogger<Program> logger) =>
{
    // We just ask for an ILogger like we do our dbcontext
    // then use it as normal
    logger.LogInformation("Started seeing database");

    //what I want to do is reset the items that I know I stuck into the db.
    foreach (InventoryItem inv in db.Inventory)
    {
        switch (inv.Id)
        {
            case 1:
                inv.CurrentStock = 5;
                break;
            case 2:
                inv.CurrentStock = 3;
                break;
            case 3:
                inv.CurrentStock = 8;
                break;
            default:
                break;
        }
    }

    db.SaveChanges();//persistent the changes to the database
    logger.LogInformation("Stock reset to default values");
    return Results.Ok("Stock reset to default values");

});

//Fulfillment stuff for orders goes down here
// Im going to take in info from the front end (swagger for now)
// I have a few options 
// I can take in from the uri/query string
// I can also take in parameters from the body

//Quick method to fulfill one order
app.MapPost("/orders",async(OrderPlayload orderRequest, IDbContextFactory<LibraryDbContext> factory,
            CancellationToken ct,  IFulfillmentService fSvc)=>
{
    //Remember we create an order in our db
    // And then try to create a Succesful fulfillment record against the db
    await using var db = await factory.CreateDbContextAsync(ct); //ask for db context to place order

    var newOrder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        // Using the orderRequest from the HTTP request body to create my order
        Lines = {new OrderLine {ProductId = orderRequest.ProductId, Quantity = orderRequest.Quantity}}
    };

    db.Orders.Add(newOrder); // add new order
    await db.SaveChangesAsync(ct); //save that order

    // Now that we've added the order - we try to fulfill it
    var result = await fSvc.FulfillOneAsync(newOrder.Id,ct);
    return Results.Ok(new {orderId = newOrder.Id, result = result.ToString()});

});

// Burst endopoint
//Forgoing creating a redcord - we will taje these from a the query string
// IHostApplicationLifetime - this lets us see events related to the app lifetime
// We are going to use it to make sure we "flush" pending orders if the app is asked to stop
app.MapPost("/orders/burst",(int n, bool expedited, ISeeder seeder,
        IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) =>
        {
            var ids = seeder.SeedOrders(n, expedited); //calling the seed orders method with the sruff from front end
            var appStoping = lifetime.ApplicationStopping; // gives us a cancellation tokes that is called when app goes to shutdow

            _ = Task.Run(async () => // assigning thw task result to a discard runs
            {
                try
                {
                    using var scope = scopes.CreateScope(); //ask for a fresh scope
                    var service = scope.ServiceProvider.GetRequiredService<IFulfillmentService>(); // grab a fulfillemtn service
                    await service.FulfillBurstAsync(ids, appStoping); // use it to call fulfillBurst
                }catch(Exception ex)
                {
                    // This task is fire and forget because we aren't waiting ot storing its result
                    // any exceptions would be "swallowd" i.e they would die with the task in the background
                    Log.Error(ex ,"Burst fulfillment failed");
                }
            },appStoping);
    
});

app.MapGet("/verify/no-oversell", (LibraryDbContext db)=>
{
    var rows = db.Inventory.Include(i => i.Product).ToList();
    var negative = rows.Where(i => i.CurrentStock < 0).ToList();
    var fulfilled = db.FulfillmentEvents.Count(e => e.Type == "Fulfilled");

    return new
    {
        anyNegative = negative.Any(),
        onHand = rows.Select(i => new {i.ProductId, i.CurrentStock}),
        unitsFulfilled = fulfilled
    };
});

app.MapPost("/benchmark",async (int n, IFulfillmentService fs, ISeeder seeder, CancellationToken ct)=>
{
    // Lets see how sequential vs parallel runs compare - 
    var ids1 = seeder.ResetAndCreateOrders(n);
    
    // First, sequential
    var sw1 = Stopwatch.StartNew();

    foreach ( var id in ids1)
    {
        await fs.FulfillOneAsync(id, ct);
    }
    sw1.Stop();

    //Next Concurrent
    var ids2 = seeder.ResetAndCreateOrders(n);

    var sw2 = Stopwatch.StartNew(); // start second stopwathc
    await fs.FulfillBurstAsync(ids2, ct);
    sw2.Stop();

    return new
    {
        sequentialMs = sw1.ElapsedMilliseconds,
        concurrentMs = sw2.ElapsedMilliseconds
    };
    
});

// Completion report -- what orders got completed and when
// Note: In general Expedited orders should be completed first. In practice - it depends on how long each thread takes
// if for some reason an expedited order's thread slows down (due to some background process on the computer or something)
// then a normal order CAN beat it. But we should see a defined trend.
app.MapGet("/reports/by-completion", (LibraryDbContext db) =>
{
   return db.Orders // look inside orders table
        .Where(o => o.Status == Status.Fulfilled) // grab fulfilled orders
        .OrderBy(o => o.CompletedUtc) // order by when they were completed
        .Select(o => new { o.Id, o.Priority, o.CompletedUtc}) // use info from those orders to make some return objects
        .ToList(); // put them in a list and return them as JSON body of response

});


//Binary Search on the sorted 
app.MapGet("/reports/rank-of/{units:int}",(int units, LibraryDbContext db) =>
{
   // Unsing LINQ and BinarySearch to grab units sold per product, order descending
   var unitDesc = db.FulfillmentEvents
   .Where(e => e.Type == "Fulfilled")
   .Join(db.OrderLines, e=> e.OrderId, l => l.OrderId, (e,l) => l) 
   .GroupBy(l => l.ProductId)
   .Select(g => g.Sum(l => l.Quantity))
   .OrderByDescending(u => u)
   .ToArray();

    // Sorred DESC => using Binary Search to find the index of a specific quantity sold
    // 1000, 400, 330, 34
    // Our BinarySearch needs a comparer - for something like an int or a char this is easy
    // if you want to do this with custom classes - you need to override CompareTo like we do ToString
    var index = Array.BinarySearch(unitDesc, units, Comparer<int>.Create((a,b) => b.CompareTo(a)));
    return new {units, rank = index >= 0 ? index + 1 : -1};// If BinarySearch doesn't find a thing - returns some bitwise
    //complement or something - collapse to -1
});

app.MapPost("/orders-with-factory", async (OrderRequest req, OrderFactory factory,
    IDbContextFactory<LibraryDbContext> dbf, CancellationToken ct) =>
{
    try
    {
        Order newOrder = factory.CreateOrder(req.Kind, req.CustomerId,
            req.Lines.Select(l => (l.Sku, l.Qty)));

        await using var db = await dbf.CreateDbContextAsync(ct);

        db.Orders.Add(newOrder);

        await db.SaveChangesAsync(ct);

        return Results.Created($"/orders/{newOrder.Id}", new {newOrder.Id});
    }
    catch (UnkownSkuException ex)
    {
        Log.Warning("Rejected order: unknown SKU {SKU}", ex.Sku);
        return Results.BadRequest(new {error = ex.Message, sku = ex.Sku});
    }
});

// My file always ends with app.Run() - minimal API or Controller API
app.Run();
Log.CloseAndFlush(); // Flush and close the log when the app is shutting down
public record OrderPlayload(int ProductId, int Quantity, int CustomerId);
public record OrderLineRequest(string Sku, int Qty);
public record OrderRequest(string Kind, int CustomerId, List<OrderLineRequest> Lines);
