using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSim
{
    /// <summary>
    /// The whole HUD, built in code: coin pill, level + XP bar, day and
    /// time-of-day bar, paged seed cards, market / sound / language /
    /// next-day buttons, toast and the first-run tutorial.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private const int CardsPerPage = 4;
        private const float TopBarHeight = 132f;
        private const float BottomBarHeight = 214f;

        private Canvas _canvas;
        private Text _coinsText;
        private RectTransform _coinPill;
        private Text _levelText;
        private Image _xpFill;
        private Text _dayText;
        private Image _dayProgressFill;
        private Text _hintText;
        private Text _seedTitleText;
        private Text _pageText;
        private Text _toastText;
        private Image _toastBg;
        private CanvasGroup _toastGroup;
        private Button _langButton;
        private Button _soundButton;
        private Button _vibrationButton;
        private Button _nextDayButton;
        private readonly List<Button> _toolButtons = new List<Button>();
        private Button _marketButton;
        private Text _marketLabel;
        private CanvasGroup _tutorialGroup;

        private readonly List<Button> _seedCards = new List<Button>();
        private readonly List<Text> _seedNames = new List<Text>();
        private readonly List<Text> _seedPrices = new List<Text>();
        private readonly List<Image> _seedIcons = new List<Image>();
        private readonly List<Image> _seedBackgrounds = new List<Image>();

        private MarketPanel _market;
        private int _seedPage;

        private Coroutine _toastRoutine;
        private float _coinPulse = 1f;
        private int _lastCoins = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUi();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStatsChanged += RefreshAll;
                RefreshAll();
            }
            if (!PlayerPrefs.HasKey("farmsim_tutorial_done"))
                ShowTutorial();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsChanged -= RefreshAll;
        }

        private void Update()
        {
            var manager = GameManager.Instance;
            if (manager != null && _dayProgressFill != null)
            {
                float t = manager.DayProgress01;
                _dayProgressFill.fillAmount = Mathf.Lerp(_dayProgressFill.fillAmount, t, Time.deltaTime * 6f);
            }

            if (manager != null && _xpFill != null)
            {
                _xpFill.fillAmount = Mathf.Lerp(_xpFill.fillAmount, manager.LevelProgress01, Time.deltaTime * 8f);
            }

            _coinPulse = Mathf.Lerp(_coinPulse, 1f, Time.deltaTime * 10f);
            if (_coinsText != null)
                _coinsText.transform.localScale = Vector3.one * _coinPulse;
        }

        // ---------- construction ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("HUD");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildTopBar(canvasGo.transform);
            BuildBottomBar(canvasGo.transform);
            BuildToast(canvasGo.transform);

            _market = new MarketPanel();
            _market.Build(canvasGo.transform);

            BuildTutorial(canvasGo.transform);
        }

        private void BuildTopBar(Transform parent)
        {
            // A soft fade instead of a solid band: the world stays visible right
            // up under the HUD (v1.1 shipped an opaque black strip that looked
            // like letterboxing).
            var topFade = UiFactory.CreateImage(parent, "TopFade", UiFactory.FadeTop,
                new Color(0.05f, 0.08f, 0.06f, 1f), false);
            UiFactory.Place(topFade.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -226f), Vector2.zero);

            var bar = UiFactory.CreateImage(parent, "TopBar", null, new Color(0f, 0f, 0f, 0f), false);
            UiFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -TopBarHeight), Vector2.zero);

            // ---- coin pill ----
            var coinBg = UiFactory.CreateImage(bar.transform, "CoinPill", UiFactory.RoundedSprite,
                UiFactory.PanelLight, true);
            UiFactory.Place(coinBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(24f, -44f), new Vector2(324f, 44f));
            _coinPill = coinBg.rectTransform;

            var coinIcon = UiFactory.CreateImage(coinBg.transform, "CoinIcon", UiFactory.CircleSprite,
                UiFactory.Gold, false);
            UiFactory.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, -30f), new Vector2(72f, 30f));
            var innerIcon = UiFactory.CreateImage(coinIcon.transform, "Inner", UiFactory.CircleSprite,
                UiFactory.GoldDark, false);
            UiFactory.Place(innerIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-13f, -13f), new Vector2(13f, 13f));

            _coinsText = UiFactory.CreateText(coinBg.transform, "Coins", GameFonts.SemiBold, 44,
                TextAnchor.MiddleLeft, UiFactory.TextDark);
            UiFactory.Place(_coinsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(84f, 0f), new Vector2(-18f, 0f));

            // ---- level + XP ----
            var levelBg = UiFactory.CreateImage(bar.transform, "LevelPill", UiFactory.RoundedSprite,
                new Color(0.10f, 0.13f, 0.11f, 0.78f), true);
            UiFactory.Place(levelBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(344f, -44f), new Vector2(636f, 44f));

            _levelText = UiFactory.CreateText(levelBg.transform, "Level", GameFonts.SemiBold, 28,
                TextAnchor.MiddleLeft, new Color(0.95f, 0.91f, 0.80f));
            UiFactory.Place(_levelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(16f, 6f), new Vector2(-16f, 36f));
            _levelText.text = "Lv 1";

            _levelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.Shadowed(_levelText, 0.5f);

            var xpBg = UiFactory.CreateImage(levelBg.transform, "XpBg", UiFactory.RoundedSprite,
                new Color(1f, 1f, 1f, 0.16f), true);
            UiFactory.Place(xpBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(16f, -30f), new Vector2(-16f, -10f));

            var xpFill = UiFactory.CreateImage(xpBg.transform, "XpFill", UiFactory.RoundedSprite,
                UiFactory.Gold, true);
            UiFactory.Place(xpFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            xpFill.fillAmount = 0f;
            _xpFill = xpFill;

            // ---- day + time bar (centre) ----
            _dayText = UiFactory.CreateText(bar.transform, "Day", GameFonts.SemiBold, 38,
                TextAnchor.MiddleCenter, new Color(0.92f, 0.90f, 0.84f));
            UiFactory.Place(_dayText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, 6f), new Vector2(200f, 56f));
            UiFactory.Shadowed(_dayText, 0.6f);

            var barBg = UiFactory.CreateImage(bar.transform, "DayBarBg", UiFactory.RoundedSprite,
                new Color(1f, 1f, 1f, 0.18f), true);
            UiFactory.Place(barBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-190f, -46f), new Vector2(190f, -12f));

            var fill = UiFactory.CreateImage(barBg.transform, "DayBarFill", UiFactory.RoundedSprite,
                UiFactory.Gold, true);
            UiFactory.Place(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            _dayProgressFill = fill;

            // ---- right side buttons ----
            _soundButton = UiFactory.CreateButton(bar.transform, "SoundButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-660f, -36f), new Vector2(-560f, 36f),
                new Color(0.19f, 0.24f, 0.19f), "SFX", GameFonts.SemiBold, 26, OnSoundClicked);

            _vibrationButton = UiFactory.CreateButton(bar.transform, "VibrationButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-550f, -36f), new Vector2(-450f, 36f),
                new Color(0.19f, 0.24f, 0.19f), "VIB", GameFonts.SemiBold, 26, OnVibrationClicked);

            _langButton = UiFactory.CreateButton(bar.transform, "LangButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-440f, -36f), new Vector2(-340f, 36f),
                new Color(0.19f, 0.24f, 0.19f), "বাংলা", GameFonts.SemiBold, 26, OnLanguageClicked);

            _nextDayButton = UiFactory.CreateButton(bar.transform, "NextDayButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-326f, -42f), new Vector2(-24f, 42f),
                UiFactory.Gold, Localization.T("next_day"), GameFonts.SemiBold, 30, OnNextDayClicked);
        }

        private void BuildBottomBar(Transform parent)
        {
            var bottomFade = UiFactory.CreateImage(parent, "BottomFade", UiFactory.FadeBottom,
                new Color(0.05f, 0.08f, 0.06f, 1f), false);
            UiFactory.Place(bottomFade.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, BottomBarHeight + 96f));

            var bar = UiFactory.CreateImage(parent, "BottomBar", null, new Color(0f, 0f, 0f, 0f), false);
            UiFactory.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, BottomBarHeight));

            _seedTitleText = UiFactory.CreateText(bar.transform, "SeedTitle", GameFonts.SemiBold, 32,
                TextAnchor.MiddleLeft, new Color(0.90f, 0.88f, 0.82f));
            UiFactory.Place(_seedTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -54f), new Vector2(330f, -14f));

            for (int i = 0; i < CardsPerPage; i++)
            {
                int slot = i;
                float left = 30f + i * 312f;

                var card = UiFactory.CreateButton(bar.transform, "SeedCard" + i,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(left, -166f), new Vector2(left + 300f, -40f),
                    new Color(0.13f, 0.18f, 0.14f, 0.92f), null, GameFonts.Regular, 30,
                    () => OnSeedSlotClicked(slot));
                _seedCards.Add(card);
                _seedBackgrounds.Add(card.GetComponent<Image>());

                var icon = UiFactory.CreateImage(card.transform, "Icon", UiFactory.RoundedSprite,
                    Color.white, true);
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, -26f), new Vector2(64f, 26f));
                _seedIcons.Add(icon);

                var name = UiFactory.CreateText(card.transform, "Name", GameFonts.SemiBold, 30,
                    TextAnchor.LowerLeft, Color.white);
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                    new Vector2(74f, 0f), new Vector2(-10f, -8f));
                _seedNames.Add(name);

                var price = UiFactory.CreateText(card.transform, "Price", GameFonts.Regular, 24,
                    TextAnchor.UpperLeft, new Color(0.80f, 0.84f, 0.76f));
                UiFactory.Place(price.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                    new Vector2(74f, 8f), new Vector2(-10f, 0f));
                _seedPrices.Add(price);
            }

            // paging
            UiFactory.CreateButton(bar.transform, "PrevPage",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-648f, -166f), new Vector2(-588f, -106f),
                new Color(0.19f, 0.24f, 0.19f), "<", GameFonts.Bold, 32, () => ChangePage(-1));

            UiFactory.CreateButton(bar.transform, "NextPage",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-580f, -166f), new Vector2(-520f, -106f),
                new Color(0.19f, 0.24f, 0.19f), ">", GameFonts.Bold, 32, () => ChangePage(1));

            _pageText = UiFactory.CreateText(bar.transform, "PageLabel", GameFonts.Regular, 24,
                TextAnchor.MiddleCenter, new Color(0.78f, 0.80f, 0.74f));
            UiFactory.Place(_pageText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-648f, -104f), new Vector2(-520f, -74f));

            // tool selector, stacked up the right edge (above the bar)
            string[] toolLabels = { "AUTO", "PLANT", "WATER", "PICK" };
            for (int t = 0; t < toolLabels.Length; t++)
            {
                int tool = t;
                var button = UiFactory.CreateButton(parent, "Tool" + t,
                    new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-124f, 232f + t * 88f), new Vector2(-24f, 316f + t * 88f),
                    new Color(0.13f, 0.18f, 0.14f, 0.88f), toolLabels[t], GameFonts.SemiBold, 22,
                    () => OnToolClicked(tool));
                _toolButtons.Add(button);
            }

            // market button
            _marketButton = UiFactory.CreateButton(bar.transform, "MarketButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-496f, -166f), new Vector2(-30f, -40f),
                UiFactory.Green, Localization.T("market"), GameFonts.SemiBold, 38, OnMarketClicked);
            _marketLabel = _marketButton.GetComponentInChildren<Text>();
            if (_marketLabel != null) _marketLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

            _hintText = UiFactory.CreateText(bar.transform, "Hint", GameFonts.Regular, 24,
                TextAnchor.MiddleLeft, new Color(0.76f, 0.80f, 0.72f));
            UiFactory.Place(_hintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(30f, 6f), new Vector2(-500f, 38f));
            _hintText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // tiny build stamp so a new APK can be recognised at a glance
            var version = UiFactory.CreateText(bar.transform, "Version", GameFonts.Regular, 20,
                TextAnchor.LowerRight, new Color(0.78f, 0.80f, 0.74f, 0.55f));
            version.text = "v1.2.0";
            UiFactory.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-150f, 4f), new Vector2(-24f, 30f));
        }

        private void BuildToast(Transform parent)
        {
            var bg = UiFactory.CreateImage(parent, "Toast", UiFactory.RoundedSprite,
                new Color(0.09f, 0.11f, 0.10f, 0.90f), true);
            UiFactory.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-380f, -300f), new Vector2(380f, -206f));
            _toastBg = bg;
            _toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toastGroup.blocksRaycasts = false;

            _toastText = UiFactory.CreateText(bg.transform, "ToastText", GameFonts.SemiBold, 36,
                TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(_toastText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(16f, 4f), new Vector2(-16f, -4f));
        }

        private void BuildTutorial(Transform parent)
        {
            var overlay = UiFactory.CreateImage(parent, "Tutorial", null, new Color(0.03f, 0.06f, 0.03f, 0.72f), false);
            UiFactory.Place(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            _tutorialGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            _tutorialGroup.alpha = 0f;
            _tutorialGroup.blocksRaycasts = false;

            var card = UiFactory.CreateImage(overlay.transform, "Card", UiFactory.RoundedSprite, UiFactory.PanelLight, true);
            card.raycastTarget = true;
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-460f, -310f), new Vector2(460f, 310f));

            var title = UiFactory.CreateText(card.transform, "Title", GameFonts.Bold, 50,
                TextAnchor.MiddleCenter, UiFactory.GreenDark);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -90f), new Vector2(-20f, -24f));
            title.text = Localization.T("tut_title");

            var body = UiFactory.CreateText(card.transform, "Body", GameFonts.Regular, 32,
                TextAnchor.UpperLeft, UiFactory.TextDark);
            UiFactory.Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(44f, 130f), new Vector2(-44f, -110f));
            body.text = Localization.T("tut_body");

            UiFactory.CreateButton(card.transform, "StartButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-170f, 34f), new Vector2(170f, 118f),
                UiFactory.Green, Localization.T("tut_ok"), GameFonts.SemiBold, 36, HideTutorial);
        }

        // ---------- behaviour ----------

        private void OnSeedSlotClicked(int slot)
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            int index = _seedPage * CardsPerPage + slot;
            if (index >= CropDatabase.Count) return;

            manager.SelectCrop(index);
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            RefreshAll();
        }

        private void ChangePage(int direction)
        {
            int pages = Mathf.CeilToInt(CropDatabase.Count / (float)CardsPerPage);
            _seedPage = (_seedPage + direction + pages) % pages;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            RefreshAll();
        }

        private void OnMarketClicked()
        {
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            if (_market != null) _market.Toggle();
        }

        private void OnNextDayClicked()
        {
            if (GameManager.Instance == null) return;
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            GameManager.Instance.NextDay();
        }

        private void OnLanguageClicked()
        {
            Localization.Toggle();
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            RefreshAll();
            if (_market != null) _market.Refresh();
        }

        private void OnSoundClicked()
        {
            if (SfxManager.Instance != null) SfxManager.Instance.ToggleSound();
            RefreshAll();
        }

        private void OnVibrationClicked()
        {
            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.ToggleHaptics();
                if (SfxManager.Instance.HapticsOn) SfxManager.Instance.Haptic();
            }
            RefreshAll();
        }

        private void OnToolClicked(int tool)
        {
            if (GameManager.Instance != null) GameManager.Instance.SelectTool(tool);
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            RefreshAll();
        }

        public void ShowTutorial()
        {
            if (_tutorialGroup == null) return;
            StopAllCoroutines();
            StartCoroutine(FadeGroup(_tutorialGroup, 0f, 1f, 0.25f, true));
        }

        public void HideTutorial()
        {
            if (_tutorialGroup == null) return;
            StartCoroutine(FadeGroup(_tutorialGroup, 1f, 0f, 0.2f, false));
            PlayerPrefs.SetInt("farmsim_tutorial_done", 1);
            PlayerPrefs.Save();
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration, bool block)
        {
            group.blocksRaycasts = block;
            group.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
            group.blocksRaycasts = block;
        }

        // ---------- refresh ----------

        public void RefreshAll()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            if (_lastCoins >= 0 && manager.Coins != _lastCoins)
                _coinPulse = 1.18f;
            _lastCoins = manager.Coins;

            if (_coinsText != null) _coinsText.text = manager.Coins.ToString();
            if (_dayText != null) _dayText.text = Localization.T("day") + " " + manager.Day;
            if (_seedTitleText != null) _seedTitleText.text = Localization.T("select_seed");
            if (_hintText != null) _hintText.text = Localization.T("hint_empty");

            if (_levelText != null)
            {
                _levelText.text = Localization.T("level") + " " + manager.Level +
                                  "   " + manager.Xp + "/" + manager.XpToNextLevel;
            }

            int pages = Mathf.CeilToInt(CropDatabase.Count / (float)CardsPerPage);
            if (_pageText != null) _pageText.text = (_seedPage + 1) + "/" + pages;

            if (_langButton != null)
            {
                var label = _langButton.GetComponentInChildren<Text>();
                if (label != null) label.text = Localization.T("lang_button");
            }

            if (_nextDayButton != null)
            {
                var label = _nextDayButton.GetComponentInChildren<Text>();
                if (label != null) label.text = Localization.T("next_day");
            }

            if (_vibrationButton != null)
            {
                var label = _vibrationButton.GetComponentInChildren<Text>();
                bool vib = SfxManager.Instance == null || SfxManager.Instance.HapticsOn;
                if (label != null) label.text = "VIB";
                var img = _vibrationButton.GetComponent<Image>();
                if (img != null)
                    img.color = vib ? new Color(0.19f, 0.24f, 0.19f) : new Color(0.26f, 0.16f, 0.15f, 0.9f);
            }

            for (int i = 0; i < _toolButtons.Count; i++)
            {
                var img = _toolButtons[i].GetComponent<Image>();
                if (img == null) continue;
                bool selected = i == manager.ToolIndex;
                img.color = selected
                    ? UiFactory.Green
                    : new Color(0.13f, 0.18f, 0.14f, 0.88f);
                var label = _toolButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.color = selected ? Color.white : new Color(0.82f, 0.86f, 0.78f);
            }

            if (_soundButton != null)
            {
                var label = _soundButton.GetComponentInChildren<Text>();
                bool on = SfxManager.Instance == null || SfxManager.Instance.SoundOn;
                if (label != null)
                {
                    label.text = on ? "SFX" : "MUTE";
                    label.color = on ? Color.white : new Color(0.75f, 0.75f, 0.75f);
                }
                _soundButton.GetComponent<Image>().color = on
                    ? new Color(0.19f, 0.24f, 0.19f)
                    : new Color(0.30f, 0.22f, 0.22f);
            }

            if (_marketButton != null && _marketLabel != null)
            {
                int stored = manager.StoredTotal;
                _marketLabel.text = Localization.T("market") + (stored > 0 ? "  (" + stored + ")" : "");
                _marketButton.GetComponent<Image>().color = stored > 0
                    ? UiFactory.Green
                    : new Color(0.20f, 0.24f, 0.20f);
            }

            for (int slot = 0; slot < _seedCards.Count; slot++)
            {
                int index = _seedPage * CardsPerPage + slot;
                bool exists = index < CropDatabase.Count;

                if (_seedCards[slot] != null)
                    _seedCards[slot].gameObject.SetActive(exists);
                if (!exists) continue;

                var crop = CropDatabase.Get(index);
                bool unlocked = manager.IsCropUnlocked(index);
                bool selected = index == manager.SelectedCropIndex;

                if (_seedNames.Count > slot && _seedNames[slot] != null)
                {
                    _seedNames[slot].text = crop.DisplayName;
                    _seedNames[slot].color = unlocked ? Color.white : new Color(0.72f, 0.72f, 0.68f);
                }

                if (_seedPrices.Count > slot && _seedPrices[slot] != null)
                {
                    _seedPrices[slot].text = unlocked
                        ? crop.SeedCost + " -> " + manager.MarketPrice[index]
                        : Localization.T("need_level") + " " + crop.UnlockLevel;
                    _seedPrices[slot].color = unlocked
                        ? new Color(0.80f, 0.84f, 0.76f)
                        : new Color(0.90f, 0.76f, 0.44f);
                }

                if (_seedBackgrounds.Count > slot && _seedBackgrounds[slot] != null)
                {
                    _seedBackgrounds[slot].color = selected && unlocked
                        ? new Color(0.27f, 0.36f, 0.22f)
                        : (unlocked ? new Color(0.16f, 0.21f, 0.16f) : new Color(0.14f, 0.15f, 0.14f));
                }

                if (_seedIcons.Count > slot && _seedIcons[slot] != null)
                {
                    var color = crop.RipeColor;
                    if (!unlocked) color = Color.Lerp(color, new Color(0.4f, 0.4f, 0.4f), 0.65f);
                    _seedIcons[slot].color = color;
                }

                var outline = _seedCards[slot].GetComponent<Outline>();
                if (outline == null) outline = _seedCards[slot].gameObject.AddComponent<Outline>();
                outline.effectColor = UiFactory.Gold;
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = selected && unlocked;
            }

            if (_market != null && _market.IsVisible) _market.Refresh();
        }

        public void ShowToast(string message)
        {
            ShowToast(message, false, false);
        }

        public void ShowToast(string message, bool isError)
        {
            ShowToast(message, isError, false);
        }

        public void ShowToast(string message, bool isError, bool isReward)
        {
            if (_toastGroup == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);

            _toastText.text = message;
            _toastText.color = isError
                ? new Color(1f, 0.72f, 0.62f)
                : (isReward ? new Color(1f, 0.88f, 0.42f) : Color.white);
            _toastBg.color = isError
                ? new Color(0.34f, 0.12f, 0.10f, 0.92f)
                : new Color(0.10f, 0.16f, 0.09f, 0.92f);

            float width = Mathf.Clamp(_toastText.preferredWidth + 90f, 420f, 1100f);
            _toastBg.rectTransform.offsetMin = new Vector2(-width * 0.5f, -300f);
            _toastBg.rectTransform.offsetMax = new Vector2(width * 0.5f, -206f);

            _toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            const float fadeIn = 0.14f;
            const float hold = 1.25f;
            const float fadeOut = 0.35f;

            _toastGroup.alpha = 0f;
            var rt = _toastBg.rectTransform;
            Vector2 baseMin = rt.offsetMin;
            Vector2 baseMax = rt.offsetMax;

            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeIn;
                _toastGroup.alpha = t;
                rt.offsetMin = baseMin + new Vector2(0f, -18f) * (1f - t);
                rt.offsetMax = baseMax + new Vector2(0f, -18f) * (1f - t);
                yield return null;
            }
            rt.offsetMin = baseMin;
            rt.offsetMax = baseMax;
            _toastGroup.alpha = 1f;

            yield return new WaitForSeconds(hold);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                _toastGroup.alpha = 1f - (elapsed / fadeOut);
                yield return null;
            }
            _toastGroup.alpha = 0f;
            _toastRoutine = null;
        }
    }
}
