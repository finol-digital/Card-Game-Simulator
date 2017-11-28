using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json;

public class GameSelectionMenu : MonoBehaviour
{
    public RectTransform gameSelectionArea;
    public RectTransform gameSelectionTemplate;

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
        foreach (string gameName in CardGameManager.Instance.AllCardGames.Keys) {
            GameObject gameSelection = Instantiate(gameSelectionTemplate.gameObject, gameSelectionArea) as GameObject;
            gameSelection.SetActive(true);
            gameSelection.transform.localPosition = pos;
            gameSelection.GetComponentInChildren<Text>().text = gameName;
            Toggle toggle = gameSelection.GetComponent<Toggle>();
            toggle.isOn = gameName.Equals(CardGameManager.CurrentGameName);
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectGame(isOn, gameName));
            toggle.onValueChanged.AddListener(valueChange);
            pos.y -= gameSelectionTemplate.rect.height;
        }

        gameSelectionTemplate.SetParent(gameSelectionArea.parent);
        gameSelectionTemplate.gameObject.SetActive(CardGameManager.Instance.AllCardGames.Count < 1);
        gameSelectionArea.sizeDelta = new Vector2(gameSelectionArea.sizeDelta.x, gameSelectionTemplate.rect.height * CardGameManager.Instance.AllCardGames.Count);
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
            Debug.LogError("Failed to load game url! " + newGame.Error);
            CardGameManager.Instance.Messenger.Show("Failed to load game url! " + newGame.Error);
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