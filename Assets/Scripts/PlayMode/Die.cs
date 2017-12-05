using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Die : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public const float RollTime = 1.0f;
    public const float RollDelay = 0.05f;

    public int Min { get; set; }

    public int Max { get; set; }

    public Text valueText;

    public Vector2 DragOffset { get; private set; }

    private int _value;

    void Start()
    {
        Roll();
    }

    public void Roll()
    {
        StartCoroutine(DoRoll());
    }

    public IEnumerator DoRoll()
    {
        float elapsedTime = 0f;
        while (elapsedTime < RollTime) {
            Value = Random.Range(Min, Max);
            yield return new WaitForSeconds(RollDelay);
            elapsedTime += RollDelay;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        DragOffset = eventData.position - ((Vector2)transform.position);
        this.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.transform.position = eventData.position - DragOffset;
    }

    public int Value {
        get {
            return _value;
        }
        set {
            _value = value;
            valueText.text = _value.ToString();
        }
    }
}