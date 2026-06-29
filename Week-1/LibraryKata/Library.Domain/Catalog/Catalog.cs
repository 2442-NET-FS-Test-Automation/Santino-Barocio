using Library.Domain;

namespace LibraryKata.Domain;


//I've tuerned my Catalog class into a partial class
//I can now stretch it's class definition across multiple files - when I build, all the content from all the files is stitched together
public partial class Catalog
{
    //Encapsulation change: these four collections used to be PUBLIC fields, so callers
    //reached straight in (catalog._items.Add(..)). That leakd the implementation -every
    //caller becomes coupled to "it's a List", and nothing stops them clearing or reordering
    //it behinf the Catalog's back. We make te containers PROVATE and expose intent-named
    //methods instead. The class still just wraps these containers, but now IT owns how they
    //are used, and we can swap a backing store or add validation later without touching a
    //single calle

    //Backing out catalog is going to be a list
    //Your default collection - even above Array.

    // Those above are the same just diferent typing
    // public readonly List<LibraryItem> _items  = [];

    //List<T>: ordered, grow/shrink dynamically, accessible via index. Your default collection
    private readonly List<LibraryItem> _items  = new();

    //Stack<T>: LIFO- We will model a return cart. The most recently returned item
    //is re-shelved first.
    //Primary methods - Push() puts an item at the top of the stack.
    //                  Pop()  removes the top most item.
    private readonly Stack<LibraryItem> _returnCart = new();

    //Queue<T>: FIFO- modeling a hold queue, customers placing holds on books
    //Primary  methods - Enqueue(): join the back of the line
    //                   Dequeue(): removed from the front of the line
    private readonly Queue<string> _holdQueue = new ();

    //Reading list
    // LinkedList<T>: cheap inserts/removals anywhere in my list, but NO index access.
    private readonly LinkedList<LibraryItem> _readingList = new();
    
    //HASHET<T>: unique values, O(1) Lookup. Adding a duplicate silently fails
    //collection of all authors in my catalog
    private readonly HashSet<string> _authors = new();

    //-- Hashset surface --
    public IReadOnlyCollection<string> Authors => _authors;


    //List surface
    //Weapping Add/Remove/ index is the wole point of encapsulation: callers state intent,
    //the Catalog decides how. Count was already exposed this way; now the rest is too.
    public int Count => _items.Count;
    public LibraryItem this[int index] => _items[index];
    public void Add(LibraryItem item)
    {
        _items.Add(item);
        _authors.Add(item.Author);//Hashet will ignore duplicate authors
    }
    public bool Remove(LibraryItem item) => _items.Remove(item);

    //This syntax with the => is the "express body syntax", if a method only runs a single expression (one line)
    //then we can use this as a shorthand. The Compiler will infer the rest.
    public bool isEmpty => _items.Count == 0;

    // --- Stack surface (return cart) ---
    public void DropInReturnCart(LibraryItem item) => _returnCart.Push(item);
    public LibraryItem Reshelve() => _returnCart.Pop();
    public int CartCount => _returnCart.Count;

    // --- Queue surface (holds line) ---
    public void PlaceHold(string member) => _holdQueue.Enqueue(member);
    public string ServeNextHold() => _holdQueue.Dequeue(); //earliest request first (FIFO)
    public int HoldsWaiting => _holdQueue.Count;

    // --- LinkedList surface (reading list) ---
    public void AddToReadingList(LibraryItem item) => _readingList.AddLast(item);
    public void AddNextUp(LibraryItem item) => _readingList.AddFirst(item);//jump to the front of the list
    //Expose as IEnumerable so callers can foreach over it but cannot mutate the linked list directly

    public IEnumerable<LibraryItem> ReadingList => _readingList;
}