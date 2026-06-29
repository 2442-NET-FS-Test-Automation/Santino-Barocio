using Library.Domain;

namespace LibraryKata.Domain;

public interface ILibraryRepository
{
    //This interfaces is an abstraction over an actual repository class (concrete implementation)
    //Lets think ow things we want to be able to do against our Library's store of information
    
    //At minimun we want a CRUD

    //Create new items in my library
    void Add(LibraryItem item); //takes in the item to be added, can be anything that inherits from the parent


    //Read/get library items
    LibraryItem GetById(int id); //Trhows ItemnotFounException if the item doesn't exist at all
    List<LibraryItem> GetAll();

    //Update library items


    //Delete items in my library
    bool Remove(int id);


}