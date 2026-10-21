/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class PieceLayeringTests
    {
        private GameObject _canvas;
        private GameObject _events;
        private EventSystem _previousEvents;
        private CardZone _table;

        [SetUp]
        public void SetUp()
        {
            _previousEvents = EventSystem.current;
            _events = new GameObject("Layering events", typeof(EventSystem));
            EventSystem.current = _events.GetComponent<EventSystem>();
            _canvas = new GameObject("Layering canvas", typeof(RectTransform), typeof(Canvas),
                typeof(PieceGraphicRaycaster));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.GetComponent<Canvas>().sortingOrder = 32000;
            _table = new GameObject("Table", typeof(RectTransform), typeof(CardZone)).GetComponent<CardZone>();
            _table.transform.SetParent(_canvas.transform, false);
            ((RectTransform)_table.transform).sizeDelta = new Vector2(1000, 1000);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_events);
            if (_previousEvents != null)
                EventSystem.current = _previousEvents;
        }

        [UnityTest]
        public IEnumerator CardsAndStacksCreatedAfterPiecesStayBehindThem()
        {
            var die = Create("Die");
            var counter = Create("Counter");
            var card = Create("Card Model");
            var stack = Create("Card Stack");
            yield return null;

            AssertOrder(card, stack, die, counter);
            AssertTopHit(counter);
        }

        [UnityTest]
        public IEnumerator MovingCardsAndStacksToFrontKeepsPiecesVisible()
        {
            var card = Create("Card Model");
            var stack = Create("Card Stack");
            var die = Create("Die");
            var counter = Create("Counter");
            yield return null;

            card.transform.SetAsLastSibling();
            yield return null;
            AssertOrder(stack, card, die, counter);
            AssertTopHit(counter);

            stack.transform.SetAsLastSibling();
            yield return null;
            AssertOrder(card, stack, die, counter);
            AssertTopHit(counter);
        }

        [UnityTest]
        public IEnumerator SynchronizedSiblingChangesKeepPiecesAboveCards()
        {
            var card = Create("Card Model");
            var stack = Create("Card Stack");
            var die = Create("Die");
            var counter = Create("Counter");
            yield return null;

            card.OnChangeSiblingIndex(0, 3);
            counter.OnChangeSiblingIndex(2, 0);
            yield return null;

            AssertOrder(stack, card, counter, die);
            AssertTopHit(die);
        }

        [UnityTest]
        public IEnumerator DroppingCardBackOnTablePreservesPieceGeometryAndOrder()
        {
            var die = Create("Die");
            var card = Create("Card Model");
            var counter = Create("Counter");
            var diePosition = die.transform.localPosition = new Vector3(20, 30, 0);
            var counterPosition = counter.transform.localPosition = new Vector3(-20, -30, 0);
            var dieSize = ((RectTransform)die.transform).sizeDelta;
            card.transform.SetParent(_canvas.transform, false);
            yield return null;

            card.transform.SetParent(_table.transform, false);
            yield return null;

            AssertOrder(card, die, counter);
            Assert.AreEqual(diePosition, die.transform.localPosition);
            Assert.AreEqual(counterPosition, counter.transform.localPosition);
            Assert.AreEqual(dieSize, ((RectTransform)die.transform).sizeDelta);

            die.transform.SetAsLastSibling();
            yield return null;
            AssertOrder(card, counter, die);
        }

        [UnityTest]
        public IEnumerator RowAndColumnZonesKeepExplicitSiblingOrder()
        {
            _table.Type = CardZoneType.Horizontal;
            var die = Create("Die");
            var card = Create("Card Model");
            var counter = Create("Counter");
            yield return null;
            AssertOrder(die, card, counter);

            _table.Type = CardZoneType.Vertical;
            card.OnChangeSiblingIndex(1, 2);
            yield return null;
            AssertOrder(die, counter, card);
        }

        [UnityTest]
        public IEnumerator ChangingRowOrColumnToAreaReordersExistingPieces()
        {
            var die = Create("Die");
            var card = Create("Card Model");
            var counter = Create("Counter");
            var stack = Create("Card Stack");
            foreach (var type in new[] { CardZoneType.Horizontal, CardZoneType.Vertical })
            {
                _table.Type = type;
                die.transform.SetSiblingIndex(0);
                card.transform.SetSiblingIndex(1);
                counter.transform.SetSiblingIndex(2);
                yield return null;
                AssertOrder(die, card, counter, stack);

                _table.Type = CardZoneType.Area;
                yield return null;
                AssertOrder(card, stack, die, counter);
                AssertTopHit(counter);
            }
        }

        [UnityTest]
        public IEnumerator SynchronizedAreaSnapshotReordersExistingPieces()
        {
            _table.Type = CardZoneType.Horizontal;
            var die = Create("Die");
            var card = Create("Card Model");
            yield return null;
            AssertOrder(die, card);

            var variable = (NetworkVariable<int>)typeof(CardZone)
                .GetField("_typeNetworkVariable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_table);
            using (var source = new NetworkVariable<int>((int)CardZoneType.Area))
            using (var writer = new FastBufferWriter(16, Allocator.Temp))
            {
                source.WriteField(writer);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                variable.ReadField(reader);
            }
            _table.OnNetworkSpawn();
            yield return null;

            Assert.AreEqual(CardZoneType.Area, _table.Type);
            AssertOrder(card, die);
            AssertTopHit(die);
        }

        [UnityTest]
        public IEnumerator TablePiecesRemainBelowOverlayControls()
        {
            Create("Counter");
            Create("Card Stack");
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(_canvas.transform, false);
            ((RectTransform)overlay.transform).sizeDelta = new Vector2(400, 400);
            yield return null;

            var hits = RaycastAt(overlay.transform.position);
            Assert.AreEqual(overlay, hits[0].gameObject);
        }

        private CgsNetPlayable Create(string name)
        {
            var holder = new GameObject("Inactive prefab holder");
            holder.SetActive(false);
            holder.transform.SetParent(_canvas.transform, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/CardGameView/Multiplayer/{name}.prefab");
            Assert.IsNotNull(prefab, $"Missing prefab: {name}");
            var instance = Object.Instantiate(prefab, holder.transform);
            var playable = instance.GetComponent<CgsNetPlayable>();
            playable.enabled = false;
            instance.transform.SetParent(_table.transform, false);
            instance.transform.localPosition = Vector3.zero;
            Object.DestroyImmediate(holder);
            return playable;
        }

        private void AssertOrder(params CgsNetPlayable[] playables)
        {
            for (var i = 0; i < playables.Length; i++)
                Assert.AreEqual(i, playables[i].transform.GetSiblingIndex(), playables[i].name);
        }

        private void AssertTopHit(CgsNetPlayable expected)
        {
            var hits = RaycastAt(expected.transform.position);
            Assert.IsNotEmpty(hits);
            Assert.AreSame(expected, hits[0].gameObject.GetComponentInParent<CgsNetPlayable>());
        }

        private List<RaycastResult> RaycastAt(Vector2 position)
        {
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            return hits;
        }
    }
}
#endif
