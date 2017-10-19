using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public const int PlayModeSceneIndex = 1;
    public const int DeckEditorSceneIndex = 2;

    public GameObject gameLoadMenuPrefab;
    public GameObject quitButton;
    public Text versionText;

    private GameLoadMenu _gameLoader;

    void Start()
    {
        versionText.text = "Ver. " + Application.version;
    }

    public void ShowGameLoadMenu()
    {
        GameLoader.Show();
    }

    public void GoToPlayMode()
    {
        SceneManager.LoadScene(PlayModeSceneIndex);
    }

    public void GoToDeckEditor()
    {
        SceneManager.LoadScene(DeckEditorSceneIndex);
    }

    public void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    void Start()
    {
        quitButton.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
    #endif

    public GameLoadMenu GameLoader {
        get {
            if (_gameLoader == null)
                _gameLoader = Instantiate(gameLoadMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<GameLoadMenu>();
            return _gameLoader;
        }
    }
}
