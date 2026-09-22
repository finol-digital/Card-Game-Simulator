/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cgs;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class StackLoadingTests
    {
        private readonly List<GameObject> _objects = new();
        private UnityCardGame _previousGame;
        private bool _previousAutoStack;
        private PlayController _controller;
        private UnityCard _firstCard;
        private UnityCard _secondCard;

        [SetUp]
        public void SetUp()
        {
            _previousAutoStack = PlaySettings.AutoStackCards;
            PlaySettings.AutoStackCards = true;
            _previousGame = CardGameManager.Current;
            var game = new UnityCardGame(null, "stack_loading_tests")
            {
                CardSize = new Float2(2, 3), CardProperties = new List<PropertyDef>()
            };
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, game);
            _firstCard = new UnityCard(game, "first", "First", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);
            _secondCard = new UnityCard(game, "second", "Second", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);
            game.Add(_firstCard, false);
            game.Add(_secondCard, false);

            NewObject("NetworkManager").AddComponent<CgsNetManager>();
            var controllerObject = NewObject("PlayController");
            controllerObject.SetActive(false);
            _controller = controllerObject.AddComponent<PlayController>();
            var area = NewObject("PlayArea", typeof(RectTransform), typeof(NetworkObject)).AddComponent<CardZone>();
            SetField(_controller, "playAreaCardZone", area);

            var prefab = NewObject("StackPrefab", typeof(RectTransform), typeof(NetworkObject));
            var stack = prefab.AddComponent<CardStack>();
            SetField(stack, "topCard", prefab.AddComponent<Image>());
            SetField(stack, "deckLabel", NewLabel("DeckLabel", prefab.transform));
            SetField(stack, "countLabel", NewLabel("CountLabel", prefab.transform));
            SetField(_controller, "cardStackPrefab", prefab);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _previousGame);
            PlaySettings.AutoStackCards = _previousAutoStack;
        }

        [Test]
        public void LoadingTwoDecksAtSamePositionCombinesCardsBeforeStart()
        {
            var first = Load(_firstCard, Vector2.zero);
            _controller.CurrentDeckStack = Load(_secondCard, Vector2.zero);

            Assert.AreSame(first, _controller.CurrentDeckStack);
            Assert.AreEqual(1, _controller.AllCardStacks.Count());
            CollectionAssert.AreEqual(new[] { _firstCard, _secondCard }, first.Cards);
            Assert.AreEqual(_secondCard.Id, _controller.CurrentDeckStack.OwnerPopCard());
            Assert.AreEqual(_firstCard.Id, first.Cards.Single().Id);
        }

        [TestCase(1.5f, 0f, 0f, true)]
        [TestCase(2f, 0f, 0f, false)]
        [TestCase(4f, 0f, 0f, false)]
        [TestCase(2.25f, 0f, 90f, true)]
        [TestCase(0f, 2.75f, 90f, false)]
        public void LoadingUsesCardFootprints(float x, float y, float rotation, bool overlaps)
        {
            var first = Load(_firstCard, Vector2.zero);
            var second = Load(_secondCard, new Vector2(x, y) * CardGameManager.PixelsPerInch, rotation);

            Assert.AreEqual(overlaps, first == second);
            Assert.AreEqual(overlaps ? 1 : 2, _controller.AllCardStacks.Count());
            Assert.AreEqual(2, _controller.AllCardStacks.Sum(stack => stack.Cards.Count));
        }

        [Test]
        public void DisabledAutoStackKeepsOverlappingDecksSeparate()
        {
            PlaySettings.AutoStackCards = false;
            var first = Load(_firstCard, Vector2.zero);
            var second = Load(_secondCard, Vector2.zero);

            Assert.AreNotSame(first, second);
            Assert.AreEqual(2, _controller.AllCardStacks.Count());
            CollectionAssert.AreEqual(new[] { _firstCard }, first.Cards);
            CollectionAssert.AreEqual(new[] { _secondCard }, second.Cards);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RequestSettingOverridesLocalSetting(bool requestedAutoStack)
        {
            PlaySettings.AutoStackCards = !requestedAutoStack;
            var first = Load(_firstCard, Vector2.zero);
            var second = _controller.CreateCardStack("Requested deck", new[] { _secondCard }, Vector2.zero,
                Quaternion.identity, false, autoStackCards: requestedAutoStack);

            Assert.AreEqual(requestedAutoStack, first == second);
            Assert.AreEqual(2, _controller.AllCardStacks.Sum(stack => stack.Cards.Count));
        }

        [Test]
        public void LoadingDoesNotAddCardsToADeletingStack()
        {
            var first = Load(_firstCard, Vector2.zero);
            first.RequestDelete();
            var second = Load(_secondCard, Vector2.zero);

            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(new[] { _secondCard }, second.Cards);
        }

        [Test]
        public void LoadingOntoFaceupStackUpdatesTheVisibleTopCard()
        {
            _firstCard.ImageSprite = Sprite.Create(new Texture2D(2, 3), new Rect(0, 0, 2, 3), Vector2.one * 0.5f);
            _secondCard.ImageSprite = Sprite.Create(new Texture2D(2, 3), new Rect(0, 0, 2, 3), Vector2.one * 0.5f);
            var first = Load(_firstCard, Vector2.zero);
            first.IsTopFaceup = true;

            var second = Load(_secondCard, Vector2.zero);

            Assert.AreSame(first, second);
            Assert.IsTrue(first.IsTopFaceup);
            Assert.AreSame(_secondCard.ImageSprite, first.GetComponent<Image>().sprite);
        }

        private CardStack Load(UnityCard card, Vector2 position, float rotation = 0)
        {
            return _controller.CreateCardStack("Deck", new[] { card }, position,
                Quaternion.Euler(0, 0, rotation), false);
        }

        private GameObject NewObject(string name, params System.Type[] components)
        {
            var gameObject = new GameObject(name, components);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static Text NewLabel(string name, Transform parent)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent);
            return label;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
