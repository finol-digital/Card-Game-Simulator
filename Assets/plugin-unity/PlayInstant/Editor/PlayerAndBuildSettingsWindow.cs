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

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlayInstant.Editor
{
    public class PlayerAndBuildSettingsWindow : EditorWindow
    {
        private delegate bool IsCorrectState();

        private delegate bool ChangeState();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Required changes", EditorStyles.boldLabel);

            AddControl("Build Target should be Android",
                () => EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                () => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android));

            // Note: pre-2018 versions of Unity do not have an enum value for AndroidApiLevel26.
            AddControl("Android targetSdkVersion should be 26 or higher (Automatic may be required)",
                () => (int) PlayerSettings.Android.targetSdkVersion >= 26 || PlayerSettings.Android.targetSdkVersion ==
                      AndroidSdkVersions.AndroidApiLevelAuto,
                () =>
                {
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                    return true;
                });

            var graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            AddControl("Graphics API should be OpenGLES2 only",
                () => graphicsDeviceTypes.Length == 1 && graphicsDeviceTypes[0] == GraphicsDeviceType.OpenGLES2,
                () =>
                {
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] {GraphicsDeviceType.OpenGLES2});
                    return true;
                });

#if UNITY_2017_2_OR_NEWER
            AddControl("Android Multithreaded Rendering should be disabled",
                () => !PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                () =>
                {
                    PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);
                    return true;
                });
#else
            AddControl("Mobile Multithreaded Rendering should be disabled",
                () => !PlayerSettings.mobileMTRendering,
                () =>
                {
                    PlayerSettings.mobileMTRendering = false;
                    return true;
                });
#endif

#if UNITY_2018_1_OR_NEWER
            AddControl("Android Target Architecture should be ARMv7",
                () => PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7,
                () =>
                {
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                    return true;
                });
#else
            AddControl("Android Device Filter should be ARMv7 only",
                () => PlayerSettings.Android.targetDevice == AndroidTargetDevice.ARMv7,
                () =>
                {
                    PlayerSettings.Android.targetDevice = AndroidTargetDevice.ARMv7;
                    return true;
                });
#endif

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Recommended changes", EditorStyles.boldLabel);

            AddControl("Android minSdkVersion should be 21 (minimum supported by Play Instant)",
                // TODO: consider prompting if strictly greater than 21 to say that 21 enables wider reach
                () => (int) PlayerSettings.Android.minSdkVersion >= 21,
                () =>
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
                    return true;
                });

            AddControl("Android build system should be Gradle",
                () => EditorUserBuildSettings.androidBuildSystem == AndroidBuildSystem.Gradle,
                () =>
                {
                    EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                    return true;
                });

            AddControl(".NET API Compatibility Level should be \".NET 2.0 Subset\"",
                () => PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) ==
                      ApiCompatibilityLevel.NET_2_0_Subset,
                () =>
                {
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android,
                        ApiCompatibilityLevel.NET_2_0_Subset);
                    return true;
                });

            switch (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android))
            {
                case ScriptingImplementation.IL2CPP:
                    AddControl("IL2CPP builds should strip engine code",
                        () => PlayerSettings.stripEngineCode,
                        () =>
                        {
                            PlayerSettings.stripEngineCode = true;
                            return true;
                        });

                    break;

                case ScriptingImplementation.Mono2x:
                    AddControl("Mono builds should use the highest code stripping level (micro mscorlib)",
                        () => PlayerSettings.strippingLevel == StrippingLevel.UseMicroMSCorlib,
                        () =>
                        {
                            PlayerSettings.strippingLevel = StrippingLevel.UseMicroMSCorlib;
                            return true;
                        });
                    break;
            }
        }

        private void AddControl(string text, IsCorrectState isCorrectState, ChangeState changeState)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
            if (!isCorrectState())
            {
                if (GUILayout.Button("Update", GUILayout.Width(100)))
                {
                    if (changeState())
                    {
                        Debug.LogFormat("Updated setting: {0}", text);
                    }
                    else
                    {
                        Debug.LogErrorFormat("Failed to update setting: {0}", text);
                        EditorUtility.DisplayDialog("Error updating", "Failed to update setting", "OK");
                    }

                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}