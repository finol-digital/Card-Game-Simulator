using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public Text messageText;
    public Button yesButton;
    public Button cancelButton;

    public void Show(string message)
    {
        if (this.gameObject.activeSelf)
            Debug.LogWarning("Showing a message when a message is already being shown! Previous message will be overwritten");

        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        messageText.text = message;
        yesButton.gameObject.SetActive(false);
        cancelButton.GetComponentInChildren<Text>().text = "Close";
    }

    public void Prompt(string message, UnityAction action)
    {
        Show(message);
        yesButton.gameObject.SetActive(true);
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(action);
        yesButton.onClick.AddListener(Close);
        cancelButton.GetComponentInChildren<Text>().text = "Cancel";
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
