using UnityEngine;
using UnityEngine.UI;

namespace FarmSim
{
    /// <summary>
    /// Helpers for building the HUD from code with a consistent, rounded,
    /// cartoon-ish look. Sprites are generated at runtime (no image files).
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color PanelDark = new Color(0.10f, 0.12f, 0.11f, 0.93f);
        public static readonly Color PanelLight = new Color(0.90f, 0.88f, 0.82f, 0.97f);
        public static readonly Color Green = new Color(0.40f, 0.50f, 0.31f, 1f);
        public static readonly Color GreenDark = new Color(0.26f, 0.33f, 0.21f, 1f);
        public static readonly Color Gold = new Color(0.84f, 0.68f, 0.34f, 1f);
        public static readonly Color GoldDark = new Color(0.58f, 0.45f, 0.22f, 1f);
        public static readonly Color TextDark = new Color(0.15f, 0.16f, 0.14f, 1f);

        private static Sprite _rounded;
        private static Sprite _circle;

        public static Sprite RoundedSprite
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int s = 96;
                const float radius = 26f;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float a = RoundedAlpha(x + 0.5f, y + 0.5f, s, radius);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(32f, 32f, 32f, 32f));
                _rounded.name = "RoundedSprite";
                return _rounded;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circle != null) return _circle;
                const int s = 64;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[s * s];
                float half = s * 0.5f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - half) / half;
                        float dy = (y + 0.5f - half) / half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * s * 0.5f);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
                _circle.name = "CircleSprite";
                return _circle;
            }
        }

        private static float RoundedAlpha(float x, float y, int size, float radius)
        {
            float min = radius;
            float max = size - radius;
            float cx = Mathf.Clamp(x, min, max);
            float cy = Mathf.Clamp(y, min, max);
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // 1px soft edge so the corners look smooth at any scale
            return Mathf.Clamp01(radius - dist);
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool sliced)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(Transform parent, string name, Font font, int size,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color, string label, Font font, int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            var image = CreateImage(parent, name, RoundedSprite, color, true);
            image.raycastTarget = true;
            var rt = image.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText(image.transform, "Label", font, fontSize, TextAnchor.MiddleCenter, Color.white);
                var trt = text.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(8f, 2f);
                trt.offsetMax = new Vector2(-8f, -2f);
                text.text = label;
            }

            return button;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }
}
