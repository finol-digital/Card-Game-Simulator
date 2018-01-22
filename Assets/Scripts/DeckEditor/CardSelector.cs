using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelector : MonoBehaviour
{
    public const string DownInput = "Down";
    public const string UpInput = "Up";
    public const string LeftInput = "Left";
    public const string RightInput = "Right";
    
    public DeckEditor deckEditor;
    public SearchResults searchResults;
    
    public void MoveDown()
    {
        if (EventSystem.current.alreadySelecting)
            return;
    }
    
    public void MoveUp()
    {
        if (EventSystem.current.alreadySelecting)
            return;
    }
    
    public void MoveLeft()
    {
        if (EventSystem.current.alreadySelecting)
            return;
    }
    
    public void MoveRight()
    {
        if (EventSystem.current.alreadySelecting)
            return;
    }
    
    void Update()
    {
        if (Input.GetButtonDown(DownInput)
            MoveDown();
        else if (Input.GetButtonDown(UpInput)
            MoveUp();
        else if (Input.GetButtonDown(LeftInput)
            MoveLeft();
        else if (Input.GetButtonDown(RightInput)
            MoveRight();
    }
}
