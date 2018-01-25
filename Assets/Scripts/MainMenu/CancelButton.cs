using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class CancelButton : MonoBehaviour
{
    public const string CancelInput = "Cancel";

    public GraphicRaycaster RayCaster { get; private set; }

    void Start()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
        GetComponent<Button>().GetComponent<Graphic>().raycastTarget = true;
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CancelInput)) && IsPressable)
            GetComponent<Button>().onClick.Invoke();
    }

    public bool IsPressable {
        get {
            if (!gameObject.GetComponentInParent<Canvas>().Equals(CardGameManager.TopMenuCanvas))
                return false;
            if (RayCaster == null)
                RayCaster = gameObject.FindInParents<Canvas>().GetComponent<GraphicRaycaster>();
            PointerEventData ped = new PointerEventData(null) {position = GetComponent<Button>().transform.position};
            List<RaycastResult> results = new List<RaycastResult>();
            RayCaster.Raycast(ped, results);
            return results.Count >= 1 && results [0].gameObject == GetComponent<Button>().gameObject;
        }
    }
}
