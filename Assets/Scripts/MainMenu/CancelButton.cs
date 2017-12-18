using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class CancelButton : MonoBehaviour
{
    private GraphicRaycaster _rayCaster;

    void Start()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
        GetComponent<Button>().GetComponent<Graphic>().raycastTarget = true;
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel")) && IsPressable)
            GetComponent<Button>().onClick.Invoke();
    }

    public bool IsPressable {
        get {
            if (!this.gameObject.GetComponentInParent<Canvas>().Equals(CardGameManager.Instance.TopCanvas))
                return false;
            if (_rayCaster == null)
                _rayCaster = this.gameObject.FindInParents<Canvas>().GetComponent<GraphicRaycaster>();
            PointerEventData ped = new PointerEventData(null);
            ped.position = GetComponent<Button>().transform.position;
            List<RaycastResult> results = new List<RaycastResult>();
            _rayCaster.Raycast(ped, results);
            if (results.Count < 1 || results [0].gameObject != GetComponent<Button>().gameObject)
                return false;
            return true;
        }
    }
}
