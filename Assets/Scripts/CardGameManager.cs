using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CardGameManager : MonoBehaviour
{
    public const string DefaultConfigFileName = "CGS.json";
    public const string DefaultGameName = "Crucible";
    public const string DefaultGameURL = "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1bTdtQTZzTWZHZ0E";

    private static CardGameManager instance;

    private Dictionary<string, CardGame> allCardGames;
    private string configFilePath;
    private string currentGameName;
    private SpriteRenderer backgroundImage;

    void Awake()
    {
        if (instance != null) {
            // TODO: THINK ABOUT HOW THIS OBJECT ACTS BETWEEN MULTIPLE SCENES
            // Perhaps load the info from the previous manager, delete it, and set this as the new game manager?
            // remember that we have the Instance property dynamically searching for and generating this as needed too
        }
        instance = this;

        allCardGames = new Dictionary<string, CardGame>();
        configFilePath = PlayerPrefs.GetString("CGSFilepath", Application.persistentDataPath + "/" + DefaultConfigFileName);
        Debug.Log("configFilePath = " + configFilePath);
        if (!File.Exists(configFilePath)) {
            Debug.Log("Config file missing. Generating.");
            GenerateDefaultConfigFile();
        }
        LoadConfigFile();
        Debug.Log("After loading the config file, selecting the default card game from it"); 
        IEnumerator enumerator = allCardGames.Keys.GetEnumerator();
        if (enumerator.MoveNext()) {
            currentGameName = (string)enumerator.Current;
            SelectCardGame(currentGameName);
        } else
            Debug.LogError("Could not select a default card game because there are no card games loaded!");

    }

    private void GenerateDefaultConfigFile()
    {
        CardGame defaultCardGame = new CardGame(DefaultGameName, DefaultGameURL, true);
        allCardGames [defaultCardGame.Name] = defaultCardGame;
        Debug.Log("Generating Default Config File with: " + defaultCardGame.CGSConfigLine);
        SaveConfigFile();
    }

    private void SaveConfigFile()
    {
        Debug.Log("Saving config file to: " + configFilePath);
        string configJson = "[" + System.Environment.NewLine;
        foreach (CardGame cardGame in allCardGames.Values)
            configJson += cardGame.CGSConfigLine + "," + System.Environment.NewLine;
        int lastEntryEnd = configJson.LastIndexOf("}");
        if (lastEntryEnd <= 0) {
            Debug.LogWarning("Attempted to save a config file when there are no game types in it! Aborting");
            return;
        }
        configJson = configJson.Substring(0, lastEntryEnd + 1) + System.Environment.NewLine;
        configJson += "]";
        File.WriteAllText(configFilePath, configJson);
        Debug.Log("Config file saved");
    }

    private void LoadConfigFile()
    {
        Debug.Log("Loading config file from: " + configFilePath);
        string configJson = File.ReadAllText(configFilePath);
        JArray cardGames = JsonConvert.DeserializeObject<JArray>(configJson);
        foreach (JToken cardGame in cardGames) {
            string gameName = (string)cardGame.SelectToken("name");
            string gameURL = (string)cardGame.SelectToken("url");
            if (gameName == null || gameURL == null) {
                Debug.LogWarning("Incorrect entry in the config file! Ignoring it");
            } else {
                Debug.Log("Defining the card game: " + gameName + " to be at " + gameURL);
                CardGame newGame = new CardGame(gameName, gameURL, true);
                allCardGames [newGame.Name] = newGame;
            }
        }
        Debug.Log("Config file loaded");
    }

    public void SelectCardGame(string name)
    {
        if (!allCardGames.ContainsKey(name)) {
            Debug.LogError("Could not select " + name + " because the name is not recognized in the list of card games!");
            return;
        }

        Debug.Log("Selecting the card game: " + name);
        currentGameName = name;
        CardGame currentGame = allCardGames [currentGameName];
        if (!currentGame.IsLoaded)
            StartCoroutine(currentGame.LoadFromURL());

    }

    public static CardGameManager Instance {
        get {
            if (instance == null)
                instance = GameObject.FindWithTag("CardGameManager").transform.GetOrAddComponent<CardGameManager>();
            return instance;
        }
    }

    public static string CurrentGameName {
        get {
            return instance.currentGameName;
        }
    }

    public static CardGame CurrentCardGame {
        get { 
            CardGame currentGame;
            if (!Instance.allCardGames.TryGetValue(CurrentGameName, out currentGame))
                currentGame = null;
            return currentGame;
        }
    }

    public static List<Set> Sets {
        get {
            if (CurrentCardGame == null)
                return new List<Set>();
            return CurrentCardGame.Sets;
        }
    }

    public static List<Card> Cards {
        get {
            if (CurrentCardGame == null)
                return new List<Card>();
            return CurrentCardGame.Cards;
        }
    }

    public static bool IsLoaded {
        get {
            if (CurrentCardGame == null)
                return false;
            return CurrentCardGame.IsLoaded;
        }
    }

    public SpriteRenderer BackgroundImage {
        get {
            if (backgroundImage == null)
                backgroundImage = GameObject.FindGameObjectWithTag("Background").transform.GetOrAddComponent<SpriteRenderer>();
            return backgroundImage;
        }
    }
}
