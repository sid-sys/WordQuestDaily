using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>
    /// Shapes drawn in code: exact pills, rounded cards and circles. Using these (instead of painted art) for
    /// backgrounds keeps every corner a true half circle and lets content sit exactly in the middle.
    /// </summary>
    public static class Shapes
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int w, int h, System.Func<int, int, Color32> pixel, Vector4 border)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) px[y * w + x] = pixel(x, y);
            tex.SetPixels32(px); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            cache[key] = s;
            return s;
        }

        // distance from a pixel centre to the edge of a capsule / rounded rect (negative inside)
        static float RoundDist(float x, float y, float w, float h, float r)
        {
            float dx = Mathf.Max(Mathf.Abs(x - w / 2f) - (w / 2f - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(y - h / 2f) - (h / 2f - r), 0f);
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        static float Cov(float d) => Mathf.Clamp01(0.5f - d);   // anti-aliased edge

        /// <summary>A plain white rounded rectangle (tint it).</summary>
        public static Sprite RoundRect(int radius = 30)
        {
            int size = radius * 2 + 4;
            return Make("rr" + radius, size, size, (x, y) => new Color32(255, 255, 255, (byte)(Cov(RoundDist(x + 0.5f, y + 0.5f, size, size, radius)) * 255)), new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }

        public static Sprite Circle()
        {
            const int s = 128;
            return Make("circle", s, s, (x, y) => new Color32(255, 255, 255, (byte)(Cov(RoundDist(x + 0.5f, y + 0.5f, s, s, s / 2f)) * 255)), Vector4.zero);
        }

        /// <summary>
        /// Glossy pill: 128 px tall, ends are exact half circles (radius 64). Gray body with a darker rim, so a tint color
        /// gives a matching body and rim. Nine-sliced: only the middle stretches.
        /// </summary>
        public static Sprite Pill()
        {
            const int w = 256, h = 128, r = 64, rim = 7;
            return Make("pill", w, h, (x, y) =>
            {
                float d = RoundDist(x + 0.5f, y + 0.5f, w, h, r);
                float a = Cov(d);
                if (a <= 0) return new Color32(0, 0, 0, 0);
                float v;
                float t = (y + 0.5f) / h;                       // 0 bottom .. 1 top
                if (d > -rim) v = 0.50f;                        // rim
                else
                {
                    v = Mathf.Lerp(0.80f, 1.0f, Mathf.SmoothStep(0f, 1f, t));      // body gradient
                    if (d > -rim - 7 && t < 0.5f) v *= 0.93f;                      // soft inner bottom edge
                }
                byte g = (byte)(Mathf.Clamp01(v) * 255);
                return new Color32(g, g, g, (byte)(a * 255));
            }, new Vector4(r, 0, r, 0));
        }

        /// <summary>Rounded card with a colored rim. kind 0 = light card (blue-gray rim), 1 = popup (teal rim, thicker).</summary>
        public static Sprite Card(int kind)
        {
            int radius = kind == 1 ? 56 : 40, rim = kind == 1 ? 12 : 7, size = radius * 2 + 8;
            Color rimC = kind == 1 ? new Color(0.13f, 0.62f, 0.60f) : new Color(0.56f, 0.64f, 0.78f);
            Color fillTop = Color.white, fillBottom = kind == 1 ? new Color(0.98f, 0.96f, 0.88f) : new Color(0.93f, 0.95f, 0.99f);
            return Make("card" + kind, size, size, (x, y) =>
            {
                float d = RoundDist(x + 0.5f, y + 0.5f, size, size, radius);
                float a = Cov(d);
                if (a <= 0) return new Color32(0, 0, 0, 0);
                Color c = d > -rim ? rimC : Color.Lerp(fillBottom, fillTop, (y + 0.5f) / size);
                return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(a * 255));
            }, new Vector4(radius + 4, radius + 4, radius + 4, radius + 4));
        }

        /// <summary>Glossy pill body without a rim (gray, tint it): lighter on top, darker lip at the bottom.</summary>
        public static Sprite PillBody()
        {
            const int w = 256, h = 128, r = 64;
            return Make("pillbody", w, h, (x, y) =>
            {
                float d = RoundDist(x + 0.5f, y + 0.5f, w, h, r);
                float a = Cov(d);
                if (a <= 0) return new Color32(0, 0, 0, 0);
                float t = (y + 0.5f) / h;                                  // 0 bottom .. 1 top
                float v = Mathf.Lerp(0.84f, 1.0f, Mathf.SmoothStep(0f, 1f, t));
                if (t < 0.17f) v *= 0.86f;                                // darker lip under the button
                byte g = (byte)(Mathf.Clamp01(v) * 255);
                return new Color32(g, g, g, (byte)(a * 255));
            }, new Vector4(r, 0, r, 0));
        }

        /// <summary>Rounded-square body (gray, tint it) for icon buttons.</summary>
        public static Sprite TileBody()
        {
            const int s = 128, r = 38;
            return Make("tilebody", s, s, (x, y) =>
            {
                float a = Cov(RoundDist(x + 0.5f, y + 0.5f, s, s, r));
                if (a <= 0) return new Color32(0, 0, 0, 0);
                float t = (y + 0.5f) / s;
                float v = Mathf.Lerp(0.84f, 1.0f, Mathf.SmoothStep(0f, 1f, t));
                if (t < 0.16f) v *= 0.86f;
                byte g = (byte)(Mathf.Clamp01(v) * 255);
                return new Color32(g, g, g, (byte)(a * 255));
            }, new Vector4(r + 4, r + 4, r + 4, r + 4));
        }

        public static Sprite TileFlat()
        {
            const int s = 128, r = 38;
            return Make("tileflat", s, s, (x, y) => new Color32(255, 255, 255, (byte)(Cov(RoundDist(x + 0.5f, y + 0.5f, s, s, r)) * 255)), new Vector4(r + 4, r + 4, r + 4, r + 4));
        }

        /// <summary>Soft round white gloss for the top of icon buttons.</summary>
        public static Sprite TileGloss()
        {
            const int w = 128, h = 64, r = 32;
            return Make("tilegloss", w, h, (x, y) =>
            {
                float a = Cov(RoundDist(x + 0.5f, y + 0.5f, w, h, r));
                float t = (y + 0.5f) / h;
                return new Color32(255, 255, 255, (byte)(a * Mathf.Lerp(0.04f, 0.38f, t) * 255));
            }, new Vector4(r, 0, r, 0));
        }

        struct CardLook { public Color Outer, Rim, Top, Bottom; public int OuterPx, RimPx, Radius; }
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static CardLook L(string outer, string rim, string top, string bottom, int outerPx, int rimPx, int radius) =>
            new CardLook { Outer = Hex(outer), Rim = Hex(rim), Top = Hex(top), Bottom = Hex(bottom), OuterPx = outerPx, RimPx = rimPx, Radius = radius };

        static readonly Dictionary<string, CardLook> Looks = new Dictionary<string, CardLook>
        {
            { "card_a", L("#E9B84A", "#FFFFFF", "#FFFFFF", "#F0F6FF", 3, 6, 40) },
            { "card_b", L("#E9B84A", "#FFFFFF", "#FFFFFF", "#F0F6FF", 3, 6, 40) },
            { "card_gold", L("#E0A21C", "#FFF1B8", "#FFF7D6", "#FFE9A0", 3, 6, 40) },
            { "card_green", L("#4FB868", "#E9FBEA", "#F1FFF1", "#CFF2D3", 3, 6, 40) },
            { "card_red", L("#E8737F", "#FFEFF1", "#FFF3F4", "#FFD6DB", 3, 6, 40) },
            { "card_blue", L("#5FA8E8", "#EAF6FF", "#F2F9FF", "#D3EAFF", 3, 6, 40) },
            { "popup", L("#6BB6DE", "#FFFFFF", "#E3F6FF", "#C5E9FA", 4, 12, 56) },
            { "popup_cream", L("#E0A23A", "#FFFFFF", "#FFFDF0", "#FFF0C2", 4, 12, 56) },
            { "popup_pink", L("#E88BC2", "#FFFFFF", "#FFF2FA", "#FFD9EE", 4, 12, 56) },
            { "popup_mint", L("#5BC08A", "#FFFFFF", "#EEFFF4", "#CFF5DE", 4, 12, 56) },
            { "popup_lilac", L("#9C7BE0", "#FFFFFF", "#F5F0FF", "#DCD0FA", 4, 12, 56) },
        };

        public static bool IsCardKey(string key) => key != null && Looks.ContainsKey(key);

        /// <summary>Baked card: colored outline, white rim, soft gradient body. Nine-sliced.</summary>
        public static Sprite CardVariant(string key)
        {
            var look = Looks[key];
            int size = look.Radius * 2 + 12;
            return Make("cv_" + key, size, size, (x, y) =>
            {
                float d = RoundDist(x + 0.5f, y + 0.5f, size, size, look.Radius);
                float a = Cov(d);
                if (a <= 0) return new Color32(0, 0, 0, 0);
                Color c;
                if (d > -look.OuterPx) c = look.Outer;
                else if (d > -(look.OuterPx + look.RimPx)) c = look.Rim;
                else c = Color.Lerp(look.Bottom, look.Top, (y + 0.5f) / size);
                return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(a * 255));
            }, new Vector4(look.Radius + 6, look.Radius + 6, look.Radius + 6, look.Radius + 6));
        }

        /// <summary>Flat white pill (exact half-circle ends), tint it. Used for the word capsules on the board.</summary>
        public static Sprite FlatPill()
        {
            const int w = 256, h = 128, r = 64;
            return Make("flatpill", w, h, (x, y) => new Color32(255, 255, 255, (byte)(Cov(RoundDist(x + 0.5f, y + 0.5f, w, h, r)) * 255)), new Vector4(r, 0, r, 0));
        }

        /// <summary>Soft white-to-clear gloss strip for the top half of pills.</summary>
        public static Sprite Gloss()
        {
            const int w = 128, h = 64, r = 32;
            return Make("gloss", w, h, (x, y) =>
            {
                float a = Cov(RoundDist(x + 0.5f, y + 0.5f, w, h, r));
                float t = (y + 0.5f) / h;                        // 1 at top
                return new Color32(255, 255, 255, (byte)(a * Mathf.Lerp(0.05f, 0.42f, t) * 255));
            }, new Vector4(r, 0, r, 0));
        }
    }

    /// <summary>Keeps nine-slice corner size correct for whatever size the image really gets.</summary>
    [RequireComponent(typeof(Image))]
    public class SliceFitter : MonoBehaviour
    {
        public enum Kind { Pill, Card }
        public Kind Mode = Kind.Pill;
        public float CardBorder = 48f;
        Image img;
        void OnEnable() { Apply(); }
        void OnRectTransformDimensionsChange() { Apply(); }
        void Apply()
        {
            if (img == null) img = GetComponent<Image>();
            if (img == null || img.sprite == null) return;
            var r = ((RectTransform)transform).rect;
            float h = Mathf.Max(8f, r.height), w = Mathf.Max(8f, r.width);
            if (Mode == Kind.Pill)
            {
                float hh = Mathf.Min(h, w);                      // a pill can never be taller than wide
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / hh;
            }
            else
            {
                float maxBorder = Mathf.Min(w, h) * 0.46f;
                img.pixelsPerUnitMultiplier = Mathf.Max(1f, CardBorder / maxBorder);
            }
        }
    }
}
