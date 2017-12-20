using System.Globalization;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PointsCounter : NetworkBehaviour
{
    public Text pointsText;

    [SyncVar]
    private float _points;

    public void Decrement()
    {
        Count--;
    }

    public void Increment()
    {
        Count++;
    }

    public float Count {
        get { return _points; }
        set {
            _points = value;
            pointsText.text = _points.ToString(CultureInfo.InvariantCulture);
        }
    }
}
