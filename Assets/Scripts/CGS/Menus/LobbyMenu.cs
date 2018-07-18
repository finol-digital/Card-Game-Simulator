using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyMenu : SelectionPanel
{
    public Button joinButton;

    public List<string> HostNames { get; private set; } = new List<string>();
    public string SelectedHost { get; private set; } = "";

    void Update()
    {
        if (!Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;

        if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && joinButton.interactable)
            Join();
        else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Contains(EventSystem.current.currentSelectedGameObject))
            EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
        else if (Input.GetButtonDown(Inputs.New))
            Host();
        else if (Input.GetButtonDown(Inputs.Vertical))
            ScrollToggles(Input.GetAxis(Inputs.Vertical) > 0);
        else if (Input.GetButtonDown(Inputs.Page))
            ScrollPage(Input.GetAxis(Inputs.Page) < 0);
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
            Hide();
    }

    public void Show(UnityAction cancelAction)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(cancelAction);

        HostNames.Clear();
        SelectedHost = string.Empty;
        Rebuild(HostNames, SelectHost, SelectedHost);

        CardGameManager.Instance.Discovery.lobby = this;
        CardGameManager.Instance.Discovery.SearchForHost();
    }

    public void DisplayHosts(List<string> hostNames)
    {
        if (hostNames == null || hostNames.Equals(HostNames))
            return;

        HostNames.Clear();
        foreach (string hostname in hostNames)
            HostNames.Add(hostname.Split(':').Last());
        if (!HostNames.Contains(SelectedHost))
        {
            SelectedHost = string.Empty;
            joinButton.interactable = false;
        }

        Rebuild(HostNames, SelectHost, SelectedHost);
    }

    public void Host(UnityAction cancelAction = null)
    {
        NetworkManager.singleton.StartHost();
        NetworkManager.singleton.StartCoroutine(WaitToShowDeckLoader(cancelAction));
        Hide();
    }

    public IEnumerator WaitToShowDeckLoader(UnityAction cancelAction)
    {
        yield return null;
        CGSNetManager.Instance.playController.ShowDeckMenu();
        CGSNetManager.Instance.playController.DeckLoader.cancelButton.onClick.RemoveAllListeners();
        CGSNetManager.Instance.playController.DeckLoader.cancelButton.onClick.AddListener(cancelAction);
    }

    public void SelectHost(Toggle toggle, string hostName)
    {
        if (string.IsNullOrEmpty(hostName))
            return;

        if (toggle.isOn)
        {
            SelectedHost = hostName;
            joinButton.interactable = true;
        }
        else if (!toggle.group.AnyTogglesOn() && SelectedHost.Equals(hostName))
            Join();
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
