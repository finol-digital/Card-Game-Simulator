/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class Die : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler
    {
        private const float RollTime = 1.0f;
        private const float RollDelay = 0.05f;

        public Text valueText;
        public List<CanvasGroup> buttons;

        public int Min { get; set; }
        public int Max { get; set; }

        private int Value
        {
            get => _value;
            set
            {
                int oldValue = _value;
                int newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;
                _value = newValue;
                OnChangeValue(oldValue, newValue);
            }
        }

        [SyncVar(hook = "OnChangeValue")] private int _value;

        private Vector2 _dragOffset;

        private void Start()
        {
            Roll();
            HideButtons();
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
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position - _dragOffset;
        }

        [UsedImplicitly]
        public void OnChangeValue(int oldValue, int newValue)
        {
            valueText.text = _value.ToString();
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
            StartCoroutine(DoRoll());
        }

        private IEnumerator DoRoll()
        {
            var elapsedTime = 0f;
            while (elapsedTime < RollTime)
            {
                Value = Random.Range(Min, Max);
                yield return new WaitForSeconds(RollDelay);
                elapsedTime += RollDelay;
            }
        }

        private void ShowButtons()
        {
            foreach (CanvasGroup button in buttons)
            {
                button.alpha = 1;
                button.interactable = true;
                button.blocksRaycasts = true;
            }
        }

        private void HideButtons()
        {
            foreach (CanvasGroup button in buttons)
            {
                button.alpha = 0;
                button.interactable = false;
                button.blocksRaycasts = false;
            }
        }
    }
}
