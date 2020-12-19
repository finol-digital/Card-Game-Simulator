using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_ANDROID && !UNITY_EDITOR
using ICSharpCode.SharpZipLib.Core;
#endif

namespace CardGameDef.Unity
{
    public static class UnityFileMethods
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    public const string AndroidStreamingAssetsDirectory = "assets/";
    public const string AndroidStreamingAssetsInternalDataDirectory = "assets/bin/";
    public const string DirectorySeparator = "/";
#endif
        public const string FilePrefix = "file://";
        public const string MetaExtension = ".meta";
        public const string ZipExtension = ".zip";
        public const string JsonExtension = ".json";

        public static string GetSafeFilePath(string filePath)
        {
            return !string.IsNullOrEmpty(filePath)
                ? string.Join("_", filePath.Split(Path.GetInvalidPathChars()))
                : string.Empty;
        }

        public static string GetSafeFileName(string fileName)
        {
            return !string.IsNullOrEmpty(fileName)
                ? string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()))
                : string.Empty;
        }

        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
                if (!filePath.EndsWith(MetaExtension))
                    File.Copy(filePath, Path.Combine(targetDir, Path.GetFileName(filePath)));

            foreach (string directory in Directory.GetDirectories(sourceDir))
                if (!string.IsNullOrEmpty(directory))
                    CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

#if ENABLE_WINMD_SUPPORT
    public static async System.Threading.Tasks.Task<string> CacheFileAsync(string sourceFilePath)
    {
        string fileName = Path.GetFileName(sourceFilePath);
        Windows.Storage.StorageFile sourceStorageFile = Crosstales.FB.FileBrowserWSAImpl.LastOpenFile;
        Windows.Storage.StorageFolder cacheStorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
        var cacheStorageFile =
 await sourceStorageFile.CopyAsync(cacheStorageFolder, fileName,  Windows.Storage.NameCollisionOption.ReplaceExisting);
        string cacheFilePath = cacheStorageFile.Path;
        return cacheFilePath;
    }
#elif UNITY_STANDALONE
        public static string CacheFile(string sourceFilePath)
        {
            string fileName = Path.GetFileName(sourceFilePath);
            string cacheFilePath =
                Path.Combine(Application.temporaryCachePath, fileName ?? throw new FileNotFoundException());
            File.Copy(sourceFilePath, cacheFilePath);
            return cacheFilePath;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    public static void ExtractAndroidStreamingAssets(string targetPath)
    {
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        if (targetPath[targetPath.Length - 1] != '/' || targetPath[targetPath.Length - 1] != '\\')
            targetPath += '/';

        HashSet<string> createdDirectories = new HashSet<string>();

        ZipFile zf = null;
        try
        {
            zf = new ZipFile(File.OpenRead(Application.dataPath));
            foreach (ZipEntry zipEntry in zf)
            {
                if (!zipEntry.IsFile)
                    continue;

                string name = zipEntry.Name;
                if (!name.StartsWith(AndroidStreamingAssetsDirectory) || name.EndsWith(MetaExtension) ||
                    name.StartsWith(AndroidStreamingAssetsInternalDataDirectory)) continue;

                name = name.Replace(AndroidStreamingAssetsDirectory, string.Empty);
                string relativeDir = Path.GetDirectoryName(name);
                if (!createdDirectories.Contains(relativeDir))
                {
                    Directory.CreateDirectory(targetPath + relativeDir);
                    createdDirectories.Add(relativeDir);
                }

                byte[] buffer = new byte[4096];
                using (Stream zipStream = zf.GetInputStream(zipEntry))
                {
                    using (FileStream streamWriter = File.Create(targetPath + name))
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            if (zf != null)
            {
                zf.IsStreamOwner = true;
                zf.Close();
            }
        }
    }
#endif

        public static void ExtractZip(string zipPath, string targetDir)
        {
            if (!File.Exists(zipPath))
                return;

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            var fastZip = new FastZip();
            fastZip.ExtractZip(zipPath, targetDir, null);
        }

        public static void UnwrapFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string fileContents = File.ReadAllText(filePath);
            string unwrappedContent = string.Concat(fileContents.Skip(1).Take(fileContents.Length - 2));
            File.WriteAllText(filePath, unwrappedContent);
        }

        public static IEnumerator SaveUrlToFile(string url, string filePath, string postJsonBody = null,
            Dictionary<string, string> responseHeaders = null)
        {
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Debug.LogWarning("SaveUrlToFile::UrlInvalid:" + url + "," + filePath);
                yield break;
            }

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("SaveUrlToFile::FilepathInvalid:" + url + "," + filePath);
                yield break;
            }

            using (UnityWebRequest www =
                (postJsonBody == null ? UnityWebRequest.Get(url) : new UnityWebRequest(url, "POST")))
            {
                if (postJsonBody != null)
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(postJsonBody);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/json");
                }

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(www.error))
                {
                    Debug.LogWarning("SaveUrlToFile::www.error:" + www.responseCode + " " + www.error);
                    yield break;
                }

                if (responseHeaders != null)
                    foreach (KeyValuePair<string, string> responseHeader in www.GetResponseHeaders())
                        responseHeaders.Add(responseHeader.Key, responseHeader.Value);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllBytes(directory + "/" + fileName, www.downloadHandler.data);
            }
        }

        public static IEnumerator RunOutputCoroutine<T>(IEnumerator coroutine, Action<T> output) where T : class
        {
            if (coroutine == null || output == null)
            {
                Debug.LogWarning("RunOutputCoroutine::ParamsInvalid:" + coroutine + "," + output);
                yield break;
            }

            object result = null;
            while (coroutine.MoveNext())
            {
                result = coroutine.Current;
                yield return result;
            }

            output(result as T);
        }

        // Note: Memory Leak Potential
        public static IEnumerator CreateAndOutputSpriteFromImageFile(string imageFilePath, string backUpImageUrl)
        {
            if (!File.Exists(imageFilePath))
                yield return SaveUrlToFile(backUpImageUrl, imageFilePath);

            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(FilePrefix + imageFilePath))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(www.error))
                {
                    Debug.LogWarning("CreateAndOutputSpriteFromImageFile::www.Error:" + www.error);
                    yield return null;
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture) www.downloadHandler).texture;
                    yield return CreateSprite(texture);
                }
            }
        }

        // Note: Memory Leak Potential
        public static IEnumerator CreateAndOutputSpriteFromImageFile(string imageUrl)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(www.error))
                {
                    Debug.LogWarning("CreateAndOutputSpriteFromImageFile::www.Error:" + www.error);
                    yield return null;
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture) www.downloadHandler).texture;
                    yield return CreateSprite(texture);
                }
            }
        }

        // Note: Memory Leak Potential
        public static Sprite CreateSprite(string textureFilePath)
        {
            if (!File.Exists(textureFilePath))
            {
                Debug.LogWarning("CreateSprite::TextureFileMissing");
                return null;
            }

            var texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(File.ReadAllBytes(textureFilePath));
            return CreateSprite(texture2D);
        }

        // Note: Memory Leak Potential
        public static Sprite CreateSprite(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("CreateSprite::TextureNull");
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
