using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base for collected constants of very general utility for the asset.</summary>
   public abstract class BaseConstants
   {
      #region Constant variables

      /// <summary>Author of the asset.</summary>
      public const string ASSET_AUTHOR = "crosstales LLC";

      /// <summary>URL of the asset author.</summary>
      public const string ASSET_AUTHOR_URL = "https://www.crosstales.com";

      /// <summary>URL of the crosstales assets in UAS.</summary>
      public const string ASSET_CT_URL = "https://assetstore.unity.com/lists/crosstales-42213?aid=1011lNGT";

      /// <summary>URL of the crosstales Discord-channel.</summary>
      public const string ASSET_SOCIAL_DISCORD = "https://discord.gg/ZbZ2sh4";

      /// <summary>URL of the crosstales Facebook-profile.</summary>
      public const string ASSET_SOCIAL_FACEBOOK = "https://www.facebook.com/crosstales/";

      /// <summary>URL of the crosstales Twitter-profile.</summary>
      public const string ASSET_SOCIAL_TWITTER = "https://twitter.com/crosstales";

      /// <summary>URL of the crosstales Youtube-profile.</summary>
      public const string ASSET_SOCIAL_YOUTUBE = "https://www.youtube.com/c/Crosstales";

      /// <summary>URL of the crosstales LinkedIn-profile.</summary>
      public const string ASSET_SOCIAL_LINKEDIN = "https://www.linkedin.com/company/crosstales";

      /// <summary>URL of the 3rd party asset "PlayMaker".</summary>
      public const string ASSET_3P_PLAYMAKER = "https://assetstore.unity.com/packages/slug/368?aid=1011lNGT";

      /// <summary>URL of the "Badword Filter" asset.</summary>
      public const string ASSET_BWF = "https://assetstore.unity.com/packages/slug/26255?aid=1011lNGT";

      /// <summary>URL of the "DJ" asset.</summary>
      public const string ASSET_DJ = "https://assetstore.unity.com/packages/slug/41993?aid=1011lNGT";

      /// <summary>URL of the "File Browser" asset.</summary>
      public const string ASSET_FB = "https://assetstore.unity.com/packages/slug/98713?aid=1011lNGT";

      /// <summary>URL of the "Online Check" asset.</summary>
      public const string ASSET_OC = "https://assetstore.unity.com/packages/slug/74688?aid=1011lNGT";

      /// <summary>URL of the "Radio" asset.</summary>
      public const string ASSET_RADIO = "https://assetstore.unity.com/packages/slug/32034?aid=1011lNGT";

      /// <summary>URL of the "RT-Voice" asset.</summary>
      public const string ASSET_RTV = "https://assetstore.unity.com/packages/slug/41068?aid=1011lNGT";

      /// <summary>URL of the "Turbo Backup" asset.</summary>
      public const string ASSET_TB = "https://assetstore.unity.com/packages/slug/98711?aid=1011lNGT";

      /// <summary>URL of the "Turbo Builder" asset.</summary>
      public const string ASSET_TPB = "https://assetstore.unity.com/packages/slug/98714?aid=1011lNGT";

      /// <summary>URL of the "Turbo Switch" asset.</summary>
      public const string ASSET_TPS = "https://assetstore.unity.com/packages/slug/60040?aid=1011lNGT";

      /// <summary>URL of the "True Random" asset.</summary>
      public const string ASSET_TR = "https://assetstore.unity.com/packages/slug/61617?aid=1011lNGT";

      /// <summary>Factor for kilo bytes.</summary>
      public const int FACTOR_KB = 1024;

      /// <summary>Factor for mega bytes.</summary>
      public const int FACTOR_MB = FACTOR_KB * 1024;

      /// <summary>Factor for giga bytes.</summary>
      public const int FACTOR_GB = FACTOR_MB * 1024;

      /// <summary>Float value of 32768.</summary>
      public const float FLOAT_32768 = 32768f;

      /// <summary>Float tolerance.</summary>
      public const float FLOAT_TOLERANCE = 0.0001f;

      /// <summary>ToString for two decimal places.</summary>
      public const string FORMAT_TWO_DECIMAL_PLACES = "0.00";

      /// <summary>ToString for no decimal places.</summary>
      public const string FORMAT_NO_DECIMAL_PLACES = "0";

      /// <summary>ToString for percent.</summary>
      public const string FORMAT_PERCENT = "0%";


      // Default values
      public const bool DEFAULT_DEBUG = false;

      /// <summary>Path delimiter for Windows.</summary>
      public const string PATH_DELIMITER_WINDOWS = @"\";

      /// <summary>Path delimiter for Unix.</summary>
      public const string PATH_DELIMITER_UNIX = "/";

      #endregion


      #region Changable variables

      /// <summary>Development debug logging for the asset.</summary>
      public static bool DEV_DEBUG = false;

      // Text fragments for the asset
      public static string TEXT_TOSTRING_START = " {";
      public static string TEXT_TOSTRING_END = "}";
      public static string TEXT_TOSTRING_DELIMITER = "', ";
      public static string TEXT_TOSTRING_DELIMITER_END = "'";

      // Prefixes for URLs and paths
      public static string PREFIX_HTTP = "http://";
      public static string PREFIX_HTTPS = "https://";

      /// <summary>Kill processes after 5000 milliseconds.</summary>
      public static int PROCESS_KILL_TIME = 5000;

      /// <summary>Path to the cmd under Windows.</summary>
      public static string CMD_WINDOWS_PATH = @"C:\Windows\system32\cmd.exe";

      /// <summary>Show the BWF banner.</summary>
      public static bool SHOW_BWF_BANNER = true;

      /// <summary>Show the DJ banner.</summary>
      public static bool SHOW_DJ_BANNER = true;

      /// <summary>Show the FB banner.</summary>
      public static bool SHOW_FB_BANNER = true;

      /// <summary>Show the OC banner.</summary>
      public static bool SHOW_OC_BANNER = true;

      /// <summary>Show the Radio banner.</summary>
      public static bool SHOW_RADIO_BANNER = true;

      /// <summary>Show the RTV banner.</summary>
      public static bool SHOW_RTV_BANNER = true;

      /// <summary>Show the TB banner.</summary>
      public static bool SHOW_TB_BANNER = true;

      /// <summary>Show the TPB banner.</summary>
      public static bool SHOW_TPB_BANNER = false; //TODO change as soon as it's in the UAS

      /// <summary>Show the TPS banner.</summary>
      public static bool SHOW_TPS_BANNER = true;

      /// <summary>Show the TR banner.</summary>
      public static bool SHOW_TR_BANNER = true;

      #endregion


      #region Properties

      /// <summary>URL prefix for files.</summary>
      public static string PREFIX_FILE
      {
         get
         {
            if ((BaseHelper.isWindowsBasedPlatform || BaseHelper.isWindowsEditor) && !BaseHelper.isMacOSEditor && !BaseHelper.isLinuxEditor)
            {
               return "file:///";
            }

            return "file://";
         }
      }

      /// <summary>Application path.</summary>
      public static string APPLICATION_PATH
      {
         get { return BaseHelper.ValidatePath(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1)); }
      }

      #endregion
   }
}
// © 2015-2020 crosstales LLC (https://www.crosstales.com)