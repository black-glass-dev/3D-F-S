using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Procedurally generated textures — no image files needed in the repo.
    /// Patterns are intentionally near-white "detail" maps: the actual hue
    /// comes from the material colour, so one texture serves many objects.
    /// </summary>
    public static class TextureFactory
    {
        public const int Size = 96;

        private static readonly Dictionary<SurfacePattern, Texture2D> Cache =
            new Dictionary<SurfacePattern, Texture2D>();

        private static Texture2D _dot;
        private static Texture2D _white;

        public static Texture2D Get(SurfacePattern pattern)
        {
            Texture2D tex;
            if (Cache.TryGetValue(pattern, out tex) && tex != null)
                return tex;

            tex = Build(pattern);
            Cache[pattern] = tex;
            return tex;
        }

        /// <summary>Soft round dot — used for particles and UI circles.</summary>
        public static Texture2D Dot
        {
            get
            {
                if (_dot != null) return _dot;
                const int s = 64;
                _dot = NewTexture(s);
                var px = new Color32[s * s];
                float half = s * 0.5f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - half) / half;
                        float dy = (y + 0.5f - half) / half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a;
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                _dot.SetPixels32(px);
                _dot.Apply();
                return _dot;
            }
        }

        public static Texture2D White
        {
            get
            {
                if (_white != null) return _white;
                _white = NewTexture(4);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                _white.SetPixels32(px);
                _white.Apply();
                return _white;
            }
        }

        private static Texture2D NewTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "FarmGenTex";
            return tex;
        }

        private static float Noise(float x, float y, float scale)
        {
            return Mathf.PerlinNoise(x * scale, y * scale);
        }

        private static Texture2D Build(SurfacePattern pattern)
        {
            int s = Size;
            var tex = NewTexture(s);
            var px = new Color32[s * s];

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)s;
                    float v = y / (float)s;
                    float b = 1f;

                    switch (pattern)
                    {
                        case SurfacePattern.Grass:
                        {
                            // speckled lawn with a few darker blades
                            float n = Noise(u, v, 26f);
                            b = 0.86f + n * 0.20f;
                            if (Noise(u + 4.5f, v + 1.7f, 90f) > 0.72f) b -= 0.10f;
                            break;
                        }
                        case SurfacePattern.GrassDark:
                        {
                            float n = Noise(u, v, 20f);
                            b = 0.78f + n * 0.16f;
                            break;
                        }
                        case SurfacePattern.Soil:
                        {
                            // tilled furrows running along X
                            float furrow = Mathf.Sin(v * Mathf.PI * 2f * 5f) * 0.5f + 0.5f;
                            float n = Noise(u, v, 34f);
                            b = 0.80f + furrow * 0.12f + n * 0.12f;
                            break;
                        }
                        case SurfacePattern.SoilWet:
                        {
                            float furrow = Mathf.Sin(v * Mathf.PI * 2f * 5f) * 0.5f + 0.5f;
                            float n = Noise(u, v, 22f);
                            b = 0.70f + furrow * 0.10f + n * 0.10f;
                            break;
                        }
                        case SurfacePattern.Wood:
                        {
                            // vertical grain
                            float grain = Mathf.Sin(u * Mathf.PI * 2f * 9f + Noise(u, v, 5f) * 3f) * 0.5f + 0.5f;
                            b = 0.85f + grain * 0.15f;
                            break;
                        }
                        case SurfacePattern.Stone:
                        {
                            float n = Noise(u, v, 12f) * 0.7f + Noise(u + 3f, v + 8f, 40f) * 0.3f;
                            b = 0.84f + n * 0.22f;
                            break;
                        }
                        case SurfacePattern.Water:
                        {
                            float wave = Mathf.Sin((v * 7f + Noise(u, v, 6f) * 2f) * Mathf.PI) * 0.5f + 0.5f;
                            b = 0.88f + wave * 0.14f;
                            break;
                        }
                        case SurfacePattern.Leaf:
                        {
                            float n = Noise(u, v, 30f);
                            b = 0.88f + n * 0.16f;
                            break;
                        }
                        default:
                        {
                            b = 1f;
                            break;
                        }
                    }

                    b = Mathf.Clamp01(b);
                    byte c = (byte)(b * 255f);
                    px[y * s + x] = new Color32(c, c, c, 255);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
