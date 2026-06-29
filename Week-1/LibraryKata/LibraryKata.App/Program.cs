using System.Runtime.CompilerServices;

// If I have code from another namespace I want to use here - I use a "using" statement
using Library.Domain;
using LibraryKata.Domain;
using Serilog;

namespace LibraryKata.App; //A namespace is like a bucket or logical container for different realted code files.

public class Program
{

    //public - accesible across the program
    //static - Main can be called upon without a Program object. It is a Static/class method.
    //void   - it doesn't return anything 

    public static async Task Main()
    {
        //Lets configure Serilog here befores any code execution
        //Serilog works via a singleton object. Its shared golbally
        //throughout the app, configure once, use anywhere
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()//verbose > Debug > Info > Warning > Error > Fatal
           .WriteTo.Console()//Sink: where do my logs go? Console, text file, database, etc?
           .CreateLogger();//create the logger based on the config above

        //When I call dotnet run, it finds Main() and begins code execution at the first line of the
        //main method. I wrote my code, inside DataTypesAndOperators() - a separate method. So if I want
        //that code to run, I need to call it inside Main()
        Program.DatatypesAndOperators();
        //Wired in: these were defined but never called from Main, so the control-flow,
        //loops, and arrays demos never actually ran. Call them in source order.
        ControlFlow();
        ClassesExample();
        OopDemo();
        CollectionsDemo();
        ExceptionsDemo();
        AdvanceClassesDemo();
        await AsyncHttpDemo();

        //In case there are any lingering logs by the time we hit line 41 above
        //Don't just stop execution, write the logs to their sink THEN close the program
        Log.CloseAndFlush();
    }

    // private - accessible only within this class
    // static - it belongs to the class, not the objects of the class
    // void - return nothing
    private static void DatatypesAndOperators() //If I had arguments, or inputs for this method, they would go inside the parenthesis after the method name
    {
        Console.WriteLine("=== Data types and operators ===");

        //C# is a Strongly typed language
        //We cannot just create variables ans shove whatever we want into them like JS or Python
        int number = 3;
        double lateFee = 1;
        bool isMember = true;
        float floats = 1.1f;
        char letras = 'a';
        string words = "asd";


        //Operators
        String user = "Jon"; // Single = is the assignemt operator
        int total = number * 2; // Exameple of an arithmetic operator, like + - * /
        bool isEnough = total > 4; // comparison - compares the value in total to 4, greater than 4 is true
        // >, <, >=, <= comparison operators
        bool exactlySix = total == 6; // equality
        // There's no === like JS in C#
        // obj1 == obj2 always is false because they never have the same memory space
        bool lendable = isMember && isEnough; // Logic operator 
        // && - and, || - or, ! - not, logical XOR returns true if ONLY one condition is true


        //basic way to concat
        Console.WriteLine(letras + "was checked by" + user);
        
        //create much cleaner formatted string
        Console.WriteLine($"{words} on shelf {letras}: {number} copies, fee {lateFee}");

        //arithmetic shortcuts +=,-=,,*=,/=
        total += 1;
    }

    private static void ControlFlow()
    {
        Console.WriteLine("\n == Control Flow ==");

        int copiesAvailabe = 0;
        bool isMember = true;
        if (copiesAvailabe > 1)
        {
            Console.WriteLine("Available for checkout");
        }else if (copiesAvailabe == 1)
        {
            Console.WriteLine("Last copy");
        }
        else
        {
            Console.WriteLine("Available for checkout");
            Console.WriteLine("Check later");
        }

        
        // Switch
        string genre = "Mystery";

        switch (genre)
        {
            case "Mystery":
                Console.WriteLine("Checkout section A!");
                break;
            case "Science fiction":
                Console.WriteLine("Checkout section B!");
                break;
            default:
                Console.WriteLine("default");
                break;
        }

        //Switch en .Net 8 (New)
        string section = genre switch
        {
            //This is my expression body
            "Mystery" => "Section A",
            "Science-Fiction" => "Section B",
            _ => "Uh oh" //Default
        };
        Console.WriteLine(section);

    
    }

    private static void Loops()
    {
        //C# provides for loops as well, same as Java and any other language
        //For, while, do-while, etc
        for (int day = 1; day <= 3; day++)
        {
            Console.WriteLine($"Reminder day {day}: fee so far {CalculateLateFee(day)}");
        }    

        int onShelf = 3;
        while (onShelf > 0)
        {
            Console.WriteLine($"{onShelf} copies on the shelf");
            onShelf--; //quick decrement shorthand
        }
        Console.WriteLine("No copies on shelf");
    }

