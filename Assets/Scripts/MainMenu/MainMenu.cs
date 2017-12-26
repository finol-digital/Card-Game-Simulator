using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public const int MainMenuSceneIndex = 1;
    public const int PlayModeSceneIndex = 2;
    public const int DeckEditorSceneIndex = 3;
    public const string ExitPrompt = "Exit CGS?";

    public Text currentGameText;
    public Button exitButton;
    public Text versionText;

    void OnEnable()
    {
        if (currentGameText != null)
            CardGameManager.Instance.OnSceneActions.Add(UpdateCurrentGameText);
    }

    void Start()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
#endif
        versionText.text = "Ver. " + Application.version;
    }

    public void UpdateCurrentGameText()
    {
        if (currentGameText != null)
            currentGameText.text = CardGameManager.CurrentGameName;
    }

    public void GoToMainMenu()
    {
        CardGameManager.Instance.Selector.Show();
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    public void SelectCardGame()
    {
        CardGameManager.Instance.Selector.Show();
    }

    public void PlaySolo()
    {
        CardGameManager.IsMultiplayer = false;
        SceneManager.LoadScene(PlayModeSceneIndex);
    }

    public void PlayLocal()
    {
        CardGameManager.IsMultiplayer = true;
        SceneManager.LoadScene(PlayModeSceneIndex);
    }

    public void EditDeck()
    {
        SceneManager.LoadScene(DeckEditorSceneIndex);
    }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    void Update()
    {
        if (CardGameManager.Instance.Messenger.gameObject.activeSelf || CardGameManager.Instance.Selector.gameObject.activeSelf)
            return;
        if (Input.GetKeyDown(KeyCode.Escape))
            CardGameManager.Instance.Messenger.Prompt(ExitPrompt, Quit);
    }
#endif

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
