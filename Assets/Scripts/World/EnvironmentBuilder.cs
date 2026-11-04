using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Builds the world: rolling terrain, a gravel path, a farmhouse with a
    /// proper gable roof, a barn, a windmill, a pond, trees, bushes, rocks,
    /// grass and clouds.
    ///
    /// v1.0.3 art direction: muted, natural colours (no neon green), softer
    /// light, and every object grounded with a soft contact shadow. Everything
    /// static lives under one root so GameBootstrap can collapse it with
    /// StaticBatchingUtility.Combine() — hundreds of props, a few draw calls.
    /// </summary>
    public class EnvironmentBuilder
    {
        private Transform _staticRoot;
        private Transform _movingRoot;

        private Material _grass;
        private Material _hill;
        private Material _path;
        private Material _soil;
        private Material _wood;
        private Material _woodDark;
        private Material _stone;
        private Material _rock;
        private Material _trunk;
        private Material _leafA;
        private Material _leafB;
        private Material _leafC;
        private Material _water;
        private Material _walls;
        private Material _glass;
        private Material _barnRed;
        private Material _roofRed;
        private Material _roofBrown;
        private Material _cloud;
        private Material _flowerA;
        private Material _flowerB;
        private Material _flowerC;
        private Material _flowerD;
        private Material _straw;
        private Material _shadow;

        public Transform Build()
        {
            _staticRoot = new GameObject("Environment").transform;
            _movingRoot = new GameObject("EnvironmentMoving").transform;

            InitMaterials();

            BuildTerrain();
            BuildHills();
            BuildPath();
            BuildHouse(new Vector3(-9.6f, 0f, 6.6f));
            BuildBarn(new Vector3(10.2f, 0f, 6.2f));
            BuildWindmill(new Vector3(12.4f, 0f, -4.0f));
            BuildPond(new Vector3(-11.6f, 0f, -4.8f));
            BuildTrees();
            BuildBushes();
            BuildRocks();
            BuildFlowers();
            BuildGrassTufts();
            BuildFence();
            BuildScarecrow(new Vector3(-4.8f, 0f, 4.6f));
            BuildSign(new Vector3(4.1f, 0f, 4.8f));
            BuildCrates();
            BuildClouds();

            return _staticRoot;
        }

        // ---------- palette ----------

        private void InitMaterials()
        {
            _grass = MaterialCache.GetUnique(new Color(0.40f, 0.49f, 0.30f), SurfacePattern.Grass);
            if (_grass.HasProperty("_VertexTint")) _grass.SetFloat("_VertexTint", 1f);

            _hill = MaterialCache.Get(new Color(0.42f, 0.46f, 0.45f), SurfacePattern.GrassDark);
            _path = MaterialCache.Get(new Color(0.50f, 0.48f, 0.42f), SurfacePattern.Stone);
            _soil = MaterialCache.Get(new Color(0.40f, 0.29f, 0.20f), SurfacePattern.Soil);
            _wood = MaterialCache.Get(new Color(0.50f, 0.38f, 0.25f), SurfacePattern.Wood);
            _woodDark = MaterialCache.Get(new Color(0.34f, 0.25f, 0.17f), SurfacePattern.Wood);
            _stone = MaterialCache.Get(new Color(0.56f, 0.55f, 0.51f), SurfacePattern.Stone);
            _rock = MaterialCache.Get(new Color(0.48f, 0.47f, 0.44f), SurfacePattern.Stone);
            _trunk = MaterialCache.Get(new Color(0.34f, 0.26f, 0.19f), SurfacePattern.Wood);
            _leafA = MaterialCache.Get(new Color(0.25f, 0.38f, 0.24f), SurfacePattern.Leaf);
            _leafB = MaterialCache.Get(new Color(0.31f, 0.45f, 0.27f), SurfacePattern.Leaf);
            _leafC = MaterialCache.Get(new Color(0.19f, 0.31f, 0.21f), SurfacePattern.Leaf);
            _water = MaterialCache.Get(new Color(0.17f, 0.30f, 0.34f), SurfacePattern.Water);
            _walls = MaterialCache.Get(new Color(0.82f, 0.78f, 0.66f), SurfacePattern.Wood);
            _glass = MaterialCache.Get(new Color(0.35f, 0.45f, 0.48f), SurfacePattern.Smooth);
            _barnRed = MaterialCache.Get(new Color(0.45f, 0.22f, 0.18f), SurfacePattern.Wood);
            _roofRed = MaterialCache.Get(new Color(0.48f, 0.28f, 0.21f), SurfacePattern.Wood);
            _roofBrown = MaterialCache.Get(new Color(0.30f, 0.25f, 0.21f), SurfacePattern.Stone);
            _cloud = MaterialCache.Get(new Color(0.90f, 0.91f, 0.92f), SurfacePattern.Smooth);
            _flowerA = MaterialCache.Get(new Color(0.68f, 0.42f, 0.48f));
            _flowerB = MaterialCache.Get(new Color(0.82f, 0.70f, 0.40f));
            _flowerC = MaterialCache.Get(new Color(0.55f, 0.47f, 0.62f));
            _flowerD = MaterialCache.Get(new Color(0.85f, 0.83f, 0.76f));
            _straw = MaterialCache.Get(new Color(0.78f, 0.66f, 0.36f), SurfacePattern.Leaf);
            _shadow = MaterialCache.BlobMaterial;
        }

        // ---------- terrain & horizon ----------

        private void BuildTerrain()
        {
            const float half = 62f;
            const int cells = 96;
            int side = cells + 1;

            var verts = new Vector3[side * side];
            var uvs = new Vector2[side * side];
            var colors = new Color[side * side];
            var tris = new List<int>(cells * cells * 6);
            var inside = new Vector3(0f, -80f, 0f);

            for (int iz = 0; iz < side; iz++)
            {
                for (int ix = 0; ix < side; ix++)
                {
                    float x = -half + (ix / (float)cells) * half * 2f;
                    float z = -half + (iz / (float)cells) * half * 2f;
                    float y = TerrainShape.Height(x, z);

                    int index = iz * side + ix;
                    verts[index] = new Vector3(x, y, z);
                    uvs[index] = new Vector2(x * 0.22f, z * 0.22f);

                    // baked-in colour variation + darker hollows = depth without AO
                    float grain = Mathf.PerlinNoise(x * 0.31f + 8.1f, z * 0.31f + 3.4f);
                    float tint = 0.86f + grain * 0.20f;
                    tint *= Mathf.Lerp(0.78f, 1.02f, Mathf.InverseLerp(-1.4f, 0.7f, y));
                    colors[index] = new Color(tint, tint, tint, 1f);
                }
            }

            for (int iz = 0; iz < cells; iz++)
            {
                for (int ix = 0; ix < cells; ix++)
                {
                    int a = iz * side + ix;
                    int b = iz * side + ix + 1;
                    int c = (iz + 1) * side + ix + 1;
                    int d = (iz + 1) * side + ix;
                    AddTriOriented(tris, verts, a, b, c, inside);
                    AddTriOriented(tris, verts, a, c, d, inside);
                }
            }

            var mesh = new Mesh { name = "Terrain" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(new List<Vector3>(verts));
            mesh.SetUVs(0, new List<Vector2>(uvs));
            mesh.SetColors(new List<Color>(colors));
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(_staticRoot, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = terrain.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _grass;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void BuildHills()
        {
            var rng = new System.Random(19);
            for (int i = 0; i < 11; i++)
            {
                float angle = (i / 11f) * Mathf.PI * 2f + 0.35f;
                float distance = 40f + (float)rng.NextDouble() * 16f;
                float x = Mathf.Cos(angle) * distance;
                float z = Mathf.Sin(angle) * distance;

                var hill = new GameObject("Hill");
                hill.transform.SetParent(_staticRoot, false);
                hill.transform.localPosition = new Vector3(x, TerrainShape.Height(x, z) - 1.2f, z);
                hill.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                float radius = 11f + (float)rng.NextDouble() * 12f;
                float height = 4f + (float)rng.NextDouble() * 6f;
                hill.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);

                hill.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Cone;
                var renderer = hill.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _hill;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void BuildPath()
        {
            var control = new[]
            {
                new Vector3(-9.9f, 0f, 8.0f),
                new Vector3(-8.0f, 0f, 7.3f),
                new Vector3(-5.4f, 0f, 6.2f),
                new Vector3(-3.0f, 0f, 4.9f),
                new Vector3(-0.6f, 0f, 4.0f),
                new Vector3(1.6f, 0f, 3.4f)
            };

            var points = new List<Vector3>();
            for (int i = 0; i < control.Length - 1; i++)
            {
                for (int s = 0; s < 5; s++)
                    points.Add(Vector3.Lerp(control[i], control[i + 1], s / 5f));
            }
            points.Add(control[control.Length - 1]);

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var inside = new Vector3(0f, -40f, 0f);
            float travelled = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward = i < points.Count - 1
                    ? points[i + 1] - points[i]
                    : points[i] - points[i - 1];
                forward.y = 0f;
                forward.Normalize();
                Vector3 side = new Vector3(-forward.z, 0f, forward.x);

                float width = 0.62f + Mathf.PerlinNoise(i * 0.45f, 3.2f) * 0.20f;
                if (i > 0) travelled += Vector3.Distance(points[i - 1], points[i]);

                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 p = points[i] + side * width * k;
                    p.y = TerrainShape.Height(p.x, p.z) + 0.03f;
                    verts.Add(p);
                    uvs.Add(new Vector2((k + 1) * 0.5f, travelled * 0.5f));
                }
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int a = i * 2;
                int b = i * 2 + 1;
                int c = (i + 1) * 2 + 1;
                int d = (i + 1) * 2;
                AddTriOriented(tris, verts, a, b, c, inside);
                AddTriOriented(tris, verts, a, c, d, inside);
            }

            var mesh = new Mesh { name = "Path" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var path = new GameObject("Path");
            path.transform.SetParent(_staticRoot, false);
            path.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = path.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _path;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- buildings ----------

        private void BuildHouse(Vector3 position)
        {
            var house = new GameObject("House");
            house.transform.SetParent(_staticRoot, false);
            house.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);
            house.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);

            AddContactShadow(house.transform, new Vector3(0f, 0.02f, 0f), 4.6f);

            AddBox(house.transform, new Vector3(0f, 0.14f, 0f), new Vector3(3.7f, 0.28f, 2.95f), _stone);
            AddBox(house.transform, new Vector3(0f, 1.24f, 0f), new Vector3(3.4f, 1.94f, 2.7f), _walls);

            AddMesh(house.transform, "Roof", MeshFactory.Roof, _roofRed,
                new Vector3(0f, 2.2f, 0f), new Vector3(3.98f, 1.25f, 3.28f), 0f);

            // door + frame + handle
            AddBox(house.transform, new Vector3(0f, 0.82f, 1.36f), new Vector3(0.9f, 1.4f, 0.08f), _woodDark);
            AddBox(house.transform, new Vector3(0f, 0.78f, 1.40f), new Vector3(0.72f, 1.22f, 0.06f), _wood);
            AddBox(house.transform, new Vector3(0.24f, 0.78f, 1.44f), new Vector3(0.06f, 0.06f, 0.05f), _stone);

            AddWindow(house.transform, new Vector3(-1.12f, 1.36f, 1.37f));
            AddWindow(house.transform, new Vector3(1.12f, 1.36f, 1.37f));

            // chimney
            AddBox(house.transform, new Vector3(1.1f, 3.1f, -0.6f), new Vector3(0.44f, 1.1f, 0.44f), _stone);
            AddBox(house.transform, new Vector3(1.1f, 3.68f, -0.6f), new Vector3(0.54f, 0.1f, 0.54f), _rock);

            // porch: step + two posts + small shelter
            AddBox(house.transform, new Vector3(0f, 0.1f, 1.66f), new Vector3(1.5f, 0.18f, 0.5f), _stone);
            AddBox(house.transform, new Vector3(-0.85f, 1.1f, 1.72f), new Vector3(0.11f, 2.0f, 0.11f), _woodDark);
            AddBox(house.transform, new Vector3(0.85f, 1.1f, 1.72f), new Vector3(0.11f, 2.0f, 0.11f), _woodDark);
            AddBox(house.transform, new Vector3(0f, 2.1f, 1.62f), new Vector3(2.0f, 0.1f, 0.7f), _roofBrown);

            // flower boxes under the windows
            AddBox(house.transform, new Vector3(-1.12f, 0.98f, 1.44f), new Vector3(0.72f, 0.16f, 0.2f), _woodDark);
            AddBox(house.transform, new Vector3(1.12f, 0.98f, 1.44f), new Vector3(0.72f, 0.16f, 0.2f), _woodDark);
            AddSphere(house.transform, new Vector3(-1.28f, 1.08f, 1.46f), Vector3.one * 0.13f, _flowerA);
            AddSphere(house.transform, new Vector3(-0.98f, 1.08f, 1.46f), Vector3.one * 0.13f, _flowerB);
            AddSphere(house.transform, new Vector3(1.26f, 1.08f, 1.46f), Vector3.one * 0.13f, _flowerC);
            AddSphere(house.transform, new Vector3(0.96f, 1.08f, 1.46f), Vector3.one * 0.13f, _flowerA);
        }

        private void AddWindow(Transform parent, Vector3 localPosition)
        {
            AddBox(parent, localPosition, new Vector3(0.74f, 0.74f, 0.05f), _woodDark);
            AddBox(parent, localPosition + new Vector3(0f, 0f, 0.02f), new Vector3(0.58f, 0.58f, 0.04f), _glass);
            AddBox(parent, localPosition + new Vector3(0f, 0f, 0.04f), new Vector3(0.055f, 0.58f, 0.03f), _wood);
            AddBox(parent, localPosition + new Vector3(0f, 0f, 0.04f), new Vector3(0.58f, 0.055f, 0.03f), _wood);
            // shutters
            AddBox(parent, localPosition + new Vector3(-0.44f, 0f, 0.01f), new Vector3(0.12f, 0.7f, 0.05f), _wood);
            AddBox(parent, localPosition + new Vector3(0.44f, 0f, 0.01f), new Vector3(0.12f, 0.7f, 0.05f), _wood);
        }

        private void BuildBarn(Vector3 position)
        {
            var barn = new GameObject("Barn");
            barn.transform.SetParent(_staticRoot, false);
            barn.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);
            barn.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);

            AddContactShadow(barn.transform, new Vector3(0f, 0.02f, 0f), 4.2f);

            AddBox(barn.transform, new Vector3(0f, 1.12f, 0f), new Vector3(3.1f, 2.0f, 2.6f), _barnRed);
            AddMesh(barn.transform, "Roof", MeshFactory.Roof, _roofBrown,
                new Vector3(0f, 2.1f, 0f), new Vector3(3.62f, 1.4f, 3.06f), 0f);

            // white trim + big door with the classic cross
            AddBox(barn.transform, new Vector3(0f, 0.96f, 1.32f), new Vector3(1.5f, 1.72f, 0.06f), _walls);
            AddBox(barn.transform, new Vector3(0f, 0.96f, 1.35f), new Vector3(0.09f, 1.72f, 0.05f), _barnRed);
            AddBox(barn.transform, new Vector3(0f, 1.38f, 1.35f), new Vector3(1.5f, 0.09f, 0.05f), _barnRed);
            AddBox(barn.transform, new Vector3(0f, 1.12f, 1.35f), new Vector3(1.5f, 0.09f, 0.05f), _barnRed);

            // loft window
            AddBox(barn.transform, new Vector3(0f, 1.86f, 1.32f), new Vector3(0.5f, 0.42f, 0.05f), _woodDark);

            // hay bales outside
            for (int i = 0; i < 3; i++)
            {
                var bale = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bale.name = "HayBale";
                bale.transform.SetParent(barn.transform, false);
                bale.transform.localPosition = new Vector3(2.05f + i * 0.28f, 0.34f, -0.7f + i * 0.72f);
                bale.transform.localScale = new Vector3(0.7f, 0.34f, 0.7f);
                bale.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                bale.GetComponent<Renderer>().sharedMaterial = _straw;
                Object.Destroy(bale.GetComponent<Collider>());
            }
        }

        private void BuildWindmill(Vector3 position)
        {
            var mill = new GameObject("Windmill");
            mill.transform.SetParent(_staticRoot, false);
            mill.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);

            AddContactShadow(mill.transform, new Vector3(0f, 0.02f, 0f), 3.2f);

            // tapered tower
            AddMesh(mill.transform, "Tower", MeshFactory.Trunk, _walls,
                new Vector3(0f, 0f, 0f), new Vector3(2.1f, 4.0f, 2.1f), 0f);

            AddMesh(mill.transform, "Cap", MeshFactory.Cone, _roofRed,
                new Vector3(0f, 4.0f, 0f), new Vector3(1.5f, 0.95f, 1.5f), 0f);

            // rotating blades live under the moving root, offset in front of the tower
            var hub = new GameObject("Blades");
            hub.transform.SetParent(_movingRoot, false);
            var facing = Quaternion.Euler(0f, 40f, 0f) * Vector3.forward;
            hub.transform.position = new Vector3(position.x, TerrainShape.Height(position.x, position.z) + 3.5f, position.z)
                                    + facing * 1.15f;
            hub.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            var spinner = hub.AddComponent<Spinner>();
            spinner.Axis = Vector3.forward;
            spinner.DegreesPerSecond = 20f;

            for (int i = 0; i < 4; i++)
            {
                var arm = new GameObject("Blade" + i);
                arm.transform.SetParent(hub.transform, false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);

                var spar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spar.transform.SetParent(arm.transform, false);
                spar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                spar.transform.localScale = new Vector3(0.08f, 1.8f, 0.06f);
                spar.GetComponent<Renderer>().sharedMaterial = _wood;
                Object.Destroy(spar.GetComponent<Collider>());

                var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sail.transform.SetParent(arm.transform, false);
                sail.transform.localPosition = new Vector3(0.24f, 1.1f, 0f);
                sail.transform.localScale = new Vector3(0.38f, 1.15f, 0.03f);
                sail.GetComponent<Renderer>().sharedMaterial = _walls;
                Object.Destroy(sail.GetComponent<Collider>());
            }

            var hubSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hubSphere.transform.SetParent(hub.transform, false);
            hubSphere.transform.localScale = Vector3.one * 0.28f;
            hubSphere.GetComponent<Renderer>().sharedMaterial = _woodDark;
            Object.Destroy(hubSphere.GetComponent<Collider>());
        }

        private void BuildPond(Vector3 position)
        {
            var pond = new GameObject("Pond");
            pond.transform.SetParent(_staticRoot, false);
            pond.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pond.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            rim.transform.localScale = new Vector3(4.7f, 0.02f, 3.4f);
            rim.GetComponent<Renderer>().sharedMaterial = _stone;
            Object.Destroy(rim.GetComponent<Collider>());

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "PondWater";
            water.transform.SetParent(pond.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            water.transform.localScale = new Vector3(3.9f, 0.015f, 2.7f);
            var waterMaterial = new Material(_water) { name = "FarmPondWater" };
            var waterRenderer = water.GetComponent<Renderer>();
            waterRenderer.sharedMaterial = waterMaterial;
            waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(water.GetComponent<Collider>());
            water.AddComponent<WaterFlow>().Surface = waterMaterial;

            // a few stones around the rim
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f) * Mathf.PI * 2f + 0.3f;
                AddMesh(pond.transform, "RimStone", MeshFactory.Rock(i),
                    _rock,
                    new Vector3(Mathf.Cos(a) * 2.2f, 0.06f, Mathf.Sin(a) * 1.6f),
                    Vector3.one * Random.Range(0.28f, 0.46f), Random.Range(0f, 360f));
            }

            for (int i = 0; i < 4; i++)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "LilyPad";
                pad.transform.SetParent(pond.transform, false);
                pad.transform.localPosition = new Vector3(Random.Range(-1.1f, 1.1f), 0.05f, Random.Range(-0.7f, 0.7f));
                pad.transform.localScale = new Vector3(0.42f, 0.008f, 0.42f);
                pad.GetComponent<Renderer>().sharedMaterial = _leafB;
                Object.Destroy(pad.GetComponent<Collider>());
            }

            for (int i = 0; i < 6; i++)
            {
                var reed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reed.name = "Reed";
                reed.transform.SetParent(pond.transform, false);
                float a = Random.Range(0f, Mathf.PI * 2f);
                reed.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.9f, 0.3f, Mathf.Sin(a) * 1.35f);
                reed.transform.localScale = new Vector3(0.04f, Random.Range(0.25f, 0.5f), 0.04f);
                reed.GetComponent<Renderer>().sharedMaterial = _leafC;
                Object.Destroy(reed.GetComponent<Collider>());
            }
        }

        // ---------- vegetation ----------

        private void BuildTrees()
        {
            BuildOak(new Vector3(6.9f, 0f, 9.5f), 1.15f);
            BuildPine(new Vector3(11.8f, 0f, 1.4f), 1.0f);
            BuildOak(new Vector3(-13.4f, 0f, -0.6f), 1.25f);
            BuildPine(new Vector3(7.6f, 0f, -7.4f), 1.05f);
            BuildOak(new Vector3(-8.4f, 0f, -8.6f), 0.9f);
            BuildPine(new Vector3(0.6f, 0f, 10.6f), 1.1f);
            BuildPoplar(new Vector3(-3.2f, 0f, -10.6f), 1.0f);
            BuildOak(new Vector3(14.8f, 0f, 4.4f), 1.0f);
            BuildPoplar(new Vector3(-15.2f, 0f, 4.6f), 0.95f);
            BuildOak(new Vector3(3.4f, 0f, 8.4f), 0.8f);
            BuildPine(new Vector3(-16.0f, 0f, -7.6f), 1.15f);
            BuildOak(new Vector3(16.2f, 0f, -2.4f), 1.05f);
        }

        private void BuildOak(Vector3 position, float scale)
        {
            var tree = new GameObject("Oak");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddContactShadow(tree.transform, new Vector3(0f, 0.02f, 0f), 2.6f);

            AddMesh(tree.transform, "Trunk", MeshFactory.Trunk, _trunk,
                new Vector3(0f, 0f, 0f), new Vector3(0.46f, 1.9f, 0.46f), 0f);

            var leaf = Random.value > 0.5f ? _leafA : _leafB;
            AddMesh(tree.transform, "Canopy", MeshFactory.Bush, leaf,
                new Vector3(0f, 2.45f, 0f), new Vector3(2.5f, 2.0f, 2.5f), Random.Range(0f, 360f));
            AddMesh(tree.transform, "CanopyB", MeshFactory.Bush, _leafB,
                new Vector3(0.62f, 3.05f, 0.2f), new Vector3(1.55f, 1.35f, 1.55f), Random.Range(0f, 360f));
            AddMesh(tree.transform, "CanopyC", MeshFactory.Bush, _leafC,
                new Vector3(-0.58f, 2.85f, -0.3f), new Vector3(1.35f, 1.2f, 1.35f), Random.Range(0f, 360f));
        }

        private void BuildPine(Vector3 position, float scale)
        {
            var tree = new GameObject("Pine");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddContactShadow(tree.transform, new Vector3(0f, 0.02f, 0f), 2.4f);

            AddMesh(tree.transform, "Trunk", MeshFactory.Trunk, _trunk,
                new Vector3(0f, 0f, 0f), new Vector3(0.36f, 1.1f, 0.36f), 0f);
            AddMesh(tree.transform, "Layer1", MeshFactory.Cone, _leafC,
                new Vector3(0f, 0.85f, 0f), new Vector3(2.6f, 1.8f, 2.6f), Random.Range(0f, 360f));
            AddMesh(tree.transform, "Layer2", MeshFactory.Cone, _leafA,
                new Vector3(0f, 1.95f, 0f), new Vector3(2.0f, 1.6f, 2.0f), Random.Range(0f, 360f));
            AddMesh(tree.transform, "Layer3", MeshFactory.Cone, _leafB,
                new Vector3(0f, 2.95f, 0f), new Vector3(1.35f, 1.4f, 1.35f), Random.Range(0f, 360f));
        }

        private void BuildPoplar(Vector3 position, float scale)
        {
            var tree = new GameObject("Poplar");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = new Vector3(position.x, TerrainShape.Height(position.x, position.z), position.z);
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddContactShadow(tree.transform, new Vector3(0f, 0.02f, 0f), 2.2f);

            AddMesh(tree.transform, "Trunk", MeshFactory.Trunk, _trunk,
                new Vector3(0f, 0f, 0f), new Vector3(0.34f, 1.4f, 0.34f), 0f);
            AddMesh(tree.transform, "Crown", MeshFactory.Bush, _leafA,
                new Vector3(0f, 2.35f, 0f), new Vector3(1.45f, 2.6f, 1.45f), Random.Range(0f, 360f));
            AddMesh(tree.transform, "CrownTop", MeshFactory.Bush, _leafB,
                new Vector3(0.1f, 3.5f, 0f), new Vector3(0.95f, 1.4f, 0.95f), Random.Range(0f, 360f));
        }

        private void BuildBushes()
        {
            var positions = new[]
            {
                new Vector3(-6.2f, 0f, 7.8f), new Vector3(5.6f, 0f, 9.0f),
                new Vector3(-10.4f, 0f, 2.2f), new Vector3(12.6f, 0f, -2.6f),
                new Vector3(-3.4f, 0f, -9.0f), new Vector3(4.6f, 0f, -9.6f),
                new Vector3(15.4f, 0f, -0.4f), new Vector3(-15.6f, 0f, -2.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var bush = new GameObject("Bush");
                bush.transform.SetParent(_staticRoot, false);
                bush.transform.localPosition = new Vector3(
                    positions[i].x, TerrainShape.Height(positions[i].x, positions[i].z), positions[i].z);
                bush.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                bush.transform.localScale = Vector3.one * Random.Range(0.9f, 1.15f);

                AddMesh(bush.transform, "BodyA", MeshFactory.Bush, _leafA,
                    new Vector3(0f, 0.34f, 0f), new Vector3(1.25f, 0.78f, 1.25f), Random.Range(0f, 360f));
                AddMesh(bush.transform, "BodyB", MeshFactory.Bush, _leafB,
                    new Vector3(0.38f, 0.28f, 0.22f), new Vector3(0.8f, 0.6f, 0.8f), Random.Range(0f, 360f));
            }
        }

        private void BuildRocks()
        {
            var positions = new[]
            {
                new Vector3(-8.9f, 0f, -8.8f), new Vector3(6.4f, 0f, 3.4f),
                new Vector3(12.9f, 0f, 3.8f), new Vector3(-13.2f, 0f, 2.8f),
                new Vector3(2.4f, 0f, -9.8f), new Vector3(-11.0f, 0f, 8.4f),
                new Vector3(16.0f, 0f, 6.0f), new Vector3(-6.6f, 0f, -4.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var x = positions[i].x;
                var z = positions[i].z;
                float size = Random.Range(0.5f, 1.0f);
                AddMesh(_staticRoot, "Rock", MeshFactory.Rock(i), _rock,
                    new Vector3(x, TerrainShape.Height(x, z) + size * 0.18f, z),
                    new Vector3(size, size * 0.72f, size * Random.Range(0.85f, 1.15f)),
                    Random.Range(0f, 360f));
            }
        }

        private void BuildFlowers()
        {
            var petalColors = new[] { _flowerA, _flowerB, _flowerC, _flowerD };
            int count = GraphicsSettings.AmbientProps ? 30 : 14;

            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-16f, 16f);
                float z = Random.Range(-13f, 13f);
                if (Mathf.Abs(x) < 5.8f && Mathf.Abs(z) < 4.6f) continue;

                var flower = new GameObject("Flower");
                flower.transform.SetParent(_staticRoot, false);
                flower.transform.localPosition = new Vector3(x, TerrainShape.Height(x, z), z);

                AddCylinder(flower.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.028f, 0.13f, 0.028f), _leafA);

                var petals = petalColors[i % petalColors.Length];
                float radius = 0.07f;
                for (int p = 0; p < 4; p++)
                {
                    float a = p * Mathf.PI * 0.5f + Random.Range(-0.3f, 0.3f);
                    AddSphere(flower.transform,
                        new Vector3(Mathf.Cos(a) * radius, 0.28f, Mathf.Sin(a) * radius),
                        Vector3.one * 0.085f, petals);
                }
                AddSphere(flower.transform, new Vector3(0f, 0.29f, 0f), Vector3.one * 0.05f, _flowerB);
            }
        }

        private void BuildGrassTufts()
        {
            if (!GraphicsSettings.GrassTufts) return;

            var tuftMaterial = MaterialCache.TuftMaterial;
            var tuftRoot = new GameObject("Grass").transform;
            tuftRoot.SetParent(_staticRoot, false);

            for (int i = 0; i < 110; i++)
            {
                float x = Random.Range(-17f, 17f);
                float z = Random.Range(-14f, 14f);
                if (Mathf.Abs(x) < 5.6f && Mathf.Abs(z) < 4.4f) continue;

                var tuft = new GameObject("Tuft");
                tuft.transform.SetParent(tuftRoot, false);
                tuft.transform.localPosition = new Vector3(x, TerrainShape.Height(x, z), z);
                tuft.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.3f, 0.62f);
                tuft.transform.localScale = new Vector3(scale, scale * Random.Range(0.8f, 1.3f), scale);

                tuft.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Tuft;
                var renderer = tuft.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = tuftMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // ---------- farm dressing ----------

        private void BuildFence()
        {
            var fence = new GameObject("Fence").transform;
            fence.SetParent(_staticRoot, false);

            float halfWidth = FarmGrid.FieldWidth * 0.5f + 1.15f;
            float halfDepth = FarmGrid.FieldDepth * 0.5f + 1.15f;

            for (float x = -halfWidth; x <= halfWidth + 0.01f; x += 1.15f)
            {
                AddPost(fence, new Vector3(x, 0f, -halfDepth));
                if (Mathf.Abs(x) > 1.6f)
                    AddPost(fence, new Vector3(x, 0f, halfDepth));
            }
            for (float z = -halfDepth; z <= halfDepth + 0.01f; z += 1.15f)
            {
                AddPost(fence, new Vector3(-halfWidth, 0f, z));
                AddPost(fence, new Vector3(halfWidth, 0f, z));
            }

            AddBox(fence, new Vector3(0f, 0.52f, -halfDepth), new Vector3(halfWidth * 2f, 0.08f, 0.07f), _wood);
            AddBox(fence, new Vector3(0f, 0.3f, -halfDepth), new Vector3(halfWidth * 2f, 0.07f, 0.06f), _wood);
            AddBox(fence, new Vector3(-halfWidth, 0.52f, 0f), new Vector3(0.07f, 0.08f, halfDepth * 2f), _wood);
            AddBox(fence, new Vector3(halfWidth, 0.52f, 0f), new Vector3(0.07f, 0.08f, halfDepth * 2f), _wood);

            AddBox(fence, new Vector3(-1.6f, 0.42f, halfDepth), new Vector3(0.18f, 0.84f, 0.18f), _woodDark);
            AddBox(fence, new Vector3(1.6f, 0.42f, halfDepth), new Vector3(0.18f, 0.84f, 0.18f), _woodDark);
        }

        private void AddPost(Transform parent, Vector3 position)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(position.x, 0.34f, position.z);
            post.transform.localScale = new Vector3(0.13f, 0.68f, 0.13f);
            post.transform.localRotation = Quaternion.Euler(0f, Random.Range(-6f, 6f), 0f);
            post.GetComponent<Renderer>().sharedMaterial = _woodDark;
            Object.Destroy(post.GetComponent<Collider>());
        }

        private void BuildScarecrow(Vector3 position)
        {
            var scarecrow = new GameObject("Scarecrow");
            scarecrow.transform.SetParent(_staticRoot, false);
            scarecrow.transform.localPosition = new Vector3(
                position.x, TerrainShape.Height(position.x, position.z), position.z);
            scarecrow.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);

            AddContactShadow(scarecrow.transform, new Vector3(0f, 0.02f, 0f), 1.5f);

            AddBox(scarecrow.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.12f, 1.64f, 0.12f), _woodDark);
            AddBox(scarecrow.transform, new Vector3(0f, 1.24f, 0f), new Vector3(1.35f, 0.11f, 0.11f), _woodDark);
            AddBox(scarecrow.transform, new Vector3(0f, 1.12f, 0.03f), new Vector3(0.78f, 0.8f, 0.26f), _flowerC);
            AddSphere(scarecrow.transform, new Vector3(0f, 1.68f, 0f), Vector3.one * 0.36f, _straw);
            AddBox(scarecrow.transform, new Vector3(0f, 1.88f, 0f), new Vector3(0.9f, 0.06f, 0.9f), _straw);
            AddBox(scarecrow.transform, new Vector3(0f, 1.99f, 0f), new Vector3(0.42f, 0.24f, 0.42f), _straw);
        }

        private void BuildSign(Vector3 position)
        {
            var sign = new GameObject("Sign");
            sign.transform.SetParent(_staticRoot, false);
            sign.transform.localPosition = new Vector3(
                position.x, TerrainShape.Height(position.x, position.z), position.z);
            sign.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);

            AddBox(sign.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.12f), _woodDark);
            AddBox(sign.transform, new Vector3(0f, 0.98f, 0f), new Vector3(1.2f, 0.55f, 0.1f), _wood);
            AddBox(sign.transform, new Vector3(0f, 0.98f, 0.06f), new Vector3(1.0f, 0.36f, 0.03f), _walls);
        }

        private void BuildCrates()
        {
            AddCrate(new Vector3(7.9f, 0f, 3.9f), 0.6f);
            AddCrate(new Vector3(8.45f, 0f, 4.4f), 0.5f);
            AddCrate(new Vector3(8.15f, 0.55f, 4.15f), 0.45f);
        }

        private void AddCrate(Vector3 position, float size)
        {
            float ground = TerrainShape.Height(position.x, position.z);
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.SetParent(_staticRoot, false);
            crate.transform.localPosition = new Vector3(position.x, ground + position.y + size * 0.5f, position.z);
            crate.transform.localScale = Vector3.one * size;
            crate.transform.localRotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
            crate.GetComponent<Renderer>().sharedMaterial = _wood;
            Object.Destroy(crate.GetComponent<Collider>());
        }

        private void BuildClouds()
        {
            if (!GraphicsSettings.Clouds) return;

            for (int c = 0; c < 6; c++)
            {
                var cloud = new GameObject("Cloud");
                cloud.transform.SetParent(_movingRoot, false);
                cloud.transform.localPosition = new Vector3(
                    Random.Range(-28f, 28f), Random.Range(15f, 21f), Random.Range(-26f, 20f));

                var drifter = cloud.AddComponent<Drifter>();
                drifter.Velocity = new Vector3(Random.Range(0.3f, 0.7f), 0f, 0f);
                drifter.WrapDistance = 32f;

                for (int p = 0; p < 4; p++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = "Puff";
                    puff.transform.SetParent(cloud.transform, false);
                    puff.transform.localPosition = new Vector3(
                        (p - 1.5f) * Random.Range(1.3f, 1.9f), Random.Range(-0.25f, 0.3f), Random.Range(-0.7f, 0.7f));
                    float size = Random.Range(1.9f, 3.2f);
                    puff.transform.localScale = new Vector3(size, size * 0.5f, size * 0.8f);
                    var renderer = puff.GetComponent<Renderer>();
                    renderer.sharedMaterial = _cloud;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    Object.Destroy(puff.GetComponent<Collider>());
                }
            }
        }

        // ---------- helpers ----------

        private void AddContactShadow(Transform parent, Vector3 localPosition, float diameter)
        {
            var blob = new GameObject("ContactShadow");
            blob.transform.SetParent(parent, false);
            blob.transform.localPosition = localPosition;
            blob.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            blob.transform.localScale = new Vector3(diameter, 1f, diameter);
            blob.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Disc;
            var renderer = blob.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _shadow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void AddTriOriented(List<int> tris, IList<Vector3> verts, int a, int b, int c, Vector3 inside)
        {
            Vector3 normal = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (Vector3.Dot(normal, verts[a] - inside) < 0f)
            {
                int swap = b;
                b = c;
                c = swap;
            }
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Material material,
            Vector3 localPosition, Vector3 scale, float yRotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void AddBox(Transform parent, Vector3 localPosition, Vector3 scale, Material material,
            float yRotation = 0f)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            box.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(box.GetComponent<Collider>());
        }

        private static void AddSphere(Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = scale;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(sphere.GetComponent<Collider>());
        }

        private static void AddCylinder(Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(cylinder.GetComponent<Collider>());
        }
    }
}
