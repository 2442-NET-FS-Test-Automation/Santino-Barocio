//This class will be our actual Library Catalog store of info
namespace LibraryKata.Domain;   

using Library.Domain;
using Serilog;

public class InMemoryLibraryRepository : ILibraryRepository
{

    //Beacause we don't have an outside store of info (like a SQL database)
    //We are kind of forced to rely on a list. We will store info outside
    //of program execution - I promise.
    private readonly Dictionary<int, LibraryItem> _items = new();

    public void Add(LibraryItem item)
    {

        //Add a new item 
        //_items.Add(item);

        // New dictionary add code
        _items.Add(item.Id,item);// if we use this method - it DOUES throw when we add a duplicate
        // _items[item.Id] = item; Alternative dictionay adding syntax, adds or ovewrites (doesn't warn you)

        //We just added a new item - thats a significant event. Lets log it
        //Notice not using string interpolation - this uses Serilog's template
        //string format
        Log.Information("Added {Title} - id: {Id}", item.Title,item.Id);
    }

    public List<LibraryItem> GetAll()
    {
        // Don't want to accidentally pass a pointer to my real List
        // return a copu of the list
        // return _items.ToList();

        //Instead of refactoring to work with a dictionay for the return
        // we can just ask for a list of all values in the dictionary
        return _items.Values.ToList();
        
    }

    public LibraryItem GetById(int id)
    {
        //OLD LIST BACKED METHOD FOR LOOKUP
        //In order to find an Item in our collection with the given Id
        //we need to search for it. We could use something like LINQ
        //but that's is own lesson/day
        /*foreach (LibraryItem item in _items)
        {
            if (item.Id == id)
            {
                return item;
            }
        }*/

        // New dictionary backed lookup code
        // TryGetValue uses an out parameter. We pass it some value to do key based lookup
        // We also need to use the out  keyword, and give a type and variable name for the second return.
        // ? - means that this mught be null (if we don0t find anything)
        if (_items.TryGetValue(id, out LibraryItem? item))//using an out parameter to get a second return value
        {
            return item;
        }

        //If we make it here - we exited the foreach without finding an item for that id
        Log.Warning("Lookup failed for id: {Id}",id);
        throw new ItemNotFoundException(id); //Throwing our custom exception, with offending id
    }

    public bool Remove(int id)
    {
        /*   foreach (LibraryItem item in _items)
           {
               if (item.Id == id)
               {
                   _items.Remove(item);
                   Log.Information("Removed item with Id {Id}",id);
                   return true;
               }
           }*/

        if (_items.Remove(id)) // .Remove() returns a true and remove the item if found, reutrns false otherwise
        {
            Log.Information("Removed item with Id {Id}",id);
            return true;
        }
        Log.Information("Removal failed for item with id {Id}",id);
        return false;
    }
}
