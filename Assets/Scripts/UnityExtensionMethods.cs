using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;

public static class ThreadSafeRandom
{
    [ThreadStatic] private static System.Random Local;

    public static System.Random ThisThreadsRandom {
        get { return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + System.Threading.Thread.CurrentThread.ManagedThreadId))); }
    }
}

static public class UnityExtensionMethods
{
    static public void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list [k];
            list [k] = list [n];
            list [n] = value;
        }
    }

    static public T FindInParents<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();

        if (component != null)
            return component;

        Transform transform = go.transform.parent;
        while (transform != null && component == null) {
            component = transform.gameObject.GetComponent<T>();
            transform = transform.parent;
        }
        return component;
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
            Debug.LogWarning("Failed to load from " + url + ", error: " + loader.error);
            yield break;
        }

        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName)) {
            Debug.LogWarning("Could not save to " + filePath + ", as it is an improperly formed path");
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
            if (string.IsNullOrEmpty(backUpImageURL))
                yield break;
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

    public static string GetSafeFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;
        return string.Join("_", filePath.Split(Path.GetInvalidPathChars()));
    }

    public static string GetSafeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    public static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        foreach (string filePath in Directory.GetFiles(sourceDir))
            File.Copy(filePath, Path.Combine(targetDir, Path.GetFileName(filePath)));

        foreach (string directory in Directory.GetDirectories(sourceDir))
            CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
    }

    public static void ExtractZip(string zipPath, string targetDir)
    {
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        FastZip fastZip = new FastZip();
        fastZip.ExtractZip(zipPath, targetDir, null);
    }

}
