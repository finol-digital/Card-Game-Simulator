using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Popup : MonoBehaviour
{
    public const string CloseLabel = "Close";
    public const string CancelLabel = "Cancel";
    
    struct Message
    {
        public string Text;
        public UnityAction NoAction;
        public UnityAction YesAction;
    };

    public Text messageText;
    public Button yesButton;
    public Button noButton;
    public Button cancelButton;

    public Queue<Message> MessageQueue { get; } = new Queue<Message>();

    public bool IsNewMessage { get; private set; }

    void LateUpdate()
    {
        if (Input.GetButtonDown(CardIn.SubmitInput) && yesButton.gameObject.activeInHierarchy)
            yesButton.onClick?.Invoke();
        else if (!IsNewMessage && Input.GetButtonDown(CardIn.SubmitInput) && !yesButton.gameObject.activeInHierarchy)
            Close();
        else if (Input.GetButtonDown(CardIn.NoInput) && noButton.gameObject.activeInHierarchy)
            noButton.onClick?.Invoke();
        else if (!IsNewMessage && (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput)))
            Close();
        IsNewMessage = false;
    }

    public void Show(string text)
    {
        Prompt(text, null);
    }

    public void Prompt(string text, UnityAction yesAction)
    {
        Ask(text, null, yesAction);
    }

    public void Ask(string text, UnityAction noAction, UnityAction yesAction)
    {
        Message message = new Message() {Text = text, NoAction = noAction, YesAction = yesAction};
        if (gameObject.activeSelf) {
            MessageQueue.Enqueue(message);
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        DisplayMessage(message);
    }

    private void DisplayMessage(Message message)
    {
        messageText.text = message.Text ?? string.Empty;
        yesButton.gameObject.SetActive(message.YesAction != null);
        noButton.gameObject.SetActive(message.NoAction != null);
        
        yesButton.onClick.RemoveAllListeners();
        if (yesAction != null)
            yesButton.onClick.AddListener(yesAction);
        yesButton.onClick.AddListener(Close);
        
        noButton.onClick.RemoveAllListeners();
        if (noAction != null)
            noButton.onClick.AddListener(noAction);
        noButton.onClick.AddListener(Close);
        
        if (message.YesAction == null && message.NoAction == null)
            cancelButton.GetComponentInChildren<Text>().text = CloseLabel;
        else
            cancelButton.GetComponentInChildren<Text>().text = CancelLabel;
        
        IsNewMessage = true;
    }

    public void Close()
    {
        if (MessageQueue.Count > 0)
            DisplayMessage(MessageQueue.Dequeue());
        else
            gameObject.SetActive(false);
    }
}
