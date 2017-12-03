using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointsCounter : MonoBehaviour
{
    public Text pointsText;

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
        get {
            return _points;
        }
        set {
            _points = value;
            pointsText.text = _points.ToString();
        }
    }
}
