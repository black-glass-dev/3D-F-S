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
            switch (cropIndex)
            {
                case 1:
                    visual.BuildTomato();
                    break;
                case 2:
                    visual.BuildPumpkin();
                    break;
                default:
                    visual.BuildWheat();
                    break;
            }
            return visual;
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

                // grain head (ripens to gold)
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(Root.transform, false);
                head.transform.localPosition = pos + Vector3.up * 0.58f;
                head.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
                Object.Destroy(head.GetComponent<Collider>());
                var headRenderer = head.GetComponent<Renderer>();
                headRenderer.sharedMaterial = MaterialCache.Get(green, SurfacePattern.Leaf);
                _leaves.Add(headRenderer);
                _fruits.Add(head.transform);
                _fruitRenderers.Add(headRenderer);
            }
        }

        private void BuildTomato()
        {
            var leaf = new Color(0.30f, 0.42f, 0.25f);
            var leafLight = new Color(0.38f, 0.52f, 0.29f);

            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.SetParent(Root.transform, false);
            bush.transform.localPosition = Vector3.up * 0.26f;
            bush.transform.localScale = new Vector3(0.62f, 0.5f, 0.62f);
            Object.Destroy(bush.GetComponent<Collider>());
            var bushRenderer = bush.GetComponent<Renderer>();
            bushRenderer.sharedMaterial = MaterialCache.Get(leaf, SurfacePattern.Leaf);
            _leaves.Add(bushRenderer);

            var bushTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bushTop.name = "BushTop";
            bushTop.transform.SetParent(Root.transform, false);
            bushTop.transform.localPosition = new Vector3(0.1f, 0.44f, -0.05f);
            bushTop.transform.localScale = new Vector3(0.42f, 0.36f, 0.42f);
            Object.Destroy(bushTop.GetComponent<Collider>());
            var bushTopRenderer = bushTop.GetComponent<Renderer>();
            bushTopRenderer.sharedMaterial = MaterialCache.Get(leafLight, SurfacePattern.Leaf);
            _leaves.Add(bushTopRenderer);

            var offsets = new[]
            {
                new Vector3(0.26f, 0.24f, 0.12f),
                new Vector3(-0.22f, 0.30f, 0.18f),
                new Vector3(0.04f, 0.20f, -0.26f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var tomato = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tomato.name = "Tomato";
                tomato.transform.SetParent(Root.transform, false);
                tomato.transform.localPosition = offsets[i];
                tomato.transform.localScale = Vector3.one * 0.17f;
                Object.Destroy(tomato.GetComponent<Collider>());
                var renderer = tomato.GetComponent<Renderer>();
                renderer.sharedMaterial = MaterialCache.Get(CropDatabase.Get(_cropIndex).RipeColor);
                _fruits.Add(tomato.transform);
                _fruitRenderers.Add(renderer);
            }
        }

        private void BuildPumpkin()
        {
            var leaf = new Color(0.28f, 0.40f, 0.24f);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * 0.24f, 0.12f, Mathf.Sin(angle) * 0.24f);

                var leafBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leafBall.name = "Leaf";
                leafBall.transform.SetParent(Root.transform, false);
                leafBall.transform.localPosition = pos;
                leafBall.transform.localScale = new Vector3(0.34f, 0.16f, 0.34f);
                Object.Destroy(leafBall.GetComponent<Collider>());
                var renderer = leafBall.GetComponent<Renderer>();
                renderer.sharedMaterial = MaterialCache.Get(leaf, SurfacePattern.Leaf);
                _leaves.Add(renderer);
            }

            var offsets = new[]
            {
                new Vector3(-0.12f, 0.16f, 0.06f),
                new Vector3(0.22f, 0.14f, -0.12f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var pumpkin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pumpkin.name = "Pumpkin";
                pumpkin.transform.SetParent(Root.transform, false);
                pumpkin.transform.localPosition = offsets[i];
                pumpkin.transform.localScale = new Vector3(0.3f, 0.22f, 0.3f);
                Object.Destroy(pumpkin.GetComponent<Collider>());
                var renderer = pumpkin.GetComponent<Renderer>();
                renderer.sharedMaterial = MaterialCache.Get(CropDatabase.Get(_cropIndex).RipeColor);
                _fruits.Add(pumpkin.transform);
                _fruitRenderers.Add(renderer);
            }
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
                fruit.localScale = fruitBaseScale(i) * s;
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

        /// <summary>Small per-part base scale, kept so the growth lerp is smooth.</summary>
        private Vector3 fruitBaseScale(int index)
        {
            switch (_cropIndex)
            {
                case 1: return Vector3.one * 0.17f;
                case 2: return new Vector3(0.3f, 0.22f, 0.3f);
                default: return new Vector3(0.09f, 0.16f, 0.09f);
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
