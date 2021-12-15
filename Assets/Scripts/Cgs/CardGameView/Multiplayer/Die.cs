/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Die : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const string DeletePrompt = "Delete die?";

        private const float RollTime = 1.0f;
        private const float RollDelay = 0.05f;

        public Text valueText;
        public CanvasGroup buttonsCanvasGroup;

        [field: SyncVar] public int Min { get; set; }

        [field: SyncVar] public int Max { get; set; }

        private int Value
        {
            get => _value;
            set
            {
                int newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                if (NetworkManager.singleton.isNetworkActive)
                    CmdUpdateValue(newValue);
                else
                {
                    int oldValue = value;
                    _value = newValue;
                    OnChangeValue(oldValue, newValue);
                }
            }
        }

        [SyncVar(hook = nameof(OnChangeValue))]
        private int _value;

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        private Vector2 _dragOffset;

        private float _rollTime;
        private float _rollDelay;

        private void Start()
        {
            valueText.text = _value.ToString();
            if (Vector2.zero != position)
                ((RectTransform) transform).localPosition = position;
            if (!NetworkManager.singleton.isNetworkActive || isServer)
                _rollTime = RollTime;

            HideButtons();
        }

        private void Update()
        {
            if (_rollTime <= 0 || (NetworkManager.singleton.isNetworkActive && !isServer))
                return;

            _rollTime -= Time.deltaTime;
            _rollDelay += Time.deltaTime;
            if (_rollDelay < RollDelay)
                return;

            Value = Random.Range(Min, Max);
            _rollDelay = 0;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // OnPointerDown is required for OnPointerUp to trigger
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            ShowButtons();
        }

        public void OnSelect(BaseEventData eventData)
        {
            ShowButtons();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            HideButtons();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragOffset = eventData.position - ((Vector2) transform.position);
            transform.SetAsLastSibling();

            HideButtons();

            if (NetworkManager.singleton.isNetworkActive)
                CmdTransferAuthority();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (NetworkManager.singleton.isNetworkActive && !hasAuthority)
                return;
            var rectTransform = ((RectTransform) transform);
            rectTransform.position = eventData.position - _dragOffset;
            if (NetworkManager.singleton.isNetworkActive)
                CmdUpdatePosition(rectTransform.localPosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!hasAuthority)
                return;
            CmdReleaseAuthority();
        }

        [Command(requiresAuthority = false)]
        private void CmdTransferAuthority(NetworkConnectionToClient sender = null)
        {
            if (sender != null && netIdentity.connectionToClient == null)
                netIdentity.AssignClientAuthority(sender);
        }

        [Command]
        private void CmdUpdatePosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            transform.localPosition = newValue;
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

        private void ShowButtons()
        {
            buttonsCanvasGroup.alpha = 1;
            buttonsCanvasGroup.interactable = true;
            buttonsCanvasGroup.blocksRaycasts = true;
        }

        private void HideButtons()
        {
            buttonsCanvasGroup.alpha = 0;
            buttonsCanvasGroup.interactable = false;
            buttonsCanvasGroup.blocksRaycasts = false;
        }

        [Command]
        private void CmdReleaseAuthority()
        {
            netIdentity.RemoveClientAuthority();
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, Delete);
        }

        private void Delete()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdDelete();
            else
                Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        private void CmdDelete()
        {
            if (netIdentity.connectionToClient != null)
            {
                Debug.LogWarning("Ignoring request to delete, since it is currently owned by a client!");
                return;
            }

            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }
}
