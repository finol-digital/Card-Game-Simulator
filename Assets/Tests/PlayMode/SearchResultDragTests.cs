/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cgs;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class SearchResultDragTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const float PruneWaitSeconds = 1f;

        private GameObject _root;
        private GameObject _eventsObject;
        private RectTransform _results;
        private UnityCardGame _game;
        private UnityCardGame _previousGame;
        private object _previousManager;
        private object _previousViewer;

        [SetUp]
        public void SetUp()
        {
            _previousGame = CardGameManager.Current;
            _previousManager = typeof(CardGameManager).GetField("_instance", PrivateStatic).GetValue(null);
            _previousViewer = typeof(CardViewer).GetField("_instance", PrivateStatic).GetValue(null);
            _game = new UnityCardGame(null, "search_result_drag_tests")
            {
                CardSize = new Float2(2, 3), CardProperties = new List<PropertyDef>()
            };
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _game);

            _eventsObject = new GameObject("Search result drag events", typeof(EventSystem));
            _root = new GameObject("Search result drag tests", typeof(Canvas));
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var managerObject = Child("Inactive manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<CardGameManager>();
            manager.CardCanvases.Add(canvas);
            typeof(CardGameManager).GetField("_instance", PrivateStatic).SetValue(null, manager);

            var viewerObject = Child("Inactive viewer");
            viewerObject.SetActive(false);
            var viewer = viewerObject.AddComponent<CardViewer>();
            SetField(viewer, "preview", Child("Preview", viewerObject.transform).AddComponent<CanvasGroup>());
            typeof(CardViewer).GetField("_instance", PrivateStatic).SetValue(null, viewer);

            _results = (RectTransform)Child("Search results").transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_eventsObject);
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _previousGame);
            typeof(CardGameManager).GetField("_instance", PrivateStatic).SetValue(null, _previousManager);
            typeof(CardViewer).GetField("_instance", PrivateStatic).SetValue(null, _previousViewer);
        }

        [UnityTest]
        public IEnumerator DraggingSearchResultIntoDeckKeepsSourceCard()
        {
            var source = CreateCardModel(_results);
            source.DoesCloneOnDrag = true;
            var zone = Child("Deck zone");
            zone.AddComponent<NetworkObject>().AutoObjectParentSync = false;
            var cardZone = zone.AddComponent<CardZone>();
            cardZone.Type = CardZoneType.Vertical;
            yield return null;

            var eventData = new PointerEventData(EventSystem.current)
            {
                pointerId = 0,
                position = RectTransformUtility.WorldToScreenPoint(null, source.transform.position)
            };
            source.OnPointerDown(eventData);
            source.OnBeginDrag(eventData);
            var clone = CardModel.GetPointerDrag(eventData);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(source, clone);
            Assert.IsEmpty(source.PointerPositions);

            cardZone.OnPointerEnter(eventData);
            Assert.AreSame(cardZone, clone.PlaceHolderCardZone);
            clone.OnEndDrag(eventData);

            var elapsed = 0f;
            while (elapsed < PruneWaitSeconds || (clone != null && clone.transform.parent != zone.transform))
            {
                Assert.Less(elapsed, PruneWaitSeconds * 5);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(source != null);
            Assert.AreSame(_results, source.transform.parent);
            Assert.IsFalse(source.ToDelete);
            Assert.IsTrue(clone != null);
            Assert.AreSame(zone.transform, clone.transform.parent);
        }

        [UnityTest]
        public IEnumerator MovingWithoutRecoverablePlaceHolderDeletesCard()
        {
            var cardModel = CreateCardModel(_results);
            yield return null;

            typeof(CardModel).GetProperty("IsMovingToPlaceHolder", PrivateInstance).SetValue(cardModel, true);
            LogAssert.Expect(LogType.Warning, $"FinishMovingToPlaceHolder Lost PlaceHolder for {cardModel.name}!");
            yield return null;
            yield return null;

            Assert.IsTrue(cardModel == null);
        }

        private CardModel CreateCardModel(Transform parent)
        {
            var modelObject = Child("Card", parent);
            modelObject.SetActive(false);
            modelObject.AddComponent<Image>();
            modelObject.AddComponent<Outline>();
            modelObject.AddComponent<CanvasGroup>();
            modelObject.AddComponent<BoxCollider2D>();
            modelObject.AddComponent<NetworkObject>().AutoObjectParentSync = false;
            var cardModel = modelObject.AddComponent<CardModel>();
            var label = Child("Card name", modelObject.transform).AddComponent<Text>();
            SetField(cardModel, "nameLabel", label.gameObject);
            SetField(cardModel, "nameText", label);
            modelObject.SetActive(true);
            cardModel.Value = Card("search_result", "Search result");
            return cardModel;
        }

        private GameObject Child(string name, Transform parent = null)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent != null ? parent : _root.transform, false);
            return child;
        }

        private UnityCard Card(string id, string name)
        {
            var card = new UnityCard(_game, id, name, Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);
            _game.Add(card, false);
            return card;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }
    }
}
