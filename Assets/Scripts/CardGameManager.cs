using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public delegate void CardGameSelectedDelegate();

public class CardGameManager : MonoBehaviour
{
    public const string QuitPrompt = "Quit?";

    public string CurrentGameName { get; set; }

    public GameObject PopupPrefab;

    private static CardGameManager _instance;

    private Dictionary<string, CardGame> _allCardGames;
    private List<Dropdown.OptionData> _gameOptions;
    private List<CardGameSelectedDelegate> _onSelectActions;
    private SpriteRenderer _backgroundImage;
    private Popup _popup;

    void Awake()
    {
        if (_instance != null) {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        CurrentGameName = "";

        Debug.Log("Card Game Manager is initializing");
        if (!Directory.Exists(GamesFilePathBase))
            Directory.CreateDirectory(GamesFilePathBase);
        CardGame defaultGame;
        defaultGame = new CardGame("DB", "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1bTdtQTZzTWZHZ0E");
        AllCardGames [defaultGame.Name] = defaultGame;
        defaultGame = new CardGame("HS", "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1b0d5WGFJb195UTg");
        AllCardGames [defaultGame.Name] = defaultGame;

        Debug.Log("Card Game Manager is reading the card games directory");
        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames [gameName] = new CardGame(gameName);
        }

        Debug.Log("Card Game Manager is loading all the card games");
        foreach (CardGame cardGame in AllCardGames.Values)
            StartCoroutine(cardGame.Load());
    }

    IEnumerator Start()
    {
        Debug.Log("Card game manager is monitoring the loads for errors");
        while (!IsLoaded) {
            // TODO: CHECK FOR ERRORS (cardGame.Error)
            yield return null;
        }

        UpdateGameSelection();
    }

    // TODO: CLEAN THIS UP
    public void UpdateGameSelection()
    {
        Dropdown gameSelection = GameObject.FindGameObjectWithTag("GameSelection").GetComponent<Dropdown>();
        _gameOptions = new List<Dropdown.OptionData>();
        foreach (string gameName in AllCardGames.Keys) {
            _gameOptions.Add(new Dropdown.OptionData() { text = gameName });
        }
        gameSelection.options = _gameOptions;
        gameSelection.value = 0;
        SelectCardGame(0);
    }

    public void SelectCardGame(int index)
    {
        SelectCardGame(_gameOptions [index].text);
    }

    public void SelectCardGame(string name)
    {
        if (!IsLoaded) {
            Debug.LogWarning("Attempted to select a new card game before the current game finished loading! Ignoring the request");
            return;
        }

        if (!AllCardGames.ContainsKey(name)) {
            Debug.LogError("Could not select " + name + " because the name is not recognized in the list of card games!");
            ShowMessage("Error selecting " + name + "!");
            return;
        }

        CurrentGameName = name;
        BackgroundImage.sprite = Current.BackgroundImageSprite;
        CardInfoViewer.Instance.UpdatePropertyOptions();
        foreach (CardGameSelectedDelegate action in OnSelectActions)
            action();
    }

    public void AddOnSelectAction(CardGameSelectedDelegate onSelectAction)
    {
        OnSelectActions.Add(onSelectAction);
    }

    public void ShowMessage(string message)
    {
        Popup.Show(message);
    }

    public void PromptAction(string message, UnityAction action)
    {
        Popup.Prompt(message, action);
    }

    public void PromptForQuit()
    {
        Popup.Prompt(QuitPrompt, Quit);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public static string GamesFilePathBase {
        get {
            return Application.persistentDataPath + "/games";
        }
    }

    public static CardGameManager Instance {
        get {
            return _instance;
        }
    }

    public static CardGame Current {
        get {
            CardGame currentGame;
            if (!Instance.AllCardGames.TryGetValue(Instance.CurrentGameName, out currentGame))
                return new CardGame("");
            return currentGame;
        }
    }

    public static bool IsLoaded {
        get {
            foreach (CardGame game in Instance.AllCardGames.Values)
                if (!game.IsLoaded)
                    return false;
            return true;
        }
    }

    public Dictionary<string, CardGame> AllCardGames {
        get {
            if (_allCardGames == null)
                _allCardGames = new Dictionary<string, CardGame>();
            return _allCardGames;
        }
    }

    public List<CardGameSelectedDelegate> OnSelectActions {
        get {
            if (_onSelectActions == null)
                _onSelectActions = new List<CardGameSelectedDelegate>();
            return _onSelectActions;
        }
    }

    private SpriteRenderer BackgroundImage {
        get {
            if (_backgroundImage == null)
                _backgroundImage = GameObject.FindGameObjectWithTag("Background").GetOrAddComponent<SpriteRenderer>();
            return _backgroundImage;
        }
    }

    public Popup Popup {
        get {
            if (_popup == null)
                _popup = Instantiate(PopupPrefab, GameObject.FindGameObjectWithTag("Canvas").transform).GetOrAddComponent<Popup>();
            return _popup;
        }
    }
}
