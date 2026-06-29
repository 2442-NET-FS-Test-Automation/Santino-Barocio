using System.Runtime.InteropServices;

namespace Playlist.Domain;
//Here we are faking a database to keep track of our media using a PlayListItem List
public class MediaLibrary
{
    private readonly Dictionary<int, PlaylistItem> _itemLookup = new();
    private readonly HashSet<string> _authors = new();
    public IReadOnlyCollection<string> Authors => _authors;
    // a stack works as a pile of plates (LAST IN FIRST OUT)
    private readonly Stack<(string action, PlaylistItem item)> _undoSong = new(); 
    private List<PlaylistItem> _library = new List<PlaylistItem>();
    
    private readonly PlaylistItem?[,] _displayCase = new PlaylistItem?[3, 3];

    public void Add(PlaylistItem item)
    {
        _library.Add(item);
        _itemLookup[item.Id] = item;
        if (!string.IsNullOrEmpty(item.Author))
        {
            _authors.Add(item.Author); // Adds the author if is not already there
        }
        _undoSong.Push(("Add",item)); // Registers the action into the stack (action = "Add")
    }
    
    public void Remove(PlaylistItem item)
    {
        _library.Remove(item);
        _itemLookup.Remove(item.Id);
        _undoSong.Push(("Remove", item)); // Registers the action into the stack (action = "Remove")
    }

    public void Undo()
    {

        if(_undoSong.Count == 0)
        {
            Console.WriteLine("Nothing to undo!\n");
            return;
        }

        Console.WriteLine("Undo selection!");
        var (action, item) = _undoSong.Pop(); // removes and returns the object located at the top of a <Stack>
        if (action == "Add")
        {
            _library.Remove(item); // undo: if the element was added before and there was an undo, remove it from the list
            _itemLookup.Remove(item.Id);  
        } 
        else if(action == "Remove") 
        {
            _library.Add(item); // undo: if the element was removed and you needed to undo, then it gets added again
            _itemLookup[item.Id] = item;
        }
    }
    
    public List<PlaylistItem> GetItems() => _library;
    
    // public bool hasUndoHistory => _undoSong.Count > 0;

    public PlaylistItem GetItemById(int id)
    {
        if (_itemLookup.TryGetValue(id, out PlaylistItem? item))
        {
            return item;
        }
        throw new PlaylistItemNotFoundExc(id);
    }
    
    public List<PlaylistItem> Find(Predicate<PlaylistItem> condition)
    {
        List<PlaylistItem> results = new();
        foreach (var item in _library)
        {
            // Si el elemento cumple la condición, se agrega a los resultados
            if (condition(item))
            {
                results.Add(item);
            }
        }
        return results;
    }

    public void PlaceInDisplayCase(PlaylistItem item, int row, int col)
    {
        if (row >= 0 && row < _displayCase.GetLength(0) && col >= 0 && col < _displayCase.GetLength(1))
        {
            _displayCase[row, col] = item;
        }
    }

    // Método para imprimir la cuadrícula
    public void ShowDisplayCase()
    {
        Console.WriteLine($"\n--- Physical Display Case ({_displayCase.GetLength(0)}x{_displayCase.GetLength(1)}) ---");
        for (int r = 0; r < _displayCase.GetLength(0); r++)
        {
            for (int c = 0; c < _displayCase.GetLength(1); c++)
            {
                var item = _displayCase[r, c];
                string name = item != null ? item.Title ?? "Unknown" : "[Empty]";
                Console.Write($"{name,-15}"); 
            }
            Console.WriteLine();
        }
    }

    public IEnumerable<PlaylistItem> GetItemsLazy()
    {
        foreach (var item in _library)
        {
            yield return item;
        }
    }

}