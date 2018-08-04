using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace CGS.Play
{
    public class Die : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler
    {
        public const float RollTime = 1.0f;
        public const float RollDelay = 0.05f;

        public Text valueText;
        public List<CanvasGroup> buttons;

        public int Min { get; set; }
        public int Max { get; set; }

        public Vector2 DragOffset { get; private set; }

        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (_value > Max)
                    _value = Min;
                if (_value < Min)
                    _value = Max;
                valueText.text = _value.ToString();
            }
        }
        [SyncVar]
        private int _value;

        void Start()
        {
            Roll();
            HideButtons();
        }

        public void Roll()
        {
            StartCoroutine(DoRoll());
        }

        public IEnumerator DoRoll()
        {
            float elapsedTime = 0f;
            while (elapsedTime < RollTime)
            {
                Value = Random.Range(Min, Max);
                yield return new WaitForSeconds(RollDelay);
                elapsedTime += RollDelay;
            }
        }

        public void ShowButtons()
        {
            foreach (CanvasGroup button in buttons)
            {
                button.alpha = 1;
                button.interactable = true;
                button.blocksRaycasts = true;
            }
        }

        public void HideButtons()
        {
            foreach (CanvasGroup button in buttons)
            {
                button.alpha = 0;
                button.interactable = false;
                button.blocksRaycasts = false;
            }
        }

        // Required for OnPointerUp to trigger
        public void OnPointerDown(PointerEventData eventData) { }

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
            DragOffset = eventData.position - ((Vector2)transform.position);
            transform.SetAsLastSibling();
            HideButtons();
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position - DragOffset;
        }

        public void Decrement()
        {
            Value--;
        }

        public void Increment()
        {
            Value++;
        }
    }
}
