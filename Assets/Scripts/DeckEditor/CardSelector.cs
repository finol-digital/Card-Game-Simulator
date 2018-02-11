using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelector : MonoBehaviour
{
    public DeckEditor editor;
    public SearchResults results;

    void Update()
    {
        if (!Input.anyKeyDown || CardGameManager.TopMenuCanvas != null)
            return;

        if (Input.GetButtonDown(CardIn.VerticalInput)) {
            if (Input.GetAxis(CardIn.VerticalInput) > 0)
                SelectUp();
            else
                SelectDown();
        }
        else if (Input.GetButtonDown(CardIn.HorizontalInput)) {
            if (Input.GetAxis(CardIn.HorizontalInput) > 0)
                SelectRight();
            else
                SelectLeft();
        } else if (Input.GetButtonDown(CardIn.ColumnInput)) {
            if (Input.GetAxis(CardIn.ColumnInput) > 0)
                ShiftRight();
            else
                ShiftLeft();
        } else if (Input.GetButtonDown(CardIn.PageInput)) {
            if (Input.GetAxis(CardIn.PageInput) > 0)
                PageRight();
            else
                PageLeft();
        }
    }

    public void SelectDown()
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

    public void SelectUp()
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

    public void SelectLeft()
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
                results.PageLeft();
                i = results.layoutArea.childCount - 1;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
    }

    public void SelectRight()
    {
        if (EventSystem.current.alreadySelecting)
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
                results.PageRight();
                i = 0;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
    }

    public void ShiftLeft()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        List<CardModel> editorCards = editor.CardModels;
        if (editorCards.Count < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        Transform startParent = null;
        for (int i = editorCards.Count - 1; i >= 0; i--) {
            if (startParent == null && editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            if (editorCards[i] == CardInfoViewer.Instance.SelectedCardModel)
                startParent = editorCards[i].transform.parent;
            if (startParent != editorCards[i].transform.parent) {
                EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                return;
            }
        }
        EventSystem.current.SetSelectedGameObject(editorCards[editorCards.Count - 1].gameObject);
    }

    public void ShiftRight()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        List<CardModel> editorCards = editor.CardModels;
        if (editorCards.Count < 1) {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        Transform startParent = null;
        for (int i = 0; i < editorCards.Count; i++) {
            if (startParent == null && editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                continue;
            if (editorCards[i] == CardInfoViewer.Instance.SelectedCardModel)
                startParent = editorCards[i].transform.parent;
            if (startParent != editorCards[i].transform.parent) {
                EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                return;
            }
        }
        EventSystem.current.SetSelectedGameObject(editorCards[0].gameObject);
    }

    public void PageLeft()
    {
        results.PageLeft();
    }

    public void PageRight()
    {
        results.PageRight();
    }

    public void MoveLeft()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float aspectRatio = rt.rect.width / rt.rect.height;
        if (aspectRatio > 1 && aspectRatio < 1.5f)
            PageLeft();
        else
            SelectLeft();
    }

    public void MoveRight()
    {
        RectTransform rt = GetComponent<RectTransform>();
        float aspectRatio = rt.rect.width / rt.rect.height;
        if (aspectRatio > 1 && aspectRatio < 1.5f)
            PageRight();
        else
            SelectRight();
    }
}
