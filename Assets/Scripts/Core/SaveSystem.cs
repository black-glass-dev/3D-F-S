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
    }

    [System.Serializable]
    public class GameSaveData
    {
        public int Coins = 100;
        public int Day = 1;
        public int SelectedCrop = 0;
        public List<PlotSaveData> Plots = new List<PlotSaveData>();
    }

    /// <summary>Saves the whole farm as JSON in PlayerPrefs (works on Android).</summary>
    public static class SaveSystem
    {
        private const string Key = "farmsim_save_v1";

        public static void Save(GameSaveData data)
        {
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
        }
    }
}
