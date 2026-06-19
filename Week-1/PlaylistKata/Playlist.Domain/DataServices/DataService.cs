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
                HttpResponseMessage response = await _client.GetAsync(searchUrl);
            
                // Throw an exception if the status code is not a success code (e.g., 404, 500)
                response.EnsureSuccessStatusCode(); 

                // Await the asynchronous read (Never use .Result in async code to avoid deadlocks)
                JsonElement jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON string into your C# object
                //Song? result = JsonSerializer.Deserialize<Song>(jsonResponse);
                Song? result = new Song(jsonResponse.)
            
                return result;
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
}