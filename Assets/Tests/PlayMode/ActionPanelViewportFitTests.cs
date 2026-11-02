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
        private RenderTexture _cameraTarget;
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
            if (_cameraTarget != null)
                Object.DestroyImmediate(_cameraTarget);
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

        [UnityTest]
        public IEnumerator AllActionPanelsAvoidMovingAndRotatedSelections()
        {
            foreach (var entry in new[] { ("Card Viewer", "Card Action Panel"),
                         ("Card Docked Viewer", "Card Action Panel"), ("Playable Viewer", "Stack Action Panel"),
                         ("Playable Viewer", "Die Action Panel"), ("Playable Viewer", "Counter Action Panel") })
            {
                _container.SetActive(false);
                var viewer = CreateViewer(entry.Item1);
                var viewport = (RectTransform)viewer.transform;
                viewport.sizeDelta = new Vector2(3200, 1800);
                var panel = viewer.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == entry.Item2);
                var fit = panel.GetComponent<ActionPanelViewportFit>();
                Assert.IsNotNull(fit, entry.ToString());
                panel.gameObject.SetActive(true);
                var target = (RectTransform)new GameObject("Selected object", typeof(RectTransform)).transform;
                target.SetParent(viewport, false);
                target.sizeDelta = new Vector2(240, 340);
                viewer.SetActive(true);
                _container.SetActive(true);
                yield return null;
                yield return null;
                var preferred = panel.anchoredPosition;
                var preferredBounds = BoundsIn(viewport, panel);
                target.localPosition = preferredBounds.center;
                fit.SetAvoidanceTarget(target);
                yield return null;
                AssertClearOfSelection(viewport, panel, target);
                Assert.AreEqual(preferred.y, panel.anchoredPosition.y, 0.1f, "Move horizontally only.");
                Assert.AreEqual(Vector3.one, panel.localScale, "Do not shrink when a side has room.");

                // Re-evaluate visual bounds after rotation, zoom, and movement without reselecting.
                target.localRotation = Quaternion.Euler(0, 0, 45);
                target.localScale = Vector3.one * 1.5f;
                yield return null;
                AssertClearOfSelection(viewport, panel, target);
                target.localPosition = new Vector2(viewport.rect.xMin + 250, preferredBounds.center.y);
                yield return null;
                AssertClearOfSelection(viewport, panel, target);
                target.localPosition = new Vector2(viewport.rect.xMax - 250, preferredBounds.center.y);
                yield return null;
                AssertClearOfSelection(viewport, panel, target);

                // A wide selection on a narrow screen requires fitting the panel into a side strip.
                viewport.sizeDelta = new Vector2(800, 1800);
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
                target.sizeDelta = new Vector2(400, 800);
                target.localPosition = new Vector2(viewport.rect.center.x, preferredBounds.center.y);
                yield return null;
                yield return null;
                AssertClearOfSelection(viewport, panel, target);
                Assert.Less(panel.localScale.x, 1);

                viewport.sizeDelta = new Vector2(3200, 1800);
                fit.SetAvoidanceTarget(null);
                yield return null;
                Assert.AreEqual(preferred.x, panel.anchoredPosition.x, 0.1f);
                Assert.AreEqual(preferred.y, panel.anchoredPosition.y, 0.1f);
                Assert.AreEqual(Vector3.one, panel.localScale);

                target.localPosition = preferredBounds.center;
                fit.SetAvoidanceTarget(target);
                yield return null;
                AssertClearOfSelection(viewport, panel, target);
                Object.DestroyImmediate(target.gameObject);
                yield return null;
                Assert.AreEqual(preferred.x, panel.anchoredPosition.x, 0.1f,
                    "A deleted selection must release the panel.");
                Object.DestroyImmediate(viewer);
            }
        }

        [UnityTest]
        public IEnumerator AvoidanceUsesScreenBoundsAcrossScaledCanvasesAndCameras()
        {
            var cameraObject = new GameObject("Selection camera", typeof(Camera));
            cameraObject.transform.SetParent(_container.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.transform.localPosition = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 450;
            _cameraTarget = new RenderTexture(1200, 900, 0);
            camera.targetTexture = _cameraTarget;
            camera.aspect = 1200f / 900;

            var source = new GameObject("Play area canvas", typeof(Canvas)).GetComponent<Canvas>();
            source.transform.SetParent(_container.transform, false);
            source.renderMode = RenderMode.WorldSpace;
            source.worldCamera = camera;
            source.transform.localPosition = Vector3.zero;
            source.transform.localScale = Vector3.one;
            var target = (RectTransform)new GameObject("Selection", typeof(RectTransform)).transform;
            target.SetParent(source.transform, false);
            target.sizeDelta = new Vector2(90, 90);
            target.localPosition = new Vector2(-30, 0);

            var destination = new GameObject("Actions canvas", typeof(Canvas)).GetComponent<Canvas>();
            destination.transform.SetParent(_container.transform, false);
            destination.renderMode = RenderMode.WorldSpace;
            var viewport = (RectTransform)destination.transform;
            viewport.sizeDelta = new Vector2(800, 600);
            viewport.localScale = Vector3.one * 1.5f;
            viewport.localPosition = new Vector2(600, 450);
            var panel = (RectTransform)new GameObject("Actions", typeof(RectTransform)).transform;
            panel.SetParent(viewport, false);
            panel.sizeDelta = new Vector2(120, 100);
            var fit = panel.gameObject.AddComponent<ActionPanelViewportFit>();
            fit.SetAvoidanceTarget(target);
            _container.SetActive(true);
            yield return null;
            yield return null;
            Assert.Greater(panel.anchoredPosition.x, 0,
                $"Move right when that is the closer free side. Selection screen position: {camera.WorldToScreenPoint(target.position)}; " +
                $"panel position: {panel.position}; viewport: {viewport.rect}; camera pixels: {camera.pixelRect}.");
            AssertScreenBoundsDoNotOverlap(panel, target, camera);

            target.localPosition = new Vector2(30, 0);
            yield return null;
            Assert.Less(panel.anchoredPosition.x, 0, "Move left when the selection crosses the preferred position.");
            AssertScreenBoundsDoNotOverlap(panel, target, camera);

            source.transform.localScale = Vector3.one * 2;
            camera.orthographicSize = 300;
            yield return null;
            AssertScreenBoundsDoNotOverlap(panel, target, camera);

            camera.transform.position = new Vector3(0, -1000, -10);
            yield return null;
            Assert.AreEqual(Vector2.zero, panel.anchoredPosition,
                "A selection outside the panel's vertical range must not displace the actions.");
        }

        private static void AssertScreenBoundsDoNotOverlap(RectTransform panel, RectTransform target, Camera camera)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var points = corners.Select(camera.WorldToScreenPoint).ToArray();
            var selectedBounds = Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y),
                points.Max(p => p.x), points.Max(p => p.y));
            panel.GetWorldCorners(corners);
            var panelBounds = Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y),
                corners.Max(p => p.x), corners.Max(p => p.y));
            Assert.IsFalse(panelBounds.Overlaps(selectedBounds), $"{panelBounds} covers {selectedBounds}");
        }

        private static void AssertClearOfSelection(RectTransform viewport, RectTransform panel, RectTransform target)
        {
            var panelBounds = BoundsIn(viewport, panel);
            var targetBounds = BoundsIn(viewport, target);
            Assert.IsFalse(panelBounds.Overlaps(targetBounds), $"{panel.name}: {panelBounds} covers {targetBounds}");
            Assert.GreaterOrEqual(panelBounds.xMin, viewport.rect.xMin - 0.1f);
            Assert.LessOrEqual(panelBounds.xMax, viewport.rect.xMax + 0.1f);
            Assert.GreaterOrEqual(panelBounds.yMin, viewport.rect.yMin + 20 - 0.1f);
            Assert.LessOrEqual(panelBounds.yMax, viewport.rect.yMax - 160 + 0.1f);
            Assert.Greater(panel.localScale.x, 0, "Actions must remain visible when there is space beside the selection.");
        }

        private IEnumerator CheckPanel(string prefabName, string panelName)
        {
            var viewer = CreateViewer(prefabName);
            var viewport = (RectTransform)viewer.transform;
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
                         new Vector2(1280, 800), new Vector2(2560, 820), new Vector2(1080, 1920), new Vector2(800, 1920), new Vector2(480, 1920),
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
                if (panelName == "Card Action Panel")
                {
                    foreach (var viewerMode in viewer.GetComponentsInChildren<RectTransform>(true)
                                 .Where(t => t.name is "Minimal" or "Expanded"))
                    {
                        var viewerBounds = BoundsIn(viewport, viewerMode);
                        Assert.AreEqual(viewerBounds.yMin - 20, panelBounds.yMax, 0.1f,
                            $"{prefabName} {viewerMode.name} must leave a small gap above the actions at {size}.");
                    }
                }

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
            longTooltip.enabled = false;
            var serialized = new SerializedObject(longTooltip);
            // This fixture supplies literal stress text instead of the prefab's
            // localized action description. Rebind after changing that contract.
            serialized.FindProperty("localizationKey").stringValue = string.Empty;
            serialized.FindProperty("tooltip").stringValue = string.Join(" ", Enumerable.Repeat(
                "A long action description that must stay on one line", 10));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            longTooltip.enabled = true;
            longTooltip.OnPointerEnter(null);
            yield return null;
            yield return null;
            var longTipObject = (GameObject)typeof(ToolTip).GetField("_toolTipGameObject",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(longTooltip);
            AssertSingleLine(longTipObject);
            StringAssert.EndsWith("…", longTipObject.GetComponentInChildren<UnityEngine.UI.Text>().text);
            Assert.Greater(((RectTransform)longTipObject.transform).rect.width, 500,
                "Long hints should use the panel width before truncating.");
            var longLabel = longTipObject.GetComponentInChildren<UnityEngine.UI.Text>();
            longLabel.fontSize += 4;
            yield return null;
            yield return null;
            AssertSingleLine(longTipObject);
            StringAssert.EndsWith("…", longLabel.text,
                "Font metric changes must recompute truncation even when content and bounds are unchanged.");
            longTooltip.OnPointerExit(null);
        }

        private GameObject CreateViewer(string prefabName)
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
            return viewer;
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
            var points = corners.Select(viewport.InverseTransformPoint).ToArray();
            return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y),
                points.Max(p => p.x), points.Max(p => p.y));
        }
    }
}
#endif
