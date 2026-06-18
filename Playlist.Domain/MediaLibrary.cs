namespace Playlist.Domain;
//Here we are faking a database to keep track of our media using a PlayListItem List
public class MediaLibrary
{
    private List<PlaylistItem> _library = new List<PlaylistItem>();
    
    public void Add(PlaylistItem item)
    {
        _library.Add(item);
    }
    
    public void Remove(PlaylistItem item)
    {
        _library.Remove(item);
    }
    
    public List<PlaylistItem> GetItems()
    {
        return _library;
    }
    
    public PlaylistItem? GetItem(int id)
    {
        return _library.Find(x => x.Id == id);
    }
    
    
}