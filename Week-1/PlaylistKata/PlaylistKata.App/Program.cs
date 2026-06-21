using Playlist.Domain;
using Playlist.Domain.DataServices;
using Serilog;

namespace PlaylistKata.App;

public class Program
{
    private static MediaLibrary _mediaLibrary = new MediaLibrary();

    public static async Task Main()
    {
        GetSeedItems();
        //Serilog configured once 
        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.Console()
           .WriteTo.File("logs/logger.txt")
           .CreateLogger();


        var running = true;
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
                    Console.WriteLine("Search song on API!");
                    await AsyncSearchFromApi();
                    break;
                case 6:
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
        Console.WriteLine("=== Menu ===");
        Console.WriteLine("Select an option");
        Console.WriteLine("\n 1.Add track");
        Console.WriteLine("\n 2.Play");
        Console.WriteLine("\n 3.Show media library");
        Console.WriteLine("\n 4.Total playlist time");
        Console.WriteLine("\n 5. Search from API");
        Console.WriteLine("\n 6.Exit\n");
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

        Console.Write("Type the track Duration: ");
        float.TryParse(Console.ReadLine(), out float duration);

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
                string album = Console.ReadLine();
                _mediaLibrary.Add(new Song(trackName, author, duration, 0, genre, album));
                break;
            case 2:
                Console.WriteLine("Podcast!");
                Console.Write("Type the Special Guest: ");
                string specialGuest = Console.ReadLine();
                _mediaLibrary.Add(new Podcast(trackName, author, duration, 0, specialGuest));
                break;
            case 3:
                Console.WriteLine("AudioBook!");
                Console.Write("Type the Name of the Voice Actor: ");
                string reader = Console.ReadLine();
                _mediaLibrary.Add(new AudioBook(trackName, author, duration, 0, reader));
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

        foreach (PlaylistItem track in _mediaLibrary.GetItems())
        {
            if (track.Title == trackName)
            {
                track.Play();
                Console.WriteLine($"Now playing: {track.Title} (Played {track.TimesPlayed} times)");
                return;
            }
        }
        
        Console.WriteLine("Track not found.");
    }

    private static void ListTrack()
    {
        foreach (PlaylistItem track in _mediaLibrary.GetItems())
        {
            Console.WriteLine(track.Describe());
        }
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
    }
}