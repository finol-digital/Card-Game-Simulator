using UnityEngine;
using UnityEngine.UI;

public class PointsCounter : MonoBehaviour
{
    public Text pointsText;

    public void Decrement()
    {
        Count--;
    }

    public void Increment()
    {
        Count++;
    }

    public void UpdateText()
    {
        pointsText.text = Count.ToString();
    }

    protected int Count {
        get { return CGSNetManager.Instance.LocalPlayer.CurrentScore; }
        set { CGSNetManager.Instance.LocalPlayer.RequestScoreUpdate(value); }
    }
}
