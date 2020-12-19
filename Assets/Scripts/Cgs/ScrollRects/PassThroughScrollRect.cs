using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollRects
{
    public class PassThroughScrollRect : ScrollRect
    {
        [Header("Additional Fields")] [SerializeField]
        public ScrollRect parentScrollRect;

        public bool routeToParent;

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.OnInitializePotentialDrag(eventData);
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
            {
                if (parentScrollRect != null)
                    parentScrollRect.OnBeginDrag(eventData);
            }
            else
                base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (routeToParent)
            {
                if (parentScrollRect != null)
                    parentScrollRect.OnDrag(eventData);
            }
            else
                base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (routeToParent)
            {
                if (parentScrollRect != null)
                    parentScrollRect.OnEndDrag(eventData);
            }
            else
                base.OnEndDrag(eventData);

            routeToParent = false;
        }
    }
}
