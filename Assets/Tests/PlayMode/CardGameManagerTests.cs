using System;
using System.Collections;
using System.IO;
using CardGameDef.Unity;
using Cgs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private CardGameManager _manager;

        [SetUp]
        public void Setup()
        {
            var manager = new GameObject();
            manager.AddComponent<EventSystem>();
            _manager = manager.AddComponent<CardGameManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_manager.gameObject);
        }

        [UnityTest]
        public IEnumerator CanLoadDefaultGames()
        {
            if (Directory.Exists(UnityCardGame.GamesDirectoryPath))
                Directory.Delete(UnityCardGame.GamesDirectoryPath, true);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);

            // Default is Standard Playing Cards
            _manager.AllCardGames.Clear();
            _manager.LookupCardGames();
            _manager.ResetCurrentToDefault();
            _manager.ResetGameScene();
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

        [UnityTest]
        public IEnumerator CanGetGames()
        {
            var jsonFile = Resources.Load("games") as TextAsset;
            Assert.NotNull(jsonFile);
            var cardGameRefList = JsonUtility.FromJson<CardGameRefList>(jsonFile.text);
            Assert.IsTrue(cardGameRefList.games.Length > 0);

            foreach (CardGameRef game in cardGameRefList.games)
            {
                Debug.Log("Testing download for: " + game.name);
                yield return _manager.GetCardGame(game.url);
                Assert.IsTrue(CardGameManager.Current.HasLoaded);
                Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
                Assert.IsTrue(CardGameManager.Current.Cards.Count > 0);
                Assert.AreEqual(game.name, CardGameManager.Current.Name);
            }
        }

        [Test]
        [Ignore("TODO")]
        public void CanRecoverFromFailure()
        {
            // TODO:
            Assert.IsTrue(false);
        }
    }
}
