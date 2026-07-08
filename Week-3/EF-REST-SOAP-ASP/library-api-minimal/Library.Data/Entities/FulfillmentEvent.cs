namespace Library.Data.Entities;

public class FulfillmentEvent
{
    public int Id {get; set;}
    public int OrderId {get; set;}
    // = default! is something we're doing for EF Core. If we were to make this nullable we'd
    // satisfy the compiler - but what if I DON'T want the database to allow a null?
    // = default! Lets me shove some default value (varies per type) into the property on creation
    public string Type {get; set;} = default!;
    public DateTime FulfilledAtUtc{get; set;} = DateTime.UtcNow;
}