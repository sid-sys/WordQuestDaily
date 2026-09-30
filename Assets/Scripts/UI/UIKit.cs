using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordQuest
{
    public static class Palette
    {
        public static readonly Color Ink = new Color32(0x1B, 0x2A, 0x49, 255);        // main dark text
        public static readonly Color InkSoft = new Color32(0x5B, 0x6B, 0x8C, 255);
        public static readonly Color Navy = new Color32(0x18, 0x24, 0x4A, 255);
        public static readonly Color Cream = new Color32(0xFF, 0xF8, 0xE6, 255);
        public static readonly Color White = Color.white;
        public static readonly Color Green = new Color32(0x3C, 0xC2, 0x5A, 255);
        public static readonly Color Yellow = new Color32(0xFF, 0xC8, 0x2E, 255);
        public static readonly Color Orange = new Color32(0xFF, 0x8A, 0x2B, 255);
        public static readonly Color Red = new Color32(0xF0, 0x4B, 0x4B, 255);
        public static readonly Color Blue = new Color32(0x2F, 0x9B, 0xF5, 255);
        public static readonly Color Purple = new Color32(0x8E, 0x5B, 0xE8, 255);
        public static readonly Color Pink = new Color32(0xFF, 0x5F, 0x9E, 255);
        public static readonly Color Teal = new Color32(0x1F, 0xB5, 0xA8, 255);
        public static readonly Color Grey = new Color32(0xA9, 0xB3, 0xC7, 255);

        /// <summary>Bright colors for the found-word capsules.</summary>
        public static readonly Color[] Capsules =
        {
            new Color32(0xFF, 0x3B, 0x5C, 255), new Color32(0x1E, 0x90, 0xFF, 255), new Color32(0x22, 0xC5, 0x5E, 255),
            new Color32(0x8B, 0x3D, 0xFF, 255), new Color32(0xFF, 0x9F, 0x1C, 255), new Color32(0x12, 0xC8, 0xE6, 255),
            new Color32(0xFF, 0x4F, 0xA0, 255), new Color32(0xF5, 0xC4, 0x00, 255),
        };
    }

    /// <summary>Loads sprites from Resources/Art by name.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("Art/" + key);
            if (s == null) Debug.LogWarning("Missing art: " + key);
            cache[key] = s;
            return s;
        }
    }

    /// <summary>Press-down squash and click sound for every button.</summary>
    public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Down = 0.93f;
        Vector3 baseScale = Vector3.one; bool init;
        void Init() { if (!init) { baseScale = transform.localScale; init = true; } }
        public void OnPointerDown(PointerEventData e) { Init(); transform.localScale = baseScale * Down; }
        public void OnPointerUp(PointerEventData e) { Init(); transform.localScale = baseScale; }
        public void OnPointerExit(PointerEventData e) { Init(); transform.localScale = baseScale; }
        void OnDisable() { if (init) transform.localScale = baseScale; }
    }

    /// <summary>Gentle breathing scale for highlighted things.</summary>
    public class Pulse : MonoBehaviour
    {
        public float Amount = 0.08f, Speed = 3f; Vector3 b; void Awake() { b = transform.localScale; }
        void Update() { transform.localScale = b * (1f + Amount * Mathf.Sin(Time.unscaledTime * Speed)); }
    }

    public static class UI
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        // ---------- layout helpers ----------
        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchors at (ax, ay), pivot centered, position x/y from that anchor, size w x h.</summary>
        public static RectTransform Place(RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        // ---------- images ----------
        /// <summary>For left aligned text: x is the LEFT edge of the box (not its center).</summary>
        public static RectTransform PlaceL(RectTransform rt, float ax, float ay, float xLeft, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(xLeft, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Img(Transform parent, string spriteKey, string name = null, bool raycast = false)
        {
            var rt = Node(parent, name ?? spriteKey ?? "Image");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Get(spriteKey);
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            if (img.sprite == null) img.color = new Color(1, 1, 1, 0);
            return img;
        }

        public static Image Icon(Transform parent, string key, float size, string name = null)
        {
            var img = Img(parent, key, name);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Image Round(Transform parent, Color c, int radius = 30, string name = "Round", bool raycast = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Shapes.RoundRect(radius); img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = raycast;
            return img;
        }

        public static Image FlatPill(Transform parent, Color c, string name = "Pill")
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Shapes.FlatPill(); img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
            var fit = rt.gameObject.AddComponent<SliceFitter>(); fit.Mode = SliceFitter.Kind.Pill;
            return img;
        }

        public static Image Solid(Transform parent, Color c, string name = "Solid", bool raycast = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c; img.raycastTarget = raycast;
            return img;
        }

        static readonly Dictionary<string, Color> PillColors = new Dictionary<string, Color>
        {
            { "btn_green", new Color32(0x3C, 0xC2, 0x5A, 255) }, { "btn_yellow", new Color32(0xFF, 0xC8, 0x2E, 255) },
            { "btn_blue", new Color32(0x2F, 0x9B, 0xF5, 255) }, { "btn_red", new Color32(0xF0, 0x4B, 0x4B, 255) },
            { "btn_purple", new Color32(0x8E, 0x5B, 0xE8, 255) }, { "btn_grey", new Color32(0xA9, 0xB3, 0xC7, 255) },
            { "btn_white", new Color32(0xF4, 0xF6, 0xFB, 255) }, { "btn_navy", new Color32(0x18, 0x24, 0x4A, 255) },
            { "btn_orange", new Color32(0xFF, 0x8A, 0x2B, 255) }, { "btn_pink", new Color32(0xFF, 0x5F, 0x9E, 255) },
            { "chip", new Color32(0x1B, 0x27, 0x4D, 235) }, { "navbar", new Color32(0x14, 0x1E, 0x44, 245) },
            { "track", new Color32(0x14, 0x1E, 0x3F, 255) }, { "fill", new Color32(0x3C, 0xC2, 0x5A, 255) },
        };

        public static bool IsPillKey(string key) => key != null && PillColors.ContainsKey(key);

        /// <summary>Gives an Image the look of `key`: exact pills and cards are drawn in code, everything else uses painted art.</summary>
        public static void ApplyStyle(Image img, string key, float height = 100f)
        {
            img.type = Image.Type.Sliced; img.preserveAspect = false;
            var fit = img.GetComponent<SliceFitter>();
            var gloss = img.transform.Find("Gloss");
            if (IsPillKey(key))
            {
                img.sprite = Shapes.Pill(); img.color = PillColors[key];
                if (fit == null) fit = img.gameObject.AddComponent<SliceFitter>();
                fit.Mode = SliceFitter.Kind.Pill;
                bool wantGloss = key.StartsWith("btn_") || key == "fill";
                if (wantGloss && gloss == null)
                {
                    var g = Node(img.transform, "Gloss");
                    var gi = g.gameObject.AddComponent<Image>();
                    gi.sprite = Shapes.Gloss(); gi.type = Image.Type.Sliced; gi.raycastTarget = false;
                    gi.pixelsPerUnitMultiplier = 64f / Mathf.Max(8f, height * 0.40f);
                    g.anchorMin = new Vector2(0, 0.52f); g.anchorMax = new Vector2(1, 1);
                    g.offsetMin = new Vector2(height * 0.26f, 0); g.offsetMax = new Vector2(-height * 0.26f, -height * 0.08f);
                }
                else if (!wantGloss && gloss != null) UnityEngine.Object.Destroy(gloss.gameObject);
            }
            else if (key == "card_a" || key == "card_b" || key == "popup")
            {
                bool pop = key == "popup";
                img.sprite = Shapes.Card(pop ? 1 : 0); img.color = Color.white;
                if (fit == null) fit = img.gameObject.AddComponent<SliceFitter>();
                fit.Mode = SliceFitter.Kind.Card; fit.CardBorder = pop ? 68f : 48f;
            }
            else
            {
                img.sprite = Art.Get(key);
                if (fit != null) UnityEngine.Object.Destroy(fit);
                if (img.sprite != null) img.pixelsPerUnitMultiplier = Mathf.Max(0.1f, img.sprite.rect.height / Mathf.Max(1, height));
            }
        }

        /// <summary>Nine-sliced image. Pills and cards are drawn in code so their corners are exact.</summary>
        public static Image Sliced(Transform parent, string key, float height, string name = null, bool raycast = false)
        {
            var rt = Node(parent, name ?? key ?? "Image");
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            ApplyStyle(img, key, height);
            if (img.sprite == null) img.color = new Color(1, 1, 1, 0);
            return img;
        }

        // ---------- text ----------
        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = false)
        {
            var rt = Node(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.05f, 0.1f, 0.25f, 0.85f); o.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        public static Text Wrapped(Transform parent, string text, int size, Color color, float width, TextAnchor anchor = TextAnchor.UpperCenter)
        {
            var t = Label(parent, text, size, color, anchor);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.rectTransform.sizeDelta = new Vector2(width, t.rectTransform.sizeDelta.y);
            return t;
        }

        // ---------- buttons ----------
        public static Button Click(Graphic target, Action onClick, bool sound = true)
        {
            target.raycastTarget = true;
            var b = target.gameObject.GetComponent<Button>();
            if (b == null) b = target.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = target;
            if (target.gameObject.GetComponent<ButtonFx>() == null) target.gameObject.AddComponent<ButtonFx>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { if (sound) Sfx.Play(Sfx.Kind.Click); onClick?.Invoke(); });
            return b;
        }

        /// <summary>A pill button from the art set with a centered label.</summary>
        public static Button Pill(Transform parent, string spriteKey, string label, float w, float h, Action onClick, int fontSize = 44)
        {
            var img = Sliced(parent, spriteKey, h, "Button_" + label, true);
            img.rectTransform.sizeDelta = new Vector2(w, h);
            var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            Stretch(t.rectTransform, 0, 4, 0, 0);
            return Click(img, onClick);
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Round icon button (back, close, settings ...).</summary>
        public static Button Round(Transform parent, string key, float size, Action onClick)
        {
            var img = Icon(parent, key, size, key);
            return Click(img, onClick);
        }

        // ---------- progress bar ----------
        public class Bar
        {
            public RectTransform Root; public Image Fill; public Text Label; float pad, minW; public float Value;
            public void Set(float v)
            {
                Value = Mathf.Clamp01(v);
                float full = Root.rect.width - pad * 2;
                var rt = Fill.rectTransform;
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.offsetMin = new Vector2(pad, 3); rt.offsetMax = new Vector2(pad + Mathf.Lerp(0, full, Value), -3);
                float w = rt.rect.width;
                Fill.enabled = Value > 0.001f;
                if (Fill.enabled && w < minW) rt.offsetMax = new Vector2(pad + minW, -3);
            }
            public void Setup(float padding, float min) { pad = padding; minW = min; }
        }

        public static Bar ProgressBar(Transform parent, float w, float h, Color? fillColor = null, string name = "Bar")
        {
            var bar = new Bar { Root = Node(parent, name) };
            bar.Root.sizeDelta = new Vector2(w, h);
            var track = Sliced(bar.Root, "track", h, "Track");
            Stretch(track.rectTransform);
            bar.Fill = Sliced(bar.Root, "fill", h - 6, "Fill");
            if (fillColor.HasValue) bar.Fill.color = fillColor.Value;
            bar.Setup(4, h - 8f);
            bar.Set(0);
            return bar;
        }
    }
}
