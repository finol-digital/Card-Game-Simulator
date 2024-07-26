/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace FinolDigital.Cgs.CardGameDef.Unity
{
    public class UnityCard : Card
    {
        public const string SizeWarningMessage = "WARNING: Card image for {0} ({1}) is too large! \n" +
                                                 "Recommended Action: Delete the Card, compress the image file using a tool like " +
                                                 "https://www.iloveimg.com/compress-image " +
                                                 ", then re-import.";

        public static readonly UnityCard Blank = new(UnityCardGame.UnityInvalid,
            string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>(), false);

        public string ImageFileName
        {
            get
            {
                var id = Id;
                var backFaceIdExtension = "." + BackFaceId;
                if (!IsBackFaceCard && !string.IsNullOrEmpty(BackFaceId) && id.Contains(backFaceIdExtension))
                    id = id[..id.IndexOf(backFaceIdExtension, StringComparison.Ordinal)];
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
            {
                if (((UnityCardGame) SourceGame).CoroutineRunner != null)
                    ((UnityCardGame) SourceGame).CoroutineRunner.StartCoroutine(GetAndSetImageSprite());
                else
                    Debug.LogWarning("RegisterDisplay::NoImageOrImageLoader");
            }
        }

        public IEnumerator GetAndSetImageSprite()
        {
            if (IsLoadingImage)
                yield break;

            IsLoadingImage = true;
            Sprite newSprite = null;
#if UNITY_WEBGL
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(ImageWebUrl)
                , output => newSprite = output);
#else
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(ImageFilePath, ImageWebUrl.Replace(" ", "%20"))
                , output => newSprite = output);
            var fileInfo = new FileInfo(ImageFilePath);
            if (fileInfo.Exists && fileInfo.Length > 1_000_000)
            {
                var sizeWarningMessage = string.Format(SizeWarningMessage, Name, Id);
                Debug.LogError(sizeWarningMessage);
            }
#endif
            if (newSprite != null)
                ImageSprite = newSprite;
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
