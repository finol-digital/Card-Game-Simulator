using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelector : MonoBehaviour
{
    public const string VerticalInput = "Vertical";
    public const string HorizontalInput = "Horizontal";

    public DeckEditor editor;
    public SearchResults results;

    public void MoveDown()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        List<CardModel> editorCards = editor.CardModels;
        if (editorCards.Count < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        for (int i = 0; i < editorCards.Count; i++) {
            if (editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            i++;
            if (i == editorCards.Count)
                i = 0;
            EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(editorCards[0].gameObject);
    }

    public void MoveUp()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        List<CardModel> editorCards = editor.CardModels;
        if (editorCards.Count < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        for (int i = editorCards.Count - 1; i >= 0; i--) {
            if (editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            i--;
            if (i < 0)
                i = editorCards.Count - 1;
            EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(editorCards[editorCards.Count - 1].gameObject);
    }

    public void MoveLeft()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        if (results.layoutArea.childCount < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

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

        if (results.layoutArea.childCount < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

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
        if (Input.GetButtonDown(VerticalInput) && CardGameManager.TopMenuCanvas == null) {
            if (Input.GetAxis(VerticalInput) > 0)
                MoveUp();
            else
                MoveDown();
        }

        if (Input.GetButtonDown(HorizontalInput) && CardGameManager.TopMenuCanvas == null) {
            if (Input.GetAxis(HorizontalInput) > 0)
                MoveRight();
            else
                MoveLeft();
        }
    }
}
