/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Cgs.Localization;
using FinolDigital.Cgs.Json;

namespace Cgs.GameContent
{
    public static class GameContentDiagnostics
    {
        public sealed class CatalogReport
        {
            public string Locale { get; internal set; }
            public bool Reachable { get; internal set; }
            public LocalizationCoverage Coverage { get; internal set; }
            public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; internal set; }
        }

        // Uses selectable UI codes, not device-language aliases or sibling matching.
        public static IReadOnlyList<CatalogReport> Inspect(GameLocalization manifest, LocalizationTargets targets,
            IReadOnlyDictionary<string, LocalizationCatalogDocument> available = null,
            IEnumerable<string> selectableLocales = null)
        {
            if (manifest?.IsUsable != true || manifest.Locales == null)
                return Array.Empty<CatalogReport>();
            var selectable = selectableLocales ?? CgsLocalization.Languages.Select(language => language.Code);
            var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var locale in selectable.Append(manifest.DefaultLocale))
                reachable.UnionWith(LocalizationLocale.FallbackChain(locale, manifest.DefaultLocale));
            var reports = new List<CatalogReport>();
            foreach (var source in manifest.Locales)
            {
                var diagnostics = new List<LocalizationDiagnostic>();
                var canReach = reachable.Contains(source.Key);
                if (!canReach)
                    diagnostics.Add(new LocalizationDiagnostic("unreachable-locale", "/locales/" + source.Key,
                        "No selectable CGS language or default fallback reaches this catalog; sibling locales are not matched.", true));
                var catalog = source.Value.Inline;
                if (catalog == null && available != null)
                    catalog = available.FirstOrDefault(pair => pair.Key.Equals(source.Key, StringComparison.OrdinalIgnoreCase)).Value?.Catalog;
                if (catalog != null)
                    diagnostics.AddRange(targets.Validate(catalog));
                reports.Add(new CatalogReport
                {
                    Locale = source.Key, Reachable = canReach,
                    Coverage = catalog == null ? null : targets.Coverage(catalog),
                    Diagnostics = diagnostics.AsReadOnly()
                });
            }
            return reports.AsReadOnly();
        }
    }
}
