using System.Runtime.InteropServices;

namespace Playlist.Domain;
//Here we are faking a database to keep track of our media using a PlayListItem List
public class MediaLibrary
{
    // a stack works as a pile of plates (LAST IN FIRST OUT)
    private readonly Stack<(string action, PlaylistItem item)> _undoSong = new(); 
    private List<PlaylistItem> _library = new List<PlaylistItem>();
    
    public void Add(PlaylistItem item)
    {
        _library.Add(item);
        _undoSong.Push(("Add",item)); // Registers the action into the stack (action = "Add")
    }
    
    public void Remove(PlaylistItem item)
    {
        _library.Remove(item);
        _undoSong.Push(("Remove", item)); // Registers the action into the stack (action = "Remove")
    }

    public void Undo()
    {

        if(undoCount == 0)
        {
            Console.WriteLine("Nothing to undo!\n");
            return;
        }

        var (action, item) = _undoSong.Pop(); // removes and returns the object located at the top of a <Stack>
        if (action == "Add") _library.Remove(item); // undo: if the element was added before and there was an undo, remove it from the list
        else if(action == "Remove") _library.Add(item); // undo: if the element was removed and you needed to undo, then it gets added again
    }
    
    public List<PlaylistItem> GetItems() => _library;
    
    public PlaylistItem? GetItem(int id) => _library.Find(x => x.Id == id);
    
    public int undoCount => _undoSong.Count;
    
    public bool hasUndoHistory => _undoSong.Count > 0;
}