    //I can use this shorthand for one line methods
    private static decimal CalculateLateFee(int dayLate) => dayLate * 2;

    private static void ArraysWork()
    {
        //C# provides for arrays as well as lists and other collections - we'll get to those later.
        string[] books = {"Dune","Harry Potter","Percy Jackson","LOTR" };

        Console.WriteLine(books[2]);//I can access individual elements - keeping in mind we index at 0
        Console.WriteLine(books.Length);

        //C# allows for for-each loops
        foreach (string book in books)
        {
            Console.WriteLine(book);
        }
    }


    private static void ClassesExample()
    {
        Console.WriteLine("Using our domain Book class");

        //Instantiating my first book, calling the constructor via "new"
        Book dune = new Book("Dune","Frank Herbert", 3);
        Book littlePrince  = new Book("LittlePrince","Antoine de Saint-Exupéry",0);

        //If I want to print book info, I can just pass the book variable
        //It calls the toString() for me. The next two lines do the same
        Console.WriteLine(dune);
        Console.WriteLine(littlePrince.ToString());

        Console.WriteLine($"Checking out Dune: {dune.Checkout()}");//true
        Console.WriteLine($"Chechking out LittlePrince: {littlePrince.Checkout()}");//false


    }

    public static void OopDemo()
    {
        Console.WriteLine("\n\n == OOP Demo stuff ==");

        //Leveraging polymorphism - Books, ReferenceBooks, Magazines, -all are LibraryItems.
        LibraryItem[] catalog =
        {
            new Book("Dune","Frank Herbert", 2),
            new ReferenceBook("C# Langugage Standards", "Microsoft","Technology"),
            new Magazine("Sports Illustrated", "Francisco",5,"Conde Naste")
        };

        foreach(LibraryItem item in catalog)
        {
            Console.WriteLine(item.Describe());
        }

        //We can even use interfaces as reference types
        foreach(LibraryItem item in catalog)
        {
            if (item is ILendable lendable)
            {
                Console.WriteLine($"{item.Title}: Checkout -> {lendable.Checkout()}");
            }
            else
            {
                Console.WriteLine($"{item.Title} is Reference only");
            }
            
        }

        //override vs new behavior
        Magazine wired = new Magazine("Wired","Luis",3,"Conde Nast");
        LibraryItem baseMag = wired;

        Console.WriteLine("== Override vs new on the same object, different ref type ==");
        Console.WriteLine($"Magazine reference -> {wired.ShelfLabel()}");
        Console.WriteLine($"Library item reference -> {baseMag.ShelfLabel()}");


    }


    //Collections demo stuff
    private static void CollectionsDemo()
    {
        Console.WriteLine("=== COLLECTIONS DEMO STUFF ===");

        //Creating a catalog object
        //Because this backed by a list, it grows and shrinks for us
        Catalog catalog = new();


        //I could create my objects
        Book dune = new Book("Dune", "Frank Herbert", 3);

        //Then add them - we now through Catalog.Add(), which wraps the private list
        //We never touch catalog._items directly anymore: the list is the Catalog's business
        catalog.Add(dune);

        // I can also call a constructor inside the Add() method call
        // Methods having their arguments satisfied  by the return of other methods is a common pattern
        // and sometimes you'll get like 4-5 callbacks deep in tools like ASP.NET

        catalog.Add(new ReferenceBook("C#", "Microsoft", "Technology"));
        catalog.Add(new Magazine("Nat Geo", "Charlie", 4, "Technology"));


        //Count is a wrapper property; catalog[0] uses the indexer - reads like an array
        //but it's read-only, so no one can do catalog[0] = somethingElse
        Console.WriteLine($"Catalog holds {catalog.Count} first is {catalog[0].Title}");


        //The other containers, each reached through intent-named methods instead of raw fields:
        //STACK (LIFO) -return cart: the last book dropped is the first re-shelved.
        catalog.DropInReturnCart(catalog[0]);
        catalog.DropInReturnCart(catalog[2]);
        Console.WriteLine($"Return cart has {catalog.CartCount}; reshelving \"{catalog.Reshelve().Title}\" first");

        //QUEUE (FIFO)
        catalog.PlaceHold("Ada");
        catalog.PlaceHold("Grace");
        Console.WriteLine($"{catalog.HoldsWaiting} holds waiting; serving {catalog.ServeNextHold()} first");

        //LINKEDLIST - a reading list we reorder; AddNextUp jumps to the front.
        catalog.AddToReadingList(catalog[0]);
        catalog.AddNextUp(catalog[1]);
        Console.WriteLine("Reafing list order:");
        foreach (LibraryItem item in catalog.ReadingList)
        {
            Console.WriteLine($" - {item.Title}");
        }


        //Enum + Struct use
        ItemKind kind = ItemKind.Magazine; //Example of selecting an enum value;
        
        ShelfLocation location = new ShelfLocation(3,12); // Struct - looks alot like a class, but it is a VALUE type
        
        Console.WriteLine($"{kind} sits at {location}");

        Book duneCopy = dune; //copies the reference
        //Lets say I modify duneCopy, what hapens to the data in dune?
        //All we copied was the pointer - these two things are not independent


        ShelfLocation location2 = location; //copies the data/fields
        //these are not linked in the same way, I can edit the data in one without touching the other

        //Generic: our own Shelf<T> that can hold anything - though technically all the collections
        //we used this far have been generic classes themselves
        Shelf<LibraryItem> shelf = new Shelf<LibraryItem>(2);
        Shelf<int> intShelf = new Shelf<int>(200);

        shelf.TryAdd(catalog[0]);
        shelf.TryAdd(catalog[1]);

        Console.WriteLine($"Trying to add a third thing in our catalog: {shelf.TryAdd(catalog[2])}");

    }

