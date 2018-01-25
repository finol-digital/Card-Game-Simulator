using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public const string CloseLabel = "Close";
    public const string CancelLabel = "Cancel";

    public Text messageText;
    public Button yesButton;
    public Button noButton;
    public Button cancelButton;

    public Queue<string> MessageQueue { get; } = new Queue<string>();

    void Update()
    {
        if (Input.GetButtonDown(CardIn.SubmitInput) && yesButton.gameObject.activeInHierarchy)
            yesButton.onClick?.Invoke();
        else if (Input.GetButtonDown(CardIn.NoInput) && noButton.gameObject.activeInHierarchy)
            noButton.onClick?.Invoke();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
            Close();
    }

    public void Show(string message)
    {
        if (gameObject.activeSelf) {
            MessageQueue.Enqueue(message);
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        DisplayMessage(message);
    }

    public void Prompt(string message, UnityAction yesAction)
    {
        Show(message);
        yesButton.gameObject.SetActive(true);
        yesButton.onClick.RemoveAllListeners();
        if (yesAction != null)
            yesButton.onClick.AddListener(yesAction);
        yesButton.onClick.AddListener(Close);
        cancelButton.GetComponentInChildren<Text>().text = CancelLabel;
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

    private void DisplayMessage(string message)
    {
        messageText.text = message ?? string.Empty;
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        cancelButton.GetComponentInChildren<Text>().text = CloseLabel;
    }

    public void Close()
    {
        if (MessageQueue.Count > 0)
            DisplayMessage(MessageQueue.Dequeue());
        else
            gameObject.SetActive(false);
    }
}
