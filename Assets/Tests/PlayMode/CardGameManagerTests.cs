/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityExtensionMethods;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    [Serializable]
    public class Game
    {
        public string name;
        public string autoUpdateUrl;
    }

    public class CardGameManagerTests
    {
        [SetUp]
        public void Setup()
        {
            Object.Instantiate(Resources.Load<GameObject>("CardGameManager"));
        }

        [Test]
        public void ImportCompletionRunsWhenFileIsMissing()
        {
            var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".cgs.zip");
            var completions = 0;
            LogAssert.Expect(LogType.Error, string.Format(CardGameManager.FileNotFoundErrorMessage, path));

            CardGameManager.Instance.ImportCardGame(path, () => completions++);

            Assert.AreEqual(1, completions);
        }

        [TestCase(".zip", false)]
        [TestCase(".cgs.zip", true)]
        public void ImportCompletionReleasesInvalidArchive(string extension, bool corruptZip)
        {
            var path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + extension);
            File.WriteAllText(path, "Not ZIP data");
            var completions = 0;
            try
            {
                if (corruptZip)
                    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                        "^" + System.Text.RegularExpressions.Regex.Escape(CardGameManager.ImportFailureErrorMessage)));
                else
                    LogAssert.Expect(LogType.Error, string.Format(CardGameManager.InvalidCgsZipFileErrorMessage, path));

                CardGameManager.Instance.ImportCardGame(path, () =>
                {
                    completions++;
                    File.Delete(path);
                });

                Assert.AreEqual(1, completions);
                Assert.IsFalse(File.Exists(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ImportCompletionWaitsForOverwritePromptAndRunsOnDismissal()
        {
            var gameId = "ImportTest-" + Guid.NewGuid().ToString("N");
            var path = Path.Combine(Application.temporaryCachePath, gameId + ".cgs.zip");
            var gameDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, gameId);
            Directory.CreateDirectory(gameDirectory);
            var existingFile = Path.Combine(gameDirectory, "keep.txt");
            File.WriteAllText(existingFile, "Existing game data");
            File.WriteAllText(path, "ZIP is not read when overwrite is canceled");
            var manager = CardGameManager.Instance;
            while (manager.Messenger.gameObject.activeSelf)
                manager.Messenger.OkClose();
            var completions = 0;
            try
            {
                manager.ImportCardGame(path, () =>
                {
                    completions++;
                    File.Delete(path);
                });
                Assert.AreEqual(0, completions);
                Assert.IsTrue(File.Exists(path), "The pending confirmation still owns the ZIP.");

                // Escape and backdrop dismissal call OkClose, without clicking No.
                manager.Messenger.OkClose();
                manager.Messenger.OkClose();

                Assert.AreEqual(1, completions);
                Assert.IsFalse(File.Exists(path));
                Assert.AreEqual("Existing game data", File.ReadAllText(existingFile));
            }
            finally
            {
                manager.Messenger.OkClose();
                File.Delete(path);
                Directory.Delete(gameDirectory, true);
            }
        }

        [UnityTest]
        public IEnumerator CanCreateNewGameAndCard()
        {
            var newCardGame = new UnityCardGame(CardGameManager.Instance, "gameName")
            {
                AutoUpdate = -1, CardSize = new Float2(250, 350),
                BannerImageFileType = "png",
                BannerImageUrl = null,
                CardBackImageFileType = "png",
                CardBackImageUrl = null,
                CardSetIdentifier = "setCode",
                PlayMatImageFileType = "png",
                PlayMatImageUrl = null,
                Copyright = "",
                RulesUrl = null,
                CardPrimaryProperty = "",
                CardProperties = new List<PropertyDef>()
            };
            if (Directory.Exists(newCardGame.GameDirectoryPath))
                Directory.Delete(newCardGame.GameDirectoryPath, true);

            var previousDeveloperMode = PlayerPrefs.GetInt("DeveloperMode", 0);
            PlayerPrefs.SetInt("DeveloperMode", 1);

            Directory.CreateDirectory(newCardGame.GameDirectoryPath);
            var defaultContractResolver = new DefaultContractResolver
                { NamingStrategy = new CamelCaseNamingStrategy() };
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = defaultContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            File.WriteAllText(newCardGame.GameFilePath,
                JsonConvert.SerializeObject(newCardGame, jsonSerializerSettings));

            yield return CardGameManager.Instance.UpdateCardGame(newCardGame);

            CardGameManager.Instance.AllCardGames[newCardGame.Id] = newCardGame;
            CardGameManager.Instance.Select(newCardGame.Id);

            var card = new UnityCard(CardGameManager.Current,
                Guid.NewGuid().ToString().ToUpper()
                , "Test",
                Set.DefaultCode, null,
                false);

            yield return UnityFileMethods.SaveUrlToFile(new Uri(
                    Application.streamingAssetsPath
                    + "/" +
                    Tags.StandardPlayingCardsDirectoryName + "/" +
                    "CardBack.png").AbsoluteUri,
                card.ImageFilePath);

            CardGameManager.Current.Add(card);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.IsTrue(CardGameManager.Current.Cards.Count > 0);
            Assert.IsTrue(File.Exists(CardGameManager.Current.Cards.First().Value.ImageFilePath));
            Assert.AreEqual("gameName", CardGameManager.Current.Name);

            PlayerPrefs.SetInt("DeveloperMode", previousDeveloperMode);
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator CanGetGames()
        {
            var jsonFile = Resources.Load("games") as TextAsset;
            Assert.NotNull(jsonFile);
            var games = JsonConvert.DeserializeObject<List<Game>>(jsonFile.text);
            Assert.IsTrue(games.Count > 0);

            foreach (var game in games)
            {
                // Enable retry if there are a lot of tests to do
                var maxAttempts = games.Count > 10 ? 5 : 1;
                var missingImages = new List<string>();
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    Debug.Log($"Testing download for: {game.name}, attempt {attempt} of {maxAttempts}");
                    var downloadStart = DateTime.UtcNow.AddSeconds(-2);
                    yield return CardGameManager.Instance.GetCardGame(game.autoUpdateUrl);
                    yield return new WaitForSeconds(1); // Wait to load set cards
                    missingImages = MissingImages(CardGameManager.Current, downloadStart);
                    if (CardGameManager.Current.HasLoaded && string.IsNullOrEmpty(CardGameManager.Current.Error) &&
                        CardGameManager.Current.Cards.Count > 0 && missingImages.Count == 0)
                        break;
                    yield return new WaitForSeconds(10 * attempt);
                }

                Assert.IsTrue(CardGameManager.Current.HasLoaded);
                Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
                Assert.IsTrue(CardGameManager.Current.Cards.Count > 0);
                Assert.AreEqual(game.name, CardGameManager.Current.Name);
                Assert.IsEmpty(missingImages,
                    $"{game.name} failed to download: {string.Join(", ", missingImages)}");
            }

            // No need to wait for slow card loads
            CardGameManager.Instance.StopAllCoroutines();
            yield return new WaitForSeconds(1);
        }

        private static List<string> MissingImages(UnityCardGame cardGame, DateTime downloadStart)
        {
            var expectedImages = new List<(Uri url, string filePath)>
            {
                (cardGame.BannerImageUrl, cardGame.BannerImageFilePath),
                (cardGame.CardBackImageUrl, cardGame.CardBackImageFilePath),
                (cardGame.PlayMatImageUrl, cardGame.PlayMatImageFilePath)
            };
            expectedImages.AddRange(cardGame.CardBackFaceImageUrls
                .Where(cardBack => !string.IsNullOrEmpty(cardBack.Id))
                .Select(cardBack => (cardBack.Url, Path.Combine(cardGame.BacksDirectoryPath,
                    cardBack.Id + "." + cardGame.CardBackImageFileType.ToLower()))));
            expectedImages.AddRange(cardGame.GameBoardUrls
                .Where(gameBoard => !string.IsNullOrEmpty(gameBoard.Id))
                .Select(gameBoard => (gameBoard.Url, cardGame.GameBoardsDirectoryPath + "/" + gameBoard.Id + "." +
                                                     cardGame.GameBoardImageFileType)));

            return expectedImages
                .Where(image => image.url != null && image.url.IsAbsoluteUri)
                .Where(image => !File.Exists(image.filePath) ||
                                File.GetLastWriteTimeUtc(image.filePath) < downloadStart)
                .Select(image => image.url.AbsoluteUri)
                .ToList();
        }

        [UnityTest]
        public IEnumerator CanLoadDefaultGames()
        {
            if (Directory.Exists(UnityCardGame.GamesDirectoryPath))
                Directory.Delete(UnityCardGame.GamesDirectoryPath, true);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);

            // Default is Standard Playing Cards
            CardGameManager.Instance.AllCardGames.Clear();
            CardGameManager.Instance.LookupCardGames();
            CardGameManager.Instance.ResetCurrentToDefault();
            CardGameManager.Instance.ResetGameScene();
            yield return new WaitUntil(() =>
                !CardGameManager.Current.IsLoading && !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Standard Playing Cards", CardGameManager.Current.Name);

            // Mahjong
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
            yield return new WaitUntil(() =>
                !CardGameManager.Current.IsLoading && !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Mahjong", CardGameManager.Current.Name);

            // Dominoes
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
            yield return new WaitUntil(() =>
                !CardGameManager.Current.IsLoading && !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Dominoes", CardGameManager.Current.Name);
        }

    }
}
