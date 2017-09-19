using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayModeManager : MonoBehaviour
{
    public GameObject deckLoadMenuPrefab;

    private Deck _deck;
    private DeckLoadMenu _deckLoader;

    void Start()
    {
        DeckLoader.Show(LoadDeck, UnityExtensionMethods.GetSafeFileName);
    }

    public void LoadDeck(Deck newDeck)
    {
        _deck = newDeck;
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
