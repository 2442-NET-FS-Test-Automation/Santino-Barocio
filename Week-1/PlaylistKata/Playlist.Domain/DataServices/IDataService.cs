namespace Playlist.Domain.DataServices;

public interface IDataService
{
    Task<Song?> GetSong(string artist, string song);
}