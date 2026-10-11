/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Cgs.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Debug = UnityEngine.Debug;

namespace Cgs.Editor.Localization
{
    public static class CatalogImporter
    {
        public const string ImportCommand = "CGS/Localization/Import Catalogs";
        private const string CatalogError = "CGS localization catalogs are invalid. ";
        private const string DriftError = "CGS localization assets are stale. Run " + ImportCommand + ". ";
        private const string AssetDirectory = "Assets/Localization/CgsUi";
        private static string ProjectDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string CatalogDirectory => Path.Combine(ProjectDirectory, "translations");

        private sealed class CatalogSet
        {
            public List<SupportedLanguage> Languages;
            public Dictionary<string, JObject> Entries;
            public JObject English => Entries["en"];
        }

        [MenuItem(ImportCommand)]
        public static void Import()
        {
            ImportDirectory(CatalogDirectory);
        }

        public static void ImportDirectory(string directory)
        {
            // All structural and formatter validation happens before any asset mutation.
            var catalogs = ReadValidated(directory);
            var collection = LocalizationEditorSettings.GetStringTableCollection(CgsLocalization.TableName);
            CheckRetiredKeys(collection, catalogs);
            var locales = ConfigureLocales(catalogs.Languages);
            if (collection == null)
            {
                Directory.CreateDirectory(AssetDirectory);
                AssetDatabase.Refresh();
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    CgsLocalization.TableName, AssetDirectory, locales);
            }
            foreach (var language in catalogs.Languages)
            {
                var table = collection.GetTable(language.Code) as StringTable;
                if (table == null)
                    table = (StringTable)collection.AddNewTable(language.Code);
                foreach (var source in catalogs.English.Properties().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    var entryData = catalogs.Entries[language.Code][source.Name];
                    var text = (string)entryData["text"];
                    var entry = table.GetEntry(source.Name);
                    if (entry == null)
                        entry = table.AddEntry(source.Name, text);
                    else if (entry.Value != text)
                        entry.Value = text;
                    entry.IsSmart = (bool)source.Value["smart"];
                    SetComment(entry, entryData.ToString(Formatting.None));
                    SetComment(collection.SharedData.GetEntry(source.Name).Metadata,
                        source.Value.ToString(Formatting.None));
                }
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            ConfigureSettings(catalogs.Languages, locales.Single(locale => locale.Identifier.Code == "en"));
            ConfigureLocalGroups(collection, locales);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(CatalogImporter), collection);
            AssetDatabase.SaveAssets();
            Debug.Log($"Imported {catalogs.English.Count} CGS messages for {catalogs.Languages.Count} languages.");
        }

        [MenuItem("CGS/Localization/Validate Generated Tables")]
        public static void ValidateGenerated()
        {
            var catalogs = ReadValidated(CatalogDirectory);
            if (AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild
                != AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
                throw new BuildFailedException(DriftError + "Addressables must rebuild with the player to include current localization content.");
            var collection = LocalizationEditorSettings.GetStringTableCollection(CgsLocalization.TableName);
            if (collection == null)
                throw new BuildFailedException(DriftError + "Missing CgsUi collection.");
            CheckRetiredKeys(collection, catalogs);
            foreach (var language in catalogs.Languages)
            {
                var table = collection.GetTable(language.Code) as StringTable;
                if (table == null)
                    throw new BuildFailedException(DriftError + language.Code);
                ValidateLocalAsset(table);
                if (!LocalizationEditorSettings.GetPreloadTableFlag(table))
                    throw new BuildFailedException(DriftError + "String table is not preloaded: " + language.Code);
                foreach (var source in catalogs.English.Properties())
                {
                    var entry = table.GetEntry(source.Name);
                    var expected = catalogs.Entries[language.Code][source.Name];
                    if (entry == null || entry.Value != (string)expected["text"]
                        || entry.IsSmart != (bool)source.Value["smart"]
                        || entry.GetMetadata<Comment>()?.CommentText != expected.ToString(Formatting.None)
                        || collection.SharedData.GetEntry(source.Name)?.Metadata.GetMetadata<Comment>()?.CommentText
                        != source.Value.ToString(Formatting.None))
                        throw new BuildFailedException(DriftError + language.Code + " / " + source.Name);
                }
            }
            var actual = LocalizationEditorSettings.GetLocales().Select(locale => locale.Identifier.Code).OrderBy(code => code);
            var supported = catalogs.Languages.Select(language => language.Code).OrderBy(code => code);
            if (!actual.SequenceEqual(supported) || !LocalizationSettings.StringDatabase.UseFallback
                || !LocalizationSettings.AssetDatabase.UseFallback
                || LocalizationSettings.InitializeSynchronously || LocalizationSettings.ProjectLocale.Identifier.Code != "en")
                throw new BuildFailedException(DriftError + "Locale availability, fallback or initialization configuration.");
            var selectors = LocalizationSettings.StartupLocaleSelectors;
            var selector = selectors.Count == 1 ? selectors[0] as CgsLocaleSelector : null;
            if (selector == null || !selector.Languages.Select(LanguageSignature).SequenceEqual(catalogs.Languages.Select(LanguageSignature)))
                throw new BuildFailedException(DriftError + "Startup selector or language manifest.");
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                ValidateLocalAsset(locale);
                if (locale.Identifier.Code != "en" && locale.Metadata.GetMetadata<FallbackLocale>()?.Locale != LocalizationSettings.ProjectLocale)
                    throw new BuildFailedException(DriftError + "Missing English fallback: " + locale.Identifier.Code);
            }
        }

