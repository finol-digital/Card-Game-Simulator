using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public Text messageText;
    public Button yesButton;
    public Button noButton;
    public Button cancelButton;

    public void Show(string message)
    {
        if (this.gameObject.activeSelf)
            Debug.LogWarning("Showing a message when a message is already being shown! Previous message will be overwritten");

        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        messageText.text = message;
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        cancelButton.GetComponentInChildren<Text>().text = "Close";
    }

    public void Prompt(string message, UnityAction yesAction)
    {
        Show(message);
        yesButton.gameObject.SetActive(true);
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(yesAction);
        yesButton.onClick.AddListener(Close);
        cancelButton.GetComponentInChildren<Text>().text = "Cancel";
    }

    public void Ask(string message, UnityAction noAction, UnityAction yesAction)
    {
        Prompt(message, yesAction);
        noButton.gameObject.SetActive(true);
        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(noAction);
        noButton.onClick.AddListener(Close);
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
