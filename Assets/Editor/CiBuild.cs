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
    /// CI entry point used by GitHub Actions.
    /// Creates the Main scene (a single GameBootstrap object — the whole game is
    /// built at runtime), applies Android player settings and builds an APK to
    /// build/Android/FarmSim.apk.
    ///
    /// Run locally: menu FarmSim → Build Android APK (CI)
    /// Run in CI   : Unity -batchmode -quit -executeMethod FarmSim.Editor.CiBuild.PerformBuild
    /// </summary>
    public static class CiBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ApkPath = "build/Android/FarmSim.apk";
        private const string VERSION = "1.2.0";

        [MenuItem("FarmSim/Build Android APK (CI)")]
        public static void PerformBuild()
        {
            try
            {
                CreateMainScene();
                ApplyPlayerSettings();
                ApplyIcon();

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

                Debug.Log("APK BUILD SUCCEEDED: " + ApkPath +
                          " (" + (report.summary.totalSize / (1024UL * 1024UL)) + " MB)");
            }
            catch (Exception e)
            {
                Debug.LogError("APK BUILD FAILED: " + e);
                // In batch mode make the CI job fail instead of exiting 0.
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
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

        /// <summary>
        /// Replaces the default Unity launcher icon with the game's own icon.
        /// </summary>
        private static void ApplyIcon()
        {
            const string iconPath = "Assets/Art/AppIcon.png";
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("App icon not found at " + iconPath + " — keeping the Unity default.");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null) return;

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            Debug.Log("App icon applied from " + iconPath);
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "FarmSimStudio";
            PlayerSettings.productName = "3D Farm Simulator";
            PlayerSettings.bundleVersion = VERSION;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.farmsim.threedgame");
            // Linear lighting: the single biggest "cinematic instead of washed
            // out" switch available on mobile (gamma space flattened every
            // shadow into the mid tones).
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            // Shrink the APK a little; the game uses no reflection-based APIs.
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            // 64-bit ARM requires IL2CPP (Mono only supports 32-bit ARM on Android).
            // IL2CPP is also what Google Play demands for new uploads.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);

            // Ship an APK (not an .aab) so it can be installed directly on a phone.
            EditorUserBuildSettings.buildAppBundle = false;

            // No custom keystore in CI: Unity signs with the debug keystore, which
            // is fine for testing/sideloading. For Play Store, add a real keystore
            // via GitHub Secrets (see README).
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }
}
#endif