        internal static void ValidateLocalAsset(UnityEngine.Object asset)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var path = AssetDatabase.GetAssetPath(asset);
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            var schema = entry?.parentGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null || !schema.IncludeInBuild
                || schema.BuildPath.GetName(settings) != "Local.BuildPath"
                || schema.LoadPath.GetName(settings) != "Local.LoadPath")
                throw new BuildFailedException(DriftError + "Asset is not bundled locally: " + path);
        }

        private static string LanguageSignature(SupportedLanguage language)
            => language.Code + "|" + language.NativeName + "|" + string.Join(",", language.Aliases);

        private static CatalogSet ReadValidated(string directory)
        {
            directory = Path.GetFullPath(directory);
            ValidateWithPython(directory);
            var manifest = ReadObject(Path.Combine(directory, "locales.json"));
            var languages = ((JArray)manifest["locales"]).Select(locale => new SupportedLanguage(
                (string)locale["code"], (string)locale["nativeName"], locale["aliases"].ToObject<string[]>())).ToList();
            var result = new CatalogSet
            {
                Languages = languages,
                Entries = languages.ToDictionary(language => language.Code,
                    language => (JObject)ReadObject(Path.Combine(directory, language.Code + ".json"))["entries"])
            };
            // Validate against the installed formatter, including representative zero/one/many counts.
            var formatter = UnityEngine.Localization.SmartFormat.Smart.CreateDefaultSmartFormat();
            foreach (var language in languages)
            foreach (var source in result.English.Properties())
            {
                if (!(bool)source.Value["smart"])
                    continue;
                foreach (var count in new[] { 0, 1, 2, 5, 21 })
                {
                    var arguments = ((JObject)source.Value["arguments"]).Properties().ToDictionary(argument => argument.Name,
                        argument => SampleArgument(argument.Name, (string)argument.Value, count));
                    try
                    {
                        formatter.Format(CultureInfo.GetCultureInfo(language.Code),
                            (string)result.Entries[language.Code][source.Name]["text"], new object[] { arguments });
                    }
                    catch (Exception error)
                    {
                        throw new BuildFailedException(CatalogError + language.Code + " / " + source.Name + ": " + error.Message);
                    }
                }
            }
            return result;
        }

        private static object SampleArgument(string name, string description, int count)
        {
            return description.IndexOf("integer", StringComparison.OrdinalIgnoreCase) >= 0
                   || name == "progress" ? count : "Example";
        }

        private static JObject ReadObject(string path)
        {
            using var stream = File.OpenText(path);
            using var reader = new JsonTextReader(stream);
            return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        private static void ValidateWithPython(string directory)
        {
            var executable = Environment.GetEnvironmentVariable("CGS_PYTHON");
            if (string.IsNullOrWhiteSpace(executable))
                executable = Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3";
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = ProjectDirectory,
                Arguments = "\"" + Path.Combine(ProjectDirectory, "tools/localization/validate.py") + "\" --directory \"" + directory + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONDONTWRITEBYTECODE"] = "1";
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Python could not start.");
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    throw new InvalidOperationException("Catalog validation timed out.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
                var report = output.GetAwaiter().GetResult();
                if (report.Contains("WARNING:"))
                    Debug.LogWarning(report);
            }
            catch (Exception error)
            {
                throw new BuildFailedException(CatalogError + error.Message
                    + " Install Python 3.10+ or set CGS_PYTHON to its executable path.");
            }
        }

        private static void CheckRetiredKeys(StringTableCollection collection, CatalogSet catalogs)
        {
            if (collection == null)
                return;
            var retired = collection.SharedData.Entries.Where(entry => catalogs.English[entry.Key] == null)
                .Select(entry => entry.Key).ToArray();
            if (retired.Length > 0)
                throw new BuildFailedException(CatalogError + "Retired keys require explicit migration: " + string.Join(", ", retired));
        }

        private static List<Locale> ConfigureLocales(List<SupportedLanguage> languages)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var all = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Localization" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
            var supported = new HashSet<string>(languages.Select(language => language.Code));
            foreach (var locale in all)
            {
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(locale)));
                if (!supported.Contains(locale.Identifier.Code) && entry != null)
                    LocalizationEditorSettings.RemoveLocale(locale);
            }
            var result = new List<Locale>();
            foreach (var language in languages)
            {
                var locale = all.FirstOrDefault(item => item.Identifier.Code == language.Code);
                if (locale == null)
                {
                    locale = Locale.CreateLocale(language.Code);
                    AssetDatabase.CreateAsset(locale, "Assets/Localization/Locales/" + language.Code + ".asset");
                }
                LocalizationEditorSettings.AddLocale(locale);
                result.Add(locale);
            }
            var english = result.Single(locale => locale.Identifier.Code == "en");
            foreach (var locale in result.Where(locale => locale != english))
            {
                var fallback = locale.Metadata.GetMetadata<FallbackLocale>();
                if (fallback == null)
                    locale.Metadata.AddMetadata(new FallbackLocale(english));
                else
                    fallback.Locale = english;
                EditorUtility.SetDirty(locale);
            }
            return result;
        }

        private static void ConfigureSettings(List<SupportedLanguage> languages, Locale english)
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            var selectors = settings.GetStartupLocaleSelectors();
            var oldSelector = selectors.Count == 1 ? selectors[0] as CgsLocaleSelector : null;
            if (oldSelector == null || !oldSelector.Languages.Select(LanguageSignature).SequenceEqual(languages.Select(LanguageSignature)))
            {
                settings.GetStartupLocaleSelectors().Clear();
                settings.GetStartupLocaleSelectors().Add(new CgsLocaleSelector(languages));
            }
            LocalizationSettings.ProjectLocale = english;
            LocalizationSettings.InitializeSynchronously = false;
            LocalizationSettings.StringDatabase.UseFallback = true;
            LocalizationSettings.AssetDatabase.UseFallback = true;
            LocalizationSettings.StringDatabase.NoTranslationFoundMessage = string.Empty;
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureLocalGroups(StringTableCollection collection, List<Locale> locales)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            // A machine-wide preference can otherwise silently package stale
            // content from the last build for a different target or catalog.
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            var paths = collection.StringTables.Select(AssetDatabase.GetAssetPath)
                .Concat(locales.Select(AssetDatabase.GetAssetPath)).Append(AssetDatabase.GetAssetPath(collection.SharedData));
            foreach (var path in paths)
            {
                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                var schema = entry?.parentGroup.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    throw new BuildFailedException("Localization asset is not in a bundled Addressables group: " + path);
                schema.BuildPath.SetVariableByName(settings, "Local.BuildPath");
                schema.LoadPath.SetVariableByName(settings, "Local.LoadPath");
                schema.IncludeInBuild = true;
                EditorUtility.SetDirty(schema);
            }
            EditorUtility.SetDirty(settings);
        }

        private static void SetComment(IMetadataCollection metadata, string text)
        {
            var comment = metadata.GetMetadata<Comment>();
            if (comment == null)
                metadata.AddMetadata(new Comment { CommentText = text });
            else if (comment.CommentText != text)
                comment.CommentText = text;
        }
    }

    public sealed class LocalizationBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            CatalogImporter.ValidateGenerated();
            FontCatalogBuilder.ValidateGlyphs();
        }
    }
}
