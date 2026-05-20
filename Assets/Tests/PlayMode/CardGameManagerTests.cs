using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    Debug.Log($"Testing download for: {game.name}, attempt {attempt} of {maxAttempts}");
                    yield return CardGameManager.Instance.GetCardGame(game.autoUpdateUrl);
                    yield return new WaitForSeconds(1); // Wait to load set cards
                    if (CardGameManager.Current.HasLoaded && string.IsNullOrEmpty(CardGameManager.Current.Error) &&
                        CardGameManager.Current.Cards.Count > 0)
                        break;
                    yield return new WaitForSeconds(10 * attempt);
                }

                Assert.IsTrue(CardGameManager.Current.HasLoaded);
                Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
                Assert.IsTrue(CardGameManager.Current.Cards.Count > 0);
                Assert.AreEqual(game.name, CardGameManager.Current.Name);
            }

            // No need to wait for slow card loads
            CardGameManager.Instance.StopAllCoroutines();
            yield return new WaitForSeconds(1);
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

#if !UNITY_WEBGL
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
#endif
        }

        [Test]
        public void WriteAllCardsJson_WritesSchemaAlignedBacks()
        {
            var game = new UnityCardGame(null, "write_backs_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>()
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var withBack = new UnityCard(game, "card_with_back", "With Back", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false, false, "DARK_BACK");
            var defaultBack = new UnityCard(game, "card_default_back", "Default Back", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);

            game.Add(withBack, false);
            game.Add(defaultBack, false);
            game.WriteAllCardsJson();

            var allCards = JArray.Parse(File.ReadAllText(game.CardsFilePath));
            var withBackToken = allCards.Children<JObject>().First(token =>
                string.Equals(token.Value<string>("id"), "card_with_back", StringComparison.Ordinal));
            var defaultBackToken = allCards.Children<JObject>().First(token =>
                string.Equals(token.Value<string>("id"), "card_default_back", StringComparison.Ordinal));

            Assert.AreEqual("DARK_BACK", withBackToken["backs"]?[0]?.Value<string>());
            Assert.IsNull(withBackToken["backFaceId"]);
            Assert.IsNull(defaultBackToken["backs"]);
            Assert.IsNull(defaultBackToken["backFaceId"]);

            Directory.Delete(game.GameDirectoryPath, true);
        }

        [Test]
        public void LoadCards_UsesBacksThenBackFaceIdFallback()
        {
            var game = new UnityCardGame(null, "load_backs_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>()
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var allCards = new JArray
            {
                new JObject
                {
                    ["id"] = "legacy_back",
                    ["name"] = "Legacy",
                    ["set"] = Set.DefaultCode,
                    ["backFaceId"] = "LEGACY_BACK"
                },
                new JObject
                {
                    ["id"] = "canonical_back",
                    ["name"] = "Canonical",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray("CANON_BACK")
                },
                new JObject
                {
                    ["id"] = "both_back",
                    ["name"] = "Both",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray("PREFERRED_BACK"),
                    ["backFaceId"] = "LEGACY_SHOULD_NOT_WIN"
                },
                new JObject
                {
                    ["id"] = "default_back",
                    ["name"] = "Default",
                    ["set"] = Set.DefaultCode
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("legacy_back", out var legacyCard));
            Assert.AreEqual("LEGACY_BACK", legacyCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("canonical_back", out var canonicalCard));
            Assert.AreEqual("CANON_BACK", canonicalCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("both_back", out var bothCard));
            Assert.AreEqual("PREFERRED_BACK", bothCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("default_back", out var defaultCard));
            Assert.IsTrue(string.IsNullOrEmpty(defaultCard.BackFaceId));

            Directory.Delete(game.GameDirectoryPath, true);
        }
    }
}
