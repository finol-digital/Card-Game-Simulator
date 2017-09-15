using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

public delegate void CardGameSelectedDelegate();

public class CardGameManager : MonoBehaviour
{
    public const string GameSelectionTag = "GameSelection";
    public const string BackgroundImageTag = "Background";
    public const string MainCanvasTag = "Canvas";
    public const string PlayerPrefGameName = "DefaultGame";
    public const string FirstGameName = "Standard";
    public const string InvalidGameSelectionMessage = "Could not select the card game because the name is not recognized in the list of card games! Try selecting a different card game.";

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
    private Image _backgroundImage;
    private Popup _popup;

    void Awake()
    {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        if (!Directory.Exists(GamesFilePathBase)) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidStreamingAssets.Extract(GamesFilePathBase);
            #else
            UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, GamesFilePathBase);
            #endif
        }
        
        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames [gameName] = new CardGame(gameName, string.Empty);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameSelection();
    }

    public void ResetGameSelection()
    {
        int defaultGameIndex = 0;
        GameSelectionOptions.Clear();
        foreach (string gameName in AllCardGames.Keys) {
            GameSelectionOptions.Add(new Dropdown.OptionData() { text = gameName });
            if (gameName.Equals(PlayerPrefs.GetString(PlayerPrefGameName, FirstGameName)))
                defaultGameIndex = GameSelectionOptions.Count - 1;
        }

        if (GameSelection == null)
            return;

        GameSelection.options = GameSelectionOptions;
        GameSelection.onValueChanged.RemoveAllListeners();
        GameSelection.onValueChanged.AddListener(SelectCardGame);
        GameSelection.value = defaultGameIndex;
    }

    public void SelectCardGame(int index)
    {
        if (index < 0 || index >= GameSelectionOptions.Count) {
            Debug.LogError(InvalidGameSelectionMessage);
            Popup.Show(InvalidGameSelectionMessage);
            return;
        }

        SelectCardGame(GameSelectionOptions [index].text);
    }

    public void SelectCardGame(string name)
    {
        if (!AllCardGames.ContainsKey(name)) {
            Debug.LogError(InvalidGameSelectionMessage);
            Popup.Show(InvalidGameSelectionMessage);
            return;
        }

        CurrentGameName = name;
        StartCoroutine(DoGameSelectionActions());
    }

    public IEnumerator DoGameSelectionActions()
    {
        if (!Current.IsLoaded)
            StartCoroutine(Current.Load());
        
        while (!Current.IsLoaded) {
            if (!string.IsNullOrEmpty(Current.Error)) {
                // TODO: BETTER ERROR HANDLING
                Popup.Show(Current.Error);
                yield break;
            }
            yield return null;
        }

        PlayerPrefs.SetString(PlayerPrefGameName, CurrentGameName);
        BackgroundImage.sprite = Current.BackgroundImageSprite;
        CardInfoViewer.Instance.ResetPropertyOptions();

        foreach (CardGameSelectedDelegate action in OnSelectActions)
            action();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
                return new CardGame(string.Empty, string.Empty);
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
            if (_gameSelection == null) {
                GameObject obj = GameObject.FindGameObjectWithTag(GameSelectionTag);
                if (obj != null)
                    _gameSelection = obj.GetComponent<Dropdown>();
            }
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

    private Image BackgroundImage {
        get {
            if (_backgroundImage == null)
                _backgroundImage = GameObject.FindGameObjectWithTag(BackgroundImageTag).GetOrAddComponent<Image>();
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
