/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs.Menu;
using FinolDigital.Cgs.CardGameDef;
using FinolDigital.Cgs.CardGameDef.Unity;
using JetBrains.Annotations;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardEditorMenu : Modal
    {
        public const string DownloadCardImage = "Download Card Image";
        public const string DownloadCardImagePrompt = "Enter card image url...";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        public const string ImportImage = "Import Image";
#else
        public const string SelectCardImageFilePrompt = "Select Card Image File";
#endif
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string ImageImportFailedWarningMessage = "Failed to get the image! Unable to import the card.";

        public GameObject downloadMenuPrefab;
        public RectTransform scrollRectContent;
        public RectTransform cardPropertyTemplate;
        public List<InputField> inputFields;
        public InputField cardIdInputField;
        public InputField setCodeInputField;
        public Dropdown backSelector;
        public Image cardImage;
        public Button saveButton;

        [UsedImplicitly]
        public string CardName
        {
            get => _cardName;
            set
            {
                _cardName = value;
                ValidateSaveButton();
            }
        }

        private string _cardName = string.Empty;

        [UsedImplicitly]
        public string CardId
        {
            get => _cardId;
            set
            {
                _cardId = UnityFileMethods.GetSafeFileName(value);
                if (_cardId.Length >= 64)
                    _cardId = _cardId[..64];
                if (!_cardId.Equals(cardIdInputField.text))
                    cardIdInputField.text = _cardId;
            }
        }

        private string _cardId = string.Empty;

        [UsedImplicitly]
        public string SetCode
        {
            get => _setCode;
            set
            {
                _setCode = UnityFileMethods.GetSafeFilePath(value);
                if (_cardId.Length >= 64)
                    _cardId = _cardId[..64];
                if (!_setCode.Equals(setCodeInputField.text))
                    setCodeInputField.text = _setCode;
            }
        }

        private string _setCode = string.Empty;

        [UsedImplicitly] public int Back { get; set; }

        private string BackFace => Back < BackFaceOptions.Count ? BackFaceOptions[Back].text : string.Empty;

        private List<Dropdown.OptionData> BackFaceOptions { get; } = new();

        private string BackFaceId => CardGameManager.Current.CardBackFaceImageSprites.ContainsKey(BackFace)
            ? BackFace
            : string.Empty;

        private Uri CardImageUri
        {
            get => _cardImageUri;
            set
            {
                _cardImageUri = value;
                ValidateSaveButton();
            }
        }

        private Uri _cardImageUri;

        private Sprite CardImageSprite
        {
            get => _cardImageSprite;
            set
            {
                if (_cardImageSprite != null)
                    Destroy(_cardImageSprite);
                _cardImageSprite = value;
            }
        }

        private Sprite _cardImageSprite;

        private DownloadMenu Downloader =>
            _downloader ??= Instantiate(downloadMenuPrefab).GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

        private UnityAction _onCreationCallback;

        private readonly List<TMP_InputField> _inputFields = new();

        private void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused) ||
                _inputFields.Any(field => field.isFocused))
                return;

            if ((Inputs.IsSubmit || Inputs.IsNew) && saveButton.interactable)
                StartSaveCard();
            if (Inputs.IsLoad && saveButton.interactable)
                DownloadCardImageFromWeb();
            if (Inputs.IsSave && saveButton.interactable)
                ImportCardImageFromFile();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        public void Show(UnityAction onCreationCallback)
        {
            Show();
            SetCode = string.Concat(CardGameManager.Current.Name.Where(char.IsLetterOrDigit));

            BackFaceOptions.Clear();
            BackFaceOptions.Add(new Dropdown.OptionData() {text = string.Empty});
            foreach (var backFaceKey in CardGameManager.Current.CardBackFaceImageSprites.Keys)
                BackFaceOptions.Add(new Dropdown.OptionData() {text = backFaceKey});
            backSelector.options = BackFaceOptions;
            backSelector.value = 0;

            cardImage.sprite = CardImageSprite != null ? CardImageSprite : CardGameManager.Current.CardBackImageSprite;

            _onCreationCallback = onCreationCallback;

            for (var i = _inputFields.Count - 1; i >= 0; i--)
                Destroy(_inputFields[i].transform.parent.gameObject);
            _inputFields.Clear();
            foreach (var propertyDef in CardGameManager.Current.CardProperties.Where(def =>
                         PropertyType.String.Equals(def.Type)))
            {
                var newTransform = Instantiate(cardPropertyTemplate.gameObject, scrollRectContent)
                    .transform;
                newTransform.gameObject.SetActive(true);
                newTransform.GetComponentInChildren<Text>().text = propertyDef.Display;
                _inputFields.Add(newTransform.GetComponentInChildren<TMP_InputField>());
            }
        }

        [UsedImplicitly]
        public void DownloadCardImageFromWeb()
        {
            Downloader.Show(DownloadCardImage, DownloadCardImagePrompt, DownloadCardImageFromWeb);
        }

        private IEnumerator DownloadCardImageFromWeb(string url)
        {
            CardImageUri = new Uri(url);
            yield return UpdateCardImage();
        }

        [UsedImplicitly]
        public void ImportCardImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardImageFromFile, ImportImage);
