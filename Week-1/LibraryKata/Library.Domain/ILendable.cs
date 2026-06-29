namespace Library.Domain;

//Interfaces in C# are contract for the behaviors they do not define the implementation of the methods within.
public interface ILendable
{

    // Only metod signatures, not bodies, not even access modifiers
    bool Checkout();
    
    void ReturnCopy();

}