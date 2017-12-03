using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public delegate void CardGameSelectedDelegate();

public class CardGameManager : MonoBehaviour
{
    public const string CardGameManagerTag = "CardGameManager";
    public const string BackgroundImageTag = "Background";
    public const string CanvasTag = "Canvas";
    public const string SelectorPrefabName = "Game Selection Menu";
    public const string PlayerPrefGameName = "DefaultGame";
    public const string FirstGameName = "Standard Playing Cards";
    public const string MessengerPrefabName = "Popup";
    public const string InvalidGameSelectionMessage = "Could not select the card game because the name is not recognized in the list of card games! Try selecting a different card game.";

    // Pixels per inch
    public static int PPI {
        get { return 100; }
    }

    public static string GamesFilePathBase {
        get { return Application.persistentDataPath + "/games"; }
    }

    public static string CurrentGameName { get; set; }

    public static bool IsMultiplayer { get; set; }

    public static bool IsQuitting { get; private set; }

    private static CardGameManager _instance;

    private Dictionary<string, CardGame> _allCardGames;
    private GameSelectionMenu _selector;
    private List<CardGameSelectedDelegate> _onSelectActions;
    private Popup _messenger;
    private Image _backgroundImage;
    private Canvas _topCanvas;

    void Awake()
    {
        if (CardGameManager._instance != null && CardGameManager._instance != this) {
            Destroy(this.gameObject);
            return;
        }
        CardGameManager.Instance = this;
        DontDestroyOnLoad(this.gameObject);

        if (!Directory.Exists(GamesFilePathBase)) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            UnityExtensionMethods.ExtractAndroidStreamingAssets(GamesFilePathBase);
            #else
            UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, GamesFilePathBase);
            #endif
        }
        
        foreach (string gameDirectory in Directory.GetDirectories(GamesFilePathBase)) {
            string gameName = gameDirectory.Substring(GamesFilePathBase.Length + 1);
            AllCardGames [gameName] = new CardGame(gameName, string.Empty);
        }
        CurrentGameName = PlayerPrefs.GetString(PlayerPrefGameName, FirstGameName);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SelectCardGame(CurrentGameName);
    }

    void OnSceneUnloaded(Scene scene)
    {
        OnSelectActions.Clear();
    }

    public void SelectCardGame(string name)
    {
        if (!AllCardGames.ContainsKey(name)) {
            Debug.LogError(InvalidGameSelectionMessage);
            Messenger.Show(InvalidGameSelectionMessage);
            return;
        }

        CurrentGameName = name;
        StartCoroutine(DoGameSelectionActions());
    }

    public IEnumerator DoGameSelectionActions()
    {
        string selectedGame = CurrentGameName;
        if (!Current.IsLoaded) {
            if (Current.IsLoading)
                yield break;
            StartCoroutine(Current.Load());
        }
        while (!Current.IsLoaded) {
            if (!string.IsNullOrEmpty(Current.Error)) {
                Debug.LogError(Current.Error);
                Messenger.Show(Current.Error);
                yield break;
            }
            yield return null;
        }
        if (selectedGame != CurrentGameName)
            yield break;

        PlayerPrefs.SetString(PlayerPrefGameName, CurrentGameName);
        if (BackgroundImage != null)
            BackgroundImage.sprite = Current.BackgroundImageSprite;
        if (CardInfoViewer.Instance != null)
            CardInfoViewer.Instance.Reset();

        for (int i = OnSelectActions.Count - 1; i >= 0; i--)
            if (OnSelectActions [i] == null)
                OnSelectActions.RemoveAt(i);
        foreach (CardGameSelectedDelegate action in OnSelectActions)
            action();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        CardGameManager.IsQuitting = true;
    }

    public static CardGameManager Instance {
        get {
            if (IsQuitting)
                return null;
            
            if (_instance == null) {
                GameObject cardGameManager = GameObject.FindGameObjectWithTag(CardGameManagerTag);
                if (cardGameManager == null) {
                    cardGameManager = new GameObject(CardGameManagerTag);
                    cardGameManager.tag = CardGameManagerTag;
                    cardGameManager.transform.position = Vector3.zero;
                }
                _instance = cardGameManager.GetOrAddComponent<CardGameManager>();
            }
            return _instance;
        }
        private set {
            _instance = value;
        }
    }

    public static CardGame Current {
        get {
            CardGame currentGame;
            if (!Instance.AllCardGames.TryGetValue(CurrentGameName, out currentGame))
                return new CardGame();
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

    public GameSelectionMenu Selector {
        get {
            if (_selector == null) {
                _selector = Instantiate(Resources.Load<GameObject>(SelectorPrefabName)).GetOrAddComponent<GameSelectionMenu>();
                _selector.transform.SetParent(this.transform);
            }
            return _selector;
        }
    }

    public List<CardGameSelectedDelegate> OnSelectActions {
        get {
            if (_onSelectActions == null)
                _onSelectActions = new List<CardGameSelectedDelegate>();
            return _onSelectActions;
        }
    }

    public Popup Messenger {
        get {
            if (_messenger == null)
                _messenger = Instantiate(Resources.Load<GameObject>(MessengerPrefabName)).GetOrAddComponent<Popup>();
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

    public Canvas TopCanvas {
        get {
            if (_topCanvas == null)
                foreach (GameObject canvasGO in GameObject.FindGameObjectsWithTag(CanvasTag))
                    if (canvasGO.activeSelf && (_topCanvas == null || canvasGO.GetComponent<Canvas>().sortingOrder > _topCanvas.sortingOrder))
                        _topCanvas = canvasGO.GetComponent<Canvas>();
            return _topCanvas;
        }
        set {
            _topCanvas = value;
        }
    }
}
