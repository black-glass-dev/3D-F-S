using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Builds the whole world out of procedural low-poly meshes: rolling hills,
    /// trees, a farmhouse, a barn, a windmill, a pond, grass tufts, clouds.
    ///
    /// Everything static is parented under one root so GameBootstrap can run
    /// StaticBatchingUtility.Combine() on it — hundreds of props collapse into
    /// a handful of draw calls, which is how the game stays fast on phones.
    /// Moving props (windmill blades, clouds) live under a second root.
    /// </summary>
    public class EnvironmentBuilder
    {
        private Transform _staticRoot;
        private Transform _movingRoot;

        private Material _grass;
        private Material _grassDark;
        private Material _hill;
        private Material _path;
        private Material _soil;
        private Material _wood;
        private Material _woodDark;
        private Material _stone;
        private Material _rockMat;
        private Material _trunk;
        private Material _leafA;
        private Material _leafB;
        private Material _leafC;
        private Material _water;
        private Material _white;
        private Material _glass;
        private Material _red;
        private Material _roofRed;
        private Material _roofBrown;
        private Material _cloud;
        private Material _flowerA;
        private Material _flowerB;
        private Material _flowerC;
        private Material _flowerD;
        private Material _straw;

        public Transform Build()
        {
            _staticRoot = new GameObject("Environment").transform;
            _movingRoot = new GameObject("EnvironmentMoving").transform;

            InitMaterials();

            BuildGround();
            BuildHills();
            BuildPath();
            BuildHouse(new Vector3(-9.6f, 0f, 6.6f));
            BuildBarn(new Vector3(10.4f, 0f, 6.2f));
            BuildWindmill(new Vector3(13.2f, 0f, -4.6f));
            BuildPond(new Vector3(-12.4f, 0f, -5.6f));
            BuildTrees();
            BuildBushes();
            BuildRocks();
            BuildFlowers();
            BuildGrassTufts();
            BuildFence();
            BuildScarecrow(new Vector3(-4.8f, 0f, 4.4f));
            BuildSign(new Vector3(4.1f, 0f, 4.6f));
            BuildCrates();
            BuildClouds();

            return _staticRoot;
        }

        private void InitMaterials()
        {
            _grass = MaterialCache.Get(new Color(0.47f, 0.71f, 0.33f), SurfacePattern.Grass);
            _grass.mainTextureScale = new Vector2(20f, 20f);
            _grassDark = MaterialCache.Get(new Color(0.40f, 0.64f, 0.29f), SurfacePattern.GrassDark);
            _hill = MaterialCache.Get(new Color(0.44f, 0.60f, 0.40f), SurfacePattern.GrassDark);
            _path = MaterialCache.Get(new Color(0.74f, 0.70f, 0.62f), SurfacePattern.Stone);
            _soil = MaterialCache.Get(new Color(0.50f, 0.33f, 0.19f), SurfacePattern.Soil);
            _wood = MaterialCache.Get(new Color(0.68f, 0.50f, 0.28f), SurfacePattern.Wood);
            _woodDark = MaterialCache.Get(new Color(0.44f, 0.30f, 0.16f), SurfacePattern.Wood);
            _stone = MaterialCache.Get(new Color(0.72f, 0.70f, 0.66f), SurfacePattern.Stone);
            _rockMat = MaterialCache.Get(new Color(0.58f, 0.57f, 0.53f), SurfacePattern.Stone);
            _trunk = MaterialCache.Get(new Color(0.44f, 0.30f, 0.18f), SurfacePattern.Wood);
            _leafA = MaterialCache.Get(new Color(0.28f, 0.60f, 0.28f), SurfacePattern.Leaf);
            _leafB = MaterialCache.Get(new Color(0.36f, 0.70f, 0.32f), SurfacePattern.Leaf);
            _leafC = MaterialCache.Get(new Color(0.20f, 0.48f, 0.26f), SurfacePattern.Leaf);
            _water = MaterialCache.Get(new Color(0.30f, 0.60f, 0.83f), SurfacePattern.Water);
            _white = MaterialCache.Get(new Color(0.96f, 0.94f, 0.86f), SurfacePattern.Wood);
            _glass = MaterialCache.Get(new Color(0.55f, 0.76f, 0.90f), SurfacePattern.Smooth);
            _red = MaterialCache.Get(new Color(0.72f, 0.27f, 0.22f), SurfacePattern.Wood);
            _roofRed = MaterialCache.Get(new Color(0.76f, 0.34f, 0.24f), SurfacePattern.Wood);
            _roofBrown = MaterialCache.Get(new Color(0.38f, 0.26f, 0.20f), SurfacePattern.Stone);
            _cloud = MaterialCache.Get(new Color(0.98f, 0.99f, 1f), SurfacePattern.Smooth);
            _flowerA = MaterialCache.Get(new Color(0.95f, 0.45f, 0.55f));
            _flowerB = MaterialCache.Get(new Color(0.99f, 0.83f, 0.34f));
            _flowerC = MaterialCache.Get(new Color(0.76f, 0.56f, 0.95f));
            _flowerD = MaterialCache.Get(new Color(0.98f, 0.97f, 0.94f));
            _straw = MaterialCache.Get(new Color(0.93f, 0.80f, 0.42f), SurfacePattern.Leaf);
        }

        // ---------- ground & horizon ----------

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_staticRoot, false);
            ground.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = _grass;
            Object.Destroy(ground.GetComponent<Collider>());

            // soft colour break-up patches
            for (int i = 0; i < 16; i++)
            {
                float x = Random.Range(-14f, 14f);
                float z = Random.Range(-12f, 12f);
                if (Mathf.Abs(x) < 5.8f && Mathf.Abs(z) < 4.4f) x += 9f;

                var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = "GrassPatch";
                patch.transform.SetParent(_staticRoot, false);
                patch.transform.localPosition = new Vector3(x, 0.001f, z);
                float size = Random.Range(2.4f, 5.2f);
                patch.transform.localScale = new Vector3(size, 0.01f, size * Random.Range(0.6f, 1.2f));
                patch.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                patch.GetComponent<Renderer>().sharedMaterial = _grassDark;
                Object.Destroy(patch.GetComponent<Collider>());
            }
        }

        private void BuildHills()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f + 0.2f;
                float distance = 26f + (float)rng.NextDouble() * 16f;
                var position = new Vector3(Mathf.Cos(angle) * distance, -0.6f, Mathf.Sin(angle) * distance);

                var hill = new GameObject("Hill");
                hill.transform.SetParent(_staticRoot, false);
                hill.transform.localPosition = position;
                float radius = 8f + (float)rng.NextDouble() * 10f;
                float height = 3.5f + (float)rng.NextDouble() * 5f;
                hill.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
                hill.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                var filter = hill.AddComponent<MeshFilter>();
                filter.sharedMesh = MeshFactory.Cone;
                var renderer = hill.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _hill;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void BuildPath()
        {
            for (int i = 0; i < 6; i++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "PathTile";
                tile.transform.SetParent(_staticRoot, false);
                tile.transform.localPosition = new Vector3(-7.4f + i * 0.9f, 0.004f, 4.6f + i * 0.55f);
                tile.transform.localScale = new Vector3(1.0f, 0.02f, 1.15f);
                tile.transform.localRotation = Quaternion.Euler(0f, -32f + Random.Range(-5f, 5f), 0f);
                tile.GetComponent<Renderer>().sharedMaterial = _path;
                Object.Destroy(tile.GetComponent<Collider>());
            }

            // small stepping stones towards the field gate
            for (int i = 0; i < 5; i++)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "Step";
                stone.transform.SetParent(_staticRoot, false);
                stone.transform.localPosition = new Vector3(-4.2f + i * 1.15f, 0.004f, 4.4f - i * 0.25f);
                stone.transform.localScale = new Vector3(0.8f, 0.02f, 0.7f);
                stone.transform.localRotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
                stone.GetComponent<Renderer>().sharedMaterial = _stone;
                Object.Destroy(stone.GetComponent<Collider>());
            }
        }

        // ---------- buildings ----------

        private void BuildHouse(Vector3 position)
        {
            var house = new GameObject("House");
            house.transform.SetParent(_staticRoot, false);
            house.transform.localPosition = position;
            house.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);

            AddBox(house.transform, new Vector3(0f, 0.14f, 0f), new Vector3(3.7f, 0.28f, 2.95f), _stone);
            AddBox(house.transform, new Vector3(0f, 1.25f, 0f), new Vector3(3.4f, 1.95f, 2.7f), _white);
            AddBox(house.transform, new Vector3(0f, 2.36f, 0f), new Vector3(3.95f, 0.34f, 3.2f), _roofRed);
            AddBox(house.transform, new Vector3(0f, 2.62f, 0f), new Vector3(3.1f, 0.28f, 2.5f), _roofRed);
            AddBox(house.transform, new Vector3(0f, 2.82f, 0f), new Vector3(1.9f, 0.2f, 1.5f), _roofRed);

            // door + frame
            AddBox(house.transform, new Vector3(0f, 0.75f, 1.37f), new Vector3(0.86f, 1.34f, 0.06f), _woodDark);
            AddBox(house.transform, new Vector3(0f, 0.7f, 1.40f), new Vector3(0.7f, 1.2f, 0.05f), _wood);

            // windows
            AddWindow(house.transform, new Vector3(-1.1f, 1.35f, 1.37f));
            AddWindow(house.transform, new Vector3(1.1f, 1.35f, 1.37f));

            // chimney
            AddBox(house.transform, new Vector3(1.15f, 2.95f, -0.55f), new Vector3(0.4f, 1.0f, 0.4f), _stone);
            AddBox(house.transform, new Vector3(1.15f, 3.47f, -0.55f), new Vector3(0.5f, 0.1f, 0.5f), _rockMat);

            // porch step
            AddBox(house.transform, new Vector3(0f, 0.1f, 1.62f), new Vector3(1.5f, 0.16f, 0.5f), _stone);

            // flower boxes
            AddBox(house.transform, new Vector3(-1.1f, 0.95f, 1.44f), new Vector3(0.7f, 0.16f, 0.2f), _woodDark);
            AddBox(house.transform, new Vector3(1.1f, 0.95f, 1.44f), new Vector3(0.7f, 0.16f, 0.2f), _woodDark);
            AddSphere(house.transform, new Vector3(-1.25f, 1.06f, 1.46f), Vector3.one * 0.13f, _flowerA);
            AddSphere(house.transform, new Vector3(-0.95f, 1.06f, 1.46f), Vector3.one * 0.13f, _flowerC);
            AddSphere(house.transform, new Vector3(1.25f, 1.06f, 1.46f), Vector3.one * 0.13f, _flowerB);
            AddSphere(house.transform, new Vector3(0.95f, 1.06f, 1.46f), Vector3.one * 0.13f, _flowerA);
        }

        private void AddWindow(Transform parent, Vector3 localPosition)
        {
            AddBox(parent, localPosition, new Vector3(0.7f, 0.7f, 0.05f), _woodDark);
            AddBox(parent, localPosition + new Vector3(0f, 0f, 0.02f), new Vector3(0.56f, 0.56f, 0.04f), _glass);
            AddBox(parent, localPosition + new Vector3(0f, 0f, 0.04f), new Vector3(0.06f, 0.56f, 0.03f), _wood);
        }

        private void BuildBarn(Vector3 position)
        {
            var barn = new GameObject("Barn");
            barn.transform.SetParent(_staticRoot, false);
            barn.transform.localPosition = position;
            barn.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);

            AddBox(barn.transform, new Vector3(0f, 1.15f, 0f), new Vector3(3.1f, 2.0f, 2.6f), _red);
            AddBox(barn.transform, new Vector3(0f, 2.3f, 0f), new Vector3(3.6f, 0.36f, 3.0f), _roofBrown);
            AddBox(barn.transform, new Vector3(0f, 2.56f, 0f), new Vector3(2.5f, 0.26f, 2.2f), _roofBrown);

            // big white doors with a cross
            AddBox(barn.transform, new Vector3(0f, 0.95f, 1.33f), new Vector3(1.5f, 1.7f, 0.06f), _white);
            AddBox(barn.transform, new Vector3(0f, 0.95f, 1.36f), new Vector3(0.1f, 1.7f, 0.05f), _red);
            AddBox(barn.transform, new Vector3(0f, 1.35f, 1.36f), new Vector3(1.5f, 0.1f, 0.05f), _red);
            AddBox(barn.transform, new Vector3(0f, 1.75f, 1.36f), new Vector3(1.5f, 0.1f, 0.05f), _red);

            // hay bales
            for (int i = 0; i < 3; i++)
            {
                var bale = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bale.name = "HayBale";
                bale.transform.SetParent(_staticRoot, false);
                bale.transform.localPosition = position + new Vector3(2.2f + i * 0.3f, 0.34f, -0.6f + i * 0.75f);
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
            mill.transform.localPosition = position;
            mill.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);

            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.SetParent(mill.transform, false);
            tower.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            tower.transform.localScale = new Vector3(1.7f, 1.9f, 1.7f);
            tower.GetComponent<Renderer>().sharedMaterial = _white;
            Object.Destroy(tower.GetComponent<Collider>());

            var cap = new GameObject("Cap");
            cap.transform.SetParent(mill.transform, false);
            cap.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            cap.transform.localScale = new Vector3(2.2f, 1.1f, 2.2f);
            cap.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Cone;
            cap.AddComponent<MeshRenderer>().sharedMaterial = _roofRed;

            // rotating blades live under the moving root
            var hub = new GameObject("Blades");
            hub.transform.SetParent(_movingRoot, false);
            hub.transform.position = position + new Vector3(0f, 3.35f, 0f);
            // Blades face the camera (which looks in from ~40 degrees).
            hub.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            hub.AddComponent<Spinner>().Axis = Vector3.forward;
            hub.GetComponent<Spinner>().DegreesPerSecond = 22f;

            for (int i = 0; i < 4; i++)
            {
                var arm = new GameObject("Blade" + i);
                arm.transform.SetParent(hub.transform, false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);

                var spar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spar.transform.SetParent(arm.transform, false);
                spar.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                spar.transform.localScale = new Vector3(0.08f, 1.7f, 0.06f);
                spar.GetComponent<Renderer>().sharedMaterial = _wood;
                Object.Destroy(spar.GetComponent<Collider>());

                var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sail.transform.SetParent(arm.transform, false);
                sail.transform.localPosition = new Vector3(0.22f, 1.05f, 0f);
                sail.transform.localScale = new Vector3(0.36f, 1.1f, 0.03f);
                sail.GetComponent<Renderer>().sharedMaterial = _white;
                Object.Destroy(sail.GetComponent<Collider>());
            }

            var hubSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hubSphere.transform.SetParent(hub.transform, false);
            hubSphere.transform.localScale = Vector3.one * 0.26f;
            hubSphere.GetComponent<Renderer>().sharedMaterial = _woodDark;
            Object.Destroy(hubSphere.GetComponent<Collider>());
        }

        private void BuildPond(Vector3 position)
        {
            var pond = new GameObject("Pond");
            pond.transform.SetParent(_staticRoot, false);
            pond.transform.localPosition = position;

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pond.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            rim.transform.localScale = new Vector3(4.6f, 0.02f, 3.3f);
            rim.GetComponent<Renderer>().sharedMaterial = _path;
            Object.Destroy(rim.GetComponent<Collider>());

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "PondWater";
            water.transform.SetParent(pond.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            water.transform.localScale = new Vector3(3.9f, 0.015f, 2.7f);
            // Unique material copy: only the pond scrolls its texture.
            var waterMaterial = new Material(_water) { name = "FarmPondWater" };
            var waterRenderer = water.GetComponent<Renderer>();
            waterRenderer.sharedMaterial = waterMaterial;
            Object.Destroy(water.GetComponent<Collider>());
            water.AddComponent<WaterFlow>().Surface = waterMaterial;

            // lily pads
            for (int i = 0; i < 4; i++)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "LilyPad";
                pad.transform.SetParent(pond.transform, false);
                pad.transform.localPosition = new Vector3(
                    Random.Range(-1.2f, 1.2f), 0.05f, Random.Range(-0.8f, 0.8f));
                pad.transform.localScale = new Vector3(0.45f, 0.008f, 0.45f);
                pad.GetComponent<Renderer>().sharedMaterial = _leafB;
                Object.Destroy(pad.GetComponent<Collider>());
            }

            // reeds
            for (int i = 0; i < 6; i++)
            {
                var reed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reed.name = "Reed";
                reed.transform.SetParent(pond.transform, false);
                float angle = Random.Range(0f, Mathf.PI * 2f);
                reed.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 1.9f, 0.3f, Mathf.Sin(angle) * 1.3f);
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
            BuildOak(new Vector3(-12.6f, 0f, -0.6f), 1.25f);
            BuildPine(new Vector3(7.6f, 0f, -7.2f), 1.05f);
            BuildOak(new Vector3(-8.2f, 0f, -8.2f), 0.9f);
            BuildPine(new Vector3(0.6f, 0f, 10.2f), 1.1f);
            BuildPoplar(new Vector3(-3.2f, 0f, -10.4f), 1.0f);
            BuildOak(new Vector3(14.2f, 0f, 4.2f), 1.0f);
            BuildPoplar(new Vector3(-14.4f, 0f, 4.6f), 0.95f);
            BuildOak(new Vector3(3.4f, 0f, 8.2f), 0.8f);
        }

        private void BuildOak(Vector3 position, float scale)
        {
            var tree = new GameObject("Oak");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddCylinder(tree.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.34f, 0.8f, 0.34f), _trunk);
            var leaf = Random.value > 0.5f ? _leafA : _leafB;
            AddSphere(tree.transform, new Vector3(0f, 2.0f, 0f), new Vector3(1.9f, 1.7f, 1.9f), leaf);
            AddSphere(tree.transform, new Vector3(0.55f, 2.6f, 0.15f), new Vector3(1.15f, 1.05f, 1.15f), _leafB);
            AddSphere(tree.transform, new Vector3(-0.5f, 2.45f, -0.25f), new Vector3(1.0f, 0.95f, 1.0f), _leafC);
        }

        private void BuildPine(Vector3 position, float scale)
        {
            var tree = new GameObject("Pine");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddCylinder(tree.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.28f, 0.6f, 0.28f), _trunk);
            AddCone(tree.transform, new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 1.6f, 2.4f), _leafC);
            AddCone(tree.transform, new Vector3(0f, 1.9f, 0f), new Vector3(1.9f, 1.5f, 1.9f), _leafA);
            AddCone(tree.transform, new Vector3(0f, 2.8f, 0f), new Vector3(1.3f, 1.3f, 1.3f), _leafB);
        }

        private void BuildPoplar(Vector3 position, float scale)
        {
            var tree = new GameObject("Poplar");
            tree.transform.SetParent(_staticRoot, false);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            AddCylinder(tree.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.26f, 0.9f, 0.26f), _trunk);
            AddSphere(tree.transform, new Vector3(0f, 2.45f, 0f), new Vector3(1.15f, 1.85f, 1.15f), _leafA);
            AddSphere(tree.transform, new Vector3(0.12f, 3.35f, 0f), new Vector3(0.8f, 1.0f, 0.8f), _leafB);
        }

        private void BuildBushes()
        {
            var positions = new[]
            {
                new Vector3(-6.2f, 0f, 7.6f), new Vector3(5.6f, 0f, 8.8f),
                new Vector3(-10.2f, 0f, 2.2f), new Vector3(12.4f, 0f, -2.6f),
                new Vector3(-3.4f, 0f, -8.8f), new Vector3(4.6f, 0f, -9.4f),
                new Vector3(15.2f, 0f, -0.4f), new Vector3(-15.4f, 0f, -2.2f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var bush = new GameObject("Bush");
                bush.transform.SetParent(_staticRoot, false);
                bush.transform.localPosition = positions[i];
                bush.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.15f);
                bush.transform.localScale = Vector3.one * scale;

                AddSphere(bush.transform, new Vector3(0f, 0.36f, 0f), new Vector3(1.1f, 0.78f, 1.1f), _leafA);
                AddSphere(bush.transform, new Vector3(0.36f, 0.30f, 0.22f), new Vector3(0.7f, 0.55f, 0.7f), _leafB);
                AddSphere(bush.transform, new Vector3(-0.2f, 0.5f, -0.18f), Vector3.one * 0.12f, _flowerA);
                AddSphere(bush.transform, new Vector3(0.24f, 0.52f, -0.1f), Vector3.one * 0.11f, _flowerA);
            }
        }

        private void BuildRocks()
        {
            var positions = new[]
            {
                new Vector3(-8.8f, 0f, -8.6f), new Vector3(6.4f, 0f, 3.4f),
                new Vector3(12.6f, 0f, 3.8f), new Vector3(-13.2f, 0f, 2.8f),
                new Vector3(2.4f, 0f, -9.6f), new Vector3(-11.0f, 0f, 8.2f),
                new Vector3(16.0f, 0f, 6.0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock";
                rock.transform.SetParent(_staticRoot, false);
                float size = Random.Range(0.38f, 0.8f);
                rock.transform.localPosition = positions[i] + new Vector3(0f, size * 0.32f, 0f);
                rock.transform.localScale = new Vector3(size, size * 0.62f, size * Random.Range(0.8f, 1.2f));
                rock.transform.localRotation = Quaternion.Euler(
                    Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
                rock.GetComponent<Renderer>().sharedMaterial = _rockMat;
                Object.Destroy(rock.GetComponent<Collider>());
            }
        }

        private void BuildFlowers()
        {
            var petalColors = new[] { _flowerA, _flowerB, _flowerC, _flowerD };

            int flowerCount = GraphicsSettings.AmbientProps ? 26 : 12;
            for (int i = 0; i < flowerCount; i++)
            {
                float x = Random.Range(-15f, 15f);
                float z = Random.Range(-12f, 12f);
                if (Mathf.Abs(x) < 5.8f && Mathf.Abs(z) < 4.4f) continue;

                var flower = new GameObject("Flower");
                flower.transform.SetParent(_staticRoot, false);
                flower.transform.localPosition = new Vector3(x, 0f, z);

                AddCylinder(flower.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.028f, 0.13f, 0.028f), _leafA);

                var petals = petalColors[i % petalColors.Length];
                float radius = 0.075f;
                for (int p = 0; p < 4; p++)
                {
                    float a = p * Mathf.PI * 0.5f + Random.Range(-0.3f, 0.3f);
                    AddSphere(flower.transform,
                        new Vector3(Mathf.Cos(a) * radius, 0.29f, Mathf.Sin(a) * radius),
                        Vector3.one * 0.09f, petals);
                }
                AddSphere(flower.transform, new Vector3(0f, 0.30f, 0f), Vector3.one * 0.055f, _flowerB);
            }
        }

        private void BuildGrassTufts()
        {
            if (!GraphicsSettings.GrassTufts) return;

            var tuftMaterial = MaterialCache.TuftMaterial;
            var tuftRoot = new GameObject("Grass").transform;
            tuftRoot.SetParent(_staticRoot, false);

            for (int i = 0; i < 90; i++)
            {
                float x = Random.Range(-16f, 16f);
                float z = Random.Range(-13f, 13f);
                if (Mathf.Abs(x) < 5.6f && Mathf.Abs(z) < 4.3f) continue;

                var tuft = new GameObject("Tuft");
                tuft.transform.SetParent(tuftRoot, false);
                tuft.transform.localPosition = new Vector3(x, 0f, z);
                tuft.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.28f, 0.6f);
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

            // gate posts at the front opening
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
            scarecrow.transform.localPosition = position;
            scarecrow.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);

            AddBox(scarecrow.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.12f, 1.6f, 0.12f), _woodDark);
            AddBox(scarecrow.transform, new Vector3(0f, 1.22f, 0f), new Vector3(1.35f, 0.11f, 0.11f), _woodDark);
            AddBox(scarecrow.transform, new Vector3(0f, 1.1f, 0.03f), new Vector3(0.76f, 0.78f, 0.26f), _flowerC);
            AddSphere(scarecrow.transform, new Vector3(0f, 1.66f, 0f), Vector3.one * 0.35f, _straw);
            AddBox(scarecrow.transform, new Vector3(0f, 1.85f, 0f), new Vector3(0.86f, 0.06f, 0.86f), _straw);
            AddBox(scarecrow.transform, new Vector3(0f, 1.96f, 0f), new Vector3(0.4f, 0.24f, 0.4f), _straw);
        }

        private void BuildSign(Vector3 position)
        {
            var sign = new GameObject("Sign");
            sign.transform.SetParent(_staticRoot, false);
            sign.transform.localPosition = position;
            sign.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);

            AddBox(sign.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.12f), _woodDark);
            AddBox(sign.transform, new Vector3(0f, 0.98f, 0f), new Vector3(1.2f, 0.55f, 0.1f), _wood);
            AddBox(sign.transform, new Vector3(0f, 0.98f, 0.06f), new Vector3(1.0f, 0.36f, 0.03f), _white);
        }

        private void BuildCrates()
        {
            AddCrate(new Vector3(8.1f, 0f, 3.9f), 0.6f);
            AddCrate(new Vector3(8.6f, 0f, 4.4f), 0.5f);
            AddCrate(new Vector3(8.35f, 0.55f, 4.15f), 0.45f);
        }

        private void AddCrate(Vector3 position, float size)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.SetParent(_staticRoot, false);
            crate.transform.localPosition = position + new Vector3(0f, size * 0.5f, 0f);
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
                    Random.Range(-26f, 26f), Random.Range(12f, 17f), Random.Range(-24f, 18f));

                var drifter = cloud.AddComponent<Drifter>();
                drifter.Velocity = new Vector3(Random.Range(0.35f, 0.8f), 0f, 0f);
                drifter.WrapDistance = 30f;

                for (int p = 0; p < 4; p++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = "Puff";
                    puff.transform.SetParent(cloud.transform, false);
                    puff.transform.localPosition = new Vector3(
                        (p - 1.5f) * Random.Range(1.1f, 1.6f), Random.Range(-0.2f, 0.3f), Random.Range(-0.6f, 0.6f));
                    float size = Random.Range(1.6f, 2.8f);
                    puff.transform.localScale = new Vector3(size, size * 0.55f, size * 0.8f);
                    var renderer = puff.GetComponent<Renderer>();
                    renderer.sharedMaterial = _cloud;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    Object.Destroy(puff.GetComponent<Collider>());
                }
            }
        }

        // ---------- primitive helpers ----------

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

        private static void AddCone(Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var cone = new GameObject("Cone");
            cone.transform.SetParent(parent, false);
            cone.transform.localPosition = localPosition;
            cone.transform.localScale = scale;
            cone.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Cone;
            cone.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
