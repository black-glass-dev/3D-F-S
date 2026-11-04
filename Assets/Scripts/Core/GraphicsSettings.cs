using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// One place where the game decides how expensive it is allowed to look.
    /// The look was designed on the "High" tier; lower tiers keep the same
    /// art direction but drop the cheapest-to-lose parts (grass cards, long
    /// shadows, MSAA) so old phones still hold 60 fps.
    /// </summary>
    public static class GraphicsSettings
    {
        public enum Tier
        {
            Low = 0,
            Medium = 1,
            High = 2
        }

        public static Tier Current { get; private set; } = Tier.Medium;

        public static bool GrassTufts => Current != Tier.Low;
        public static bool Clouds => true;
        public static bool AmbientProps => Current != Tier.Low;
        public static bool SoftShadows => Current != Tier.Low;

        public static float ShadowDistance
        {
            get
            {
                switch (Current)
                {
                    case Tier.High: return 34f;
                    case Tier.Medium: return 26f;
                    default: return 18f;
                }
            }
        }

        public static int AntiAliasing => Current == Tier.High ? 4 : 2;
        public static int PixelLightCount => Current == Tier.High ? 3 : 1;
        public static int ShadowResolutionLevel => Current == Tier.High ? 2 : 1;

        /// <summary>Guess a tier from the device and apply it to QualitySettings.</summary>
        public static void Apply()
        {
            Detect();
            ApplyQuality();
        }

        private static void Detect()
        {
            int cores = SystemInfo.processorCount;
            int memoryMb = SystemInfo.systemMemorySize;
            int gpuMemoryMb = SystemInfo.graphicsMemorySize;

            if (cores <= 4 || memoryMb > 0 && memoryMb < 2400 || gpuMemoryMb > 0 && gpuMemoryMb < 320)
                Current = Tier.Low;
            else if (cores <= 6 || memoryMb < 3600 || gpuMemoryMb < 900)
                Current = Tier.Medium;
            else
                Current = Tier.High;
        }

        private static void ApplyQuality()
        {
            QualitySettings.shadows = Current == Tier.Low
                ? ShadowQuality.HardOnly
                : ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolutionLevel >= 2
                ? ShadowResolution.VeryHigh
                : ShadowResolution.Medium;
            QualitySettings.shadowDistance = ShadowDistance;
            QualitySettings.shadowCascades = Current == Tier.High ? 2 : 0;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;

            QualitySettings.antiAliasing = AntiAliasing;
            QualitySettings.pixelLightCount = PixelLightCount;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.vSyncCount = 0;
            QualitySettings.skinWeights = SkinWeights.TwoBones;

            // the camera renders exactly what the design needs
            Application.targetFrameRate = 60;
        }

        /// <summary>Quality of the colour grade (lower tiers get a gentler look).</summary>
        public static float GradeSaturation => Current == Tier.High ? 1.14f : 1.07f;

        public static float GradeContrast => Current == Tier.High ? 1.10f : 1.05f;
    }
}
