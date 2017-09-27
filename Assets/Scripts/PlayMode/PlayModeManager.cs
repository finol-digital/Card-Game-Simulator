using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayModeManager : MonoBehaviour
{
    public RectTransform playArea;
    public GameObject deckLoadMenuPrefab;
    public DeckZone deckZone;

    private DeckLoadMenu _deckLoader;

    void Start()
    {
        DeckLoader.Show(LoadDeck, UnityExtensionMethods.GetSafeFileName);
        playArea.gameObject.GetOrAddComponent<CardStack>().OnCardDropActions.Add(SetPlayActions);
    }

    public void LoadDeck(Deck newDeck)
    {
        deckZone.Deck = newDeck;
    }

    public void SetPlayActions(CardStack cardStack, CardModel cardModel)
    {
        cardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = cardModel.Rotate;
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }
}
