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

        /// <summary>Colors for the found-word capsules.</summary>
        public static readonly Color[] Capsules =
        {
            new Color32(0xFF, 0x5F, 0x6D, 255), new Color32(0xFF, 0x9F, 0x1C, 255), new Color32(0xFF, 0xD1, 0x2E, 255),
            new Color32(0x4C, 0xD1, 0x64, 255), new Color32(0x2F, 0xC4, 0xC9, 255), new Color32(0x3D, 0x8B, 0xFF, 255),
            new Color32(0x9B, 0x5B, 0xF0, 255), new Color32(0xFF, 0x62, 0xB6, 255),
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

    public static class Shapes
    {
        static readonly Dictionary<int, Sprite> round = new Dictionary<int, Sprite>();
        /// <summary>A plain white rounded rectangle, nine-sliced. Tint it with Image.color.</summary>
        public static Sprite RoundRect(int radius = 30)
        {
            if (round.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float c = size / 2f - 0.5f, half = size / 2f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - c) - (half - radius), 0f), dy = Mathf.Max(Mathf.Abs(y - c) - (half - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
            round[radius] = sp;
            return sp;
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

        public static Image Solid(Transform parent, Color c, string name = "Solid", bool raycast = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c; img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Nine-sliced image. `height` is how tall it will be drawn; the corner size scales with it.</summary>
        public static Image Sliced(Transform parent, string key, float height, string name = null, bool raycast = false)
        {
            var img = Img(parent, key, name, raycast);
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            if (img.sprite != null) img.pixelsPerUnitMultiplier = Mathf.Max(0.1f, img.sprite.rect.height / Mathf.Max(1, height));
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
            bar.Setup(4, h * 0.6f);
            bar.Set(0);
            return bar;
        }
    }
}
