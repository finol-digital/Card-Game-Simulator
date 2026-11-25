/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class BundledGameInstallerTests
    {
        private const string Default = "Default@test";
        private const string Second = "Second@test";
        private const string Third = "Third@test";

        private string _root;
        private string _bundle;
        private string _games;
        private string _state;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "BundledGameInstallerTests-" + Guid.NewGuid());
            _bundle = Path.Combine(_root, "bundle");
            _games = Path.Combine(_root, "games");
            _state = Path.Combine(_root, "games-install");
            foreach (var gameId in new[] { Default, Second, Third })
            {
                var directory = Path.Combine(_bundle, gameId);
                Directory.CreateDirectory(Path.Combine(directory, "sets"));
                File.WriteAllText(Path.Combine(directory, BundledGameInstaller.GameFileName), "{\"name\":\"" + gameId + "\"}");
                File.WriteAllText(Path.Combine(directory, "AllCards.json"), "[]");
                File.WriteAllText(Path.Combine(directory, "sets", "set.json"), "{}");
            }

            // Not a game: StreamingAssets also holds the Addressables catalog
            Directory.CreateDirectory(Path.Combine(_bundle, "aa"));
            File.WriteAllText(Path.Combine(_bundle, "aa", "catalog.json"), "{}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        private BundledGameInstaller Launch(string defaultGameId = Default)
        {
            var installer = new BundledGameInstaller(new FolderBundledGames(_bundle), _games, _state);
            installer.Prepare();
            installer.InstallNow(defaultGameId);
            installer.StartBackground();
            return installer;
        }

        private static List<string> TakeInstalled(BundledGameInstaller installer)
        {
            Assert.IsTrue(installer.Background.Wait(TimeSpan.FromSeconds(30)), "The background installation finished");
            var installed = new List<string>();
            while (installer.TryTakeInstalled(out var gameId))
                installed.Add(gameId);
            return installed;
        }

        private List<string> InstalledGames() =>
            Directory.GetDirectories(_games).Select(Path.GetFileName).OrderBy(id => id, StringComparer.Ordinal).ToList();

        private void AssertComplete(string gameId)
        {
            var expected = Directory.GetFiles(Path.Combine(_bundle, gameId), "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(Path.Combine(_bundle, gameId), file)).OrderBy(f => f).ToList();
            var actual = Directory.GetFiles(Path.Combine(_games, gameId), "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(Path.Combine(_games, gameId), file)).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(expected, actual, gameId + " has every bundled file");
        }

        private string PendingFile => Path.Combine(_state, BundledGameInstaller.PendingFileName);
        private string StagingDirectory => Path.Combine(_state, BundledGameInstaller.StagingDirectoryName);

        [Test]
        public void FirstLaunchInstallsTheDefaultGameBeforeTheOthers()
        {
            var installer = new BundledGameInstaller(new FolderBundledGames(_bundle), _games, _state);
            installer.Prepare();
            CollectionAssert.AreEqual(new[] { Default, Second, Third }, installer.Pending);

            installer.InstallNow(Second);

            CollectionAssert.AreEqual(new[] { Second }, InstalledGames());
            AssertComplete(Second);
            CollectionAssert.AreEqual(new[] { Default, Third }, installer.Pending);
        }

        [Test]
        public void OtherBundledGamesAreReportedAsEachFinishesInTheBackground()
        {
            var installer = Launch();

            CollectionAssert.AreEqual(new[] { Second, Third }, TakeInstalled(installer),
                "Only the games installed in the background are reported");
            CollectionAssert.AreEqual(new[] { Default, Second, Third }, InstalledGames());
            foreach (var gameId in InstalledGames())
                AssertComplete(gameId);
            CollectionAssert.IsEmpty(installer.Pending);
            FileAssert.DoesNotExist(PendingFile);
            DirectAssert.DoesNotExist(StagingDirectory);
        }

        [Test]
        public void AnInterruptedInstallIsRepairedOnTheNextLaunch()
        {
            // The first launch installed the default game and was closed while copying the second
            var firstLaunch = new BundledGameInstaller(new FolderBundledGames(_bundle), _games, _state);
            firstLaunch.Prepare();
            firstLaunch.InstallNow(Default);
            var partialCopy = Path.Combine(StagingDirectory, Second);
            Directory.CreateDirectory(partialCopy);
            File.WriteAllText(Path.Combine(partialCopy, BundledGameInstaller.GameFileName), "{\"na");
            CollectionAssert.AreEqual(new[] { Default }, InstalledGames(), "A partial copy is never in the games folder");

            var relaunch = Launch();

            CollectionAssert.AreEqual(new[] { Second, Third }, TakeInstalled(relaunch));
            CollectionAssert.AreEqual(new[] { Default, Second, Third }, InstalledGames());
            AssertComplete(Second);
            AssertComplete(Third);
            DirectAssert.DoesNotExist(StagingDirectory);
            FileAssert.DoesNotExist(PendingFile);
        }

        [Test]
        public void AGameMovedIntoPlaceJustBeforeClosingIsKept()
        {
            var firstLaunch = new BundledGameInstaller(new FolderBundledGames(_bundle), _games, _state);
            firstLaunch.Prepare();
            firstLaunch.InstallNow(Default);
            // The second game was renamed into place, but CGS closed before saving that it was done
            Directory.CreateDirectory(Path.Combine(_games, Second));
            File.WriteAllText(Path.Combine(_games, Second, BundledGameInstaller.GameFileName), "edited");

            TakeInstalled(Launch());

            Assert.AreEqual("edited", File.ReadAllText(Path.Combine(_games, Second, BundledGameInstaller.GameFileName)));
            FileAssert.DoesNotExist(PendingFile);
        }

        [Test]
        public void UpdatedEditedAndDeletedBundledGamesStayThatWayAfterRelaunch()
        {
            TakeInstalled(Launch());
            var updatedFile = Path.Combine(_games, Default, "AllCards.json");
            File.WriteAllText(updatedFile, "[{\"id\":\"updated\"}]");
            var editedFile = Path.Combine(_games, Second, BundledGameInstaller.GameFileName);
            File.WriteAllText(editedFile, "{\"name\":\"Edited\"}");
            Directory.Delete(Path.Combine(_games, Third), true);

            var relaunch = Launch();

            CollectionAssert.IsEmpty(TakeInstalled(relaunch));
            CollectionAssert.AreEqual(new[] { Default, Second }, InstalledGames(), "The deleted game is not reinstalled");
            Assert.AreEqual("[{\"id\":\"updated\"}]", File.ReadAllText(updatedFile));
            Assert.AreEqual("{\"name\":\"Edited\"}", File.ReadAllText(editedFile));
        }

        [Test]
        public void AGameThatAppearedDuringInstallationIsNotReplaced()
        {
            var installer = new BundledGameInstaller(new FolderBundledGames(_bundle), _games, _state);
            installer.Prepare();
            installer.InstallNow(Default);
            // As when the player downloads the game before the background installation reaches it
            Directory.CreateDirectory(Path.Combine(_games, Third));
            File.WriteAllText(Path.Combine(_games, Third, BundledGameInstaller.GameFileName), "downloaded");

            installer.StartBackground();

            CollectionAssert.AreEqual(new[] { Second, Third }, TakeInstalled(installer));
            Assert.AreEqual("downloaded", File.ReadAllText(Path.Combine(_games, Third, BundledGameInstaller.GameFileName)));
            CollectionAssert.IsEmpty(installer.Pending);
        }

        [Test]
        public void AnEmptyGamesFolderGetsTheBundledGamesAgain()
        {
            TakeInstalled(Launch());
            foreach (var directory in Directory.GetDirectories(_games))
                Directory.Delete(directory, true);

            TakeInstalled(Launch());

            CollectionAssert.AreEqual(new[] { Default, Second, Third }, InstalledGames());
        }

        private static class DirectAssert
        {
            public static void DoesNotExist(string directory) =>
                Assert.IsFalse(Directory.Exists(directory), directory + " does not exist");
        }
    }
}
