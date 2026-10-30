/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Reflection;
using Cgs;
using Cgs.Decks;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class DeckEditorScrollTests
    {
        [Test]
        public void ScrollbarsMoveContentAfterRepeatedPivots()
        {
            var previousGame = CardGameManager.Current;
            var root = new GameObject("Deck editor test", typeof(RectTransform));
            root.SetActive(false);
            var canvas = new GameObject("Scroll test canvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current))
                    .SetValue(null, new UnityCardGame(null, "deck_scroll_test"));
                var editor = root.AddComponent<DeckEditor>();
                var view = Rect("View", canvas.transform, new Vector2(200, 100));
                view.gameObject.SetActive(false);
                var scroll = view.gameObject.AddComponent<SecondaryScrollView>();
                scroll.viewport = view;
                scroll.content = Rect("Content", view, new Vector2(200, 600));
                scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.inertia = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.horizontalScrollbar = Rect("Horizontal", view, new Vector2(200, 20))
                    .gameObject.AddComponent<Scrollbar>();
                scroll.horizontalScrollbar.gameObject.SetActive(false);
                scroll.verticalScrollbar = Rect("Vertical", view, new Vector2(20, 100))
                    .gameObject.AddComponent<Scrollbar>();
                scroll.verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
                view.gameObject.SetActive(true);

                SetField(editor, "scrollRect", scroll);
                SetField(editor, "layoutContent", scroll.content);
                SetField(editor, "verticalScrollAreas", new List<GameObject>());
                SetField(editor, "horizontalScrollAreas", new List<GameObject>());
                SetField(editor, "nameText", Rect("Name", root.transform, Vector2.one).gameObject.AddComponent<Text>());
                SetField(editor, "countText", Rect("Count", root.transform, Vector2.one).gameObject.AddComponent<Text>());
                var verticalZone = Rect("Vertical zone", root.transform, new Vector2(280, 500));
                verticalZone.gameObject.SetActive(false);
                verticalZone.gameObject.AddComponent<VerticalLayoutGroup>();
                SetField(editor, "cardZoneVerticalPrefab", verticalZone.gameObject);
                var horizontalZone = Rect("Horizontal zone", root.transform, new Vector2(500, 370));
                horizontalZone.gameObject.SetActive(false);
                horizontalZone.gameObject.AddComponent<HorizontalLayoutGroup>();
                SetField(editor, "cardZoneHorizontalPrefab", horizontalZone.gameObject);

                Canvas.ForceUpdateCanvases();
                scroll.verticalScrollbar.value = 0.25f;
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0.25f).Within(0.001f));

                for (var i = 0; i < 3; i++)
                {
                    editor.Pivot();
                    Assert.That(editor.IsHorizontalLayout, Is.True);
                    Assert.That(scroll.horizontalScrollbar.gameObject.activeSelf, Is.True);
                    Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.False);
                    // Supply overflow independently of the number of cards in the user's game.
                    scroll.content.sizeDelta = new Vector2(1200, 0);
                    Canvas.ForceUpdateCanvases();
                    scroll.horizontalScrollbar.value = 0.75f;
                    Assert.That(scroll.horizontalNormalizedPosition, Is.EqualTo(0.75f).Within(0.001f));
                    Assert.That(scroll.content.anchoredPosition.x, Is.EqualTo(-750).Within(0.1f));
                    scroll.horizontalScrollbar.value = 0.25f;
                    Assert.That(scroll.content.anchoredPosition.x, Is.EqualTo(-250).Within(0.1f));

                    editor.Pivot();
                    Assert.That(editor.IsHorizontalLayout, Is.False);
                    Assert.That(scroll.horizontalScrollbar.gameObject.activeSelf, Is.False);
                    Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.True);
                    scroll.content.sizeDelta = new Vector2(DeckEditor.VerticalLayoutContentWidthDelta, 600);
                    Canvas.ForceUpdateCanvases();
                    scroll.verticalScrollbar.value = 0.25f;
                    Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0.25f).Within(0.001f));
                    Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(375).Within(0.1f));
                }
            }
            finally
            {
                Object.DestroyImmediate(canvas);
                Object.DestroyImmediate(root);
                typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, previousGame);
            }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size;
            return rect;
        }

        private static void SetField(DeckEditor editor, string name, object value) =>
            typeof(DeckEditor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, value);
    }
}
