/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Generates the reference scene and builds the minimal reference player with CGS's startup-relevant settings.
///
///   Unity -batchmode -quit -projectPath tools/startup-reference -executeMethod ReferenceBuild.Build \
///     -referenceTarget StandaloneLinux64 -referenceOutput Builds/StandaloneLinux64/Reference.x86_64
/// </summary>
public static class ReferenceBuild
{
    private const string ScenePath = "Assets/Scenes/Reference.unity";
    private const string PipelinePath = "Assets/Settings/ReferenceURP.asset";
    private const string RendererPath = "Assets/Settings/ReferenceURP_Renderer.asset";
    // iOS needs three period-separated integers; result JSON marks reference runs with "reference":true
    private const string Version = "6000.6.8";

    [MenuItem("Reference/Generate Scene")]
    public static void GenerateScene()
    {
        ConfigureRenderPipeline();
        ConfigurePlayerSettings();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var buttonObject = new GameObject("Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        ((RectTransform)buttonObject.transform).sizeDelta = new Vector2(320, 120);
        var label = new GameObject("Text", typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(buttonObject.transform, false);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Start";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        var markerObject = new GameObject("MenuMarker", typeof(Image));
        markerObject.transform.SetParent(canvasObject.transform, false);
        markerObject.GetComponent<Image>().color = new Color(0f, 0.85f, 0f);
        var markerRect = (RectTransform)markerObject.transform;
        markerRect.anchorMin = markerRect.anchorMax = markerRect.pivot = new Vector2(0, 1);
        markerRect.sizeDelta = new Vector2(96, 96);
        markerRect.anchoredPosition = Vector2.zero;

        var startup = new GameObject("ReferenceStartup").AddComponent<ReferenceStartup>();
        var serialized = new SerializedObject(startup);
        serialized.FindProperty("button").objectReferenceValue = buttonObject.GetComponent<Button>();
        serialized.FindProperty("menuMarker").objectReferenceValue = markerObject.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        var arguments = Environment.GetCommandLineArgs();
        var targetName = Argument(arguments, "-referenceTarget") ?? "StandaloneLinux64";
        var output = Argument(arguments, "-referenceOutput") ?? Path.Combine("Builds", targetName, "Reference");
        var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetName);

        GenerateScene();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = target,
            options = BuildOptions.None
        });
        Debug.Log($"ReferenceBuild: {report.summary.result} -> {output}");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static void ConfigureRenderPipeline()
    {
        // CGS renders with URP, so the reference pays the same pipeline startup cost
        Directory.CreateDirectory("Assets/Settings");
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
            pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        for (var level = 0; level < QualitySettings.names.Length; level++)
        {
            QualitySettings.SetQualityLevel(level, false);
            QualitySettings.renderPipeline = pipeline;
        }
    }

    private static void ConfigurePlayerSettings()
    {
        // Mirror CGS's startup-relevant player settings (see ProjectSettings/ProjectSettings.asset in CGS)
        PlayerSettings.companyName = "Finol Digital LLC";
        PlayerSettings.productName = "CGS Startup Reference";
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.backgroundColor = new Color(0.2509804f, 0.2509804f, 0.2509804f);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.gcIncremental = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.finoldigital.cgsstartupreference");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.finoldigital.CgsStartupReference");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.finoldigital.CgsStartupReference");
        foreach (var target in new[]
                 {
                     NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS,
                     NamedBuildTarget.WebGL
                 })
        {
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
        }

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Unity 6.6 minimum
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
    }

    private static string Argument(string[] arguments, string name)
    {
        var index = Array.IndexOf(arguments, name);
        return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
    }
}
