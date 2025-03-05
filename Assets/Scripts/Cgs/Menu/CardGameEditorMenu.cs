/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SFB;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public class CardGameEditorMenu : Modal
    {
#if ENABLE_WINMD_SUPPORT
        public const string PlatformWarningMessage = "Sorry, Backs Folder is not supported from Windows Store!";
#endif

        public const string DownloadBannerImage = "Download Banner Image";
        public const string DownloadBannerImagePrompt = "Enter banner image url...";
        public const string DownloadCardBackImage = "Download Card Back Image";
        public const string DownloadCardBackImagePrompt = "Enter card back image url...";
        public const string DownloadPlayMatImage = "Download PlayMat Image";
        public const string DownloadPlayMatImagePrompt = "Enter playmat image url...";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        public const string ImportImage = "Import Image";
#else
        public const string SelectBannerImageFilePrompt = "Select Banner Image File";
        public const string SelectCardBackImageFilePrompt = "Select Card Back Image File";
        public const string SelectPlayMatImageFilePrompt = "Select PlayMat Image File";
#endif
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string SelectFolderPrompt = "Select Folder";
        public const string ImportFolderWarningMessage = "No folder found for import! ";
        public const string ImportBackFailedWarningMessage = "Failed to find back: ";
        public const string CreationWarningMessage = "Failed to create the custom card game! ";
        public const string CreationCleanupErrorMessage = "Failed to both create and cleanup during creation! ";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public Image bannerImage;
        public Image cardBackImage;
        public Image playMatImage;
        public Text backsFolderText;
        public Button saveButton;

        private DownloadMenu Downloader =>
            _downloader ??= Instantiate(downloadMenuPrefab).GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

        [UsedImplicitly]
        public string GameName
        {
            get => _gameName;
            set
            {
                _gameName = value != null ? value.Replace("@", "") : string.Empty;
                if (!_gameName.Equals(inputFields[0].text))
                    inputFields[0].text = _gameName;
                ValidateCreateButton();
            }
        }

        private string _gameName = string.Empty;

        private UnityCardGame _game = new(null);

        [UsedImplicitly]
        public string Width
        {
            get => _width.ToString(CultureInfo.CurrentCulture);
            set
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var width)
                    && width is > 0 and < 10)
                    _width = width;
                else
                    Debug.LogWarning("Attempted to set invalid card width: " + value);
            }
        }

        private float _width = 2.5f;

        [UsedImplicitly]
        public string Height
        {
            get => _height.ToString(CultureInfo.CurrentCulture);
            set
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var height)
                    && height is > 0 and < 10)
                    _height = height;
                else
                    Debug.LogWarning("Attempted to set invalid card height: " + value);
            }
        }

        private float _height = 3.5f;

        [UsedImplicitly] public int BannerImageFileType { get; set; }

        [UsedImplicitly] public int CardBackImageFileType { get; set; }

        [UsedImplicitly] public int PlayMatImageFileType { get; set; }

        [UsedImplicitly] public string Copyright { get; set; }

        [UsedImplicitly] public string RulesUrl { get; set; }

        [UsedImplicitly] public string CardProperty { get; set; } = "description";

        [UsedImplicitly]
        public string BacksFolderPath
        {
            get => _backsFolderPath;
            set
            {
                if (!FileBrowserHelpers.DirectoryExists(value))
                {
                    Debug.LogWarning(ImportFolderWarningMessage + value);
                    CardGameManager.Instance.Messenger.Show(ImportFolderWarningMessage + value);
                    return;
                }

                _backsFolderPath = value;
                backsFolderText.text = _backsFolderPath;
            }
        }

        private string _backsFolderPath = string.Empty;

        private bool _isEdit;

        public void ShowNew()
        {
            Show();
            GameName = string.Empty;
            Width = "2.5";
            Height = "3.5";
            bannerImage.sprite = Resources.Load<Sprite>("Banner");
            BannerImageFileType = 0;
            cardBackImage.sprite = Resources.Load<Sprite>("CardBack");
            CardBackImageFileType = 0;
            playMatImage.sprite = Resources.Load<Sprite>("PlayMat");
            PlayMatImageFileType = 0;
            Copyright = string.Empty;
            RulesUrl = string.Empty;
            CardProperty = "description";
            _game = new UnityCardGame(CardGameManager.Instance);
            _isEdit = false;
        }

        public void ShowCurrent()
        {
            Show();
            GameName = CardGameManager.Current.Name;
            Width = CardGameManager.Current.CardSize.X.ToString(CultureInfo.InvariantCulture);
            Height = CardGameManager.Current.CardSize.Y.ToString(CultureInfo.InvariantCulture);
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            BannerImageFileType = CardGameManager.Current.BannerImageFileType.EndsWith("png") ? 0 : 1;
            cardBackImage.sprite = CardGameManager.Current.CardBackImageSprite;
            CardBackImageFileType = CardGameManager.Current.CardBackImageFileType.EndsWith("png") ? 0 : 1;
            playMatImage.sprite = CardGameManager.Current.PlayMatImageSprite;
            PlayMatImageFileType = CardGameManager.Current.PlayMatImageFilePath.EndsWith("png") ? 0 : 1;
            Copyright = CardGameManager.Current.Copyright;
            RulesUrl = CardGameManager.Current.RulesUrl?.ToString();
            CardProperty = "description";
            _game = new UnityCardGame(CardGameManager.Instance, GameName)
            {
                AutoUpdate = CardGameManager.Current.AutoUpdate,
                AutoUpdateUrl = CardGameManager.Current.AutoUpdateUrl,
                CardSize = CardGameManager.Current.CardSize,
                BannerImageFileType = CardGameManager.Current.BannerImageFileType,
                BannerImageUrl = CardGameManager.Current.BannerImageUrl,
                CardBackImageFileType = CardGameManager.Current.CardBackImageFileType,
                CardBackImageUrl = CardGameManager.Current.CardBackImageUrl,
                CardSetIdentifier = CardGameManager.Current.CardSetIdentifier,
                PlayMatImageFileType = CardGameManager.Current.PlayMatImageFileType,
                PlayMatImageUrl = CardGameManager.Current.PlayMatImageUrl,
                Copyright = CardGameManager.Current.Copyright,
                RulesUrl = CardGameManager.Current.RulesUrl,
                CardPrimaryProperty = CardGameManager.Current.CardPrimaryProperty,
                CardProperties = CardGameManager.Current.CardProperties
            };
            _isEdit = true;
        }

        private void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Inputs.IsSubmit || Inputs.IsNew) && saveButton.interactable)
                StartCreation();
            else if (Inputs.IsSort)
                DownloadBannerImageFromWeb();
            else if (Inputs.IsFilter)
                ImportBannerImageFromFile();
            else if (Inputs.IsLoad)
                DownloadCardBackImageFromWeb();
            else if (Inputs.IsSave)
                ImportCardBackImageFromFile();
            else if (Inputs.IsFocusNext)
                DownloadPlayMatImageFromWeb();
            else if (Inputs.IsFocusBack)
                ImportPlayMatImageFromFile();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        #region Banner

        [UsedImplicitly]
        public void DownloadBannerImageFromWeb()
        {
            Downloader.Show(DownloadBannerImage, DownloadBannerImagePrompt, DownloadBannerImageFromWeb);
        }

        private IEnumerator DownloadBannerImageFromWeb(string url)
        {
            _game.BannerImageUrl = new Uri(url);
            yield return UpdateBannerImage();
        }

        [UsedImplicitly]
        public void ImportBannerImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportBannerImageFromFile, ImportImage);
