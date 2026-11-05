using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// The user's own 3D models.
    ///
    /// Two kinds of drop-in assets are supported, both inside
    ///     Assets/Resources/Models/
    ///
    ///  1. &lt;key&gt;.fbx / &lt;key&gt;.obj  — Unity imports it; nothing else needed.
    ///  2. &lt;key&gt;.bytes                — the compact baked mesh written by
    ///     tools/bake_models.py (positions + normals + vertex colours).
    ///     Used for heavy scanned models: a 500 000-triangle photogrammetry
    ///     house becomes a 62 000-triangle, 2.5 MB mesh with no texture fetch
    ///     and a single draw call.
    ///
    /// Keys: house, barn, windmill, tree, pine, poplar, rock, scarecrow, well.
    /// Anything without an asset keeps its procedural version, so a missing
    /// file can never break the scene.
    ///
    /// Every model is auto-normalised: scaled to the requested height, centred
    /// on its parent and dropped so its lowest point sits on y = 0.
    ///
    /// Imported materials that use a built-in shader (Standard) are stripped in
    /// Android builds — that caused the v1.0.0 magenta screen — so their
    /// texture/colour is copied onto the game's own FarmSim/Lit material.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Folder = "Models/";

        private const int HeaderSize = 20;   // magic(4) ver(2) flags(2) vcount(4) tcount(4) idx(4)
        private const int VertexStride = 28; // v1: 3f pos + 3f normal + 4 bytes colour
        private const int VertexStrideUv = 32; // v2: 3f pos + 3f normal + 2f uv

        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        private static readonly Dictionary<string, Material> BakedMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, bool> TexturedKeys = new Dictionary<string, bool>();
        private static readonly Dictionary<Material, Material> Adapted = new Dictionary<Material, Material>();

        private static readonly Color DefaultPropColor = new Color(0.72f, 0.70f, 0.64f);

        public static bool Exists(string key)
        {
            return Resources.Load<GameObject>(Folder + key) != null
                   || Resources.Load<TextAsset>(Folder + key) != null;
        }

        /// <summary>
        /// Spawns a model scaled to <paramref name="targetHeight"/> metres, facing
        /// +Z. Returns null when no asset exists for <paramref name="key"/>, which
        /// tells the caller to build the procedural fallback instead.
        /// </summary>
        public static GameObject Spawn(string key, Transform parent, Vector3 localPosition,
            float targetHeight, float yRotation = 0f, Material materialOverride = null,
            bool castShadows = true)
        {
            var instance = Instantiate(key);
            if (instance == null) return null;

            instance.name = key;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Measure at the origin with no rotation: world bounds == local bounds.
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
                renderer.sharedMaterial = materialOverride != null
                    ? materialOverride
                    : AdaptMaterial(renderer.sharedMaterial, key);
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

        // ------------------------------------------------------------------
        // instantiation
        // ------------------------------------------------------------------
        private static GameObject Instantiate(string key)
        {
            var prefab = Resources.Load<GameObject>(Folder + key);
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab);
                go.name = key;
                return go;
            }

            var mesh = LoadBakedMesh(key);
            if (mesh == null) return null;

            var built = new GameObject(key + "_mesh");
            built.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = built.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = BakedMaterial(key);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return built;
        }

        private static Material BakedMaterial(string key)
        {
            Material mat;
            if (BakedMaterials.TryGetValue(key, out mat) && mat != null) return mat;

            mat = new Material(MaterialCache.LitShader) { name = "FarmBaked_" + key };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.02f);

            bool textured;
            TexturedKeys.TryGetValue(key, out textured);
            if (textured)
            {
                var albedo = Resources.Load<Texture2D>(Folder + key + "_albedo");
                if (albedo != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
                if (mat.HasProperty("_VertexColor")) mat.SetFloat("_VertexColor", 0f);
            }
            else if (mat.HasProperty("_VertexColor"))
            {
                mat.SetFloat("_VertexColor", 1f);
            }
            BakedMaterials[key] = mat;
            return mat;
        }

        // ------------------------------------------------------------------
        // material handling
        // ------------------------------------------------------------------
        /// <summary>
        /// Returns a FarmSim/Lit material that looks like the imported one.
        /// Keeping the albedo texture is what makes an imported model read as
        /// "the same art" as the rest of the farm.
        /// </summary>
        private static Material AdaptMaterial(Material source, string key)
        {
            if (source == null) return MaterialCache.Get(DefaultPropColor);
            if (source.shader == MaterialCache.LitShader) return source;

            Material adapted;
            if (Adapted.TryGetValue(source, out adapted) && adapted != null) return adapted;

            adapted = new Material(MaterialCache.LitShader) { name = "FarmImported_" + key };
            if (adapted.HasProperty("_Color"))
                adapted.SetColor("_Color", source.HasProperty("_Color") ? source.color : Color.white);
            if (adapted.HasProperty("_Smoothness")) adapted.SetFloat("_Smoothness", 0.04f);

            Texture texture = null;
            if (source.HasProperty("_MainTex")) texture = source.mainTexture;
            if (texture == null) texture = source.mainTexture;
            if (texture == null) texture = Resources.Load<Texture2D>(Folder + key + "_atlas");
            if (texture != null && adapted.HasProperty("_MainTex")) adapted.SetTexture("_MainTex", texture);

            // Imported meshes are not always wound the way Unity expects, and a
            // see-through building is worse than a few wasted pixels.
            if (adapted.HasProperty("_Cull")) adapted.SetFloat("_Cull", 0f);

            Adapted[source] = adapted;
            return adapted;
        }

        // ------------------------------------------------------------------
        // baked mesh loading  (see tools/bake_models.py for the format)
        // ------------------------------------------------------------------
        private static Mesh LoadBakedMesh(string key)
        {
            Mesh cached;
            if (Meshes.TryGetValue(key, out cached) && cached != null) return cached;

            var asset = Resources.Load<TextAsset>(Folder + key);
            if (asset == null) return null;

            Mesh mesh = Parse(asset.bytes, key);
            if (mesh == null)
            {
                // A silent null here means the world quietly falls back to the
                // procedural prop (that is how the house model went missing in
                // v1.1.x) — make it shout in the device log instead.
                Debug.LogError("[ModelLibrary] " + key + ".bytes exists but could not be parsed; " +
                               "using the procedural fallback.");
                return null;
            }
            Meshes[key] = mesh;

            // The bytes were copied into managed arrays; release the 2.5 MB blob.
            Resources.UnloadAsset(asset);
            return mesh;
        }

        private static Mesh Parse(byte[] bytes, string key)
        {
            if (bytes == null || bytes.Length < HeaderSize) return null;
            if (bytes[0] != (byte)'F' || bytes[1] != (byte)'M' || bytes[2] != (byte)'S' || bytes[3] != (byte)'H')
                return null;

            int version = bytes[4] | (bytes[5] << 8);
            if (version != 1 && version != 2) return null;

            int vertexCount = ReadInt(bytes, 8);
            int triangleCount = ReadInt(bytes, 12);
            int indexSize = ReadInt(bytes, 16);
            if (vertexCount <= 0 || triangleCount <= 0) return null;
            // v1 indexes are 2 or 4 bytes; v2 may also be 0 (non-indexed).
            if (indexSize != 2 && indexSize != 4 && !(version == 2 && indexSize == 0)) return null;

            int stride = version == 2 ? VertexStrideUv : VertexStride;
            long expected = HeaderSize + (long)vertexCount * stride
                            + (long)triangleCount * 3 * indexSize;
            if (bytes.Length < expected) return null;

            if (version == 2)
            {
                TexturedKeys[key] = true;
                return ParseTextured(bytes, key, vertexCount, triangleCount, indexSize);
            }

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var colors = new Color32[vertexCount];

            var raw = new float[vertexCount * 6];
            System.Buffer.BlockCopy(bytes, HeaderSize, raw, 0, vertexCount * 24);

            int colourOffset = HeaderSize + vertexCount * 24;
            // System.Buffer.BlockCopy only takes arrays of primitives, so the RGBA
            // stream goes through a byte[] and is packed into Color32 by hand.
            var colourBytes = new byte[vertexCount * 4];
            System.Buffer.BlockCopy(bytes, colourOffset, colourBytes, 0, colourBytes.Length);

            for (int i = 0; i < vertexCount; i++)
            {
                int f = i * 6;
                vertices[i] = new Vector3(raw[f], raw[f + 1], raw[f + 2]);
                normals[i] = new Vector3(raw[f + 3], raw[f + 4], raw[f + 5]);

                int c = i * 4;
                colors[i] = new Color32(colourBytes[c], colourBytes[c + 1], colourBytes[c + 2], colourBytes[c + 3]);
            }

            var triangles = new int[triangleCount * 3];
            int indexOffset = colourOffset + vertexCount * 4;
            if (indexSize == 4)
            {
                System.Buffer.BlockCopy(bytes, indexOffset, triangles, 0, triangleCount * 3 * 4);
            }
            else
            {
                var shortIndices = new ushort[triangleCount * 3];
                System.Buffer.BlockCopy(bytes, indexOffset, shortIndices, 0, triangleCount * 3 * 2);
                for (int i = 0; i < shortIndices.Length; i++) triangles[i] = shortIndices[i];
            }

            var mesh = new Mesh { name = key };
            if (vertexCount > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Version 2: duplicated vertices, UVs, no vertex colours.</summary>
        private static Mesh ParseTextured(byte[] bytes, string key, int vertexCount, int triangleCount, int indexSize)
        {
            var raw = new float[vertexCount * 8];
            System.Buffer.BlockCopy(bytes, HeaderSize, raw, 0, vertexCount * 32);

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                int f = i * 8;
                vertices[i] = new Vector3(raw[f], raw[f + 1], raw[f + 2]);
                normals[i] = new Vector3(raw[f + 3], raw[f + 4], raw[f + 5]);
                uvs[i] = new Vector2(raw[f + 6], raw[f + 7]);
            }

            var triangles = new int[triangleCount * 3];
            int indexOffset = HeaderSize + vertexCount * 32;
            if (indexSize == 0)
            {
                for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
            }
            else if (indexSize == 4)
            {
                System.Buffer.BlockCopy(bytes, indexOffset, triangles, 0, triangleCount * 3 * 4);
            }
            else
            {
                var shortIndices = new ushort[triangleCount * 3];
                System.Buffer.BlockCopy(bytes, indexOffset, shortIndices, 0, triangleCount * 3 * 2);
                for (int i = 0; i < shortIndices.Length; i++) triangles[i] = shortIndices[i];
            }

            var mesh = new Mesh { name = key };
            if (vertexCount > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int ReadInt(byte[] bytes, int offset)
        {
            return bytes[offset] | (bytes[offset + 1] << 8)
                   | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
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
