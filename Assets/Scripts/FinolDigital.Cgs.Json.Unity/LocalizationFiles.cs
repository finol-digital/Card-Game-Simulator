/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityExtensionMethods;

namespace FinolDigital.Cgs.Json.Unity
{
    public static class LocalizationFiles
    {
        private static readonly Dictionary<string, object> Gates = new();
        private static readonly Dictionary<string, long> Installations = new();

        internal static long Installation(string gameDirectory, bool replace = false)
        {
            var root = Path.GetFullPath(gameDirectory);
            lock (Gate(root))
            {
                lock (Installations)
                {
                    Installations.TryGetValue(root, out var generation);
                    if (replace)
                        Installations[root] = ++generation;
                    return generation;
                }
            }
        }

        public static void CopyPackageSnapshot(string gameDirectory, string destination)
        {
            WithSnapshot(gameDirectory, () =>
            {
                UnityFileMethods.CopyDirectory(gameDirectory, destination);
                var staging = Path.Combine(destination, "localization", "staging");
                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);
                foreach (var pending in Directory.GetFiles(destination, "cgs.json.*.download"))
                    File.Delete(pending);
            });
        }

        internal static object Gate(string gameDirectory)
        {
            var root = Path.GetFullPath(gameDirectory);
            lock (Gates)
            {
                if (!Gates.TryGetValue(root, out var gate))
                    Gates[root] = gate = new object();
                return gate;
            }
        }

        public static void WithSnapshot(string gameDirectory, Action action)
        {
            lock (Gate(gameDirectory))
                action();
        }
        // .NET Standard has no portable realpath API. Reject links in every
        // ancestor rather than accepting a lexically confined path through one.
        public static string ExistingPhysicalPath(string path)
        {
            try
            {
                var full = Path.GetFullPath(path);
                if (!File.Exists(full) && !Directory.Exists(full))
                    return null;
                for (var current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        return null;
                return full;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is NotSupportedException)
            {
                return null;
            }
        }

        public static LocalizationOrigin ReadDefinitionOrigin(string gameDirectory, string documentPath)
        {
            try
            {
                if (ExistingPhysicalPath(gameDirectory) == null || ExistingPhysicalPath(documentPath) == null)
                    return null;
                var statePath = Path.Combine(gameDirectory, "localization", "state.json");
                if (File.Exists(statePath))
                {
                    if (ExistingPhysicalPath(statePath) == null)
                        return null;
                    var state = JObject.Parse(File.ReadAllText(statePath));
                    if (state.Value<int?>("version") != 1)
                        return null;
                    var origin = state["definitionOrigin"] as JObject;
                    if (origin != null)
                    {
                        if (origin.Value<string>("kind") == "bundled")
                            return LocalizationOrigin.Bundled(gameDirectory, documentPath, ExistingPhysicalPath);
                        if (origin.Value<string>("documentSha256") != Digest(documentPath))
                            return null;
                        if (origin.Value<string>("kind") == "remote" &&
                            Uri.TryCreate(origin.Value<string>("url"), UriKind.Absolute, out var remote) &&
                            (remote.Scheme == "https" || remote.Scheme == "http"))
                            return LocalizationOrigin.Remote(remote);
                        return null;
                    }
                }
                return LocalizationOrigin.Bundled(gameDirectory, documentPath, ExistingPhysicalPath);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Game-content origin unavailable: " + error.Message);
                return null;
            }
        }

        public static bool RecordDefinitionOrigin(string gameDirectory, string downloadedDocument, FileDownloadResult result)
        {
            lock (Gate(gameDirectory))
                return RecordOrigin(gameDirectory, downloadedDocument, result);
        }

        private static bool RecordOrigin(string gameDirectory, string downloadedDocument, FileDownloadResult result)
        {
            if (result == null || !result.Succeeded)
                return false;
            try
            {
                if (ExistingPhysicalPath(gameDirectory) == null)
                    return false;
                var directory = Path.Combine(gameDirectory, "localization");
                Directory.CreateDirectory(directory);
                if (ExistingPhysicalPath(directory) == null)
                    return false;
                var path = Path.Combine(directory, "state.json");
                var state = new JObject { ["version"] = 1 };
                if (File.Exists(path))
                {
                    if (ExistingPhysicalPath(path) == null)
                        return false;
                    try
                    {
                        var existing = JObject.Parse(File.ReadAllText(path));
                        if (existing.Value<int?>("version") == 1)
                            state = existing;
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Ignoring unsupported game-content state: " + error.Message);
                    }
                }
                state["definitionOrigin"] = new JObject
                {
                    ["kind"] = !result.IsRemote ? "bundled" : result.FinalDocumentUri == null ? "unavailable" : "remote",
                    ["url"] = result.FinalDocumentUri?.AbsoluteUri,
                    ["documentSha256"] = Digest(downloadedDocument)
                };
                WriteAtomically(path, state.ToString(Formatting.None));
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Could not record game-content origin: " + error.Message);
                return false;
            }
        }

        private static string Digest(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        internal static void WriteAtomically(string path, string content)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, content);
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }
}
