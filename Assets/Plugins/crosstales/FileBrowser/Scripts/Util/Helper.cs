namespace Crosstales.FB.Util
{
   /// <summary>Various helper functions.</summary>
   public abstract class Helper : Common.Util.BaseHelper
   {
      #region Static properties

      /// <summary>Checks if the current platform is supported.</summary>
      /// <returns>True if the current platform is supported.</returns>
      public static bool isSupportedPlatform
      {
         get { return isWindowsPlatform || isMacOSPlatform || isLinuxPlatform || isWSAPlatform; }
      }

      #endregion
   }
}
// © 2017-2020 crosstales LLC (https://www.crosstales.com)