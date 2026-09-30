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
