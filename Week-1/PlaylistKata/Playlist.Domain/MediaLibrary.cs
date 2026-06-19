using System.Runtime.InteropServices;

namespace Playlist.Domain;
//Here we are faking a database to keep track of our media using a PlayListItem List
public class MediaLibrary
{
    private readonly Stack<(string action, PlaylistItem item)> _undoSong = new(); 
    private List<PlaylistItem> _library = new List<PlaylistItem>();
    
    public void Add(PlaylistItem item)
    {
        _library.Add(item);
        _undoSong.Push(("Add",item));
    }
    
    public void Remove(PlaylistItem item)
    {
        _library.Remove(item);
        _undoSong.Push(("Remove", item));
    }

    public void Undo()
    {

        if(undoCount == 0)
        {
            Console.WriteLine("Nothing to undo!\n");
            return;
        }

        var (action, item) = _undoSong.Pop();
        if (action == "Add") _library.Remove(item);
        else if(action == "Remove") _library.Add(item);
    }
    
    public List<PlaylistItem> GetItems() => _library;
    
    public PlaylistItem? GetItem(int id) => _library.Find(x => x.Id == id);
    
    public int undoCount => _undoSong.Count;
    
    public bool hasUndoHistory => _undoSong.Count > 0;
}