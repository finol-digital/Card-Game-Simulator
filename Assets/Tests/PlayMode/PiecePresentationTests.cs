/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class PiecePresentationTests
    {
        private GameObject _canvasObject;
        private GameObject _events;
        private PieceGraphicRaycaster _raycaster;
        private EventSystem _previousEvents;
        private RectTransform _table;
        private PieceDisplaySize _oldSize;
        private bool _hadPreference;
        private int _rawPreference;
        private readonly List<RaycastResult> _hits = new();
        private Touchscreen _touchscreen;
        private InputSystemUIInputModule _module;
        private InputActionAsset _actions;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.UpdateMode _updateMode;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadPreference = PlayerPrefs.HasKey("DiceAndCounterDisplaySize");
            _rawPreference = PlayerPrefs.GetInt("DiceAndCounterDisplaySize");
            _oldSize = PlaySettings.PieceSize;
            _backgroundBehavior = InputSystem.settings.backgroundBehavior;
            _updateMode = InputSystem.settings.updateMode;
            _editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            PlaySettings.PieceSize = PieceDisplaySize.Standard;
            _previousEvents = EventSystem.current;
            _events = new GameObject("Piece test events", typeof(EventSystem));
            EventSystem.current = _events.GetComponent<EventSystem>();
            _canvasObject = new GameObject("Piece test canvas", typeof(RectTransform), typeof(Canvas),
                typeof(PieceGraphicRaycaster));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            _raycaster = _canvasObject.GetComponent<PieceGraphicRaycaster>();
            _table = new GameObject("Table", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _table.SetParent(_canvasObject.transform, false);
            _table.sizeDelta = new Vector2(2000, 2000);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_touchscreen != null)
                InputSystem.RemoveDevice(_touchscreen);
            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
            InputSystem.settings.updateMode = _updateMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
            Object.DestroyImmediate(_canvasObject);
            Object.DestroyImmediate(_events);
            if (_actions != null)
                Object.DestroyImmediate(_actions);
            if (_previousEvents != null)
                EventSystem.current = _previousEvents;
            PlaySettings.PieceSize = _oldSize;
            if (_hadPreference)
                PlayerPrefs.SetInt("DiceAndCounterDisplaySize", _rawPreference);
            else
                PlayerPrefs.DeleteKey("DiceAndCounterDisplaySize");
        }

        [TestCase(PieceDisplaySize.Auto, false, 1f)]
        [TestCase(PieceDisplaySize.Auto, true, 1.5f)]
        [TestCase(PieceDisplaySize.Standard, true, 1f)]
        [TestCase(PieceDisplaySize.Large, false, 1.5f)]
        [TestCase(PieceDisplaySize.ExtraLarge, false, 2f)]
        [TestCase((PieceDisplaySize)99, false, 1f)]
        public void PolicyResolvesDefaultsAndOverrides(PieceDisplaySize choice, bool mobile, float expected)
        {
            Assert.AreEqual(expected, PieceDisplayPolicy.Scale(choice, mobile));
        }

        [Test]
        public void PreferencePersistsAndInvalidValuesFallBack()
        {
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            Assert.AreEqual(3, PlayerPrefs.GetInt("DiceAndCounterDisplaySize"));
            Assert.AreEqual(PieceDisplaySize.ExtraLarge, PlaySettings.PieceSize);
            PlayerPrefs.SetInt("DiceAndCounterDisplaySize", -7);
            Assert.AreEqual(PieceDisplaySize.Auto, PlaySettings.PieceSize);
        }

        [TestCase("Die")]
        [TestCase("Counter")]
        public void RealPrefabKeepsLogicalGeometryAcrossRefreshAndReparent(string name)
        {
            var holder = new GameObject("Inactive holder");
            holder.SetActive(false);
            holder.transform.SetParent(_table, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/CardGameView/Multiplayer/{name}.prefab");
            var piece = Object.Instantiate(prefab, holder.transform);
            foreach (var behaviour in piece.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is not LocalPiecePresentation && behaviour is not UnityEngine.EventSystems.UIBehaviour)
                    behaviour.enabled = false;
            var root = piece.GetComponent<RectTransform>();
            var logicalSize = root.sizeDelta;
            var collider = piece.GetComponent<BoxCollider2D>();
            var colliderSize = collider != null ? collider.size : Vector2.zero;
            var presentation = piece.GetComponent<LocalPiecePresentation>();
            Assert.IsNotNull(presentation);
            holder.SetActive(true);
            foreach (var choice in new[] { PieceDisplaySize.Large, PieceDisplaySize.ExtraLarge, PieceDisplaySize.Standard })
            {
                PlaySettings.PieceSize = choice;
                Assert.AreEqual(Vector3.one * PieceDisplayPolicy.Scale(choice, false), presentation.Visual.transform.localScale);
                Assert.AreEqual(logicalSize, root.sizeDelta);
                Assert.AreEqual(Vector3.one, root.localScale);
                if (collider != null)
                    Assert.AreEqual(colliderSize, collider.size);
            }
            PlaySettings.PieceSize = PieceDisplaySize.Large;
            root.SetParent(_table, false);
            piece.SetActive(false);
            piece.SetActive(true);
            Assert.AreEqual(Vector3.one * 1.5f, presentation.Visual.transform.localScale);
            Assert.IsFalse(presentation.Visual.raycastTarget, "Visuals must not intercept canonical drops.");
        }

        [UnityTest]
        public IEnumerator TouchMarginDoesNotChangeMouseHitsOrCanonicalDrops()
        {
            var piece = Piece(Vector2.zero);
            yield return null;
            var position = ScreenPoint(piece, new Vector2(58, 0));
            Assert.AreEqual(piece.gameObject, Hit(position, true));
            Assert.AreEqual(_table.gameObject, Hit(position, false));
            Assert.AreEqual(_table.gameObject, Hit(position, true, true));
            PlaySettings.PieceSize = PieceDisplaySize.Large;
            position = ScreenPoint(piece, new Vector2(70, 0));
            Assert.AreEqual(piece.gameObject, Hit(position, false));
            Assert.AreEqual(_table.gameObject, Hit(position, false, true));
            Assert.AreEqual(piece.gameObject, Hit(ScreenPoint(piece, Vector2.zero), true, true));
        }

        [UnityTest]
        public IEnumerator DirectContentWinsAndAssistedTiesUseRenderOrder()
        {
            var left = Piece(new Vector2(-60, 0));
            var right = Piece(new Vector2(60, 0));
            yield return null;
            var middle = ScreenPoint(left, new Vector2(60, 0));
            Assert.AreEqual(right.gameObject, Hit(middle, true));
            Assert.AreEqual(left.gameObject, Hit(middle + Vector2.left * 2, true));
            var card = new GameObject("Direct card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_table, false);
            ((RectTransform)card.transform).sizeDelta = new Vector2(10, 10);
            yield return null;
            Assert.AreEqual(card, Hit(middle, true));
        }

        [UnityTest]
        public IEnumerator DropCanReachTargetUnderEnlargedVisual()
        {
            var target = Piece(new Vector2(100, 0));
            var enlarged = Piece(Vector2.zero);
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            yield return null;
            var position = ScreenPoint(enlarged, new Vector2(80, 0));
            Assert.AreEqual(enlarged.gameObject, Hit(position, true));
            Assert.AreEqual(target.gameObject, Hit(position, true, true));
        }

        [UnityTest]
        public IEnumerator InvisibleAndBlockedPiecesCannotAcquireTouches()
        {
            var piece = Piece(Vector2.zero);
            yield return null;
            var position = ScreenPoint(piece, new Vector2(58, 0));
            var group = piece.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            Assert.AreEqual(_table.gameObject, Hit(position, true));
            Assert.AreEqual(_table.gameObject, Hit(ScreenPoint(piece, Vector2.zero), true));
            group.interactable = true;
            group.alpha = 0;
            Assert.AreEqual(_table.gameObject, Hit(position, true));
            group.alpha = 1;
            group.blocksRaycasts = false;
            Assert.AreEqual(_table.gameObject, Hit(position, true));
            piece.gameObject.SetActive(false);
            Assert.AreEqual(_table.gameObject, Hit(position, true));
        }

        [UnityTest]
        public IEnumerator ViewportClippingAndOverlayBlockAssistance()
        {
            var piece = Piece(Vector2.zero);
            _table.sizeDelta = new Vector2(100, 100);
            _table.gameObject.AddComponent<RectMask2D>();
            yield return null;
            Assert.IsNull(Hit(ScreenPoint(piece, new Vector2(58, 0)), true));
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(_canvasObject.transform, false);
            ((RectTransform)overlay.transform).sizeDelta = new Vector2(200, 200);
            yield return null;
            Assert.AreEqual(overlay, Hit(ScreenPoint(piece, Vector2.zero), true));
        }

        [UnityTest]
        public IEnumerator MarginRetainsScreenSizeWithRotatedAndZoomedTable()
        {
            var piece = Piece(Vector2.zero);
            _table.localScale = Vector3.one * 0.5f;
            _table.localRotation = Quaternion.Euler(0, 0, 90);
            yield return null;
            var edge = ScreenPoint(piece, new Vector2(50, 0));
            Assert.AreEqual(piece.gameObject, Hit(edge + Vector2.up * 10, true));
            Assert.AreEqual(_table.gameObject, Hit(edge + Vector2.up * 14, true));
            Assert.AreEqual(_table.gameObject, Hit(edge + Vector2.up * 10, false));
        }

        [UnityTest]
        public IEnumerator PlayableDragRegistryKeepsDropCanonicalEvenWhenEventStateIsStale()
        {
            var source = Piece(new Vector2(-200, 0));
            var target = Piece(Vector2.zero);
            var playable = source.gameObject.AddComponent<PieceDragProbe>();
            yield return null;
            var press = new ExtendedPointerEventData(EventSystem.current)
            {
                pointerId = 41, pointerType = UIPointerType.Touch,
                position = ScreenPoint(source, new Vector2(58, 0))
            };
            playable.OnPointerDown(press);
            playable.OnBeginDrag(press);
            Assert.IsTrue(CgsNetPlayable.IsPointerDragging(41));
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            var position = ScreenPoint(target, new Vector2(80, 0));
            Assert.AreEqual(_table.gameObject, Hit(position, true, false, 41));
            Assert.AreEqual(target.gameObject, Hit(position, true, false, 42), "Another finger must still acquire pieces.");
            playable.OnEndDrag(press);
            Assert.IsFalse(CgsNetPlayable.IsPointerDragging(41));
            Assert.AreEqual(target.gameObject, Hit(position, true, false, 41));
        }

        [Test]
        public void ZoneOverlapDoesNotFollowLocalVisualScale()
        {
            var piece = Piece(new Vector2(100, 0));
            var dieCollider = piece.gameObject.AddComponent<BoxCollider2D>();
            dieCollider.size = new Vector2(100, 100);
            var zone = new GameObject("Zone", typeof(RectTransform), typeof(BoxCollider2D));
            zone.transform.SetParent(_table, false);
            ((RectTransform)zone.transform).anchoredPosition = new Vector2(185, 0);
            var zoneCollider = zone.GetComponent<BoxCollider2D>();
            zoneCollider.size = new Vector2(50, 50);
            Physics2D.SyncTransforms();
            var before = zoneCollider.Distance(dieCollider);
            Assert.IsFalse(before.isOverlapped);
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            Physics2D.SyncTransforms();
            var after = zoneCollider.Distance(dieCollider);
            Assert.IsFalse(after.isOverlapped);
            Assert.AreEqual(before.distance, after.distance);
        }

        [UnityTest]
        public IEnumerator InputModuleCapturesOriginalPlayableAndDropsOnlyOnCanonicalTarget()
        {
            var source = Piece(new Vector2(-180, 0));
            var target = Piece(Vector2.zero);
            var sourceProbe = source.gameObject.AddComponent<PieceDragProbe>();
            var targetProbe = target.gameObject.AddComponent<PieceDragProbe>();
            // Keep the die and its assisted touch target inside a 640-wide CI viewport.
            var die = RealPiece<Die>("Die", new Vector2(180, 0));
            source.name = "Source piece";
            target.name = "Target piece";
            PlaySettings.PieceSize = PieceDisplaySize.Large;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            _touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(_touchscreen);
            _module = _events.AddComponent<InputSystemUIInputModule>();
            _module.enabled = false;
            // Own actions keep this test independent of project actions and the Editor mouse.
            // Wildcard bindings deliver each finger through the real UI input module.
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = _actions.AddActionMap("UI");
            var point = map.AddAction("Point", InputActionType.PassThrough, "<Touchscreen>/touch*/position");
            var click = map.AddAction("Click", InputActionType.PassThrough, "<Touchscreen>/touch*/press");
            _module.actionsAsset = _actions;
            _module.point = InputActionReference.Create(point);
            _module.leftClick = InputActionReference.Create(click);
            _actions.devices = new InputDevice[] { _touchscreen };
            _module.enabled = true;
            yield return null;

            var press = ScreenPoint(source, new Vector2(-80, 0));
            var visualOnly = ScreenPoint(target, new Vector2(70, 0));
            Assert.AreEqual(source.gameObject, Hit(press, true), "The synthetic press must be inside the displayed viewport.");
            Assert.IsTrue(_module.leftClick.action.enabled);
            Assert.IsTrue(_touchscreen.enabled);
            QueueTouch(1, UnityEngine.InputSystem.TouchPhase.Began, press);
            _hits.Clear();
            EventSystem.current.RaycastAll(new ExtendedPointerEventData(EventSystem.current)
                { position = press, pointerType = UIPointerType.Touch }, _hits);
            Assert.AreEqual(1, sourceProbe.Presses,
                $"One press must reach the original playable. Device pressed={_touchscreen.primaryTouch.press.isPressed}, " +
                $"point={_module.point.action.ReadValue<Vector2>()}, expected={press}, " +
                $"click={_module.leftClick.action.ReadValue<float>()}, enabled={sourceProbe.isActiveAndEnabled}, " +
                $"lastHit={_module.GetLastRaycastResult(_touchscreen.deviceId).gameObject}, " +
                $"focus={EventSystem.current.isFocused}, background={InputSystem.settings.backgroundBehavior}, " +
                $"module={EventSystem.current.currentInputModule}, " +
                $"hits={string.Join(",", _hits.Select(hit => hit.gameObject.name + ":" + hit.depth))}");
            QueueTouch(1, UnityEngine.InputSystem.TouchPhase.Moved, visualOnly);
            Assert.AreEqual(1, sourceProbe.DragStarts);
            QueueTouch(1, UnityEngine.InputSystem.TouchPhase.Ended, visualOnly);
            Assert.AreEqual(0, targetProbe.Drops, "Enlarged visuals cannot become drop targets.");
            Assert.AreEqual(1, sourceProbe.DragEnds);
            Assert.IsFalse(CgsNetPlayable.IsAnyDragging);

            QueueTouch(2, UnityEngine.InputSystem.TouchPhase.Began, press);
            var canonical = ScreenPoint(target, Vector2.zero);
            QueueTouch(2, UnityEngine.InputSystem.TouchPhase.Moved, canonical);
            QueueTouch(2, UnityEngine.InputSystem.TouchPhase.Ended, canonical);
            Assert.AreEqual(1, targetProbe.Drops);
            Assert.AreEqual(source.gameObject, targetProbe.DroppedObject);
            Assert.AreEqual(2, sourceProbe.DragStarts);
            Assert.AreEqual(2, sourceProbe.DragEnds);
            Assert.IsFalse(CgsNetPlayable.IsAnyDragging);

            QueueTouch(3, UnityEngine.InputSystem.TouchPhase.Began, press);
            QueueTouch(3, UnityEngine.InputSystem.TouchPhase.Moved, canonical);
            QueueTouch(3, UnityEngine.InputSystem.TouchPhase.Canceled, canonical);
            Assert.IsFalse(CgsNetPlayable.IsAnyDragging, "Cancelled touches must release existing pointer capture.");

            var secondPress = ScreenPoint(target, new Vector2(80, 0));
            QueueTouch(4, UnityEngine.InputSystem.TouchPhase.Began, press);
            QueueTouch(5, UnityEngine.InputSystem.TouchPhase.Began, secondPress);
            QueueTouch(4, UnityEngine.InputSystem.TouchPhase.Moved, canonical);
            QueueTouch(5, UnityEngine.InputSystem.TouchPhase.Moved, secondPress + Vector2.right * 100);
            Assert.AreEqual(4, sourceProbe.DragStarts);
            Assert.AreEqual(1, targetProbe.DragStarts);
            Assert.IsTrue(CgsNetPlayable.IsPointerDragging(sourceProbe.CurrentPointerEventData.pointerId));
            Assert.IsTrue(CgsNetPlayable.IsPointerDragging(targetProbe.CurrentPointerEventData.pointerId));
            QueueTouch(4, UnityEngine.InputSystem.TouchPhase.Ended, canonical);
            Assert.IsTrue(CgsNetPlayable.IsAnyDragging, "Releasing one finger must retain the other drag.");
            QueueTouch(5, UnityEngine.InputSystem.TouchPhase.Ended, secondPress + Vector2.right * 100);
            Assert.IsFalse(CgsNetPlayable.IsAnyDragging);

            // Start/Update require the game singleton; dispatch synchronously before another frame.
            die.enabled = true;
            Canvas.ForceUpdateCanvases();
            var diePress = ScreenPoint(die.GetComponent<LocalPiecePresentation>(), new Vector2(80, 0));
            Assert.AreEqual(die.gameObject, Hit(diePress, true),
                $"The die tap must hit its assisted target inside the {Screen.width}x{Screen.height} viewport.");
            var remaining = typeof(Die).GetField("_rollRemainingTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hadRollPreference = PlayerPrefs.HasKey("DoubleClickToRollDice");
            var rollPreference = PlayerPrefs.GetInt("DoubleClickToRollDice");
            try
            {
                PlaySettings.DoubleClickToRollDice = true;
                QueueTouch(6, UnityEngine.InputSystem.TouchPhase.Began, diePress);
                QueueTouch(6, UnityEngine.InputSystem.TouchPhase.Ended, diePress);
                Assert.AreEqual(die.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual(0f, remaining.GetValue(die), "The first tap selects without duplicate roll dispatch.");
                QueueTouch(7, UnityEngine.InputSystem.TouchPhase.Began, diePress);
                QueueTouch(7, UnityEngine.InputSystem.TouchPhase.Ended, diePress);
                Assert.Greater((float)remaining.GetValue(die), 0f);
                remaining.SetValue(die, 0f);
                PlaySettings.DoubleClickToRollDice = false;
                QueueTouch(8, UnityEngine.InputSystem.TouchPhase.Began, diePress);
                QueueTouch(8, UnityEngine.InputSystem.TouchPhase.Ended, diePress);
                Assert.AreEqual(0f, remaining.GetValue(die), "Touch assistance must respect the roll preference.");
            }
            finally
            {
                die.enabled = false;
                if (hadRollPreference)
                    PlayerPrefs.SetInt("DoubleClickToRollDice", rollPreference);
                else
                    PlayerPrefs.DeleteKey("DoubleClickToRollDice");
            }
        }

        private void QueueTouch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(_touchscreen, new TouchState { touchId = id, phase = phase, position = position });
            // The public Update() chooses Editor updates when Game View lacks focus, even
            // with IgnoreFocus enabled. Explicitly drive the player update in this Editor test.
            typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static, null, new[] { typeof(InputUpdateType) }, null)
                .Invoke(null, new object[] { InputUpdateType.Manual });
            _module.Process();
        }

        [UnityTest]
        public IEnumerator RealCounterDropOnlyMergesInsideOriginalBounds()
        {
            var source = RealPiece<Counter>("Counter", new Vector2(-200, 0));
            var target = RealPiece<Counter>("Counter", Vector2.zero);
            source.Value = 3;
            target.Value = 4;
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            yield return null;
            var presentation = target.GetComponent<LocalPiecePresentation>();
            var outside = ScreenPoint(presentation, new Vector2(80, 0));
            var data = new PointerEventData(EventSystem.current)
            {
                position = outside, dragging = true, pointerDrag = source.gameObject
            };
            ExecuteEvents.ExecuteHierarchy(Hit(outside, true, true), data, ExecuteEvents.dropHandler);
            Assert.AreEqual(3, source.Value);
            Assert.AreEqual(4, target.Value);
            Assert.IsFalse(target.ToDelete);
            data.position = ScreenPoint(presentation, Vector2.zero);
            ExecuteEvents.ExecuteHierarchy(Hit(data.position, true, true), data, ExecuteEvents.dropHandler);
            Assert.AreEqual(7, source.Value);
            Assert.IsTrue(target.ToDelete);
        }

        [Test]
        public void RealDiceZoneMembershipIgnoresVisualOverlap()
        {
            var die = RealPiece<Die>("Die", new Vector2(100, 0));
            die.Value = 4;
            var zoneObject = new GameObject("Dice zone", typeof(RectTransform), typeof(BoxCollider2D));
            zoneObject.transform.SetParent(_table, false);
            var zone = zoneObject.AddComponent<DiceZone>();
            zone.enabled = false;
            zoneObject.GetComponent<BoxCollider2D>().size = new Vector2(50, 50);
            ((RectTransform)zoneObject.transform).anchoredPosition = new Vector2(185, 0);
            var update = typeof(DiceZone).GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var choice in new[] { PieceDisplaySize.Standard, PieceDisplaySize.ExtraLarge })
            {
                PlaySettings.PieceSize = choice;
                Physics2D.SyncTransforms();
                update.Invoke(zone, null);
                Assert.AreEqual(0, zone.DiceInZone.Count);
            }
            ((RectTransform)zoneObject.transform).anchoredPosition = new Vector2(160, 0);
            Physics2D.SyncTransforms();
            update.Invoke(zone, null);
            Assert.AreEqual(1, zone.DiceInZone.Count);
            Assert.AreEqual(4, zone.DiceInZone.Sum(item => item.Value));
            PlaySettings.PieceSize = PieceDisplaySize.Standard;
            update.Invoke(zone, null);
            Assert.AreEqual(die, zone.DiceInZone.Single());
        }

        [Test]
        public void SelectionHighlightsOnlyTheSelectedVisual()
        {
            var first = RealPiece<Die>("Die", Vector2.zero);
            var second = RealPiece<Die>("Die", Vector2.right * 150);
            var update = typeof(CgsNetPlayable).GetMethod("UpdateSelectableHighlight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            update.Invoke(first, new object[] { true });
            var firstMaterial = first.GetComponent<LocalPiecePresentation>().Visual.material;
            var secondMaterial = second.GetComponent<LocalPiecePresentation>().Visual.material;
            Assert.AreNotSame(firstMaterial, secondMaterial);
            Assert.AreEqual(1f, firstMaterial.GetFloat(Cgs.UI.SelectableHighlight.IsSelectedPropertyId));
            Assert.AreEqual(0f, secondMaterial.GetFloat(Cgs.UI.SelectableHighlight.IsSelectedPropertyId));
            PlaySettings.PieceSize = PieceDisplaySize.ExtraLarge;
            Assert.AreEqual(1f, firstMaterial.GetFloat(Cgs.UI.SelectableHighlight.IsSelectedPropertyId));
            update.Invoke(first, new object[] { false });
            Assert.AreEqual(0f, firstMaterial.GetFloat(Cgs.UI.SelectableHighlight.IsSelectedPropertyId));
        }

        private T RealPiece<T>(string name, Vector2 position) where T : CgsNetPlayable
        {
            var holder = new GameObject("Inactive prefab holder");
            holder.SetActive(false);
            holder.transform.SetParent(_table, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/CardGameView/Multiplayer/{name}.prefab");
            var root = Object.Instantiate(prefab, holder.transform);
            var playable = root.GetComponent<T>();
            // Exercise real event handlers without the game singleton or rolling in Start.
            playable.enabled = false;
            ((RectTransform)root.transform).anchoredPosition = position;
            holder.SetActive(true);
            if (playable is Counter counter)
                root.GetComponent<CounterDropArea>().DropHandler = counter;
            return playable;
        }

        private LocalPiecePresentation Piece(Vector2 position)
        {
            var root = new GameObject("Piece", typeof(RectTransform), typeof(Image));
            root.SetActive(false);
            root.transform.SetParent(_table, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(100, 100);
            rect.anchoredPosition = position;
            root.GetComponent<Image>().color = Color.clear;
            root.GetComponent<CanvasRenderer>().cullTransparentMesh = false;
            var child = new GameObject("Visual", typeof(RectTransform), typeof(Image));
            child.transform.SetParent(root.transform, false);
            var childRect = (RectTransform)child.transform;
            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.sizeDelta = Vector2.zero;
            child.GetComponent<Image>().raycastTarget = false;
            var piece = root.AddComponent<LocalPiecePresentation>();
            var serialized = new SerializedObject(piece);
            serialized.FindProperty("visual").objectReferenceValue = child.GetComponent<Image>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            Canvas.ForceUpdateCanvases();
            return piece;
        }

        private GameObject Hit(Vector2 position, bool touch, bool dragging = false, int pointer = 1)
        {
            Canvas.ForceUpdateCanvases();
            _hits.Clear();
            _raycaster.Raycast(new ExtendedPointerEventData(EventSystem.current)
            {
                position = position, pointerId = pointer, dragging = dragging,
                pointerType = touch ? UIPointerType.Touch : UIPointerType.MouseOrPen
            }, _hits);
            return _hits.OrderByDescending(hit => hit.depth).Select(hit => hit.gameObject).FirstOrDefault();
        }

        private static Vector2 ScreenPoint(LocalPiecePresentation piece, Vector2 local)
        {
            return RectTransformUtility.WorldToScreenPoint(null, piece.transform.TransformPoint(local));
        }
    }

    public class PieceDragProbe : CgsNetPlayable, IDropHandler
    {
        public int Presses { get; private set; }
        public int DragStarts { get; private set; }
        public int DragEnds { get; private set; }
        public int Drops { get; private set; }
        public GameObject DroppedObject { get; private set; }
        protected override void OnPointerDownPlayable(PointerEventData eventData) { Presses++; }
        protected override void OnBeginDragPlayable(PointerEventData eventData) { DragStarts++; }
        protected override void OnDragPlayable(PointerEventData eventData) { }
        protected override void OnEndDragPlayable(PointerEventData eventData) { DragEnds++; }
        protected override void PostDragPlayable(PointerEventData eventData) { }
        public void OnDrop(PointerEventData eventData)
        {
            Drops++;
            DroppedObject = eventData.pointerDrag;
        }
    }
}
#endif
