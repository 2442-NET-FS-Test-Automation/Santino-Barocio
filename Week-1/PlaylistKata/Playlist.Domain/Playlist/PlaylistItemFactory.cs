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
        string album="n/a",
        string voicereader="n/a",
        string specialguest="n/a")
    {
        switch (kind)
        {
            case ItemKind.Song:
                return new Song(title, author, duration, timesplayed, genre,album);
            case ItemKind.AudioBook:
                return new AudioBook(title, author, duration, timesplayed,voicereader);
            case ItemKind.Podcast:
                return new Podcast(title, author, duration, timesplayed, specialguest);
            default:
                throw new PlayListItemException($"Unknown playlist item kind: {kind}"); // if item not found it throws an exception
        }
    }
}