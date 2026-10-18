/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.Play;
using Cgs.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public sealed class LocalPiecePresentation : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image visual;

        private static readonly List<LocalPiecePresentation> Pieces = new();
        private readonly List<CanvasGroup> _groups = new();
        private readonly Vector3[] _corners = new Vector3[4];
        private Material _material;

        public static IReadOnlyList<LocalPiecePresentation> ActivePieces => Pieces;
        public UnityEngine.UI.Image Visual => visual;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            Pieces.Clear();
        }

        private void OnEnable()
        {
            if (!Pieces.Contains(this))
                Pieces.Add(this);
            PlaySettings.PieceSizeChanged += Refresh;
            Refresh();
            SetSelected(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
        }

        private void OnDisable()
        {
            Pieces.Remove(this);
            PlaySettings.PieceSizeChanged -= Refresh;
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        public void SetSelected(bool selected)
        {
            if (visual == null)
                return;
            // The original playable's material modifier lives on the logical root. The visual
            // child needs its own material so selection never modifies another piece's artwork.
            if (_material == null)
            {
                _material = new Material(visual.material) { hideFlags = HideFlags.DontSave };
                visual.material = _material;
            }
            if (_material.HasProperty(SelectableHighlight.IsSelectedPropertyId))
                _material.SetFloat(SelectableHighlight.IsSelectedPropertyId, selected ? 1f : 0f);
            visual.SetMaterialDirty();
        }

        private void Refresh()
        {
            if (visual != null)
                visual.rectTransform.localScale = Vector3.one *
                    PieceDisplayPolicy.Scale(PlaySettings.PieceSize, PieceDisplayPolicy.IsMobile);
        }

        public bool CanAcquire(Vector2 position, Camera camera)
        {
            if (visual == null || !visual.isActiveAndEnabled || visual.depth < 0 ||
                visual.canvasRenderer.cull || visual.color.a <= 0 || !visual.Raycast(position, camera))
                return false;

            _groups.Clear();
            visual.GetComponentsInParent(false, _groups);
            foreach (var group in _groups)
            {
                if (!group.interactable || !group.blocksRaycasts || group.alpha <= 0)
                    return false;
                if (group.ignoreParentGroups)
                    break;
            }
            return true;
        }

        public bool Contains(Vector2 position, Camera camera, float screenMargin)
        {
            var rect = visual.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, position, camera, out var local))
                return false;
            rect.GetWorldCorners(_corners);
            var origin = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
            var width = Vector2.Distance(origin, RectTransformUtility.WorldToScreenPoint(camera, _corners[3]));
            var height = Vector2.Distance(origin, RectTransformUtility.WorldToScreenPoint(camera, _corners[1]));
            if (width <= 0 || height <= 0)
                return false;
            var bounds = rect.rect;
            bounds.xMin -= screenMargin * rect.rect.width / width;
            bounds.xMax += screenMargin * rect.rect.width / width;
            bounds.yMin -= screenMargin * rect.rect.height / height;
            bounds.yMax += screenMargin * rect.rect.height / height;
            return bounds.Contains(local);
        }
    }
}
