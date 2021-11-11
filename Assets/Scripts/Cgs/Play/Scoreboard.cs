/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play
{
    public class Scoreboard : MonoBehaviour
    {
        public const string Offline = "Offline";
        public const string RoomIdIpCopiedMessage = "The Room Id/IP has been copied to the clipboard.";
        public const string RoomIdIpErrorMessage = "Error failed to copy the Room Id/IP to the clipboard!: ";

        public const string DefaultPlayerName = "Unnamed Player";
        public const string PlayerNamePlayerPrefs = "PlayerName";

        public Transform roomInfoPanel;

        public Text roomNameText;
        public Text roomIdIpText;

        public InputField nameInputField;

        public Text pointsText;

        public RectTransform scoreContent;

        public ScoreTemplate scoreTemplate;

        private static bool IsOnline => CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive;

        private bool IsPointsOutOfSync => IsOnline && CgsNetManager.Instance.LocalPlayer != null &&
                                          CgsNetManager.Instance.LocalPlayer.Points != Points;

        private int Points
        {
            get => int.Parse(pointsText.text);
            set => pointsText.text = value.ToString();
        }

        private void Start()
        {
            nameInputField.text = PlayerPrefs.GetString(PlayerNamePlayerPrefs, DefaultPlayerName);
        }

        private void Update()
        {
            if (roomInfoPanel.gameObject.activeSelf)
                Refresh();
        }

        public void Decrement()
        {
            Points--;
            if (IsPointsOutOfSync)
                CgsNetManager.Instance.LocalPlayer.RequestPointsUpdate(Points);
        }

        public void Increment()
        {
            Points++;
            if (IsPointsOutOfSync)
                CgsNetManager.Instance.LocalPlayer.RequestPointsUpdate(Points);
        }

        public void ToggleScorePanel()
        {
            roomInfoPanel.gameObject.SetActive(!roomInfoPanel.gameObject.activeSelf);
        }

        public void ToggleNameInput()
        {
            nameInputField.interactable = !nameInputField.interactable;
        }

        [UsedImplicitly]
        public void ChangeName(string newName)
        {
            PlayerPrefs.SetString(PlayerNamePlayerPrefs, newName);
            if (IsOnline && CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.Name != newName)
                CgsNetManager.Instance.LocalPlayer.RequestNameUpdate(newName);
        }

        [UsedImplicitly]
        public void Share()
        {
            try
            {
                string shareText = IsOnline ? CgsNetManager.Instance.playController.Lobby.IdIp : Offline;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                (new NativeShare()).SetText(shareText).Share();
#else
                UniClipboard.SetText(shareText);
                CardGameManager.Instance.Messenger.Show(RoomIdIpCopiedMessage);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(RoomIdIpErrorMessage + e.Message);
                CardGameManager.Instance.Messenger.Show(RoomIdIpErrorMessage + e.Message);
            }
        }

        private void Refresh()
        {
            // TODO: Improve this very slow process
            roomNameText.text = IsOnline ? CgsNetManager.Instance.RoomName : Offline;
            roomIdIpText.text = IsOnline ? CgsNetManager.Instance.RoomIdIp : Offline;

            List<Tuple<string, int, string>> scores = GameObject.FindGameObjectsWithTag("Player")
                .Select(player => player.GetComponent<CgsNetPlayer>()).Select(cgsNetPlayer =>
                    new Tuple<string, int, string>(cgsNetPlayer.Name, cgsNetPlayer.Points, cgsNetPlayer.HandCount))
                .ToList();
            scoreContent.DestroyAllChildren();
            scoreContent.sizeDelta = new Vector2(scoreContent.sizeDelta.x,
                ((RectTransform)scoreTemplate.transform).rect.height * scores.Count);
            foreach ((string playerName, int points, string handCount) in scores)
            {
                var entry = Instantiate(scoreTemplate.gameObject, scoreContent).GetComponent<ScoreTemplate>();
                entry.gameObject.SetActive(true);
                entry.nameText.text = playerName;
                entry.pointsText.text = points.ToString();
                entry.handCountText.text = string.IsNullOrEmpty(handCount)
                    ? CgsNetManager.Instance.playController.drawer
                        .cardZoneRectTransforms[CgsNetManager.Instance.LocalPlayer.CurrentHand]
                        .GetComponentsInChildren<CardModel>().Length.ToString()
                    : handCount;
            }
        }
    }
}
