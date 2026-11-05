using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmSim
{
    /// <summary>
    /// The barn / market overlay: every stored crop, today's price and a
    /// one-tap sell button per row, plus "sell everything".
    /// Built in code, hidden until the market button is pressed.
    /// </summary>
    public class MarketPanel
    {
        private GameObject _overlay;
        private CanvasGroup _group;
        private Text _titleText;
        private Text _emptyText;
        private Text _totalText;
        private Button _sellAllButton;
        private Text _sellAllLabel;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<Text> _nameTexts = new List<Text>();
        private readonly List<Text> _countTexts = new List<Text>();
        private readonly List<Text> _priceTexts = new List<Text>();
        private readonly List<Button> _sellButtons = new List<Button>();
        private readonly List<Text> _sellLabels = new List<Text>();
        private readonly List<Image> _icons = new List<Image>();

        public bool IsVisible { get; private set; }

        // ---------- construction ----------

        public void Build(Transform parent)
        {
            var overlay = UiFactory.CreateImage(parent, "MarketOverlay", null,
                new Color(0.03f, 0.05f, 0.04f, 0.74f), false);
            overlay.raycastTarget = true;
            UiFactory.Place(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _overlay = overlay.gameObject;

            _group = _overlay.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _overlay.SetActive(false);

            var card = UiFactory.CreateImage(overlay.transform, "Card", UiFactory.RoundedSprite,
                UiFactory.PanelDark, true);
            card.raycastTarget = true;
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-640f, -430f), new Vector2(640f, 430f));

            // title
            _titleText = UiFactory.CreateText(card.transform, "Title", GameFonts.Bold, 44,
                TextAnchor.MiddleLeft, new Color(0.95f, 0.92f, 0.86f));
            UiFactory.Place(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(40f, -92f), new Vector2(-140f, -26f));
            _titleText.text = Localization.T("market");

            // close button
            UiFactory.CreateButton(card.transform, "Close",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-104f, -88f), new Vector2(-32f, -30f),
                new Color(0.26f, 0.28f, 0.24f), "X", GameFonts.Bold, 34, Hide);

            // rows
            for (int i = 0; i < CropDatabase.Count; i++)
                BuildRow(card.transform, i);

            // empty state
            _emptyText = UiFactory.CreateText(card.transform, "Empty", GameFonts.Regular, 34,
                TextAnchor.MiddleCenter, new Color(0.80f, 0.80f, 0.74f, 0.9f));
            UiFactory.Place(_emptyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(60f, 150f), new Vector2(-60f, -120f));
            _emptyText.text = Localization.T("store_empty");

            // footer: barn value + sell all
            _totalText = UiFactory.CreateText(card.transform, "Total", GameFonts.SemiBold, 36,
                TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.62f));
            UiFactory.Place(_totalText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(40f, 30f), new Vector2(-420f, 110f));

            _sellAllButton = UiFactory.CreateButton(card.transform, "SellAll",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-380f, 28f), new Vector2(-36f, 112f),
                UiFactory.Gold, Localization.T("sell_all"), GameFonts.SemiBold, 34, OnSellAll);
            _sellAllLabel = _sellAllButton.GetComponentInChildren<Text>();

            _overlay.SetActive(false);
        }

        private void BuildRow(Transform parent, int index)
        {
            float top = 112f + index * 74f;
            float bottom = top + 66f;

            var row = UiFactory.CreateImage(parent, "Row" + index, UiFactory.RoundedSprite,
                index % 2 == 0 ? new Color(0.15f, 0.17f, 0.15f, 0.95f) : new Color(0.12f, 0.14f, 0.12f, 0.95f), true);
            UiFactory.Place(row.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(32f, -bottom), new Vector2(-32f, -top));
            _rows.Add(row.gameObject);

            var icon = UiFactory.CreateImage(row.transform, "Icon", UiFactory.RoundedSprite,
                CropDatabase.Get(index).RipeColor, true);
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, -24f), new Vector2(62f, 24f));
            _icons.Add(icon);

            var name = UiFactory.CreateText(row.transform, "Name", GameFonts.SemiBold, 32,
                TextAnchor.MiddleLeft, Color.white);
            UiFactory.Place(name.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(78f, 0f), new Vector2(400f, 0f));
            _nameTexts.Add(name);

            var count = UiFactory.CreateText(row.transform, "Count", GameFonts.Regular, 30,
                TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.78f));
            UiFactory.Place(count.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(410f, 0f), new Vector2(560f, 0f));
            _countTexts.Add(count);

            var price = UiFactory.CreateText(row.transform, "Price", GameFonts.Regular, 30,
                TextAnchor.MiddleLeft, new Color(0.88f, 0.82f, 0.62f));
            UiFactory.Place(price.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(570f, 0f), new Vector2(-260f, 0f));
            _priceTexts.Add(price);

            var sell = UiFactory.CreateButton(row.transform, "Sell",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-236f, -25f), new Vector2(-20f, 25f),
                UiFactory.Green, Localization.T("sell"), GameFonts.SemiBold, 28,
                () => OnSellRow(index));
            _sellButtons.Add(sell);
            _sellLabels.Add(sell.GetComponentInChildren<Text>());
        }

        // ---------- behaviour ----------

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (_overlay == null) return;
            _overlay.SetActive(true);
            IsVisible = true;
            _group.blocksRaycasts = true;
            _group.alpha = 1f;
            Refresh();
        }

        public void Hide()
        {
            if (_overlay == null) return;
            IsVisible = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _overlay.SetActive(false);
        }

        private void OnSellRow(int index)
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            int earned = manager.SellCrop(index);
            if (earned <= 0)
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                return;
            }

            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.PlayCoin();
                SfxManager.Instance.Haptic();
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast(
                    Localization.T("sold_for") + " +" + earned, false, true);
                UIManager.Instance.RefreshAll();
            }
            Refresh();
        }

        private void OnSellAll()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;

            int earned = manager.SellAll();
            if (earned <= 0)
            {
                if (SfxManager.Instance != null) SfxManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast(Localization.T("nothing_to_sell"), true);
                return;
            }

            if (SfxManager.Instance != null)
            {
                SfxManager.Instance.PlayCoin();
                SfxManager.Instance.Haptic();
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast(Localization.T("sold_for") + " +" + earned, false, true);
                UIManager.Instance.RefreshAll();
            }
            Refresh();
        }

        // ---------- refresh ----------

        public void Refresh()
        {
            var manager = GameManager.Instance;
            if (manager == null || _overlay == null) return;

            if (_titleText != null) _titleText.text = Localization.T("market");
            if (_emptyText != null) _emptyText.gameObject.SetActive(manager.StoredTotal == 0);

            for (int i = 0; i < _rows.Count; i++)
            {
                int stored = manager.StoredOf(i);
                int price = manager.MarketPrice[i];
                int value = stored * price;
                var crop = CropDatabase.Get(i);
                bool unlocked = manager.IsCropUnlocked(i);

                if (_icons.Count > i && _icons[i] != null)
                {
                    var color = crop.RipeColor;
                    if (!unlocked) color = Color.Lerp(color, new Color(0.35f, 0.35f, 0.35f), 0.7f);
                    if (stored == 0) color.a = 0.55f;
                    _icons[i].color = color;
                }

                if (_nameTexts.Count > i && _nameTexts[i] != null)
                {
                    // crop names stay visible (players like knowing what is coming),
                    // but locked ones are dimmed
                    _nameTexts[i].text = crop.DisplayName;
                    _nameTexts[i].color = unlocked
                        ? Color.white
                        : new Color(0.62f, 0.62f, 0.58f, 0.85f);
                }

                if (_countTexts.Count > i && _countTexts[i] != null)
                    _countTexts[i].text = "×" + stored;

                if (_priceTexts.Count > i && _priceTexts[i] != null)
                {
                    _priceTexts[i].text = stored > 0
                        ? price + " × " + stored + "  =  " + value
                        : Localization.T("market_price") + " " + price;
                }

                if (_sellButtons.Count > i && _sellButtons[i] != null)
                {
                    _sellButtons[i].interactable = stored > 0;
                    var image = _sellButtons[i].GetComponent<Image>();
                    if (image != null)
                        image.color = stored > 0 ? UiFactory.Green : new Color(0.22f, 0.24f, 0.21f);
                }

                if (_sellLabels.Count > i && _sellLabels[i] != null)
                    _sellLabels[i].text = stored > 0 ? value.ToString() : Localization.T("sell");
            }

            if (_totalText != null)
                _totalText.text = Localization.T("total_value") + ": " + manager.StoreValue;

            if (_sellAllButton != null)
            {
                _sellAllButton.interactable = manager.StoredTotal > 0;
                if (_sellAllLabel != null) _sellAllLabel.text = Localization.T("sell_all");
            }
        }
    }
}
