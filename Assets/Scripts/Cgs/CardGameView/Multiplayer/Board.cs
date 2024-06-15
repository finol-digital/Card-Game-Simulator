/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public class Board : CgsNetPlayable
    {
        public string GameBoardId
        {
            get => IsSpawned ? _idNetworkVariable.Value : _gameBoardId;
            set
            {
                _gameBoardId = value;
                if (IsSpawned)
                    _idNetworkVariable.Value = value;
            }
        }

        private string _gameBoardId = string.Empty;
        private NetworkVariable<CgsNetString> _idNetworkVariable;

        public Vector2 Size
        {
            get => IsSpawned ? _sizeNetworkVariable.Value : _size;
            set
            {
                _size = value;
                if (IsSpawned)
                    _sizeNetworkVariable.Value = value;
            }
        }

        private Vector2 _size;
        private NetworkVariable<Vector2> _sizeNetworkVariable;

        protected override void OnAwakePlayable()
        {
            _idNetworkVariable = new NetworkVariable<CgsNetString>();
            _sizeNetworkVariable = new NetworkVariable<Vector2>();
        }

        protected override void OnStartPlayable()
        {
            var rectTransform = (RectTransform) transform;

            rectTransform.localScale = Vector3.one;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.offsetMin =
                new Vector2(Position.x, Position.y) * CardGameManager.PixelsPerInch;
            rectTransform.offsetMax =
                new Vector2(Size.x, Size.y) * CardGameManager.PixelsPerInch +
                rectTransform.offsetMin;

            var boardFilepath = CardGameManager.Current.GameBoardsDirectoryPath + "/" + GameBoardId + "." +
                                CardGameManager.Current.GameBoardImageFileType;
            var boardImageSprite = File.Exists(boardFilepath)
                ? UnityFileMethods.CreateSprite(boardFilepath)
                : null;
            Debug.Log($"boardImage: {GameBoardId} {boardImageSprite != null}");
            if (boardImageSprite != null)
            {
                var image = gameObject.GetOrAddComponent<Image>();
                image.sprite = boardImageSprite;
            }

            rectTransform.localScale = Vector3.one;
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            // Disable preview
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            // Disable preview
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            // Disable select
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            // Disable select
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            // Disable select
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void PostDragPlayable(PointerEventData eventData)
        {
            // Disable drag
        }

        protected override void ActOnDrag()
        {
            // Disable drag
        }
    }
}
