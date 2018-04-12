using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public const string NoRulesErrorMessage = "This card game has not correctly set a link to an online rulebook.";
    public const string DeveloperEmail = "david@finoldigital.com";
    public const string EmailSubject = "Card Game Simulator Feedback";
    public const string WebsiteUrl = "https://www.cardgamesimulator.com";
    public const string KeyboardUrl = "https://www.cardgamesimulator.com/KEYBOARD.html";

    public List<GameObject> buttons;
    public Text versionText;

    void Start()
    {
        versionText.text = MainMenu.VersionMessage + Application.version;
    }

    void Update()
    {
        if (Input.GetKeyDown(Inputs.BluetoothReturn))
            EventSystem.current.currentSelectedGameObject?.GetComponent<Button>()?.onClick?.Invoke();
        else if (Input.GetButtonDown(Inputs.Vertical) && !buttons.Contains(EventSystem.current.currentSelectedGameObject))
            EventSystem.current.SetSelectedGameObject(buttons[1].gameObject);
        else if (Input.GetButtonDown(Inputs.Sort))
            ViewRules();
        else if (Input.GetButtonDown(Inputs.New))
            ViewWebsite();
        else if (Input.GetButtonDown(Inputs.Load))
            ViewKeyboardShortcuts();
        else if (Input.GetButtonDown(Inputs.Save))
            ContactDeveloper();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
            BackToMainMenu();
    }

    public void ViewRules()
    {
        if (Uri.IsWellFormedUriString(CardGameManager.Current.RulesUrl, UriKind.Absolute))
            Application.OpenURL(CardGameManager.Current.RulesUrl);
        else
            CardGameManager.Instance.Messenger.Show(NoRulesErrorMessage);
    }

    public void ViewWebsite()
    {
        Application.OpenURL(WebsiteUrl);
    }

    public void ViewKeyboardShortcuts()
    {
        Application.OpenURL(KeyboardUrl);
    }

    public void ContactDeveloper()
    {
        Application.OpenURL("mailto:" + DeveloperEmail + "?subject=" + EscapeUrl(EmailSubject));
    }

    private string EscapeUrl(string url)
    {
        return WWW.EscapeURL(url).Replace("+", "%20");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
    }
}
