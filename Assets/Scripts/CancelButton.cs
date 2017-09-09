using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class CancelButton : MonoBehaviour
{
    private Button _button;
    private GraphicRaycaster _rayCaster;

    void Start()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
        Button.GetComponent<Graphic>().raycastTarget = true;
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel")) && IsPressable)
            Button.onClick.Invoke();
    }

    public Button Button {
        get {
            if (_button == null)
                _button = GetComponent<Button>();
            return _button;
        }
    }

    public bool IsPressable {
        get {
            if (_rayCaster == null)
                _rayCaster = this.gameObject.FindInParents<Canvas>().GetComponent<GraphicRaycaster>();
            PointerEventData ped = new PointerEventData(null);
            ped.position = Button.transform.position;
            List<RaycastResult> results = new List<RaycastResult>();
            _rayCaster.Raycast(ped, results);
            if (results.Count < 1 || results [0].gameObject != Button.gameObject)
                return false;
            return true;
        }
    }
}
