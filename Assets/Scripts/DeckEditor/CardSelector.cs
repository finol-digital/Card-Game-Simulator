using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelector : MonoBehaviour
{
    public const string DownInput = "Down";
    public const string UpInput = "Up";
    public const string LeftInput = "Left";
    public const string RightInput = "Right";
    
    public DeckEditor editor;
    public SearchResults results;
    
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
        if (EventSystem.current.alreadySelecting || results.layoutArea.childCount < 1)
            return;

        for (int i = results.layoutArea.childCount - 1; i >= 0; i--) {
            if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            i--;
            if (i < 0) {
                results.MoveLeft();
                i = results.layoutArea.childCount - 1;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(results.layoutArea.childCount - 1).gameObject);
    }
    
    public void MoveRight()
    {
        if (EventSystem.current.alreadySelecting || results.layoutArea.childCount < 1)
            return;

        for (int i = 0; i < results.layoutArea.childCount; i++) {
            if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            i++;
            if (i == results.layoutArea.childCount) {
                results.MoveRight();
                i = 0;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
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
