/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Reflection;
using Cgs;
using Cgs.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class SteamDeckTests
    {
        private Gamepad _gamepad;
        private Mouse _mouse;
        private Keyboard _keyboard;
        private GameObject _root;
        private GameObject _events;
        private FloatingTextKeyboard _presenter;
        private FakeKeyboard _backend;

        private sealed class FakeKeyboard : IFloatingTextKeyboard
        {
            public event Action Dismissed;
            public int ShowCount;
            public int HideCount;
            public bool Multiline;
            public bool Succeeds = true;
            public bool AutoDismiss = true;
            public bool Show(bool multiline, RectInt bounds)
            {
                ShowCount++;
                Multiline = multiline;
                Assert.Greater(bounds.width, 0);
                Assert.Greater(bounds.height, 0);
                return Succeeds;
            }
            public void Hide()
            {
                HideCount++;
                if (AutoDismiss)
                    Dismiss();
            }
            public void Dismiss() => Dismissed?.Invoke();
        }

        [SetUp]
        public void SetUp()
        {
            InvokeInput("Reset");
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InvokeInput("Initialize");
            _root = new GameObject("Keyboard test canvas", typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _events = new GameObject("Keyboard test events", typeof(EventSystem));
            _backend = new FakeKeyboard();
            _presenter = _root.AddComponent<FloatingTextKeyboard>();
            _presenter.Configure(_backend);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_events);
            InputSystem.RemoveDevice(_gamepad);
            InputSystem.RemoveDevice(_mouse);
            InputSystem.RemoveDevice(_keyboard);
            InvokeInput("Reset");
            InvokeInput("Initialize");
        }

        private static void InvokeInput(string method) => typeof(ActiveInputDevice)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        [Test]
        public void MouseMotionKeepsControllerPromptsButClicksAndKeysSwitch()
        {
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
            InputSystem.QueueStateEvent(_mouse, new MouseState {position = new Vector2(100, 50), delta = Vector2.one});
            InputSystem.Update();
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
            InputSystem.Update();
            Assert.IsFalse(ActiveInputDevice.UseGamepad);
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Assert.IsFalse(ActiveInputDevice.UseGamepad);
        }

        [Test]
        public void StickMovementRestoresPromptsAndVirtualTypingDoesNotChangeThem()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            using var action = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            action.Enable();
            InputSystem.QueueStateEvent(_gamepad, new GamepadState {leftStick = new Vector2(0.05f, 0)});
            InputSystem.Update();
            Assert.IsFalse(ActiveInputDevice.UseGamepad);
            InputSystem.QueueStateEvent(_gamepad, new GamepadState {leftStick = Vector2.right});
            InputSystem.Update();
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
            ActiveInputDevice.IsOnScreenKeyboardOpen = true;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
        }

        [Test]
        public void DeckHasControllerPromptsEvenBeforeSteamInputConnects()
        {
            InputSystem.RemoveDevice(_gamepad);
            ActiveInputDevice.EnableSteamDeck();
            Assert.IsTrue(ActiveInputDevice.UseGamepad);
            Assert.AreEqual("A / B / X / Y / L1 / R2 / Menu", ActiveInputDevice.FormatBinding(
                "Button South / Button East / Button West / Button North / LB / RT / Start"));
        }

        [Test]
        public void KeyboardBoundsUseTopLeftClientCoordinatesAndClampToScreen()
        {
            Assert.AreEqual(new RectInt(100, 300, 300, 100),
                FloatingTextKeyboard.ToKeyboardBounds(new Vector2(100, 400), new Vector2(400, 500), 1280, 800));
            Assert.AreEqual(new RectInt(0, 0, 1280, 800),
                FloatingTextKeyboard.ToKeyboardBounds(new Vector2(-20, -30), new Vector2(1400, 900), 1280, 800));
        }

        private InputField CreateLegacyField()
        {
            var fieldObject = new GameObject("Player Name", typeof(RectTransform), typeof(InputField));
            fieldObject.transform.SetParent(_root.transform, false);
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(fieldObject.transform, false);
            var field = fieldObject.GetComponent<InputField>();
            field.textComponent = label.GetComponent<Text>();
            field.textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return field;
        }

        private static void Focus(InputField field)
        {
            EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
        }

        [UnityTest]
        public IEnumerator LegacyFieldOpensOnceAndCanBeRefocusedAfterDismissal()
        {
            var field = CreateLegacyField();
            Focus(field);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _backend.ShowCount);
            Assert.IsTrue(ActiveInputDevice.IsOnScreenKeyboardOpen);
            _backend.Dismiss();
            yield return null;
            yield return null;
            Assert.AreEqual(1, _backend.ShowCount);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Focus(field);
            yield return null;
            yield return null;
            Assert.AreEqual(2, _backend.ShowCount);
            field.gameObject.SetActive(false);
            yield return null;
            Assert.AreEqual(1, _backend.HideCount);
            Assert.IsFalse(ActiveInputDevice.IsOnScreenKeyboardOpen);
        }

        [UnityTest]
        public IEnumerator ReadOnlyAndFailedRequestsDoNotSpamTheKeyboard()
        {
            var field = CreateLegacyField();
            field.readOnly = true;
            Focus(field);
            yield return null;
            yield return null;
            Assert.AreEqual(0, _backend.ShowCount);
            field.readOnly = false;
            _backend.Succeeds = false;
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(1, _backend.ShowCount);
            Assert.IsFalse(ActiveInputDevice.IsOnScreenKeyboardOpen);
        }

        [UnityTest]
        public IEnumerator SwitchingFieldsWaitsForDismissalAndSupportsTmpMultiline()
        {
            var first = CreateLegacyField();
            Focus(first);
            yield return null;
            yield return null;
            _backend.AutoDismiss = false;
            var secondObject = new GameObject("Card search", typeof(RectTransform), typeof(TMP_InputField));
            secondObject.transform.SetParent(_root.transform, false);
            var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(secondObject.transform, false);
            var second = secondObject.GetComponent<TMP_InputField>();
            second.textViewport = (RectTransform)secondObject.transform;
            second.textComponent = label.GetComponent<TextMeshProUGUI>();
            second.lineType = TMP_InputField.LineType.MultiLineNewline;
            EventSystem.current.SetSelectedGameObject(secondObject);
            second.ActivateInputField();
            yield return null;
            yield return null;
            Assert.AreEqual(1, _backend.HideCount);
            Assert.AreEqual(1, _backend.ShowCount);
            _backend.Dismiss();
            yield return null;
            yield return null;
            Assert.AreEqual(2, _backend.ShowCount);
            Assert.IsTrue(_backend.Multiline);
        }
    }
}
