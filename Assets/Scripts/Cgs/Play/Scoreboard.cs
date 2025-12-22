/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public Transform scoreboardPanel;

        public Text roomNameText;
        public Text roomIdIpText;

        public InputField nameInputField;

        public InputField pointsInputField;

        public RectTransform scoreContent;

        public ScoreTemplate scoreTemplate;

        private static bool IsOnline => CgsNetManager.Instance != null && CgsNetManager.Instance.IsOnline;

        private bool IsPointsOutOfSync => IsOnline && CgsNetManager.Instance.LocalPlayer != null &&
                                          CgsNetManager.Instance.LocalPlayer.Points != Points;

        private int Points
        {
            get => int.Parse(pointsInputField.text);
            set
            {
                pointsInputField.text = value.ToString();
                if (IsPointsOutOfSync)
                    CgsNetManager.Instance.LocalPlayer.RequestPointsUpdate(Points);
            }
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed += InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed += InputAdd;
        }

        private void Start()
        {
            nameInputField.text = PlayerPrefs.GetString(PlayerNamePlayerPrefs, DefaultPlayerName);
        }

        private void Update()
        {
            if (scoreboardPanel.gameObject.activeInHierarchy)
                Refresh();
        }

        [UsedImplicitly]
        public void ChangePoints(string points)
        {
            if (int.TryParse(points, out var pointsInt) && pointsInt != Points)
                Points = pointsInt;
        }

        private void InputSub(InputAction.CallbackContext context)
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.WasVisible || CardViewer.Instance.Zoom
                || PlayableViewer.Instance.IsVisible || PlayableViewer.Instance.WasVisible
                || CardGameManager.Instance.ModalCanvas != null || nameInputField.isFocused)
                return;

            Decrement();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Points--;
        }

        private void InputAdd(InputAction.CallbackContext context)
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.WasVisible || CardViewer.Instance.Zoom
                || PlayableViewer.Instance.IsVisible || PlayableViewer.Instance.WasVisible
                || CardGameManager.Instance.ModalCanvas != null || nameInputField.isFocused)
                return;

            Increment();
        }

        [UsedImplicitly]
        public void Increment()
        {
            Points++;
        }

        [UsedImplicitly]
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
                var shareText = IsOnline ? CgsNetManager.Instance.RoomIdIp : Offline;
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
            roomNameText.text = IsOnline ? CardGameManager.Current.Name : Offline;
            roomIdIpText.text = IsOnline ? CgsNetManager.Instance.RoomIdIp : Offline;

            var scores = GameObject.FindGameObjectsWithTag("Player")
                .Select(player => player.GetComponent<CgsNetPlayer>()).Select(cgsNetPlayer =>
                    new Tuple<string, int, string>(cgsNetPlayer.Name, cgsNetPlayer.Points, cgsNetPlayer.GetHandCount()))
                .ToList();
            scoreContent.DestroyAllChildren();
            scoreContent.sizeDelta = new Vector2(scoreContent.sizeDelta.x,
                ((RectTransform)scoreTemplate.transform).rect.height * scores.Count);
            foreach (var (playerName, points, handCount) in scores)
            {
                var entry = Instantiate(scoreTemplate.gameObject, scoreContent).GetComponent<ScoreTemplate>();
                entry.gameObject.SetActive(true);
                entry.nameText.text = playerName;
                entry.pointsText.text = points.ToString();
                entry.handCountText.text = string.IsNullOrEmpty(handCount)
                    ? PlayController.Instance.drawer
                        .cardZoneRectTransforms[CgsNetManager.Instance.LocalPlayer.CurrentHand]
                        .GetComponentsInChildren<CardModel>().Length.ToString()
                    : handCount;
            }
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed -= InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed -= InputAdd;
        }
    }
}
