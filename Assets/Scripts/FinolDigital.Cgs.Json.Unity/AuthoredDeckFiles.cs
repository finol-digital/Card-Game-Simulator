/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityExtensionMethods;

namespace FinolDigital.Cgs.Json.Unity
{
    // Sidecars attest an installation event; a matching name or card list never does.
    public static class AuthoredDeckFiles
    {
        public sealed class Binding
        {
            public string Name { get; internal set; }
            public string Generation { get; internal set; }
        }

        private static readonly Dictionary<string, long> Mutations = new();
        private static string StatePath(UnityCardGame game) => Path.Combine(game.GameDirectoryPath, "localization", "provenance.json");

        private static string Key(UnityCardGame game, string path) => Key(game.GameDirectoryPath, path);

        private static string Key(string root, string path)
        {
            var full = Path.GetFullPath(path);
            var directory = Path.GetFullPath(Path.Combine(root, "decks"));
            var defaults = Path.GetFullPath(Path.Combine(root, "localization", "default-decks"));
            if (Path.GetDirectoryName(full) == defaults)
                return "defaults/" + Path.GetFileName(full);
            if (Path.GetDirectoryName(full) != directory)
                throw new ArgumentException("Authored deck artifacts must be direct children of the game's deck directory.", nameof(path));
            return Path.GetFileName(full);
        }

        private static JObject ReadState(UnityCardGame game) => ReadState(game.GameDirectoryPath, game.Id);

        private static JObject ReadState(string root, string gameId)
        {
            var path = Path.Combine(root, "localization", "provenance.json");
            if (LocalizationFiles.ExistingPhysicalPath(path, root) != null)
            {
                try
                {
                    var state = JObject.Parse(File.ReadAllText(path));
                    if (state.Value<int?>("version") == 1 && state.Value<string>("game") == gameId)
                        return state;
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Authored deck provenance unavailable: " + error.Message);
                }
            }
            return new JObject { ["version"] = 1, ["game"] = gameId, ["decks"] = new JObject() };
        }

        private static void WriteState(UnityCardGame game, JObject state)
        {
            var path = StatePath(game);
            var directory = Path.GetDirectoryName(path);
            if (LocalizationFiles.ExistingPhysicalPath(game.GameDirectoryPath, game.GameDirectoryPath) == null)
                throw new IOException("The installed game directory is unavailable.");
            if (LocalizationFiles.ConfinedPath(directory, game.GameDirectoryPath) == null)
                throw new IOException("Authored deck provenance cannot follow filesystem links.");
            Directory.CreateDirectory(directory);
            if (LocalizationFiles.ExistingPhysicalPath(directory, game.GameDirectoryPath) == null ||
                (File.Exists(path) && LocalizationFiles.ExistingPhysicalPath(path, game.GameDirectoryPath) == null))
                throw new IOException("Authored deck provenance cannot follow filesystem links.");
            LocalizationFiles.WriteAtomically(path, state.ToString(Formatting.None));
        }

        public static IEnumerable<string> Names(UnityCardGame game)
        {
            if (game.Localization?.IsUsable != true || !Directory.Exists(game.DecksDirectoryPath))
                yield break;
            foreach (var path in Directory.GetFiles(game.DecksDirectoryPath))
            {
                var name = AuthoredName(game, path);
                if (name != null)
                    yield return name;
            }
        }

        public static string AuthoredName(UnityCardGame game, string path) => ReadBinding(game, path)?.Name;

        public static Binding ReadBinding(UnityCardGame game, string path)
        {
            return game.Localization?.IsUsable == true ? ReadBinding(game.GameDirectoryPath, game.Id, path) : null;
        }

        // Also supports validating a portable package before installing it.
        public static Binding ReadBinding(string root, string gameId, string path)
        {
            lock (LocalizationFiles.Gate(root))
            {
                try
                {
                    var key = Key(root, path);
                    if (LocalizationFiles.ExistingPhysicalPath(path, root) == null)
                        return null;
                    var entry = (ReadState(root, gameId)["decks"] as JObject)?[key] as JObject;
                    if (entry == null || !Guid.TryParseExact(entry.Value<string>("generation"), "N", out _) ||
                        string.IsNullOrEmpty(entry.Value<string>("name")) ||
                        !Uri.TryCreate(entry.Value<string>("source"), UriKind.Absolute, out var source) ||
                        (source.Scheme != "https" && source.Scheme != "http") ||
                        entry.Value<string>("sha256") != Fingerprint(File.ReadAllBytes(path)))
                        return null;
                    return new Binding { Name = entry.Value<string>("name"), Generation = entry.Value<string>("generation") };
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Authored deck binding unavailable: " + error.Message);
                    return null;
                }
            }
        }

        // Must complete before user writes/deletes, even when the bytes are identical.
        public static void Revoke(UnityCardGame game, string path)
        {
            lock (LocalizationFiles.Gate(game.GameDirectoryPath))
            {
                var key = Key(game, path);
                var full = Path.GetFullPath(path);
                lock (Mutations)
                {
                    Mutations.TryGetValue(full, out var version);
                    Mutations[full] = checked(version + 1);
                }
                if (!File.Exists(StatePath(game)))
                    return;
                var state = ReadState(game);
                (state["decks"] as JObject)?.Remove(key);
                WriteState(game, state);
            }
        }