    public static void ExceptionsDemo()
    {
        Console.WriteLine("\n=== Exception, patterns, logging ===");
        
        //By using this Liskov Subsistution from SOLID, if I later swap to 
        //a SQLibraryRepo or whatever, this is the only line I have to changes
        ILibraryRepository repo = new InMemoryLibraryRepository();

        //Injecting our existing repo object to satisfy LibraryUnitOfWork's dependency
        IUnitOfWork libraryWork = new LibraryUnitOfWork(repo);

        //Create a book, but using our factory method - notice
        LibraryItem dune = LibraryItemFactory.Create(ItemKind.Book, "Dune","Frank Herbert", copies:2 , "General");

        repo.Add(dune);

        //Magazines need a publisher, but we provided a default value for the publisher argument in Create
        //Lets see if it works
        repo.Add(LibraryItemFactory.Create(ItemKind.Magazine, "Wired", "Axel", copies: 2));


        //Pretend we're commiting changes to a DB or something
        libraryWork.Stage("added 2 items");
        libraryWork.Commit();

        //We went through the trouble of creating custom exceptions
        //Lets actually see them work for us. If yoy hace code that can pontentially fail
        //wrap ir in a try-catch (optional finally)

        try
        {
            //Potentially offending code goes here
            LibraryItem missing = repo.GetById(99);
            Console.WriteLine(missing.Describe());//We won't hit this I believe

        }
        catch (ItemNotFoundException ex)
        {
            //Your code can potentially throw more tahn one excetion type. Handle them
            //from most -> least specific
            //We stored the offending id on tje exception itself, here we can ask for it logging
            Log.Error("Lookup failed for id {Id}: {Message}",ex.Id, ex.Message);
        }
        catch (LibraryException ex)
        {
            Log.Error("Library Error: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Non Library Error: {Message}", ex.Message);
        }
        finally //optional but adding a finally log adds code that runs wheter an exception is caught or not.
        {//guaranty that this part of the code will run no matter what/ can be used for cleanup or closing important things and even if the try ends in a return
            //Usefull for DB operations where you want to cleanup but you found
            //the object to return
            Console.WriteLine("hit out finally block - lookup attempt done");
        }

        Book noCopies = new Book("Count of Montecristo", "Alejandro Dumas", 0);
        try
        {
            Borrow(noCopies);
        }
        catch (ItemNotAvailableException ex)
        {
            Log.Warning("Borrow refused: {Message}", ex.Message);
        }

    }

    public static void Borrow(Book book)
    {
        // We can use the Checkout (boolean return) method from the book object
        // in an if or something
        if(!book.Checkout())
        {
            throw new ItemNotAvailableException(book.Title);
        }

    }

