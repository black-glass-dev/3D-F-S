using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// v1.6.0 weather. Rainy days water every growing plot for free, so they
    /// need to be visible: one looping, low-particle-count system that covers
    /// the whole farm (a single box emitter, no follow logic, no allocation per
    /// frame) plus the darker sky/light handled by GameManager.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        private ParticleSystem _rain;
        private ParticleSystemRenderer _rainRenderer;
        private bool _on;

        public void Init(Material material)
        {
            Instance = this;
            _rain = BuildRain(material);
            SetRain(false);
        }

        public void SetRain(bool on)
        {
            if (_rain == null || _on == on) return;
            _on = on;

            if (on)
            {
                _rain.Play();
            }
            else
            {
                _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private ParticleSystem BuildRain(Material material)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 15f, 0f);
            // v1.8.0: the default box shape fires particles along its local +Z,
            // which made the rain streak sideways across the farm. Rotating the
            // emitter 90 degrees about X points +Z straight down, while the
            // box itself (46 x 46 x 0.2 in local space) stays a flat slab.
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(11f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.055f);
            main.startColor = new Color(0.74f, 0.82f, 0.90f, 0.55f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 900;

            var emission = ps.emission;
            emission.rateOverTime = 420f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(46f, 46f, 0.2f);

            // a touch of sideways drift so the drops are not perfectly vertical
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.separateAxes = true;      // x/y/z are ignored without this
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.055f;
                renderer.lengthScale = 2.2f;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (material != null) renderer.sharedMaterial = material;
                _rainRenderer = renderer;
            }

            return ps;
        }

        /// <summary>Slightly fewer drops on the low tier.</summary>
        public void ApplyQuality()
        {
            if (_rain == null) return;
            var emission = _rain.emission;
            emission.rateOverTime = GraphicsSettings.Current == GraphicsSettings.Tier.Low ? 260f : 420f;
            if (_rainRenderer != null)
                _rainRenderer.enabled = true;
        }
    }
}
