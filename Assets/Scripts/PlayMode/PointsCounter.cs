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
        Points--;
    }

    public void Increment()
    {
        Points++;
    }

    public float Points {
        get {
            return _points;
        }
        set {
            _points = value;
            pointsText.text = _points.ToString();
        }
    }
}
