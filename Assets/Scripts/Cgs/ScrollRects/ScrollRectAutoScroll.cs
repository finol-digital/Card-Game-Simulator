using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollRects
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectAutoScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scrollSpeed = 10f;

        private bool _active;
        private ScrollRect _scrollRect;
        private Vector2 _nextScrollPosition = Vector2.up;
        private readonly List<Selectable> _selectables = new List<Selectable>();

        void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        void OnEnable()
        {
            if (_scrollRect)
                _scrollRect.content.GetComponentsInChildren(_selectables);
        }

        void Start()
        {
            if (_scrollRect)
                _scrollRect.content.GetComponentsInChildren(_selectables);

            ScrollToSelected(true);
        }

        void Update()
        {
            // Scroll via input
            InputScroll();

            // Scroll via hovering
            if (!_active)
            {
                _scrollRect.normalizedPosition = Vector2.Lerp(_scrollRect.normalizedPosition, _nextScrollPosition,
                    scrollSpeed * Time.deltaTime);
            }
            else
            {
                _nextScrollPosition = _scrollRect.normalizedPosition;
            }
        }

        void InputScroll()
        {
            if (_selectables.Count <= 0)
                return;

            if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical") ||
                Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
            {
                ScrollToSelected(false);
            }
        }

        void ScrollToSelected(bool quickScroll)
        {
            int selectedIndex = -1;
            Selectable selectedElement = EventSystem.current.currentSelectedGameObject
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
                : null;

            if (selectedElement)
                selectedIndex = _selectables.IndexOf(selectedElement);

            if (selectedIndex <= -1)
                return;

            if (quickScroll)
            {
                _scrollRect.normalizedPosition =
                    new Vector2(0, 1 - (selectedIndex / ((float) _selectables.Count - 1)));
                _nextScrollPosition = _scrollRect.normalizedPosition;
            }
            else
            {
                _nextScrollPosition = new Vector2(0, 1 - (selectedIndex / ((float) _selectables.Count - 1)));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _active = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _active = false;
            ScrollToSelected(false);
        }
    }
}