        public static void WriteUserFile(UnityCardGame game, string path, string text)
        {
            lock (LocalizationFiles.Gate(game.GameDirectoryPath))
            {
                Revoke(game, path);
                Directory.CreateDirectory(game.DecksDirectoryPath);
                File.WriteAllText(path, text);
            }
        }

        public static IEnumerator Download(UnityCardGame game, string name, string source, string path,
            Func<string, string, Action<FileDownloadResult>, IEnumerator> download = null)
        {
            if (BuiltInGameContent.DeckPath(game, name, source) != null)
                yield break;
            if (game.Localization?.IsUsable != true)
            {
                yield return UnityFileMethods.SaveUrlToFile(source, path);
                yield break;
            }
            var definition = game.DefinitionGeneration;
            var root = game.GameDirectoryPath;
            var full = LocalizationFiles.ConfinedPath(path, root);
            if (full == null || !full.StartsWith(Path.GetFullPath(game.DecksDirectoryPath) + Path.DirectorySeparatorChar,
                Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                Debug.LogWarning("Authored deck path escapes the deck directory or follows a filesystem link.");
                yield break;
            }
            // Legacy definitions may put decks in subdirectories. Keep downloading
            // those safely, but never infer a direct-file provenance binding for them.
            if (Path.GetDirectoryName(full) != Path.GetFullPath(game.DecksDirectoryPath))
            {
                yield return download == null ? UnityFileMethods.SaveUrlToFile(source, full) : download(source, full, _ => { });
                yield break;
            }
            var key = Key(game, full);
            var installation = LocalizationFiles.Installation(root);
            long version;
            lock (Mutations)
                Mutations.TryGetValue(full, out version);
            var staging = Path.Combine(root, "localization", "staging");
            if (!PrepareStaging(root, staging))
                yield break;
            var temporary = Path.Combine(staging, Guid.NewGuid().ToString("N") + ".download");
            try
            {
                FileDownloadResult result = null;
                yield return download == null
                    ? UnityFileMethods.SaveUrlToFile(source, temporary, completed: value => result = value)
                    : download(source, temporary, value => result = value);
                if (result?.Succeeded == true && root == game.GameDirectoryPath && definition == game.DefinitionGeneration)
                    TryInstall(game, name, source, temporary, full, key, version, installation);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Staged authored deck cleanup failed: " + error.Message);
                }
            }
        }

        private static bool PrepareStaging(string root, string directory)
        {
            try
            {
                if (LocalizationFiles.ExistingPhysicalPath(root, root) == null)
                    return false;
                if (LocalizationFiles.ConfinedPath(directory, root) == null)
                    return false;
                Directory.CreateDirectory(directory);
                return LocalizationFiles.ExistingPhysicalPath(directory, root) != null;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Authored deck staging unavailable: " + error.Message);
                return false;
            }
        }

        internal static void InstallBundled(UnityCardGame game, string name, string source, byte[] payload, string path)
        {
            var root = game.GameDirectoryPath;
            var directory = Path.GetDirectoryName(path);
            var staging = Path.Combine(root, "localization", "staging");
            if (!BuiltInGameContent.IsManagedDeck(game, path) || LocalizationFiles.ConfinedPath(path, root) == null ||
                !PrepareStaging(root, staging))
                return;
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(staging, Guid.NewGuid().ToString("N") + ".download");
            try
            {
                File.WriteAllBytes(temporary, payload);
                long version;
                lock (Mutations)
                    Mutations.TryGetValue(path, out version);
                Install(game, name, source, temporary, path, Key(game, path), version, LocalizationFiles.Installation(root));
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static void TryInstall(UnityCardGame game, string name, string source, string temporary,
            string path, string key, long expectedVersion, long installation)
        {
            try
            {
                Install(game, name, source, temporary, path, key, expectedVersion, installation);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Authored deck installation failed: " + error.Message);
            }
        }

        private static void Install(UnityCardGame game, string name, string source, string temporary,
            string path, string key, long expectedVersion, long installation)
        {
            lock (LocalizationFiles.Gate(game.GameDirectoryPath))
            {
                lock (Mutations)
                {
                    Mutations.TryGetValue(path, out var version);
                    if (version != expectedVersion || LocalizationFiles.Installation(game.GameDirectoryPath) != installation)
                        return;
                }
                if (LocalizationFiles.ExistingPhysicalPath(temporary, game.GameDirectoryPath) == null ||
                    (File.Exists(path) && LocalizationFiles.ExistingPhysicalPath(path, game.GameDirectoryPath) == null))
                    return;
                var payload = File.ReadAllBytes(temporary);
                if (payload.Length == 0)
                    return;
                Directory.CreateDirectory(game.DecksDirectoryPath);
                if (LocalizationFiles.ExistingPhysicalPath(game.DecksDirectoryPath, game.GameDirectoryPath) == null)
                    return;
                // Revoke first: interruption cannot leave stale provenance on new bytes.
                Revoke(game, path);
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
                var state = ReadState(game);
                var decks = state["decks"] as JObject ?? new JObject();
                decks[key] = new JObject
                {
                    ["generation"] = Guid.NewGuid().ToString("N"), ["name"] = name,
                    ["source"] = source, ["sha256"] = Fingerprint(payload)
                };
                state["decks"] = decks;
                WriteState(game, state);
                game.NotifyContentChanged();
            }
        }

        private static string Fingerprint(byte[] payload)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
