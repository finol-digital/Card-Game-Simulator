/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Linq;
using Cgs.Localization;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Editor.Localization
{
    public static class AuthoredUiBinder
    {
        private static JArray Entries => (JArray)JObject.Parse(File.ReadAllText(
            Path.Combine(CatalogImporter.CatalogDirectory, "bindings/authored.json")))["entries"];

        public static string[] BoundAssets => Entries.Where(row => row["key"] != null)
            .Select(row => (string)row["asset"]).Distinct().ToArray();

        public static void StartBinding()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetSceneManagerSetup()
                .Any(setup => SceneManager.GetSceneByPath(setup.path).isDirty))
                throw new InvalidOperationException("Save scene edits and leave Play mode before binding UI.");
            var paths = Entries.Where(row => row["key"] != null).Select(row => (string)row["asset"]).Distinct()
                .OrderBy(path => path.EndsWith(".unity") ? 1 : 0)
                .ThenBy(path => AssetDatabase.GetDependencies(path, true).Length).ToArray();
            var index = 0;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/localization-bind-progress.txt", "Starting\n");
            void Next()
            {
                try
                {
                    if (index == paths.Length)
                    {
                        AddLanguageSetting();
                        File.AppendAllText("Logs/localization-bind-progress.txt", "COMPLETE\n");
                        return;
                    }
                    var path = paths[index++];
                    BindAsset(path);
                    File.AppendAllText("Logs/localization-bind-progress.txt", path + "\n");
                    EditorApplication.delayCall += Next;
                }
                catch (Exception error)
                {
                    File.AppendAllText("Logs/localization-bind-progress.txt", error + "\n");
                    Debug.LogException(error);
                }
            }
            EditorApplication.delayCall += Next;
        }

        // Operate one asset at a time so a maintainer can inspect each serialized result.
        public static void BindAsset(string path)
        {
            var rows = Entries.Where(row => (string)row["asset"] == path && row["key"] != null).ToArray();
            if (path.EndsWith(".prefab", StringComparison.Ordinal))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Bind(new[] { root }, rows);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            else
            {
                var scene = SceneManager.GetSceneByPath(path);
                var wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.isDirty)
                    throw new InvalidOperationException("Save scene edits first: " + path);
                try
                {
                    Bind(scene.GetRootGameObjects(), rows);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
            }
        }

        private static Transform Find(GameObject[] roots, string path)
        {
            var split = path.IndexOf('/');
            var rootName = split < 0 ? path : path.Substring(0, split);
            var matches = roots.Where(item => item.name == rootName).Take(2).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Missing or ambiguous authored UI root: " + path);
            var root = matches[0].transform;
            return split < 0 ? root : root.Find(path.Substring(split + 1));
        }

        private static Cgs.UI.ToolTip GetTooltip(Transform target, string path)
        {
            var tooltip = target.GetComponent<Cgs.UI.ToolTip>();
            if (tooltip == null)
                throw new InvalidOperationException("Missing tooltip component: " + path);
            return tooltip;
        }

        public static void ValidateAsset(string path)
        {
            var rows = Entries.Where(row => (string)row["asset"] == path && row["key"] != null).ToArray();
            void Validate(GameObject[] roots)
            {
                foreach (var row in rows)
                {
                    var target = Find(roots, (string)row["path"]);
                    var key = (string)row["key"];
                    var property = (string)row["property"];
                    if (target == null)
                        throw new InvalidOperationException("Missing target: " + path + " / " + row["path"]);
                    if (property == "tooltip")
                    {
                        var serialized = new SerializedObject(GetTooltip(target, (string)row["path"]));
                        if (serialized.FindProperty("localizationKey").stringValue != key)
                            throw new InvalidOperationException("Missing tooltip binding: " + row["path"]);
                    }
                    else if (property.StartsWith("options.", StringComparison.Ordinal))
                    {
                        var adapter = target.GetComponent<LocalizedDropdown>();
                        if (adapter == null)
                            throw new InvalidOperationException("Missing dropdown binding: " + row["path"]);
                        var options = new SerializedObject(adapter).FindProperty("options");
                        var index = int.Parse(property.Substring("options.".Length));
                        var found = false;
                        for (var option = 0; option < options.arraySize; option++)
                        {
                            var item = options.GetArrayElementAtIndex(option);
                            found |= item.FindPropertyRelative("index").intValue == index
                                && item.FindPropertyRelative("key").stringValue == key;
                        }
                        if (!found)
                            throw new InvalidOperationException("Wrong dropdown option binding: " + row["path"]);
                    }
                    else if (target.GetComponentInParent<Dropdown>(true) == null)
                    {
                        var binding = target.GetComponent<CgsLocalizeStringEvent>();
                        Component text = target.GetComponent<Text>();
                        text ??= target.GetComponent<TMP_Text>();
                        if (binding == null || binding.StringReference.TableEntryReference.Key != key
                            || binding.OnUpdateString.GetPersistentEventCount() != 1
                            || binding.OnUpdateString.GetPersistentTarget(0) != text
                            || binding.OnUpdateString.GetPersistentMethodName(0) != "set_text"
                            || binding.OnUpdateString.GetPersistentListenerState(0) != UnityEventCallState.EditorAndRuntime)
                            throw new InvalidOperationException("Invalid persistent text binding: " + path + " / " + row["path"]);
                    }
                }
            }
            if (path.EndsWith(".prefab", StringComparison.Ordinal))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Validate(new[] { root }); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                return;
            }
            var scene = SceneManager.GetSceneByPath(path);
            var wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try { Validate(scene.GetRootGameObjects()); }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Bind(GameObject[] roots, JToken[] rows)
        {
            foreach (var text in roots.SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .Where(component => component is Text || component is TMP_Text))
            {
                BindFont(text);
                if (text.GetComponentInParent<Dropdown>(true) != null)
                {
                    var old = text.GetComponent<LocalizeStringEvent>();
                    if (old != null && old.StringReference.TableReference.TableCollectionName == CgsLocalization.TableName)
                        UnityEngine.Object.DestroyImmediate(old);
                }
            }
            foreach (var row in rows)
            {
                var target = Find(roots, (string)row["path"]);
                if (target == null)
                    throw new InvalidOperationException("Missing authored UI: " + row["path"]);
                var key = (string)row["key"];
                var english = (string)row["text"];
                var property = (string)row["property"];
                if (property == "tooltip")
                {
                    var tooltip = GetTooltip(target, (string)row["path"]);
                    var serialized = new SerializedObject(tooltip);
                    if (serialized.FindProperty("tooltip").stringValue != english)
                        throw new InvalidOperationException("Tooltip source changed: " + row["path"]);
                    serialized.FindProperty("localizationKey").stringValue = key;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (property.StartsWith("options.", StringComparison.Ordinal))
                {
                    var dropdown = target.GetComponent<Dropdown>();
                    if (!int.TryParse(property.Substring("options.".Length), out var index)
                        || dropdown == null || index < 0 || index >= dropdown.options.Count)
                        throw new InvalidOperationException("Invalid dropdown option: " + row["path"]);
                    if (dropdown.options[index].text != english)
                        throw new InvalidOperationException("Dropdown source changed: " + row["path"]);
                    var adapter = target.GetComponent<LocalizedDropdown>() ?? target.gameObject.AddComponent<LocalizedDropdown>();
                    var serialized = new SerializedObject(adapter);
                    var options = serialized.FindProperty("options");
                    var position = -1;
                    for (var option = 0; option < options.arraySize; option++)
                        if (options.GetArrayElementAtIndex(option).FindPropertyRelative("index").intValue == index)
                            position = option;
                    if (position < 0)
                        position = options.arraySize++;
                    var item = options.GetArrayElementAtIndex(position);
                    item.FindPropertyRelative("index").intValue = index;
                    item.FindPropertyRelative("key").stringValue = key;
                    item.FindPropertyRelative("english").stringValue = english;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    // Dropdown copies option values to its caption and cloned item labels.
                    // Localizing those Text components would overwrite the selected option.
                    if (target.GetComponentInParent<Dropdown>(true) != null)
                        continue;
                    var legacy = target.GetComponent<Text>();
                    var tmp = target.GetComponent<TMP_Text>();
                    if (legacy == null && tmp == null)
                        throw new InvalidOperationException("Missing text component: " + row["path"]);
                    var current = legacy != null ? legacy.text : tmp.text;
                    var existing = target.GetComponent<LocalizeStringEvent>();
                    if (existing == null && current != english)
                        throw new InvalidOperationException("Text source changed: " + row["path"]);
                    BindText(legacy != null ? (Component)legacy : tmp, key, english);
                }
            }
        }

        private static void BindFont(Component text)
        {
            var mixed = text.transform.GetComponentsInParent<Transform>(true).Any(parent => parent.name == "Language Dropdown");
            if (text.GetComponent<LocalizedUiFont>() != null && !mixed)
                return;
            UnityEngine.Object font = text is Text legacy ? legacy.font : ((TMP_Text)text).font;
            var name = font != null ? font.name : null;
            var key = name != null && name.StartsWith("Exo") ? "heading." : "body.";
            key += name != null && name.Contains("Bold") ? "bold" : "regular";
            if (mixed)
                key = "mixed";
            var binding = text.GetComponent<LocalizedUiFont>() ?? text.gameObject.AddComponent<LocalizedUiFont>();
            binding.enabled = false;
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            binding.enabled = true;
            EditorUtility.SetDirty(binding);
            PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
        }

        public static void BindText(Component text, string key, string english)
        {
            var existing = text.GetComponent<LocalizeStringEvent>();
            if (existing != null && existing is not CgsLocalizeStringEvent)
                UnityEngine.Object.DestroyImmediate(existing);
            var binding = text.GetComponent<CgsLocalizeStringEvent>() ?? text.gameObject.AddComponent<CgsLocalizeStringEvent>();
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("english").stringValue = english;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            binding.StringReference = new LocalizedString(CgsLocalization.TableName, key);
            while (binding.OnUpdateString.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(binding.OnUpdateString, 0);
            // A public property setter serializes to set_text; lambdas do not persist in UnityEvents.
            var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text,
                text.GetType().GetProperty("text").SetMethod);
            UnityEventTools.AddPersistentListener(binding.OnUpdateString, setter);
            binding.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(binding);
            PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
        }

        private static void BindLanguageFonts(GameObject row)
        {
            foreach (var text in row.GetComponentsInChildren<Component>(true)
                .Where(component => component is Text || component is TMP_Text))
                BindFont(text);
        }

        public static void AddLanguageSetting()
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/Settings.unity");
            var wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen)
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity", OpenSceneMode.Additive);
            if (scene.isDirty)
                throw new InvalidOperationException("Save Settings scene edits first.");
            try
            {
                var settings = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Cgs.Menu.Settings>(true)).Single();
                var serialized = new SerializedObject(settings);
                if (serialized.FindProperty("languageDropdown").objectReferenceValue is Dropdown existingDropdown)
                {
                    var existingLabel = existingDropdown.transform.parent.GetComponentsInChildren<Text>(true)
                        .Single(text => !text.transform.IsChildOf(existingDropdown.transform));
                    BindText(existingLabel, "settings.language", "Language");
                    BindLanguageFonts(existingDropdown.transform.parent.gameObject);
                    EditorSceneManager.SaveScene(scene);
                    return;
                }
                var frameRate = (Dropdown)serialized.FindProperty("framerateDropdown").objectReferenceValue;
                var row = frameRate.transform.parent;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Components/Settings Dropdown.prefab");
                var languageRow = (GameObject)PrefabUtility.InstantiatePrefab(prefab, row.parent);
                languageRow.name = "Language Dropdown";
                languageRow.transform.SetSiblingIndex(row.GetSiblingIndex());
                var dropdown = languageRow.GetComponentInChildren<Dropdown>(true);
                while (dropdown.onValueChanged.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(dropdown.onValueChanged, 0);
                dropdown.options = new System.Collections.Generic.List<Dropdown.OptionData> { new("English") };
                dropdown.SetValueWithoutNotify(0);
                dropdown.RefreshShownValue();
                var label = languageRow.GetComponentsInChildren<Text>(true).Single(text => !text.transform.IsChildOf(dropdown.transform));
                label.text = "Language";
                BindText(label, "settings.language", "Language");
                BindLanguageFonts(languageRow);
                serialized.FindProperty("languageDropdown").objectReferenceValue = dropdown;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(dropdown);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
