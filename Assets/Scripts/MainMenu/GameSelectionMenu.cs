using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameSelectionMenu : SelectionPanel
{
    public const string GameLoadErrorMessage = "Failed to load game url! ";

    public RectTransform downloadPanel;
    public InputField urlInput;
    public Button cancelButton;
    public Button downloadButton;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Rebuild(CardGameManager.Instance.AllCardGames.Keys.ToList(), SelectGame, CardGameManager.CurrentGameName);
    }

    public void SelectGame(bool isOn, string gameName)
    {
        CardGameManager.Instance.SelectCardGame(gameName);
        if (isOn)
            gameObject.SetActive(true);
        else
            Hide();
    }

    public void ShowDownloadPanel()
    {
        downloadPanel.gameObject.SetActive(true);
    }

    public void Paste()
    {
        if (urlInput.interactable)
            urlInput.text = UniClipboard.GetText();
    }

    public void Clear()
    {
        urlInput.text = string.Empty;
    }

    public void CheckDownloadUrl(string url)
    {
        downloadButton.interactable = System.Uri.IsWellFormedUriString(url.Trim(), System.UriKind.Absolute);
    }

    public void StartDownload()
    {
        CardGameManager.Instance.StartCoroutine(DownloadGame());
    }

    public IEnumerator DownloadGame()
    {
        CardGame newGame = new CardGame(Set.DefaultCode, urlInput.text.Trim()) {AutoUpdate = true};
        urlInput.text = string.Empty;
        urlInput.interactable = false;
        cancelButton.interactable = false;
        yield return newGame.Load();

        if (string.IsNullOrEmpty(newGame.Error)) {
            PlayerPrefs.SetString(CardGameManager.PlayerPrefGameName, newGame.Name);
            CardGameManager.Instance.AllCardGames [newGame.Name] = newGame;
            CardGameManager.Instance.SelectCardGame(newGame.Name);
        } else {
            Debug.LogError(GameLoadErrorMessage + newGame.Error);
            CardGameManager.Instance.Messenger.Show(GameLoadErrorMessage + newGame.Error);
        }
        urlInput.interactable = true;
        cancelButton.interactable = true;
        HideDownloadPanel();
    }

    public void HideDownloadPanel()
    {
        Show();
        downloadPanel.gameObject.SetActive(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
