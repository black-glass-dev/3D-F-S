using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    [System.Serializable]
    public class PlotSaveData
    {
        public int State;      // 0 empty, 1 growing, 2 ready
        public int CropIndex;
        public float Progress; // 0..1
        public bool Watered;
        public bool Locked;    // land that has not been bought yet
    }

    [System.Serializable]
    public class GameSaveData
    {
        public int Coins = 120;
        public int Day = 1;
        public int SelectedCrop = 0;
        public int TotalHarvests = 0;
        public float DayTime = 0f;

        // --- v1.1 ---
        public int Xp = 0;
        public int Level = 1;
        /// <summary>Harvested crops waiting in the barn, one slot per crop.</summary>
        public List<int> Store = new List<int>();
        public int TotalEarned = 0;

        // --- v1.2 ---
        public int Tool = 0;
        public bool Well = false;
        public bool Sprinkler = false;

        public List<PlotSaveData> Plots = new List<PlotSaveData>();
    }

    /// <summary>
    /// Saves the whole farm as JSON in PlayerPrefs (works on Android).
    /// The key stays "v1" on purpose: v1.0.x saves load fine, the new v1.1
    /// fields simply arrive as their defaults.
    /// </summary>
    public static class SaveSystem
    {
        private const string Key = "farmsim_save_v1";

        public static void Save(GameSaveData data)
        {
            if (data == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static GameSaveData TryLoad()
        {
            if (!PlayerPrefs.HasKey(Key))
                return null;
            try
            {
                return JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(Key));
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
