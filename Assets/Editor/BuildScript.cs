using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildScript
{
    private const string DefaultScenePath = "Assets/Scenes/Main.unity";
    private const string DefaultOutputPath = "build/Android/BladeMemory.apk";
    private const string AndroidPackageName = "com.blademory.game";

    public static void PrepareForCloudBuild()
    {
        Debug.Log("[BuildScript] Starting CloudBuild preparation...");

        EnsureSceneExists();

        Debug.Log($"[BuildScript] Configuring Android PlayerSettings (Package: {AndroidPackageName}, IL2CPP, ARM64, MinAPI: 26)...");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageName);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        EditorUserBuildSettings.buildAppBundle = false;

        AssetDatabase.SaveAssets();
        Debug.Log("[BuildScript] PrepareForCloudBuild completed successfully.");
    }

    public static void BuildAndroid()
    {
        Debug.Log("[BuildScript] Starting Android APK build process...");

        PrepareForCloudBuild();

        string outputPath = GetBuildPath();
        string outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { DefaultScenePath },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Executing BuildPipeline.BuildPlayer -> Output: {outputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Android APK Build SUCCEEDED! Total size: {summary.totalSize} bytes.");
        }
        else
        {
            Debug.LogError($"[BuildScript] Android APK Build FAILED with result: {summary.result} (Total Errors: {summary.totalErrors})");
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureSceneExists()
    {
        if (File.Exists(DefaultScenePath))
        {
            Debug.Log($"[BuildScript] Scene already exists at: {DefaultScenePath}");
        }
        else
        {
            Debug.Log($"[BuildScript] No scene found at {DefaultScenePath}. Creating minimal scene with Camera, Directional Light, and Cube...");

            string sceneDirectory = Path.GetDirectoryName(DefaultScenePath);
            if (!string.IsNullOrEmpty(sceneDirectory) && !Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera
            GameObject cameraGo = new GameObject("Main Camera");
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 1.5f, -10f);
            cameraGo.transform.LookAt(new Vector3(0, 0.5f, 0));
            cameraGo.AddComponent<AudioListener>();

            // 2. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.position = new Vector3(0, 3f, 0);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0);

            // 3. Cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(newScene, DefaultScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BuildScript] Minimal scene created and saved to {DefaultScenePath}");
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(DefaultScenePath, true)
        };
        Debug.Log($"[BuildScript] EditorBuildSettings configured with scene: {DefaultScenePath}");
    }

    private static string GetBuildPath()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-customBuildPath" && i + 1 < args.Length)
            {
                string path = args[i + 1];
                if (!path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(path) || !Path.HasExtension(path))
                    {
                        path = Path.Combine(path, "BladeMemory.apk");
                    }
                }
                return path;
            }
        }
        return DefaultOutputPath;
    }
}
