using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base for various helper functions.</summary>
   public abstract class BaseHelper
   {
      #region Variables

      public static readonly System.Globalization.CultureInfo BaseCulture =
         new System.Globalization.CultureInfo("en-US"); //TODO set with current user locale?

      protected static readonly System.Text.RegularExpressions.Regex lineEndingsRegex =
         new System.Text.RegularExpressions.Regex(@"\r\n|\r|\n");

      //protected static readonly Regex cleanStringRegex = new Regex(@"([^a-zA-Z0-9 ]|[ ]{2,})");
      protected static readonly System.Text.RegularExpressions.Regex cleanSpacesRegex =
         new System.Text.RegularExpressions.Regex(@"\s+");

      protected static readonly System.Text.RegularExpressions.Regex cleanTagsRegex =
         new System.Text.RegularExpressions.Regex(@"<.*?>");
      //protected static readonly System.Text.RegularExpressions.Regex asciiOnlyRegex = new System.Text.RegularExpressions.Regex(@"[^\u0000-\u00FF]+");

      protected static readonly System.Random rnd = new System.Random();

      protected const string file_prefix = "file://";

      #endregion


      #region Properties

      /// <summary>Checks if an Internet connection is available.</summary>
      /// <returns>True if an Internet connection is available.</returns>
      public static bool isInternetAvailable
      {
         get
         {
#if CT_OC
                return OnlineCheck.OnlineCheck.isInternetAvailable;
#else
            return Application.internetReachability != NetworkReachability.NotReachable;
#endif
         }
      }

      /// <summary>Checks if the current platform is Windows.</summary>
      /// <returns>True if the current platform is Windows.</returns>
      public static bool isWindowsPlatform
      {
         get
         {
#if UNITY_STANDALONE_WIN
            return true;
#else
                return false;
#endif
            //return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
         }
      }

      /// <summary>Checks if the current platform is OSX.</summary>
      /// <returns>True if the current platform is OSX.</returns>
      public static bool isMacOSPlatform
      {
         get
         {
#if UNITY_STANDALONE_OSX
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
         }
      }

      /// <summary>Checks if the current platform is Linux.</summary>
      /// <returns>True if the current platform is Linux.</returns>
      public static bool isLinuxPlatform
      {
         get
         {
#if UNITY_STANDALONE_LINUX
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor;
         }
      }

      /// <summary>Checks if the current platform is standalone (Windows, macOS or Linux).</summary>
      /// <returns>True if the current platform is standalone (Windows, macOS or Linux).</returns>
      public static bool isStandalonePlatform
      {
         get { return isWindowsPlatform || isMacOSPlatform || isLinuxPlatform; }
      }

      /// <summary>Checks if the current platform is Android.</summary>
      /// <returns>True if the current platform is Android.</returns>
      public static bool isAndroidPlatform
      {
         get
         {
#if UNITY_ANDROID
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.Android;
         }
      }

      /// <summary>Checks if the current platform is iOS.</summary>
      /// <returns>True if the current platform is iOS.</returns>
      public static bool isIOSPlatform
      {
         get
         {
#if UNITY_IOS
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.IPhonePlayer;
         }
      }

      /// <summary>Checks if the current platform is tvOS.</summary>
      /// <returns>True if the current platform is tvOS.</returns>
      public static bool isTvOSPlatform
      {
         get
         {
#if UNITY_IOS
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.tvOS;
         }
      }

      /// <summary>Checks if the current platform is WSA.</summary>
      /// <returns>True if the current platform is WSA.</returns>
      public static bool isWSAPlatform
      {
         get
         {
#if UNITY_WSA
                return true;
#else
            return false;
#endif
            /*
            return Application.platform == RuntimePlatform.WSAPlayerARM ||
                Application.platform == RuntimePlatform.WSAPlayerX86 ||
                Application.platform == RuntimePlatform.WSAPlayerX64;
                */
         }
      }

      /// <summary>Checks if the current platform is XboxOne.</summary>
      /// <returns>True if the current platform is XboxOne.</returns>
      public static bool isXboxOnePlatform
      {
         get
         {
#if UNITY_XBOXONE
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.XboxOne;
         }
      }

      /// <summary>Checks if the current platform is PS4.</summary>
      /// <returns>True if the current platform is PS4.</returns>
      public static bool isPS4Platform
      {
         get
         {
#if UNITY_PS4
                return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is WebGL.</summary>
      /// <returns>True if the current platform is WebGL.</returns>
      public static bool isWebGLPlatform
      {
         get
         {
#if UNITY_WEBGL
                return true;
#else
            return false;
#endif
            //return Application.platform == RuntimePlatform.WebGLPlayer;
         }
      }

      /// <summary>Checks if the current platform is Web (WebPlayer or WebGL).</summary>
      /// <returns>True if the current platform is Web (WebPlayer or WebGL).</returns>
      public static bool isWebPlatform
      {
         get { return isWebGLPlatform; }
      }

      /// <summary>Checks if the current platform is Windows-based (Windows standalone, WSA or XboxOne).</summary>
      /// <returns>True if the current platform is Windows-based (Windows standalone, WSA or XboxOne).</returns>
      public static bool isWindowsBasedPlatform
      {
         get { return isWindowsPlatform || isWSAPlatform || isXboxOnePlatform; }
      }

      /// <summary>Checks if the current platform is WSA-based (WSA or XboxOne).</summary>
      /// <returns>True if the current platform is WSA-based (WSA or XboxOne).</returns>
      public static bool isWSABasedPlatform
      {
         get { return isWSAPlatform || isXboxOnePlatform; }
      }

      /// <summary>Checks if the current platform is Apple-based (macOS standalone, iOS or tvOS).</summary>
      /// <returns>True if the current platform is Apple-based (macOS standalone, iOS or tvOS).</returns>
      public static bool isAppleBasedPlatform
      {
         get { return isMacOSPlatform || isIOSPlatform || isTvOSPlatform; }
      }

      /// <summary>Checks if the current platform is iOS-based (iOS or tvOS).</summary>
      /// <returns>True if the current platform is iOS-based (iOS or tvOS).</returns>
      public static bool isIOSBasedPlatform
      {
         get { return isIOSPlatform || isTvOSPlatform; }
      }

      /// <summary>Checks if we are inside the Editor.</summary>
      /// <returns>True if we are inside the Editor.</returns>
      public static bool isEditor
      {
         get { return isWindowsEditor || isMacOSEditor || isLinuxEditor; }
      }

      /// <summary>Checks if we are inside the Windows Editor.</summary>
      /// <returns>True if we are inside the Windows Editor.</returns>
      public static bool isWindowsEditor
      {
         get
         {
#if UNITY_EDITOR_WIN
            return true;
#else
                return false;
#endif
         }
      }

      /// <summary>Checks if we are inside the macOS Editor.</summary>
      /// <returns>True if we are inside the macOS Editor.</returns>
      public static bool isMacOSEditor
      {
         get
         {
#if UNITY_EDITOR_OSX
                return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if we are inside the Linux Editor.</summary>
      /// <returns>True if we are inside the Linux Editor.</returns>
      public static bool isLinuxEditor
      {
         get
         {
#if UNITY_EDITOR_LINUX
                return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if we are in Editor mode.</summary>
      /// <returns>True if in Editor mode.</returns>
      public static bool isEditorMode
      {
         get { return isEditor && !Application.isPlaying; }
      }

      /// <summary>Checks if the current build target uses IL2CPP.</summary>
      /// <returns>True if the current build target uses IL2CPP.</returns>
      public static bool isIL2CPP
      {
         get
         {
#if UNITY_EDITOR
            UnityEditor.BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildTargetGroup group = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);

            return UnityEditor.PlayerSettings.GetScriptingBackend(group) == UnityEditor.ScriptingImplementation.IL2CPP;
#else
#if ENABLE_IL2CPP
            return true;
#else
            return false;
#endif
#endif
         }
      }

      /// <summary>Returns the current platform.</summary>
      /// <returns>The current platform.</returns>
      public static Model.Enum.Platform CurrentPlatform
      {
         get
         {
            if (isWindowsPlatform)
            {
               return Model.Enum.Platform.Windows;
            }

            if (isMacOSPlatform)
            {
               return Model.Enum.Platform.OSX;
            }

            if (isLinuxPlatform)
            {
               return Model.Enum.Platform.Linux;
            }

            if (isAndroidPlatform)
            {
               return Model.Enum.Platform.Android;
            }

            if (isIOSBasedPlatform)
            {
               return Model.Enum.Platform.IOS;
            }

            if (isWSABasedPlatform)
            {
               return Model.Enum.Platform.WSA;
            }

            return isWebPlatform ? Model.Enum.Platform.Web : Model.Enum.Platform.Unsupported;
         }
      }

      /// <summary>Returns the path to the the "Streaming Assets".</summary>
      /// <returns>The path to the the "Streaming Assets".</returns>
      public static string StreamingAssetsPath
      {
         get
         {
            if (isAndroidPlatform && !isEditor)
            {
               return "jar:file://" + Application.dataPath + "!/assets/";
            }

            if (isIOSBasedPlatform && !isEditor)
            {
               return Application.dataPath + "/Raw/";
            }

            return Application.dataPath + "/StreamingAssets/";
         }
      }

      #endregion


      #region Public methods

      /// <summary>Creates a string of characters with a given length.</summary>
      /// <param name="replaceChars">Characters to generate the string (if more than one character is used, the generated string will be a randomized result of all characters)</param>
      /// <param name="stringLength">Length of the generated string</param>
      /// <returns>Generated string</returns>
      public static string CreateString(string replaceChars, int stringLength)
      {
         if (replaceChars.Length > 1)
         {
            char[] chars = new char[stringLength];

            for (int ii = 0; ii < stringLength; ii++)
            {
               chars[ii] = replaceChars[rnd.Next(0, replaceChars.Length)];
            }

            return new string(chars);
         }

         return replaceChars.Length == 1 ? new string(replaceChars[0], stringLength) : string.Empty;
      }

      /// <summary>Determines if an AudioSource has an active clip.</summary>
      /// <param name="source">AudioSource to check.</param>
      /// <returns>True if the AudioSource has an active clip.</returns>
      public static bool hasActiveClip(AudioSource source)
      {
         /*
         Debug.Log("source.clip: " + source.clip);
         Debug.Log("source.loop: " + source.loop);
         Debug.Log("source.isPlaying: " + source.isPlaying);
         Debug.Log("source.timeSamples: " + source.timeSamples);
         Debug.Log("source.clip.samples: " + source.clip.samples);
         */

         int timeSamples;
         return source != null && source.clip != null &&
                (!source.loop && (timeSamples = source.timeSamples) > 0 && timeSamples < source.clip.samples - 256 ||
                 source.loop ||
                 source.isPlaying);
      }

#if !UNITY_WSA || UNITY_EDITOR
      /// <summary>HTTPS-certification callback.</summary>
      public static bool RemoteCertificateValidationCallback(System.Object sender,
         System.Security.Cryptography.X509Certificates.X509Certificate certificate,
         System.Security.Cryptography.X509Certificates.X509Chain chain,
         System.Net.Security.SslPolicyErrors sslPolicyErrors)
      {
         bool isOk = true;

         // If there are errors in the certificate chain, look at each error to determine the cause.
         if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
         {
            foreach (var t in chain.ChainStatus.Where(t =>
               t.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags
                  .RevocationStatusUnknown))
            {
               chain.ChainPolicy.RevocationFlag =
                  System.Security.Cryptography.X509Certificates.X509RevocationFlag.EntireChain;
               chain.ChainPolicy.RevocationMode =
                  System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
               chain.ChainPolicy.UrlRetrievalTimeout = new System.TimeSpan(0, 1, 0);
               chain.ChainPolicy.VerificationFlags =
                  System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllFlags;

               isOk = chain.Build((System.Security.Cryptography.X509Certificates.X509Certificate2)certificate);
            }
         }

         return isOk;
      }
#endif

      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <param name="addEndDelimiter">Add delimiter at the end of the path (optional, default: true)</param>
      /// <returns>Valid path</returns>
      public static string ValidatePath(string path, bool addEndDelimiter = true)
      {
         if (!string.IsNullOrEmpty(path))
         {
            string pathTemp = path.Trim();
            string result;

            if ((isWindowsBasedPlatform || isWindowsEditor) && !isMacOSEditor && !isLinuxEditor)
            {
               result = pathTemp.Replace('/', '\\');

               if (addEndDelimiter)
               {
                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                     result += BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
               }
            }
            else
            {
               result = pathTemp.Replace('\\', '/');

               if (addEndDelimiter)
               {
                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
                  {
                     result += BaseConstants.PATH_DELIMITER_UNIX;
                  }
               }
            }

            return string.Join(string.Empty, result.Split(System.IO.Path.GetInvalidPathChars()));
         }

         return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidateFile(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            string result = ValidatePath(path);

            if (result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS) ||
                result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            string fileName;
            if ((isWindowsBasedPlatform || isWindowsEditor) && !isMacOSEditor && !isLinuxEditor)
            {
               fileName = result.Substring(result.LastIndexOf(BaseConstants.PATH_DELIMITER_WINDOWS) + 1);
            }
            else
            {
               fileName = result.Substring(result.LastIndexOf(BaseConstants.PATH_DELIMITER_UNIX) + 1);
            }

            string newName =
               string.Join(string.Empty,
                  fileName.Split(System.IO.Path
                     .GetInvalidFileNameChars())); //.Replace(BaseConstants.PATH_DELIMITER_WINDOWS, string.Empty).Replace(BaseConstants.PATH_DELIMITER_UNIX, string.Empty);

            return result.Substring(0, result.Length - fileName.Length) + newName;
         }

         return path;
      }

      /// <summary>
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="extensions">Extensions for the file search, e.g. "png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         if (isWebPlatform && !isEditor)
         {
            Debug.LogWarning("'GetFiles' is not supported for the current platform!");
         }
         else if (isWSABasedPlatform && !isEditor)
         {
#if CT_FB_PRO
#if UNITY_WSA && !UNITY_EDITOR
             Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
             fbWsa.isBusy = true;
             UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetFiles(path, isRecursive, extensions); }, false);

             do
             {
                 //wait
             } while (fbWsa.isBusy);

             return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning(
               "'GetFiles' under UWP (WSA) is supported in 'File Browser PRO'. For more, please see: " +
               BaseConstants.ASSET_FB);
#endif
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               try
               {
                  string _path = ValidatePath(path);

                  if (extensions == null || extensions.Length == 0 || extensions.Any(extension => extension.Equals("*") || extension.Equals("*.*")))
                  {
#if NET_4_6 || NET_STANDARD_2_0
                     return System.IO.Directory.EnumerateFiles(_path, "*", isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly).ToArray();
#else
                     return System.IO.Directory.GetFiles(_path, "*",
                        isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly);
#endif
                  }

                  System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();

                  foreach (string extension in extensions)
                  {
#if NET_4_6 || NET_STANDARD_2_0
                     files.AddRange(System.IO.Directory.EnumerateFiles(_path, "*." + extension, isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly));
#else
                     files.AddRange(System.IO.Directory.GetFiles(_path, "*." + extension,
                        isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly));
#endif
                  }

                  return files.OrderBy(q => q).ToArray();
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning("Could not scan the path for files: " + ex);
               }
            }
         }

         return new string[0];
      }

      /// <summary>
      /// Find directories inside.
      /// </summary>
      /// <param name="path">Path to find the directories</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
      public static string[] GetDirectories(string path, bool isRecursive = false)
      {
         if (isWebPlatform && !isEditor)
         {
            Debug.LogWarning("'GetDirectories' is not supported for the current platform!");
         }
         else if (isWSABasedPlatform && !isEditor)
         {
#if CT_FB_PRO
#if UNITY_WSA && !UNITY_EDITOR
                Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
                fbWsa.isBusy = true;
                UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetDirectories(path, isRecursive); }, false);

                do
                {
                    //wait
                } while (fbWsa.isBusy);

                return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning(
               "'GetDirectories' under UWP (WSA) is supported in 'File Browser PRO'. For more, please see: " +
               BaseConstants.ASSET_FB);
#endif
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               try
               {
                  string _path = ValidatePath(path);
#if NET_4_6 || NET_STANDARD_2_0
                  return System.IO.Directory.EnumerateDirectories(_path, "*", isRecursive
                     ? System.IO.SearchOption.AllDirectories
                     : System.IO.SearchOption.TopDirectoryOnly).ToArray();
#else
                  return System.IO.Directory.GetDirectories(_path, "*",
                     isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly);
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning("Could not scan the path for directories: " + ex);
               }
            }
         }

         return new string[0];
      }

      /*
      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <returns>Valid path</returns>
      public static string ValidPath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {
              string pathTemp = path.Trim();
              string result = null;

              if (isWindowsPlatform)
              {
                  result = pathTemp.Replace('/', '\\');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                      result += BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
              }
              else
              {
                  result = pathTemp.Replace('\\', '/');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
                  {
                      result += BaseConstants.PATH_DELIMITER_UNIX;
                  }
              }

              return result;
          }

          return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidFilePath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {

              string result = ValidPath(path);

              if (result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS) || result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
              {
                  result = result.Substring(0, result.Length - 1);
              }

              return result;
          }

          return path;
      }
      */

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidURLFromFilePath(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (!isValidURL(path))
            {
               return BaseConstants.PREFIX_FILE + ValidateFile(path).Replace(" ", "%20").Replace('\\', '/');
            }

            return ValidateFile(path).Replace(" ", "%20").Replace('\\', '/');
         }

         return path;

         //return System.Uri.EscapeDataString(path);
      }

      /// <summary>Cleans a given URL.</summary>
      /// <param name="url">URL to clean</param>
      /// <param name="removeProtocol">Remove the protocol, e.g. http:// (default: true, optional).</param>
      /// <param name="removeWWW">Remove www (default: true, optional).</param>
      /// <param name="removeSlash">Remove slash at the end (default: true, optional)</param>
      /// <returns>Clean URL</returns>
      public static string CleanUrl(string url, bool removeProtocol = true, bool removeWWW = true,
         bool removeSlash = true)
      {
         string result = url.Trim();

         if (!string.IsNullOrEmpty(url))
         {
            if (removeProtocol)
            {
               result = result.Substring(result.IndexOf("//") + 2);
            }

            if (removeWWW)
            {
               result = result.CTReplace("www.", string.Empty);
            }

            if (removeSlash && result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            /*
            if (urlTemp.StartsWith("http://"))
            {
                result = urlTemp.Substring(7);
            }
            else if (urlTemp.StartsWith("https://"))
            {
                result = urlTemp.Substring(8);
            }
            else
            {
                result = urlTemp;
            }

            if (result.StartsWith("www."))
            {
                result = result.Substring(4);
            }
            */
         }

         return result;
      }

      /// <summary>Cleans a given text from tags.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without tags.</returns>
      public static string ClearTags(string text)
      {
         return cleanTagsRegex.Replace(text, string.Empty).Trim();
      }

      /// <summary>Cleans a given text from multiple spaces.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without multiple spaces.</returns>
      public static string ClearSpaces(string text)
      {
         return cleanSpacesRegex.Replace(text, " ").Trim();
      }

      /// <summary>Cleans a given text from line endings.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without line endings.</returns>
      public static string ClearLineEndings(string text)
      {
         return lineEndingsRegex.Replace(text, string.Empty).Trim();
      }

      /// <summary>Split the given text to lines and return it as list.</summary>
      /// <param name="text">Complete text fragment</param>
      /// <param name="ignoreCommentedLines">Ignore commente lines (default: true, optional)</param>
      /// <param name="skipHeaderLines">Number of skipped header lines (default: 0, optional)</param>
      /// <param name="skipFooterLines">Number of skipped footer lines (default: 0, optional)</param>
      /// <returns>Splitted lines as array</returns>
      public static System.Collections.Generic.List<string> SplitStringToLines(string text,
         bool ignoreCommentedLines = true, int skipHeaderLines = 0, int skipFooterLines = 0)
      {
         System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>(100);

         if (string.IsNullOrEmpty(text))
         {
            Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine +
                             "=> 'SplitStringToLines()' will return an empty string list.");
         }
         else
         {
            string[] lines = lineEndingsRegex.Split(text);

            for (int ii = 0; ii < lines.Length; ii++)
            {
               if (ii + 1 > skipHeaderLines && ii < lines.Length - skipFooterLines)
               {
                  if (!string.IsNullOrEmpty(lines[ii]))
                  {
                     if (ignoreCommentedLines)
                     {
                        if (!lines[ii].StartsWith("#"))
                        {
                           //valid and not disabled line?
                           result.Add(lines[ii]);
                        }
                     }
                     else
                     {
                        result.Add(lines[ii]);
                     }
                  }
               }
            }
         }

         return result;
      }

      /// <summary>Format byte-value to Human-Readable-Form.</summary>
      /// <returns>Formatted byte-value in Human-Readable-Form.</returns>
      public static string FormatBytesToHRF(long bytes)
      {
         string[] sizes = {"B", "KB", "MB", "GB", "TB"};
         double len = bytes;
         int order = 0;
         while (len >= 1024 && order < sizes.Length - 1)
         {
            order++;
            len /= 1024;
         }

         // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
         // show a single decimal place, and no space.
         return string.Format("{0:0.##} {1}", len, sizes[order]);
      }

      /// <summary>Format seconds to Human-Readable-Form.</summary>
      /// <returns>Formatted seconds in Human-Readable-Form.</returns>
      public static string FormatSecondsToHourMinSec(double seconds)
      {
         int totalSeconds = (int)seconds;
         int calcSeconds = totalSeconds % 60;

         if (seconds >= 86400)
         {
            int calcDays = totalSeconds / 86400;
            int calcHours = (totalSeconds -= calcDays * 86400) / 3600;
            int calcMinutes = (totalSeconds - calcHours * 3600) / 60;

            return calcDays + "d " + calcHours + ":" +
                   (calcMinutes < 10 ? "0" + calcMinutes : calcMinutes.ToString()) + ":" +
                   (calcSeconds < 10 ? "0" + calcSeconds : calcSeconds.ToString());
         }

         if (seconds >= 3600)
         {
            int calcHours = totalSeconds / 3600;
            int calcMinutes = (totalSeconds - calcHours * 3600) / 60;

            return calcHours + ":" + (calcMinutes < 10 ? "0" + calcMinutes : calcMinutes.ToString()) + ":" +
                   (calcSeconds < 10 ? "0" + calcSeconds : calcSeconds.ToString());
         }
         else
         {
            int calcMinutes = totalSeconds / 60;

            return calcMinutes + ":" + (calcSeconds < 10 ? "0" + calcSeconds : calcSeconds.ToString());
         }
      }

      /// <summary>
      /// Generate nice HSV colors.
      /// Based on https://gist.github.com/rje/6206099
      /// </summary>
      /// <param name="h">Hue</param>
      /// <param name="s">Saturation</param>
      /// <param name="v">Value</param>
      /// <param name="a">Alpha (optional)</param>
      /// <returns>True if the current platform is supported.</returns>
      public static Color HSVToRGB(float h, float s, float v, float a = 1f)
      {
         if (Mathf.Abs(s) < BaseConstants.FLOAT_TOLERANCE)
         {
            return new Color(v, v, v, a);
         }

         h /= 60f;
         int sector = Mathf.FloorToInt(h);
         float fact = h - sector;
         float p = v * (1f - s);
         float q = v * (1f - s * fact);
         float t = v * (1f - s * (1f - fact));

         switch (sector)
         {
            case 0:
               return new Color(v, t, p, a);
            case 1:
               return new Color(q, v, p, a);
            case 2:
               return new Color(p, v, t, a);
            case 3:
               return new Color(p, q, v, a);
            case 4:
               return new Color(t, p, v, a);
            default:
               return new Color(v, p, q, a);
         }
      }

      /// <summary>Checks if the URL is valid.</summary>
      /// <param name="url">URL to check</param>
      /// <returns>True if the URL is valid.</returns>
      public static bool isValidURL(string url)
      {
         return !string.IsNullOrEmpty(url) &&
                (url.StartsWith(file_prefix, System.StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith(BaseConstants.PREFIX_HTTP, System.StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith(BaseConstants.PREFIX_HTTPS, System.StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>Copy or move a file.</summary>
      /// <param name="inputFile">Input file path</param>
      /// <param name="outputFile">Output file path</param>
      /// <param name="move">Move file instead of copy (default: false, optional)</param>
      public static void FileCopy(string inputFile, string outputFile, bool move = false)
      {
         if ((isWSABasedPlatform || isWebPlatform) && !isEditor)
         {
            Debug.LogWarning("'FileCopy' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(outputFile))
            {
               try
               {
                  if (!System.IO.File.Exists(inputFile))
                  {
                     Debug.LogError("Input file does not exists: " + inputFile);
                  }
                  else
                  {
                     System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputFile));

                     if (System.IO.File.Exists(outputFile))
                     {
                        if (BaseConstants.DEV_DEBUG)
                           Debug.LogWarning("Overwrite output file: " + outputFile);

                        System.IO.File.Delete(outputFile);
                     }

                     if (move)
                     {
#if UNITY_STANDALONE || UNITY_EDITOR
                        System.IO.File.Move(inputFile, outputFile);
#else
                         System.IO.File.Copy(inputFile, outputFile);
                         System.IO.File.Delete(inputFile);
#endif
                     }
                     else
                     {
                        System.IO.File.Copy(inputFile, outputFile);
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogError("Could not copy file!" + System.Environment.NewLine + ex);
               }
            }
         }
      }

      /// <summary>
      /// Shows the location of a path or file in OS file explorer.
      /// NOTE: only works for standalone platforms
      /// </summary>
      /// <param name="file">File path</param>
      public static void ShowFileLocation(string file)
      {
         if (isStandalonePlatform || isEditor)
         {
#if UNITY_STANDALONE || UNITY_EDITOR
            string path;

            if (string.IsNullOrEmpty(file) || file.Equals("."))
            {
               path = ".";
            }
            else if ((isWindowsPlatform || isWindowsEditor) && file.Length < 4)
            {
               path = file; //root directory
            }
            else
            {
               path = ValidatePath(System.IO.Path.GetDirectoryName(file));
            }

            //Debug.Log("'" + path + "'");

            if (System.IO.Directory.Exists(path))
            {
#if ENABLE_IL2CPP
                 using (CTProcess process = new CTProcess())
                 {
                     if (isWindowsPlatform || isWindowsEditor)
                     {
                         process.StartInfo.FileName = "explorer.exe";
                         process.StartInfo.Arguments = "\"" + path + "\"";
                         process.StartInfo.UseCmdExecute = true;
                         process.StartInfo.CreateNoWindow = true;
                     }
                     else if (isMacOSPlatform || isMacOSEditor)
                     {
                         process.StartInfo.FileName = "open";
                         process.StartInfo.Arguments = "\"" + path + "\"";
                     }
                     else
                     {
                         process.StartInfo.FileName = "xdg-open";
                         process.StartInfo.Arguments = "\"" + path + "\"";
                     }

                     process.Start();
                 }
#else
               System.Diagnostics.Process.Start(path);
#endif
            }
            else
            {
               Debug.LogWarning("Path to file doesn't exist: " + path);
            }
#endif
         }
         else
         {
            Debug.LogWarning("'ShowFileLocation' is not supported on the current platform!");
         }
      }

      /// <summary>
      /// Opens a file with the OS default application.
      /// NOTE: only works for standalone platforms
      /// </summary>
      /// <param name="file">File path</param>
      public static void OpenFile(string file)
      {
         if (isStandalonePlatform || isEditor)
         {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (System.IO.File.Exists(file))
            {
#if ENABLE_IL2CPP
                 using (CTProcess process = new CTProcess())
                 {
                     if (isWindowsPlatform || isWindowsEditor)
                     {
                         process.StartInfo.FileName = "explorer.exe";
                         process.StartInfo.Arguments = "\"" + file + "\"";
                         process.StartInfo.UseCmdExecute = true;
                         process.StartInfo.CreateNoWindow = true;
                     }
                     else if (isMacOSPlatform || isMacOSEditor)
                     {
                         process.StartInfo.FileName = "open";
                         process.StartInfo.Arguments = "\"" + file + "\"";
                     }
                     else
                     {
                         process.StartInfo.FileName = "xdg-open";
                         process.StartInfo.Arguments = "\"" + file + "\"";
                     }

                     process.Start();
                 }
#else
               using (System.Diagnostics.Process process = new System.Diagnostics.Process())
               {
                  if (isMacOSPlatform || isMacOSEditor)
                  {
                     process.StartInfo.FileName = "open";
                     process.StartInfo.WorkingDirectory =
                        System.IO.Path.GetDirectoryName(file) + BaseConstants.PATH_DELIMITER_UNIX;
                     process.StartInfo.Arguments = "-t " + System.IO.Path.GetFileName(file);
                  }
                  else if (isLinuxPlatform || isLinuxEditor)
                  {
                     process.StartInfo.FileName = "xdg-open";
                     process.StartInfo.WorkingDirectory =
                        System.IO.Path.GetDirectoryName(file) + BaseConstants.PATH_DELIMITER_UNIX;
                     process.StartInfo.Arguments = System.IO.Path.GetFileName(file);
                  }
                  else
                  {
                     process.StartInfo.FileName = file;
                  }

                  process.Start();
               }
#endif
            }
            else
            {
               Debug.LogWarning("File doesn't exist: " + file);
            }
#endif
         }
         else
         {
            Debug.LogWarning("'OpenFile' is not supported on the current platform!");
         }
      }

      /// <summary>Returns the IP of a given host name.</summary>
      /// <param name="host">Host name</param>
      /// <returns>IP of a given host name.</returns>
      public static string getIP(string host)
      {
         if (!string.IsNullOrEmpty(host))
         {
#if !UNITY_WSA && !UNITY_WEBGL
            try
            {
               return System.Net.Dns.GetHostAddresses(host)[0].ToString();
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning("Could not resolve host '" + host + "': " + ex);
            }
#else
			    Debug.LogWarning("'getIP' doesn't work in WebGL or WSA! Returning original string.");
#endif
         }
         else
         {
            Debug.LogWarning("Host name is null or empty - can't resolve to IP!");
         }

         return host;
      }

      // StringHelper
      /*
      public static byte[] GetBytesFromText(string text) {
       return new UnicodeEncoding().GetBytes(text);
    }

    public static string GetTextFromBytes(byte[] bytes) {
       return new UnicodeEncoding().GetString(bytes, 0, bytes.Length);
    }

    public static byte[] GetBytesFromBase64(string text) {
       return Convert.FromBase64String(text);
    }

    public static string GetBase64FromBytes(byte[] bytes) {
       return Convert.ToBase64String(bytes);
    }
      */


      // MathHelper
      /*
      public static bool IsInRange(float actValue, float refValue, float range) {
       
       return (actValue >= refValue-range) && (actValue <= refValue+range);
    }


    public static bool IsInRange(int actValue, int refValue, int range) {
       
       return (actValue >= refValue-range) && (actValue <= refValue+range);
    }
      */


      // Add Math3dHelper?


      // Color Helper
      /*
      public static string ColorToHex(Color32 color)
      {
          //			if (color == null)
          //				throw new ArgumentNullException("color");

          string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
          return hex;
      }

      public static Color HexToColor(string hex)
      {
          if (string.IsNullOrEmpty(hex))
              throw new ArgumentNullException("hex");

          byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
          byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
          byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
          return new Color32(r, g, b, 255);
      }
      */

      #endregion
   }
}
// © 2015-2020 crosstales LLC (https://www.crosstales.com)