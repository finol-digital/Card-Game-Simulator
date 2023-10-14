using System;
using System.Collections;
using System.IO;
using Cgs;
using FinolDigital.Cgs.CardGameDef.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
        }

        [Test]
        [Ignore("TODO")]
        public void CanRecoverFromFailure()
        {
            // TODO:
            Assert.Fail();
        }
    }
}
