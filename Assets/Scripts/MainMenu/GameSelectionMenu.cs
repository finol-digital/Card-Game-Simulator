using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameSelectionMenu : SelectionPanel
{
    public const string DeleteMessage = "Please download additional card games before deleting.";
    public const string DeletePrompt = "Deleting a card game also deletes all decks saved for that card game. Are you sure you would like to delete this card game?";
    public const string DominoesUrl = "https://cardgamesim.finoldigital.com/Dominoes/Dominoes.json";
    public const string StandardUrl = "https://cardgamesim.finoldigital.com/Standard/Standard.json";
    public const string MahjongUrl = "https://cardgamesim.finoldigital.com/Mahjong/Mahjong.json";

    public RectTransform downloadPanel;
    public InputField urlInput;
    public Button cancelButton;
    public Button downloadButton;

    void LateUpdate()
    {
        if (urlInput.isFocused || !Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;

        if (downloadPanel.gameObject.activeSelf) {
            if (Input.GetButtonDown(CardIn.SubmitInput) && downloadButton.interactable)
                StartDownload();
            else if (Input.GetButtonDown(CardIn.SaveInput) && urlInput.interactable)
                Paste();
            else if (Input.GetButtonDown(CardIn.NewInput) && urlInput.interactable)
                Clear();
            else if (Input.GetButtonDown(CardIn.FocusNameInput) && urlInput.interactable)
                urlInput.ActivateInputField();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
                HideDownloadPanel();
        } else {
            if (Input.GetButtonDown(CardIn.SubmitInput) && EventSystem.current.currentSelectedGameObject == null)
                Hide();
            else if (Input.GetButtonDown(CardIn.LoadInput))
                ShowDownloadPanel();
            else if (Input.GetButtonDown(CardIn.DeleteInput))
                Delete();
            else if (Input.GetButtonDown(CardIn.VerticalInput) && EventSystem.current.currentSelectedGameObject == null)
                EventSystem.current.SetSelectedGameObject(selectionContent.GetChild(0)?.gameObject);
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
                Hide();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Rebuild(CardGameManager.Instance.AllCardGames.Keys.ToList(), SelectGame, CardGameManager.Current.Name);
    }

    public void SelectGame(bool isOn, string gameName)
    {
        if (isOn) {
            gameObject.SetActive(true);
            CardGameManager.Instance.SelectCardGame(gameName);
        }
        else
            Hide();
    }

    public void Delete()
    {
        if (CardGameManager.Instance.AllCardGames.Count > 1)
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, CardGameManager.Instance.DeleteGame);
        else
            CardGameManager.Instance.Messenger.Show(DeleteMessage);
    }

    public void ShowDownloadPanel()
    {
        downloadPanel.gameObject.SetActive(true);
    }

    public void ApplyDominoes()
    {
        if (urlInput.interactable)
            urlInput.text = DominoesUrl;
    }

    public void ApplyStandard()
    {
        if (urlInput.interactable)
            urlInput.text = StandardUrl;
    }

    public void ApplyMahjong()
    {
        if (urlInput.interactable)
            urlInput.text = MahjongUrl;
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
        string gameUrl = urlInput.text.Trim();

        urlInput.text = string.Empty;
        urlInput.interactable = false;
        cancelButton.interactable = false;

        yield return CardGameManager.Instance.DownloadCardGame(gameUrl);

        cancelButton.interactable = true;
        urlInput.interactable = true;
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
