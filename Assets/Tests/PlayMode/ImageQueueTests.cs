/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class ImageQueueTests
    {
        private sealed class Display : ICardDisplay
        {
            public bool UsesOrientedImage => false;
            public Sprite Received { get; private set; }
            public void SetImageSprite(Sprite imageSprite) => Received = imageSprite;
        }

        private sealed class ControlledQueue : ImageQueueService
        {
            public readonly List<UnityCard> Started = new();
            public readonly HashSet<UnityCard> Finished = new();
            public readonly HashSet<UnityCard> Released = new();
            public UnityCard ThrowFor;
            public Sprite ImageToDeliver;

            protected override IEnumerator LoadImageSprite(UnityCard card)
            {
                Started.Add(card);
                yield return WaitForImage(card);
                card.OnLoadImage(ImageToDeliver);
            }

            private IEnumerator WaitForImage(UnityCard card)
            {
                try
                {
                    if (card == ThrowFor)
                        throw new InvalidOperationException("Simulated image failure");
                    while (!Released.Contains(card))
                        yield return null;
                }
                finally
                {
                    Finished.Add(card);
                }
            }
        }

        private ImageQueueService _original;
        private ControlledQueue _queue;
        private GameObject _runnerObject;
        private ImageQueueTestRunner _runner;
        private readonly Display _display = new();
        private readonly List<UnityCard> _cards = new();

        [SetUp]
        public void SetUp()
        {
            _original = ImageQueueService.Instance;
            _queue = new ControlledQueue();
            SetQueue(_queue);
            _runnerObject = new GameObject("Image queue regression runner");
            _runner = _runnerObject.AddComponent<ImageQueueTestRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            _runner.StopAllCoroutines();
            foreach (var card in _cards)
                card.UnregisterDisplay(_display);
            _cards.Clear();
            UnityEngine.Object.DestroyImmediate(_runnerObject);
            SetQueue(_original);
        }

        private static void SetQueue(ImageQueueService queue)
        {
            typeof(ImageQueueService).GetProperty(nameof(ImageQueueService.Instance),
                BindingFlags.Static | BindingFlags.Public).SetValue(null, queue);
        }

        private UnityCard Show(string id)
        {
            var card = new UnityCard(UnityCardGame.UnityInvalid, id, id, "Set", null, false);
            _cards.Add(card);
            card.RegisterDisplay(_display);
            return card;
        }

        [Test]
        public void LeavingQueuedPageClearsPendingStateAndAllowsRevisit()
        {
            var old = Show("Old page");
            old.UnregisterDisplay(_display);
            Assert.IsFalse(old.IsLoadingImage);
            var current = Show("Current page");
            _queue.ProcessQueue(_runner);
            CollectionAssert.AreEqual(new[] { current }, _queue.Started);

            old.RegisterDisplay(_display);
            old.RegisterDisplay(_display);
            _queue.ProcessQueue(_runner);
            CollectionAssert.AreEqual(new[] { current, old }, _queue.Started);
        }

        [UnityTest]
        public IEnumerator PagingCancelsAllObsoleteActiveLoadsAndStartsVisibleCards()
        {
            var oldPage = new List<UnityCard>();
            for (var i = 0; i < 5; i++)
                oldPage.Add(Show("Old " + i));
            _queue.ProcessQueue(_runner);
            Assert.AreEqual(5, _queue.Started.Count);
            var visible = Show("Visible");
            _queue.ProcessQueue(_runner);
            Assert.AreEqual(5, _queue.Started.Count, "The concurrency limit must still apply.");

            foreach (var card in oldPage)
                card.UnregisterDisplay(_display);
            yield return null;
            yield return null;
            _queue.ProcessQueue(_runner);

            Assert.Contains(visible, _queue.Started);
            foreach (var card in oldPage)
            {
                Assert.IsFalse(card.IsLoadingImage);
                Assert.IsTrue(_queue.Finished.Contains(card), "Nested loaders must be disposed on cancellation.");
            }
        }

        [UnityTest]
        public IEnumerator SharedCardKeepsLoadingUntilItsLastDisplayLeaves()
        {
            var card = Show("Shared");
            var second = new Display();
            card.RegisterDisplay(second);
            _queue.ProcessQueue(_runner);
            card.UnregisterDisplay(_display);
            yield return null;
            Assert.IsTrue(card.IsLoadingImage);
            Assert.IsFalse(_queue.Finished.Contains(card));
            card.UnregisterDisplay(second);
            yield return null;
            yield return null;
            Assert.IsFalse(card.IsLoadingImage);
            Assert.IsTrue(_queue.Finished.Contains(card));
        }

        [Test]
        public void NestedFailureReleasesSlotAndAllowsRetry()
        {
            var failed = Show("Failed");
            _queue.ThrowFor = failed;
            LogAssert.Expect(LogType.Warning, "Failed to load card image: Failed: Simulated image failure");
            _queue.ProcessQueue(_runner);
            Assert.IsFalse(failed.IsLoadingImage);
            Assert.IsTrue(_queue.Finished.Contains(failed));

            _queue.ThrowFor = null;
            failed.RegisterDisplay(_display);
            for (var i = 0; i < 4; i++)
                Show("Next " + i);
            _queue.ProcessQueue(_runner);
            Assert.AreEqual(6, _queue.Started.Count, "A failed load must not permanently consume a slot.");
        }

        [UnityTest]
        public IEnumerator ReturningBeforeCancellationDoesNotDuplicateOrLoseTheLoad()
        {
            var card = Show("Revisited");
            _queue.ProcessQueue(_runner);
            card.UnregisterDisplay(_display);
            card.RegisterDisplay(_display);
            _queue.ProcessQueue(_runner);
            yield return null;
            Assert.AreEqual(1, _queue.Started.Count);
            Assert.IsTrue(card.IsLoadingImage);
            _queue.Released.Add(card);
            yield return null;
            yield return null;
            Assert.IsFalse(card.IsLoadingImage);
        }

        [UnityTest]
        public IEnumerator CompletedLoadDeliversImageToTheRegisteredDisplay()
        {
            var card = Show("Loaded");
            var texture = new Texture2D(2, 3);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 3), Vector2.zero);
            _queue.ImageToDeliver = sprite;
            _queue.Released.Add(card);
            _queue.ProcessQueue(_runner);
            yield return null;
            Assert.AreSame(sprite, _display.Received);
            Assert.AreSame(sprite, card.ImageSprite);
            Assert.IsFalse(card.IsLoadingImage);
        }
    }
}
