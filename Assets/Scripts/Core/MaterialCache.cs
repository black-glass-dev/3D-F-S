using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>Surface look for a material (see TextureFactory).</summary>
    public enum SurfacePattern
    {
        Smooth = 0,
        Grass = 1,
        GrassDark = 2,
        Soil = 3,
        SoilWet = 4,
        Wood = 5,
        Stone = 6,
        Water = 7,
        Leaf = 8
    }

    /// <summary>
    /// Central material factory. Materials are created once per colour+pattern
    /// and reused, so the whole farm shares a handful of draw batches.
    ///
    /// IMPORTANT: the shader is loaded from Resources (never Shader.Find of a
    /// built-in shader) so it can never be stripped from the player build.
    /// </summary>
    public static class MaterialCache
    {
        private static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>();
        private static Shader _lit;
        private static Shader _unlit;
        private static Shader _foliage;
        private static Material _particle;
        private static Material _tuftMaterial;

        public static Shader LitShader
        {
            get
            {
                if (_lit == null) _lit = LoadShader("Shaders/FarmLit", "FarmSim/Lit", "Standard");
                return _lit;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                    _unlit = LoadShader("Shaders/FarmParticle", "FarmSim/Particle", "Unlit/Color");
                return _unlit;
            }
        }

        public static Shader FoliageShader
        {
            get
            {
                if (_foliage == null)
                    _foliage = LoadShader("Shaders/FarmFoliage", "FarmSim/Foliage", "Sprites/Default");
                return _foliage;
            }
        }

        private static Shader LoadShader(string resourcePath, string shaderName, string builtinFallback)
        {
            var shader = Resources.Load<Shader>(resourcePath);
            if (shader != null) return shader;

            shader = Shader.Find(shaderName);
            if (shader != null) return shader;

            shader = Shader.Find(builtinFallback);
            if (shader != null) return shader;

            // Last resort: whatever the renderer's default material uses.
            var fallback = new Material(Shader.Find("Sprites/Default"));
            return fallback.shader;
        }

        public static Material Get(Color color)
        {
            return Get(color, SurfacePattern.Smooth);
        }

        public static Material Get(Color color, SurfacePattern pattern)
        {
            int key = (color.GetHashCode() * 397) ^ (int)pattern;
            Material mat;
            if (Cache.TryGetValue(key, out mat) && mat != null)
                return mat;

            mat = new Material(LitShader);
            mat.name = "FarmMat_" + pattern;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            if (pattern != SurfacePattern.Smooth && mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", TextureFactory.Get(pattern));
            Cache[key] = mat;
            return mat;
        }

        /// <summary>Alpha-cut grass tuft material (shared by every tuft).</summary>
        public static Material TuftMaterial
        {
            get
            {
                if (_tuftMaterial != null) return _tuftMaterial;
                _tuftMaterial = new Material(FoliageShader);
                _tuftMaterial.name = "FarmTuftMat";
                _tuftMaterial.mainTexture = TextureFactory.Tuft;
                if (_tuftMaterial.HasProperty("_Color"))
                    _tuftMaterial.SetColor("_Color", new Color(0.54f, 0.80f, 0.40f));
                if (_tuftMaterial.HasProperty("_Cutoff"))
                    _tuftMaterial.SetFloat("_Cutoff", 0.4f);
                return _tuftMaterial;
            }
        }

        /// <summary>Shared material for particles (unlit, tinted per particle).</summary>
        public static Material ParticleMaterial
        {
            get
            {
                if (_particle != null) return _particle;
                _particle = new Material(UnlitShader);
                _particle.name = "FarmParticleMat";
                _particle.mainTexture = TextureFactory.Dot;
                if (_particle.HasProperty("_TintColor"))
                    _particle.SetColor("_TintColor", Color.white);
                return _particle;
            }
        }
    }
}
