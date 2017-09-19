using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject quitButton;

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
        Application.Quit();
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
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
}
