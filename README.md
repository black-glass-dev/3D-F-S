# 🌾 3D Farm Simulator — মোবাইল ফার্মিং সিমুলেশন গেম

**Unity (2022.3 LTS) দিয়ে তৈরি 2.5D Isometric ফার্মিং সিমুলেশন গেম — Android মোবাইলের জন্য।**
*English summary below.*

> স্টাইল সিদ্ধান্ত: Hay Day-এর মতো **2.5D আইসোমেট্রিক** — 3D লো-পলি গ্রাফিক্স, কিন্তু ফিক্সড ক্যামেরা। দেখতে প্রিমিয়াম, মোবাইলে হালকা ও মসৃণ, টাচ কন্ট্রোল সহজ।

---

## 📥 APK ডাউনলোড

👉 **[সর্বশেষ বিল্ডের APK নামান (Releases পেজ)](https://github.com/islamyousuf032-glitch/3D-F-S/releases/latest)**

পেজের নিচে **Assets** অংশে `FarmSim.apk` (~13 MB) ফাইলটি ডাউনলোড করে ফোনে ইনস্টল করুন।
(ফোনে "Unknown sources / অজানা উৎস থেকে ইনস্টল" অনুমতি দিতে হতে পারে।)

| তথ্য | মান |
|---|---|
| সাইজ | ~13.5 MB |
| Android | 7.0+ (ARM64), ল্যান্ডস্কেপ |
| গ্রাফিক্স | লো-পলি 3D + ফিক্সড আইসোমেট্রিক ক্যামেরা (2.5D) |
| ভাষা | বাংলা / English |

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

### ধাপ ১ — একবারের জন্য Unity অ্যাকাউন্টের ২টি Secret বসানো (আপনাকে করতে হবে, ~২ মিনিট)

> Unity দিয়ে ক্লাউডে বিল্ড করতে লাইসেন্স লাগে। **ফ্রি Unity Personal অ্যাকাউন্টই যথেষ্ট** — টাকা লাগবে না।
> ⚠️ Unity এখন ফ্রি অ্যাকাউন্টের "ম্যানুয়াল অ্যাক্টিভেশন" (.alf/.ulf) বন্ধ করে দিয়েছে, তাই কোনো ফাইল ডাউনলোড করা লাগবে না — শুধু ইমেইল + পাসওয়ার্ড দিলেই বিল্ড মেশিন নিজে থেকেই লাইসেন্স চালু করে নেবে।
> **পাসওয়ার্ড আমাকে (বা কাউকে) দেওয়ার দরকার নেই — শুধু GitHub Secrets-এ বসাবেন।**

1. এই লিংকে যান 👉 **[Settings → Secrets and variables → Actions](https://github.com/islamyousuf032-glitch/3D-F-S/settings/secrets/actions)**
2. সবুজ **New repository secret** বোতামে চাপ দিন →
   - **Name:** `UNITY_EMAIL` · **Secret:** আপনার Unity অ্যাকাউন্টের ইমেইল → **Add secret**
3. আবার **New repository secret** → **Name:** `UNITY_PASSWORD` · **Secret:** আপনার Unity পাসওয়ার্ড → **Add secret**

ব্যস! দুটো Secret-ই হয়ে গেল। (যদি Unity অ্যাকাউন্টে 2FA/টু-স্টেপ চালু থাকে, বন্ধ রাখতে হতে পারে — অথবা পাসওয়ার্ডে বিশেষ চিহ্ন না থাকলে ভালো।)

### ধাপ ২ — APK বিল্ড

**Actions → "Build Android APK" → Run workflow** চালান, অথবা `build-` দিয়ে শুরু হওয়া কোনো ট্যাগ push করুন (যেমন `build-13`)।

- বিল্ড শেষ হলে APK দুটো জায়গায় পাওয়া যায়: রান পেজের **Artifacts** এবং **Releases** পেজে সরাসরি `FarmSim.apk`
- সময়: প্রথমবার ~15 মিনিট, ক্যাশের কারণে পরেরবার কম
- Unity Personal লাইসেন্স প্রতি বিল্ডে অনলাইনে চালু হয় (কোনো ফাইল লাগে না)

> APKটি debug keystore দিয়ে সাইন করা — নিজে টেস্ট/সাইডলোড করার জন্য যথেষ্ট। Play Store-এ দিতে চাইলে আসল keystore লাগবে (চাইলে আমি সেটআপ করে দেবো)।

---

## 🛠️ লোকালে চালানো (ঐচ্ছিক)

1. [Unity Hub](https://unity.com/download) দিয়ে **Unity 2022.3.62f3** (Android Build Support সহ) ইনস্টল করুন।
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
└── build-android.yml  # APK বিল্ড পাইপলাইন (Unity CLI + অনলাইন লাইসেন্স)
```

---

## 🇬🇧 English Summary

A **2.5D isometric mobile farming simulation** built with **Unity 2022.3 LTS** for Android. Tap soil plots to plant seeds (wheat/tomato/pumpkin), water crops to speed growth, harvest golden crops to earn coins. Includes a day/night cycle, auto-save, bilingual UI (Bangla/English), and touch camera (drag to pan, pinch to zoom). All visuals are procedural low-poly — zero external assets. The APK is built by **GitHub Actions** (installs Unity 2022.3 + Android module, activates a free Personal license online, then builds with the Unity CLI); see the setup steps above.

**Roadmap ideas:** 🐄 animals (cow/chicken), 🏪 village shop NPC, 🚜 tractor, 🌦️ weather/seasons, 🎵 sound & music.
