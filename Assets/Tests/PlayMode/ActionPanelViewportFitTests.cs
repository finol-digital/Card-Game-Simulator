/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Cgs.Menu;
using Cgs.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class ActionPanelViewportFitTests
    {
        private GameObject _container;
        private GameObject _eventSystem;
        private bool _tooltipsEnabled;

        [SetUp]
        public void SetUp()
        {
            _tooltipsEnabled = Settings.ButtonTooltipsEnabled;
            Settings.ButtonTooltipsEnabled = true;
            _container = new GameObject("Action panel test");
            _container.SetActive(false);
            if (EventSystem.current == null)
                _eventSystem = new GameObject("Action panel events", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_container);
            if (_eventSystem != null)
                Object.DestroyImmediate(_eventSystem);
            Settings.ButtonTooltipsEnabled = _tooltipsEnabled;
        }

        [UnityTest]
        public IEnumerator CardActionsAndTooltipsStayInViewport()
        {
            yield return CheckPanel("Card Viewer", "Card Action Panel");
        }

        [UnityTest]
        public IEnumerator DockedCardActionsAndTooltipsStayInViewport()
        {
            yield return CheckPanel("Card Docked Viewer", "Card Action Panel");
        }

        [UnityTest]
        public IEnumerator StackActionsAndTooltipsStayInViewport()
        {
            yield return CheckPanel("Playable Viewer", "Stack Action Panel");
        }

        private IEnumerator CheckPanel(string prefabName, string panelName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/CardGameView/Viewer/{prefabName}.prefab");
            var viewer = Object.Instantiate(prefab, _container.transform);
            // Exercise the real UI assets without starting a game or connecting multiplayer.
            foreach (var behaviour in viewer.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ToolTip || behaviour is ActionPanelViewportFit
                                        || behaviour is UnityEngine.EventSystems.UIBehaviour)
                    continue;
                Object.DestroyImmediate(behaviour);
            }

            viewer.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
            viewer.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var viewport = (RectTransform)viewer.transform;
            viewport.localScale = Vector3.one;
            var panel = viewer.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == panelName);
            var preferredPositionX = panel.anchoredPosition.x;
            Assert.IsNotNull(panel.GetComponent<ActionPanelViewportFit>());
            Assert.LessOrEqual(panel.rect.height, 900, "Single-line hints should allow a compact panel.");
            panel.gameObject.SetActive(true);
            viewer.SetActive(true);
            _container.SetActive(true);

            // Includes the review's reference height, a shorter ultrawide canvas, narrow portrait,
            // and a return to the original size to catch cumulative scale/position drift.
            foreach (var size in new[] {new Vector2(3200, 1800), new Vector2(1920, 1080),
                         new Vector2(2560, 820), new Vector2(1080, 1920), new Vector2(800, 1920), new Vector2(480, 1920),
                         new Vector2(320, 1920), new Vector2(1080, 1920), new Vector2(3200, 1800)})
            {
                viewport.sizeDelta = size;
                yield return null;
                yield return null;
                var panelBounds = BoundsIn(viewport, panel);
                Assert.GreaterOrEqual(panelBounds.xMin, viewport.rect.xMin - 0.1f, $"{prefabName} at {size}");
                Assert.LessOrEqual(panelBounds.xMax, viewport.rect.xMax + 0.1f, $"{prefabName} at {size}");
                Assert.GreaterOrEqual(panelBounds.yMin, viewport.rect.yMin + 20 - 0.1f);
                Assert.LessOrEqual(panelBounds.yMax, viewport.rect.yMax - 160 + 0.1f);

                var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>();
                Assert.AreEqual(5, buttons.Length);
                foreach (var button in buttons)
                {
                    var tooltip = button.GetComponentInChildren<ToolTip>();
                    Assert.IsNotNull(tooltip);
                    tooltip.OnPointerEnter(null);
                    yield return null;
                    yield return null;
                    var tipObject = (GameObject)typeof(ToolTip).GetField("_toolTipGameObject",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tooltip);
                    Assert.IsNotNull(tipObject, $"{prefabName}: {button.name} must create its tooltip.");
                    var tipRect = (RectTransform)tipObject.transform;
                    AssertSingleLine(tipObject);
                    StringAssert.DoesNotContain("…", tipObject.GetComponentInChildren<UnityEngine.UI.Text>().text,
                        "Standard action hints fit the panel and must not be truncated.");
                    var tipBounds = BoundsIn(viewport, tipRect);
                    Assert.GreaterOrEqual(tipBounds.xMin, viewport.rect.xMin - 0.1f, button.name);
                    Assert.LessOrEqual(tipBounds.xMax, viewport.rect.xMax + 0.1f, button.name);
                    Assert.GreaterOrEqual(tipBounds.yMin, viewport.rect.yMin - 0.1f, button.name);
                    Assert.LessOrEqual(tipBounds.yMax, viewport.rect.yMax + 0.1f, button.name);
                    foreach (var other in buttons)
                    {
                        // The tooltip may touch its own button's decorative padding, but not its label.
                        var content = other == button ? other.transform.Find("Label") : other.transform;
                        var buttonBounds = BoundsIn(viewport, (RectTransform)content);
                        var overlapHeight = Mathf.Min(tipBounds.yMax, buttonBounds.yMax)
                                            - Mathf.Max(tipBounds.yMin, buttonBounds.yMin);
                        Assert.LessOrEqual(overlapHeight, 0.1f,
                            $"{button.name} tooltip {tipBounds} overlaps {other.name} {buttonBounds} at {size}.");
                    }
                    tooltip.OnPointerExit(null);
                    yield return null;
                    yield return null;
                    StringAssert.DoesNotContain("…", tipObject.GetComponentInChildren<UnityEngine.UI.Text>().text,
                        "A short shortcut must not be truncated due to fractional text widths.");
                }

                if (Mathf.Approximately(size.y, 1800))
                {
                    Assert.AreEqual(Vector3.one, panel.localScale, "Restore full size when space returns.");
                    Assert.AreEqual(preferredPositionX, panel.anchoredPosition.x, 0.1f,
                        "Restore the preferred horizontal position when space returns.");
                }
            }

            var longTooltip = panel.GetComponentsInChildren<ToolTip>()[0];
            var serialized = new SerializedObject(longTooltip);
            serialized.FindProperty("tooltip").stringValue = string.Join(" ", Enumerable.Repeat(
                "A long action description that must stay on one line", 10));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            longTooltip.OnPointerEnter(null);
            yield return null;
            yield return null;
            var longTipObject = (GameObject)typeof(ToolTip).GetField("_toolTipGameObject",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(longTooltip);
            AssertSingleLine(longTipObject);
            StringAssert.EndsWith("…", longTipObject.GetComponentInChildren<UnityEngine.UI.Text>().text);
            Assert.Greater(((RectTransform)longTipObject.transform).rect.width, 500,
                "Long hints should use the panel width before truncating.");
            longTooltip.OnPointerExit(null);
        }

        private static void AssertSingleLine(GameObject tooltip)
        {
            var text = tooltip.GetComponentInChildren<UnityEngine.UI.Text>();
            Assert.IsNotEmpty(text.text);
            StringAssert.DoesNotContain("\n", text.text);
            Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + 0.1f,
                "Truncated text must fit horizontally without clipping.");
            Assert.LessOrEqual(((RectTransform)tooltip.transform).rect.height, 60,
                "Hints must occupy only one line, including their shortcut.");
        }

        private static Rect BoundsIn(RectTransform viewport, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var bottomLeft = viewport.InverseTransformPoint(corners[0]);
            var topRight = viewport.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }
    }
}
#endif
