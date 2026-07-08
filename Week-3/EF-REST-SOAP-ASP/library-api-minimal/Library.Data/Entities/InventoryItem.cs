namespace Library.Data.Entities;

public class InventoryItem
{
    public int Id {get; set;}
    public int ProductId {get; set;} // FK - 1:1 with product
    public Product Product {get; set;} = default!; // we can have EF give a default value
    public int CurrentStock {get; set;} // how many of this thing do we have

    // Adding a RowVersion property - we sill use this in OnModelCreaton
    // We will use this to tracj concurrency
    public byte[] RowVersion {get;set;} = default!;
}