#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmSim.Editor
{
    /// <summary>
    /// CI entry point used by GitHub Actions (game-ci/unity-builder).
    /// Creates the Main scene (a single GameBootstrap object — the whole
    /// game is built at runtime), applies Android player settings and
    /// builds an APK to build/Android/FarmSim.apk.
    /// </summary>
    public static class CiBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ApkPath = "build/Android/FarmSim.apk";

        [MenuItem("FarmSim/Build Android APK (CI)")]
        public static void PerformBuild()
        {
            CreateMainScene();
            ApplyPlayerSettings();

            Directory.CreateDirectory("build/Android");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Android build failed: " + report.summary.result +
                                    " (" + report.summary.totalErrors + " errors)");
            }

            Debug.Log("Android APK built successfully: " + ApkPath +
                      " (" + (report.summary.totalSize / (1024UL * 1024UL)) + " MB)");
        }

        private static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("GameBootstrap");
            root.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "FarmSim Studio";
            PlayerSettings.productName = "3D Farm Simulator";
            PlayerSettings.bundleVersion = "1.0.0";

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.farmsim.threedgame");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // No custom keystore in CI: Unity signs with the debug keystore,
            // which is fine for testing/sideloading. For Play Store, add a
            // real keystore via GitHub Secrets (see README).
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }
}
#endif
