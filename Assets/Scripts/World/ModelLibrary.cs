using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Optional imported 3D models.
    ///
    /// Drop any .fbx / .obj into
    ///     Assets/Resources/Models/
    /// and the world builder uses it instead of the procedural version as soon
    /// as the file name matches a key below ("house", "barn", "windmill",
    /// "tree", "pine", "poplar", "rock", "scarecrow", "well").
    ///
    /// The model is auto-normalised: whatever its native size is, it is scaled
    /// to the requested height, centred on its parent and dropped so its
    /// lowest point sits on y = 0. That means an imported asset can never
    /// float or swallow the map.
    ///
    /// Imported materials that use a built-in shader (Standard) are stripped in
    /// Android builds — that caused the v1.0.0 magenta screen — so unusable
    /// shaders are swapped for the game's own FarmSim/Lit material.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Folder = "Models/";

        public static bool Exists(string key)
        {
            return Resources.Load<GameObject>(Folder + key) != null;
        }

        /// <summary>
        /// Spawns a model scaled to <paramref name="targetHeight"/> metres.
        /// Returns null when the resource does not exist, which tells the
        /// caller to build the procedural fallback instead.
        /// </summary>
        public static GameObject Spawn(string key, Transform parent, Vector3 localPosition,
            float targetHeight, float yRotation = 0f, Material materialOverride = null,
            bool castShadows = true)
        {
            var prefab = Resources.Load<GameObject>(Folder + key);
            if (prefab == null) return null;

            // Measure at the origin with no rotation: world bounds == local bounds.
            var instance = Object.Instantiate(prefab);
            instance.name = key;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var bounds = ComputeBounds(instance);
            float height = bounds.size.y;
            float scale = height > 0.0001f ? targetHeight / height : 1f;

            instance.transform.SetParent(parent, false);
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            instance.transform.localPosition = localPosition + new Vector3(
                -bounds.center.x * scale,
                -bounds.min.y * scale,
                -bounds.center.z * scale);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                if (materialOverride != null)
                {
                    renderer.sharedMaterial = materialOverride;
                }
                else if (renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.isSupported)
                {
                    renderer.sharedMaterial = MaterialCache.Get(new Color(0.72f, 0.70f, 0.64f));
                }

                renderer.shadowCastingMode = castShadows
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }

            // Imported props often bring colliders; the world is decorative, so
            // drop them (the farm plots keep their own colliders).
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Object.Destroy(colliders[i]);

            return instance;
        }

        private static Bounds ComputeBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
