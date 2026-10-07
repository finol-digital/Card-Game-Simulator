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
        public void LanguageRow_BindsMixedFontsForCaptionAndInactiveItems()
        {
            var row = new UnityEngine.GameObject("Language Dropdown");
            row.SetActive(false);
            try
            {
                foreach (var name in new[] { "Label", "Caption", "Inactive item" })
                {
                    var child = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform),
                        typeof(UnityEngine.UI.Text));
                    child.transform.SetParent(row.transform);
                    child.SetActive(name != "Inactive item");
                }
                var bind = typeof(AuthoredUiBinder).GetMethod("BindLanguageFonts",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                bind.Invoke(null, new object[] { row });
                bind.Invoke(null, new object[] { row });
                var texts = row.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                Assert.AreEqual(3, texts.Length);
                foreach (var text in texts)
                {
                    var fonts = text.GetComponents<Cgs.Localization.LocalizedUiFont>();
                    Assert.AreEqual(1, fonts.Length, text.name);
                    Assert.AreEqual("mixed", new SerializedObject(fonts[0]).FindProperty("key").stringValue);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(row); }
        }

        [TestCase("text", false)]
        [TestCase("options.0", false)]
        [TestCase("options.-1", true)]
        [TestCase("options.1", true)]
        [TestCase("options.invalid", true)]
        public void AuthoredBinding_InvalidTargetIdentifiesRow(string property, bool addDropdown)
        {
            var root = new UnityEngine.GameObject("Binding test");
            root.SetActive(false);
            try
            {
                if (addDropdown)
                    root.AddComponent<UnityEngine.UI.Dropdown>().options.Add(
                        new UnityEngine.UI.Dropdown.OptionData("Original"));
                var row = new Newtonsoft.Json.Linq.JObject
                {
                    ["path"] = root.name, ["property"] = property,
                    ["key"] = "settings.language", ["text"] = "Original"
                };
                var bind = typeof(AuthoredUiBinder).GetMethod("Bind",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var error = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                    bind.Invoke(null, new object[] { new[] { root }, new Newtonsoft.Json.Linq.JToken[] { row } }));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
                StringAssert.Contains(root.name, error.InnerException.Message);
                Assert.IsNull(root.GetComponent<Cgs.Localization.LocalizedDropdown>());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void AuthoredBinding_ValidOptionStillChecksSourceAndBinds()
        {
            var root = new UnityEngine.GameObject("Binding test");
            root.SetActive(false);
            try
            {
                root.AddComponent<UnityEngine.UI.Dropdown>().options.Add(
                    new UnityEngine.UI.Dropdown.OptionData("Original"));
                var row = new Newtonsoft.Json.Linq.JObject
                {
                    ["path"] = root.name, ["property"] = "options.0",
                    ["key"] = "settings.language", ["text"] = "Changed"
                };
                var bind = typeof(AuthoredUiBinder).GetMethod("Bind",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var arguments = new object[] { new[] { root }, new Newtonsoft.Json.Linq.JToken[] { row } };
                var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => bind.Invoke(null, arguments));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
                StringAssert.Contains("Dropdown source changed", error.InnerException.Message);
                row["text"] = "Original";
                bind.Invoke(null, arguments);
                var adapter = root.GetComponent<Cgs.Localization.LocalizedDropdown>();
                Assert.IsNotNull(adapter);
                var options = new SerializedObject(adapter).FindProperty("options");
                Assert.AreEqual(1, options.arraySize);
                Assert.AreEqual("settings.language", options.GetArrayElementAtIndex(0).FindPropertyRelative("key").stringValue);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Import_UnsupportedLocaleWithoutAddressableEntryDoesNotBlockValidation()
        {
            var locale = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Localization/Locales" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Locale>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single(item => item.Identifier.Code == "af-ZA");
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(locale));
            Assert.IsNull(settings.FindAssetEntry(guid));
            try
            {
                LocalizationEditorSettings.AddLocale(locale);
                Assert.Contains(locale, LocalizationEditorSettings.GetLocales());
                settings.RemoveAssetEntry(guid, false);
                Assert.IsNull(settings.FindAssetEntry(guid));
                CatalogImporter.Import();
                Assert.IsFalse(LocalizationEditorSettings.GetLocales().Contains(locale));
                CatalogImporter.ValidateGenerated();
            }
            finally
            {
                LocalizationEditorSettings.RemoveLocale(locale);
                AssetDatabase.SaveAssets();
            }
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
