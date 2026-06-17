namespace Playlist.Domain;

public class AudioBook : PlaylistItem
{
    public string  VoiceReader{get;}

    public AudioBook(string title, string author, float duration , int timesplayed, string voicereader) : base (title, author, duration, timesplayed)
    {
         VoiceReader = voicereader;
    }

    public override string Describe()
    {
        return $"Track: {Id},Title: {Title}, duration {Duration} by {Author}, The Reader is {VoiceReader}. (Times played {TimesPlayed})";
    }

}