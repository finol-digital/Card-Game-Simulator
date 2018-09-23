using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CGS.Menus
{
    public class TitleScreen : MonoBehaviour
    {
        public Text versionText;

        void Start()
        {
            versionText.text = MainMenu.VersionMessage + Application.version;
#if UNITY_ANDROID || UNITY_IOS
            Branch.initSession(CallbackWithBranchUniversalObject);
#endif
        }

        void CallbackWithBranchUniversalObject(BranchUniversalObject buo, BranchLinkProperties linkProps, string error)
        {
            if (error != null)
            {
                Debug.LogError(error);
                return;
            }

            if (linkProps.controlParams.Count > 0)
            {
                CardGameManager.Instance.Messenger.Show("Deeplink params : "
                                    + buo.ToJsonString()
                                    + linkProps.ToJsonString());
            }
        }

        void Update()
        {
            if (CardGameManager.TopMenuCanvas != null)
                return;

            if (Input.anyKeyDown)
                SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
