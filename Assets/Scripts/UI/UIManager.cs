using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSim
{
    /// <summary>
    /// The whole HUD, built in code: coin pill, day + time-of-day bar,
    /// seed cards, language / sound / next-day buttons, toast and the
    /// first-run tutorial. No prefabs, no scene wiring.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Canvas _canvas;
        private Text _coinsText;
        private RectTransform _coinPill;
        private Text _dayText;
        private Image _dayProgressFill;
        private Text _hintText;
        private Text _seedTitleText;
        private Text _toastText;
        private Image _toastBg;
        private CanvasGroup _toastGroup;
        private Button _langButton;
        private Button _soundButton;
        private Button _nextDayButton;
        private CanvasGroup _tutorialGroup;

        private readonly List<Button> _seedCards = new List<Button>();
        private readonly List<Text> _seedNames = new List<Text>();
        private readonly List<Text> _seedPrices = new List<Text>();
        private readonly List<Image> _seedIcons = new List<Image>();
        private readonly List<Image> _seedBackgrounds = new List<Image>();

        private Coroutine _toastRoutine;
        private float _coinPulse = 1f;
        private int _lastCoins = -1;

        private const float TopBarHeight = 132f;
        private const float BottomBarHeight = 214f;

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
            if (GameManager.Instance != null && _dayProgressFill != null)
            {
                float t = GameManager.Instance.DayProgress01;
                _dayProgressFill.fillAmount = Mathf.Lerp(_dayProgressFill.fillAmount, t, Time.deltaTime * 6f);
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
            BuildTutorial(canvasGo.transform);
        }

        private void BuildTopBar(Transform parent)
        {
            var bar = UiFactory.CreateImage(parent, "TopBar", null, new Color(0.09f, 0.16f, 0.08f, 0.92f), false);
            UiFactory.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -TopBarHeight), Vector2.zero);

            // coin pill
            var coinBg = UiFactory.CreateImage(bar.transform, "CoinPill", UiFactory.RoundedSprite, UiFactory.PanelLight, true);
            UiFactory.Place(coinBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(24f, -44f), new Vector2(324f, 44f));
            _coinPill = coinBg.rectTransform;

            var coinIcon = UiFactory.CreateImage(coinBg.transform, "CoinIcon", UiFactory.CircleSprite, UiFactory.Gold, false);
            UiFactory.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, -30f), new Vector2(72f, 30f));
            var innerIcon = UiFactory.CreateImage(coinIcon.transform, "Inner", UiFactory.CircleSprite, UiFactory.GoldDark, false);
            UiFactory.Place(innerIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-13f, -13f), new Vector2(13f, 13f));

            _coinsText = UiFactory.CreateText(coinBg.transform, "Coins", GameFonts.SemiBold, 46,
                TextAnchor.MiddleLeft, UiFactory.TextDark);
            UiFactory.Place(_coinsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(84f, 0f), new Vector2(-18f, 0f));

            // day + time-of-day bar (centre)
            _dayText = UiFactory.CreateText(bar.transform, "Day", GameFonts.SemiBold, 42,
                TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f));
            UiFactory.Place(_dayText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-180f, -62f), new Vector2(180f, -12f));

            var barBg = UiFactory.CreateImage(bar.transform, "DayBarBg", UiFactory.RoundedSprite,
                new Color(1f, 1f, 1f, 0.22f), true);
            UiFactory.Place(barBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-150f, -86f), new Vector2(150f, -70f));

            var fill = UiFactory.CreateImage(barBg.transform, "DayBarFill", UiFactory.RoundedSprite, UiFactory.Gold, true);
            UiFactory.Place(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, 0f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            _dayProgressFill = fill;

            // right side buttons
            _soundButton = UiFactory.CreateButton(bar.transform, "SoundButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-300f, -38f), new Vector2(-188f, 38f),
                new Color(0.22f, 0.34f, 0.20f), "SFX", GameFonts.SemiBold, 30, OnSoundClicked);

            _langButton = UiFactory.CreateButton(bar.transform, "LangButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-180f, -38f), new Vector2(-68f, 38f),
                new Color(0.22f, 0.34f, 0.20f), "বাংলা", GameFonts.SemiBold, 30, OnLanguageClicked);

            _nextDayButton = UiFactory.CreateButton(bar.transform, "NextDayButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-336f, -44f), new Vector2(-24f, 44f),
                UiFactory.Gold, Localization.T("next_day"), GameFonts.SemiBold, 32, OnNextDayClicked);
        }

        private void BuildBottomBar(Transform parent)
        {
            var bar = UiFactory.CreateImage(parent, "BottomBar", null, new Color(0.09f, 0.16f, 0.08f, 0.92f), false);
            UiFactory.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, BottomBarHeight));

            _seedTitleText = UiFactory.CreateText(bar.transform, "SeedTitle", GameFonts.SemiBold, 34,
                TextAnchor.MiddleLeft, new Color(1f, 0.96f, 0.86f));
            UiFactory.Place(_seedTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -58f), new Vector2(330f, -12f));

            for (int i = 0; i < CropDatabase.Count; i++)
            {
                int index = i;
                var crop = CropDatabase.Get(i);
                float cardWidth = 380f;
                float left = 30f + i * (cardWidth + 18f);

                var card = UiFactory.CreateButton(bar.transform, "SeedCard" + i,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(left, -150f), new Vector2(left + cardWidth, -8f),
                    new Color(0.20f, 0.30f, 0.17f), null, GameFonts.Regular, 30,
                    () => OnSeedClicked(index));
                _seedCards.Add(card);
                _seedBackgrounds.Add(card.GetComponent<Image>());

                var icon = UiFactory.CreateImage(card.transform, "Icon", UiFactory.RoundedSprite, crop.RipeColor, true);
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, -46f), new Vector2(104f, 46f));
                _seedIcons.Add(icon);

                var name = UiFactory.CreateText(card.transform, "Name", GameFonts.SemiBold, 33,
                    TextAnchor.LowerLeft, Color.white);
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                    new Vector2(116f, -4f), new Vector2(-12f, -10f));
                _seedNames.Add(name);

                var price = UiFactory.CreateText(card.transform, "Price", GameFonts.Regular, 26,
                    TextAnchor.UpperLeft, new Color(0.85f, 0.95f, 0.80f));
                UiFactory.Place(price.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                    new Vector2(116f, 10f), new Vector2(-12f, 4f));
                _seedPrices.Add(price);
            }

            _hintText = UiFactory.CreateText(bar.transform, "Hint", GameFonts.Regular, 27,
                TextAnchor.MiddleLeft, new Color(0.82f, 0.92f, 0.78f));
            UiFactory.Place(_hintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(30f, 6f), new Vector2(-30f, 40f));
        }

        private void BuildToast(Transform parent)
        {
            var bg = UiFactory.CreateImage(parent, "Toast", UiFactory.RoundedSprite,
                new Color(0.10f, 0.14f, 0.09f, 0.90f), true);
            UiFactory.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-380f, -300f), new Vector2(380f, -206f));
            _toastBg = bg;
            _toastGroup = bg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
            _toastGroup.blocksRaycasts = false;

            _toastText = UiFactory.CreateText(bg.transform, "ToastText", GameFonts.SemiBold, 38,
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
                new Vector2(-440f, -300f), new Vector2(440f, 300f));

            var title = UiFactory.CreateText(card.transform, "Title", GameFonts.Bold, 52,
                TextAnchor.MiddleCenter, UiFactory.GreenDark);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -90f), new Vector2(-20f, -24f));
            title.text = Localization.T("tut_title");

            var body = UiFactory.CreateText(card.transform, "Body", GameFonts.Regular, 34,
                TextAnchor.UpperLeft, UiFactory.TextDark);
            UiFactory.Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(44f, 130f), new Vector2(-44f, -110f));
            body.text = Localization.T("tut_body");

            UiFactory.CreateButton(card.transform, "StartButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-170f, 34f), new Vector2(170f, 118f),
                UiFactory.Green, Localization.T("tut_ok"), GameFonts.SemiBold, 38, HideTutorial);
        }

        // ---------- behaviour ----------

        private void OnSeedClicked(int index)
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.SelectCrop(index);
            if (SfxManager.Instance != null) SfxManager.Instance.PlayTap();
            RefreshAll();
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
        }

        private void OnSoundClicked()
        {
            if (SfxManager.Instance != null) SfxManager.Instance.ToggleSound();
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
                    ? new Color(0.22f, 0.34f, 0.20f)
                    : new Color(0.30f, 0.22f, 0.22f);
            }

            for (int i = 0; i < _seedCards.Count; i++)
            {
                var crop = CropDatabase.Get(i);
                bool selected = i == manager.SelectedCropIndex;

                if (_seedNames.Count > i && _seedNames[i] != null)
                    _seedNames[i].text = crop.DisplayName;

                if (_seedPrices.Count > i && _seedPrices[i] != null)
                {
                    _seedPrices[i].text = Localization.T("cost") + " " + crop.SeedCost +
                                          "  ·  " + Localization.T("sell") + " " + crop.SellPrice;
                }

                if (_seedBackgrounds.Count > i && _seedBackgrounds[i] != null)
                {
                    _seedBackgrounds[i].color = selected
                        ? new Color(0.27f, 0.40f, 0.22f)
                        : new Color(0.16f, 0.24f, 0.14f);
                }

                if (_seedIcons.Count > i && _seedIcons[i] != null)
                    _seedIcons[i].color = crop.RipeColor;

                var outline = _seedCards[i].GetComponent<Outline>();
                if (outline == null) outline = _seedCards[i].gameObject.AddComponent<Outline>();
                outline.effectColor = UiFactory.Gold;
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = selected;
            }
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
