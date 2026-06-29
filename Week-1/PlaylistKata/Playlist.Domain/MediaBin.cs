namespace Playlist.Domain;

// Clase genérica (puede ser Song, Podcast, o Audiobook)
public class MediaBin<T> where T : PlaylistItem
{
    private readonly T[] _items;
    private int _used;
    public MediaBin(int capacity)
    {
        _items = new T[capacity];
    }

    public int Capacity => _items.Length;
    public int Count => _used;

    public bool TryAdd(T item)
    {
        if (_used == _items.Length)
        {
            return false;
        }

        _items[_used++] = item;
        return true;
    }

    public T Get(int index)
    {
        return _items[index];
    }
}