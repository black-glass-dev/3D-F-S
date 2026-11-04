using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// All game audio is synthesised at runtime — no sound files in the repo
    /// (keeps the APK tiny and avoids licensing questions).
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource _source;
        private AudioClip _tap;
        private AudioClip _plant;
        private AudioClip _water;
        private AudioClip _harvest;
        private AudioClip _coin;
        private AudioClip _error;
        private AudioClip _ready;
        private AudioClip _day;

        public bool SoundOn { get; private set; }
        public bool HapticsOn { get; private set; }

        public void Init()
        {
            Instance = this;
            SoundOn = PlayerPrefs.GetInt("farmsim_sound", 1) == 1;
            HapticsOn = PlayerPrefs.GetInt("farmsim_haptics", 1) == 1;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.9f;
            _source.ignoreListenerPause = true;

            _tap = BuildTap();
            _plant = BuildPlant();
            _water = BuildWater();
            _harvest = BuildHarvest();
            _coin = BuildCoin();
            _error = BuildError();
            _ready = BuildReady();
            _day = BuildDay();
        }

        // ---------- public API ----------

        public void PlayTap()
        {
            Play(_tap, 0.97f, 1.06f);
        }

        public void PlayPlant()
        {
            Play(_plant, 0.95f, 1.05f);
            Haptic();
        }

        public void PlayWater()
        {
            Play(_water, 0.94f, 1.04f);
        }

        public void PlayHarvest()
        {
            Play(_harvest, 0.98f, 1.04f);
            Play(_coin, 0.98f, 1.02f);
            Haptic();
        }

        public void PlayCoin()
        {
            Play(_coin, 0.97f, 1.05f);
        }

        public void PlayError()
        {
            Play(_error, 1f, 1f);
            Haptic();
        }

        public void PlayReady()
        {
            Play(_ready, 0.98f, 1.03f);
        }

        public void PlayNewDay()
        {
            Play(_day, 1f, 1f);
        }

        public void Haptic()
        {
            if (!HapticsOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void ToggleSound()
        {
            SoundOn = !SoundOn;
            PlayerPrefs.SetInt("farmsim_sound", SoundOn ? 1 : 0);
            PlayerPrefs.Save();
            if (SoundOn) PlayTap();
        }

        public void ToggleHaptics()
        {
            HapticsOn = !HapticsOn;
            PlayerPrefs.SetInt("farmsim_haptics", HapticsOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Play(AudioClip clip, float pitchMin, float pitchMax)
        {
            if (!SoundOn || _source == null || clip == null) return;
            _source.pitch = Random.Range(pitchMin, pitchMax);
            _source.PlayOneShot(clip, 1f);
        }

        // ---------- synthesis ----------

        private static float[] Buffer(float seconds)
        {
            int len = Mathf.Max(64, Mathf.CeilToInt(SampleRate * seconds));
            return new float[len];
        }

        private static AudioClip ToClip(string name, float[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AddTone(float[] buf, float startSec, float durSec,
            float freqStart, float freqEnd, float gain, int wave)
        {
            int start = Mathf.Clamp(Mathf.RoundToInt(startSec * SampleRate), 0, buf.Length - 1);
            int count = Mathf.Min(Mathf.RoundToInt(durSec * SampleRate), buf.Length - start);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float freq = Mathf.Lerp(freqStart, freqEnd, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float s;
                if (wave == 0)
                {
                    s = Mathf.Sin(phase);
                }
                else if (wave == 1)
                {
                    float wrapped = Mathf.Repeat(phase / (2f * Mathf.PI), 1f);
                    s = 4f * Mathf.Abs(wrapped - 0.5f) - 1f;
                }
                else
                {
                    s = Mathf.Sin(phase) >= 0f ? 0.55f : -0.55f;
                }
                float attack = Mathf.Min(1f, t * 60f);
                float decay = Mathf.Pow(1f - t, 2.0f);
                buf[start + i] += s * attack * decay * gain;
            }
        }

        private static void AddNoise(float[] buf, float startSec, float durSec,
            float gain, float smooth, int seed)
        {
            var rng = new System.Random(seed);
            int start = Mathf.Clamp(Mathf.RoundToInt(startSec * SampleRate), 0, buf.Length - 1);
            int count = Mathf.Min(Mathf.RoundToInt(durSec * SampleRate), buf.Length - start);
            float last = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, raw, smooth);
                float env = Mathf.Sin(Mathf.PI * t);
                buf[start + i] += last * env * gain;
            }
        }

        private static AudioClip BuildTap()
        {
            var buf = Buffer(0.07f);
            AddTone(buf, 0f, 0.05f, 1500f, 900f, 0.22f, 0);
            AddNoise(buf, 0f, 0.02f, 0.05f, 0.4f, 11);
            return ToClip("sfx_tap", buf);
        }

        private static AudioClip BuildPlant()
        {
            var buf = Buffer(0.26f);
            AddTone(buf, 0f, 0.18f, 150f, 70f, 0.55f, 0);
            AddNoise(buf, 0f, 0.16f, 0.16f, 0.12f, 21);
            AddTone(buf, 0.1f, 0.1f, 520f, 700f, 0.12f, 0);
            return ToClip("sfx_plant", buf);
        }

        private static AudioClip BuildWater()
        {
            var buf = Buffer(0.42f);
            AddNoise(buf, 0f, 0.38f, 0.30f, 0.22f, 31);
            AddTone(buf, 0.02f, 0.34f, 700f, 340f, 0.12f, 0);
            return ToClip("sfx_water", buf);
        }

        private static AudioClip BuildHarvest()
        {
            var buf = Buffer(0.5f);
            AddTone(buf, 0f, 0.20f, 523f, 523f, 0.22f, 1);
            AddTone(buf, 0.08f, 0.20f, 659f, 659f, 0.22f, 1);
            AddTone(buf, 0.16f, 0.26f, 784f, 784f, 0.20f, 1);
            AddNoise(buf, 0f, 0.1f, 0.10f, 0.3f, 41);
            return ToClip("sfx_harvest", buf);
        }

        private static AudioClip BuildCoin()
        {
            var buf = Buffer(0.3f);
            AddTone(buf, 0f, 0.10f, 988f, 988f, 0.24f, 0);
            AddTone(buf, 0.06f, 0.20f, 1319f, 1319f, 0.22f, 0);
            return ToClip("sfx_coin", buf);
        }

        private static AudioClip BuildError()
        {
            var buf = Buffer(0.22f);
            AddTone(buf, 0f, 0.14f, 220f, 160f, 0.20f, 2);
            AddTone(buf, 0.10f, 0.12f, 200f, 150f, 0.16f, 2);
            return ToClip("sfx_error", buf);
        }

        private static AudioClip BuildReady()
        {
            var buf = Buffer(0.5f);
            AddTone(buf, 0f, 0.22f, 880f, 880f, 0.16f, 0);
            AddTone(buf, 0.12f, 0.30f, 1175f, 1175f, 0.14f, 0);
            return ToClip("sfx_ready", buf);
        }

        private static AudioClip BuildDay()
        {
            var buf = Buffer(0.8f);
            AddTone(buf, 0f, 0.35f, 392f, 392f, 0.14f, 1);
            AddTone(buf, 0.15f, 0.35f, 523f, 523f, 0.14f, 1);
            AddTone(buf, 0.3f, 0.45f, 659f, 659f, 0.13f, 1);
            return ToClip("sfx_day", buf);
        }
    }
}
