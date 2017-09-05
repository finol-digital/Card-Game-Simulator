using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CardGameManager : MonoBehaviour
{
    private static CardGameManager instance;

    private Dictionary<string, CardGame> allCardGames;
    private CardGame current;
    private string gamesFilePathBase;
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
        current = new CardGame("CARD GAME SIMULATOR");
        gamesFilePathBase = Application.persistentDataPath + "/games";
        backgroundImage = null;

        Debug.Log("Card Game Manager is initializing");
        CreateDefaultGames();

        Debug.Log("Card Game Manager is reading the card games directory");
        foreach (string gameDirectory in Directory.GetDirectories(gamesFilePathBase)) {
            string gameName = gameDirectory.Substring(gamesFilePathBase.Length + 1);
            allCardGames [gameName] = new CardGame(gameName);
        }

        Debug.Log("Card Game Manager is loading all the card games");
        foreach (CardGame cardGame in allCardGames.Values)
            StartCoroutine(cardGame.Load());
    }

    public void CreateDefaultGames()
    {
        if (!Directory.Exists(gamesFilePathBase))
            Directory.CreateDirectory(gamesFilePathBase);
        CardGame defaultGame = new CardGame("DEFAULT", "https://drive.google.com/uc?export=download&id=0B8G-U4tnM7g1bTdtQTZzTWZHZ0E");
        allCardGames [defaultGame.Name] = defaultGame;
    }

    IEnumerator Start()
    {
        Debug.Log("Card game manager is waiting for the card games to load");
        while (!IsLoaded)
            yield return null;

        Debug.Log("Card Game Manager is selecting the default card game");
        IEnumerator enumerator = allCardGames.Keys.GetEnumerator();
        if (enumerator.MoveNext())
            SelectCardGame((string)enumerator.Current);
        else
            Debug.LogError("Could not select a default card game because there are no card games loaded!");
    }

    public void SelectCardGame(string name)
    {
        if (!IsLoaded) {
            Debug.LogWarning("Attempted to select a new card game before the current game finished loading! Ignoring the request");
            return;
        }

        if (!allCardGames.ContainsKey(name)) {
            Debug.LogError("Could not select " + name + " because the name is not recognized in the list of card games!");
            return;
        }

        Debug.Log("Selecting the card game: " + name);
        current = allCardGames [name];
        BackgroundImage.sprite = Current.BackgroundImage;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public static CardGameManager Instance {
        get {
            if (instance == null)
                instance = GameObject.FindWithTag("CardGameManager").transform.GetOrAddComponent<CardGameManager>();
            return instance;
        }
    }

    public static Dictionary<string, CardGame> AllCardGames {
        get {
            return Instance.allCardGames;
        }
    }

    public static CardGame Current {
        get { 
            return Instance.current;
        }
    }

    public static string GamesFilePathBase {
        get {
            return Instance.gamesFilePathBase;
        }
    }

    public static bool IsLoaded {
        get {
            foreach (CardGame game in AllCardGames.Values)
                if (!game.IsLoaded)
                    return false;
            return true;
        }
    }

    private SpriteRenderer BackgroundImage {
        get {
            if (backgroundImage == null)
                backgroundImage = GameObject.FindGameObjectWithTag("Background").transform.GetOrAddComponent<SpriteRenderer>();
            return backgroundImage;
        }
    }
}
