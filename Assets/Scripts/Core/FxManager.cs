using System.Collections;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Juice: particle puffs and floating "+coins" text.
    /// Everything is created on demand and destroyed automatically, so there
    /// is no pooling bookkeeping to get wrong.
    /// </summary>
    public class FxManager : MonoBehaviour
    {
        public static FxManager Instance { get; private set; }

        private Material _particleMaterial;

        public void Init()
        {
            Instance = this;
            _particleMaterial = MaterialCache.ParticleMaterial;
        }

        // ---------- particles ----------

        public void Dust(Vector3 position)
        {
            Burst(position, new Color(0.76f, 0.63f, 0.45f), 9, 1.0f, 0.17f, 0.5f, 0.4f);
        }

        public void Splash(Vector3 position)
        {
            Burst(position + Vector3.up * 0.28f, new Color(0.55f, 0.80f, 1f), 14, 1.7f, 0.13f, 0.55f, 1.1f);
        }

        public void Sparkle(Vector3 position, Color tint)
        {
            Burst(position + Vector3.up * 0.55f, tint, 10, 1.1f, 0.12f, 0.7f, -0.12f);
        }

        public void Harvest(Vector3 position, Color cropColor)
        {
            Burst(position + Vector3.up * 0.35f, cropColor, 14, 1.9f, 0.15f, 0.6f, 1.0f);
            Burst(position + Vector3.up * 0.5f, new Color(1f, 0.95f, 0.55f), 8, 1.2f, 0.11f, 0.75f, -0.05f);
        }

        private void Burst(Vector3 position, Color color, int count, float speed,
            float size, float lifetime, float gravity)
        {
            var go = new GameObject("fx");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = _particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Emit(count);
            Destroy(go, lifetime + 0.6f);
        }

        // ---------- floating text ----------

        public void CoinPop(Vector3 worldPosition, int amount)
        {
            var go = new GameObject("coinpop");
            go.transform.position = worldPosition + Vector3.up * 0.9f;

            var text = go.AddComponent<TextMesh>();
            text.text = "+" + amount;
            text.font = GameFonts.Builtin;
            text.fontSize = 72;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 0.88f, 0.32f, 1f);

            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (text.font != null)
            {
                meshRenderer.sharedMaterial = text.font.material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            StartCoroutine(AnimateCoinPop(go, text));
        }

        private IEnumerator AnimateCoinPop(GameObject go, TextMesh text)
        {
            float duration = 1.1f;
            float elapsed = 0f;
            Vector3 start = go.transform.position;
            Color baseColor = text.color;
            Camera cam = Camera.main;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                go.transform.position = start + Vector3.up * (t * 0.9f);
                float scale = 1f + Mathf.Sin(Mathf.Clamp01(t * 3f) * Mathf.PI) * 0.25f;
                go.transform.localScale = Vector3.one * scale;

                var c = baseColor;
                c.a = 1f - Mathf.Pow(t, 2f);
                text.color = c;

                if (cam != null)
                    go.transform.rotation = cam.transform.rotation;

                yield return null;
            }

            Destroy(go);
        }
    }
}
