using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public Text messageText;
    public Button yesButton;

    public void Show(string message)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        messageText.text = message;
        yesButton.gameObject.SetActive(false);
    }

    public void Prompt(string message, UnityAction action)
    {
        Show(message);
        yesButton.gameObject.SetActive(true);
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(action);
        yesButton.onClick.AddListener(Close);
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
