using System;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Central game state: coins, day cycle, selected seed, saving, and the
    /// time-of-day look (sky + sun + ambient light).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int Coins { get; private set; }
        public int Day { get; private set; }
        public int SelectedCropIndex { get; private set; }
        public int TotalHarvests { get; private set; }

        public const float DayLengthSeconds = 90f;

        public float TimeInDay { get; private set; }

        public float DayProgress01
        {
            get { return Mathf.Clamp01(TimeInDay / DayLengthSeconds); }
        }

        public event Action OnStatsChanged;

        private Light _sun;
        private FarmGrid _grid;
        private Material _skyMaterial;
        private float _autoSaveTimer;

        private static readonly Color SkyTopDawn = new Color(0.46f, 0.44f, 0.74f);
        private static readonly Color SkyTopDay = new Color(0.28f, 0.54f, 0.92f);
        private static readonly Color SkyTopDusk = new Color(0.28f, 0.26f, 0.52f);
        private static readonly Color SkyBottomDawn = new Color(0.99f, 0.74f, 0.52f);
        private static readonly Color SkyBottomDay = new Color(0.74f, 0.86f, 0.96f);
        private static readonly Color SkyBottomDusk = new Color(0.97f, 0.55f, 0.38f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Coins = 100;
            Day = 1;
            SelectedCropIndex = 0;
            // Start mid-morning: the first frame should already look sunny.
            TimeInDay = DayLengthSeconds * 0.16f;
        }

        public void Init(FarmGrid grid, Light sun)
        {
            _grid = grid;
            _sun = sun;
            _skyMaterial = RenderSettings.skybox;
            ApplyTimeOfDay();
        }

        // ---------- save ----------

        public void ApplySave(GameSaveData data)
        {
            if (data == null) return;
            Coins = Mathf.Max(0, data.Coins);
            Day = Mathf.Max(1, data.Day);
            SelectedCropIndex = Mathf.Clamp(data.SelectedCrop, 0, CropDatabase.Count - 1);
            TotalHarvests = Mathf.Max(0, data.TotalHarvests);
            TimeInDay = Mathf.Clamp(data.DayTime, 0f, DayLengthSeconds - 0.5f);
            Notify();
        }

        public GameSaveData CollectSave()
        {
            var data = new GameSaveData
            {
                Coins = Coins,
                Day = Day,
                SelectedCrop = SelectedCropIndex,
                TotalHarvests = TotalHarvests,
                DayTime = TimeInDay
            };
            if (_grid != null)
                data.Plots = _grid.CollectSaveData();
            return data;
        }

        public void SaveNow()
        {
            SaveSystem.Save(CollectSave());
        }

        // ---------- loop ----------

        private void Update()
        {
            TimeInDay += Time.deltaTime;
            if (TimeInDay >= DayLengthSeconds)
            {
                NextDay();
                return;
            }

            ApplyTimeOfDay();

            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= 10f)
            {
                _autoSaveTimer = 0f;
                SaveNow();
            }
        }

        private void ApplyTimeOfDay()
        {
            float t = DayProgress01;
            // smooth dawn -> day -> dusk curve
            float daylight = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            if (_skyMaterial != null)
            {
                if (_skyMaterial.HasProperty("_TopColor"))
                    _skyMaterial.SetColor("_TopColor", EvalThree(t, SkyTopDawn, SkyTopDay, SkyTopDusk));
                if (_skyMaterial.HasProperty("_BottomColor"))
                    _skyMaterial.SetColor("_BottomColor", EvalThree(t, SkyBottomDawn, SkyBottomDay, SkyBottomDusk));
            }

            RenderSettings.ambientLight = Color.Lerp(
                new Color(0.38f, 0.36f, 0.44f),
                new Color(0.56f, 0.58f, 0.55f),
                daylight);

            RenderSettings.fogColor = Color.Lerp(
                new Color(0.50f, 0.44f, 0.52f),
                new Color(0.75f, 0.83f, 0.89f),
                daylight);

            if (_sun != null)
            {
                float angle = Mathf.Lerp(18f, 152f, t);
                _sun.transform.rotation = Quaternion.Euler(angle, 40f, 0f);
                // Exposure is deliberately conservative: 1.25 blew the
                // path and pond out to white in v1.0.1.
                _sun.intensity = Mathf.Lerp(0.42f, 1.00f, daylight);
                _sun.color = Color.Lerp(
                    new Color(1f, 0.70f, 0.50f),
                    new Color(1f, 0.965f, 0.90f),
                    daylight);
            }
        }

        private static Color EvalThree(float t, Color a, Color b, Color c)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f)
                return Color.Lerp(a, b, t * 2f);
            return Color.Lerp(b, c, (t - 0.5f) * 2f);
        }

        public void NextDay()
        {
            TimeInDay = DayLengthSeconds * 0.10f;
            Day++;
            if (_grid != null)
                _grid.OnNewDay();
            Notify();
            SaveNow();
            if (SfxManager.Instance != null) SfxManager.Instance.PlayNewDay();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("new_day") + " " + Day);
        }

        // ---------- economy ----------

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Notify();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount == 0) return;
            Coins += amount;
            Notify();
        }

        public void RegisterHarvest(int coins)
        {
            TotalHarvests++;
            AddCoins(coins);
        }

        public void SelectCrop(int index)
        {
            SelectedCropIndex = Mathf.Clamp(index, 0, CropDatabase.Count - 1);
            Notify();
        }

        private void Notify()
        {
            if (OnStatsChanged != null) OnStatsChanged();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }
    }
}
