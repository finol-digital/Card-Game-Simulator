/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// Card files load over several frames, parsed off the main thread, with the same cards as a load in one call.
    /// </summary>
    public class CardFileLoadingTests
    {
        private readonly List<string> _directories = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var directory in _directories.Where(Directory.Exists))
                Directory.Delete(directory, true);
            _directories.Clear();
        }

        /// <summary>Writes a game with card properties, several sets, reprints, and <paramref name="pages"/> card files.</summary>
        private string CreateGame(string name, int pages, int cardsPerPage)
        {
            var directory = Path.Combine(UnityCardGame.GamesDirectoryPath, name);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            _directories.Add(directory);

            var properties = new[] { "type", "text", "power" }.Select(property =>
                new Dictionary<string, string> { ["name"] = property, ["type"] = "string" }).ToList();
            File.WriteAllText(Path.Combine(directory, "cgs.json"), JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["autoUpdate"] = -1,
                    ["allCardsUrlPageCount"] = pages,
                    ["allCardsUrlPageCountStartIndex"] = 1,
                    ["cardProperties"] = properties
                }));
            for (var page = 1; page <= pages; page++)
            {
                var cards = Enumerable.Range(0, cardsPerPage).Select(index => new Dictionary<string, string>
                {
                    ["id"] = $"{name}-{page}-{index}",
                    // Every tenth card reprints an earlier name, in another set
                    ["name"] = index % 10 == 9 ? $"Card {page}-{index - 1}" : $"Card {page}-{index}",
                    ["set"] = "S" + index % 7,
                    ["type"] = index % 2 == 0 ? "Creature" : "Spell",
                    ["text"] = "Rules text " + index,
                    ["power"] = (index % 5).ToString()
                });
                var fileName = page == 1 ? "AllCards.json" : $"AllCards.json{page}";
                File.WriteAllText(Path.Combine(directory, fileName), JsonConvert.SerializeObject(cards));
            }

            var game = new UnityCardGame(null, name);
            game.ReadProperties(false);
            Assert.IsTrue(string.IsNullOrEmpty(game.Error), game.Error);
            if (!_directories.Contains(game.GameDirectoryPath))
                _directories.Add(game.GameDirectoryPath);
            return game.Id;
        }

        private static UnityCardGame ReadGame(string gameId)
        {
            var game = new UnityCardGame(null, gameId);
            game.ReadProperties(false);
            return game;
        }

        private static readonly string[] PropertyNames = { "type", "text", "power" };

        private static string Describe(UnityCardGame game) => string.Join("\n", game.Cards.Values
            .OrderBy(card => card.Id).Select(card => card.Id + "|" + card.Name + "|" + card.SetCode + "|" +
                                                     card.IsReprint + "|" + string.Join(",", PropertyNames
                                                         .Select(property => card.GetPropertyValueString(property)))));

        [UnityTest]
        public IEnumerator LoadingOverFramesGivesTheSameCardsAsLoadingAtOnce()
        {
            const int pages = 3;
            var gameId = CreateGame("CardFileLoadingSame", pages, 400);

            var atOnce = ReadGame(gameId);
            for (var page = 1; page <= pages; page++)
                atOnce.LoadCards(page);

            var overFrames = ReadGame(gameId);
            for (var page = 1; page <= pages; page++)
                yield return overFrames.LoadCardsRoutine(page);

            Assert.IsTrue(string.IsNullOrEmpty(overFrames.Error), overFrames.Error);
            Assert.AreEqual(pages * 400, overFrames.Cards.Count);
            Assert.AreEqual(Describe(atOnce), Describe(overFrames), "Both loads give the same cards");
            CollectionAssert.AreEquivalent(atOnce.Sets.Keys, overFrames.Sets.Keys, "Both loads give the same sets");
            Assert.AreEqual(1f, overFrames.ContentLoadProgress, 0.0001f);
        }

        [UnityTest]
        public IEnumerator ALargeCardFileDoesNotStallAFrame()
        {
            const int cards = 30000;
            var game = ReadGame(CreateGame("CardFileLoadingLarge", 1, cards));

            var frames = 0;
            var longestFrameMs = 0.0;
            var watch = Stopwatch.StartNew();
            var load = game.LoadCardsRoutine(1);
            // Step the load as Unity would, one step per frame, timing the main thread's work in each step
            while (true)
            {
                var step = Stopwatch.StartNew();
                var isRunning = MoveNext(load);
                longestFrameMs = System.Math.Max(longestFrameMs, step.Elapsed.TotalMilliseconds);
                if (!isRunning)
                    break;
                frames++;
                yield return null;
            }

            UnityEngine.Debug.Log($"CardFileLoadingTests: {cards} cards in {watch.ElapsedMilliseconds} ms over " +
                                  $"{frames} frames; longest step {longestFrameMs:F0} ms");
            Assert.IsTrue(string.IsNullOrEmpty(game.Error), game.Error);
            Assert.AreEqual(cards, game.Cards.Count);
            Assert.Greater(frames, 1, "The load spreads over several frames");
            // Loading this file in one call took 1.8 s or more on the Lenovo IdeaPad Y510P; spread over frames, its
            // longest step took 89 ms there, mostly garbage collection, so this leaves room for slower CI machines
            Assert.Less(longestFrameMs, 250, "No single frame does most of the load");
        }

        // Runs a step of a coroutine and its nested coroutines, as Unity does each frame
        private static bool MoveNext(IEnumerator routine)
        {
            if (routine.Current is IEnumerator nested && MoveNext(nested))
                return true;
            return routine.MoveNext();
        }

        [UnityTest]
        public IEnumerator ACorruptCardFileReportsAnErrorAsBefore()
        {
            var gameId = CreateGame("CardFileLoadingCorrupt", 1, 10);
            var game = ReadGame(gameId);
            File.WriteAllText(game.CardsFilePath, "{ not valid json");

            yield return game.LoadCardsRoutine(1);

            Assert.IsFalse(string.IsNullOrEmpty(game.Error), "A corrupt card file sets the game's error");
            Assert.IsFalse(game.HasLoaded);
            Assert.AreEqual(0, game.Cards.Count);

            var atOnce = ReadGame(gameId);
            atOnce.LoadCards(1);
            Assert.IsFalse(string.IsNullOrEmpty(atOnce.Error), "Loading at once reports the same failure");
        }
    }
}
