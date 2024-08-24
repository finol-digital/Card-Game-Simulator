/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace FinolDigital.Cgs.CardGameDef.Unity
{
    public class UnityCard : Card
    {
        public static readonly UnityCard Blank = new(UnityCardGame.UnityInvalid,
            string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>(), false);

        public string ImageFileName
        {
            get
            {
                var id = Id;
                if (!IsBackFaceCard && !string.IsNullOrEmpty(BackFaceId) && id.EndsWith("." + BackFaceId))
                    id = id[..id.LastIndexOf('.')];
                return UnityFileMethods.GetSafeFileName(id + "." + SourceGame.CardImageFileType);
            }
        }

        public string ImageFilePath =>
            UnityFileMethods.GetSafeFilePath(((UnityCardGame) SourceGame).SetsDirectoryPath) + "/" +
            UnityFileMethods.GetSafeFilePath(SetCode.Replace(':', '_').Replace('#', '_')) + "/" + ImageFileName;

        public Sprite ImageSprite
        {
            get => _imageSprite;
            set
            {
                if (_imageSprite != null)
                {
                    Object.Destroy(_imageSprite.texture);
                    Object.Destroy(_imageSprite);
                }

                _imageSprite = value;
                foreach (var cardDisplay in DisplaysUsingImage)
                    cardDisplay.SetImageSprite(_imageSprite);
            }
        }

        private Sprite _imageSprite;

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
                cardDisplay.SetImageSprite(ImageSprite);
            else if (!IsLoadingImage)
                EnqueueImageLoad();
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

        public void UnregisterDisplay(ICardDisplay cardDisplay)
        {
            cardDisplay.SetImageSprite(null);
            DisplaysUsingImage.Remove(cardDisplay);
            if (DisplaysUsingImage.Count < 1)
                ImageSprite = null;
        }
    }
}
