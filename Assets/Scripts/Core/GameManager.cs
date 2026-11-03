using System;
using UnityEngine;

namespace FarmSim
{
    /// <summary>Central game state: coins, day cycle, selected seed, saving.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int Coins { get; private set; }
        public int Day { get; private set; }
        public int SelectedCropIndex { get; private set; }

        public const float DayLengthSeconds = 90f;
        public float TimeInDay { get; private set; }

        public event Action OnStatsChanged;

        private Light _sun;
        private FarmGrid _grid;
        private float _autoSaveTimer;

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
        }

        public void Init(FarmGrid grid, Light sun)
        {
            _grid = grid;
            _sun = sun;
        }

        public void ApplySave(GameSaveData data)
        {
            Coins = data.Coins;
            Day = data.Day;
            SelectedCropIndex = Mathf.Clamp(data.SelectedCrop, 0, CropDatabase.Count - 1);
            Notify();
        }

        public GameSaveData CollectSave()
        {
            var data = new GameSaveData
            {
                Coins = Coins,
                Day = Day,
                SelectedCrop = SelectedCropIndex
            };
            if (_grid != null)
                data.Plots = _grid.CollectSaveData();
            return data;
        }

        public void SaveNow()
        {
            SaveSystem.Save(CollectSave());
        }

        private void Update()
        {
            TimeInDay += Time.deltaTime;
            if (TimeInDay >= DayLengthSeconds)
                NextDay();

            UpdateSun();

            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= 10f)
            {
                _autoSaveTimer = 0f;
                SaveNow();
            }
        }

        private void UpdateSun()
        {
            if (_sun == null) return;
            float t = Mathf.Clamp01(TimeInDay / DayLengthSeconds); // 0 dawn -> 1 dusk
            float angle = Mathf.Lerp(35f, 145f, t);
            _sun.transform.rotation = Quaternion.Euler(angle, 35f, 0f);
            _sun.intensity = Mathf.Lerp(0.85f, 1.15f, Mathf.Sin(t * Mathf.PI));
            _sun.color = Color.Lerp(new Color(1f, 0.85f, 0.65f), Color.white, Mathf.Sin(t * Mathf.PI));
        }

        public void NextDay()
        {
            TimeInDay = 0f;
            Day++;
            if (_grid != null)
                _grid.OnNewDay();
            Notify();
            SaveNow();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(Localization.T("new_day") + " " + Day);
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Notify();
            return true;
        }

        public void AddCoins(int amount)
        {
            Coins += amount;
            Notify();
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
