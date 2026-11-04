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
    /// Two-language (English / Bangla) text table. Bangla is rendered with the
    /// bundled Hind Siliguri font, so it shows correctly on every device
    /// (the old built-in font had no Bangla glyphs at all).
    /// </summary>
    public static class Localization
    {
        public static GameLanguage Current = GameLanguage.English;

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "coins", "Coins" },
            { "day", "Day" },
            { "next_day", "Next Day" },
            { "lang_button", "বাংলা" },
            { "wheat", "Wheat" },
            { "tomato", "Tomato" },
            { "pumpkin", "Pumpkin" },
            { "cost", "Cost" },
            { "sell", "Sell" },
            { "hint_empty", "Tap a soil plot to plant. Tap a growing crop to water it. Tap a golden crop to harvest!" },
            { "hint_growing", "Tap a growing crop to water it (grows faster!)" },
            { "hint_ready", "Tap a golden crop to harvest!" },
            { "not_enough_coins", "Not enough coins!" },
            { "planted", "Planted" },
            { "watered", "Watered! Growing faster" },
            { "already_watered", "Already watered" },
            { "harvested", "Harvested! +" },
            { "new_day", "A new day begins! Day" },
            { "select_seed", "Seeds" },
            { "tut_title", "Welcome to your farm" },
            { "tut_body",
                "1. Pick a seed below.\n" +
                "2. Tap a soil plot to plant it.\n" +
                "3. Tap the growing crop to water it — it grows 1.8x faster.\n" +
                "4. Tap the golden crop to harvest and earn coins.\n\n" +
                "Drag to move the camera, pinch to zoom, double tap to reset." },
            { "tut_ok", "Let's farm!" },
            { "ready", "Ready to harvest" },
            { "no_coins_short", "Need more coins" }
        };

        private static readonly Dictionary<string, string> Bn = new Dictionary<string, string>
        {
            { "coins", "কয়েন" },
            { "day", "দিন" },
            { "next_day", "পরের দিন" },
            { "lang_button", "English" },
            { "wheat", "গম" },
            { "tomato", "টমেটো" },
            { "pumpkin", "কুমড়া" },
            { "cost", "দাম" },
            { "sell", "বিক্রি" },
            { "hint_empty", "মাটিতে ট্যাপ করে বীজ লাগান। বাড়ন্ত ফসলে ট্যাপ করে পানি দিন। সোনালি ফসলে ট্যাপ করে ফসল তুলুন!" },
            { "hint_growing", "পানি দিতে বাড়ন্ত ফসলে ট্যাপ করুন — দ্রুত বাড়বে!" },
            { "hint_ready", "ফসল তুলতে সোনালি ফসলে ট্যাপ করুন!" },
            { "not_enough_coins", "যথেষ্ট কয়েন নেই!" },
            { "planted", "বীজ লাগানো হয়েছে" },
            { "watered", "পানি দেওয়া হয়েছে! দ্রুত বাড়ছে" },
            { "already_watered", "আগেই পানি দেওয়া হয়েছে" },
            { "harvested", "ফসল তোলা হয়েছে! +" },
            { "new_day", "নতুন দিন শুরু! দিন" },
            { "select_seed", "বীজ" },
            { "tut_title", "আপনার খামারে স্বাগতম" },
            { "tut_body",
                "১. নিচ থেকে একটি বীজ বেছে নিন।\n" +
                "২. মাটির জমিতে ট্যাপ করে বীজ লাগান।\n" +
                "৩. বাড়ন্ত ফসলে ট্যাপ করুন — পানি পেলে ১.৮ গুণ দ্রুত বাড়ে।\n" +
                "৪. সোনালি হয়ে গেলে ট্যাপ করুন — কয়েন পাবেন।\n\n" +
                "ড্র্যাগ করে ক্যামেরা সরান, পিঞ্চ করে জুম করুন, ডাবল ট্যাপে রিসেট।" },
            { "tut_ok", "শুরু করি!" },
            { "ready", "তোলার জন্য তৈরি" },
            { "no_coins_short", "আরও কয়েন দরকার" }
        };

        public static string T(string key)
        {
            if (key == null) return string.Empty;
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
