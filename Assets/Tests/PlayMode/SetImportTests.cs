/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Cgs;
using Cgs.Cards;
using Cgs.Menu;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class SetImportTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase("png", false)]
        [TestCase("png", true)]
        [TestCase("PNG", true)]
        public void ImportsAndSavesEveryCardInLargeFolder(string imageFileType, bool mixedCase)
        {
            const int cardCount = 300;
            var folder = Path.Combine(Application.temporaryCachePath, "SetImport-" + Guid.NewGuid().ToString("N"));
            var game = new UnityCardGame(null, "SetImport-" + Guid.NewGuid().ToString("N"))
            {
                CardImageFileType = imageFileType
            };
            var instance = typeof(CardGameManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousManager = instance.GetValue(null);
            var current = typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current));
            var previousGame = current.GetValue(null);
            var root = new GameObject("Inactive set import test");
            root.SetActive(false);
            try
            {
                Directory.CreateDirectory(folder);
                var texture = new Texture2D(2, 3);
                var bytes = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                for (var i = 0; i < cardCount; i++)
                {
                    var extension = mixedCase ? new[] { "png", "PNG", "PnG" }[i % 3] : "png";
                    File.WriteAllBytes(Path.Combine(folder, $"Card {i:D3}.{extension}"), bytes);
                }
                File.WriteAllText(Path.Combine(folder, "Ignore.apng"), "Not a PNG card");
                Directory.CreateDirectory(Path.Combine(folder, "Ignore.png"));

                var manager = root.AddComponent<CardGameManager>();
                instance.SetValue(null, manager);
                current.SetValue(null, game);
                typeof(CardGameManager).GetField("messenger", PrivateInstance)
                    .SetValue(manager, AddChild<Dialog>(root));
                typeof(CardGameManager).GetField("progress", PrivateInstance)
                    .SetValue(manager, AddChild<ProgressBar>(root));
                var menu = AddChild<SetImportMenu>(root);
                var preview = AddChild<TextMeshProUGUI>(root);
                typeof(SetImportMenu).GetField("cardNamesText", PrivateInstance).SetValue(menu, preview);
                typeof(SetImportMenu).GetField("importButton", PrivateInstance)
                    .SetValue(menu, AddChild<Button>(root));
                typeof(SetImportMenu).GetProperty("SetFolderPath", PrivateInstance).SetValue(menu, folder);

                var completions = 0;
                typeof(SetImportMenu).GetField("_onCreationCallback", PrivateInstance)
                    .SetValue(menu, (UnityAction)(() => completions++));
                var import = (IEnumerator)typeof(SetImportMenu).GetMethod("ImportSet", PrivateInstance)
                    .Invoke(menu, null);
                // Execute every step of the production coroutine without activating scene UI.
                while (import.MoveNext()) { }

                Assert.That(completions, Is.EqualTo(1));
                Assert.That(game.Cards.Count, Is.EqualTo(cardCount));
                var names = preview.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.That(names.Length, Is.EqualTo(cardCount));
                CollectionAssert.AreEquivalent(Enumerable.Range(0, cardCount).Select(i => $"Card {i:D3}"), names);
                var saved = JArray.Parse(File.ReadAllText(game.CardsFilePath));
                CollectionAssert.AreEquivalent(game.Cards.Keys, saved.Select(card => card.Value<string>("id")));
                for (var i = 0; i < cardCount; i++)
                {
                    var card = game.Cards[$"Card_{i:D3}"];
                    Assert.That(card.Name, Is.EqualTo($"Card {i:D3}"));
                    CollectionAssert.AreEqual(bytes, File.ReadAllBytes(card.ImageFilePath));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                instance.SetValue(null, previousManager);
                current.SetValue(null, previousGame);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
                if (Directory.Exists(game.GameDirectoryPath))
                    Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        private static T AddChild<T>(GameObject parent) where T : Component
        {
            var child = new GameObject(typeof(T).Name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<T>();
        }
    }
}
