using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject addGameMenuPrefab;
    public GameObject quitButton;

    private AddGameMenu _addGameMenu;

    public void ShowAddGameMenu()
    {
        AddGameMenu.Show();
    }

    public void GoToPlayMode()
    {
        SceneManager.LoadScene(1);
    }

    public void GoToDeckEditor()
    {
        SceneManager.LoadScene(2);
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

    public AddGameMenu AddGameMenu {
        get {
            if (_addGameMenu == null)
                _addGameMenu = Instantiate(addGameMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<AddGameMenu>();
            return _addGameMenu;
        }
    }
}
