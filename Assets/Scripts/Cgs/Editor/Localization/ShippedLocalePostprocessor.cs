/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Localization;
using UnityEngine.Localization;

namespace Cgs.Editor.Localization
{
    // Localization registers every imported Locale, including unused assets kept
    // for future contributors. Apply the shipped manifest after that registration.
    public sealed class ShippedLocalePostprocessor : AssetPostprocessor
    {
        [RunAfterPackage("com.unity.localization")]
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var importedLocales = importedAssets
                .Where(path => path.StartsWith("Assets/Localization/Locales/", System.StringComparison.Ordinal)
                    && path.EndsWith(".asset", System.StringComparison.Ordinal))
                .Select(AssetDatabase.LoadAssetAtPath<Locale>).Where(locale => locale != null).ToArray();
            if (importedLocales.Length == 0)
                return;

            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(CatalogImporter.CatalogDirectory, "locales.json")));
            var supported = new HashSet<string>(manifest["locales"].Select(locale => (string)locale["code"]));
            foreach (var locale in importedLocales.Where(locale => !supported.Contains(locale.Identifier.Code)))
                LocalizationEditorSettings.RemoveLocale(locale);
        }
    }
}
