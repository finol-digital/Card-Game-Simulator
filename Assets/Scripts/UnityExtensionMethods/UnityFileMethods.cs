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

namespace UnityExtensionMethods
{
    public static class UnityFileMethods
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public const string AndroidStreamingAssetsDirectory = "assets/";
        public const string AndroidStreamingAssetsInternalDataDirectory = "assets/bin/";
#endif
        private const string DirectorySeparator = "/";
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

        private static string ValidatePath(string path, bool addEndDelimiter = true)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var pathTemp = path.Trim();

            // ReSharper disable once JoinDeclarationAndInitializer
            string result;
#if UNITY_STANDALONE_WIN
            result = pathTemp.Replace('/', '\\');
            if (addEndDelimiter && !result.EndsWith("\\"))
                result += "\\";
#else
            result = pathTemp.Replace('\\', '/');
            if (addEndDelimiter && !result.EndsWith("/"))
                result += "/";
#endif

            return string.Join(string.Empty, result.Split(Path.GetInvalidPathChars()));
        }

        public static string ValidateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var result = ValidatePath(path);

            if (result.EndsWith("\\") || result.EndsWith("/"))
                result = result[..^1];

            // ReSharper disable once JoinDeclarationAndInitializer
            string fileName;
#if UNITY_STANDALONE_WIN
            fileName = result[(result.LastIndexOf('\\') + 1)..];
#else
            fileName = result[(result.LastIndexOf('/') + 1)..];
#endif

            var newName = string.Join(string.Empty, fileName.Split(Path.GetInvalidFileNameChars()));
            return result[..^fileName.Length] + newName;
        }

        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
                if (!filePath.EndsWith(MetaExtension))
                    File.Copy(filePath, Path.Combine(targetDir, Path.GetFileName(filePath)));

            foreach (var directory in Directory.GetDirectories(sourceDir))
                if (!string.IsNullOrEmpty(directory))
                    CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

#if ENABLE_WINMD_SUPPORT
        public static async System.Threading.Tasks.Task<string> CacheFileAsync(string sourceFilePath)
        {
            string fileName = Path.GetFileName(sourceFilePath);
            Windows.Storage.StorageFile sourceStorageFile = UwpFileBrowser.LastOpenFile;
            Windows.Storage.StorageFolder cacheStorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            var cacheStorageFile = await sourceStorageFile.CopyAsync(
                cacheStorageFolder, fileName,  Windows.Storage.NameCollisionOption.ReplaceExisting);
            string cacheFilePath = cacheStorageFile.Path;
            return cacheFilePath;
        }
#elif UNITY_STANDALONE
        public static string CacheFile(string sourceFilePath)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var cacheFilePath =
                Path.Combine(Application.temporaryCachePath, fileName ?? throw new FileNotFoundException());
            File.Copy(sourceFilePath, cacheFilePath, true);
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

        public static void CreateZip(string sourceDirectory, string targetDirectory, string zipFileName)
        {
            if (!Directory.Exists(sourceDirectory))
                return;

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            var zipFilePath = Path.Combine(targetDirectory, zipFileName);
            var fastZip = new FastZip
            {
                CreateEmptyDirectories = true
            };
            fastZip.CreateZip(zipFilePath, sourceDirectory, true, null);
        }

        public static void ExtractZip(string zipPath, string targetDirectory)
        {
            if (!File.Exists(zipPath))
                return;

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            var fastZip = new FastZip();
            fastZip.ExtractZip(zipPath, targetDirectory, null);
        }

        public static void UnwrapFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var fileContents = File.ReadAllText(filePath);
            var unwrappedContent = string.Concat(fileContents.Skip(1).Take(fileContents.Length - 2));
            File.WriteAllText(filePath, unwrappedContent);
        }

        public static IEnumerator SaveUrlToFile(string url, string filePath, string postJsonBody = null,
            Dictionary<string, string> headers = null)
        {
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Debug.LogWarning("SaveUrlToFile::UrlInvalid:" + url + "," + filePath);
                yield break;
            }

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("SaveUrlToFile::FilepathInvalid:" + url + "," + filePath);
                yield break;
            }

            var uriBuilder = url.StartsWith("http")
                ? new UriBuilder(url)
                {
                    Scheme = Uri.UriSchemeHttps, // enforce https over http
                    Port = -1 // default port for scheme
                }
                : new UriBuilder(url);
            var uri = uriBuilder.Uri;

            using var unityWebRequest =
                (postJsonBody == null ? UnityWebRequest.Get(uri) : new UnityWebRequest(uri, "POST"));
            if (postJsonBody != null)
            {
                var bytes = Encoding.UTF8.GetBytes(postJsonBody);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(bytes);
                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                unityWebRequest.SetRequestHeader("Content-Type", "application/json");
            }

            if (headers != null)
                foreach (var header in headers)
                    unityWebRequest.SetRequestHeader(header.Key, header.Value);

            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.result != UnityWebRequest.Result.Success ||
                !string.IsNullOrEmpty(unityWebRequest.error))
            {
                Debug.LogWarning("SaveUrlToFile::www.error:" + unityWebRequest.responseCode + " " +
                                 unityWebRequest.error + " " + unityWebRequest.url);
                yield break;
            }

            if (headers != null)
                foreach (var responseHeader in unityWebRequest.GetResponseHeaders())
                    headers.Add(responseHeader.Key, responseHeader.Value);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(directory + DirectorySeparator + fileName, unityWebRequest.downloadHandler.data);
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

            using var unityWebRequest = UnityWebRequestTexture.GetTexture(FilePrefix + imageFilePath);
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.result != UnityWebRequest.Result.Success ||
                !string.IsNullOrEmpty(unityWebRequest.error))
            {
                Debug.LogWarning("CreateAndOutputSpriteFromImageFile::www.Error:" + unityWebRequest.error + " " +
                                 unityWebRequest.url);
                yield return null;
            }
            else
            {
                var texture = ((DownloadHandlerTexture) unityWebRequest.downloadHandler).texture;
                yield return CreateSprite(texture);
            }
        }

        // Note: Memory Leak Potential
        public static IEnumerator CreateAndOutputSpriteFromImageFile(string imageUrl)
        {
            using var unityWebRequest = UnityWebRequestTexture.GetTexture(imageUrl);
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.result != UnityWebRequest.Result.Success ||
                !string.IsNullOrEmpty(unityWebRequest.error))
            {
                Debug.LogWarning("CreateAndOutputSpriteFromImageFile::www.Error:" + unityWebRequest.error + " " +
                                 unityWebRequest.url);
                yield return null;
            }
            else
            {
                var texture = ((DownloadHandlerTexture) unityWebRequest.downloadHandler).texture;
                yield return CreateSprite(texture);
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
        private static Sprite CreateSprite(Texture2D texture)
        {
            if (texture != null)
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            Debug.LogWarning("CreateSprite::TextureNull");
            return null;
        }
    }
}
