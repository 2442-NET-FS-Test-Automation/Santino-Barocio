using Playlist.Domain;
using Playlist.Domain.DataServices;
using Playlist.Domain.Utils;
using Serilog;


namespace PlaylistKata.App;

public class Program
{
    private static MediaLibrary _mediaLibrary = new MediaLibrary();

    private static bool _isPlaying = false;
    private static PlaylistItem? _currentTrack = null;

    public static async Task Main()
    {
        GetSeedItems();
        //Serilog configured once 
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.Console()
           .WriteTo.File("logs/logger.txt")
           .CreateLogger();


        bool running = true;
        while (running)
        {
            Menu();
            if (!int.TryParse(Console.ReadLine(), out int choice)) continue;

            switch (choice)
            {
                case 1:
                    Console.WriteLine("Adding a track!");
                    AddTrack();
                    break;
                case 2:
                    Console.WriteLine("Playing a track!");
                    PlayTrack();
                    break;
                case 3:
                    Console.WriteLine("Listing the tracks!");
                    ListTrack();
                    break;
                case 4:
                    Console.WriteLine("Total tracks time!");
                    TimeTrack();
                    break;
                case 5:
                    Console.WriteLine("Search song on API and Add!");
                    await AsyncSearchFromApi();
                    break;
                case 6:
                    Console.WriteLine("Placing a track in the Display Case!");
                    PlaceInGrid();
                    break;
                case 7:
                    _mediaLibrary.ShowDisplayCase();
                    break;
                case 8:
                    Console.WriteLine("Custom Search!");
                    SearchByDuration();
                    break;
                case 9:
                    Console.WriteLine("Lazy loading tracks!");
                    ListTracksLazily();
                    break;
                case 10:
                    Console.WriteLine("Bye bye!");
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }
        

        //Serilog being closed
        Log.CloseAndFlush();
    }

    private static void Menu()
    {

        if (_isPlaying && _currentTrack != null)
            {
                Console.WriteLine($"[ ▶ NOW PLAYING: {_currentTrack.Title} ]");
            }

        Console.WriteLine("\n=== Menu ===");
        Console.WriteLine("Select an option");
        Console.WriteLine(" 1. Add track");
        Console.WriteLine(" 2. Play");
        Console.WriteLine(" 3. Show media library");
        Console.WriteLine(" 4. Total playlist time");
        Console.WriteLine(" 5. Search from API and Add");
        Console.WriteLine(" 6. Place track in Display Case");
        Console.WriteLine(" 7. View physical Display Case");
        Console.WriteLine(" 8. Search tracks by minimum duration");
        Console.WriteLine(" 9. Lazy load tracks (step-by-step)");
        Console.WriteLine(" 10. Exit\n");
    }

    private static void AddMenu()
    {
        Console.WriteLine("=== AddMenu ===");
        Console.WriteLine("Select an option");
        Console.WriteLine("\n 1.Song");
        Console.WriteLine("\n 2.Podcast");
        Console.WriteLine("\n 3.AudioBook");
    }

    private static void AddTrack()
    {
        Console.Write("Type the track name: ");
        string trackName = Console.ReadLine();

        Console.Write("Type the Author of the track: ");
        string author = Console.ReadLine();

        Console.Write("Type the track Duration (Format MM:SS, e.g., 03:45): ");
        string? durationInput = Console.ReadLine();

        if (!InputValidator.TryValidateDuration(durationInput, out float duration))
        {
            Console.WriteLine("Error: Malformed duration format. Track was not saved.\n");
            return; // Falla ruidosamente y aborta la creación
        }

        AddMenu();
        Console.Write(": ");
        int.TryParse(Console.ReadLine(), out int choice);

        switch (choice)
        {
            case 1:
                Console.WriteLine("Song!");
                Console.Write("Type the track Genre: ");
                string genre = Console.ReadLine();

                Console.Write("Type the track album: ");
                string? album = Console.ReadLine();
                Song newSong = new Song(trackName, author, duration, 0, genre, album); // adding song to a library before the confirmation
                _mediaLibrary.Add(newSong);
                ConfirmSelection(newSong); // confirm if the song added was correct, that's for making use of the Undo method
                break;
            case 2:
                Console.WriteLine("Podcast!");
                Console.Write("Type the Special Guest: ");
                string specialGuest = Console.ReadLine();
                Podcast newPodcast = new Podcast(trackName, author, duration, 0, specialGuest);
                _mediaLibrary.Add(newPodcast);
                ConfirmSelection(newPodcast);
                break;
            case 3:
                Console.WriteLine("AudioBook!");
                Console.Write("Type the Name of the Voice Actor: ");
                string reader = Console.ReadLine();
                AudioBook newAudioBook = new AudioBook(trackName, author, duration, 0, reader);
                _mediaLibrary.Add(newAudioBook);
                ConfirmSelection(newAudioBook);
                break;
            default:
                Console.WriteLine("Invalid choice");
                break;
        }
    }

    private static void PlayTrack()
    {
        Console.Write("Type the track name to play: ");
        string trackName = Console.ReadLine();

        if (string.IsNullOrEmpty(trackName))
        {
            Console.WriteLine("You must type a valid track name.");
            return;
        }

        foreach (PlaylistItem track in _mediaLibrary.GetItems())
        {
            if (track.Title != null && track.Title.Equals(trackName, StringComparison.OrdinalIgnoreCase))
            {
                if (_isPlaying)
                {
                    Console.WriteLine("Stopping current track...");
                }

                StartBackgroundPlayback(track);
                Console.WriteLine($"\nStarted playing: {track.Title} in the background.");
                return;
            }
        }
        
        Console.WriteLine("Track not found.");
    }

    private static void ListTrack()
    {
        Console.WriteLine($"\n--- Library (Unique Artists/Authors: {_mediaLibrary.Authors.Count}) ---");
        foreach (PlaylistItem track in _mediaLibrary.GetItems())
        {
            Console.WriteLine(track.Describe());
        }
        Console.WriteLine("----------------------------------\n");
    }

    private static void TimeTrack()
    {
        float total = CalculateTotalTime(_mediaLibrary.GetItems());
        Console.WriteLine($"Total playlist time: {total}");
    }

    
    private static float CalculateTotalTime(List<PlaylistItem> items)
    {
        float trackTime = 0;
        foreach (PlaylistItem track in items)
        {
            if (track.Duration.HasValue)
            {
                trackTime += track.Duration.Value;
            }
        }
        return trackTime;
    }

    public static void ConfirmSelection(PlaylistItem item)
    {
        Console.WriteLine("Is your selection correct? ");
        Console.Write("Type 'y' to confirm: ");
        string answer = Console.ReadLine();

        if(answer.ToLower() != "y")
        {
            _mediaLibrary.Undo();
        }
    }
    
    public static void GetSeedItems()
    {
        _mediaLibrary.Add(new AudioBook("Book1", "Author1", 1.1f, 0, "Reader1"));
        _mediaLibrary.Add(new AudioBook("Book2", "Author2", 2.2f, 0, "Reader2"));
        _mediaLibrary.Add(new Song("Song1", "Artist1", 3.3f, 0, "Rock", "No"));
        _mediaLibrary.Add(new Song("Song2", "Artist2", 4.4f, 0, "Pop", "Yes"));
        _mediaLibrary.Add(new Podcast("Pod1", "Host1", 5.5f, 0, "Guest1"));
        _mediaLibrary.Add(new Podcast("Pod2", "Host2", 6.6f, 0, "Guest2"));
    }

    // Search, type the data below and get api information about the song
    public static async Task AsyncSearchFromApi()
    {
        Console.Write("Type the Artist: ");
        string? artist = Console.ReadLine() ?? "";

        Console.Write("Type the Song: ");
        string? song = Console.ReadLine() ?? "";

        var DataService = new DataService();
        Song? result = await DataService.GetSong(artist, song);

        if(result == null)
        {
            Console.WriteLine("Song could not be found!");
            return;
        }

        _mediaLibrary.Add(result);
        Console.WriteLine($"Added new Song: {result.Describe()}");
        ConfirmSelection(result); // confirm typing 'y' in the keyboard
    }

    private static void PlaceInGrid()
    {
        Console.Write("Type the track ID to place: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID format.");
            return;
        }

        try
        {
            PlaylistItem track = _mediaLibrary.GetItemById(id);
            
            Console.Write("Enter Row (0 to 2): ");
            int.TryParse(Console.ReadLine(), out int row);
            
            Console.Write("Enter Column (0 to 2): ");
            int.TryParse(Console.ReadLine(), out int col);
            
            _mediaLibrary.PlaceInDisplayCase(track, row, col);
            Console.WriteLine($"Track '{track.Title}' placed successfully at [{row},{col}]!");
        }
        catch (PlaylistItemNotFoundExc ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("--- End of display placement attempt ---\n");
        }
    }

