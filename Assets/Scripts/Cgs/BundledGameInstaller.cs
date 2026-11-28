/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityExtensionMethods;
using Debug = UnityEngine.Debug;

namespace Cgs
{
    /// <summary>The games that ship with CGS, which it installs into the writable games folder.</summary>
    internal interface IBundledGames
    {
        /// <summary>The bundled games' folder names, which are also their ids.</summary>
        IReadOnlyList<string> GameIds();

        /// <summary>Copies a bundled game's files into <paramref name="targetDirectory"/>.</summary>
        void Copy(string gameId, string targetDirectory);
    }

    /// <summary>Bundled games read from a folder, such as StreamingAssets on platforms that can read it directly.</summary>
    internal sealed class FolderBundledGames : IBundledGames
    {
        private readonly string _root;

        public FolderBundledGames(string root)
        {
            _root = root;
        }

        public IReadOnlyList<string> GameIds()
        {
            if (!Directory.Exists(_root))
                return Array.Empty<string>();
            return Directory.GetDirectories(_root)
                .Where(directory => File.Exists(Path.Combine(directory, BundledGameInstaller.GameFileName)))
                .Select(Path.GetFileName)
                .OrderBy(gameId => gameId, StringComparer.Ordinal)
                .ToList();
        }

        public void Copy(string gameId, string targetDirectory)
        {
            UnityFileMethods.CopyDirectory(Path.Combine(_root, gameId), targetDirectory);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>Bundled games read from the StreamingAssets inside the Android APK.</summary>
    internal sealed class AndroidBundledGames : IBundledGames
    {
        public IReadOnlyList<string> GameIds()
        {
            return UnityFileMethods.ListAndroidStreamingAssetsDirectories(BundledGameInstaller.GameFileName)
                .OrderBy(gameId => gameId, StringComparer.Ordinal)
                .ToList();
        }

        public void Copy(string gameId, string targetDirectory)
        {
            UnityFileMethods.ExtractAndroidStreamingAssetsDirectory(gameId, targetDirectory);
        }
    }
#endif

    /// <summary>
    /// Installs the bundled games into the games folder. One game can be installed right away, before the Main Menu
    /// shows, and the rest on a background thread. Each game is copied into a staging folder and then renamed into
    /// place, so a game in the games folder is always complete. The games still to install are saved in a pending
    /// list, so a launch that is closed early resumes on the next launch. A game is installed only on the first launch
    /// (or when the games folder has no games, as before), so a bundled game the player deletes stays deleted.
    /// </summary>
    internal sealed class BundledGameInstaller
    {
        internal const string GameFileName = "cgs.json";
        internal const string PendingFileName = "pending.txt";
        internal const string StagingDirectoryName = "staging";

        private readonly IBundledGames _bundle;
        private readonly string _gamesDirectory;
        private readonly string _stateDirectory;
        private readonly object _pendingLock = new();
        private readonly List<string> _pending = new();
        private readonly ConcurrentQueue<string> _installed = new();
        private Task _background;

        private string PendingFilePath => Path.Combine(_stateDirectory, PendingFileName);
        private string StagingDirectory => Path.Combine(_stateDirectory, StagingDirectoryName);

        /// <param name="bundle">Where the bundled games come from.</param>
        /// <param name="gamesDirectory">The writable games folder.</param>
        /// <param name="stateDirectory">A folder for the pending list and staging copies. It must be on the same
        /// volume as <paramref name="gamesDirectory"/>, so that moving a staged game into place is a rename.</param>
        public BundledGameInstaller(IBundledGames bundle, string gamesDirectory, string stateDirectory)
        {
            _bundle = bundle;
            _gamesDirectory = gamesDirectory;
            _stateDirectory = stateDirectory;
        }

        /// <summary>A copy of the games that have not been installed yet.</summary>
        public IReadOnlyList<string> GetPending()
        {
            lock (_pendingLock)
                return _pending.ToList();
        }

        /// <summary>Completes when the background installation has finished, successfully or not.</summary>
        public Task Background => _background ?? Task.CompletedTask;

        /// <summary>
        /// Decides which bundled games still need installing and discards copies that an earlier launch left
        /// unfinished. Run this before reading the games folder.
        /// </summary>
        public void Prepare()
        {
            Directory.CreateDirectory(_stateDirectory);
            List<string> pending;
            var isListKnown = true;
            if (!Directory.Exists(_gamesDirectory) || Directory.GetDirectories(_gamesDirectory).Length < 1)
                pending = _bundle.GameIds().ToList();
            else if (File.Exists(PendingFilePath))
                isListKnown = TryReadPending(out pending);
            else
                pending = new List<string>();

            lock (_pendingLock)
            {
                _pending.Clear();
                // The list is saved before the games folder exists, so a first launch closed now starts over. A list
                // that could not be read is left as it is, so the next launch can install those games.
                if (isListKnown)
                {
                    _pending.AddRange(pending);
                    SavePending();
                }
            }

            Directory.CreateDirectory(_gamesDirectory);
            // Each install also clears its own staging folder first, so this is only housekeeping
            DeleteQuietly(StagingDirectory);
        }

        private bool TryReadPending(out List<string> pending)
        {
            try
            {
                pending = File.ReadAllLines(PendingFilePath).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"BundledGameInstaller: could not read the pending list, so it waits for the next launch: {e.Message}");
                pending = new List<string>();
                return false;
            }
        }

        /// <summary>Installs <paramref name="gameId"/> now, if it is still pending.</summary>
        public void InstallNow(string gameId)
        {
            if (string.IsNullOrEmpty(gameId) || !GetPending().Contains(gameId))
                return;
            var stopwatch = Stopwatch.StartNew();
            if (TryInstall(gameId))
                Debug.Log($"BundledGameInstaller: installed {gameId} in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>Installs the other pending games on a background thread.</summary>
        public void StartBackground()
        {
            var remaining = GetPending();
            if (remaining.Count == 0)
            {
                DeleteQuietly(StagingDirectory);
                return;
            }

            _background = Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                foreach (var gameId in remaining.Where(TryInstall))
                    _installed.Enqueue(gameId);

                DeleteQuietly(StagingDirectory);
                Debug.Log($"BundledGameInstaller: installed {remaining.Count} more in {stopwatch.ElapsedMilliseconds} ms");
            });
        }

        /// <summary>Takes the next game that the background installation has finished, if any.</summary>
        public bool TryTakeInstalled(out string gameId)
        {
            return _installed.TryDequeue(out gameId);
        }

        private bool TryInstall(string gameId)
        {
            var target = Path.Combine(_gamesDirectory, gameId);
            var staging = Path.Combine(StagingDirectory, gameId);
            try
            {
                // An earlier launch may have renamed it into place just before closing, or the player may have
                // downloaded it since; either way it is complete and must not be replaced
                if (!Directory.Exists(target))
                {
                    if (Directory.Exists(staging))
                        Directory.Delete(staging, true);
                    Directory.CreateDirectory(StagingDirectory);
                    _bundle.Copy(gameId, staging);
                    Directory.Move(staging, target);
                }

                Complete(gameId);
                return true;
            }
            catch (Exception e) when (Directory.Exists(target))
            {
                // The game appeared while this copy ran, as when the player downloaded it meanwhile
                Debug.Log($"BundledGameInstaller: kept the existing {gameId}: {e.Message}");
                DeleteQuietly(staging);
                Complete(gameId);
                return false;
            }
            catch (Exception e)
            {
                // It stays pending, so the next launch tries again
                Debug.LogError($"BundledGameInstaller: could not install {gameId}: {e}");
                DeleteQuietly(staging);
                return false;
            }
        }

        private void Complete(string gameId)
        {
            lock (_pendingLock)
            {
                _pending.Remove(gameId);
                SavePending();
            }
        }

        // Callers hold _pendingLock. A failed save is only logged: a game that is in place counts as installed
        // whatever the list says, and the next launch completes it again when it finds it there.
        private void SavePending()
        {
            try
            {
                if (_pending.Count == 0)
                {
                    if (File.Exists(PendingFilePath))
                        File.Delete(PendingFilePath);
                    return;
                }

                // Replace the list in one step, so it is never seen half-written
                var temporaryPath = PendingFilePath + ".tmp";
                File.WriteAllLines(temporaryPath, _pending);
                if (File.Exists(PendingFilePath))
                    File.Replace(temporaryPath, PendingFilePath, null);
                else
                    File.Move(temporaryPath, PendingFilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"BundledGameInstaller: could not save the pending list: {e.Message}");
            }
        }

        private static void DeleteQuietly(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception e)
            {
                Debug.Log($"BundledGameInstaller: could not remove {directory}: {e.Message}");
            }
        }
    }
}