    public static void AdvanceClassesDemo()
    {
        Console.WriteLine("\n === Advance classes ===");

        //First, a quick detour, lets interact with the Garbage Collector GB
        Console.WriteLine(GC.GetTotalMemory(forceFullCollection: false) / 1024);

        ILibraryRepository repo = new InMemoryLibraryRepository();

        //Create a book, but using our factory method
        LibraryItem dune = LibraryItemFactory.Create(ItemKind.Book, "Dune","Frank Herbert", copies:2 , "General");

        repo.Add(dune);

        //Magazines need a publisher, but we provided a default value for the publisher argument in Create
        //Lets see if it works
        repo.Add(LibraryItemFactory.Create(ItemKind.Magazine, "Wired", "Axel", copies: 2));
        repo.Add(LibraryItemFactory.Create(ItemKind.Book, "Dune Messiah", "Frank Herbert", copies: 3));
        repo.Add(LibraryItemFactory.Create(ItemKind.ReferenceBook, "C# Language Reference", "Microsoft",3, ""));


        Catalog catalog = new();
        foreach (LibraryItem item in repo.GetAll())
        {
            catalog.Add(item);
        }

        Console.WriteLine($"We have {catalog.Authors.Count} unique authors in our catalog");

        foreach(string author in catalog.Authors)
        {
            Console.WriteLine(author);
        }

        //Lets search our catalog now that it's backed by a dictionary
        //Lets use our Find() method

        List<LibraryItem> byFrankHerbert = catalog.Find(item => item.Author == "Frank Herbert");
        Console.WriteLine($"There are {byFrankHerbert.Count} books by Frank Herbert");

        //Lets see how many items in the catalog are Lendable
        Console.WriteLine("We hace a mix of lendable and non-lendable item");

        foreach(LibraryItem item in catalog.Lendable())
        {
            Console.WriteLine($"{item.Title}");
        }
    }

    public static async Task AsyncHttpDemo()
    {
        OpenLibraryClient client = new();

        string[] isbns = { "9780132350884", "9780201633610"};

        // I want to fetch the data from OpenLibrary for both ISBNs
        // I do not want to sit here and type the same 
        //I would end up waiting almost identical calls - thats calid but the curricula says "optimized async code"
        Task<LibraryItem?>[] fetchedBooks = new Task<LibraryItem?>[isbns.Length];

        // Next, we loop through the array and call FetchByIdAsync - we use a traditional C-syntax for-loop
        // beacuase we care about indexes for this
        for (int i=0; i< isbns.Length; i++)
        {
            //Notice, this is an async mehthod call - but we didn't await it.
            fetchedBooks[i] = client.FetchByIsbnAsync(isbns[i]);
        }

        // If we ONLY wanted one book, and we just had one isbn, we could do something like the following
        //foundBook = await client.FetchByIsbnAsync("1234567890123);


        //In between starting the request on line 457, and the Task.WhenAll() call, I can do other stuff. I can call other methods.
        //I can access other systems, etc

        LibraryItem?[] foundBooks = await Task.WhenAll(fetchedBooks);
    
        // This works, but what if there's  nothing there?
        // LibraryItem? firstBookFound = foundBooks[0];

        //To be safe, we can use a quick ternary operator. Like a quick if-else check
        //ternary syntax (some condition to check) ? trueValue : falseValue
        LibraryItem? firstBookFound = foundBooks.Length > 0? foundBooks[0]: null;


        // Using WhenALL to do concurren fecthching. If we didn't do this, and we awaited EVERY SINGLE call one by one
        // Think about the amoun of latency we'd be eating
        Console.WriteLine($"Fetched: {firstBookFound?.Describe() ?? "nothing"}");

        // Boxing and unboxing - mostly deprecated, replaced by Generics
        // Sometimes we needed to store value types on the heap, think of adding an int to a List. Before generics (List<T>)
        // We had ArrayList to accomplish the same thing. Under the hood, an ArrayList couldn't accept value types.

        //We have an int
        int toBeBoxed = 6;

        // We "box it", by giving wrapping it in an object reference
        // So now it's on the heap
        object boxed = toBeBoxed; // This boxing process is something like 15-20x slower than just assigning and int

        // Later, say, when we read something from the ArrayList into an int variable
        int unboxed = (int)boxed;

        // How can we avoid this?
        // DONT USE OLD NON GENERIC COLLECTIONS
        // List<T> is modern, uses generics, avoid box-unbox
        // ArrayList - deprecated, slow, uses boxing and unboxing

        // To read more about this: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing 
    }

}


