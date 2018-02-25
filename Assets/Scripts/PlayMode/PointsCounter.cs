using System.Globalization;
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

    protected float Count {
        get { return CGSNetManager.Instance.LocalPlayer.Points; }
        set {
            CGSNetManager.Instance.LocalPlayer.Points = value;
            pointsText.text = CGSNetManager.Instance.LocalPlayer.Points.ToString(CultureInfo.InvariantCulture);
        }
    }
}
