using UnityEngine;

namespace FarmSim
{
    /// <summary>Cheap colour grading pass (contrast, saturation, warmth, vignette).</summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        public float Saturation = 1.14f;
        public float Contrast = 1.10f;
        public float Brightness = -0.015f;
        public float Warmth = 0.55f;
        public float Vignette = 0.62f;
        public float ShadowLift = 0.55f;

        private Material _material;
        private bool _supported;

        private void Awake()
        {
            var shader = Resources.Load<Shader>("Shaders/FarmPost");
            if (shader != null && shader.isSupported)
            {
                _material = new Material(shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
                _supported = true;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!_supported || _material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material.SetFloat("_Saturation", Saturation);
            _material.SetFloat("_Contrast", Contrast);
            _material.SetFloat("_Brightness", Brightness);
            _material.SetFloat("_Warmth", Warmth);
            _material.SetFloat("_Vignette", Vignette);
            _material.SetFloat("_ShadowLift", ShadowLift);
            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null)
                DestroyImmediate(_material);
        }
    }
}
