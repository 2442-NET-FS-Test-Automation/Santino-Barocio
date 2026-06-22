namespace Playlist.Domain;

public readonly struct GridLocation
{
    public int Row {get;}
    public int Column{get;}

    public GridLocation(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public override string ToString()
    {
        return $"Row {Row}, Col {Column}";
    }

}