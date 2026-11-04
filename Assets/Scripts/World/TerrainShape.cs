using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Height of the terrain at any world position. The farm itself sits on a
    /// flat shelf (so plots, fences and buildings line up), while the land
    /// beyond it rolls into low hills — that is what stops the ground from
    /// looking like a flat green table.
    /// </summary>
    public static class TerrainShape
    {
        /// <summary>Radius around the farm that stays perfectly flat.</summary>
        public const float FlatRadius = 13.5f;

        /// <summary>Radius where the hills have reached full height.</summary>
        public const float HillRadius = 27f;

        public static float Height(float x, float z)
        {
            float distance = Mathf.Sqrt(x * x + z * z);
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatRadius, HillRadius, distance));
            if (blend <= 0f) return 0f;

            float broad = Mathf.PerlinNoise(x * 0.045f + 12.3f, z * 0.045f + 4.7f) - 0.5f;
            float detail = Mathf.PerlinNoise(x * 0.13f + 2.1f, z * 0.13f + 9.4f) - 0.5f;

            return (broad * 3.4f + detail * 0.55f) * blend;
        }

        /// <summary>Same as Height but clamped so props never sink into a slope.</summary>
        public static Vector3 Place(float x, float z)
        {
            return new Vector3(x, Height(x, z), z);
        }
    }
}
