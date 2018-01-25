using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public const int MainMenuSceneIndex = 1;
    public const int PlayModeSceneIndex = 2;
    public const int DeckEditorSceneIndex = 3;
    public const string VerticalInput = "Vertical";
    public const string CancelInput = "Cancel";
    public const string ExitPrompt = "Exit CGS?";

    public Text currentGameText;
    public Button multiplayerButton;
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

    void Update()
    {
        if (Input.GetButtonDown(VerticalInput) && EventSystem.current.currentSelectedGameObject == null && multiplayerButton != null && CardGameManager.TopMenuCanvas == null)
            EventSystem.current.SetSelectedGameObject(multiplayerButton.gameObject);
        else if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CancelInput)) && CardGameManager.TopMenuCanvas == null)
            CardGameManager.Instance.Messenger.Prompt(ExitPrompt, Quit);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
