/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace FinolDigital.Cgs.Json.Unity
{
    public class UnityCard : Card
    {
        public static readonly UnityCard Blank = new(UnityCardGame.UnityInvalid,
            string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>(), false);

        public string ImageFileType
        {
            get => !string.IsNullOrEmpty(_imageFileType) ? _imageFileType : SourceGame.CardImageFileType;
            set => _imageFileType = value;
        }

        private string _imageFileType;

        public string ImageFileName
        {
            get
            {
                var id = Id;
                if (!IsBackFaceCard && !string.IsNullOrEmpty(BackFaceId) && id.EndsWith("." + BackFaceId))
                    id = id[..id.LastIndexOf('.')];
                return UnityFileMethods.GetSafeFileName(id + "." + ImageFileType);
            }
        }

        public string ImageFilePath =>
            UnityFileMethods.GetSafeFilePath(((UnityCardGame)SourceGame).SetsDirectoryPath) + "/" +
            UnityFileMethods.GetSafeFilePath(SetCode.Replace(':', '_').Replace('#', '_')) + "/" + ImageFileName;

        public Sprite ImageSprite
        {
            get => _imageSprite;
            set
            {
                if (_orientedImageSprite != null && _orientedImageSprite != _imageSprite)
                {
                    Object.Destroy(_orientedImageSprite.texture);
                    Object.Destroy(_orientedImageSprite);
                }

                _orientedImageSprite = null;

                if (_imageSprite != null)
                {
                    Object.Destroy(_imageSprite.texture);
                    Object.Destroy(_imageSprite);
                }

                _imageSprite = value;
                foreach (var cardDisplay in DisplaysUsingImage)
                    cardDisplay.SetImageSprite(GetImageSpriteFor(cardDisplay));
            }
        }

        private Sprite _imageSprite;

        // Card images with a different orientation than the game's card size (eg a horizontal scan for a
        // vertical card) would get stretched to fit, so this variant is rotated to match the card's orientation
        public Sprite OrientedImageSprite
        {
            get
            {
                if (_orientedImageSprite == null)
                    _orientedImageSprite = CreateOrientedImageSprite();
                return _orientedImageSprite;
            }
        }

        private Sprite _orientedImageSprite;

        public bool IsLoadingImage { get; private set; }

        protected HashSet<ICardDisplay> DisplaysUsingImage { get; private set; }

        public UnityCard(UnityCardGame sourceGame, string id, string name, string setCode,
            Dictionary<string, PropertyDefValuePair> properties, bool isReprint, bool isBackFaceCard = false,
            string backFaceId = "") : base(sourceGame, id, name, setCode,
            properties, isReprint, isBackFaceCard, backFaceId)
        {
            SourceGame = sourceGame;
            DisplaysUsingImage = new HashSet<ICardDisplay>();
        }

        public void RegisterDisplay(ICardDisplay cardDisplay)
        {
            DisplaysUsingImage.Add(cardDisplay);
            if (ImageSprite != null)
                cardDisplay.SetImageSprite(GetImageSpriteFor(cardDisplay));
            else if (!IsLoadingImage)
                EnqueueImageLoad();
        }

        private Sprite GetImageSpriteFor(ICardDisplay cardDisplay)
        {
            return cardDisplay.UsesOrientedImage ? OrientedImageSprite : ImageSprite;
        }

        private void EnqueueImageLoad()
        {
            IsLoadingImage = true;
            ImageQueueService.Instance.Enqueue(this);
        }

        public void OnLoadImage(Sprite imageSprite)
        {
            if (imageSprite != null)
                ImageSprite = imageSprite;
            IsLoadingImage = false;
        }

        // Returns ImageSprite itself when its orientation already matches the game's card size
        private Sprite CreateOrientedImageSprite()
        {
            if (_imageSprite == null || SourceGame == null)
                return _imageSprite;

            var texture = _imageSprite.texture;
            var cardSize = SourceGame.CardSize;
            var isImageLandscape = texture.width > texture.height;
            var isImagePortrait = texture.width < texture.height;
            var isCardLandscape = Mathf.Abs(cardSize.X) > Mathf.Abs(cardSize.Y);
            var isCardPortrait = Mathf.Abs(cardSize.X) < Mathf.Abs(cardSize.Y);
            if (!(isImageLandscape && isCardPortrait) && !(isImagePortrait && isCardLandscape))
                return _imageSprite;

            // Rotate clockwise, so that rotating the card counter-clockwise in-game reads the image upright
            try
            {
                var rotatedTexture = UnityFileMethods.Rotate90Clockwise(texture);
                return Sprite.Create(rotatedTexture, new Rect(0, 0, rotatedTexture.width, rotatedTexture.height),
                    new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to rotate image for card: {Name} ({Id}): {e}");
                return _imageSprite;
            }
        }

        public void UnregisterDisplay(ICardDisplay cardDisplay)
        {
            cardDisplay.SetImageSprite(null);
            DisplaysUsingImage.Remove(cardDisplay);
            if (DisplaysUsingImage.Count < 1)
                ImageSprite = null;
        }
    }
}
