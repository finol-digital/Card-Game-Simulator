using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckEditor : MonoBehaviour
{
    public GameObject cardStackPrefab;
    public RectTransform deckEditorContent;
    public int cardsPerStack = 4;

    private List<CardStack> cardStacks;

    void Start()
    {
        Debug.Log("Creating card stacks in the deck editor");
        cardStacks = new List<CardStack>();
        int numCardStacks = Mathf.FloorToInt(deckEditorContent.sizeDelta.x / cardStackPrefab.GetComponent<RectTransform>().sizeDelta.x);
        for (int i = 0; i < numCardStacks; i++) {
            GameObject newCardStack = Instantiate(cardStackPrefab, deckEditorContent);
            cardStacks.Add(newCardStack.transform.GetOrAddComponent<CardStack>());
        }
    }

    public void AddCardModel(CardModel cardToAdd)
    {
        Debug.Log("Adding to the deck: " + cardToAdd.gameObject.name);
        CardInfoViewer.Instance.DeselectCard();
        foreach (CardStack stack in cardStacks) {
            if (stack.transform.childCount < cardsPerStack) {
                GameObject cardCopy = Instantiate(cardToAdd.gameObject, stack.transform);
                CardModel copyModel = cardCopy.transform.GetOrAddComponent<CardModel>();
                copyModel.SetAsCard(cardToAdd.RepresentedCard, false);
                return;
            }
        }
        Debug.Log("Failed to find an open stack to which we could add a card! Card not added.");
    }

}
