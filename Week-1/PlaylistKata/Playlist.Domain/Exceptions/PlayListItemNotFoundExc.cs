namespace Playlist.Domain;

public class PlaylistItemNotFoundExc : Exception
{
    public int Id {get;}

    public PlaylistItemNotFoundExc(int id)
        : base($"No track with id {id}")
    {
        Id = id;
    }

    
}