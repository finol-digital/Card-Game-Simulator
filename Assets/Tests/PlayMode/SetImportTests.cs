/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cgs;
using Cgs.Cards;
using Cgs.Menu;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class SetImportTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly FieldInfo _instance = typeof(CardGameManager).GetField("_instance",
            BindingFlags.Static | BindingFlags.NonPublic);
        private readonly PropertyInfo _current = typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current));
        private object _previousManager;
        private object _previousGame;
        private GameObject _root;
        private UnityCardGame _game;
        private string _folder;
        private SetImportMenu _menu;
        private TMP_Text _preview;
        private Dialog _dialog;
        private int _completions;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Application.temporaryCachePath, "SetImport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _game = new UnityCardGame(null, "SetImport-" + Guid.NewGuid().ToString("N"))
            {
                CardImageFileType = "png"
            };
            _previousManager = _instance.GetValue(null);
            _previousGame = _current.GetValue(null);
            _root = new GameObject("Inactive set import test");
            _root.SetActive(false);
            var manager = _root.AddComponent<CardGameManager>();
            _instance.SetValue(null, manager);
            _current.SetValue(null, _game);
            _dialog = AddChild<Dialog>(_root);
            typeof(CardGameManager).GetField("messenger", PrivateInstance).SetValue(manager, _dialog);
            typeof(CardGameManager).GetField("progress", PrivateInstance)
                .SetValue(manager, AddChild<ProgressBar>(_root));
            _menu = AddChild<SetImportMenu>(_root);
            _preview = AddChild<TextMeshProUGUI>(_root);
            typeof(SetImportMenu).GetField("cardNamesText", PrivateInstance).SetValue(_menu, _preview);
            typeof(SetImportMenu).GetField("importButton", PrivateInstance)
                .SetValue(_menu, AddChild<Button>(_root));
            _completions = 0;
            typeof(SetImportMenu).GetField("_onCreationCallback", PrivateInstance)
                .SetValue(_menu, (UnityAction)(() => _completions++));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            _instance.SetValue(null, _previousManager);
            _current.SetValue(null, _previousGame);
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
            if (Directory.Exists(_game.GameDirectoryPath))
                Directory.Delete(_game.GameDirectoryPath, true);
        }

        [TestCase("png", false)]
        [TestCase("png", true)]
        [TestCase("PNG", true)]
        public void ImportsAndSavesEveryCardInLargeFolder(string imageFileType, bool mixedCase)
        {
            const int cardCount = 300;
            _game.CardImageFileType = imageFileType;
            var texture = new Texture2D(2, 3);
            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            for (var i = 0; i < cardCount; i++)
            {
                var extension = mixedCase ? new[] { "png", "PNG", "PnG" }[i % 3] : "png";
                File.WriteAllBytes(Path.Combine(_folder, $"Card {i:D3}.{extension}"), bytes);
            }
            File.WriteAllText(Path.Combine(_folder, "Ignore.apng"), "Not a PNG card");
            Directory.CreateDirectory(Path.Combine(_folder, "Ignore.png"));

            CompleteImport(CreateImport());

            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_game.Cards.Count, Is.EqualTo(cardCount));
            var names = _preview.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(names.Length, Is.EqualTo(cardCount));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, cardCount).Select(i => $"Card {i:D3}"), names);
            var saved = JArray.Parse(File.ReadAllText(_game.CardsFilePath));
            CollectionAssert.AreEquivalent(_game.Cards.Keys, saved.Select(card => card.Value<string>("id")));
            for (var i = 0; i < cardCount; i++)
            {
                var card = _game.Cards[$"Card_{i:D3}"];
                Assert.That(card.Name, Is.EqualTo($"Card {i:D3}"));
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(card.ImageFilePath));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DuplicateIdPreservesFirstCardAndImageAndReportsRejectedFilename(bool alreadyInGame)
        {
            var firstPath = Path.Combine(_folder, "Same Card.png");
            var secondPath = Path.Combine(_folder, "Same_Card.PNG");
            File.WriteAllBytes(firstPath, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(secondPath, new byte[] { 4, 5, 6 });
            UnityCard original;
            IEnumerator import;
            if (alreadyInGame)
            {
                original = new UnityCard(_game, "Same_Card", "Existing card", Path.GetFileName(_folder), null, false);
                Directory.CreateDirectory(Path.GetDirectoryName(original.ImageFilePath));
                File.WriteAllBytes(original.ImageFilePath, new byte[] { 7, 8, 9 });
                _game.Add(original, false);
                File.Delete(firstPath);
                import = CreateImport();
            }
            else
            {
                import = CreateImport();
                Assert.IsTrue(import.MoveNext()); // Initial UI yield, before enumerating the folder.
                Assert.IsTrue(import.MoveNext()); // Import the first file, in filesystem order.
                original = _game.Cards["Same_Card"];
            }
            var originalBytes = File.ReadAllBytes(original.ImageFilePath);
            var rejectedName = original.Name == "Same_Card" ? "Same Card.png" : "Same_Card.PNG";
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(SetImportMenu.ImportErrorMessage) +
                ".*" + Regex.Escape(rejectedName) + "[\\s\\S]*" +
                Regex.Escape(SetImportMenu.DuplicateCardIdErrorMessage + "Same_Card")));

            CompleteImport(import);

            Assert.That(_game.Cards.Count, Is.EqualTo(1));
            Assert.That(_game.Cards["Same_Card"], Is.SameAs(original));
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(original.ImageFilePath));
            Assert.That(_completions, Is.EqualTo(1));
            var saved = JArray.Parse(File.ReadAllText(_game.CardsFilePath));
            Assert.That(saved.Count, Is.EqualTo(1));
            Assert.That(saved[0].Value<string>("name"), Is.EqualTo(original.Name));
            var warning = GetImportWarning();
            StringAssert.Contains(rejectedName, warning.English);
            StringAssert.Contains(SetImportMenu.DuplicateCardIdErrorMessage + "Same_Card", warning.English);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExistingImageIsPreservedWhenDifferentIdResolvesToSamePath(bool imageBelongsToCard)
        {
            _game.CardBackFaceImageSprites["Blue"] = null;
            var backs = (IList)typeof(SetImportMenu).GetProperty("BackFaceOptions", PrivateInstance).GetValue(_menu);
            backs.Add(new Dropdown.OptionData("Blue"));
            var original = new UnityCard(_game, "A", "Original", Path.GetFileName(_folder), null, false,
                false, "Blue");
            Directory.CreateDirectory(Path.GetDirectoryName(original.ImageFilePath));
            var originalBytes = new byte[] { 7, 8, 9 };
            File.WriteAllBytes(original.ImageFilePath, originalBytes);
            if (imageBelongsToCard)
                _game.Add(original, false);
            var incoming = new UnityCard(_game, "A.Blue", "A.Blue", original.SetCode, null, false, false, "Blue");
            Assert.That(incoming.Id, Is.Not.EqualTo(original.Id));
            Assert.That(incoming.ImageFilePath, Is.EqualTo(original.ImageFilePath));
            File.WriteAllBytes(Path.Combine(_folder, "A.Blue.PNG"), new byte[] { 1, 2, 3 });
            var otherBytes = new byte[] { 4, 5, 6 };
            File.WriteAllBytes(Path.Combine(_folder, "Other.PNG"), otherBytes);
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(SetImportMenu.ImportErrorMessage) +
                ".*A.Blue.PNG[\\s\\S]*" + Regex.Escape(SetImportMenu.ExistingCardImageErrorMessage)));

            CompleteImport(CreateImport());

            Assert.That(_completions, Is.EqualTo(1));
            Assert.That(_game.Cards.ContainsKey(incoming.Id), Is.False);
            Assert.That(_game.Cards.Count, Is.EqualTo(imageBelongsToCard ? 2 : 1));
            if (imageBelongsToCard)
                Assert.That(_game.Cards[original.Id], Is.SameAs(original));
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(original.ImageFilePath));
            CollectionAssert.AreEqual(otherBytes, File.ReadAllBytes(_game.Cards["Other"].ImageFilePath));
            var saved = JArray.Parse(File.ReadAllText(_game.CardsFilePath));
            CollectionAssert.AreEquivalent(_game.Cards.Keys, saved.Select(card => card.Value<string>("id")));
            var warning = GetImportWarning();
            StringAssert.Contains("A.Blue.PNG", warning.English);
            StringAssert.Contains(SetImportMenu.ExistingCardImageErrorMessage, warning.English);
        }

        private Cgs.Localization.UiMessage GetImportWarning()
        {
            var messages = ((IEnumerable)typeof(Dialog).GetProperty("MessageQueue", PrivateInstance)
                .GetValue(_dialog)).Cast<object>().ToArray();
            Assert.That(messages.Length, Is.EqualTo(1));
            var warning = (Cgs.Localization.UiMessage)messages[0].GetType().GetField("Localized").GetValue(messages[0]);
            Assert.That(warning.Key, Is.EqualTo("errors.import"));
            return warning;
        }

        private IEnumerator CreateImport()
        {
            typeof(SetImportMenu).GetProperty("SetFolderPath", PrivateInstance).SetValue(_menu, _folder);
            return (IEnumerator)typeof(SetImportMenu).GetMethod("ImportSet", PrivateInstance).Invoke(_menu, null);
        }

        private static void CompleteImport(IEnumerator import)
        {
            while (import.MoveNext())
            {
                // Execute every step of the production coroutine without activating scene UI.
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
