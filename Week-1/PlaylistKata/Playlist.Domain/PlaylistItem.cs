namespace Playlist.Domain;

public abstract class PlaylistItem
{
    private static int _nextId = 1;
    public int Id {get;}
    public string? Title {get; private set;}
    public string? Author {get; private set;}
    public float? Duration {get; private set;}
    public int TimesPlayed {get; private set;}


    protected PlaylistItem(string title, string author, float duration , int timesplayed)
    {
        Id = _nextId++;
        Title = title;
        Author = author;
        Duration = duration;
        TimesPlayed = timesplayed;
    }

    public void Play() => TimesPlayed += 1;

    public void Play(int times) => TimesPlayed += times;


    public virtual string Describe()
    {
        return $"{Id}: {Title}";
    }
}
