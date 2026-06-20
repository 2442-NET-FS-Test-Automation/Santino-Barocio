namespace Playlist.Domain;

public class PlayListItemException : Exception
{
    public PlayListItemException(string message) : base(message){}
}