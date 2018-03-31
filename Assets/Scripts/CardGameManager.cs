using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public delegate void GameSceneDelegate();

public class CardGameManager : MonoBehaviour
{
    public const string CardGameManagerTag = "CardGameManager";
    public const string BackgroundImageTag = "Background";
    public const string CardCanvasTag = "CardCanvas";
    public const string MenuCanvasTag = "MenuCanvas";
    public const string SelectorPrefabName = "Game Selection Menu";
    public const string PlayerPrefGameName = "DefaultGame";
    public const string FirstGameName = "Standard Playing Cards";
    public const string MessengerPrefabName = "Popup";
    public const string InvalidGameSelectionMessage = "Could not select the card game because the name is not recognized in the list of card games! Try selecting a different card game.";
    public const string GameLoadErrorMessage = "Error loading game!: ";
    public const string GameDeleteErrorMessage = "Error deleting game!: ";
    public const int PixelsPerInch = 100;

    public static string GamesFilePathBase => Application.persistentDataPath + "/games";
    public static CardGame Current { get; private set; } = new CardGame();
    public static bool IsQuitting { get; private set; }

    public Dictionary<string, CardGame> AllCardGames { get; } = new Dictionary<string, CardGame>();
    public List<GameSceneDelegate> OnSceneActions { get; } = new List<GameSceneDelegate>();

    private LobbyDiscovery _discovery;
    public LobbyDiscovery Discovery => _discovery ??
                                         (_discovery = gameObject.GetOrAddComponent<LobbyDiscovery>());

    private static CardGameManager _instance;
    private GameSelectionMenu _selector;
    private Popup _messenger;
    private Image _backgroundImage;

    void Awake()
    {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        FindCardGames();
        if (AllCardGames.Count < 1)
            CreateDefaultCardGames();

        CardGame currentGame;
        Current = AllCardGames.TryGetValue(PlayerPrefs.GetString(PlayerPrefGameName, FirstGameName), out currentGame) ? currentGame : new CardGame();

        Application.logMessageReceived += HandleLog;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void CreateDefaultCardGames()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, GamesFilePathBase);
#else
        UnityExtensionMethods.ExtractAndroidStreamingAssets(GamesFilePathBase);
#endif
        FindCardGames();
    }

    private void FindCardGames()
    {
        if (!Directory.Exists(GamesFilePathBase))
            return;

        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames[gameName] = new CardGame(gameName, string.Empty);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        Messenger.Show(logString);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DoGameSceneActions();
    }

    void OnSceneUnloaded(Scene scene)
    {
        OnSceneActions.Clear();
    }

    public IEnumerator DownloadCardGame(string gameUrl)
    {
        //Messenger.Show("Game download has started");
        CardGame newGame = new CardGame(Set.DefaultCode, gameUrl) {AutoUpdate = true};
        Current = newGame;
        yield return newGame.Download();
        if (string.IsNullOrEmpty(newGame.Error))
            AllCardGames[newGame.Name] = newGame;
        else
            Debug.LogError(GameLoadErrorMessage + newGame.Error);
        SelectCardGame(newGame.Name);
        //Messenger.Show("Game download has finished");
    }

    public void SelectCardGame(string gameName, string gameUrl)
    {
        if (string.IsNullOrEmpty(gameName) || !AllCardGames.ContainsKey(gameName)) {
            StartCoroutine(DownloadCardGame(gameUrl));
            return;
        }
        SelectCardGame(gameName);
    }

    public void SelectCardGame(string gameName)
    {
        if (string.IsNullOrEmpty(gameName) || !AllCardGames.ContainsKey(gameName)) {
            Debug.LogError(InvalidGameSelectionMessage);
            Selector.Show();
            return;
        }

        CardGame currentGame;
        Current = AllCardGames.TryGetValue(gameName, out currentGame) ? currentGame : new CardGame();

        DoGameSceneActions();
    }

    public IEnumerator LoadCards()
    {
        Messenger.Show("Cards are loading in the background. Performance may be affected in the meantime.");
        yield return Current.LoadCardPages();
        if (!string.IsNullOrEmpty(Current.Error))
            Debug.LogError(GameLoadErrorMessage + Current.Error);
        Messenger.Show("All cards have finished loading.");
    }

    public void DoGameSceneActions()
    {
        if (!Current.IsLoaded)
            Current.Load();

        if (!string.IsNullOrEmpty(Current.Error))
            Debug.LogError(GameLoadErrorMessage + Current.Error);
        else
            PlayerPrefs.SetString(PlayerPrefGameName, Current.Name);

        if (BackgroundImage != null)
            BackgroundImage.sprite = Current.BackgroundImageSprite;
        CardInfoViewer.Instance?.ResetInfo();

        for (int i = OnSceneActions.Count - 1; i >= 0; i--)
            if (OnSceneActions [i] == null)
                OnSceneActions.RemoveAt(i);
        foreach (GameSceneDelegate action in OnSceneActions)
            action();
    }

    public void DeleteGame()
    {
        try {
            Directory.Delete(Current.FilePathBase, true);
            AllCardGames.Remove(Current.Name);
            SelectCardGame(AllCardGames.Keys.First());
            Selector.Show();
        } catch (Exception ex) {
            Debug.LogError(GameDeleteErrorMessage + ex.Message);
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        IsQuitting = true;
    }

    public static CardGameManager Instance {
        get {
            if (IsQuitting) return null;
            if (_instance != null) return _instance;
            GameObject cardGameManager = GameObject.FindGameObjectWithTag(CardGameManagerTag);
            if (cardGameManager == null) {
                cardGameManager = new GameObject(CardGameManagerTag) {tag = CardGameManagerTag};
                cardGameManager.transform.position = Vector3.zero;
            }
            _instance = cardGameManager.GetOrAddComponent<CardGameManager>();
            return _instance;
        }
    }

    public GameSelectionMenu Selector {
        get {
            if (_selector != null) return _selector;
            _selector = Instantiate(Resources.Load<GameObject>(SelectorPrefabName)).GetOrAddComponent<GameSelectionMenu>();
            _selector.transform.SetParent(null);
            return _selector;
        }
    }

    public Popup Messenger {
        get {
            if (_messenger != null) return _messenger;
            _messenger = Instantiate(Resources.Load<GameObject>(MessengerPrefabName)).GetOrAddComponent<Popup>();
            _messenger.transform.SetParent(transform);
            return _messenger;
        }
    }
    private Image BackgroundImage {
        get {
            if (_backgroundImage == null && GameObject.FindGameObjectWithTag(BackgroundImageTag) != null)
                _backgroundImage = GameObject.FindGameObjectWithTag(BackgroundImageTag).GetOrAddComponent<Image>();
            return _backgroundImage;
        }
    }

    public static Canvas TopCardCanvas {
        get {
            Canvas topCanvas = null;
            foreach (GameObject canvas in GameObject.FindGameObjectsWithTag(CardCanvasTag))
                if (canvas.activeSelf && (topCanvas == null || canvas.GetComponent<Canvas>().sortingOrder > topCanvas.sortingOrder))
                    topCanvas = canvas.GetComponent<Canvas>();
            return topCanvas;
        }
    }

    public static Canvas TopMenuCanvas {
        get {
            Canvas topCanvas = null;
            foreach (GameObject canvas in GameObject.FindGameObjectsWithTag(MenuCanvasTag))
                if (canvas.activeSelf && (topCanvas == null || canvas.GetComponent<Canvas>().sortingOrder > topCanvas.sortingOrder))
                    topCanvas = canvas.GetComponent<Canvas>();
            return topCanvas;
        }
    }
}
