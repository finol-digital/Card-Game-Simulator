using System;
using System.IO;
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

    static public IEnumerator RunOutputCoroutine<T>(IEnumerator target, Action<T> output) where T : class
    {
        object result = null;
        while (target.MoveNext()) {
            result = target.Current;
            yield return result;
        }
        output(result as T);
    }

    static public IEnumerator SaveURLToFile(string url, string filePath)
    {
        Debug.Log("Saving from " + url + " to " + filePath);
        WWW loader = new WWW(url);
        yield return loader;

        string directory = filePath.Substring(0, filePath.LastIndexOf('/'));
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (string.IsNullOrEmpty(loader.error))
            File.WriteAllBytes(filePath, loader.bytes);
        else
            Debug.LogWarning("Failed to save from " + url + " to " + filePath + ", error: " + loader.error);
    }

    static public IEnumerator LoadOrGetImage(string imageFilePath, string backUpImageURL)
    {
        if (!File.Exists(imageFilePath)) {
            if (string.IsNullOrEmpty(backUpImageURL)) {
                Debug.Log("Image file does not exist, and no backup URL is defined, so the image will not be updated");
                yield break;
            }
            yield return UnityExtensionMethods.SaveURLToFile(backUpImageURL, imageFilePath);
        }

        WWW imageFileLoader = new WWW("file://" + imageFilePath);
        yield return imageFileLoader;
        if (string.IsNullOrEmpty(imageFileLoader.error))
            yield return Sprite.Create(imageFileLoader.texture, new Rect(0, 0, imageFileLoader.texture.width, imageFileLoader.texture.height), new Vector2(0.5f, 0.5f));
        else
            Debug.LogWarning("Failed to load image from " + imageFilePath + ", error: " + imageFileLoader.error);
    }

    static public string GetSafeFilepath(string filepath)
    {
        return string.Join("_", filepath.Split(Path.GetInvalidPathChars()));
    }

    static public string GetSafeFilename(string filename)
    {
        return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }
}
