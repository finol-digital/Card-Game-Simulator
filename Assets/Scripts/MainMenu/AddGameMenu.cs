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
        urlInput.text = UniClipboard.GetText();
    }

    public void Clear()
    {
        urlInput.text = string.Empty;
    }

    public void AddGame()
    {
        CardGameManager.Instance.StartCoroutine(LoadGame());
        Hide();
    }

    public IEnumerator LoadGame()
    {
        string error = string.Empty;
        CardGame newGame = new CardGame(string.Empty, urlInput.text.Trim());
        WWW load = new WWW(newGame.AutoUpdateURL);
        yield return load;
        if (!string.IsNullOrEmpty(load.error))
            error += load.error;

        try {
            if (string.IsNullOrEmpty(error))
                JsonConvert.PopulateObject(load.text, newGame);
        } catch (Exception e) {
            error += e.Message;
        }

        if (!string.IsNullOrEmpty(newGame.Name)) {
            // TODO: CHECK THE CARD GAME NAME DOESN'T ALREADY EXIST
            PlayerPrefs.SetString(CardGameManager.PlayerPrefGameName, newGame.Name);
            CardGameManager.Instance.AllCardGames [newGame.Name] = newGame;
            CardGameManager.Instance.ResetGameSelection();
        } else
            CardGameManager.Instance.Popup.Show("Failed to load game url! " + error);
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
