namespace Library.Domain;


//Library item will be an abstract class - it cannot be instantiated.
//It Will still have a constructor - because child classes NEED to be able 
//to call their parent's constructor - but WE can't call it via new
public abstract class LibraryItem
{
    //Things about a book we can model - what is the "shape" of a book
    //Because I want to use a no-arg Constructor, its best practice to make
    //my properties nullable.
    public string? Title {get; private set;}//Auto property syntax - no writing getters and setters
    public string? Author {get; private set;}
  
    //The same way we can have static methods (belong to the class)
    //We can have static properties/members
    private static int _nextId = 1;//By convention, static properties have an underscore
    public int Id {get;}// No setterm, I don't want someone to reassign this.

    // My abstract class DOES have a constructor
    // public: anyone can see/call this
    // private: only accessible within this class
    // protected: this class and derived (child) classes only

    protected LibraryItem(string title, string author)
    {
        Id = _nextId++;
        Title = title;
        Author = author;
    }

    // Abstract Method - Only a signature - no body
    public abstract string Describe();

    //Abstract classes CAN contain concrete implementation - and can mix the abstract methods to save time later
    //potentially. Our child WILL implement Describe() - use that for the ToString()
    public override string ToString() => Describe();
    
    //Concrete methods have a body, Abstract methods MUST be override, virtual methods have a body and MAY be overridden
    public virtual string ShelfLabel()
    {
        return $"{Id}: {Title}";
    }
}
