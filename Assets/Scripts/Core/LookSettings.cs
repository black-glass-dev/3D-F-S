using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// "How lit is my farm" — a three step player setting (dim / normal /
    /// bright) that is multiplied into the sun, the ambient light, the fill
    /// light and the camera grade.
    ///
    /// Several players reported props reading as near-black. Part of that was
    /// a colour-space mistake (fixed by staying in gamma space), but phone
    /// screens differ a lot, so the brightness is now also in the player's
    /// hands and the default is the brighter step.
    /// </summary>
    public static class LookSettings
    {
        public const int StepCount = 3;
        public const int DefaultStep = 2;

        private const string Key = "farmsim_light";

        private static int _step = -1;

        public static int Step
        {
            get
            {
                if (_step < 0)
                    _step = Mathf.Clamp(PlayerPrefs.GetInt(Key, DefaultStep), 0, StepCount - 1);
                return _step;
            }
        }

        /// <summary>Multiplier applied to every light source and to exposure.</summary>
        public static float Multiplier
        {
            get
            {
                switch (Step)
                {
                    case 0: return 0.86f;
                    case 1: return 1.00f;
                    default: return 1.16f;
                }
            }
        }

        public static string Label
        {
            get
            {
                switch (Step)
                {
                    case 0: return Localization.T("light_dim");
                    case 1: return Localization.T("light_normal");
                    default: return Localization.T("light_bright");
                }
            }
        }

        /// <summary>Advances to the next step and remembers it.</summary>
        public static void Cycle()
        {
            _step = (Step + 1) % StepCount;
            PlayerPrefs.SetInt(Key, _step);
            PlayerPrefs.Save();
        }
    }
}
