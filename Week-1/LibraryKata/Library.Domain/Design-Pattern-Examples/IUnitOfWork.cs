namespace LibraryKata.Domain;

public interface IUnitOfWork
{
    //THis is not a method, this is a property
    ILibraryRepository Items {get;}

    void Stage(string change); //method to allow us to stage changes - Like "git add"

    int Commit(); //method to actually commit those changes
}