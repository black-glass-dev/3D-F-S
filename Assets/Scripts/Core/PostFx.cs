using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Cinematic colour grade in a single full-screen pass: exposure, filmic
    /// tonemap, contrast, desaturation, split tone, black lift, vignette and
    /// grain. Values are set by GameBootstrap per quality tier.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        [Header("Exposure & tone")]
        public float Exposure = 1.02f;
        [Range(0f, 1f)] public float ToneStrength = 0.85f;
        public float Contrast = 1.10f;
        [Range(0f, 1.5f)] public float Saturation = 0.90f;
        [Range(0f, 0.2f)] public float Lift = 0.03f;

        [Header("Colour & finish")]
        public float Warmth = 0.7f;
        public float Vignette = 0.62f;
        public float Grain = 0.012f;

        private Material _material;
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ToneId = Shader.PropertyToID("_ToneStrength");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int LiftId = Shader.PropertyToID("_Lift");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int WarmthId = Shader.PropertyToID("_Warmth");
        private static readonly int GrainId = Shader.PropertyToID("_Grain");

        private void Awake()
        {
            var shader = Resources.Load<Shader>("Shaders/FarmPost");
            if (shader == null) shader = Shader.Find("FarmSim/PostFx");
            if (shader != null && shader.isSupported)
            {
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // The player's "light" setting scales the whole grade, so the
            // brightness button feels like it does something everywhere.
            _material.SetFloat(ExposureId, Exposure * LookSettings.Multiplier);
            _material.SetFloat(ToneId, ToneStrength);
            _material.SetFloat(ContrastId, Contrast);
            _material.SetFloat(SaturationId, Saturation);
            _material.SetFloat(LiftId, Lift);
            _material.SetFloat(VignetteId, Vignette * Mathf.Lerp(1.0f, 0.82f, (LookSettings.Multiplier - 0.86f) / 0.30f));
            _material.SetFloat(WarmthId, Warmth);
            _material.SetFloat(GrainId, Grain);

            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
        }
    }
}
