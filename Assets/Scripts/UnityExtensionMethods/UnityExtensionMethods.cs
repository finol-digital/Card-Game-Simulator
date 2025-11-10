/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityExtensionMethods
{
    public static class UnityExtensionMethods
    {
        public enum SwipeDirection
        {
            None,
            Left,
            Right,
            Down,
            Up
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        public static void DestroyAllChildren(this Transform parent)
        {
            for (var i = parent.transform.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null);
                Object.Destroy(child.gameObject);
            }
        }

        public static Vector2 CalculateMean(List<Vector2> list)
        {
            if (list == null || list.Count == 0)
                return Vector2.zero;
            return list.Aggregate(Vector2.zero, (current, vector) => current + vector) / list.Count;
        }

        public static SwipeDirection GetSwipeDirection(Vector2 dragVector2)
        {
            if (dragVector2.sqrMagnitude <= 0.0001f)
                return SwipeDirection.None;

            var positiveX = Mathf.Abs(dragVector2.x);
            var positiveY = Mathf.Abs(dragVector2.y);
            SwipeDirection swipeDir;
            swipeDir = (positiveX > positiveY)
                ? ((dragVector2.x > 0) ? SwipeDirection.Right : SwipeDirection.Left)
                : ((dragVector2.y > 0) ? SwipeDirection.Up : SwipeDirection.Down);
            return swipeDir;
        }
    }
}
