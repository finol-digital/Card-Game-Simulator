using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json;

public class GameSelectionMenu : MonoBehaviour
{
    public const string GameLoadErrorMessage = "Failed to load game url! ";

    public RectTransform gameSelectionArea;
    public RectTransform gameSelectionTemplate;
    public Scrollbar scrollBar;

    public RectTransform downloadPanel;
    public InputField urlInput;
    public Button cancelButton;
    public Button downloadButton;

    public void Show()
    {
        this.gameObject.SetActive(true);

        gameSelectionArea.GetComponent<ToggleGroup>().SetAllTogglesOff();
        gameSelectionArea.DestroyAllChildren();
        gameSelectionTemplate.SetParent(gameSelectionArea);

        Vector3 pos = gameSelectionTemplate.localPosition;
        pos.y = 0;
        float i = 0;
        float index = 0;
        foreach (string gameName in CardGameManager.Instance.AllCardGames.Keys) {
            GameObject gameSelection = Instantiate(gameSelectionTemplate.gameObject, gameSelectionArea) as GameObject;
            gameSelection.SetActive(true);
            // FIX FOR UNITY BUG SETTING SCALE TO 0 WHEN RESOLUTION=REFERENCE_RESOLUTION(1080p)
            gameSelection.transform.localScale = Vector3.one;
            gameSelection.transform.localPosition = pos;
            gameSelection.GetComponentInChildren<Text>().text = gameName;
            Toggle toggle = gameSelection.GetComponent<Toggle>();
            toggle.isOn = gameName.Equals(CardGameManager.CurrentGameName);
            if (toggle.isOn)
                index = i;
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectGame(isOn, gameName));
            toggle.onValueChanged.AddListener(valueChange);
            pos.y -= gameSelectionTemplate.rect.height;
            i++;
        }

        gameSelectionTemplate.SetParent(gameSelectionArea.parent);
        gameSelectionTemplate.gameObject.SetActive(CardGameManager.Instance.AllCardGames.Count < 1);
        gameSelectionArea.sizeDelta = new Vector2(gameSelectionArea.sizeDelta.x, gameSelectionTemplate.rect.height * CardGameManager.Instance.AllCardGames.Count);

        float newSpot = gameSelectionTemplate.GetComponent<RectTransform>().rect.height * (index + ((index < CardGameManager.Instance.AllCardGames.Keys.Count / 2f) ? 0f : 1f)) / gameSelectionArea.sizeDelta.y;
        StartCoroutine(SkipFrameToMoveScrollbar(1 - Mathf.Clamp01(newSpot)));
    }

    public IEnumerator SkipFrameToMoveScrollbar(float scrollBarValue)
    {
        yield return null;
        scrollBar.value = Mathf.Clamp01(scrollBarValue);
    }

    public void SelectGame(bool isOn, string gameName)
    {
        if (!isOn)
            return;

        string previousGameName = CardGameManager.CurrentGameName;
        CardGameManager.Instance.SelectCardGame(gameName);
        if (gameName.Equals(previousGameName))
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
        CardGame newGame = new CardGame(Set.DefaultCode, urlInput.text.Trim());
        newGame.AutoUpdate = true;
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
        this.gameObject.SetActive(false);
    }
}