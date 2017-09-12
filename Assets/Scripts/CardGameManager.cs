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
    public const string GameSelectionTag = "GameSelection";
    public const string BackgroundImageTag = "Background";
    public const string MainCanvasTag = "Canvas";
    public const string PlayerPrefGameName = "DefaultGame";
    public const string FirstGameName = "Standard";
    public const string InvalidGameSelectionMessage = "Could not select the card game because the name is not recognized in the list of card games! Try selecting a different card game.";
    public const string QuitPrompt = "Quit?";

    public static string GamesFilePathBase {
        get { return Application.persistentDataPath + "/games"; }
    }

    public string CurrentGameName { get; set; }

    public GameObject PopupPrefab;

    private static CardGameManager _instance;

    private Dictionary<string, CardGame> _allCardGames;
    private Dropdown _gameSelection;
    private List<Dropdown.OptionData> _gameSelectionOptions;
    private List<CardGameSelectedDelegate> _onSelectActions;
    private SpriteRenderer _backgroundImage;
    private Popup _popup;

    void Awake()
    {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        if (!Directory.Exists(GamesFilePathBase))
            UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, GamesFilePathBase);
        
        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames [gameName] = new CardGame(gameName);
        }
        // TODO: ADDING IN CUSTOM GAMES, LIKE DB AND HS
        //CardGame defaultGame;
        //defaultGame = new CardGame("DB", "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1bTdtQTZzTWZHZ0E");
        //AllCardGames [defaultGame.Name] = defaultGame;
        //defaultGame = new CardGame("HS", "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1b0d5WGFJb195UTg");
        //AllCardGames [defaultGame.Name] = defaultGame;

        UpdateGameSelection();
    }

    public void UpdateGameSelection()
    {
        GameSelectionOptions.Clear();
        foreach (string gameName in AllCardGames.Keys)
            GameSelectionOptions.Add(new Dropdown.OptionData() { text = gameName });
        GameSelection.options = GameSelectionOptions;
        GameSelection.onValueChanged.AddListener(SelectCardGame);
        SelectCardGame(PlayerPrefs.GetString(PlayerPrefGameName, FirstGameName));
    }

    public void SelectCardGame(int index)
    {
        SelectCardGame(GameSelectionOptions [index].text);
    }

    public void SelectCardGame(string name)
    {
        CardGame selectedCardGame;
        if (!AllCardGames.TryGetValue(name, out selectedCardGame)) {
            Debug.LogError(InvalidGameSelectionMessage);
            Popup.Show(InvalidGameSelectionMessage);
            return;
        }

        CurrentGameName = name;
        PlayerPrefs.SetString(PlayerPrefGameName, CurrentGameName);

        if (!Current.IsLoaded)
            StartCoroutine(Current.Load());
        StartCoroutine(DoGameSelectionActions());
    }

    public IEnumerator DoGameSelectionActions()
    {
        while (!Current.IsLoaded)
            yield return null;

        BackgroundImage.sprite = Current.BackgroundImageSprite;
        CardInfoViewer.Instance.UpdatePropertyOptions();

        for (int i = OnSelectActions.Count - 1; i >= 0; i--)
            if (OnSelectActions [i] == null)
                OnSelectActions.RemoveAt(i);
        foreach (CardGameSelectedDelegate action in OnSelectActions)
            action();
    }

    public void PromptForQuit()
    {
        Popup.Prompt(QuitPrompt, Quit);
    }

    public void Quit()
    {
        Application.Quit();
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
                return new CardGame(string.Empty);
            return currentGame;
        }
    }

    public Dictionary<string, CardGame> AllCardGames {
        get {
            if (_allCardGames == null)
                _allCardGames = new Dictionary<string, CardGame>();
            return _allCardGames;
        }
    }

    public Dropdown GameSelection {
        get {
            if (_gameSelection == null)
                _gameSelection = GameObject.FindGameObjectWithTag(GameSelectionTag).GetComponent<Dropdown>();
            return _gameSelection;
        }
    }

    public List<Dropdown.OptionData> GameSelectionOptions {
        get {
            if (_gameSelectionOptions == null)
                _gameSelectionOptions = new List<Dropdown.OptionData>();
            return _gameSelectionOptions;
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
                _backgroundImage = GameObject.FindGameObjectWithTag(BackgroundImageTag).GetOrAddComponent<SpriteRenderer>();
            return _backgroundImage;
        }
    }

    public Popup Popup {
        get {
            if (_popup == null)
                _popup = Instantiate(PopupPrefab, GameObject.FindGameObjectWithTag(MainCanvasTag).transform).GetOrAddComponent<Popup>();
            return _popup;
        }
    }
}
