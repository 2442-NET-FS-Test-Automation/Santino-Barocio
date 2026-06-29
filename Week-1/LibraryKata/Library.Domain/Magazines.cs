namespace Library.Domain;

//sealed is pretty simple, it means this class is not inheritable
//Nobody can be a child of Magazine. MOre a signal of intent and design than anything, but still useful.
public sealed class Magazine: LibraryItem, ILendable
{
    public int CirculationCopies { get; private set;}
    public string Publisher {get; private set;}

    public Magazine(string title, string author, int circulationCopies, string publisher) 
        : base(title, author)
    {
        CirculationCopies = circulationCopies;
        Publisher = publisher;

    }

    //OVERLOADING: same constructor name, different parameter list - the compier picks
    //which one to run based on the arguments you pass
    //A LibraryItemFactory (Week 2) builds every item from the canonical
    //(title, author, copies) shape. This overload lets Magazine join that common path
    //without forcing the caller to invent a publisher. It chains to the full constructor
    //Above with ': this(...)', supplying a default publisher
    public Magazine(string title, string author, int circulationCopies)
        : this(title, author, circulationCopies, "Unknown")
    {
    }


    public override string Describe()
    {
        return $"{Title} by {Author}";
    }

    //Providing implementation via "new" instead of override - has implications for Later
    //this is thecnically Method Hiding - depends on the reference type
    //Calling this method in an object instantiated like this:
    //LibraryItem sportsIllustrated = new MAgazine(...); - calls LibraryItem's ShelfLabel
    //This is most likely not what you want
    //new vs override - bery different behavior
    public new string ShelfLabel()
    {
        return $"MAG-{Id} {Title}";
    }

    public bool Checkout()
    {
        //Ateempt to checkout a book - if copies is already 0, return false
        if (CirculationCopies == 0)
        {
            return false;
        }
        //Otherwise, we pass over the above code block
        //We can decrement the available copies and return true
        CirculationCopies--;
        return true;
    }

    //Providing for return behavior
    public void ReturnCopy() => CirculationCopies++;

}