using System.IO;
using System.Collections.Generic;
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
    public const int PixelsPerInch = 100;

    public static string GamesFilePathBase => Application.persistentDataPath + "/games";
    public static string CurrentGameName { get; set; } = "";
    public static bool IsMultiplayer { get; set; }
    public static bool IsQuitting { get; private set; }

    public Dictionary<string, CardGame> AllCardGames { get; } = new Dictionary<string, CardGame>();
    public List<GameSceneDelegate> OnSceneActions { get; } = new List<GameSceneDelegate>();

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

        if (!Directory.Exists(GamesFilePathBase)) {
#if !UNITY_ANDROID || UNITY_EDITOR
            UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, GamesFilePathBase);
#else
            UnityExtensionMethods.ExtractAndroidStreamingAssets(GamesFilePathBase);
#endif
        }

        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames [gameName] = new CardGame(gameName, string.Empty);
        }
        CurrentGameName = PlayerPrefs.GetString(PlayerPrefGameName, FirstGameName);

        Application.logMessageReceived += HandleLog;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
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

    public void SelectCardGame(string gameName)
    {
        if (string.IsNullOrEmpty(gameName) || !AllCardGames.ContainsKey(gameName)) {
            Debug.LogError(InvalidGameSelectionMessage);
            return;
        }

        CurrentGameName = gameName;
        DoGameSceneActions();
    }

    public void DoGameSceneActions()
    {
        if (!Current.IsLoaded)
            Current.Load();

        if (!string.IsNullOrEmpty(Current.Error))
            Debug.LogError(GameLoadErrorMessage + Current.Error);
        else
            PlayerPrefs.SetString(PlayerPrefGameName, CurrentGameName);

        if (BackgroundImage != null)
            BackgroundImage.sprite = Current.BackgroundImageSprite;
        CardInfoViewer.Instance?.ResetInfo();

        for (int i = OnSceneActions.Count - 1; i >= 0; i--)
            if (OnSceneActions [i] == null)
                OnSceneActions.RemoveAt(i);
        foreach (GameSceneDelegate action in OnSceneActions)
            action();
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

    public static CardGame Current {
        get { CardGame currentGame;
            return Instance.AllCardGames.TryGetValue(CurrentGameName, out currentGame) ? currentGame : new CardGame();
        }
    }

    public GameSelectionMenu Selector {
        get {
            if (_selector != null) return _selector;
            _selector = Instantiate(Resources.Load<GameObject>(SelectorPrefabName)).GetOrAddComponent<GameSelectionMenu>();
            _selector.transform.SetParent(transform);
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
