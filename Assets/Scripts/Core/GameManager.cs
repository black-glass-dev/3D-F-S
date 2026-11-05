using System;
using UnityEngine;

namespace FarmSim
{
    /// <summary>
    /// Central game state: coins, XP/levels, the barn store, daily market
    /// prices, the day cycle, saving, and the time-of-day look
    /// (sky + sun + ambient light).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const float DayLengthSeconds = 90f;
        public const int StartCoins = 120;
        public const int StartPlots = 24;
        public const int MaxPlots = 36;

        public int Coins { get; private set; }
        public int Day { get; private set; }
        public int SelectedCropIndex { get; private set; }
        public int TotalHarvests { get; private set; }
        public int TotalEarned { get; private set; }

        // --- progression ---
        public int Xp { get; private set; }
        public int Level { get; private set; }

        /// <summary>Today's market price per crop (fluctuates every day).</summary>
        public int[] MarketPrice { get; private set; }

        /// <summary>Harvested crops waiting in the barn, indexed like CropDatabase.</summary>
        public int[] Store { get; private set; }

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
        private bool _harvestHintShown;

        // Muted, filmic sky palette — no candy colours.
        private static readonly Color SkyTopDawn = new Color(0.34f, 0.33f, 0.46f);
        private static readonly Color SkyTopDay = new Color(0.30f, 0.44f, 0.62f);
        private static readonly Color SkyTopDusk = new Color(0.28f, 0.25f, 0.36f);
        private static readonly Color SkyBottomDawn = new Color(0.86f, 0.62f, 0.46f);
        private static readonly Color SkyBottomDay = new Color(0.74f, 0.78f, 0.78f);
        private static readonly Color SkyBottomDusk = new Color(0.74f, 0.44f, 0.34f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Coins = StartCoins;
            Day = 1;
            SelectedCropIndex = 0;
            Level = 1;
            Xp = 0;
            Store = new int[CropDatabase.Count];
            MarketPrice = new int[CropDatabase.Count];

            // Start mid-morning: the first frame should already look sunny.
            TimeInDay = DayLengthSeconds * 0.16f;
            RecomputeMarket();
        }

        public void Init(FarmGrid grid, Light sun)
        {
            _grid = grid;
            _sun = sun;
            _skyMaterial = RenderSettings.skybox;
            ApplyTimeOfDay();
        }

        // ---------- progression ----------

        /// <summary>XP needed to reach the next level from the current one.</summary>
        public int XpToNextLevel
        {
            get { return 40 + (Level - 1) * 65; }
        }

        public float LevelProgress01
        {
            get
            {
                int need = XpToNextLevel;
                return need <= 0 ? 0f : Mathf.Clamp01(Xp / (float)need);
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;

            int guard = 0;
            while (Xp >= XpToNextLevel && guard++ < 50)
            {
                Xp -= XpToNextLevel;
                Level++;

                int reward = 10 + Level * 5;
                Coins += reward;

                if (SfxManager.Instance != null) SfxManager.Instance.PlayNewDay();
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast(
                        Localization.T("level_up") + " " + Level + "  ·  +" + reward, false, true);
                }
                AnnounceUnlocks();
            }

            Notify();
        }

        private void AnnounceUnlocks()
        {
            for (int i = 0; i < CropDatabase.Count; i++)
            {
                var crop = CropDatabase.Get(i);
                if (crop.UnlockLevel == Level)
                {
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowToast(crop.DisplayName + " — " + Localization.T("new_unlock"));
                }
            }
        }

        public bool IsCropUnlocked(int index)
        {
            return CropDatabase.Get(index).UnlockLevel <= Level;
        }

        // ---------- barn & market ----------

        public int StoredOf(int cropIndex)
        {
            if (Store == null || cropIndex < 0 || cropIndex >= Store.Length) return 0;
            return Store[cropIndex];
        }

        public int StoredTotal
        {
            get
            {
                if (Store == null) return 0;
                int total = 0;
                for (int i = 0; i < Store.Length; i++) total += Store[i];
                return total;
            }
        }

        public int StoreValue
        {
            get
            {
                if (Store == null) return 0;
                int total = 0;
                for (int i = 0; i < Store.Length; i++) total += Store[i] * MarketPrice[i];
                return total;
            }
        }

        /// <summary>Sells every stored unit of one crop at today's price.</summary>
        public int SellCrop(int cropIndex)
        {
            if (Store == null || cropIndex < 0 || cropIndex >= Store.Length) return 0;
            int count = Store[cropIndex];
            if (count <= 0) return 0;

            int earned = count * MarketPrice[cropIndex];
            Store[cropIndex] = 0;
            Coins += earned;
            TotalEarned += earned;
            Notify();
            return earned;
        }

        public int SellAll()
        {
            int earned = 0;
            for (int i = 0; i < Store.Length; i++) earned += SellCrop(i);
            if (earned > 0)
            {
                SaveNow();
                Notify();
            }
            return earned;
        }

