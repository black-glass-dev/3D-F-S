using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmSim
{
    /// <summary>
    /// Builds the entire game at runtime: managers, camera, sky, sun, the
    /// farm field and the surrounding decoration. The project needs no
    /// hand-made scene or prefabs — the CI build creates a scene containing
    /// only this component and everything else appears when the game starts.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Localization.LoadSaved();

            QualitySettings.shadowDistance = 40f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 2;

            // --- managers ---
            var manager = gameObject.AddComponent<GameManager>();
            var sfx = gameObject.AddComponent<SfxManager>();
            sfx.Init();
            var fx = gameObject.AddComponent<FxManager>();
            fx.Init();

            // --- sky ---
            var skyShader = Resources.Load<Shader>("Shaders/SkyGradient");
            if (skyShader != null)
            {
                RenderSettings.skybox = new Material(skyShader);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 130f;

            // --- camera (fixed isometric angle = 2.5D) ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 220f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            var camController = camGo.AddComponent<CameraController>();
            camController.Init(new Vector3(0f, 0f, 0.2f), 7.4f,
                new Vector2(-9.5f, 9.5f), new Vector2(-7.5f, 8.5f));

            var input = gameObject.AddComponent<FarmInput>();
            input.Init(cam);

            // --- sun ---
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.04f;
            RenderSettings.sun = sun;

            // --- event system for UI touches ---
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // --- world ---
            BuildEnvironment();

            var gridGo = new GameObject("FarmGrid");
            var grid = gridGo.AddComponent<FarmGrid>();
            grid.Build();

            manager.Init(grid, sun);

            // UI is created after the manager so it can subscribe to refresh events.
            gameObject.AddComponent<UIManager>();
            UIManager.Instance.RefreshAll();

            // --- restore previous save (if any) ---
            var save = SaveSystem.TryLoad();
            if (save != null)
            {
                manager.ApplySave(save);
                grid.LoadSaveData(save.Plots);
                UIManager.Instance.RefreshAll();
            }
        }

        // ---------- environment ----------

        private void BuildEnvironment()
        {
            BuildGround();
            BuildPaths();
            BuildHouse(new Vector3(-9.2f, 0f, 6.2f));
            BuildBarn(new Vector3(9.6f, 0f, 5.6f));
            BuildPond(new Vector3(-11.5f, 0f, -5.2f));

            BuildTree(new Vector3(8.2f, 0f, 7.6f), 1.15f);
            BuildTree(new Vector3(10.6f, 0f, 1.6f), 0.92f);
            BuildTree(new Vector3(-11.8f, 0f, -0.4f), 1.25f);
            BuildTree(new Vector3(7.4f, 0f, -6.4f), 1.0f);
            BuildTree(new Vector3(-7.6f, 0f, -7.4f), 0.86f);
            BuildTree(new Vector3(0.8f, 0f, 9.4f), 1.05f);
            BuildTree(new Vector3(-2.6f, 0f, -9.6f), 0.95f);

            BuildBushes();
            BuildRocks();
            BuildFlowers();
            BuildFence();
            BuildScarecrow(new Vector3(-4.4f, 0f, 3.9f));
            BuildSign(new Vector3(3.6f, 0f, 4.1f));
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(9f, 1f, 9f);
            ground.transform.position = new Vector3(0f, -0.02f, 0f);
            var mat = MaterialCache.Get(new Color(0.46f, 0.72f, 0.34f), SurfacePattern.Grass);
            if (mat.HasProperty("_MainTex") && mat.mainTexture != null)
            {
                mat.mainTextureScale = new Vector2(18f, 18f);
            }
            ground.GetComponent<Renderer>().material = mat;
            Destroy(ground.GetComponent<Collider>());

            // darker grass patches for variation
            for (int i = 0; i < 14; i++)
            {
                var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = "GrassPatch";
                float x = Random.Range(-13f, 13f);
                float z = Random.Range(-11f, 11f);
                if (Mathf.Abs(x) < 5.6f && Mathf.Abs(z) < 4.2f) x += 8f;
                patch.transform.position = new Vector3(x, 0.004f, z);
                float size = Random.Range(1.6f, 3.6f);
                patch.transform.localScale = new Vector3(size, 0.012f, size * Random.Range(0.7f, 1.3f));
                patch.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                patch.GetComponent<Renderer>().material =
                    MaterialCache.Get(new Color(0.40f, 0.66f, 0.30f), SurfacePattern.GrassDark);
                Destroy(patch.GetComponent<Collider>());
            }
        }

        private void BuildPaths()
        {
            var stone = MaterialCache.Get(new Color(0.72f, 0.70f, 0.64f), SurfacePattern.Stone);
            for (int i = 0; i < 9; i++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "PathTile";
                tile.transform.position = new Vector3(-6.6f + i * 0.0f, 0.006f, 4.6f - i * 0.95f);
                tile.transform.localScale = new Vector3(1.5f, 0.02f, 0.95f);
                tile.transform.rotation = Quaternion.Euler(0f, Random.Range(-6f, 6f), 0f);
                tile.GetComponent<Renderer>().material = stone;
                Destroy(tile.GetComponent<Collider>());
            }
        }

        private void BuildHouse(Vector3 position)
        {
            var house = new GameObject("House");
            house.transform.position = position;

            var walls = MaterialCache.Get(new Color(0.96f, 0.90f, 0.76f), SurfacePattern.Stone);
            var roofMat = MaterialCache.Get(new Color(0.78f, 0.32f, 0.22f), SurfacePattern.Wood);
            var wood = MaterialCache.Get(new Color(0.50f, 0.33f, 0.18f), SurfacePattern.Wood);

            AddBox(house.transform, new Vector3(0f, 1.05f, 0f), new Vector3(3.4f, 2.1f, 2.7f), walls, 0f);
            AddBox(house.transform, new Vector3(0f, 2.32f, 0f), new Vector3(3.9f, 0.42f, 3.15f), roofMat, 0f);
            AddBox(house.transform, new Vector3(0f, 2.62f, 0f), new Vector3(2.6f, 0.34f, 2.1f), roofMat, 0f);

            // door
            AddBox(house.transform, new Vector3(0f, 0.62f, 1.38f), new Vector3(0.78f, 1.24f, 0.08f), wood, 0f);
            // windows
            var glass = MaterialCache.Get(new Color(0.55f, 0.78f, 0.92f), SurfacePattern.Smooth);
            AddBox(house.transform, new Vector3(-1.05f, 1.30f, 1.37f), new Vector3(0.62f, 0.62f, 0.06f), glass, 0f);
            AddBox(house.transform, new Vector3(1.05f, 1.30f, 1.37f), new Vector3(0.62f, 0.62f, 0.06f), glass, 0f);
            // chimney
            var brick = MaterialCache.Get(new Color(0.62f, 0.34f, 0.28f), SurfacePattern.Stone);
            AddBox(house.transform, new Vector3(1.1f, 2.9f, -0.5f), new Vector3(0.42f, 0.9f, 0.42f), brick, 0f);
        }

        private void BuildBarn(Vector3 position)
        {
            var barn = new GameObject("Barn");
            barn.transform.position = position;

            var red = MaterialCache.Get(new Color(0.72f, 0.26f, 0.22f), SurfacePattern.Wood);
            var roof = MaterialCache.Get(new Color(0.36f, 0.24f, 0.20f), SurfacePattern.Stone);
            var white = MaterialCache.Get(new Color(0.94f, 0.92f, 0.86f), SurfacePattern.Wood);

            AddBox(barn.transform, new Vector3(0f, 1.0f, 0f), new Vector3(3.0f, 2.0f, 2.6f), red, 0f);
            AddBox(barn.transform, new Vector3(0f, 2.2f, 0f), new Vector3(3.5f, 0.4f, 3.0f), roof, 0f);
            AddBox(barn.transform, new Vector3(0f, 0.85f, 1.34f), new Vector3(1.3f, 1.6f, 0.08f), white, 0f);
            AddBox(barn.transform, new Vector3(0f, 0.85f, 1.36f), new Vector3(0.09f, 1.6f, 0.06f), red, 0f);
            AddBox(barn.transform, new Vector3(0f, 0.85f, 1.37f), new Vector3(1.3f, 0.09f, 0.06f), red, 0f);
        }

        private void BuildPond(Vector3 position)
        {
            var pond = new GameObject("Pond");
            pond.transform.position = position;

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pond.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            rim.transform.localScale = new Vector3(4.2f, 0.015f, 3.0f);
            rim.GetComponent<Renderer>().material =
                MaterialCache.Get(new Color(0.62f, 0.58f, 0.44f), SurfacePattern.Stone);
            Destroy(rim.GetComponent<Collider>());

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "PondWater";
            water.transform.SetParent(pond.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            water.transform.localScale = new Vector3(3.6f, 0.012f, 2.5f);
            water.GetComponent<Renderer>().material =
                MaterialCache.Get(new Color(0.32f, 0.62f, 0.85f), SurfacePattern.Water);
            Destroy(water.GetComponent<Collider>());
        }

        private void BuildTree(Vector3 position, float scale)
        {
            var tree = new GameObject("Tree");
            tree.transform.position = position;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var trunkMat = MaterialCache.Get(new Color(0.46f, 0.31f, 0.18f), SurfacePattern.Wood);
            var leafMat = MaterialCache.Get(new Color(0.24f, 0.58f, 0.26f), SurfacePattern.Leaf);
            var leafLightMat = MaterialCache.Get(new Color(0.34f, 0.70f, 0.32f), SurfacePattern.Leaf);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            trunk.transform.localScale = new Vector3(0.34f, 0.75f, 0.34f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            Destroy(trunk.GetComponent<Collider>());

            AddSphere(tree.transform, new Vector3(0f, 1.95f, 0f), new Vector3(1.7f, 1.9f, 1.7f), leafMat);
            AddSphere(tree.transform, new Vector3(0.45f, 2.75f, 0.1f), new Vector3(1.05f, 1.05f, 1.05f), leafLightMat);
            AddSphere(tree.transform, new Vector3(-0.4f, 2.5f, -0.25f), new Vector3(0.85f, 0.85f, 0.85f), leafMat);
        }

        private void BuildBushes()
        {
            var leafMat = MaterialCache.Get(new Color(0.30f, 0.62f, 0.28f), SurfacePattern.Leaf);
            var berryMat = MaterialCache.Get(new Color(0.86f, 0.30f, 0.36f), SurfacePattern.Smooth);

            var positions = new[]
            {
                new Vector3(-5.6f, 0f, 7.2f), new Vector3(5.4f, 0f, 8.2f),
                new Vector3(-9.8f, 0f, 1.8f), new Vector3(12.0f, 0f, -3.4f),
                new Vector3(-3.2f, 0f, -8.4f), new Vector3(4.2f, 0f, -8.6f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var bush = new GameObject("Bush");
                bush.transform.position = positions[i];
                bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                AddSphere(bush.transform, new Vector3(0f, 0.34f, 0f), new Vector3(1.0f, 0.72f, 1.0f), leafMat);
                AddSphere(bush.transform, new Vector3(0.32f, 0.28f, 0.2f), new Vector3(0.6f, 0.5f, 0.6f), leafMat);
                AddSphere(bush.transform, new Vector3(-0.18f, 0.46f, -0.16f), Vector3.one * 0.11f, berryMat);
            }
        }

        private void BuildRocks()
        {
            var rockMat = MaterialCache.Get(new Color(0.58f, 0.57f, 0.53f), SurfacePattern.Stone);
            var positions = new[]
            {
                new Vector3(-8.4f, 0f, -8.2f), new Vector3(6.2f, 0f, 3.2f),
                new Vector3(11.4f, 0f, 3.6f), new Vector3(-12.6f, 0f, 2.4f),
                new Vector3(2.2f, 0f, -9.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock";
                rock.transform.position = positions[i] + new Vector3(0f, 0.16f, 0f);
                float size = Random.Range(0.34f, 0.7f);
                rock.transform.localScale = new Vector3(size, size * 0.7f, size * Random.Range(0.8f, 1.2f));
                rock.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                rock.GetComponent<Renderer>().material = rockMat;
                Destroy(rock.GetComponent<Collider>());
            }
        }

        private void BuildFlowers()
        {
            var stemMat = MaterialCache.Get(new Color(0.30f, 0.62f, 0.28f), SurfacePattern.Leaf);
            var colors = new[]
            {
                new Color(0.95f, 0.42f, 0.55f), new Color(0.98f, 0.82f, 0.32f),
                new Color(0.75f, 0.55f, 0.95f), new Color(0.98f, 0.96f, 0.92f)
            };

            for (int i = 0; i < 12; i++)
            {
                float x = Random.Range(-12.5f, 12.5f);
                float z = Random.Range(-10f, 10.5f);
                if (Mathf.Abs(x) < 5.4f && Mathf.Abs(z) < 4.0f) continue;

                var flower = new GameObject("Flower");
                flower.transform.position = new Vector3(x, 0f, z);
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(flower.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.14f, 0f);
                stem.transform.localScale = new Vector3(0.03f, 0.14f, 0.03f);
                stem.GetComponent<Renderer>().material = stemMat;
                Destroy(stem.GetComponent<Collider>());

                AddSphere(flower.transform, new Vector3(0f, 0.30f, 0f), Vector3.one * 0.12f,
                    MaterialCache.Get(colors[i % colors.Length]));
            }
        }

        private void BuildFence()
        {
            var fence = new GameObject("Fence");
            var wood = MaterialCache.Get(new Color(0.68f, 0.50f, 0.28f), SurfacePattern.Wood);

            float halfWidth = FarmGrid.FieldWidth * 0.5f + 1.05f;
            float halfDepth = FarmGrid.FieldDepth * 0.5f + 1.05f;

            // back and side fences with a gap at the front for the path
            for (float x = -halfWidth; x <= halfWidth + 0.01f; x += 1.1f)
            {
                AddPost(fence.transform, new Vector3(x, 0f, -halfDepth), wood);
                if (Mathf.Abs(x) > 1.4f)
                    AddPost(fence.transform, new Vector3(x, 0f, halfDepth), wood);
            }
            for (float z = -halfDepth; z <= halfDepth + 0.01f; z += 1.1f)
            {
                AddPost(fence.transform, new Vector3(-halfWidth, 0f, z), wood);
                AddPost(fence.transform, new Vector3(halfWidth, 0f, z), wood);
            }

            // horizontal rails
            AddRail(fence.transform, new Vector3(0f, 0.44f, -halfDepth), new Vector3(halfWidth * 2f, 0.07f, 0.07f), wood);
            AddRail(fence.transform, new Vector3(0f, 0.26f, -halfDepth), new Vector3(halfWidth * 2f, 0.07f, 0.07f), wood);
            AddRail(fence.transform, new Vector3(-halfWidth, 0.44f, 0f), new Vector3(0.07f, 0.07f, halfDepth * 2f), wood);
            AddRail(fence.transform, new Vector3(halfWidth, 0.44f, 0f), new Vector3(0.07f, 0.07f, halfDepth * 2f), wood);
        }

        private void AddPost(Transform parent, Vector3 position, Material material)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(position.x, 0.32f, position.z);
            post.transform.localScale = new Vector3(0.13f, 0.64f, 0.13f);
            post.GetComponent<Renderer>().material = material;
            Destroy(post.GetComponent<Collider>());
        }

        private void AddRail(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Rail";
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = position;
            rail.transform.localScale = scale;
            rail.GetComponent<Renderer>().material = material;
            Destroy(rail.GetComponent<Collider>());
        }

        private void BuildScarecrow(Vector3 position)
        {
            var scarecrow = new GameObject("Scarecrow");
            scarecrow.transform.position = position;

            var wood = MaterialCache.Get(new Color(0.52f, 0.36f, 0.20f), SurfacePattern.Wood);
            var cloth = MaterialCache.Get(new Color(0.62f, 0.40f, 0.62f), SurfacePattern.Smooth);
            var straw = MaterialCache.Get(new Color(0.92f, 0.78f, 0.40f), SurfacePattern.Leaf);

            // post + crossbar
            AddBox(scarecrow.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.12f, 1.5f, 0.12f), wood, 0f);
            AddBox(scarecrow.transform, new Vector3(0f, 1.18f, 0f), new Vector3(1.3f, 0.11f, 0.11f), wood, 0f);
            // shirt
            AddBox(scarecrow.transform, new Vector3(0f, 1.08f, 0.02f), new Vector3(0.72f, 0.72f, 0.26f), cloth, 0f);
            // straw head + hat
            AddSphere(scarecrow.transform, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.34f, straw);
            AddBox(scarecrow.transform, new Vector3(0f, 1.8f, 0f), new Vector3(0.72f, 0.06f, 0.72f), wood, 0f);
            AddBox(scarecrow.transform, new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.22f, 0.36f), wood, 0f);
        }

        private void BuildSign(Vector3 position)
        {
            var sign = new GameObject("Sign");
            sign.transform.position = position;
            var wood = MaterialCache.Get(new Color(0.58f, 0.40f, 0.22f), SurfacePattern.Wood);
            var board = MaterialCache.Get(new Color(0.86f, 0.74f, 0.52f), SurfacePattern.Wood);

            AddBox(sign.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.12f, 0.8f, 0.12f), wood, 0f);
            AddBox(sign.transform, new Vector3(0f, 0.86f, 0f), new Vector3(1.1f, 0.5f, 0.1f), board, 0f);
        }

        private void AddBox(Transform parent, Vector3 localPosition, Vector3 scale, Material material, float yRotation)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            box.GetComponent<Renderer>().material = material;
            Destroy(box.GetComponent<Collider>());
        }

        private void AddSphere(Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = scale;
            sphere.GetComponent<Renderer>().material = material;
            Destroy(sphere.GetComponent<Collider>());
        }
    }
}
