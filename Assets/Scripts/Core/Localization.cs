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
            { "hint_empty", "Plant a seed, water it, harvest it — then sell the harvest in the Market!" },
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
                "4. Tap the golden crop to harvest — it goes to your barn.\n" +
                "5. Open the Market and sell for coins!\n\n" +
                "Drag to move the camera, pinch to zoom, double tap to reset." },
            { "tut_ok", "Let's farm!" },
            { "ready", "Ready to harvest" },
            { "no_coins_short", "Need more coins" },

            // --- v1.1 ---
            { "carrot", "Carrot" },
            { "chili", "Chili" },
            { "corn", "Corn" },
            { "rice", "Rice" },
            { "jute", "Jute" },
            { "level", "Lv" },
            { "level_up", "Level up! Lv" },
            { "new_unlock", "New crop unlocked!" },
            { "need_level", "Unlocks at level" },
            { "market", "Market" },
            { "barn", "Barn" },
            { "store_empty", "The barn is empty. Harvest a crop first!" },
            { "sell_all", "Sell all" },
            { "sold_for", "Sold for" },
            { "nothing_to_sell", "Nothing to sell yet" },
            { "total_value", "Barn value" },
            { "buy_land", "Buy land" },
            { "locked_land", "Locked land — tap to buy" },
            { "land_bought", "Land bought!" },
            { "close", "Close" },
            { "market_price", "Today's price" },
            { "plots", "plots" },
            { "vib_on", "Vibration on" },
            { "vib_off", "Vibration off" },
            { "well", "Well" },
            { "sprinkler", "Sprinkler" },
            { "well_bought", "The well is repaired — every seed is watered!" },
            { "sprinkler_bought", "Sprinklers on — crops water themselves each morning!" },
            { "already_bought", "Already built" },
            { "need_well_first", "Repair the well first" },
            { "tool_need_empty", "Pick the PLANT tool for an empty plot" },
            { "tool_need_growing", "Pick the WATER tool for a growing crop" },
            { "tool_need_ready", "Pick the PICK tool for a ripe crop" },
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
            { "hint_empty", "বীজ লাগান, পানি দিন, ফসল তুলুন — তারপর বাজারে বিক্রি করুন!" },
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
                "৪. সোনালি হলে ট্যাপ করুন — ফসল গুদামে জমা হবে।\n" +
                "৫. \"বাজার\" খুলে বিক্রি করুন — কয়েন পাবেন!\n\n" +
                "ড্র্যাগ করে ক্যামেরা সরান, পিঞ্চ করে জুম করুন, ডাবল ট্যাপে রিসেট।" },
            { "tut_ok", "শুরু করি!" },
            { "ready", "তোলার জন্য তৈরি" },
            { "no_coins_short", "আরও কয়েন দরকার" },

            // --- v1.1 ---
            { "carrot", "গাজর" },
            { "chili", "মরিচ" },
            { "corn", "ভুট্টা" },
            { "rice", "ধান" },
            { "jute", "পাট" },
            { "level", "লেভেল" },
            { "level_up", "লেভেল আপ! লেভেল" },
            { "new_unlock", "নতুন ফসল আনলক হয়েছে!" },
            { "need_level", "আনলক হবে লেভেলে" },
            { "market", "বাজার" },
            { "barn", "গুদাম" },
            { "store_empty", "গুদাম খালি। আগে ফসল তুলুন!" },
            { "sell_all", "সব বিক্রি" },
            { "sold_for", "বিক্রি হয়েছে" },
            { "nothing_to_sell", "বিক্রি করার কিছু নেই" },
            { "total_value", "গুদামের মূল্য" },
            { "buy_land", "জমি কিনুন" },
            { "locked_land", "লক করা জমি — কিনতে ট্যাপ করুন" },
            { "land_bought", "নতুন জমি কেনা হয়েছে!" },
            { "close", "বন্ধ" },
            { "market_price", "আজকের দাম" },
            { "plots", "জমি" },
            { "vib_on", "ভাইব্রেশন চালু" },
            { "vib_off", "ভাইব্রেশন বন্ধ" },
            { "well", "কুয়া" },
            { "sprinkler", "স্প্রিংকলার" },
            { "well_bought", "কুয়া মেরামত হয়েছে — বীজ পোতার সাথে সাথেই পানি!" },
            { "sprinkler_bought", "স্প্রিংকলার চালু — প্রতিদিন সকালে নিজে থেকেই পানি!" },
            { "already_bought", "আগেই করা আছে" },
            { "need_well_first", "আগে কুয়া মেরামত করুন" },
            { "tool_need_empty", "খালি জমির জন্য PLANT টুল বেছে নিন" },
            { "tool_need_growing", "বাড়ন্ত ফসলের জন্য WATER টুল নিন" },
            { "tool_need_ready", "পাকা ফসলের জন্য PICK টুল নিন" },
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
