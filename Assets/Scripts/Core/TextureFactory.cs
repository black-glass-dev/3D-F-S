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
        private static Texture2D _tuft;

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

        /// <summary>Alpha-cut grass blades for tuft cards.</summary>
        public static Texture2D Tuft
        {
            get
            {
                if (_tuft != null) return _tuft;

                const int s = 96;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.name = "GrassTuft";

                var px = new Color32[s * s];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 0);

                var rng = new System.Random(7);
                for (int blade = 0; blade < 11; blade++)
                {
                    float baseX = 10f + blade * 7f + (float)rng.NextDouble() * 4f;
                    float height = s * (0.40f + (float)rng.NextDouble() * 0.55f);
                    float width = 1.8f + (float)rng.NextDouble() * 1.6f;
                    float bend = ((float)rng.NextDouble() - 0.5f) * 16f;
                    float shade = 0.62f + (float)rng.NextDouble() * 0.34f;

                    for (int y = 0; y < (int)height; y++)
                    {
                        float t = y / height;
                        float x = baseX + bend * t * t;
                        int xi = Mathf.RoundToInt(x);
                        int thickness = Mathf.Max(0, Mathf.RoundToInt(width * (1f - t * 0.75f)));
                        byte c = (byte)(Mathf.Clamp01(shade) * 255f);

                        for (int k = -thickness; k <= thickness; k++)
                        {
                            int pxX = xi + k;
                            if (pxX < 0 || pxX >= s || y < 0 || y >= s) continue;
                            px[y * s + pxX] = new Color32(c, c, c, 255);
                        }
                    }
                }

                tex.SetPixels32(px);
                tex.Apply(true);
                _tuft = tex;
                return _tuft;
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
                            // tilled furrows with a few small stones and clods
                            float furrow = Mathf.Sin(v * Mathf.PI * 2f * 5f) * 0.5f + 0.5f;
                            float n = Noise(u, v, 34f);
                            float clod = Noise(u + 9.3f, v + 2.1f, 55f);
                            b = 0.76f + furrow * 0.14f + n * 0.12f;
                            if (clod > 0.78f) b -= 0.12f;
                            if (clod < 0.16f) b += 0.08f;
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
                            // mottled stone with soft cracks (thin dark lines)
                            float n = Noise(u, v, 12f) * 0.7f + Noise(u + 3f, v + 8f, 40f) * 0.3f;
                            b = 0.80f + n * 0.22f;
                            float crack = Mathf.Abs(Noise(u + 5.2f, v + 1.4f, 7f) - 0.5f);
                            if (crack < 0.035f) b -= 0.18f;
                            break;
                        }
                        case SurfacePattern.Water:
                        {
                            // gentle ripples plus a few bright sparkles
                            float wave = Mathf.Sin((v * 7f + Noise(u, v, 6f) * 2f) * Mathf.PI) * 0.5f + 0.5f;
                            float sparkle = Noise(u + 12f, v + 4f, 26f);
                            b = 0.80f + wave * 0.16f;
                            if (sparkle > 0.80f) b += 0.16f;
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
