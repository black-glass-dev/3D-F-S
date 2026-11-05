using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace FarmSim
{
    /// <summary>
    /// Builds the entire game at runtime: managers, camera, sky, sun, the
    /// farm field and the surrounding world. The project needs no hand-made
    /// scene or prefabs — the CI build creates a scene containing only this
    /// component and everything else appears when the game starts.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private Camera _camera;
        private Light _sun;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Localization.LoadSaved();
            GraphicsSettings.Apply();

            // --- managers ---
            var manager = gameObject.AddComponent<GameManager>();
            var sfx = gameObject.AddComponent<SfxManager>();
            sfx.Init();
            var fx = gameObject.AddComponent<FxManager>();
            fx.Init();

            SetupSky();
            _sun = CreateSun();
            _camera = CreateCamera();
            SetupInput();

            // --- world ---
            var worldRoot = new EnvironmentBuilder().Build();
            OptimizeStaticWorld(worldRoot);

            var gridGo = new GameObject("FarmGrid");
            var grid = gridGo.AddComponent<FarmGrid>();
            grid.Build();

            manager.Init(grid, _sun);

            // UI is created after the manager so it can subscribe to refresh events.
            gameObject.AddComponent<UIManager>();
            UIManager.Instance.RefreshAll();

            // --- restore previous save (if any) ---
            var save = SaveSystem.TryLoad();
            if (save != null)
            {
                manager.ApplySave(save);
                grid.LoadSaveData(save.Plots);

                // v1.3.0: crops keep growing while the game is closed.
                int ripened = grid.ApplyOfflineGrowth(save.SavedAt);
                UIManager.Instance.RefreshAll();

                if (ripened > 0)
                    UIManager.Instance.ShowToast(
                        Localization.T("offline_grown") + " (" + ripened + ")", false, true);
            }
        }

        // ---------- rendering setup ----------

        private void SetupSky()
        {
            var skyShader = Resources.Load<Shader>("Shaders/SkyGradient");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "FarmSky" };
                if (sky.HasProperty("_TopColor"))
                    sky.SetColor("_TopColor", new Color(0.30f, 0.44f, 0.62f));
                if (sky.HasProperty("_BottomColor"))
                    sky.SetColor("_BottomColor", new Color(0.74f, 0.78f, 0.78f));
                if (sky.HasProperty("_Exponent"))
                    sky.SetFloat("_Exponent", 1.2f);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.61f, 0.66f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.4f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            // The camera orbits ~31 m from the farm, so the old 32 m fog start
            // put haze over the whole scene and flattened every shadow.
            RenderSettings.fogStartDistance = 78f;
            RenderSettings.fogEndDistance = 260f;
            RenderSettings.fogColor = new Color(0.72f, 0.77f, 0.80f);
        }

        private Light CreateSun()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.0f;
            sun.shadows = GraphicsSettings.SoftShadows ? LightShadows.Soft : LightShadows.Hard;
            sun.shadowStrength = 0.70f;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.30f;
            sun.renderMode = LightRenderMode.ForcePixel;
            // Lower sun = longer, softer shadows (cinematic late-morning light)
            sunGo.transform.rotation = Quaternion.Euler(38f, 34f, 0f);
            RenderSettings.sun = sun;

            // Cool fill light from the opposite side: softens the Lambert
            // terminator and lifts shadows without a custom lighting model.
            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.58f, 0.70f, 0.86f);
            fill.intensity = 0.40f;
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForceVertex;
            fillGo.transform.rotation = Quaternion.Euler(22f, 214f, 0f);

            return sun;
        }

        private Camera CreateCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.allowHDR = false;
            cam.allowMSAA = GraphicsSettings.AntiAliasing > 1;
            cam.renderingPath = RenderingPath.Forward;

            var controller = camGo.AddComponent<CameraController>();
            controller.Init(new Vector3(0f, 0f, 0.2f), 7.4f,
                new Vector2(-9.5f, 9.5f), new Vector2(-7.5f, 8.5f));

            // Colour grade: the last, cheapest step that makes the low-poly
            // world look intentional instead of flat.
            var grade = camGo.AddComponent<PostFx>();
            grade.Exposure = 1.02f;
            grade.ToneStrength = 0.88f;
            grade.Saturation = GraphicsSettings.GradeSaturation;
            grade.Contrast = GraphicsSettings.GradeContrast;
            grade.Lift = 0.03f;
            grade.Warmth = 0.72f;
            grade.Vignette = 0.5f;
            grade.Grain = GraphicsSettings.Current == GraphicsSettings.Tier.Low ? 0f : 0.008f;

            return cam;
        }

        private void SetupInput()
        {
            var input = gameObject.AddComponent<FarmInput>();
            input.Init(_camera);

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// Collapses every static prop into a handful of combined meshes.
        /// The farm has ~450 props but only a few materials, so this is the
        /// single biggest performance win in the whole build. If combining is
        /// not possible on a device (unreadable mesh), the world simply keeps
        /// rendering per-object — never let this break the scene.
        /// </summary>
        private static void OptimizeStaticWorld(Transform worldRoot)
        {
            if (worldRoot == null) return;

            var filters = worldRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null || !mesh.isReadable) return;
            }

            try
            {
                StaticBatchingUtility.Combine(worldRoot.gameObject);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Static batching skipped: " + e.Message);
            }
        }
    }
}
