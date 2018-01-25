using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyMenu : SelectionPanel
{
    public const string SubmitInput = "Submit";
    public const string VerticalInput = "Vertical";
    public const string NewInput = "New";
    public const string CancelInput = "Cancel";

    public Button cancelButton;
    public Button joinButton;

    public List<string> HostNames { get; private set; } = new List<string>();
    public string SelectedHost { get; private set; } = "";
    
    void Update()
    {
        if (Input.GetButtonUp(SubmitInput) && joinButton.interactable)
            Join();
        else if (Input.GetButtonUp(NewInput))
            Host();
        else if (Input.GetButtonDown(VerticalInput) && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.SetSelectedGameObject(selectionContent.GetChild(0));
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CancelInput))
            Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        HostNames.Clear();
        SelectedHost = string.Empty;
        Rebuild(HostNames, SelectHost, SelectedHost);

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
        NetworkManager.singleton.StartCoroutine(WaitToShowDeckLoader());
        Hide();
    }

    public IEnumerator WaitToShowDeckLoader()
    {
        yield return null;
        LocalNetManager.Instance.playController.ShowDeckMenu();
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
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