    private static void SearchByDuration()
    {
        Console.Write("Find tracks longer than (minutes, e.g., 3.5): ");
        if (float.TryParse(Console.ReadLine(), out float minDuration))
        {
            List<PlaylistItem> results = _mediaLibrary.Find(item => item.Duration.HasValue && item.Duration.Value > minDuration);
            
            Console.WriteLine($"\n--- Tracks longer than {minDuration} mins ---");
            if (results.Count == 0)
            {
                Console.WriteLine("No tracks found.");
            }
            else
            {
                foreach (var item in results)
                {
                    Console.WriteLine(item.Describe());
                }
            }
            Console.WriteLine("----------------------------------\n");
        }
        else
        {
            Console.WriteLine("Invalid duration format.");
        }
    }

    private static void ListTracksLazily()
    {
        Console.WriteLine("\n--- Lazy Loading Library ---");
        
        foreach (PlaylistItem track in _mediaLibrary.GetItemsLazy())
        {
            Console.WriteLine(track.Describe());
            Console.Write("Press Enter to fetch the next track (or type 'q' to stop): ");
            
            string? input = Console.ReadLine();
            if (input?.ToLower() == "q")
            {
                break;
            }
        }
        
        Console.WriteLine("--- End of Lazy Load ---\n");
    }

    private static void StartBackgroundPlayback(PlaylistItem track)
        {
            _isPlaying = true;
            _currentTrack = track;
            track.Play(); 
            Task.Run(async () =>
            {
                int durationMs = track.Duration.HasValue ? (int)(track.Duration.Value * 1000) : 5000;
                
                await Task.Delay(durationMs); // Espera sin bloquear el hilo
                
                _isPlaying = false;
                _currentTrack = null;
            });
        }

}