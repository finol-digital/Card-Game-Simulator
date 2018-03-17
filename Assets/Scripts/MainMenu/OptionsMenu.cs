using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public const string NoRulesErrorMessage = "This card game has not correctly set a link to an online rulebook.";
    public const string DeveloperEmail = "david@finoldigital.com";
    public const string EmailSubject = "Card Game Simulator Feedback";

    public Text versionText;

    void Start()
    {
        versionText.text = "Ver. " + Application.version;
    }

    void Update()
    {
        if (Input.GetButtonDown(Inputs.Sort))
            ViewRules();
        else if (Input.GetButtonDown(Inputs.New))
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
