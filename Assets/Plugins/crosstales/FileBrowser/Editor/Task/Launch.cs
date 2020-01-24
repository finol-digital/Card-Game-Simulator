#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.FB.EditorTask
{
    /// <summary>Show the configuration window on the first launch.</summary>
    [InitializeOnLoad]
    public static class Launch
    {

        #region Constructor

        static Launch()
        {
            bool launched = EditorPrefs.GetBool(EditorUtil.EditorConstants.KEY_LAUNCH);

            if (!launched) {
                EditorIntegration.ConfigWindow.ShowWindow(2);
                EditorPrefs.SetBool(EditorUtil.EditorConstants.KEY_LAUNCH, true);
            }
        }

        #endregion
    }
}
#endif
// © 2019 crosstales LLC (https://www.crosstales.com)