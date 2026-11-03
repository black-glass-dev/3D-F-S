using System.Collections.Generic;
using UnityEngine;

namespace FarmSim
{
    public enum GameLanguage
    {
        English = 0,
        Bangla = 1
    }

    /// <summary>
    /// Simple two-language (English / Bangla) localization table.
    /// NOTE: Unity's built-in UI text does not shape complex Bengali
    /// conjunct characters perfectly on every device. English is the
    /// default; Bangla can be switched from the in-game Lang button.
    /// </summary>
    public static class Localization
    {
        public static GameLanguage Current = GameLanguage.English;

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "coins", "Coins" },
            { "day", "Day" },
            { "next_day", "Next Day >>" },
            { "lang_button", "বাংলা" },
            { "wheat", "Wheat" },
            { "tomato", "Tomato" },
            { "pumpkin", "Pumpkin" },
            { "cost", "Cost" },
            { "sell", "Sell" },
            { "hint_empty", "Tap a soil plot to plant the selected seed." },
            { "hint_growing", "Tap a growing crop to water it (grows faster!)." },
            { "hint_ready", "Tap a golden crop to harvest and sell it!" },
            { "not_enough_coins", "Not enough coins!" },
            { "planted", "Planted!" },
            { "watered", "Watered! Growing faster..." },
            { "already_watered", "Already watered. Wait for it to grow." },
            { "harvested", "Harvested! +" },
            { "new_day", "A new day begins!" },
            { "select_seed", "Select seed:" },
        };

        private static readonly Dictionary<string, string> Bn = new Dictionary<string, string>
        {
            { "coins", "কয়েন" },
            { "day", "দিন" },
            { "next_day", "পরের দিন >>" },
            { "lang_button", "English" },
            { "wheat", "গম" },
            { "tomato", "টমেটো" },
            { "pumpkin", "কুমড়া" },
            { "cost", "দাম" },
            { "sell", "বিক্রি" },
            { "hint_empty", "বীজ লাগাতে মাটির জমিতে ট্যাপ করুন।" },
            { "hint_growing", "পানি দিতে বাড়ন্ত ফসলে ট্যাপ করুন (দ্রুত বাড়বে!)।" },
            { "hint_ready", "ফসল তুলে বিক্রি করতে সোনালি ফসলে ট্যাপ করুন!" },
            { "not_enough_coins", "যথেষ্ট কয়েন নেই!" },
            { "planted", "বীজ লাগানো হয়েছে!" },
            { "watered", "পানি দেওয়া হয়েছে! দ্রুত বাড়ছে..." },
            { "already_watered", "আগেই পানি দেওয়া হয়েছে। অপেক্ষা করুন।" },
            { "harvested", "ফসল তোলা হয়েছে! +" },
            { "new_day", "নতুন দিন শুরু!" },
            { "select_seed", "বীজ বেছে নিন:" },
        };

        public static string T(string key)
        {
            var table = Current == GameLanguage.Bangla ? Bn : En;
            string value;
            if (table.TryGetValue(key, out value))
                return value;
            if (En.TryGetValue(key, out value))
                return value;
            return key;
        }

        public static void Toggle()
        {
            Current = Current == GameLanguage.English ? GameLanguage.Bangla : GameLanguage.English;
            PlayerPrefs.SetInt("farmsim_lang", (int)Current);
            PlayerPrefs.Save();
        }

        public static void LoadSaved()
        {
            Current = (GameLanguage)PlayerPrefs.GetInt("farmsim_lang", 0);
        }
    }
}
