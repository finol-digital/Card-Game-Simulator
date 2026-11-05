/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FinolDigital.Cgs.Json.Unity
{
    public sealed class LocalizationCache
    {
        public sealed class Request
        {
            internal LocalizationCache Owner;
            internal string Key;
            internal string Category;
            internal long Ticket;
            internal JObject Identity;
        }

        public sealed class Entry
        {
            public byte[] Payload { get; internal set; }
            public string FilePath { get; internal set; }
            public Uri FinalDocumentUri { get; internal set; }
            public DateTime PublishedUtc { get; internal set; }
            public long Ticket { get; internal set; }
        }

        private static readonly Dictionary<string, LocalizationCache> Owners = new();
        private readonly Dictionary<string, long> _started = new();
        private readonly string _root;
        private readonly string _gameId;
        private readonly object _gate;
        private string DirectoryPath => Path.Combine(_root, "localization");
        private string StatePath => Path.Combine(DirectoryPath, "state.json");

        public LocalizationCache(string gameDirectory, string canonicalGameId)
        {
            _root = Path.GetFullPath(gameDirectory);
            _gameId = canonicalGameId;
            _gate = LocalizationFiles.Gate(_root);
            lock (_gate)
            {
                lock (Owners)
                    Owners[_root] = this;
            }
        }

        public static void Invalidate(string gameDirectory)
        {
            var root = Path.GetFullPath(gameDirectory);
            lock (LocalizationFiles.Gate(root))
            {
                LocalizationFiles.Installation(root, true);
                lock (Owners)
                    Owners.Remove(root);
            }
        }

        public bool IsCurrentOwner => IsCurrent();

        private bool IsCurrent()
        {
            lock (Owners)
                return Owners.TryGetValue(_root, out var owner) && ReferenceEquals(owner, this);
        }

        public Request Begin(string locale, Uri source, string assetTarget = null)
        {
            if (source == null || !source.IsAbsoluteUri || (source.Scheme != "https" && source.Scheme != "http"))
                throw new ArgumentException("Cache sources must be resolved HTTP(S) URLs.", nameof(source));
            var category = assetTarget == null ? "catalogs" : "assets";
            var identity = new JObject
            {
                ["game"] = _gameId, ["locale"] = locale.ToLowerInvariant(),
                ["source"] = source.AbsoluteUri, ["target"] = assetTarget ?? string.Empty, ["category"] = category
            };
            var key = Hash(Encoding.UTF8.GetBytes(identity.ToString(Formatting.None)));
            lock (_gate)
            {
                if (!IsCurrent())
                    throw new InvalidOperationException("The localization cache owner was replaced.");
                var published = (ReadState()["entries"] as JObject)?[key]?.Value<long?>("ticket") ?? 0;
                _started.TryGetValue(key, out var started);
                var ticket = checked(Math.Max(started, published) + 1);
                _started[key] = ticket;
                return new Request { Owner = this, Key = key, Category = category, Ticket = ticket, Identity = identity };
            }
        }

        public Entry Read(Request request)
        {
            if (request == null || request.Owner != this)
                return null;
            lock (_gate)
            {
                if (!IsCurrent())
                    return null;
                try
                {
                    var metadata = (ReadState()["entries"] as JObject)?[request.Key] as JObject;
                    if (metadata == null || !JToken.DeepEquals(metadata["identity"], request.Identity))
                        return null;
                    var name = metadata.Value<string>("file");
                    if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name) ||
                        !name.StartsWith(request.Key + ".", StringComparison.Ordinal) || !name.EndsWith(".data", StringComparison.Ordinal))
                        return null;
                    var path = Path.Combine(DirectoryPath, request.Category, name);
                    if (LocalizationFiles.ExistingPhysicalPath(path, _root) == null)
                        return null;
                    var bytes = File.ReadAllBytes(path);
                    if (Hash(bytes) != metadata.Value<string>("sha256"))
                        return null;
                    Uri.TryCreate(metadata.Value<string>("origin"), UriKind.Absolute, out var origin);
                    if (origin != null && origin.Scheme != "http" && origin.Scheme != "https")
                        return null;
                    return new Entry
                    {
                        Payload = bytes, FilePath = path, FinalDocumentUri = origin,
                        Ticket = metadata.Value<long>("ticket"),
                        PublishedUtc = new DateTime(metadata.Value<long>("publishedUtcTicks"), DateTimeKind.Utc)
                    };
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Game-content cache entry unavailable: " + error.Message);
                    return null;
                }
            }
        }

        // Validation must establish usability (catalog structure or decoded image),
        // not just HTTP success. Failures never advance the successful ticket.
        public bool Publish(Request request, byte[] payload, Uri finalDocumentUri, Func<byte[], bool> isUsable,
            JObject validators = null)
        {
            if (request == null || request.Owner != this || payload == null || isUsable == null ||
                (finalDocumentUri != null && (!finalDocumentUri.IsAbsoluteUri ||
                    (finalDocumentUri.Scheme != "http" && finalDocumentUri.Scheme != "https"))))
                return false;
            try
            {
                if (!isUsable(payload))
                    return false;
                lock (_gate)
                {
                    if (!IsCurrent() || LocalizationFiles.ExistingPhysicalPath(_root, _root) == null)
                        return false;
                    if (File.Exists(StatePath) && LocalizationFiles.ExistingPhysicalPath(StatePath, _root) == null)
                        return false;
                    var state = ReadState();
                    var entries = state["entries"] as JObject ?? new JObject();
                    var previousFile = entries[request.Key]?.Value<string>("file");
                    var published = entries[request.Key]?.Value<long?>("ticket") ?? 0;
                    if (request.Ticket <= published)
                        return false;
                    var directory = Path.Combine(DirectoryPath, request.Category);
                    if (LocalizationFiles.ConfinedPath(directory, _root) == null)
                        return false;
                    Directory.CreateDirectory(directory);
                    if (LocalizationFiles.ExistingPhysicalPath(directory, _root) == null)
                        return false;
                    var name = request.Key + "." + Guid.NewGuid().ToString("N") + ".data";
                    var path = Path.Combine(directory, name);
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        stream.Write(payload, 0, payload.Length);
                    try
                    {
                        entries[request.Key] = new JObject
                        {
                            ["identity"] = request.Identity.DeepClone(), ["ticket"] = request.Ticket,
                            ["file"] = name, ["sha256"] = Hash(payload), ["origin"] = finalDocumentUri?.AbsoluteUri,
                            ["publishedUtcTicks"] = DateTime.UtcNow.Ticks, ["validators"] = validators?.DeepClone()
                        };
                        state["entries"] = entries;
                        LocalizationFiles.WriteAtomically(StatePath, state.ToString(Formatting.None));
                    }
                    catch
                    {
                        File.Delete(path);
                        throw;
                    }
                    if (previousFile != null && previousFile == Path.GetFileName(previousFile) &&
                        previousFile.StartsWith(request.Key + ".", StringComparison.Ordinal) && previousFile.EndsWith(".data", StringComparison.Ordinal))
                    {
                        try { File.Delete(Path.Combine(directory, previousFile)); }
                        catch (Exception error) { Debug.LogWarning("Superseded localization cache cleanup failed: " + error.Message); }
                    }
                    return true;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Game-content cache publication failed: " + error.Message);
                return false;
            }
        }

        private JObject ReadState()
        {
            if (LocalizationFiles.ExistingPhysicalPath(StatePath, _root) != null)
            {
                try
                {
                    var state = JObject.Parse(File.ReadAllText(StatePath));
                    if (state.Value<int?>("version") == 1)
                        return state;
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Ignoring unsupported game-content cache: " + error.Message);
                }
            }
            return new JObject { ["version"] = 1 };
        }

        private static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
