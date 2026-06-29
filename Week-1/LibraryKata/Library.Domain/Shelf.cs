
namespace LibraryKata.Domain;

//Generic Shelf
//I want to create a Shelf, and a shelf can hold anything
//I don't want to be limited to LibraryItems, I can put like computer hardware or supplies on the shelf


//T is the standard placeholder for... "some type" that we will determine later
//You will see it all over the place in documentation and code examples
public class Shelf<T>
{
    private readonly T[] _slots;
    private int used;//As things are added ti ny array, the Shelf objet tracks how
    //Slots of the shelf are beign used internally here

    public Shelf(int capacity)
    {
        _slots = new T[capacity];
    }

    //Exposing some array properties as needed
    public int capacity => _slots.Length;
    public int Count => used; // exposing that use as public property

    //Method to add items to our shelf
    public bool TryAdd(T item)
    {
        if (used == _slots.Length)
        {
            return false;
        }


        //If the shelf isn't full then...
        //Access the _slots array's index of the current used +1
        //Increment used
        //Set that index equal to the incoming item
        _slots[used++] = item;
        return true;
    }


    //Method to allow index access

    public T Get(int index)
    {
        return _slots[index];
    }
}  
