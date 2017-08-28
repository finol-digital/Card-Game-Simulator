using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckEditor : MonoBehaviour
{
    public GameObject CardStackPrefab;
    public RectTransform DeckEditorContent;

    private List<CardStack> cardStacks;

    void Start()
    {
        Debug.Log("Creating card stacks in the deck editor");
        cardStacks = new List<CardStack>();
        int numCardStacks = Mathf.FloorToInt(DeckEditorContent.sizeDelta.x / CardStackPrefab.GetComponent<RectTransform>().sizeDelta.x);
        for (int i = 0; i < numCardStacks; i++) {
            GameObject newCardStack = Instantiate(CardStackPrefab, DeckEditorContent);
            cardStacks.Add(newCardStack.transform.GetOrAddComponent<CardStack>());
        }
    }


}
