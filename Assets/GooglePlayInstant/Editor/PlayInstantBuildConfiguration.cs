// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods for accessing and persisting build settings.
    /// </summary>
    public static class PlayInstantBuildConfiguration
    {
        /// <summary>
        /// Configuration class that is serialized as JSON. See associated properties for field documentation.
        /// </summary>
        [Serializable]
        private class Configuration
        {
            // Note: field names do not match style convention, but were chosen for proper looking JSON serialization.
            public string assetBundleManifestPath;
            public string instantUrl;
            public string[] scenesInBuild;
        }

        private const string PlayInstantScriptingDefineSymbol = "PLAY_INSTANT";

        // Allowed characters for splitting PlayerSettings.GetScriptingDefineSymbolsForGroup().
        private static readonly char[] ScriptingDefineSymbolsSplitChars = {';', ',', ' '};
        private static readonly string ConfigurationFilePath = Path.Combine("Library", "PlayInstantBuildConfig.json");

        // Holds an in-memory copy of configuration for quick access.
        private static Configuration _config;

        /// <summary>
        /// Optional field used to prevent removal of required components when building with engine stripping.
        /// <see cref="https://docs.unity3d.com/ScriptReference/BuildPlayerOptions-assetBundleManifestPath.html"/>
        /// Never null.
        /// </summary>
        public static string AssetBundleManifestPath
        {
            get
            {
                LoadConfigIfNecessary();
                return _config.assetBundleManifestPath ?? string.Empty;
            }
        }

        /// <summary>
        /// Optional URL that can be used to launch this instant app. If empty, the app will be "URL-less" and
        /// use an automatically created URL, e.g. https://instant.apps/package-name. Never null.
        /// </summary>
        public static string InstantUrl
        {
            get
            {
                LoadConfigIfNecessary();
                return _config.instantUrl ?? string.Empty;
            }
        }

        /// <summary>
        /// Optional array of scenes to include in the build. If not specified, the enabled scenes from the
        /// Unity "Build Settings" window will be used. Never null.
        /// </summary>
        public static string[] ScenesInBuild
        {
            get
            {
                LoadConfigIfNecessary();
                return _config.scenesInBuild ?? new string[0];
            }
        }

        /// <summary>
        /// Persists the specified configuration to disk.
        /// </summary>
        public static void SaveConfiguration(string instantUrl, string[] scenesInBuild, string assetBundleManifestPath)
        {
            _config = _config ?? new Configuration();
            _config.instantUrl = instantUrl;
            _config.scenesInBuild = scenesInBuild;
            _config.assetBundleManifestPath = assetBundleManifestPath;
            File.WriteAllText(ConfigurationFilePath, JsonUtility.ToJson(_config));
        }

        /// <summary>
        /// Returns true if the selected build type is "Instant" or false if "Installed".
        /// </summary>
        public static bool IsInstantBuildType()
        {
            return IsScriptingSymbolDefined(GetScriptingDefineSymbols(), PlayInstantScriptingDefineSymbol);
        }

        /// <summary>
        /// Changes the selected build type to "Instant" and defines a "PLAY_INSTANT" scripting define symbol.
        /// </summary>
        public static void SetInstantBuildType()
        {
            AddScriptingDefineSymbol(PlayInstantScriptingDefineSymbol);
        }

        /// <summary>
        /// Changes the selected build type to "Installed" and removes the "PLAY_INSTANT" scripting define symbol.
        /// </summary>
        public static void SetInstalledBuildType()
        {
            var scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (IsScriptingSymbolDefined(scriptingDefineSymbols, PlayInstantScriptingDefineSymbol))
            {
                SetScriptingDefineSymbols(scriptingDefineSymbols.Where(sym => sym != PlayInstantScriptingDefineSymbol));
            }
        }

        /// <summary>
        /// Adds the specified scripting define symbol for Android, but only if it isn't already defined.
        /// </summary>
        public static void AddScriptingDefineSymbol(string symbol)
        {
            var scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (!IsScriptingSymbolDefined(scriptingDefineSymbols, symbol))
            {
                SetScriptingDefineSymbols(scriptingDefineSymbols.Concat(new[] {symbol}));
            }
        }

        private static bool IsScriptingSymbolDefined(string[] scriptingDefineSymbols, string symbol)
        {
            return Array.IndexOf(scriptingDefineSymbols, symbol) >= 0;
        }

        private static string[] GetScriptingDefineSymbols()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (string.IsNullOrEmpty(symbols))
            {
                return new string[0];
            }

            return symbols.Split(ScriptingDefineSymbolsSplitChars, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void SetScriptingDefineSymbols(IEnumerable<string> scriptingDefineSymbols)
        {
            var symbols = string.Join(";", scriptingDefineSymbols.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
        }

        private static void LoadConfigIfNecessary()
        {
            if (_config != null)
            {
                return;
            }

            _config = new Configuration();
            if (File.Exists(ConfigurationFilePath))
            {
                try
                {
                    var configurationJson = File.ReadAllText(ConfigurationFilePath);
                    _config = JsonUtility.FromJson<Configuration>(configurationJson);
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("Failed to load {0} due to exception: {1}", ConfigurationFilePath, ex);
                }
            }
            else
            {
                Debug.Log("Migrating Instant URL from preference file to JSON config file...");
                var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) ?? "unknown";
                var key = "GooglePlayInstant.InstantUrl." + packageName;
                var oldInstantUrl = EditorPrefs.GetString(key);
                SaveConfiguration(oldInstantUrl, null, null);
                EditorPrefs.DeleteKey(key);
                Debug.Log("Migrated Instant URL.");
            }
        }
    }
}