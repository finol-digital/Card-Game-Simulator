/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;

namespace Cgs.GameContent
{
    // Canonical references only. Possessing a matching name does not establish a binding.
    public sealed class GameContentLabel
    {
        public string Kind { get; set; }
        public string Key { get; set; }

        public static GameContentLabel ForDeck(UnityCardGame game, UnityDeck deck)
        {
            if (game.Localization?.IsUsable != true)
                return null;
            if (!string.IsNullOrEmpty(game.GamePlayDeckName))
                return new GameContentLabel { Kind = "gamePlayDeckName", Key = game.GamePlayDeckName };
            var authored = deck.AuthoredName;
            return authored == null ? null : new GameContentLabel { Kind = "authoredDeck", Key = authored };
        }

        public static GameContentLabel ForExtraGroup(UnityCardGame game, string group) =>
            game.Localization?.IsUsable == true ? new GameContentLabel { Kind = "extraGroup", Key = group } : null;

        public bool IsValid(UnityCardGame game, string canonicalName)
        {
            if (game?.Localization?.IsUsable != true || string.IsNullOrEmpty(Key) || Key != canonicalName)
                return false;
            try
            {
                return Kind switch
                {
                    "gamePlayDeckName" => Key == game.GamePlayDeckName,
                    "extraGroup" => game.Extras?.Any(extra => Key ==
                        (string.IsNullOrEmpty(extra.Group) ? ExtraDef.DefaultExtraGroup : extra.Group)) == true,
                    "authoredDeck" => game.DeckUrls?.Any(deck => deck.Name == Key) == true ||
                        game.GameStartDecks?.Any(deck => deck.Name == Key) == true ||
                        AuthoredDeckFiles.Names(game).Contains(Key, StringComparer.Ordinal),
                    _ => false
                };
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogWarning("Optional label provenance unavailable: " + error.Message);
                return false;
            }
        }

        public string Resolve(LocalizationContext context, string canonicalName) => Kind switch
        {
            "gamePlayDeckName" => context.GamePlayDeckName(canonicalName),
            "extraGroup" => context.ExtraGroupName(Key),
            "authoredDeck" => context.DeckName(Key, true),
            _ => canonicalName
        };
    }
}
