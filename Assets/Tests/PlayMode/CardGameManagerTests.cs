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
    public class CardGameRef
    {
        public string name;
        public string url;
    }

    [Serializable]
    public class CardGameRefList
    {
        public CardGameRef[] games;
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
            var defaultContractResolver = new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()};
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
            var cardGameRefList = JsonUtility.FromJson<CardGameRefList>(jsonFile.text);
            Assert.IsTrue(cardGameRefList.games.Length > 0);

            foreach (var cardGameRef in cardGameRefList.games)
            {
                // Enable retry if there are a lot of tests to do
                var maxAttempts = cardGameRefList.games.Length > 10 ? 5 : 1;
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    Debug.Log($"Testing download for: {cardGameRef.name}, attempt {attempt} of {maxAttempts}");
                    yield return CardGameManager.Instance.GetCardGame(cardGameRef.url);
                    yield return new WaitForSeconds(1); // Wait to load set cards
                    if (CardGameManager.Current.HasLoaded && string.IsNullOrEmpty(CardGameManager.Current.Error) &&
                        CardGameManager.Current.Cards.Count > 0)
                        break;
                    yield return new WaitForSeconds(10 * attempt);
                }

                Assert.IsTrue(CardGameManager.Current.HasLoaded);
                Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
                Assert.IsTrue(CardGameManager.Current.Cards.Count > 0);
                Assert.AreEqual(cardGameRef.name, CardGameManager.Current.Name);
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
            yield return new WaitUntil(() => !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Standard Playing Cards", CardGameManager.Current.Name);

#if !UNITY_WEBGL
            // Mahjong
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
            yield return new WaitUntil(() => !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Mahjong", CardGameManager.Current.Name);

            // Dominoes
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
            yield return new WaitUntil(() => !CardGameManager.Current.IsDownloading);

            Assert.IsTrue(CardGameManager.Current.HasLoaded);
            Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.AreEqual("Dominoes", CardGameManager.Current.Name);
#endif
        }
    }
}
