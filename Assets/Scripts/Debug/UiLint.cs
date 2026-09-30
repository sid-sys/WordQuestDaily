#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>
    /// Editor-only layout checker used in tests: finds text or icons that overlap each other or sit on the edge of
    /// the card / button they belong to. Run it from the test harness on every screen.
    /// </summary>
    public static class UiLint
    {
        class Item { public Graphic g; public string name; public Rect r; public bool isText; }

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }

        static Rect TextRect(Text t)
        {
            var box = WorldRect(t.rectTransform);
            float scale = t.canvas != null ? t.canvas.scaleFactor : 1f;
            float w = Mathf.Min(t.preferredWidth * scale, box.width), h = Mathf.Min(t.preferredHeight * scale, box.height);
            if (t.horizontalOverflow == HorizontalWrapMode.Overflow) w = t.preferredWidth * scale;
            float x = box.center.x - w / 2f, y = box.center.y - h / 2f;
            switch (t.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.MiddleLeft: case TextAnchor.LowerLeft: x = box.xMin; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight: x = box.xMax - w; break;
            }
            return new Rect(x, y, w, h);
        }

        static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (int i = 0; i < 3 && t.parent != null; i++) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        static bool IsBackground(Graphic g)
        {
            var img = g as Image;
            if (img == null) return false;
            if (img.type == Image.Type.Sliced || img.type == Image.Type.Tiled) return true;
            if (g.name == "Pulse" || g.name == "Chest" || g.name == "Close" || g.name == "Badge") return true;
            string n = g.name;
            return n == "Rim" || n == "Face" && g.transform.parent != null && g.transform.parent.name == "Pause" || n == "Pulse" || n == "Chest" || n == "Close" || n == "Badge" || n == "Gloss" || n == "Bg" || n == "Scrim" || n == "Painting" || n == "Shadow" || n == "Hit" || n == "Strike" || n == "Fill" || n == "Track";
        }

        static bool Clip(Graphic g, ref Rect r)
        {
            for (var t = g.transform.parent; t != null; t = t.parent)
            {
                var m = t.GetComponent<RectMask2D>();
                if (m == null) continue;
                var mr = WorldRect((RectTransform)t);
                float x0 = Mathf.Max(r.xMin, mr.xMin), x1 = Mathf.Min(r.xMax, mr.xMax), y0 = Mathf.Max(r.yMin, mr.yMin), y1 = Mathf.Min(r.yMax, mr.yMax);
                if (x1 <= x0 || y1 <= y0) return false;
                r = new Rect(x0, y0, x1 - x0, y1 - y0);
            }
            return true;
        }

        static bool IsAncestor(Transform a, Transform b)
        {
            for (var t = b; t != null; t = t.parent) if (t == a) return true;
            return false;
        }

        public static string Run(Transform root, bool verbose = false)
        {
            var items = new List<Item>();
            var containers = new List<Image>();
            foreach (var g in root.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.gameObject.activeInHierarchy || g.color.a < 0.1f) continue;
                var t = g as Text;
                if (t != null)
                {
                    if (string.IsNullOrEmpty(t.text)) continue;
                    var tr = TextRect(t); if (!Clip(g, ref tr)) continue;
                    items.Add(new Item { g = g, name = Path(g.transform) + " \"" + t.text + "\"", r = tr, isText = true });
                }
                else
                {
                    var img = g as Image;
                    if (img == null || img.sprite == null) continue;
                    if (IsBackground(g)) { if (img.type == Image.Type.Sliced && img.GetComponent<SliceFitter>() != null && g.name != "Fill" && g.name != "Track" && g.name != "Gloss") containers.Add(img); continue; }
                    var r = WorldRect(g.rectTransform);
                    if (r.width > 900 || r.height > 900) continue;
                    if (g.name.StartsWith("P") && g.name.Length == 1) continue;   // particles
                    if (!Clip(g, ref r)) continue;
                    items.Add(new Item { g = g, name = Path(g.transform), r = r });
                }
            }
            var sb = new StringBuilder();
            int problems = 0;
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i]; var b = items[j];
                    if (IsAncestor(a.g.transform, b.g.transform) || IsAncestor(b.g.transform, a.g.transform)) continue;
                    if ((a.g.transform.parent != null && a.g.transform.parent.name == "Badge") || (b.g.transform.parent != null && b.g.transform.parent.name == "Badge")) continue;
                    if (a.g.transform.parent != null && a.g.transform.parent.name == "Pause") continue;
                    // a text that lives inside an icon's parent (badge count) is fine when the parent is that icon
                    float ix = Mathf.Min(a.r.xMax, b.r.xMax) - Mathf.Max(a.r.xMin, b.r.xMin);
                    float iy = Mathf.Min(a.r.yMax, b.r.yMax) - Mathf.Max(a.r.yMin, b.r.yMin);
                    if (ix <= 0 || iy <= 0) continue;
                    float inter = ix * iy, small = Mathf.Min(a.r.width * a.r.height, b.r.width * b.r.height);
                    if (small <= 0 || inter / small < 0.14f) continue;
                    problems++; sb.AppendLine($"OVERLAP {a.name}  <->  {b.name}  ({Mathf.RoundToInt(inter / small * 100)}%)");
                }
            // items must sit inside the card or pill they are drawn on, with a small margin
            foreach (var it in items)
            {
                Image best = null; float bestArea = float.MaxValue;
                foreach (var c in containers)
                {
                    if (!IsAncestor(c.transform, it.g.transform) && !IsAncestor(c.transform.parent, it.g.transform)) continue;
                    var cr = WorldRect(c.rectTransform);
                    float scale = c.canvas != null ? c.canvas.scaleFactor : 1f;
                    var inner = new Rect(cr.x + 10 * scale, cr.y + 10 * scale, cr.width - 20 * scale, cr.height - 20 * scale);
                    if (!cr.Overlaps(it.r)) continue;
                    if (cr.width * cr.height < bestArea && cr.Contains(it.r.center)) { best = c; bestArea = cr.width * cr.height; }
                }
                if (best == null) continue;
                var box = WorldRect(best.rectTransform); float sc = best.canvas != null ? best.canvas.scaleFactor : 1f; float m = 10 * sc;
                if (it.r.xMin < box.xMin + m - 0.5f || it.r.xMax > box.xMax - m + 0.5f || it.r.yMin < box.yMin + m - 0.5f || it.r.yMax > box.yMax - m + 0.5f)
                { problems++; sb.AppendLine($"EDGE {it.name}  vs  {Path(best.transform)}"); }
            }
            return problems == 0 ? "lint ok (" + items.Count + " items)" : problems + " problems:\n" + sb;
        }
    }
}
#endif
