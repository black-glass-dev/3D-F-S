using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Builds a small low-poly plant out of primitives for a given crop and
    /// animates it through its growth stages. No meshes are imported, so the
    /// look stays consistent and the build stays tiny.
    /// </summary>
    public class CropVisual
    {
        public GameObject Root { get; private set; }

        private readonly List<Renderer> _leaves = new List<Renderer>();
        private readonly List<Transform> _fruits = new List<Transform>();
        private readonly List<Renderer> _fruitRenderers = new List<Renderer>();
        private readonly List<Vector3> _fruitBaseScales = new List<Vector3>();
        private readonly int _cropIndex;

        private CropVisual(int cropIndex, GameObject root)
        {
            _cropIndex = cropIndex;
            Root = root;
        }

        public static CropVisual Build(int cropIndex, Transform parent)
        {
            var root = new GameObject("CropVisual");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var visual = new CropVisual(cropIndex, root);
            CropDatabase.Get(cropIndex); // keep index validation in one place

            switch (cropIndex)
            {
                case 1:
                    visual.BuildTomato();
                    break;
                case 2:
                    visual.BuildPumpkin();
                    break;
                case 3:
                    visual.BuildCarrot();
                    break;
                case 4:
                    visual.BuildChili();
                    break;
                case 5:
                    visual.BuildCorn();
                    break;
                case 6:
                    visual.BuildRice();
                    break;
                case 7:
                    visual.BuildJute();
                    break;
                default:
                    visual.BuildWheat();
                    break;
            }
            return visual;
        }

        // ---------- helpers ----------

        private Renderer AddLeaf(PrimitiveType type, Vector3 position, Vector3 scale, Color color,
            float yRotation = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Leaf";
            go.transform.SetParent(Root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Object.Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialCache.Get(color, SurfacePattern.Leaf);
            _leaves.Add(renderer);
            return renderer;
        }

        private void AddFruit(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Fruit";
            go.transform.SetParent(Root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialCache.Get(color);
            _fruits.Add(go.transform);
            _fruitRenderers.Add(renderer);
            _fruitBaseScales.Add(scale);
        }

        // ---------- crop shapes ----------

        private void BuildWheat()
        {
            var green = new Color(0.50f, 0.62f, 0.32f);
            var greenDark = new Color(0.36f, 0.48f, 0.26f);

            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                float radius = i == 0 ? 0f : 0.16f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(Root.transform, false);
                stem.transform.localPosition = pos + Vector3.up * 0.28f;
                stem.transform.localScale = new Vector3(0.035f, 0.28f, 0.035f);
                stem.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
                Object.Destroy(stem.GetComponent<Collider>());
                var stemRenderer = stem.GetComponent<Renderer>();
                stemRenderer.sharedMaterial = MaterialCache.Get(greenDark, SurfacePattern.Leaf);
                _leaves.Add(stemRenderer);

                AddFruit(PrimitiveType.Sphere, pos + Vector3.up * 0.58f,
                    new Vector3(0.09f, 0.16f, 0.09f), green);
            }
        }

        private void BuildTomato()
        {
            var leaf = new Color(0.30f, 0.42f, 0.25f);
            var leafLight = new Color(0.38f, 0.52f, 0.29f);

            AddLeaf(PrimitiveType.Sphere, Vector3.up * 0.26f, new Vector3(0.62f, 0.5f, 0.62f), leaf);
            AddLeaf(PrimitiveType.Sphere, new Vector3(0.1f, 0.44f, -0.05f), new Vector3(0.42f, 0.36f, 0.42f), leafLight);

            var offsets = new[]
            {
                new Vector3(0.26f, 0.24f, 0.12f),
                new Vector3(-0.22f, 0.30f, 0.18f),
                new Vector3(0.04f, 0.20f, -0.26f)
            };

            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            for (int i = 0; i < offsets.Length; i++)
                AddFruit(PrimitiveType.Sphere, offsets[i], Vector3.one * 0.17f, ripe);
        }

        private void BuildPumpkin()
        {
            var leaf = new Color(0.28f, 0.40f, 0.24f);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * 0.24f, 0.12f, Mathf.Sin(angle) * 0.24f);
                AddLeaf(PrimitiveType.Sphere, pos, new Vector3(0.34f, 0.16f, 0.34f), leaf);
            }

            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            AddFruit(PrimitiveType.Sphere, new Vector3(-0.12f, 0.16f, 0.06f), new Vector3(0.3f, 0.22f, 0.3f), ripe);
            AddFruit(PrimitiveType.Sphere, new Vector3(0.22f, 0.14f, -0.12f), new Vector3(0.26f, 0.19f, 0.26f), ripe);
        }

        private void BuildCarrot()
        {
            var leaf = new Color(0.34f, 0.52f, 0.28f);
            var leafDark = new Color(0.26f, 0.42f, 0.24f);

            // feathery fronds
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * 0.07f, 0.24f, Mathf.Sin(angle) * 0.07f);
                var frond = AddLeaf(PrimitiveType.Cube, pos, new Vector3(0.05f, 0.36f, 0.12f),
                    i % 2 == 0 ? leaf : leafDark, angle * Mathf.Rad2Deg);
                frond.transform.localRotation = Quaternion.Euler(Mathf.Cos(angle) * 18f, angle * Mathf.Rad2Deg,
                    Mathf.Sin(angle) * 18f);
            }

            // orange shoulders poking out of the soil
            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            AddFruit(PrimitiveType.Sphere, new Vector3(0.05f, 0.10f, 0.03f), new Vector3(0.17f, 0.14f, 0.17f), ripe);
            AddFruit(PrimitiveType.Sphere, new Vector3(-0.11f, 0.09f, -0.08f), new Vector3(0.14f, 0.12f, 0.14f), ripe);
        }

        private void BuildChili()
        {
            var leaf = new Color(0.28f, 0.40f, 0.24f);

            AddLeaf(PrimitiveType.Sphere, Vector3.up * 0.22f, new Vector3(0.52f, 0.42f, 0.52f), leaf);
            AddLeaf(PrimitiveType.Sphere, new Vector3(0.14f, 0.38f, 0.1f), new Vector3(0.34f, 0.3f, 0.34f), leaf);

            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            AddFruit(PrimitiveType.Sphere, new Vector3(0.18f, 0.32f, 0.16f), new Vector3(0.08f, 0.19f, 0.08f), ripe);
            AddFruit(PrimitiveType.Sphere, new Vector3(-0.16f, 0.28f, 0.12f), new Vector3(0.08f, 0.17f, 0.08f), ripe);
            AddFruit(PrimitiveType.Sphere, new Vector3(0.02f, 0.34f, -0.19f), new Vector3(0.08f, 0.18f, 0.08f), ripe);
        }

        private void BuildCorn()
        {
            var stalk = new Color(0.40f, 0.54f, 0.28f);
            var leaf = new Color(0.34f, 0.50f, 0.26f);

            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stalk";
            stem.transform.SetParent(Root.transform, false);
            stem.transform.localPosition = Vector3.up * 0.42f;
            stem.transform.localScale = new Vector3(0.06f, 0.42f, 0.06f);
            Object.Destroy(stem.GetComponent<Collider>());
            var stemRenderer = stem.GetComponent<Renderer>();
            stemRenderer.sharedMaterial = MaterialCache.Get(stalk, SurfacePattern.Leaf);
            _leaves.Add(stemRenderer);

            // long drooping leaves
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 20f;
                var rad = angle * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(rad) * 0.16f, 0.34f + i * 0.06f, Mathf.Sin(rad) * 0.16f);
                var leafRenderer = AddLeaf(PrimitiveType.Cube, pos, new Vector3(0.42f, 0.03f, 0.14f), leaf, angle);
                leafRenderer.transform.localRotation = Quaternion.Euler(0f, angle, -18f);
            }

            // two cobs
            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            AddFruit(PrimitiveType.Sphere, new Vector3(0.14f, 0.44f, 0.04f), new Vector3(0.11f, 0.22f, 0.11f), ripe);
            AddFruit(PrimitiveType.Sphere, new Vector3(-0.12f, 0.52f, -0.03f), new Vector3(0.1f, 0.2f, 0.1f), ripe);
        }

        private void BuildRice()
        {
            var green = new Color(0.44f, 0.56f, 0.32f);
            var greenDark = new Color(0.34f, 0.46f, 0.26f);

            for (int i = 0; i < 7; i++)
            {
                float angle = i * 51f * Mathf.Deg2Rad;
                float radius = 0.06f + (i % 3) * 0.05f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(Root.transform, false);
                stem.transform.localPosition = pos + Vector3.up * 0.34f;
                stem.transform.localScale = new Vector3(0.022f, 0.34f, 0.022f);
                stem.transform.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                Object.Destroy(stem.GetComponent<Collider>());
                var renderer = stem.GetComponent<Renderer>();
                renderer.sharedMaterial = MaterialCache.Get(greenDark, SurfacePattern.Leaf);
                _leaves.Add(renderer);

                AddFruit(PrimitiveType.Sphere, pos + Vector3.up * 0.70f,
                    new Vector3(0.05f, 0.13f, 0.05f), CropDatabase.Get(_cropIndex).RipeColor);
            }

            AddLeaf(PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), new Vector3(0.4f, 0.16f, 0.4f), green);
        }

        private void BuildJute()
        {
            var leaf = new Color(0.32f, 0.46f, 0.26f);
            var leafLight = new Color(0.40f, 0.54f, 0.30f);

            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * 0.1f, 0f, Mathf.Sin(angle) * 0.1f);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(Root.transform, false);
                stem.transform.localPosition = pos + Vector3.up * 0.4f;
                stem.transform.localScale = new Vector3(0.045f, 0.4f, 0.045f);
                stem.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                Object.Destroy(stem.GetComponent<Collider>());
                var renderer = stem.GetComponent<Renderer>();
                renderer.sharedMaterial = MaterialCache.Get(leaf, SurfacePattern.Leaf);
                _leaves.Add(renderer);

                // broad soft leaves
                AddLeaf(PrimitiveType.Sphere, pos + new Vector3(0.1f, 0.44f, 0.02f),
                    new Vector3(0.3f, 0.05f, 0.22f), leafLight, angle * Mathf.Rad2Deg);
                AddLeaf(PrimitiveType.Sphere, pos + new Vector3(-0.08f, 0.62f, -0.05f),
                    new Vector3(0.26f, 0.05f, 0.2f), leaf, angle * Mathf.Rad2Deg + 60f);
            }

            // the ripening stems turn pale green
            var ripe = CropDatabase.Get(_cropIndex).RipeColor;
            AddFruit(PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0f), new Vector3(0.14f, 0.12f, 0.14f), ripe);
        }

        // ---------- animation ----------

        /// <summary>0 = just planted, 1 = fully ripe.</summary>
        public void SetGrowth(float t, bool ripe)
        {
            if (Root == null) return;

            t = Mathf.Clamp01(t);
            float scale = Mathf.Lerp(0.28f, 1f, t);
            Root.transform.localScale = new Vector3(scale, scale, scale);

            var crop = CropDatabase.Get(_cropIndex);
            bool showFruit = t > 0.45f;
            float fruitT = Mathf.Clamp01((t - 0.45f) / 0.55f);

            for (int i = 0; i < _fruits.Count; i++)
            {
                var fruit = _fruits[i];
                if (fruit == null) continue;
                fruit.gameObject.SetActive(showFruit);
                if (!showFruit) continue;

                float s = Mathf.Lerp(0.35f, 1f, fruitT);
                fruit.localScale = _fruitBaseScales[i] * s;
                var renderer = _fruitRenderers[i];
                if (renderer != null)
                {
                    var color = ripe ? crop.RipeColor : Color.Lerp(crop.SproutColor, crop.RipeColor, fruitT * 0.5f);
                    renderer.sharedMaterial = MaterialCache.Get(color);
                }
            }

            if (ripe)
            {
                float sway = Mathf.Sin(Time.time * 2.4f) * 2.5f;
                Root.transform.localRotation = Quaternion.Euler(0f, sway, 0f);
            }
        }

        public void Destroy()
        {
            if (Root != null)
                Object.Destroy(Root);
            Root = null;
        }
    }
}
