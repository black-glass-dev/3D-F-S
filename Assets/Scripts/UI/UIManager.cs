using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSim
{
    /// <summary>
    /// All HUD is built in code (no prefabs / scene wiring needed):
    /// top bar (coins, day, language, next day) + bottom seed shop bar + toast.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Text _coinsText;
        private Text _dayText;
        private Text _hintText;
        private Text _toastText;
        private Text _seedTitleText;
        private Button _langButton;
        private Button _nextDayButton;
        private readonly List<Button> _seedButtons = new List<Button>();
        private readonly List<Text> _seedButtonTexts = new List<Text>();
        private Coroutine _toastRoutine;

        private static readonly Color PanelColor = new Color(0.13f, 0.20f, 0.10f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.30f, 0.50f, 0.20f);
        private static readonly Color SelectedColor = new Color(0.95f, 0.75f, 0.25f);

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
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsChanged -= RefreshAll;
        }

        // ---------- construction ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("HUD");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Top bar
            var topBar = MakePanel(canvasGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), new Vector2(0f, 0f));
            _coinsText = MakeText(topBar.transform, font, 44, TextAnchor.MiddleLeft);
            Place(_coinsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.35f, 1f), new Vector2(30f, 0f), new Vector2(0f, 0f));
            _dayText = MakeText(topBar.transform, font, 44, TextAnchor.MiddleCenter);
            Place(_dayText.rectTransform, new Vector2(0.35f, 0.5f), new Vector2(0.60f, 1f), Vector2.zero, Vector2.zero);

            _nextDayButton = MakeButton(topBar.transform, font, "", () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.NextDay();
            });
            Place(_nextDayButton.GetComponent<RectTransform>(), new Vector2(0.62f, 0.5f), new Vector2(0.82f, 1f),
                new Vector2(5f, 12f), new Vector2(-5f, -12f));

            _langButton = MakeButton(topBar.transform, font, "", () =>
            {
                Localization.Toggle();
                RefreshAll();
            });
            Place(_langButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.5f), new Vector2(0.98f, 1f),
                new Vector2(5f, 12f), new Vector2(-10f, -12f));

            // Bottom seed bar
            var bottomBar = MakePanel(canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 190f));
            _seedTitleText = MakeText(bottomBar.transform, font, 36, TextAnchor.MiddleLeft);
            Place(_seedTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0.25f, 1f),
                new Vector2(30f, -55f), new Vector2(0f, -5f));

            _hintText = MakeText(bottomBar.transform, font, 32, TextAnchor.MiddleLeft);
            _hintText.color = new Color(0.85f, 0.95f, 0.80f);
            Place(_hintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f),
                new Vector2(30f, 5f), new Vector2(-30f, 0f));

            for (int i = 0; i < CropDatabase.Count; i++)
            {
                int index = i;
                var btn = MakeButton(bottomBar.transform, font, "", () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.SelectCrop(index);
                });
                float left = 0.27f + i * 0.24f;
                Place(btn.GetComponent<RectTransform>(), new Vector2(left, 1f), new Vector2(left + 0.22f, 1f),
                    new Vector2(5f, -105f), new Vector2(-5f, -55f));
                _seedButtons.Add(btn);
                _seedButtonTexts.Add(btn.GetComponentInChildren<Text>());
            }

            // Toast (center screen, fades out)
            _toastText = MakeText(canvasGo.transform, font, 52, TextAnchor.MiddleCenter);
            _toastText.color = Color.white;
            Place(_toastText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f),
                new Vector2(-700f, -60f), new Vector2(700f, 60f));
            var outline = _toastText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(3f, -3f);
            _toastText.text = "";
        }

        private static GameObject MakePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = PanelColor;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static Text MakeText(Transform parent, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button MakeButton(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ButtonColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var text = MakeText(go.transform, font, 36, TextAnchor.MiddleCenter);
            text.text = label;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return btn;
        }

        private static void Place(RectTransform rt, Vector2 center, Vector2 sizeAnchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            // Helper overloaded below; see PlaceExact for real implementation.
            PlaceExact(rt, center, sizeAnchor, offsetMin, offsetMax);
        }

        private static void PlaceExact(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        // ---------- runtime ----------

        public void RefreshAll()
        {
            if (GameManager.Instance == null) return;
            _coinsText.text = Localization.T("coins") + ": " + GameManager.Instance.Coins;
            _dayText.text = Localization.T("day") + " " + GameManager.Instance.Day;
            _seedTitleText.text = Localization.T("select_seed");
            _hintText.text = Localization.T("hint_empty") + " " + Localization.T("hint_ready");
            _langButton.GetComponentInChildren<Text>().text = Localization.T("lang_button");
            _nextDayButton.GetComponentInChildren<Text>().text = Localization.T("next_day");

            for (int i = 0; i < _seedButtons.Count; i++)
            {
                var crop = CropDatabase.Get(i);
                _seedButtonTexts[i].text = crop.DisplayName + "  " +
                    Localization.T("cost") + " " + crop.SeedCost + " / " +
                    Localization.T("sell") + " " + crop.SellPrice;
                _seedButtons[i].GetComponent<Image>().color =
                    i == GameManager.Instance.SelectedCropIndex ? SelectedColor : ButtonColor;
                _seedButtonTexts[i].color =
                    i == GameManager.Instance.SelectedCropIndex ? Color.black : Color.white;
            }
        }

        public void ShowToast(string message)
        {
            if (_toastText == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            _toastText.text = message;
            var color = _toastText.color;
            color.a = 1f;
            _toastText.color = color;
            yield return new WaitForSeconds(1.6f);
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                color.a = 1f - (t / 0.6f);
                _toastText.color = color;
                yield return null;
            }
            _toastText.text = "";
            _toastRoutine = null;
        }
    }
}
