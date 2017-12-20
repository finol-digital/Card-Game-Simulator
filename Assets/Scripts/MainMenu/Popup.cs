using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public const string OverwriteWarning = "Showing a message when a message is already being shown! Previous message will be overwritten";
    public const string CloseString = "Close";
    public const string CancelString = "Cancel";

    public Text messageText;
    public Button yesButton;
    public Button noButton;
    public Button cancelButton;

    public void Show(string message)
    {
        if (gameObject.activeSelf)
            Debug.LogWarning(OverwriteWarning);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        messageText.text = message;
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        cancelButton.GetComponentInChildren<Text>().text = CloseString;
    }

    public void Prompt(string message, UnityAction yesAction)
    {
        Show(message);
        yesButton.gameObject.SetActive(true);
        yesButton.onClick.RemoveAllListeners();
        if (yesAction != null)
            yesButton.onClick.AddListener(yesAction);
        yesButton.onClick.AddListener(Close);
        cancelButton.GetComponentInChildren<Text>().text = CancelString;
    }

    public void Ask(string message, UnityAction noAction, UnityAction yesAction)
    {
        Prompt(message, yesAction);
        noButton.gameObject.SetActive(true);
        noButton.onClick.RemoveAllListeners();
        if (noAction != null)
            noButton.onClick.AddListener(noAction);
        noButton.onClick.AddListener(Close);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