#elif ENABLE_WINMD_SUPPORT
            ImportBannerImageFromFile(UwpFileBrowser.OpenFilePanel());
#elif UNITY_STANDALONE_LINUX
            var paths =
 StandaloneFileBrowser.OpenFilePanel(SelectBannerImageFilePrompt, string.Empty, string.Empty, false);
            if (paths.Length > 0)
                ImportBannerImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#else
            StandaloneFileBrowser.OpenFilePanelAsync(SelectBannerImageFilePrompt, string.Empty, string.Empty, false,
                paths => { ImportBannerImageFromFile(paths?.Length > 0 ? paths[0] : string.Empty); });
#endif
        }
#if ENABLE_WINMD_SUPPORT
        private async void ImportBannerImageFromFile(string uri)
#else
        private void ImportBannerImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            _game.BannerImageUrl = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#else
            _game.BannerImageUrl = new Uri(UnityFileMethods.CacheFile(uri));
#endif
            StartCoroutine(UpdateBannerImage());
        }

        private IEnumerator UpdateBannerImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(_game.BannerImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                bannerImage.sprite = newSprite;
            else
                Debug.LogWarning(ImportImageWarningMessage);
        }

        #endregion

        #region CardBack

        [UsedImplicitly]
        public void DownloadCardBackImageFromWeb()
        {
            Downloader.Show(DownloadCardBackImage, DownloadCardBackImagePrompt, DownloadCardBackImageFromWeb);
        }

        private IEnumerator DownloadCardBackImageFromWeb(string url)
        {
            _game.CardBackImageUrl = new Uri(url);
            yield return UpdateCardBackImage();
        }

        [UsedImplicitly]
        public void ImportCardBackImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardBackImageFromFile, ImportImage);
