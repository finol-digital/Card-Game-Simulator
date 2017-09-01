using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class UnityExtensionMethods
{
    static public T FindInParents<T>(GameObject go) where T : Component
    {
        if (go == null)
            return null;
        var comp = go.GetComponent<T>();

        if (comp != null)
            return comp;

        var t = go.transform.parent;
        while (t != null && comp == null) {
            comp = t.gameObject.GetComponent<T>();
            t = t.parent;
        }
        return comp;
    }

    static public T GetOrAddComponent<T>(this Component child) where T: Component
    {
        T result = child.GetComponent<T>();
        if (result == null) {
            result = child.gameObject.AddComponent<T>();
        }
        return result;
    }

    static public void DestroyAllChildren(this Transform parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--) {
            Transform child = parent.GetChild(i);
            child.SetParent(null);
            GameObject.Destroy(child.gameObject);
        }
    }

}
