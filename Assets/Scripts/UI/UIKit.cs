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
        public static readonly Color Ink = new Color32(0x22, 0x36, 0x78, 255);        // main dark text
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

        /// <summary>Capsule colors taken from the reference picture: yellow, purple, green, blue, magenta, red.</summary>
        public static readonly Color[] Capsules =
        {
            new Color32(0xF9, 0xBB, 0x13, 255), new Color32(0x92, 0x4E, 0xF3, 255), new Color32(0x41, 0xC0, 0x38, 255),
            new Color32(0x17, 0x8A, 0xDC, 255), new Color32(0xE0, 0x52, 0xD6, 255), new Color32(0xF1, 0x4D, 0x53, 255),
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

        struct PillDef { public Color Rim, Body; public bool Gloss; }
        static PillDef PD(string rim, string body, bool gloss)
        {
            ColorUtility.TryParseHtmlString(rim, out var r); ColorUtility.TryParseHtmlString(body, out var b);
            return new PillDef { Rim = r, Body = b, Gloss = gloss };
        }
        static readonly Dictionary<string, PillDef> PillDefs = new Dictionary<string, PillDef>
        {
            { "btn_green", PD("#FFF3C9", "#4CC93A", true) }, { "btn_yellow", PD("#FFF3C9", "#FFC32A", true) },
            { "btn_blue", PD("#FFF3C9", "#2E9BF5", true) }, { "btn_red", PD("#FFF3C9", "#F25555", true) },
            { "btn_purple", PD("#FFF3C9", "#9358F0", true) }, { "btn_orange", PD("#FFF3C9", "#FF8A2B", true) },
            { "btn_pink", PD("#FFF3C9", "#FF5FA8", true) }, { "btn_grey", PD("#F1F3F8", "#B7C0D4", true) },
            { "btn_white", PD("#DDE8F8", "#FFFFFF", false) }, { "btn_navy", PD("#FFF3C9", "#26346B", true) },
            { "chip", PD("#FFFFFF", "#FFFAE6", false) }, { "navbar", PD("#FFFFFF", "#FFFBEA", false) },
            { "track", PD("#FFFFFF", "#E1EEFD", false) }, { "fill", PD("#00000000", "#4CC93A", true) },
        };

        public static bool IsPillKey(string key) => key != null && PillDefs.ContainsKey(key);

        /// <summary>Darker shade of a pill's body color, used for the text outline on buttons.</summary>
        public static Color PillOutline(string key)
        {
            if (!PillDefs.TryGetValue(key ?? "", out var d)) return new Color(0.05f, 0.1f, 0.25f, 0.85f);
            Color.RGBToHSV(d.Body, out var h, out var sat, out var v);
            return Color.HSVToRGB(h, Mathf.Min(1f, sat + 0.15f), v * 0.5f);
        }

        /// <summary>Changes the colored body of a pill or tile (the rim keeps its color).</summary>
        public static void SetBodyColor(Image img, Color c)
        {
            var body = img.transform.Find("Body");
            if (body != null) body.GetComponent<Image>().color = c; else img.color = c;
        }

        /// <summary>Gives an Image the look of `key`: exact pills and cards are drawn in code, everything else uses painted art.</summary>
        public static void ApplyStyle(Image img, string key, float height = 100f)
        {
            img.type = Image.Type.Sliced; img.preserveAspect = false;
            var fit = img.GetComponent<SliceFitter>();
            if (IsPillKey(key))
            {
                var def = PillDefs[key];
                img.sprite = Shapes.FlatPill(); img.color = def.Rim;
                if (fit == null) fit = img.gameObject.AddComponent<SliceFitter>();
                fit.Mode = SliceFitter.Kind.Pill;
                float rimPx = def.Rim.a < 0.01f ? 0f : Mathf.Clamp(height * 0.085f, 4f, 10f);
                var bodyT = img.transform.Find("Body");
                Image bodyImg;
                if (bodyT == null)
                {
                    var bn = Node(img.transform, "Body"); bodyT = bn;
                    bodyImg = bn.gameObject.AddComponent<Image>();
                    bodyImg.sprite = Shapes.PillBody(); bodyImg.type = Image.Type.Sliced; bodyImg.raycastTarget = false;
                    bn.gameObject.AddComponent<SliceFitter>().Mode = SliceFitter.Kind.Pill;
                    bn.SetAsFirstSibling();
                }
                else bodyImg = bodyT.GetComponent<Image>();
                var br = (RectTransform)bodyT; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = new Vector2(rimPx, rimPx); br.offsetMax = new Vector2(-rimPx, -rimPx);
                bodyImg.color = def.Body;
                var gloss = bodyT.Find("Gloss");
                if (def.Gloss && gloss == null)
                {
                    var g = Node(bodyT, "Gloss");
                    var gi = g.gameObject.AddComponent<Image>();
                    gi.sprite = Shapes.Gloss(); gi.type = Image.Type.Sliced; gi.raycastTarget = false;
                    gi.pixelsPerUnitMultiplier = 64f / Mathf.Max(8f, height * 0.38f);
                    g.anchorMin = new Vector2(0, 0.55f); g.anchorMax = new Vector2(1, 1);
                    g.offsetMin = new Vector2(height * 0.24f, 0); g.offsetMax = new Vector2(-height * 0.24f, -height * 0.06f);
                }
                else if (!def.Gloss && gloss != null) UnityEngine.Object.Destroy(gloss.gameObject);
            }
            else if (Shapes.IsCardKey(key))
            {
                img.sprite = Shapes.CardVariant(key); img.color = Color.white;
                if (fit == null) fit = img.gameObject.AddComponent<SliceFitter>();
                fit.Mode = SliceFitter.Kind.Card; fit.CardBorder = key.StartsWith("popup") ? 72f : 50f;
            }
            else
            {
                img.sprite = Art.Get(key);
                if (fit != null) UnityEngine.Object.Destroy(fit);
                if (img.sprite != null) img.pixelsPerUnitMultiplier = Mathf.Max(0.1f, img.sprite.rect.height / Mathf.Max(1, height));
            }
        }

        /// <summary>Rounded-square icon button: cream rim, colored glossy body, white symbol (glyph) in the middle.</summary>
        public static Image GlyphButton(Transform parent, string glyphKey, Color body, float size, string name = "GlyphButton")
        {
            var rim = Node(parent, name).gameObject.AddComponent<Image>();
            rim.sprite = Shapes.TileFlat(); rim.type = Image.Type.Sliced; rim.color = new Color32(0xFF, 0xF3, 0xC9, 255);
            var rr = rim.rectTransform; rr.sizeDelta = new Vector2(size, size);
            float inset = Mathf.Clamp(size * 0.075f, 3f, 8f);
            var b = Node(rim.transform, "Body"); var bi = b.gameObject.AddComponent<Image>();
            bi.sprite = Shapes.TileBody(); bi.type = Image.Type.Sliced; bi.color = body; bi.raycastTarget = false;
            Stretch(b, inset, inset, inset, inset);
            var gl = Node(b, "Gloss"); var gi = gl.gameObject.AddComponent<Image>();
            gi.sprite = Shapes.TileGloss(); gi.type = Image.Type.Sliced; gi.raycastTarget = false;
            gl.anchorMin = new Vector2(0, 0.55f); gl.anchorMax = new Vector2(1, 1); gl.offsetMin = new Vector2(size * 0.16f, 0); gl.offsetMax = new Vector2(-size * 0.16f, -size * 0.05f);
            gi.pixelsPerUnitMultiplier = 64f / Mathf.Max(8f, size * 0.3f);
            // nine-slice corner size follows the button size
            rim.pixelsPerUnitMultiplier = 128f / Mathf.Max(8f, size) * 0.9f; bi.pixelsPerUnitMultiplier = 128f / Mathf.Max(8f, size - inset * 2) * 0.9f;
            var glyph = Img(rim.transform, glyphKey, "Glyph");
            glyph.rectTransform.anchorMin = glyph.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glyph.rectTransform.anchoredPosition = new Vector2(0, size * 0.015f);
            glyph.rectTransform.sizeDelta = new Vector2(size * 0.58f, size * 0.58f);
            glyph.raycastTarget = false;
            return rim;
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
            var ol = t.GetComponent<Outline>(); if (ol != null) { ol.effectColor = PillOutline(spriteKey); ol.effectDistance = new Vector2(3, -3); }
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
                Fill.gameObject.SetActive(Value > 0.001f);
                if (Value > 0.001f && w < minW) rt.offsetMax = new Vector2(pad + minW, -3);
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
            if (fillColor.HasValue) SetBodyColor(bar.Fill, fillColor.Value);
            bar.Setup(4, h - 8f);
            bar.Set(0);
            return bar;
        }
    }
}