#elif ENABLE_WINMD_SUPPORT
            ImportCardBackImageFromFile(UwpFileBrowser.OpenFilePanel());
#elif UNITY_STANDALONE_LINUX
            var paths =
 StandaloneFileBrowser.OpenFilePanel(SelectCardBackImageFilePrompt, string.Empty, string.Empty, false);
            if (paths.Length > 0)
                ImportCardBackImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#else
            StandaloneFileBrowser.OpenFilePanelAsync(SelectCardBackImageFilePrompt, string.Empty, string.Empty, false,
                paths => { ImportCardBackImageFromFile(paths?.Length > 0 ? paths[0] : string.Empty); });
#endif
        }
#if ENABLE_WINMD_SUPPORT
        private async void ImportCardBackImageFromFile(string uri)
#else
        private void ImportCardBackImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            _game.CardBackImageUrl = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#else
            _game.CardBackImageUrl = new Uri(UnityFileMethods.CacheFile(uri));
#endif
            StartCoroutine(UpdateCardBackImage());
        }

        private IEnumerator UpdateCardBackImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(_game.CardBackImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardBackImage.sprite = newSprite;
            else
                Debug.LogWarning(ImportImageWarningMessage);
        }

        #endregion

        #region PlayMat

        [UsedImplicitly]
        public void DownloadPlayMatImageFromWeb()
        {
            Downloader.Show(DownloadPlayMatImage, DownloadPlayMatImagePrompt, DownloadPlayMatImageFromWeb);
        }

        private IEnumerator DownloadPlayMatImageFromWeb(string url)
        {
            _game.PlayMatImageUrl = new Uri(url);
            yield return UpdatePlayMatImage();
        }

        [UsedImplicitly]
        public void ImportPlayMatImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportPlayMatImageFromFile, ImportImage);
#elif ENABLE_WINMD_SUPPORT
            ImportPlayMatImageFromFile(UwpFileBrowser.OpenFilePanel());
#elif UNITY_STANDALONE_LINUX
            var paths =
 StandaloneFileBrowser.OpenFilePanel(SelectPlayMatImageFilePrompt, string.Empty, string.Empty, false);
            if (paths.Length > 0)
                ImportPlayMatImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#else
            StandaloneFileBrowser.OpenFilePanelAsync(SelectPlayMatImageFilePrompt, string.Empty, string.Empty, false,
                paths => { ImportPlayMatImageFromFile(paths?.Length > 0 ? paths[0] : string.Empty); });
#endif
        }
#if ENABLE_WINMD_SUPPORT
        private async void ImportPlayMatImageFromFile(string uri)
#else
        private void ImportPlayMatImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            _game.PlayMatImageUrl = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#else
            _game.PlayMatImageUrl = new Uri(UnityFileMethods.CacheFile(uri));
