using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class UnityExtensionMethods
{
    static public T FindInParents<T>(this GameObject go) where T : Component
    {
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

    static public T GetOrAddComponent<T>(this GameObject go) where T: Component
    {
        T result = go.GetComponent<T>();
        if (result == null) {
            result = go.AddComponent<T>();
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

    public static IEnumerator SaveURLToFile(string url, string filePath)
    {
        WWW loader = new WWW(url);
        yield return loader;
        if (!string.IsNullOrEmpty(loader.error)) {
            Debug.LogError("Failed to load from " + url + ", error: " + loader.error);
            yield break;
        }

        string directory = GetSafeFilePath(ExtractDirectory(filePath));
        string fileName = GetSafeFileName(ExtractFileName(filePath));
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName)) {
            Debug.LogError("Could not save to " + filePath + ", as it is an improperly formed path");
            yield break;
        }

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(directory + "/" + fileName, loader.bytes);
    }

    public static IEnumerator RunOutputCoroutine<T>(IEnumerator coroutine, Action<T> output) where T : class
    {
        object result = null;
        while (coroutine.MoveNext()) {
            result = coroutine.Current;
            yield return result;
        }
        output(result as T);
    }

    public static IEnumerator CreateAndOutputSpriteFromImageFile(string imageFilePath, string backUpImageURL = null)
    {
        if (!File.Exists(imageFilePath)) {
            if (string.IsNullOrEmpty(backUpImageURL)) {
                Debug.LogWarning("Image file does not exist, and no backup URL is defined, so the sprite will not be updated");
                yield break;
            }
            yield return UnityExtensionMethods.SaveURLToFile(backUpImageURL, imageFilePath);
        }

        WWW imageFileLoader = new WWW("file://" + imageFilePath);
        yield return imageFileLoader;

        if (string.IsNullOrEmpty(imageFileLoader.error)) {
            Texture2D newTexture = imageFileLoader.texture;
            yield return Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), new Vector2(0.5f, 0.5f));
        } else {
            Debug.LogWarning("Failed to load image: " + imageFileLoader.error);
            yield return null;
        }
    }

    public static string ExtractDirectory(string filePath)
    {
        int position = filePath.LastIndexOf('/');
        if (position == -1)
            return String.Empty;
        return filePath.Substring(0, position);
    }

    public static string ExtractFileName(string filePath)
    {
        if (filePath.Trim().EndsWith("/"))
            return String.Empty;
        
        int position = filePath.LastIndexOf('/');
        if (position == -1)
            return filePath;
        
        return filePath.Substring(position + 1);
    }

    public static string GetSafeFilePath(string filePath)
    {
        return string.Join("_", filePath.Split(Path.GetInvalidPathChars()));
    }

    public static string GetSafeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}
