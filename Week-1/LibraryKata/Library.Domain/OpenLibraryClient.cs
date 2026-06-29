using System.Security.Cryptography.X509Certificates;
using System.Text.Json; //Library for working with JSON - written by Microsoft.
using Library.Domain;
using Serilog;

namespace LibraryKata.Domain;

public class OpenLibraryClient
{
    //We are going to create and use ONE HTTPClient for the entire process.
    //If you use one per call, you are going to Leak sockets - and eventually trigger a SocketException

    private static readonly HttpClient client = new();

    //We are going to write an async method. An async method is ANY method that cals async code.
    //So, if you use something like .FindAsync() OR you "await" a method call within a method body
    //the surrounding method MUST be declared as async

    //A Task in C# is like a Promise in JS - it is a placeholder in memory telling the runtime
    //"I expect there to be a LibraryItem (or whatever the Task is "wrapping" with it's brackets)
    //- When this method resolves. I have no idea when that is, so for now - hold that place with a Task."
    //We are also going account for the possiblity of a null - because my HTTP call could fail for a number of reasons
    //I could be rate limited, asked for a bad isbn, OpenLibrary might be dow, etc.
    public async Task<LibraryItem> FetchByIsbnAsync(string isbn)
    {
        // some http request
        string url = $"https://openlibrary.org/search.json?q=isbn:{isbn}&fields=title,author_name&limit=1";
        //This could fail for a ton of reasons - I don't control OpenLibrary OR the internet between my Laptop
        //and their servers
        try
        {
            //We are going to try get back a json formatted string from the API
            //Whenever we call upon an async method, we must await the call
            string jsonResponse = await client.GetStringAsync(url);

            //We are going to write our own parsing logic in a method called Parse()
            //Thankfully the return from OpenLibrary are small. For an unmanageable return
            //see pokeAPI
            return Parse(jsonResponse);
        }
        catch(HttpRequestException ex)
        {
            Log.Warning("Network fetch failed for {isbn}: {Message}", isbn, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning("FetchBYIsbnAsync failed: {Message}", ex.Message);
            return null;
        }
        
    }

    //We are going to write our own parsing logic
    //Mostly as an excercise to work with Json

    public static LibraryItem? Parse(string json)
    {
        // The Search API within OpenLibrary returns a JSON object, and inside that object, among other fields -
        // is a "docs" array. If we find the book we want based on it's isbn that searched for, it's inside that array
        Dictionary<string, JsonElement>? resp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        // If we didn't find anything )because the isbn wasn't valid) book will be null OR the Length of the docs array is 0
        //If the book ditionary is null, then string json was empty
        // If book.TryGetValue returns false, then we got something back, with no docs array somehow - so no book to return
        // If the docs array has length of 0, then we got the correct object shape back, nut our isbn didin't match anything - no book to return 
        if (resp is null || !resp.TryGetValue("docs", out JsonElement docs) || docs.GetArrayLength() == 0)
        {
            return null; //no docs array somehow, docs arrray is empty, or the json itself was empty - return a null
        }

        JsonElement foundBook = docs[0]; //If we get something back, we should only get one thing. We searched by isbn - they're unique

        //Now we can unpack things about this foundBook
        //We are usig the ?? null coallescing operator
        //IF something is there: return the value resulting from the code to the left of the ??
        //IF something is not there, return whatever we assign as a "default" to the right of the ??
        string title = foundBook.GetProperty("title").GetString() ?? "Untitled";

        //Gettung the author is less straightforwars because of the fact that books can have more tan one author
        //So there is not a single value author propertum it's another array
        string author = "Unknown";

        //Checking to see if we hace the author array, and if it's there grab the first author
        if(foundBook.TryGetProperty("author_name", out JsonElement authors) && authors.GetArrayLength() > 0)
        {
            author = authors[0].GetString()??"Unknown";
        }

        return LibraryItemFactory.Create(ItemKind.Book, title, author);

    }

}