#endif
            StartCoroutine(UpdatePlayMatImage());
        }

        private IEnumerator UpdatePlayMatImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(_game.PlayMatImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                playMatImage.sprite = newSprite;
            else
                Debug.LogWarning(ImportImageWarningMessage);
        }

        #endregion

        private void ValidateCreateButton()
        {
            saveButton.interactable = !string.IsNullOrEmpty(GameName);
        }

        [UsedImplicitly]
        public void StartCreation()
        {
            StartCoroutine(CreateGame());
        }

        private static List<PropertyDef> Of(string property)
        {
            var propertyDefs = new List<PropertyDef>();
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var display = textInfo.ToTitleCase(property);
            var propertyDef = new PropertyDef(property, PropertyType.String, display);
            propertyDefs.Add(propertyDef);
            return propertyDefs;
        }

        private IEnumerator CreateGame()
        {
            ValidateCreateButton();
            if (!saveButton.interactable)
                yield break;

            var gameName = GameName.Trim().Replace("@", "");

            var unityCardGame = _game;
            if (_isEdit)
            {
                unityCardGame.Name = gameName;
                unityCardGame.CardSize = new Float2(_width, _height);
                unityCardGame.BannerImageFileType = BannerImageFileType == 0 ? "png" : "jpg";
                unityCardGame.CardBackImageFileType = CardBackImageFileType == 0 ? "png" : "jpg";
                unityCardGame.PlayMatImageFileType = PlayMatImageFileType == 0 ? "png" : "jpg";
                unityCardGame.Copyright = string.IsNullOrWhiteSpace(Copyright) ? "" : Copyright;
                unityCardGame.RulesUrl =
                    Uri.IsWellFormedUriString(RulesUrl, UriKind.Absolute) ? new Uri(RulesUrl) : null;
                unityCardGame.CardPrimaryProperty = string.IsNullOrWhiteSpace(CardProperty) ? "" : CardProperty;
                unityCardGame.CardProperties = string.IsNullOrWhiteSpace(CardProperty)
                    ? new List<PropertyDef>()
                    : Of(CardProperty);
            }
            else
            {
                unityCardGame = new UnityCardGame(CardGameManager.Instance, gameName)
                {
                    AutoUpdate = -1, CardSize = new Float2(_width, _height),
                    BannerImageFileType = BannerImageFileType == 0 ? "png" : "jpg",
                    BannerImageUrl = _game.BannerImageUrl,
                    CardBackImageFileType = CardBackImageFileType == 0 ? "png" : "jpg",
                    CardBackImageUrl = _game.CardBackImageUrl,
                    CardSetIdentifier = "setCode",
                    PlayMatImageFileType = PlayMatImageFileType == 0 ? "png" : "jpg",
                    PlayMatImageUrl = _game.PlayMatImageUrl,
                    Copyright = string.IsNullOrWhiteSpace(Copyright) ? "" : Copyright,
                    RulesUrl = Uri.IsWellFormedUriString(RulesUrl, UriKind.Absolute) ? new Uri(RulesUrl) : null,
                    CardPrimaryProperty = string.IsNullOrWhiteSpace(CardProperty) ? "" : CardProperty,
                    CardProperties = string.IsNullOrWhiteSpace(CardProperty)
                        ? new List<PropertyDef>()
                        : Of(CardProperty)
                };
            }

            if (!Directory.Exists(unityCardGame.GameDirectoryPath))
                Directory.CreateDirectory(unityCardGame.GameDirectoryPath);
            var defaultContractResolver = new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()};
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = defaultContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            File.WriteAllText(unityCardGame.GameFilePath,
                JsonConvert.SerializeObject(unityCardGame, jsonSerializerSettings));

            if (!string.IsNullOrEmpty(BacksFolderPath) && FileBrowserHelpers.DirectoryExists(BacksFolderPath))
                CopyBacksFolder();

            yield return CardGameManager.Instance.UpdateCardGame(unityCardGame);

            if (!string.IsNullOrEmpty(unityCardGame.Error))
            {
                Debug.LogWarning(CreationWarningMessage + unityCardGame.Error);
                try
                {
                    Directory.Delete(unityCardGame.GameDirectoryPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError(CreationCleanupErrorMessage + ex.Message);
                }
            }
            else
            {
                CardGameManager.Instance.AllCardGames[unityCardGame.Id] = unityCardGame;
                CardGameManager.Instance.Select(unityCardGame.Id);
                Hide();
            }
        }

        [UsedImplicitly]
        public void SelectBacksFolder()
        {
#if ENABLE_WINMD_SUPPORT
            CardGameManager.Instance.Messenger.Show(PlatformWarningMessage);
            Hide();
#else
            FileBrowser.ShowLoadDialog((paths) => { BacksFolderPath = paths[0]; }, () => { },
                FileBrowser.PickMode.Folders, false, null, null, SelectFolderPrompt);
#endif
        }

        private void CopyBacksFolder()
        {
            var backsToImport = FileBrowserHelpers.GetEntriesInDirectory(BacksFolderPath, true)
                .Where(fileSystemEntry => !fileSystemEntry.IsDirectory &&
                                          !string.IsNullOrEmpty(fileSystemEntry.Extension) &&
                                          fileSystemEntry.Extension.EndsWith(CardGameManager.Current
                                              .CardBackImageFileType))
                .ToList();
            for (var i = 0; i < backsToImport.Count; i++)
            {
                try
                {
                    if (!Directory.Exists(CardGameManager.Current.BacksDirectoryPath))
                        Directory.CreateDirectory(CardGameManager.Current.BacksDirectoryPath);
                    FileBrowserHelpers.CopyFile(backsToImport[i].Path,
                        Path.Join(CardGameManager.Current.BacksDirectoryPath, backsToImport[i].Name));
                }
                catch
                {
                    Debug.LogWarning(ImportBackFailedWarningMessage + backsToImport[i].Name);
                    CardGameManager.Instance.Messenger.Show(ImportBackFailedWarningMessage + backsToImport[i].Name);
                }
            }
        }
    }
}
