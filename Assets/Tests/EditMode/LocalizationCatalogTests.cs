/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Cgs.Editor.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Localization;

namespace Tests.EditMode
{
    public class LocalizationCatalogTests
    {
        private static string Snapshot()
        {
            using var hash = SHA256.Create();
            return string.Join("\n", new[] { "Assets/Localization", "Assets/AddressableAssetsData" }
                .SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => path + ":" + Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)))));
        }

        [Test]
        public void BundledFonts_CoverCatalogsAndEveryNativeName()
        {
            FontCatalogBuilder.ValidateGlyphs();
        }

        [Test]
        public void GeneratedTables_MatchValidatedCatalogs()
        {
            CatalogImporter.ValidateGenerated();
        }

        [Test]
        public void ImportTwice_PreservesIdsGuidsAndSerializedContent()
        {
            CatalogImporter.Import();
            var first = Snapshot();
            CatalogImporter.Import();
            Assert.AreEqual(first, Snapshot(), "Repeat import must not change any generated asset or metadata.");
        }

        [Test]
        public void InvalidCatalog_DoesNotMutateAssets()
        {
            var directory = Path.Combine("Temp", "InvalidLocalization-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                foreach (var source in Directory.GetFiles(CatalogImporter.CatalogDirectory, "*.json"))
                    File.Copy(source, Path.Combine(directory, Path.GetFileName(source)));
                File.WriteAllText(Path.Combine(directory, "es.json"), "{\"schemaVersion\":1,\"locale\":\"es\",\"entries\":{}}");
                var before = Snapshot();
                Assert.Throws<BuildFailedException>(() => CatalogImporter.ImportDirectory(directory));
                Assert.AreEqual(before, Snapshot());
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void ReimportUnusedLocale_DoesNotEnableUnshippedLanguage()
        {
            var path = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Localization/Locales" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Single(assetPath => AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Locale>(assetPath)
                    .Identifier.Code == "af-ZA");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Assert.IsFalse(LocalizationEditorSettings.GetLocales().Any(locale => locale.Identifier.Code == "af-ZA"));
            CatalogImporter.ValidateGenerated();
        }

        [Test]
        public void StaleContentBuildPolicy_IsRejectedBeforeBuild()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var previous = settings.BuildAddressablesWithPlayerBuild;
            try
            {
                settings.BuildAddressablesWithPlayerBuild =
                    UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.PreferencesValue;
                Assert.Throws<BuildFailedException>(CatalogImporter.ValidateGenerated);
            }
            finally
            {
                settings.BuildAddressablesWithPlayerBuild = previous;
            }
        }

        [Test]
        public void TableDrift_IsRejectedBeforeBuild()
        {
            var table = LocalizationEditorSettings.GetStringTableCollection("CgsUi").GetTable("es")
                as UnityEngine.Localization.Tables.StringTable;
            var entry = table.GetEntry("settings.language");
            var previous = entry.Value;
            try
            {
                entry.Value = "drift";
                Assert.Throws<BuildFailedException>(CatalogImporter.ValidateGenerated);
            }
            finally
            {
                entry.Value = previous;
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
