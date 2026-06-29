namespace Playlist.Domain;

//agregado sealed
public sealed class Podcast : PlaylistItem
{
    public string SpecialGuest {get;}

    public Podcast(string title, string author, float duration, int timesplayed, string specialguest) : base (title, author, duration, timesplayed)
    {
        SpecialGuest = specialguest;
    }

    public override string Describe()
    {
        return $"Track: {Id},Title: {Title}, duration {Duration} by {Author}, The Special Guest is: {SpecialGuest}. (Times played {TimesPlayed})";
    }

}