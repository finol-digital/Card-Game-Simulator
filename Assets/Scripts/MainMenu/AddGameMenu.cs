using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class AddGameMenu : MonoBehaviour
{
    public InputField urlInput;
    public Button addButton;

    public void Show()
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
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

    public void AddGame()
    {
        CardGameManager.Instance.StartCoroutine(LoadGame());
    }

    public IEnumerator LoadGame()
    {
        urlInput.text = string.Empty;
        urlInput.interactable = false;
        CardGame newGame = new CardGame(CardGame.DefaultSet, urlInput.text.Trim());
        newGame.AutoUpdate = true;
        yield return newGame.Load();

        if (string.IsNullOrEmpty(newGame.Error)) {
            PlayerPrefs.SetString(CardGameManager.PlayerPrefGameName, newGame.Name);
            CardGameManager.Instance.AllCardGames [newGame.Name] = newGame;
            CardGameManager.Instance.ResetGameSelection();
        } else {
            Debug.LogError("Failed to load game url! " + newGame.Error);
            CardGameManager.Instance.Popup.Show("Failed to load game url! " + newGame.Error);
        }
        urlInput.interactable = true;
        Hide();
    }

    void Update()
    {
        addButton.interactable = System.Uri.IsWellFormedUriString(urlInput.text.Trim(), System.UriKind.Absolute); 
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
