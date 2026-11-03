/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cgs.CardGameView.Viewer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class CardHoverPreviewTests
    {
        private GameObject _canvasObject;
        private GameObject _eventsObject;
        private RectTransform _bounds;
        private Image _source;
        private Image _overlay;
        private CardHoverPreview _preview;
        private Sprite _sprite;
        private Texture2D _texture;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Preview test canvas", typeof(Canvas), typeof(GraphicRaycaster));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _bounds = (RectTransform)_canvasObject.transform;
            _eventsObject = new GameObject("Preview test events", typeof(EventSystem));
            var sourceObject = new GameObject("Card", typeof(RectTransform), typeof(Image));
            sourceObject.transform.SetParent(_bounds, false);
            _source = sourceObject.GetComponent<Image>();
            _texture = new Texture2D(2, 3);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 2, 3), Vector2.one * 0.5f);
            _source.sprite = _sprite;
            _source.rectTransform.sizeDelta = new Vector2(100, 140);
            _preview = new CardHoverPreview(_bounds);
            _overlay = _bounds.Find("Card Hover Preview").GetComponent<Image>();
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvasObject);
            Object.DestroyImmediate(_eventsObject);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [UnityTest]
        public IEnumerator EnlargementPreservesCardLayoutAndPointerTargets()
        {
            var position = _source.rectTransform.anchoredPosition;
            var size = _source.rectTransform.sizeDelta;
            var sibling = _source.transform.GetSiblingIndex();
            _preview.Show(_source);
            yield return null; // Populate the canvas's graphic depths before raycasting.
            Canvas.ForceUpdateCanvases();

            Assert.Greater(_overlay.rectTransform.rect.width, size.x);
            Assert.Greater(_overlay.rectTransform.rect.height, size.y);
            Assert.AreEqual(position, _source.rectTransform.anchoredPosition);
            Assert.AreEqual(size, _source.rectTransform.sizeDelta);
            Assert.AreEqual(Vector3.one, _source.transform.localScale);
            Assert.AreEqual(sibling, _source.transform.GetSiblingIndex());

            var hits = new List<RaycastResult>();
            var data = new PointerEventData(_eventsObject.GetComponent<EventSystem>())
            {
                position = RectTransformUtility.WorldToScreenPoint(null, _source.transform.position)
            };
            var raycaster = _canvasObject.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(data, hits);
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(_source.gameObject, hits[0].gameObject);
            hits.Clear();
            data.position = RectTransformUtility.WorldToScreenPoint(null,
                _source.transform.TransformPoint(new Vector3(60, 0, 0)));
            raycaster.Raycast(data, hits);
            Assert.IsEmpty(hits, "The enlarged artwork must not become a new drag or drop target.");
        }

        [Test]
        public void UpdateDoesNotRedisplayAnAlreadyHiddenViewer()
        {
            var viewerObject = new GameObject("Idle viewer", typeof(RectTransform));
            viewerObject.SetActive(false);
            viewerObject.transform.SetParent(_bounds, false);
            var viewer = viewerObject.AddComponent<CardViewer>();
            var preview = viewerObject.AddComponent<CanvasGroup>();
            preview.alpha = 1;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CardViewer).GetField("preview", flags).SetValue(viewer, preview);
            typeof(CardViewer).GetField("zoomPanel", flags).SetValue(viewer, viewerObject.transform);

            // Exercise Update alone; the live hover lifecycle is covered in CardViewerPreviewTests.
            typeof(CardViewer).GetMethod("Update", flags).Invoke(viewer, null);

            Assert.AreEqual(1, preview.alpha, "Update must not redisplay an already hidden viewer.");
            Assert.IsFalse(viewer.IsVisible);
            Assert.IsNull(viewer.SelectedCardModel);
        }

        [Test]
        public void MissingArtworkHidesOverlayUntilImageLoads()
        {
            _source.sprite = null;
            _preview.Show(_source);
            Assert.IsFalse(_overlay.enabled);
            _source.sprite = _sprite;
            _preview.Refresh();
            Assert.IsTrue(_overlay.enabled);
            Assert.AreSame(_sprite, _overlay.sprite);
            _source.sprite = null;
            _preview.Refresh();
            Assert.IsFalse(_overlay.enabled);
            Assert.IsNull(_overlay.sprite);
        }

        [TestCase(0)]
        [TestCase(45)]
        [TestCase(90)]
        public void PreviewStaysInsideViewportNearEveryCorner(float rotation)
        {
            _source.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            var bounds = _bounds.rect;
            foreach (var x in new[] { bounds.xMin, bounds.xMax })
            foreach (var y in new[] { bounds.yMin, bounds.yMax })
            {
                _source.rectTransform.localPosition = new Vector3(x, y, 0);
                _preview.Show(_source);
                var corners = new Vector3[4];
                _overlay.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = _bounds.InverseTransformPoint(corner);
                    Assert.That(local.x, Is.InRange(bounds.xMin - 0.01f, bounds.xMax + 0.01f));
                    Assert.That(local.y, Is.InRange(bounds.yMin - 0.01f, bounds.yMax + 0.01f));
                }
            }
        }

        [Test]
        public void HiddenOrRemovedCardsCannotLeaveAnEnlargedImageBehind()
        {
            _preview.Show(_source);
            Assert.IsTrue(_overlay.enabled);
            _preview.Hide();
            _preview.Refresh();
            Assert.IsFalse(_overlay.enabled);
            _preview.Show(_source);
            _source.gameObject.SetActive(false);
            _preview.Refresh();
            Assert.IsFalse(_overlay.enabled);
            _source.gameObject.SetActive(true);
            _preview.Show(_source);
            Object.DestroyImmediate(_source.gameObject);
            _preview.Refresh();
            Assert.IsFalse(_overlay.enabled);
        }
    }
}
