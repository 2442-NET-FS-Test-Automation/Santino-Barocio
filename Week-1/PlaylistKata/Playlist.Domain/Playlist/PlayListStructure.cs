using Playlist.Domain;

namespace DefaultNamespace;
//Here we are going to keep the current playlist using a <t> Linked List
//Even a wait function to fake a media item duration
public class PlayListStructure
{
    // Adding a linkedlist for the playlist so it has an pointer
    public readonly LinkedList<PlaylistItem> _playlistList = new();

    // link node to the last item
    public void AddToReadingList(PlaylistItem item) => _playlistList.AddLast(item);

    // link node to the first item
    public void AddNextUp(PlaylistItem item) => _playlistList.AddFirst(item);
    // IEnumerable exposes the list so you can access through it in a for/foreach but not 
    public IEnumerable<PlaylistItem> ReadingList => _playlistList;


}