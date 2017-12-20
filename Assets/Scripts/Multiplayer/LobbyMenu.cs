using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    public const string DeckLoadPrompt = "Would you like to join the game with your own deck?";

    public RectTransform sessionSelectionArea;
    public RectTransform sessionSelectionTemplate;
    public Button cancelButton;
    public Button joinButton;

    public List<string> Hosts { get; set; } = new List<string>();
    public string SelectedHost { get; set; } = "";
    public PlayMode Controller { get; private set; }

    public void Show(PlayMode controller)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Controller = controller;
        LocalNetManager.Instance.Discovery.lobby = this;
        LocalNetManager.Instance.SearchForHost();
    }

    public void DisplayHosts(List<string> hostNames)
    {
        if (hostNames == null || hostNames.Equals(Hosts))
            return;

        if (!hostNames.Contains(SelectedHost)) {
            SelectedHost = string.Empty;
            joinButton.interactable = false;
        }

        sessionSelectionArea.DestroyAllChildren();
        Vector2 localPosition = Vector2.zero;
        foreach (string hostName in hostNames) {
            GameObject hostSelection = Instantiate(sessionSelectionTemplate.gameObject, sessionSelectionArea);
            hostSelection.SetActive(true);
            // FIX FOR UNITY BUG SETTING SCALE TO 0 WHEN RESOLUTION=REFERENCE_RESOLUTION(1080p)
            hostSelection.transform.localScale = Vector3.one;
            hostSelection.transform.localPosition = localPosition;
            Toggle toggle = hostSelection.GetComponent<Toggle>();
            toggle.isOn = hostName.Equals(SelectedHost);
            UnityAction<bool> valueChange = isOn => SelectHost(isOn, hostName);
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = hostSelection.GetComponentInChildren<Text>();
            labelText.text = hostName;
            localPosition.y -= sessionSelectionTemplate.rect.height;
        }
        sessionSelectionArea.sizeDelta = new Vector2(sessionSelectionArea.sizeDelta.x, sessionSelectionTemplate.rect.height * hostNames.Count);

        Hosts = hostNames;
    }

    public void Host()
    {
        NetworkManager.singleton.StartHost();
        NetworkManager.singleton.StartCoroutine(WaitToPromptDeckLoad());
        Hide();
    }

    public void SelectHost(bool isOn, string hostName)
    {
        if (!isOn || string.IsNullOrEmpty(hostName))
            return;

        SelectedHost = hostName;
        joinButton.interactable = true;
    }

    public void Join()
    {
        NetworkManager.singleton.networkAddress = SelectedHost;
        NetworkManager.singleton.StartClient();
        NetworkManager.singleton.StartCoroutine(WaitToPromptDeckLoad());
        Hide();
    }

    public IEnumerator WaitToPromptDeckLoad()
    {
        yield return null;
        if (Controller != null)
            CardGameManager.Instance.Messenger.Ask(DeckLoadPrompt, null, Controller.ShowDeckMenu);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