        /// <summary>Daily price: base price x a smooth 0.85 .. 1.20 factor.</summary>
        private void RecomputeMarket()
        {
            if (MarketPrice == null) MarketPrice = new int[CropDatabase.Count];
            for (int i = 0; i < MarketPrice.Length; i++)
            {
                var crop = CropDatabase.Get(i);
                float noise = Mathf.PerlinNoise(Day * 0.37f + i * 1.71f, i * 0.93f + 2.3f);
                float factor = 0.85f + noise * 0.35f;
                MarketPrice[i] = Mathf.Max(1, Mathf.RoundToInt(crop.SellPrice * factor));
            }
        }

        // ---------- land ----------

        public int UnlockedPlots
        {
            get { return _grid != null ? _grid.UnlockedCount : StartPlots; }
        }

        public int LandCost
        {
            get { return 180 + 140 * Mathf.Max(0, UnlockedPlots - StartPlots); }
        }

        public bool BuyPlot(int plotIndex)
        {
            if (_grid == null || !_grid.IsLocked(plotIndex)) return false;

            int cost = LandCost;
            if (!SpendCoins(cost))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("not_enough_coins"), true);
                return false;
            }

            _grid.Unlock(plotIndex);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("land_bought") + "  ·  -" + cost, false, true);
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();

            SaveNow();
            Notify();
            return true;
        }

        // ---------- save ----------

        public void ApplySave(GameSaveData data)
        {
            if (data == null) return;

            Coins = Mathf.Max(0, data.Coins);
            Day = Mathf.Max(1, data.Day);
            TotalHarvests = Mathf.Max(0, data.TotalHarvests);
            TotalEarned = Mathf.Max(0, data.TotalEarned);
            TimeInDay = Mathf.Clamp(data.DayTime, 0f, DayLengthSeconds - 0.5f);

            Level = Mathf.Max(1, data.Level);
            Xp = Mathf.Max(0, data.Xp);

            if (Store == null || Store.Length != CropDatabase.Count)
                Store = new int[CropDatabase.Count];

            if (data.Store != null)
            {
                int count = Mathf.Min(data.Store.Count, Store.Length);
                for (int i = 0; i < count; i++) Store[i] = Mathf.Max(0, data.Store[i]);
            }

            SelectedCropIndex = Mathf.Clamp(data.SelectedCrop, 0, CropDatabase.Count - 1);
            if (!IsCropUnlocked(SelectedCropIndex)) SelectedCropIndex = 0;

            RecomputeMarket();
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
                DayTime = TimeInDay,
                Xp = Xp,
                Level = Level,
                TotalEarned = TotalEarned,
                Store = new System.Collections.Generic.List<int>(Store)
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
                new Color(0.32f, 0.31f, 0.38f),
                new Color(0.42f, 0.46f, 0.52f),
                daylight);

            RenderSettings.fogColor = Color.Lerp(
                new Color(0.40f, 0.36f, 0.42f),
                new Color(0.70f, 0.75f, 0.78f),
                daylight);

            if (_sun != null)
            {
                float angle = Mathf.Lerp(18f, 152f, t);
                _sun.transform.rotation = Quaternion.Euler(angle, 40f, 0f);
                // Exposure is deliberately conservative: 1.25 blew the
                // path and pond out to white in v1.0.1.
                _sun.intensity = Mathf.Lerp(0.34f, 0.95f, daylight);
                _sun.color = Color.Lerp(
                    new Color(1f, 0.66f, 0.44f),
                    new Color(1f, 0.95f, 0.87f),
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

            RecomputeMarket();
            Notify();
            SaveNow();

            if (SfxManager.Instance != null) SfxManager.Instance.PlayNewDay();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("new_day") + " " + Day);
        }

        // ---------- economy ----------

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            Notify();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount == 0) return;
            Coins += amount;
            TotalEarned += Mathf.Max(0, amount);
            Notify();
        }

        /// <summary>A harvest goes into the barn and grants XP (coins come from selling).</summary>
        public void RegisterHarvest(int cropIndex)
        {
            if (Store == null) Store = new int[CropDatabase.Count];
            int index = Mathf.Clamp(cropIndex, 0, Store.Length - 1);

            Store[index]++;
            TotalHarvests++;
            AddXp(CropDatabase.Get(index).Xp);

            if (!_harvestHintShown && UIManager.Instance != null)
            {
                _harvestHintShown = true;
                UIManager.Instance.ShowToast(Localization.T("barn") + " → " + Localization.T("market"));
            }

            Notify();
        }

        public void SelectCrop(int index)
        {
            if (!IsCropUnlocked(index))
            {
                var locked = CropDatabase.Get(index);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast(
                        Localization.T("need_level") + " " + locked.UnlockLevel, true);
                }
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                return;
            }

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
