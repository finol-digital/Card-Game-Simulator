/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Cgs.CardGameView
{
    public sealed class PieceGraphicRaycaster : UnityEngine.UI.GraphicRaycaster
    {
        public const float TouchMargin = 12f;
        private Canvas _canvas;
        private readonly List<RaycastResult> _hits = new();

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            // The input module raycasts before restoring button state. Its eventData.dragging
            // can describe the last mouse button, so consult the playable's per-pointer registry too.
            if (eventData.dragging || CgsNetPlayable.IsPointerDragging(eventData.pointerId))
            {
                base.Raycast(eventData, resultAppendList);
                return;
            }

            if (_canvas == null)
                _canvas = GetComponent<Canvas>();
            _hits.Clear();
            base.Raycast(eventData, _hits);
            var camera = eventCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_canvas.transform,
                    eventData.position, camera))
                return;
            for (var i = _hits.Count - 1; i >= 0; i--)
                if (_hits[i].gameObject.TryGetComponent<LocalPiecePresentation>(out var localPiece) &&
                    !localPiece.CanAcquire(eventData.position, camera))
                    _hits.RemoveAt(i);
            var touch = eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Touch;
            var margin = touch ? TouchMargin * _canvas.rootCanvas.scaleFactor : 0f;
            LocalPiecePresentation assisted = null;
            var nearest = float.PositiveInfinity;

            foreach (var piece in LocalPiecePresentation.ActivePieces)
            {
                if (piece.Visual == null || piece.Visual.canvas != _canvas ||
                    !piece.CanAcquire(eventData.position, camera))
                    continue;
                if (piece.Contains(eventData.position, camera, 0))
                {
                    _hits.Add(Hit(piece, eventData.position));
                    continue;
                }
                if (!touch || !piece.Contains(eventData.position, camera, margin))
                    continue;
                var center = RectTransformUtility.WorldToScreenPoint(camera, piece.Visual.rectTransform.position);
                var distance = (center - eventData.position).sqrMagnitude;
                if (distance < nearest || (Mathf.Approximately(distance, nearest) &&
                    assisted != null && piece.Visual.depth > assisted.Visual.depth))
                {
                    assisted = piece;
                    nearest = distance;
                }
            }

            if (assisted != null)
            {
                foreach (var hit in _hits)
                {
                    // Ancestor surfaces are the table/viewport behind the candidate, not direct
                    // interactive content. Everything else retains its normal blocking behavior.
                    if (!assisted.transform.IsChildOf(hit.gameObject.transform))
                    {
                        assisted = null;
                        break;
                    }
                }
                if (assisted != null)
                    _hits.Add(Hit(assisted, eventData.position));
            }

            foreach (var hit in _hits)
            {
                var result = hit;
                result.index = resultAppendList.Count;
                resultAppendList.Add(result);
            }
        }

        private RaycastResult Hit(LocalPiecePresentation piece, Vector2 position)
        {
            return new RaycastResult
            {
                gameObject = piece.gameObject,
                module = this,
                screenPosition = position,
                depth = piece.Visual.depth,
                sortingLayer = _canvas.sortingLayerID,
                sortingOrder = _canvas.sortingOrder,
                displayIndex = _canvas.targetDisplay,
                worldPosition = piece.transform.position,
                worldNormal = -piece.transform.forward
            };
        }
    }
}
