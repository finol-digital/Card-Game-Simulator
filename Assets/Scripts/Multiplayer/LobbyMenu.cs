using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyMenu : SelectionPanel
{
    public Button cancelButton;
    public Button joinButton;

    public List<string> HostNames { get; private set; } = new List<string>();
    public string SelectedHost { get; private set; } = "";

    void Update()
    {
        if (!Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;

        if (Input.GetButtonDown(Inputs.Submit) && joinButton.interactable)
            Join();
        else if (Input.GetButtonDown(Inputs.New))
            Host();
        else if (Input.GetButtonDown(Inputs.Vertical) && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(selectionContent.GetChild(0)?.gameObject);
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
            Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        HostNames.Clear();
        SelectedHost = string.Empty;
        Rebuild(HostNames, SelectHost, SelectedHost);

        CGSNetManager.Instance.Discovery.lobby = this;
        CGSNetManager.Instance.SearchForHost();
    }

    public void DisplayHosts(List<string> hostNames)
    {
        if (hostNames == null || hostNames.Equals(HostNames))
            return;

		HostNames.Clear ();
		foreach (string hostname in hostNames)
			HostNames.Add (hostname.Split (':').Last ());
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
        CGSNetManager.Instance.playController.ShowDeckMenu();
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
