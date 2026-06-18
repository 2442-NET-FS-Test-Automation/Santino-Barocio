namespace Methods;

//This is the interface for the Menu
//It will be used to declare the methods for
//the Menu and nothing else
public interface IMenu
{
	//These two method purpose is self explanatory
    void AddToPlaylist();

	//These methods will work direchtly on the library
	//Hope the names are clear enough to know what they do
	void AddToLibrary();
	void SearchMedia();
	void DeleteMedia();
	void ShowMedia();
}