#elif ENABLE_WINMD_SUPPORT
            ImportCardImageFromFile(UwpFileBrowser.OpenFilePanel());
#elif UNITY_STANDALONE_LINUX
            var paths =
 StandaloneFileBrowser.OpenFilePanel(SelectCardImageFilePrompt, string.Empty, string.Empty, false);
            if (paths.Length > 0)
                ImportCardImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#else
            StandaloneFileBrowser.OpenFilePanelAsync(SelectCardImageFilePrompt, string.Empty, string.Empty, false,
                paths => { ImportCardImageFromFile(paths?.Length > 0 ? paths[0] : string.Empty); });
#endif
        }

#if ENABLE_WINMD_SUPPORT
        private async void ImportCardImageFromFile(string uri)
#else
        private void ImportCardImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            CardImageUri = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            CardImageUri = new Uri(UnityFileMethods.CacheFile(uri));
#else
            CardImageUri = new Uri(uri);
#endif
            StartCoroutine(UpdateCardImage());
        }

        private IEnumerator UpdateCardImage()
        {
            // NOTE: Memory Leak Potential
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(CardImageUri?.AbsoluteUri)
                , output => CardImageSprite = output);
            if (CardImageSprite != null)
                cardImage.sprite = CardImageSprite;
            else
                Debug.LogWarning(ImageImportFailedWarningMessage);
        }

        public void ValidateSaveButton()
        {
            saveButton.interactable =
                !string.IsNullOrEmpty(CardName) && CardImageUri != null && CardImageUri.IsAbsoluteUri;
            Debug.Log("ValidateSaveButton: " + CardName + " " + CardImageUri);
            Debug.Log("ValidateSaveButton: " + saveButton.interactable);
        }

        [UsedImplicitly]
        public void StartSaveCard()
        {
            if (CardImageUri != null && !CardImageUri.AbsoluteUri.EndsWith(CardGameManager.Current.CardImageFileType))
                CardGameManager.Instance.Messenger.Show(
                    "WARNING!: Image file type does not match " + CardGameManager.Current.CardImageFileType, true);

            StartCoroutine(SaveCard());
        }

        private IEnumerator SaveCard()
        {
            ValidateSaveButton();
            if (!saveButton.interactable)
                yield break;

            saveButton.interactable = false;

            var propertyDefValuePairs = new Dictionary<string, PropertyDefValuePair>();
            var stringProperties =
                CardGameManager.Current.CardProperties.Where(def => PropertyType.String.Equals(def.Type)).ToList();
            for (var i = 0; i < stringProperties.Count && i < _inputFields.Count; i++)
            {
                var propertyName = stringProperties[i].Name;
                var propertyDefValuePair = new PropertyDefValuePair
                {
                    Def = stringProperties[i],
                    Value = _inputFields[i].text
                };
                propertyDefValuePairs[propertyName] = propertyDefValuePair;
            }

            var card = new UnityCard(CardGameManager.Current,
                    string.IsNullOrEmpty(CardId)
                        ? Guid.NewGuid().ToString().ToUpper()
                        : CardId, CardName,
                    string.IsNullOrEmpty(SetCode) ? Set.DefaultCode : SetCode, propertyDefValuePairs,
                    false, false, BackFaceId)
                {ImageWebUrl = CardImageUri.AbsoluteUri};
            yield return UnityFileMethods.SaveUrlToFile(CardImageUri.AbsoluteUri, card.ImageFilePath);

            if (!File.Exists(card.ImageFilePath))
            {
                Debug.LogWarning(ImageImportFailedWarningMessage);
                yield break;
            }

            CardGameManager.Current.Add(card);
            _onCreationCallback?.Invoke();

            ValidateSaveButton();
            Hide();
        }
    }
}
