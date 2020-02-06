#if UNITY_EDITOR
namespace Crosstales.FB.EditorUtil
{
   /// <summary>Collected editor constants of very general utility for the asset.</summary>
   public static class EditorConstants
   {
      #region Constant variables

      public const string KEY_UPDATE_CHECK = Util.Constants.KEY_PREFIX + "UPDATE_CHECK";

      // Keys for the configuration of the asset
      public const string KEY_UPDATE_DATE = Util.Constants.KEY_PREFIX + "UPDATE_DATE";

      public const string KEY_REMINDER_DATE = Util.Constants.KEY_PREFIX + "REMINDER_DATE";
      public const string KEY_REMINDER_COUNT = Util.Constants.KEY_PREFIX + "REMINDER_COUNT";

      public const string KEY_LAUNCH = Util.Constants.KEY_PREFIX + "LAUNCH";

      // Default values
      public const string DEFAULT_ASSET_PATH = "/Plugins/crosstales/FileBrowser/";
      public const bool DEFAULT_UPDATE_CHECK = false;

      #endregion


      #region Properties

      /// <summary>Returns the URL of the asset in UAS.</summary>
      /// <returns>The URL of the asset in UAS.</returns>
      public static string ASSET_URL
      {
         get { return Util.Constants.ASSET_PRO_URL; }
      }

      /// <summary>Returns the ID of the asset in UAS.</summary>
      /// <returns>The ID of the asset in UAS.</returns>
      public static string ASSET_ID
      {
         get { return "98713"; }
      }

      /// <summary>Returns the UID of the asset.</summary>
      /// <returns>The UID of the asset.</returns>
      public static System.Guid ASSET_UID
      {
         get { return new System.Guid("f9c139be-4da6-4d0f-894a-0675635af15f"); }
      }

      #endregion
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)