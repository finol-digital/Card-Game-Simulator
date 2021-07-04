/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class Scoreboard : MonoBehaviour
    {
        public const string DefaultPlayerName = "Unnamed Player";
        public const string PlayerNamePlayerPrefs = "PlayerName";

        public Transform scorePanel;

        public InputField nameInputField;

        public Text pointsText;

        private bool IsPointsOutOfSync => CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive &&
                                          CgsNetManager.Instance.LocalPlayer != null &&
                                          CgsNetManager.Instance.LocalPlayer.Points != Points;

        private void Start()
        {
            nameInputField.text = PlayerPrefs.GetString(PlayerNamePlayerPrefs, DefaultPlayerName);
        }

        private int Points
        {
            get => int.Parse(pointsText.text);
            set => pointsText.text = value.ToString();
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
            scorePanel.gameObject.SetActive(!scorePanel.gameObject.activeSelf);
        }

        public void ToggleNameInput()
        {
            nameInputField.interactable = !nameInputField.interactable;
        }

        [UsedImplicitly]
        public void ChangeName(string newName)
        {
            PlayerPrefs.SetString(PlayerNamePlayerPrefs, newName);
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive &&
                CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.Name != newName)
                CgsNetManager.Instance.LocalPlayer.RequestNameUpdate(newName);
        }
    }
}
