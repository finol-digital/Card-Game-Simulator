using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public const int MainMenuSceneIndex = 1;
    public const int PlayModeSceneIndex = 2;
    public const int DeckEditorSceneIndex = 3;
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

    void Update()
    {
        if (!Input.anyKeyDown || CardGameManager.TopMenuCanvas != null)
            return;

        if (currentGameText == null)
            GoToMainMenu();
        else if (Input.GetButtonDown(CardIn.VerticalInput) && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(multiplayerButton?.gameObject);
        else if (Input.GetButtonDown(CardIn.SortInput))
            SelectCardGame();
        else if (Input.GetButtonDown(CardIn.NewInput))
            StartGame();
        else if (Input.GetButtonDown(CardIn.LoadInput))
            JoinGame();
        else if (Input.GetButtonDown(CardIn.SaveInput))
            EditDeck();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
            CardGameManager.Instance.Messenger.Prompt(ExitPrompt, Quit);
    }

    public void UpdateCurrentGameText()
    {
        if (currentGameText != null)
            currentGameText.text = CardGameManager.CurrentGameName;
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    public void SelectCardGame()
    {
        CardGameManager.Instance.Selector.Show();
    }

    public void StartGame()
    {
        CardGameManager.IsMultiplayer = false;
        SceneManager.LoadScene(PlayModeSceneIndex);
    }

    public void JoinGame()
    {
        CardGameManager.IsMultiplayer = true;
        SceneManager.LoadScene(PlayModeSceneIndex);
    }

    public void EditDeck()
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
}
