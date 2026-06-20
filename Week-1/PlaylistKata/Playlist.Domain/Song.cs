namespace Playlist.Domain;

public class Song: PlaylistItem
{
    public string Genre {get; private set;}
    public string Album {get; private set;}
    
    public Song(string title, string author, float duration, int timesplayed, string genre, string album) : base(title, author, duration, timesplayed)
    {
        Genre = genre;
        Album = album;
    }
    
    public override string Describe()
    {
        return $"Track: {Id},Title: {Title}, genre: {Genre}, duration: {Duration} by {Author}. (Times played {TimesPlayed})";
    }
}