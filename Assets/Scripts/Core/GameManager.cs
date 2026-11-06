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
        public const int WellCost = 350;
        public const int SprinklerCost = 800;

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

        /// <summary>0 = auto (tap does the sensible thing), 1 plant, 2 water, 3 harvest.</summary>
        public int ToolIndex { get; private set; }

        /// <summary>A repaired well waters every seed the moment it is planted.</summary>
        public bool HasWell { get; private set; }

        /// <summary>Sprinklers re-water every growing plot at the start of each day.</summary>
        public bool HasSprinkler { get; private set; }

        /// <summary>v1.6.0: today's little task — harvest N crops for a coin bonus.</summary>
        public int DailyGoalTarget { get; private set; }
        public int DailyGoalProgress { get; private set; }
        public bool DailyGoalDone { get; private set; }

        /// <summary>v1.6.0: a rainy day waters every growing plot for free.</summary>
        public bool IsRainyDay { get; private set; }

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
            RollNewDay();
        }

        public void Init(FarmGrid grid, Light sun)
        {
            _grid = grid;
            _sun = sun;
            _skyMaterial = RenderSettings.skybox;
            ApplyTimeOfDay();
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetRain(IsRainyDay);
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

            ToolIndex = Mathf.Clamp(data.Tool, 0, 3);
            HasWell = data.Well;
            HasSprinkler = data.Sprinkler;

            // --- v1.6.0 daily goal & weather (old saves land on 0 = "roll a new one") ---
            if (data.GoalTarget > 0)
            {
                DailyGoalTarget = data.GoalTarget;
                DailyGoalProgress = Mathf.Clamp(data.GoalProgress, 0, data.GoalTarget);
                DailyGoalDone = data.GoalDone;
                IsRainyDay = data.Rainy;
            }
            else
            {
                RollNewDay();
            }

            SelectedCropIndex = Mathf.Clamp(data.SelectedCrop, 0, CropDatabase.Count - 1);
            if (!IsCropUnlocked(SelectedCropIndex)) SelectedCropIndex = 0;

            RecomputeMarket();
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetRain(IsRainyDay);
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
                Tool = ToolIndex,
                Well = HasWell,
                Sprinkler = HasSprinkler,
                SavedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                GoalTarget = DailyGoalTarget,
                GoalProgress = DailyGoalProgress,
                GoalDone = DailyGoalDone,
                Rainy = IsRainyDay,
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
            // smooth dawn -> day -> dusk curve. v1.3.0: the trough now bottoms
            // out at 0.55 instead of 0, so dawn and dusk stay readable instead
            // of turning the props into black silhouettes.
            float daylight = Mathf.Lerp(0.55f, 1f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));

            if (_skyMaterial != null)
            {
                if (_skyMaterial.HasProperty("_TopColor"))
                    _skyMaterial.SetColor("_TopColor", EvalThree(t, SkyTopDawn, SkyTopDay, SkyTopDusk));
                if (_skyMaterial.HasProperty("_BottomColor"))
                    _skyMaterial.SetColor("_BottomColor", EvalThree(t, SkyBottomDawn, SkyBottomDay, SkyBottomDusk));
            }

            // v1.3.0: the dusk end used to drop to almost night (0.40 ambient,
            // 0.48 sun) and every prop read as a black silhouette. The floor is
            // lifted and the player's light setting scales the whole rig.
            float lightMul = LookSettings.Multiplier;

            RenderSettings.ambientLight = Color.Lerp(
                new Color(0.52f, 0.50f, 0.58f),
                new Color(0.62f, 0.65f, 0.70f),
                daylight) * lightMul;

            RenderSettings.fogColor = Color.Lerp(
                new Color(0.52f, 0.48f, 0.55f),
                new Color(0.74f, 0.79f, 0.82f),
                daylight);

            // v1.6.0: a rainy day reads as soft, grey and close.
            float rainDim = IsRainyDay ? 1f : 0f;
            if (IsRainyDay)
            {
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight,
                    new Color(0.46f, 0.50f, 0.55f), 0.55f);
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor,
                    new Color(0.56f, 0.60f, 0.65f), 0.6f);
            }
            RenderSettings.fogEndDistance = Mathf.Lerp(190f, 115f, rainDim);

            if (_sun != null)
            {
                float angle = Mathf.Lerp(18f, 152f, t);
                _sun.transform.rotation = Quaternion.Euler(angle, 40f, 0f);
                // Exposure is deliberately conservative: 1.25 blew the
                // path and pond out to white in v1.0.1.
                _sun.intensity = Mathf.Lerp(0.68f, 1.05f, daylight) * lightMul *
                                 (IsRainyDay ? 0.45f : 1f);
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

        /// <summary>
        /// v1.6.0: picks today's weather and today's little task. Day one is
        /// always calm and sunny so a new player is never rained on.
        /// </summary>
        private void RollNewDay()
        {
            DailyGoalTarget = 3 + Mathf.Min(5, Level);
            DailyGoalProgress = 0;
            DailyGoalDone = false;
            IsRainyDay = Day > 1 && UnityEngine.Random.value < 0.3f;
        }

        public void NextDay()
        {
            TimeInDay = DayLengthSeconds * 0.10f;
            Day++;

            if (_grid != null) _grid.OnNewDay();

            RollNewDay();

            // Rain does the watering (the sprinkler does it on dry days).
            if (_grid != null && (IsRainyDay || HasSprinkler)) _grid.WaterAll();
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetRain(IsRainyDay);

            RecomputeMarket();
            Notify();
            SaveNow();

            if (SfxManager.Instance != null) SfxManager.Instance.PlayNewDay();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast(Localization.T("new_day") + " " + Day);
                if (IsRainyDay)
                    UIManager.Instance.ShowToast(Localization.T("rainy_day"));
            }
        }

        /// <summary>Goal progress, awarded once per day.</summary>
        private void AdvanceDailyGoal()
        {
            if (DailyGoalDone || DailyGoalTarget <= 0) return;

            DailyGoalProgress++;
            if (DailyGoalProgress < DailyGoalTarget) return;

            DailyGoalDone = true;
            int reward = 12 * DailyGoalTarget;
            AddCoins(reward);

            if (SfxManager.Instance != null) SfxManager.Instance.PlayReady();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(
                    Localization.T("goal_done") + "  +" + reward, false, true);
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
            AdvanceDailyGoal();

            if (!_harvestHintShown && UIManager.Instance != null)
            {
                _harvestHintShown = true;
                UIManager.Instance.ShowToast(Localization.T("barn") + " → " + Localization.T("market"));
            }

            Notify();
        }

        public void SelectTool(int index)
        {
            ToolIndex = Mathf.Clamp(index, 0, 3);
            Notify();
            SaveNow();
        }

        public bool BuyWell()
        {
            if (HasWell) return false;
            if (!SpendCoins(WellCost))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("not_enough_coins"), true);
                return false;
            }

            HasWell = true;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayCoin();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("well_bought"), false, true);
            Notify();
            SaveNow();
            return true;
        }

        public bool BuySprinkler()
        {
            if (HasSprinkler) return false;
            if (!SpendCoins(SprinklerCost))
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("not_enough_coins"), true);
                return false;
            }

            HasSprinkler = true;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayCoin();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("sprinkler_bought"), false, true);
            Notify();
            SaveNow();
            return true;
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
