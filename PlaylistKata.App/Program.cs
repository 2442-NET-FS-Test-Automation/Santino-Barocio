using Playlist.Domain;

namespace PlaylistKata.App;

public class Program
{
    private static List<PlaylistItem> PlaylistItems = GetSeedItems();

    public static void Main()
    {
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
                    Console.WriteLine("Bye bye!");
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }
    }

    private static void Menu()
    {
        Console.WriteLine("=== Menu ===");
        Console.WriteLine("Select an option");
        Console.WriteLine("\n 1.Add track");
        Console.WriteLine("\n 2.Play");
        Console.WriteLine("\n 3.List");
        Console.WriteLine("\n 4.Total playlist time");
        Console.WriteLine("\n 5.Exit\n");
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
                PlaylistItems.Add(new Song(trackName, author, duration, 0, genre));
                break;
            case 2:
                Console.WriteLine("Podcast!");
                Console.Write("Type the Special Guest: ");
                string specialGuest = Console.ReadLine();
                PlaylistItems.Add(new Podcast(trackName, author, duration, 0, specialGuest));
                break;
            case 3:
                Console.WriteLine("AudioBook!");
                Console.Write("Type the Name of the Voice Actor: ");
                string reader = Console.ReadLine();
                PlaylistItems.Add(new AudioBook(trackName, author, duration, 0, reader));
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

        foreach (PlaylistItem track in PlaylistItems)
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
        foreach (PlaylistItem track in PlaylistItems)
        {
            Console.WriteLine(track.Describe());
        }
    }

    private static void TimeTrack()
    {
        float total = CalculateTotalTime(PlaylistItems);
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
    
    public static List<PlaylistItem> GetSeedItems()
    {
        return new List<PlaylistItem> {
            new AudioBook("Book1", "Author1", 1.1f, 0, "Reader1"),
            new AudioBook("Book2", "Author2", 2.2f, 0, "Reader2"),
            new Song("Song1", "Artist1", 3.3f, 0, "Rock"),
            new Song("Song2", "Artist2", 4.4f, 0, "Pop"),
            new Podcast("Pod1", "Host1", 5.5f, 0, "Guest1"),
            new Podcast("Pod2", "Host2", 6.6f, 0, "Guest2")
        };
    }
}