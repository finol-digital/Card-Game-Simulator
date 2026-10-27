/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using Cgs.UI.ScrollRects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class ScrollRectAutoScrollTests
    {
        private GameObject _root;
        private GameObject _events;
        private EventSystem _previousEvents;
        private ScrollRect _scroll;

        [SetUp]
        public void SetUp()
        {
            _previousEvents = EventSystem.current;
            _events = new GameObject("Scroll test events", typeof(EventSystem));
            EventSystem.current = _events.GetComponent<EventSystem>();
            _root = new GameObject("Scroll test canvas", typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _scroll = CreateScroll(_root.transform, new Vector2(200, 100), new Vector2(200, 600));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_events);
            if (_previousEvents != null && _previousEvents.isActiveAndEnabled)
                EventSystem.current = _previousEvents;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size;
            return rect;
        }

        private static ScrollRect CreateScroll(Transform parent, Vector2 viewSize, Vector2 contentSize)
        {
            var view = Rect("View", parent, viewSize);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = Rect("Content", view, contentSize);
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            view.gameObject.AddComponent<ScrollRectAutoScroll>();
            return scroll;
        }

        private RectTransform Control(float y, float height = 30, ScrollRect scroll = null)
        {
            var rect = Rect("Control", (scroll != null ? scroll : _scroll).content, new Vector2(80, height));
            rect.anchoredPosition = new Vector2(0, -y);
            rect.gameObject.AddComponent<Button>();
            return rect;
        }

        private static void Select(RectTransform target) => EventSystem.current.SetSelectedGameObject(target.gameObject);

        private static void AssertVisible(ScrollRect scroll, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var local = scroll.viewport.InverseTransformPoint(corner);
                if (scroll.vertical)
                    Assert.That(local.y, Is.InRange(scroll.viewport.rect.yMin - 0.1f, scroll.viewport.rect.yMax + 0.1f));
                if (scroll.horizontal)
                    Assert.That(local.x, Is.InRange(scroll.viewport.rect.xMin - 0.1f, scroll.viewport.rect.xMax + 0.1f));
            }
        }

        [UnityTest]
        public IEnumerator SelectionChangesRevealUnevenRowsInBothDirections()
        {
            var first = Control(0);
            var middle = Control(255, 45);
            var last = Control(570);
            Select(first);
            yield return null;
            yield return null;
            AssertVisible(_scroll, first);
            Select(middle);
            yield return null;
            yield return null;
            AssertVisible(_scroll, middle);
            Assert.That(_scroll.content.anchoredPosition.y, Is.EqualTo(200).Within(0.1f), "Move only enough to reveal the row.");
            Select(last);
            yield return null;
            yield return null;
            AssertVisible(_scroll, last);
            Select(first);
            yield return null;
            yield return null;
            AssertVisible(_scroll, first);
        }

        [UnityTest]
        public IEnumerator ManualScrollAndPointerExitDoNotSnapBackToUnchangedSelection()
        {
            var last = Control(570);
            Select(last);
            yield return null;
            yield return null;
            _scroll.verticalNormalizedPosition = 1;
            ExecuteEvents.Execute(_scroll.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return null;
            yield return null;
            Assert.That(_scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
        }

        [Test]
        public void NewlySelectedVisibleControlStopsInertiaButUnchangedSelectionDoesNot()
        {
            var item = Control(270);
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0.5f;
            _scroll.velocity = Vector2.up * 100;
            Select(item);

            // Observe selection before ScrollRect's physics tick can decay the velocity.
            var update = typeof(ScrollRectAutoScroll).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var autoScroll = _scroll.GetComponent<ScrollRectAutoScroll>();
            update.Invoke(autoScroll, null);
            AssertVisible(_scroll, item);
            Assert.AreEqual(Vector2.zero, _scroll.velocity);
            Assert.That(_scroll.verticalNormalizedPosition, Is.EqualTo(0.5f).Within(0.001f));

            _scroll.velocity = Vector2.up * 100;
            update.Invoke(autoScroll, null);
            Assert.AreEqual(Vector2.up * 100, _scroll.velocity,
                "Manual scrolling stays free until the selection changes again.");
        }

        [UnityTest]
        public IEnumerator PointerHoverDoesNotBlockNewSelectionAndDynamicControlsWork()
        {
            yield return null;
            var last = Control(570);
            ExecuteEvents.Execute(_scroll.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            Select(last);
            yield return null;
            yield return null;
            AssertVisible(_scroll, last);
        }

        [UnityTest]
        public IEnumerator VisibleControlsAndOutsideSelectionLeaveScrollUntouched()
        {
            Select(Control(40));
            yield return null;
            yield return null;
            Assert.That(_scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
            var outside = Rect("Outside", _root.transform, new Vector2(80, 30));
            outside.anchoredPosition = new Vector2(0, -800);
            Select(outside);
            yield return null;
            yield return null;
            Assert.That(_scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator NestedDropdownScrollsWithoutMovingItsParentMenu()
        {
            var popup = CreateScroll(_scroll.content, new Vector2(100, 80), new Vector2(100, 400));
            ((RectTransform)popup.transform).anchoredPosition = new Vector2(0, -250);
            var item = Control(370, scroll: popup);
            Select(item);
            yield return null;
            yield return null;
            AssertVisible(popup, item);
            Assert.That(_scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HorizontalViewSupportsScaledHierarchyAndPreservesDisabledAxis()
        {
            _scroll.horizontal = true;
            _scroll.vertical = false;
            _scroll.content.sizeDelta = new Vector2(600, 100);
            _scroll.transform.localScale = new Vector3(1.5f, 1.5f, 1);
            var item = Control(0);
            item.anchoredPosition = new Vector2(520, 0);
            Select(item);
            yield return null;
            yield return null;
            AssertVisible(_scroll, item);
            Assert.That(_scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator SingleItemFittingContentStaysFiniteAndOversizedItemAlignsAtTop()
        {
            _scroll.content.sizeDelta = new Vector2(200, 80);
            Select(Control(0));
            yield return null;
            yield return null;
            Assert.That(_scroll.content.anchoredPosition, Is.EqualTo(Vector2.zero));
            _scroll.content.sizeDelta = new Vector2(200, 600);
            var oversized = Control(200, 150);
            Select(oversized);
            yield return null;
            yield return null;
            Assert.That(_scroll.content.anchoredPosition.y, Is.EqualTo(200).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator ReopeningViewRevealsExistingSelectionEvenWhenTimeIsPaused()
        {
            var last = Control(570);
            Select(last);
            yield return null;
            yield return null;
            _scroll.gameObject.SetActive(false);
            _scroll.verticalNormalizedPosition = 1;
            var timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                _scroll.gameObject.SetActive(true);
                yield return null;
                yield return null;
                AssertVisible(_scroll, last);
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }
    }
}
