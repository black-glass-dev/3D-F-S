using UnityEngine;

namespace FarmSim
{
    [System.Serializable]
    public class CropDef
    {
        public string Id;
        public string NameKey;
        public float GrowSeconds;
        public int SeedCost;
        public int SellPrice;
        public Color SproutColor;
        public Color RipeColor;

        public string DisplayName
        {
            get { return Localization.T(NameKey); }
        }
    }

    /// <summary>All crops available in the MVP, defined in code (no assets needed).</summary>
    public static class CropDatabase
    {
        public static readonly CropDef[] Crops =
        {
            new CropDef
            {
                Id = "wheat", NameKey = "wheat",
                GrowSeconds = 15f, SeedCost = 5, SellPrice = 14,
                SproutColor = new Color(0.42f, 0.56f, 0.32f),
                RipeColor = new Color(0.82f, 0.66f, 0.30f)
            },
            new CropDef
            {
                Id = "tomato", NameKey = "tomato",
                GrowSeconds = 30f, SeedCost = 12, SellPrice = 34,
                SproutColor = new Color(0.34f, 0.48f, 0.28f),
                RipeColor = new Color(0.70f, 0.24f, 0.19f)
            },
            new CropDef
            {
                Id = "pumpkin", NameKey = "pumpkin",
                GrowSeconds = 50f, SeedCost = 25, SellPrice = 75,
                SproutColor = new Color(0.36f, 0.50f, 0.26f),
                RipeColor = new Color(0.80f, 0.46f, 0.16f)
            },
        };

        public static CropDef Get(int index)
        {
            if (index < 0 || index >= Crops.Length) index = 0;
            return Crops[index];
        }

        public static int Count
        {
            get { return Crops.Length; }
        }
    }
}
