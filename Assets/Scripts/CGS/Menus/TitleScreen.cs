/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CGS.Menus
{
    public class TitleScreen : MonoBehaviour
    {
        public const string GameName = "GameName";
        public const string GameUrl = "GameUrl";

        public Text versionText;

        void Start()
        {
            versionText.text = MainMenu.VersionMessage + Application.version;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Branch.initSession(BranchCallbackWithBranchUniversalObject);
#endif
        }

        void BranchCallbackWithBranchUniversalObject(BranchUniversalObject buo, BranchLinkProperties linkProps, string error)
        {
            if (error != null)
            {
                Debug.LogError(error);
                return;
            }

            string gameName, gameUrl;
            if (linkProps.controlParams.TryGetValue(GameName, out gameName) && CardGameManager.Instance.AllCardGames.ContainsKey(gameName))
                CardGameManager.Instance.SelectCardGame(gameName);
            else if (linkProps.controlParams.TryGetValue(GameUrl, out gameUrl) && !string.IsNullOrEmpty(gameUrl))
            {
                CardGameManager.Instance.Selector.Show();
                CardGameManager.Instance.Selector.ShowDownloadPanel();
                CardGameManager.Instance.Selector.urlInput.text = gameUrl;
                CardGameManager.Instance.Selector.StartDownload();
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
