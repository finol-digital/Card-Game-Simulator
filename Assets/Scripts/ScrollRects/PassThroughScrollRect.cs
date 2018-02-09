using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassThroughScrollRect : ScrollRect
{
    [Header("Additional Fields")]
    [SerializeField]
    public ScrollRect parentScrollRect;
    public bool routeToParent = false;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        parentScrollRect?.OnInitializePotentialDrag(eventData);
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
            routeToParent = true;
        else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
            routeToParent = true;
        else
            routeToParent = false;

        if (routeToParent)
            parentScrollRect?.OnBeginDrag(eventData);
        else
            base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (routeToParent)
            parentScrollRect?.OnDrag(eventData);
        else
            base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (routeToParent)
            parentScrollRect?.OnEndDrag(eventData);
        else
            base.OnEndDrag(eventData);

        routeToParent = false;
    }
}
