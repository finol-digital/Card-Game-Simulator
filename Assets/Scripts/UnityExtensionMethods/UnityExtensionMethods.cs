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
        public static T FindInParents<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
                return component;

            Transform transform = go.transform.parent;
            while (transform != null && component == null)
            {
                component = transform.gameObject.GetComponent<T>();
                transform = transform.parent;
            }

            return component;
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        public static void DestroyAllChildren(this Transform parent)
        {
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
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
    }
}
