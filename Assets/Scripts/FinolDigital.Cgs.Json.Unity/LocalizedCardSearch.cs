/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;

namespace FinolDigital.Cgs.Json.Unity
{
    // UI-only adapter. Imports and DeckPlayCard queries continue using FilterCards.
    public static class LocalizedCardSearch
    {
        public static IEnumerable<UnityCard> Search(UnityCardGame game, CardSearchFilters filters,
            LocalizationContext display)
        {
            if (filters == null || game.Localization == null || !game.Localization.IsUsable)
                return game.FilterCards(filters);
            if (display == null)
                throw new ArgumentNullException(nameof(display));

            var canonical = new CardSearchFilters
            {
                Id = filters.Id,
                SetCode = filters.SetCode,
                BackFaceId = filters.BackFaceId
            };
            foreach (var pair in filters.IntMinProperties) canonical.IntMinProperties.Add(pair.Key, pair.Value);
            foreach (var pair in filters.IntMaxProperties) canonical.IntMaxProperties.Add(pair.Key, pair.Value);
            foreach (var pair in filters.BoolProperties) canonical.BoolProperties.Add(pair.Key, pair.Value);
            foreach (var pair in filters.EnumProperties) canonical.EnumProperties.Add(pair.Key, pair.Value);
            var words = (filters.Name ?? string.Empty).ToLower().Split(new[] { CardSearchFilters.Delimiter },
                StringSplitOptions.RemoveEmptyEntries);
            // Preserve the caller's mutable controls and canonical result order.
            var textFilters = filters.StringProperties.ToArray();
            return game.FilterCards(canonical).Where(card =>
                words.All(word => card.Name.ToLower().Contains(word) || display.CardName(card).ToLower().Contains(word)) &&
                textFilters.All(pair => card.GetPropertyValueString(pair.Key).ToLower().Contains(pair.Value.ToLower()) ||
                    display.PropertyValue(card, pair.Key).ToLower().Contains(pair.Value.ToLower())));
        }
    }
}
