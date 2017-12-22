using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyMenu : SelectionPanel
{
    public const string DeckLoadPrompt = "Would you like to join the game with your own deck?";

    public List<string> HostNames { get; set; } = new List<string>();
    public string SelectedHost { get; set; } = "";

    public Button cancelButton;
    public Button joinButton;

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
        if (hostNames == null || hostNames.Equals(HostNames))
            return;

        HostNames = hostNames;
        if (!HostNames.Contains(SelectedHost)) {
            SelectedHost = string.Empty;
            joinButton.interactable = false;
        }

        Rebuild(HostNames, SelectHost, SelectedHost);
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
