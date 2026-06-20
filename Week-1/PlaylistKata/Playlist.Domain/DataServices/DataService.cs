using System.Text.Json;

namespace Playlist.Domain.DataServices;


public class DataService : IDataService
{
    private readonly HttpClient _client = new();

    public async Task<Song?> GetSong(string artist, string song)
    {
        artist = artist.ToLower();
        song = song.ToLower();

        artist = artist.Replace(' ', '_');
        song = song.Replace(' ', '_');
        
        string searchUrl = 
                $"https://www.theaudiodb.com/api/v1/json/123/searchtrack.php?s={artist}&t={song}";

            try
            {
                // Fetch the data
                // HttpResponseMessage response = await _client.GetAsync(searchUrl);
            
                // Throw an exception if the status code is not a success code (e.g., 404, 500)
                // response.EnsureSuccessStatusCode(); 

                // Await the asynchronous read (Never use .Result in async code to avoid deadlocks)
                string jsonResponse = await _client.GetStringAsync(searchUrl);

                // Deserialize the JSON string into your C# object
                // Song? result = JsonSerializer.Deserialize<Song>(jsonResponse);
                //Song? result = new Song(jsonResponse.)
            
                return Parse(jsonResponse);
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific network/HTTP errors
                Console.WriteLine($"Network error while fetching the song: {httpEx.Message}");
                return null; 
            }
            catch (Exception ex)
            {
                // Handle other errors (like JSON parsing failures)
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

    public static Song? Parse(string json)
    {
        Dictionary<string, JsonElement>? resp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if(resp == null || !resp.TryGetValue("track", out JsonElement tracks) || tracks.GetArrayLength() == 0)
            return null;
        
        JsonElement foundTracks = tracks[0]; // get track by index [0] thats because there's only one element here

        // get title
        string title = foundTracks.GetProperty("strTrack").GetString() ?? "Untitled";

        // get author
        string author = foundTracks.GetProperty("strArtist").GetString() ?? "None";

        // get duration
        // the data received from json will always be a string, so there must be a conversion to float
        string durationStr = foundTracks.GetProperty("intDuration").GetString() ?? "0";

        float duration = 0f;
        if(int.TryParse(durationStr, out int durationMs)) // try conversion from string to float
            duration = durationMs / 1000f; // convert miliseconds to seconds
        
        // get timesplayed
        string timesplayedStr = foundTracks.GetProperty("intTotalPlays").GetString() ?? "0";

        int timesplayed=0;
        if(int.TryParse(timesplayedStr, out int timesplayednewInt)) // convert string to int
            timesplayed = timesplayednewInt;
            
        // get genre
        string genre = foundTracks.GetProperty("strGenre").GetString() ?? "No album";

        // get album
        string album = foundTracks.GetProperty("strAlbum").GetString() ?? "No album";

        // set json info and pass it on the parameters, then create a song with the same data obtained
        return (Song?)PlaylistItemFactory.Create(ItemKind.Song, title, author, duration, timesplayed, genre, album);
 
    }
}