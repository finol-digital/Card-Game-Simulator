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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlayInstant.Editor
{
    public class PlayInstantSettingsWindow : EditorWindow
    {
        // Allowed characters for splitting PlayerSettings.GetScriptingDefineSymbolsForGroup()
        private static readonly char[] ScriptingDefineSymbolsSplitChars = {';', ',', ' '};
        private static readonly string[] PlatformOptions = {"Installed", "Instant"};
        private const string PlayInstantScriptingDefineSymbol = "PLAY_INSTANT";
        private const int FieldMinWidth = 100;

        private bool _isInstant;
        private string _defaultUrl;

        private void Awake()
        {
            _isInstant = IsPlayInstantScriptingSymbolDefined();
            _defaultUrl = AndroidManifestHelper.GetExistingUrl();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform", GUILayout.MinWidth(FieldMinWidth));
            var index = EditorGUILayout.Popup(_isInstant ? 1 : 0, PlatformOptions, GUILayout.MinWidth(FieldMinWidth));
            _isInstant = index == 1;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_isInstant)
            {
                // TODO: make URL optional once URL-less AIAs are launched.
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Instant Default URL", GUILayout.MinWidth(FieldMinWidth));
                _defaultUrl = EditorGUILayout.TextField(_defaultUrl, GUILayout.MinWidth(FieldMinWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            if (_isInstant)
            {
                if (!AndroidManifestHelper.HasExistingAndroidManifest())
                {
                    EditorGUILayout.LabelField("Clicking 'Save' will create a new AndroidManifest.xml file.",
                        EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    string.Format("Note: the symbol \"{0}\" will be defined for scripting with #if {0} / #endif.",
                        PlayInstantScriptingDefineSymbol),
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Save"))
            {
                if (_isInstant)
                {
                    SelectPlatformInstant();
                }
                else
                {
                    SelectPlatformInstalled();
                }
            }
        }

        private void SelectPlatformInstant()
        {
            _defaultUrl = _defaultUrl == null ? string.Empty : _defaultUrl.Trim();
            if (_defaultUrl.Length == 0)
            {
                DisplayUrlError("The URL cannnot be empty");
                return;
            }

            Uri uri;
            try
            {
                // TODO: allow port numbers? allow query parameters?
                uri = new Uri(_defaultUrl);
            }
            catch (Exception ex)
            {
                DisplayUrlError(string.Format("The URL is invalid: {0}", ex.Message));
                return;
            }

            if (uri.Scheme.ToLower() != "https")
            {
                DisplayUrlError("The URL scheme should be \"https\"");
                return;
            }

            if (string.IsNullOrEmpty(uri.Host))
            {
                DisplayUrlError("The URL host must be specified");
                return;
            }

            if (AndroidManifestHelper.SwitchToInstant(uri))
            {
                // Define PlayInstantScriptingDefineSymbol
                var scriptingDefineSymbols = GetScriptingDefineSymbols();
                if (!IsPlayInstantScriptingSymbolDefined(scriptingDefineSymbols))
                {
                    SetScriptingDefineSymbols(scriptingDefineSymbols.Concat(new[] {PlayInstantScriptingDefineSymbol}));
                }

                Close();
            }
        }

        private void SelectPlatformInstalled()
        {
            // Undefine PlayInstantScriptingDefineSymbol
            var scriptingDefineSymbols = GetScriptingDefineSymbols();
            if (IsPlayInstantScriptingSymbolDefined(scriptingDefineSymbols))
            {
                SetScriptingDefineSymbols(scriptingDefineSymbols.Where(sym => sym != PlayInstantScriptingDefineSymbol));
            }

            AndroidManifestHelper.SwitchToInstalled();

            Close();
        }

        private static void DisplayUrlError(string message)
        {
            EditorUtility.DisplayDialog("Invalid Default URL", message, "OK");
            Debug.LogError(message);
        }

        private static bool IsPlayInstantScriptingSymbolDefined(string[] scriptingDefineSymbols)
        {
            return Array.IndexOf(scriptingDefineSymbols, PlayInstantScriptingDefineSymbol) >= 0;
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
            var defines = string.Join(";", scriptingDefineSymbols.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
        }

        // TODO: extract the scripting symbol code from this class
        public static bool IsPlayInstantScriptingSymbolDefined()
        {
            return IsPlayInstantScriptingSymbolDefined(GetScriptingDefineSymbols());
        }
    }
}