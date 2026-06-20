namespace Playlist.Domain;
public static class PlaylistItemFactory
{
    public static PlaylistItem Create(
        ItemKind kind,
        string title,
        string author,
        float duration,
        int timesplayed,
        string genre="n/a",
        string voicereader="n/a",
        string specialguest="n/a")
    {
        switch (kind)
        {
            case ItemKind.Song:
                return new Song(title, author, duration, timesplayed, genre);
            case ItemKind.AudioBook:
                return new AudioBook(title, author, duration, timesplayed,voicereader);
            case ItemKind.Podcast:
                return new Podcast(title, author, duration, timesplayed, specialguest);
            default:
            // here I would have to add an unknown item exception
                throw new ArgumentException(); // this line will be modified
        }
    }
}