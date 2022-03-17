/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Die : CgsNetPlayable
    {
        public const int DefaultMin = 1;
        public const int DefaultMax = 6;
        public const string DeletePrompt = "Delete die?";

        private const float RollTime = 1.0f;
        private const float RollDelay = 0.05f;

        public Text valueText;

        [field: SyncVar] public int Min { get; set; } = DefaultMin;

        [field: SyncVar] public int Max { get; set; } = DefaultMax;

        public override string ViewValue => $"Dice Value: {Value}";

        private int Value
        {
            get => _value;
            set
            {
                var newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                if (NetworkManager.singleton.isNetworkActive)
                    CmdUpdateValue(newValue);
                else
                {
                    _value = newValue;
                    OnChangeValue(value, newValue);
                }
            }
        }

        [SyncVar(hook = nameof(OnChangeValue))]
        private int _value;

        private float _rollTime;
        private float _rollDelay;

        protected override void OnStartPlayable()
        {
            _value = Min;
            valueText.text = _value.ToString();
            if (!NetworkManager.singleton.isNetworkActive || isServer)
                _rollTime = RollTime;
        }

        protected override void OnUpdatePlayable()
        {
            if (_rollTime <= 0 || (NetworkManager.singleton.isNetworkActive && !isServer))
                return;

            _rollTime -= Time.deltaTime;
            _rollDelay += Time.deltaTime;
            if (_rollDelay < RollDelay)
                return;

            Value = Random.Range(Min, Max + 1);
            _rollDelay = 0;
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            if (Settings.PreviewOnMouseOver && CardViewer.Instance != null && !CardViewer.Instance.IsVisible
                && PlayableViewer.Instance != null && !PlayableViewer.Instance.IsVisible)
                PlayableViewer.Instance.Preview(this);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.HidePreview();
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.SelectedPlayable = this;
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.IsVisible = false;
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (NetworkManager.singleton.isNetworkActive)
                RequestTransferAuthority();
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (LacksAuthority)
                RequestTransferAuthority();
            else
                UpdatePosition();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!LacksAuthority)
                UpdatePosition();
        }

        [Command(requiresAuthority = false)]
        private void CmdUpdateValue(int value)
        {
            _value = value;
        }

        [PublicAPI]
        public void OnChangeValue(int oldValue, int newValue)
        {
            valueText.text = newValue.ToString();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Value--;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Value++;
        }

        [UsedImplicitly]
        public void Roll()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdRoll();
            else
                _rollTime = RollTime;
        }

        [Command(requiresAuthority = false)]
        private void CmdRoll()
        {
            _rollTime = RollTime;
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, RequestDelete);
        }
    }
}
