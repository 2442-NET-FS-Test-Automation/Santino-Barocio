using System.Text.Json.Nodes;
using Serilog;
namespace Playlist.Domain.DataServices;


public class DataService : IDataService
{
    private readonly HttpClient _client = new();
    
        public async Task<Song?> GetSong(string artist, string song)
    {
        // Normalize input for better matching
        artist = artist.ToLower();
        song = song.ToLower();

        artist = artist.Replace(' ', '_');
        song = song.Replace(' ', '_');
        
        // Construct the search URL
        string searchUrl = 
                $"https://www.theaudiodb.com/api/v1/json/123/searchtrack.php?s={artist}&t={song}";

            try
            {
                // Fetch the data
                string jsonResponse = await _client.GetStringAsync(searchUrl);
                JsonNode? root = JsonNode.Parse(jsonResponse);

                // Check if we got a valid response and if it contains tracks
                if (root != null && root["track"] is JsonArray tracks && tracks.Count > 0)
                {
                    int.TryParse(tracks[0]?["intDuration"]?.ToString(), out int durationInMs);
                    float durationInSeconds = durationInMs / 1000.0f;
                    JsonNode? track = tracks[0];
                    return new Song(
                        track?["strTrack"]?.ToString() ?? "Unknown",
                        track?["strArtist"]?.ToString() ?? "Unknown",
                        durationInSeconds,
                        0,
                         track?["strGenre"]?.ToString() ?? "Unknown",
                         track?["strAlbum"]?.ToString() ?? "Unknown");
                }
                else
                {
                    Log.Warning("No matching song found for Artist: {Artist}, Song: {Song}",
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific network/HTTP errors
                Log.Error(httpEx, "Network error while fetching the song from {Url}", searchUrl);
                return null; 
            }
            catch (Exception ex)
            {
                // Handle other errors (like JSON parsing failures)
                Log.Error(ex, "An unexpected error occurred while parsing song data for Artist: {Artist}", artist);
                return null;
            }
        }
}