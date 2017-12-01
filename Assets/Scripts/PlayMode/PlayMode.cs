using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class PlayMode : MonoBehaviour
{
    public const string MainMenuPrompt = "Go back to the main menu?";

    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public RectTransform playAreaContent;
    public ExtensibleCardZone extraZone;
    public StackedZone deckZone;
    public ExtensibleCardZone handZone;

    private DeckLoadMenu _deckLoader;
    private CardSearchMenu _cardSearcher;

    IEnumerator Start()
    {
        if (CardGameManager.IsMultiplayer) {
            ((LocalNetManager)NetworkManager.singleton).SearchForHost();
            // TODO: BETTER MANAGEMENT OF ONLINE VS OFFLINE
            yield return new WaitForSecondsRealtime(3.0f);
            if (!NetworkManager.singleton.isNetworkActive)
                NetworkManager.singleton.StartHost();
        }

        DeckLoader.loadCancelButton.onClick.RemoveAllListeners();
        DeckLoader.loadCancelButton.onClick.AddListener(BackToMainMenu);
        DeckLoader.Show(LoadDeck);

        playAreaContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(AddCardToPlay);
    }

    void Update()
    {
        if (Input.GetButtonDown("Draw"))
            Deal(1);
    }

    public void LoadDeck(Deck newDeck)
    {
        List<Card> extraCards = newDeck.GetExtraCards();
        Dictionary<string, List<Card>> extraGroups = newDeck.GetExtraGroups();
        foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups) {
            extraZone.labelText.text = cardGroup.Key;
            foreach (Card card in cardGroup.Value)
                extraZone.AddCard(card);
            // TODO: ALLOW MULTIPLE CARD GROUPS
        }

        foreach (Card card in newDeck.Cards)
            if (!extraCards.Contains(card))
                deckZone.AddCard(card);
        deckZone.Shuffle();

        Deal(CardGameManager.Current.GameStartHandCount);
    }

    public void Deal(int cardCount)
    {
        for (int i = 0; deckZone.Count > 0 && i < cardCount; i++)
            handZone.AddCard(deckZone.PopCard());
    }

    public void ShowCardSearcher()
    {
        CardSearcher.Show(null, null, AddCardToHand);
    }

    public void AddCardToHand(List<Card> results)
    {
        if (results == null || results.Count < 1)
            return;
        
        handZone.AddCard(results [0]);
    }

    public void AddCardToPlay(CardStack cardStack, CardModel cardModel)
    {
        if (NetworkManager.singleton.isNetworkActive)
            ((LocalNetManager)NetworkManager.singleton).LocalPlayer.MoveCardToServer(cardModel);
        else
            ((LocalNetManager)NetworkManager.singleton).SetPlayActions(cardStack, cardModel);
    }

    public void PromptBackToMainMenu()
    {
        CardGameManager.Instance.Messenger.Prompt(MainMenuPrompt, BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }

    public CardSearchMenu CardSearcher {
        get {
            if (_cardSearcher == null)
                _cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>();
            return _cardSearcher;
        }
    }
}
