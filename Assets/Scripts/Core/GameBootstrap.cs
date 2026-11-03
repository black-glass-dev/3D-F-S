using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmSim
{
    /// <summary>
    /// Builds the entire game at runtime (camera, light, farm, decor, HUD).
    /// This means the project needs no hand-made scenes/prefabs: the CI build
    /// script creates a scene containing only this component and everything
    /// else appears when the game starts — on phone, editor, anywhere.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Localization.LoadSaved();

            // --- Managers ---
            var manager = gameObject.AddComponent<GameManager>();
            gameObject.AddComponent<UIManager>();

            // --- Camera (fixed isometric angle = 2.5D) ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.80f, 0.95f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            var camController = camGo.AddComponent<CameraController>();

            var input = gameObject.AddComponent<FarmInput>();
            input.Init(cam);

            // --- Sun ---
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;

            // --- Event system for UI touches ---
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // --- Environment & farm ---
            BuildEnvironment();

            var gridGo = new GameObject("FarmGrid");
            var grid = gridGo.AddComponent<FarmGrid>();
            grid.Build();

            manager.Init(grid, sun);

            // --- Restore previous save (if any) ---
            var save = SaveSystem.TryLoad();
            if (save != null)
            {
                manager.ApplySave(save);
                grid.LoadSaveData(save.Plots);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.RefreshAll();
        }

        private void BuildEnvironment()
        {
            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.transform.position = new Vector3(0f, -0.06f, 0f);
            ground.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.38f, 0.68f, 0.30f));
            Object.Destroy(ground.GetComponent<Collider>());

            // Soil bed under the plots
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "SoilBed";
            bed.transform.position = new Vector3(0f, -0.03f, 0f);
            bed.transform.localScale = new Vector3(8.6f, 0.06f, 5.4f);
            bed.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.50f, 0.35f, 0.20f));
            Object.Destroy(bed.GetComponent<Collider>());

            BuildHouse(new Vector3(-8.5f, 0f, 5.5f));
            BuildTree(new Vector3(7.5f, 0f, 6f), 1.1f);
            BuildTree(new Vector3(9f, 0f, 2.5f), 0.9f);
            BuildTree(new Vector3(-9.5f, 0f, -2f), 1.25f);
            BuildTree(new Vector3(6.5f, 0f, -5.5f), 1.0f);
            BuildTree(new Vector3(-6f, 0f, -6f), 0.85f);
            BuildFence();
        }

        private void BuildHouse(Vector3 pos)
        {
            var house = new GameObject("House");
            house.transform.position = pos;

            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.transform.SetParent(house.transform, false);
            baseCube.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            baseCube.transform.localScale = new Vector3(3f, 1.8f, 2.4f);
            baseCube.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.92f, 0.85f, 0.70f));
            Object.Destroy(baseCube.GetComponent<Collider>());

            // Simple pyramid-ish roof: a stretched, rotated cube reads fine at iso angle
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(house.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            roof.transform.localScale = new Vector3(3.5f, 0.35f, 2.9f);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            roof.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.75f, 0.30f, 0.20f));
            Object.Destroy(roof.GetComponent<Collider>());

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(house.transform, false);
            door.transform.localPosition = new Vector3(0f, 0.55f, 1.22f);
            door.transform.localScale = new Vector3(0.7f, 1.1f, 0.06f);
            door.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.40f, 0.25f, 0.12f));
            Object.Destroy(door.GetComponent<Collider>());
        }

        private void BuildTree(Vector3 pos, float scale)
        {
            var tree = new GameObject("Tree");
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * scale;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 0.7f, 0.35f);
            trunk.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.45f, 0.30f, 0.15f));
            Object.Destroy(trunk.GetComponent<Collider>());

            var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(tree.transform, false);
            leaves.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            leaves.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
            leaves.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.20f, 0.55f, 0.22f));
            Object.Destroy(leaves.GetComponent<Collider>());

            var leavesTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leavesTop.transform.SetParent(tree.transform, false);
            leavesTop.transform.localPosition = new Vector3(0.3f, 2.6f, 0.1f);
            leavesTop.transform.localScale = new Vector3(1.0f, 1.1f, 1.0f);
            leavesTop.GetComponent<Renderer>().material = MaterialCache.Get(new Color(0.25f, 0.62f, 0.26f));
            Object.Destroy(leavesTop.GetComponent<Collider>());
        }

        private void BuildFence()
        {
            var fence = new GameObject("Fence");
            var wood = MaterialCache.Get(new Color(0.60f, 0.42f, 0.22f));
            // Posts along the back and sides of the field
            for (float x = -4.6f; x <= 4.6f; x += 1.15f)
            {
                AddPost(fence.transform, new Vector3(x, 0f, -3.1f), wood);
                AddPost(fence.transform, new Vector3(x, 0f, 3.1f), wood);
            }
            for (float z = -2f; z <= 2.1f; z += 1.15f)
            {
                AddPost(fence.transform, new Vector3(-4.6f, 0f, z), wood);
                AddPost(fence.transform, new Vector3(4.6f, 0f, z), wood);
            }
        }

        private void AddPost(Transform parent, Vector3 pos, Material wood)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(pos.x, 0.3f, pos.z);
            post.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            post.GetComponent<Renderer>().material = wood;
            Object.Destroy(post.GetComponent<Collider>());
        }
    }
}
