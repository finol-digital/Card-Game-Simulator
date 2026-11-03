/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Reflection;
using Cgs;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class CardViewerPreviewTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private GameObject _root;
        private CardViewer _viewer;
        private CardModel _model;
        private CanvasGroup _preview;
        private Text _name;
        private Text _id;
        private UnityCardGame _game;
        private UnityCardGame _previousGame;
        private object _previousManager;
        private object _previousViewer;
        private bool _hadPreviewSetting;
        private int _previousPreviewSetting;

        [SetUp]
        public void SetUp()
        {
            _hadPreviewSetting = PlayerPrefs.HasKey("PreviewOnMouseOver");
            _previousPreviewSetting = PlayerPrefs.GetInt("PreviewOnMouseOver");
            Settings.PreviewOnMouseOver = true;
            _previousGame = CardGameManager.Current;
            _previousManager = typeof(CardGameManager).GetField("_instance", PrivateStatic).GetValue(null);
            _previousViewer = typeof(CardViewer).GetField("_instance", PrivateStatic).GetValue(null);
            _game = new UnityCardGame(null, "hover_preview_tests")
            {
                CardSize = new Float2(2, 3), CardProperties = new List<PropertyDef>()
            };
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _game);
            _root = new GameObject("Hover lifecycle tests", typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var managerObject = Child("Inactive manager");
            managerObject.SetActive(false);
            typeof(CardGameManager).GetField("_instance", PrivateStatic)
                .SetValue(null, managerObject.AddComponent<CardGameManager>());

            var viewerObject = Child("Viewer");
            viewerObject.SetActive(false);
            _viewer = viewerObject.AddComponent<CardViewer>();
            _viewer.enabled = false; // Invoke the real frame callbacks without scene/input initialization.
            var viewerRect = (RectTransform)viewerObject.transform;
            viewerRect.anchorMin = Vector2.zero;
            viewerRect.anchorMax = Vector2.one;
            viewerRect.sizeDelta = Vector2.zero;
            _preview = Child("Preview", viewerObject.transform).AddComponent<CanvasGroup>();
            var zoom = Child("Zoom", viewerObject.transform);
            zoom.SetActive(false);
            _name = Child("Preview name", viewerObject.transform).AddComponent<Text>();
            _id = Child("Preview id", viewerObject.transform).AddComponent<Text>();
            SetField(_viewer, "preview", _preview);
            SetField(_viewer, "zoomPanel", zoom.transform);
            SetField(_viewer, "previewNameText", new List<Text> { _name });
            SetField(_viewer, "previewIdText", new List<Text> { _id });
            SetField(_viewer, "cardImages", new List<Image>());
            SetField(_viewer, "nameTexts", new List<Text>());
            SetField(_viewer, "uniqueIdTexts", new List<Text>());
            SetField(_viewer, "propertyValueTexts", new List<Text>());
            typeof(CardViewer).GetField("_instance", PrivateStatic).SetValue(null, _viewer);
            viewerObject.SetActive(true);

            var modelObject = Child("Card");
            modelObject.SetActive(false);
            modelObject.AddComponent<Image>();
            modelObject.AddComponent<NetworkObject>();
            _model = modelObject.AddComponent<CardModel>();
            var label = Child("Card name", modelObject.transform).AddComponent<Text>();
            SetField(_model, "nameLabel", label.gameObject);
            SetField(_model, "nameText", label);
            modelObject.SetActive(true);
            _model.Value = Card("first", "First card");
            _model.Value.ImageSprite = Artwork();
            Canvas.ForceUpdateCanvases();
            _viewer.PreviewCardModel = _model;
        }

        [TearDown]
        public void TearDown()
        {
            if (_viewer != null)
                _viewer.HidePreview();
            if (_model != null)
                Object.DestroyImmediate(_model.gameObject);
            Object.DestroyImmediate(_root);
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _previousGame);
            typeof(CardGameManager).GetField("_instance", PrivateStatic).SetValue(null, _previousManager);
            typeof(CardViewer).GetField("_instance", PrivateStatic).SetValue(null, _previousViewer);
            if (_hadPreviewSetting)
                PlayerPrefs.SetInt("PreviewOnMouseOver", _previousPreviewSetting);
            else
                PlayerPrefs.DeleteKey("PreviewOnMouseOver");
        }

        [Test]
        public void LiveHoverSurvivesUpdateAndLateUpdate()
        {
            Tick();
            Tick();
            Assert.AreSame(_model, _viewer.PreviewCardModel);
            Assert.AreEqual(1, _preview.alpha);
            Assert.IsTrue(Overlay.enabled);
        }

        [Test]
        public void PreviewToggleResumesHoverWithoutAnotherPointerEnter()
        {
            Settings.PreviewOnMouseOver = false;
            Tick();
            Assert.AreSame(_model, _viewer.PreviewCardModel);
            Assert.AreEqual(0, _preview.alpha);
            Assert.IsFalse(Overlay.enabled);
            Settings.PreviewOnMouseOver = true;
            Tick();
            Assert.AreEqual(1, _preview.alpha);
            Assert.IsTrue(Overlay.enabled);
        }

        [Test]
        public void PointerExitWhileSuppressedPreventsPreviewFromReturning()
        {
            Settings.PreviewOnMouseOver = false;
            Tick();
            _model.OnPointerExit(new PointerEventData(null));
            Settings.PreviewOnMouseOver = true;
            Tick();
            Assert.IsNull(_viewer.PreviewCardModel);
            Assert.AreEqual(0, _preview.alpha);
            Assert.IsFalse(Overlay.enabled);
        }

        [Test]
        public void DisabledCardIsForgottenEvenWhilePreviewIsSuppressed()
        {
            Settings.PreviewOnMouseOver = false;
            Tick();
            _model.gameObject.SetActive(false);
            Tick();
            Assert.IsNull(_viewer.PreviewCardModel);
            Assert.IsFalse(Overlay.enabled);
        }

        [Test]
        public void ChangingHoveredCardRefreshesLabelsAndArtwork()
        {
            var previous = _model.Value;
            var replacement = Card("second", "Second card");
            _model.Value = replacement;
            replacement.ImageSprite = Artwork();
            Tick();
            Assert.AreEqual(replacement.Name, _name.text);
            Assert.AreEqual(replacement.Id, _id.text);
            Assert.AreSame(replacement.OrientedImageSprite, Overlay.sprite);
            Assert.IsFalse(previous.HasImageDisplays, "The viewer must release the old card's image subscription.");
        }

        private Image Overlay => _viewer.transform.Find("Card Hover Preview").GetComponent<Image>();

        private void Tick()
        {
            typeof(CardViewer).GetMethod("Update", PrivateInstance).Invoke(_viewer, null);
            typeof(CardViewer).GetMethod("LateUpdate", PrivateInstance).Invoke(_viewer, null);
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

        private static Sprite Artwork()
        {
            return Sprite.Create(new Texture2D(2, 3), new Rect(0, 0, 2, 3), Vector2.one * 0.5f);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }
    }
}
