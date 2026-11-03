# 🌾 3D Farm Simulator — মোবাইল ফার্মিং সিমুলেশন গেম

**Unity (2022.3 LTS) দিয়ে তৈরি 2.5D Isometric ফার্মিং সিমুলেশন গেম — Android মোবাইলের জন্য।**
*English summary below.*

> স্টাইল সিদ্ধান্ত: Hay Day-এর মতো **2.5D আইসোমেট্রিক** — 3D লো-পলি গ্রাফিক্স, কিন্তু ফিক্সড ক্যামেরা। দেখতে প্রিমিয়াম, মোবাইলে হালকা ও মসৃণ, টাচ কন্ট্রোল সহজ।

---

## 🎮 গেমে যা আছে (v1.0 MVP)

- 🟫 **6×4 = 24টি জমির প্লট** — ট্যাপ করে চাষ করুন
- 🌱 **3টি ফসল:** গম (15s), টমেটো (30s), কুমড়া (50s) — প্রতিটার আলাদা দাম ও লাভ
- 💧 **পানি দেওয়ার সিস্টেম** — পানি দিলে ফসল 1.8× দ্রুত বাড়ে
- ✨ **বৃদ্ধির ৩ ধাপ** — চারা → বাড়ন্ত → সোনালি পাকা ফসল (ববিং অ্যানিমেশনসহ)
- 🪙 **কয়েন অর্থনীতি** — বীজ কিনুন, ফসল বিক্রি করে লাভ করুন
- 🌅 **দিন-রাত চক্র** — 90 সেকেন্ডে এক দিন, সূর্য ঘোরে; "পরের দিন" বোতামে ঘুমিয়ে নতুন দিন
- 💾 **অটো-সেভ** — গেম বন্ধ করলেও ফার্ম যেমন ছিল তেমনই থাকবে
- 🌐 **দ্বিভাষিক UI** — বাংলা / English (উপরের ডানদিকের বোতামে বদলান)
- 📱 **মোবাইল টাচ কন্ট্রোল** — ড্র্যাগ করে ক্যামেরা সরান, পিঞ্চ করে জুম করুন
- 🏠 লো-পলি পরিবেশ — বাড়ি, গাছ, বেড়া (কোনো বাইরের অ্যাসেট ছাড়াই, সবকিছু কোড দিয়ে তৈরি)

### কীভাবে খেলবেন
1. নিচের বার থেকে বীজ বেছে নিন → 2. খালি জমিতে ট্যাপ করুন (বীজ লাগবে) → 3. বাড়ন্ত ফসলে ট্যাপ করুন (পানি) → 4. সোনালি ফসলে ট্যাপ করুন (তুলে বিক্রি!)

---

## 📲 APK বানানো (GitHub Actions)

### ধাপ ১ — একবারের জন্য Unity লাইসেন্স Secret সেটআপ (আপনাকে করতে হবে, ~10 মিনিট)

> Unity দিয়ে ক্লাউডে বিল্ড করতে লাইসেন্স লাগে। এটা ছাড়া বিল্ড চলবে না — এটা Unity-র নিয়ম, এড়ানোর উপায় নেই। **আপনার পাসওয়ার্ড/লাইসেন্স কাউকে (আমাকেও না) দেওয়ার দরকার নেই — শুধু GitHub Secrets-এ বসাবেন।**

1. [id.unity.com](https://id.unity.com)-এ ফ্রি Unity অ্যাকাউন্ট খুলুন (যদি না থাকে)।
2. এই রিপোতে যান: **Actions → "Unity License Activation (One-Time)" → Run workflow**।
3. রান শেষ হলে নিচের **Artifacts** থেকে `.alf` ফাইলটি ডাউনলোড করুন।
4. [license.unity3d.com/manual](https://license.unity3d.com/manual)-এ গিয়ে `.alf` ফাইল আপলোড করুন → একটি `.ulf` ফাইল ডাউনলোড হবে।
5. `.ulf` ফাইলটি টেক্সট এডিটরে খুলে **পুরো কনটেন্ট কপি** করুন।
6. রিপোতে যান: **Settings → Secrets and variables → Actions → New repository secret**, এই 3টি Secret বানান:

   | Secret নাম | মান |
   |---|---|
   | `UNITY_LICENSE` | `.ulf` ফাইলের পুরো কনটেন্ট |
   | `UNITY_EMAIL` | আপনার Unity অ্যাকাউন্টের ইমেইল |
   | `UNITY_PASSWORD` | আপনার Unity পাসওয়ার্ড |

### ধাপ ২ — APK বিল্ড

**Actions → "Build Android APK" → Run workflow** চালান (অথবা `main` ব্রাঞ্চে push করলেই অটো বিল্ড হবে)। 10–20 মিনিট পর রান পেজের **Artifacts** থেকে `FarmSim-Android-APK` ডাউনলোড করে ফোনে ইনস্টল করুন। ✅

> APKটি debug keystore দিয়ে সাইন করা — নিজে টেস্ট/সাইডলোড করার জন্য যথেষ্ট। Play Store-এ দিতে চাইলে আসল keystore লাগবে (চাইলে আমি সেটআপ করে দেবো)।

---

## 🛠️ লোকালে চালানো (ঐচ্ছিক)

1. [Unity Hub](https://unity.com/download) দিয়ে **Unity 2022.3.45f1** (Android Build Support সহ) ইনস্টল করুন।
2. এই রিপো ক্লোন করে Unity Hub-এ **Open** করুন।
3. মেনু: **FarmSim → Build Android APK (CI)** — অথবা Play বোতাম চাপলেই গেম চলবে (সিন অটো-তৈরি হয়, কিছু সেটআপ লাগে না)।

## 📁 প্রজেক্ট কাঠামো

```
Assets/
├── Scripts/
│   ├── Core/        # GameBootstrap, GameManager, Camera, Input, Save, Localization
│   ├── Farm/        # FarmGrid, FarmPlot (চাষের লজিক)
│   └── UI/          # UIManager (পুরো HUD কোড দিয়ে তৈরি)
├── Editor/CiBuild.cs  # CI-তে সিন তৈরি + APK বিল্ড
└── Scenes/            # বিল্ডের সময় অটো-তৈরি হয়
.github/workflows/
├── build-android.yml  # APK বিল্ড পাইপলাইন (GameCI)
└── activation.yml     # একবারের লাইসেন্স সেটআপ
```

---

## 🇬🇧 English Summary

A **2.5D isometric mobile farming simulation** built with **Unity 2022.3 LTS** for Android. Tap soil plots to plant seeds (wheat/tomato/pumpkin), water crops to speed growth, harvest golden crops to earn coins. Includes a day/night cycle, auto-save, bilingual UI (Bangla/English), and touch camera (drag to pan, pinch to zoom). All visuals are procedural low-poly — zero external assets. The APK is built via **GitHub Actions + GameCI**; see the setup steps above (a one-time Unity license secret is required).

**Roadmap ideas:** 🐄 animals (cow/chicken), 🏪 village shop NPC, 🚜 tractor, 🌦️ weather/seasons, 🎵 sound & music.
