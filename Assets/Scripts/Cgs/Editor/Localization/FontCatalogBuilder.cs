/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace Cgs.Editor.Localization
{
    public static class FontCatalogBuilder
    {
        private const string DirectoryPath = "Assets/Localization/Fonts";
        private const string Japanese = "Assets/Fonts/Noto Sans JP/NotoSansJP-Regular.otf";
        private const string Chinese = "Assets/Fonts/Noto Sans CJK/NotoSansCJKsc-Regular.otf";
        private const string Korean = "Assets/Fonts/Noto Sans CJK/NotoSansCJKkr-Regular.otf";

        [MenuItem("CGS/Localization/Build Font Tables")]
        public static void Build()
        {
            Directory.CreateDirectory(DirectoryPath);
            AssetDatabase.Refresh();
            var locales = LocalizationEditorSettings.GetLocales();
            var collection = LocalizationEditorSettings.GetAssetTableCollection("CgsFonts")
                ?? LocalizationEditorSettings.CreateAssetTableCollection("CgsFonts", DirectoryPath, locales);
            var sources = new Dictionary<string, string>
            {
                ["body.regular"] = "Assets/TextMesh Pro/Fonts/Open Sans/OpenSans-Regular.ttf",
                ["body.bold"] = "Assets/TextMesh Pro/Fonts/Open Sans/OpenSans-Bold.ttf",
                ["heading.regular"] = "Assets/TextMesh Pro/Fonts/Exo_2/Exo2-Regular.ttf",
                ["heading.bold"] = "Assets/TextMesh Pro/Fonts/Exo_2/Exo2-Bold.ttf",
                ["mixed"] = Chinese
            };
            foreach (var locale in locales)
            foreach (var pair in sources)
            {
                var path = pair.Key == "mixed" ? Chinese : locale.Identifier.Code switch
                {
                    "zh-Hans" => Chinese,
                    "ko" => Korean,
                    "ja" => Japanese,
                    _ => pair.Value
                };
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font == null)
                    throw new System.InvalidOperationException("Missing bundled font: " + path);
                collection.AddAssetToTable(locale.Identifier, pair.Key, font);
                collection.AddAssetToTable(locale.Identifier, "tmp." + pair.Key, GetTmp(font));
            }
            foreach (var table in collection.AssetTables)
            {
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                EditorUtility.SetDirty(table);
            }
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var group in settings.groups.Where(group => group != null && group.name.StartsWith("Localization")))
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;
                schema.BuildPath.SetVariableByName(settings, "Local.BuildPath");
                schema.LoadPath.SetVariableByName(settings, "Local.LoadPath");
                schema.IncludeInBuild = true;
                EditorUtility.SetDirty(schema);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset GetTmp(Font font)
        {
            var path = DirectoryPath + "/" + font.name + " SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null)
                return asset;
            asset = TMP_FontAsset.CreateFontAsset(font, 40, 4, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = font.name + " SDF";
            asset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures)
                AssetDatabase.AddObjectToAsset(texture, asset);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        public static void ValidateGlyphs()
        {
            var collection = LocalizationEditorSettings.GetAssetTableCollection("CgsFonts");
            if (collection == null)
                throw new System.InvalidOperationException("Missing CgsFonts collection.");
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(CatalogImporter.CatalogDirectory, "locales.json")));
            var nativeNames = string.Join(" ", manifest["locales"].Select(locale => (string)locale["nativeName"]));
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var catalog = JObject.Parse(File.ReadAllText(Path.Combine(CatalogImporter.CatalogDirectory, locale.Identifier.Code + ".json")));
                var characters = string.Join("", ((JObject)catalog["entries"]).Properties().Select(entry => (string)entry.Value["text"]));
                var table = (AssetTable)collection.GetTable(locale.Identifier);
                if (table == null || !LocalizationEditorSettings.GetPreloadTableFlag(table))
                    throw new System.InvalidOperationException("Missing preloaded font table: " + locale.Identifier.Code);
                CatalogImporter.ValidateLocalAsset(table);
                foreach (var key in new[] { "body.regular", "body.bold", "heading.regular", "heading.bold", "mixed" })
                {
                    var fontEntry = table.GetEntry(key);
                    var tmpEntry = table.GetEntry("tmp." + key);
                    if (fontEntry == null || tmpEntry == null)
                        throw new System.InvalidOperationException("Invalid saved font assets: " + locale.Identifier.Code + "/" + key);
                    var font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(fontEntry.Guid));
                    var tmp = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(tmpEntry.Guid));
                    if (font == null || tmp == null || tmp.sourceFontFile != font || tmp.material == null
                        || !AssetDatabase.Contains(tmp.material) || tmp.atlasTextures.Any(texture => !AssetDatabase.Contains(texture))
                        || !tmp.isMultiAtlasTexturesEnabled)
                        throw new System.InvalidOperationException("Invalid saved font assets: " + locale.Identifier.Code + "/" + key);
                    CatalogImporter.ValidateLocalAsset(font);
                    CatalogImporter.ValidateLocalAsset(tmp);
                    if (FontEngine.LoadFontFace(font, 40) != FontEngineError.Success)
                        throw new System.InvalidOperationException("Could not read bundled font: " + font.name);
                    var missing = (key == "mixed" ? nativeNames : characters).Distinct()
                        .Where(character => !char.IsControl(character)
                            && !FontEngine.TryGetGlyphWithUnicodeValue(character, GlyphLoadFlags.LOAD_NO_SCALE, out _)).ToArray();
                    if (missing.Length > 0)
                        throw new System.InvalidOperationException("Missing source glyphs: " + locale.Identifier.Code + "/" + key + " " + new string(missing));
                }
            }
        }
    }
}
