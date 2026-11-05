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
        public int UnlockLevel;
        public Color SproutColor;
        public Color RipeColor;

        public string DisplayName
        {
            get { return Localization.T(NameKey); }
        }

        /// <summary>XP earned by harvesting this crop once.</summary>
        public int Xp
        {
            get { return Mathf.Max(3, Mathf.RoundToInt(SellPrice * 0.45f)); }
        }
    }

    /// <summary>
    /// All crops, defined in code (no assets needed). v1.1 doubles the roster:
    /// cheap fast crops stay relevant for XP, expensive slow crops pay the bills.
    /// Later crops unlock with levels, which gives the first hours a clear goal.
    /// </summary>
    public static class CropDatabase
    {
        public static readonly CropDef[] Crops =
        {
            new CropDef
            {
                Id = "wheat", NameKey = "wheat",
                GrowSeconds = 15f, SeedCost = 5, SellPrice = 14, UnlockLevel = 1,
                SproutColor = new Color(0.42f, 0.56f, 0.32f),
                RipeColor = new Color(0.82f, 0.66f, 0.30f)
            },
            new CropDef
            {
                Id = "tomato", NameKey = "tomato",
                GrowSeconds = 30f, SeedCost = 12, SellPrice = 34, UnlockLevel = 1,
                SproutColor = new Color(0.34f, 0.48f, 0.28f),
                RipeColor = new Color(0.70f, 0.24f, 0.19f)
            },
            new CropDef
            {
                Id = "pumpkin", NameKey = "pumpkin",
                GrowSeconds = 50f, SeedCost = 25, SellPrice = 75, UnlockLevel = 2,
                SproutColor = new Color(0.36f, 0.50f, 0.26f),
                RipeColor = new Color(0.80f, 0.46f, 0.16f)
            },
            new CropDef
            {
                Id = "carrot", NameKey = "carrot",
                GrowSeconds = 26f, SeedCost = 18, SellPrice = 50, UnlockLevel = 3,
                SproutColor = new Color(0.38f, 0.54f, 0.30f),
                RipeColor = new Color(0.78f, 0.42f, 0.16f)
            },
            new CropDef
            {
                Id = "chili", NameKey = "chili",
                GrowSeconds = 38f, SeedCost = 30, SellPrice = 88, UnlockLevel = 4,
                SproutColor = new Color(0.32f, 0.46f, 0.26f),
                RipeColor = new Color(0.64f, 0.18f, 0.14f)
            },
            new CropDef
            {
                Id = "corn", NameKey = "corn",
                GrowSeconds = 55f, SeedCost = 45, SellPrice = 130, UnlockLevel = 5,
                SproutColor = new Color(0.40f, 0.56f, 0.30f),
                RipeColor = new Color(0.84f, 0.72f, 0.34f)
            },
            new CropDef
            {
                Id = "rice", NameKey = "rice",
                GrowSeconds = 70f, SeedCost = 60, SellPrice = 180, UnlockLevel = 6,
                SproutColor = new Color(0.44f, 0.58f, 0.34f),
                RipeColor = new Color(0.72f, 0.68f, 0.46f)
            },
            new CropDef
            {
                Id = "jute", NameKey = "jute",
                GrowSeconds = 85f, SeedCost = 80, SellPrice = 245, UnlockLevel = 8,
                SproutColor = new Color(0.36f, 0.52f, 0.28f),
                RipeColor = new Color(0.52f, 0.60f, 0.28f)
            }
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

        public static int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < Crops.Length; i++)
            {
                if (Crops[i].Id == id) return i;
            }
            return -1;
        }
    }
}
