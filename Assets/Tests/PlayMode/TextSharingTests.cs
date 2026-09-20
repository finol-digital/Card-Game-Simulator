/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using Cgs.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class TextSharingTests
    {
        private static string Copy(string text, Action<string> write, Func<string> read)
        {
            var method = typeof(TextSharing).GetMethod("Copy", BindingFlags.Static | BindingFlags.NonPublic);
            return (string)method.Invoke(null, new object[] { text, "Deck copied.", write, read });
        }

        [TestCase("Deck: 日本語\n4 cards", "Deck: 日本語\n4 cards")]
        [TestCase("first\nsecond", "first\r\nsecond")]
        public void CopyReportsSuccessOnlyAfterReadingBackTheText(string text, string clipboardText)
        {
            var written = false;
            var result = Copy(text, value =>
            {
                Assert.AreEqual(text, value);
                written = true;
            }, () =>
            {
                Assert.IsTrue(written);
                return clipboardText;
            });

            Assert.AreEqual("Deck copied.", result);
        }

        [Test]
        public void SilentClipboardFailureDoesNotReportSuccess()
        {
            Assert.AreEqual(TextSharing.CopyErrorMessage, Copy("new text", _ => { }, () => "old text"));
        }

        [Test]
        public void WriteExceptionReportsFailureWithoutReadingClipboard()
        {
            var result = Copy("text", _ => throw new InvalidOperationException("Clipboard busy"), () =>
            {
                Assert.Fail("Do not read after a failed write.");
                return "text";
            });
            Assert.AreEqual(TextSharing.CopyErrorMessage, result);
        }

        [Test]
        public void ReadExceptionReportsUnconfirmedCopy()
        {
            Assert.AreEqual(TextSharing.CopyUnconfirmedMessage,
                Copy("text", _ => { }, () => throw new InvalidOperationException("Read denied")));
        }

        [TestCase(null)]
        [TestCase("")]
        public void EmptyTextDoesNotClearClipboard(string text)
        {
            Assert.AreEqual(TextSharing.EmptyMessage, Copy(text,
                _ => Assert.Fail("Do not overwrite the clipboard with empty text."),
                () => throw new InvalidOperationException("Do not read for empty text.")));
        }

        [Test]
        public void EmptyShareReportsFeedbackOnce()
        {
            var calls = 0;
            TextSharing.CopyOrShare(null, feedback =>
            {
                calls++;
                Assert.AreEqual(TextSharing.EmptyMessage, feedback);
            });
            Assert.AreEqual(1, calls);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        [Test]
        public void RepeatedDialogCopyPreservesMessageAndDoesNotQueueFeedback()
        {
            var previousClipboard = GUIUtility.systemCopyBuffer;
            var dialogObject = new GameObject("Copy feedback dialog");
            try
            {
                dialogObject.SetActive(false);
                var dialog = dialogObject.AddComponent<Dialog>();
                var message = AddChild<UnityEngine.UI.Text>(dialogObject, "Message");
                SetField(dialog, "messageText", message);
                SetField(dialog, "yesButton", AddChild<UnityEngine.UI.Button>(dialogObject, "Yes"));
                SetField(dialog, "noButton", AddChild<UnityEngine.UI.Button>(dialogObject, "No"));
                var copyButton = AddChild<UnityEngine.UI.Button>(dialogObject, "Copy").gameObject;
                SetField(dialog, "copyButton", copyButton);
                SetField(dialog, "shareButton", AddChild<UnityEngine.UI.Button>(dialogObject, "Share").gameObject);
                var accepted = false;
                dialog.Prompt("Original message", () => accepted = true, true);

                dialog.CopyShare();
                Assert.AreEqual("Original message", GUIUtility.systemCopyBuffer);
                Assert.AreEqual(TextSharing.CopiedMessage + "\n\nOriginal message", message.text);
                dialog.CopyShare();
                Assert.AreEqual("Original message", GUIUtility.systemCopyBuffer);
                Assert.AreEqual(TextSharing.CopiedMessage + "\n\nOriginal message", message.text);
                dialog.IgnoreableClose();
                Assert.IsTrue(dialogObject.activeSelf, "Copying must preserve the unskippable prompt.");

                var yes = (UnityEngine.UI.Button)typeof(Dialog).GetField("yesButton",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dialog);
                yes.onClick.Invoke();
                Assert.IsTrue(accepted, "Copying must preserve the original prompt action.");
                Assert.IsFalse(dialogObject.activeSelf, "Copy feedback must not leave queued dialogs.");

                dialog.Show("Next message");
                dialog.CopyShare();
                Assert.AreEqual("Next message", GUIUtility.systemCopyBuffer);
                Assert.AreEqual(TextSharing.CopiedMessage + "\n\nNext message", message.text);
                dialog.OkClose();

                dialog.ShowStatus(TextSharing.CopiedMessage);
                Assert.IsFalse(copyButton.activeSelf);
                dialog.CopyShare();
                Assert.AreEqual("Next message", GUIUtility.systemCopyBuffer,
                    "A status notice must not overwrite the clipboard, even via the keyboard shortcut.");
                dialog.OkClose();
                dialog.Show("Another message");
#if !UNITY_STANDALONE_OSX || UNITY_EDITOR
                Assert.IsTrue(copyButton.activeSelf, "Normal messages must restore the copy button.");
#endif
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dialogObject);
                GUIUtility.systemCopyBuffer = previousClipboard;
            }
        }

        private static T AddChild<T>(GameObject parent, string name) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform);
            return child.AddComponent<T>();
        }

        private static void SetField(Dialog dialog, string name, object value)
        {
            typeof(Dialog).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dialog, value);
        }
#endif
    }
}
