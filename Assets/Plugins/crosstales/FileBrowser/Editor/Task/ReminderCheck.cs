#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.EditorUtil;

namespace Crosstales.FB.EditorTask
{
   /// <summary>Reminds the customer to create an UAS review.</summary>
   [InitializeOnLoad]
   public static class ReminderCheck
   {
      private const int numberOfDays = 17;
      private const int maxDays = numberOfDays * 2;

      #region Constructor

      static ReminderCheck()
      {
         string lastDate = EditorPrefs.GetString(EditorConstants.KEY_REMINDER_DATE);
         int count = EditorPrefs.GetInt(EditorConstants.KEY_REMINDER_COUNT) + 1;
         string date = System.DateTime.Now.ToString("yyyyMMdd"); // every day
         //string date = System.DateTime.Now.ToString("yyyyMMddHHmm"); // every minute (for tests)

         if (maxDays <= count && !date.Equals(lastDate))
         {
            //if (count % 1 == 0) // for testing only
            if (count % numberOfDays == 0) // && EditorConfig.REMINDER_CHECK)
            {
               int option = EditorUtility.DisplayDialogComplex(Util.Constants.ASSET_NAME + " - Reminder",
                  "Please don't forget to rate " + Util.Constants.ASSET_NAME + " or even better write a little review – it would be very much appreciated!",
                  "Yes, let's do it!",
                  "Not right now",
                  "Don't ask again!");

               switch (option)
               {
                  case 0:
                     Application.OpenURL(EditorConstants.ASSET_URL);
                     //EditorConfig.REMINDER_CHECK = false;
                     count = 9999;

                     Debug.LogWarning("<color=red>" + Common.Util.BaseHelper.CreateString("❤", 500) + "</color>");
                     Debug.LogWarning("<b>+++ Thank you for rating <color=blue>" + Util.Constants.ASSET_NAME + "</color>! +++</b>");
                     Debug.LogWarning("<color=red>" + Common.Util.BaseHelper.CreateString("❤", 500) + "</color>");
                     break;
                  case 1:
                     // do nothing!
                     break;
                  default:
                     count = 9999;
                     //EditorConfig.REMINDER_CHECK = false;
                     break;
               }

               //EditorConfig.Save();
            }

            EditorPrefs.SetString(EditorConstants.KEY_REMINDER_DATE, date);
            EditorPrefs.SetInt(EditorConstants.KEY_REMINDER_COUNT, count);
         }
      }

      #endregion
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)