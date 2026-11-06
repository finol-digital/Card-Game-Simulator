/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FinolDigital.Cgs.Json.Unity
{
    // Shipped author catalogs, not machine translation or source-text substitution.
    public static class BuiltInGameContent
    {
        private static readonly Dictionary<string, JObject> Packages = new();
        private static readonly Dictionary<string, string> Names = new()
        {
            ["Standard Playing Cards"] = "Standard", ["Mahjong"] = "Mahjong", ["Dominoes"] = "Dominoes"
        };

        private static JObject Package(UnityCardGame game)
        {
            if (game == null || string.IsNullOrEmpty(game.Name) || game.AutoUpdateUrl?.IsAbsoluteUri != true || !Names.TryGetValue(game.Name, out var key))
                return null;
            if (!Packages.TryGetValue(key, out var package))
            {
                var asset = Resources.Load<TextAsset>("DefaultGameContent/" + key);
                if (asset == null)
                    return null;
                try
                {
                    package = JObject.Parse(asset.text);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Built-in game catalog unavailable: " + error.Message);
                    return null;
                }
                Packages[key] = package;
            }
            return game.AutoUpdateUrl?.AbsoluteUri == package.Value<string>("url") ? package : null;
        }

        internal static void ApplyCatalog(UnityCardGame game, JObject authoredDefinition)
        {
            // Explicit metadata (including null) always wins. Only the three shipped
            // author identities receive fallback when an older manifest lacks metadata.
            if (authoredDefinition.Properties().Any(property =>
                string.Equals(property.Name, "localization", StringComparison.OrdinalIgnoreCase)))
                return;
            var package = Package(game);
            if (package != null)
                game.Localization = LocalizationJson.ReadManifest(package["localization"].ToString()).Value;
        }

        public static bool IsManagedDeck(UnityCardGame game, string path) =>
            !string.IsNullOrEmpty(path) && Path.GetDirectoryName(Path.GetFullPath(path)) ==
            Path.GetFullPath(Path.Combine(game.GameDirectoryPath, "localization", "default-decks"));

        public static string DeckPath(UnityCardGame game, string name, string source = null)
        {
            if (game.Localization?.IsUsable != true || Package(game)?["decks"]?[name] is not JObject deck ||
                (source != null && source != deck.Value<string>("source")))
                return null;
            if (!game.DeckUrls.Concat(game.GameStartDecks).Any(value => value.Name == name &&
                value.Url?.IsAbsoluteUri == true && value.Url.AbsoluteUri == deck.Value<string>("source")))
                return null;
            var canonical = Path.Combine(game.DecksDirectoryPath, name + ".txt");
            // A valid installation sidecar can prove a bundled/downloaded file. Name
            // or content matches alone can never reclassify a legacy/user deck.
            if (AuthoredDeckFiles.AuthoredName(game, canonical) == name)
                return canonical;
            var managed = Path.Combine(game.GameDirectoryPath, "localization", "default-decks", name + ".txt");
            return AuthoredDeckFiles.AuthoredName(game, managed) == name ? managed : null;
        }

        public static IEnumerable<string> DeckPaths(UnityCardGame game)
        {
            if (game.Localization?.IsUsable != true || Package(game)?["decks"] is not JObject decks)
                yield break;
            foreach (var deck in decks.Properties())
            {
                var path = DeckPath(game, deck.Name);
                if (path != null)
                    yield return path;
            }
        }

        internal static void EnsureDecks(UnityCardGame game)
        {
            if (game.Localization?.IsUsable != true || Package(game)?["decks"] is not JObject decks)
                return;
            foreach (var deck in decks.Properties())
            {
                var source = deck.Value.Value<string>("source");
                if (!game.DeckUrls.Concat(game.GameStartDecks).Any(value => value.Name == deck.Name && value.Url?.IsAbsoluteUri == true && value.Url.AbsoluteUri == source) ||
                    DeckPath(game, deck.Name, source) != null)
                    continue;
                var path = Path.Combine(game.GameDirectoryPath, "localization", "default-decks", deck.Name + ".txt");
                AuthoredDeckFiles.InstallBundled(game, deck.Name, source,
                    Encoding.UTF8.GetBytes(deck.Value.Value<string>("text")), path);
            }
        }
    }
}
