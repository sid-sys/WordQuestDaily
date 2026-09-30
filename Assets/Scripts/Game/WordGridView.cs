using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>
    /// The letter board: draws letters, handles swiping, draws the colored capsules for found words
    /// and plays the drag / cancel / found animations.
    /// </summary>
    public class WordGridView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Puzzle Puzzle;
        public float Cell;
        public Action<Placement, Vector2Int, Vector2Int> OnWordFound;   // placement, start, end
        public Action OnWrong;
        public Action<string, Color> OnSelection;   // text being swiped (null when released)
        public bool InputEnabled = true;
        public Color LastColor = Color.white;

        RectTransform rt, letterLayer, capsuleLayer, markLayer;
        Text[] letters;
        RectTransform[] letterRt;
        Image live;
        int cols, rows;
        bool dragging; Vector2Int start, end; int lastLen;
        readonly List<GameObject> capsules = new List<GameObject>();
        readonly Dictionary<GameObject, int> capsuleOf = new Dictionary<GameObject, int>();
        readonly HashSet<int> lockedCells = new HashSet<int>();
        readonly List<GameObject> marks = new List<GameObject>();
        int colorIndex; Color liveColor = Color.white;

        // effect palettes
        static readonly Color[][] EffectColors =
        {
            null, // bright rainbow set
            new Color[] { new Color32(0xFF, 0x4A, 0x0F, 255), new Color32(0xFF, 0x8A, 0x00, 255), new Color32(0xFF, 0xC4, 0x00, 255), new Color32(0xE8, 0x1E, 0x3C, 255) },
            new Color[] { new Color32(0xFF, 0xD0, 0x00, 255), new Color32(0x1E, 0x90, 0xFF, 255), new Color32(0x8B, 0x3D, 0xFF, 255), new Color32(0x12, 0xC8, 0xE6, 255) },
            new Color[] { new Color32(0xFF, 0x7A, 0xB8, 255), new Color32(0x45, 0xC8, 0xFF, 255), new Color32(0xFF, 0xC8, 0x3D, 255), new Color32(0xA3, 0x7B, 0xFF, 255) },
            null,
        };

        public Color PeekColor()
        {
            int eff = SaveSystem.Data != null ? SaveSystem.Data.effect : 0;
            Color[] set = eff >= 0 && eff < EffectColors.Length ? EffectColors[eff] : null;
            if (eff == 4) return Color.HSVToRGB((colorIndex * 0.13f) % 1f, 0.85f, 1f);
            if (set == null) set = Palette.Capsules;
            return set[colorIndex % set.Length];
        }

        public Color NextColor() { var c = PeekColor(); colorIndex++; return c; }

        public string EffectParticle()
        {
            int eff = SaveSystem.Data != null ? SaveSystem.Data.effect : 0;
            switch (eff) { case 1: return "flame"; case 2: return "xp"; case 3: return "spark_glow"; case 4: return "conf_pink"; default: return "spark_star"; }
        }

        // ---------------- build ----------------
        public void Build(Puzzle p, float cell)
        {
            Puzzle = p; Cell = cell; cols = p.Cols; rows = p.Rows;
            rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(cols * cell, rows * cell);
            var hit = gameObject.GetComponent<Image>();
            if (hit == null) hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
            capsuleLayer = UI.Stretch(UI.Node(rt, "Capsules"));
            markLayer = UI.Stretch(UI.Node(rt, "Marks"));
            letterLayer = UI.Stretch(UI.Node(rt, "Letters"));
            letters = new Text[cols * rows]; letterRt = new RectTransform[cols * rows];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    var t = UI.Label(letterLayer, p.At(x, y).ToString(), Mathf.RoundToInt(cell * 0.66f), Color.black, TextAnchor.MiddleCenter);
                    var r = t.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0.5f, 0.5f);
                    r.anchoredPosition = CellCenter(x, y); r.sizeDelta = new Vector2(cell, cell);
                    letters[y * cols + x] = t; letterRt[y * cols + x] = r;
                }
            live = MakeCapsule(markLayer, Color.white, "Live"); live.gameObject.SetActive(false);
        }

        /// <summary>Center of a cell in the grid's own coordinates (anchored at top-left).</summary>
        public Vector2 CellCenter(int x, int y) => new Vector2((x + 0.5f) * Cell, -(y + 0.5f) * Cell);

        Image MakeCapsule(Transform parent, Color c, string name)
        {
            var img = UI.FlatPill(parent, new Color(c.r, c.g, c.b, 0.92f), name);
            var r = img.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1);
            return img;
        }

        void PlaceCapsule(RectTransform r, Vector2Int a, Vector2Int b, float grow = 1f)
        {
            Vector2 pa = CellCenter(a.x, a.y), pb = CellCenter(b.x, b.y);
            Vector2 mid = (pa + pb) / 2f;
            float len = (pb - pa).magnitude + Cell * 0.86f;
            r.anchoredPosition = mid;
            r.sizeDelta = new Vector2(len * grow, Cell * 0.8f);
            float ang = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg;
            r.localRotation = Quaternion.Euler(0, 0, ang);
        }

        // ---------------- input ----------------
        bool CellAt(PointerEventData e, out Vector2Int cell, out Vector2 fractional)
        {
            cell = default; fractional = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, null, out var local)) return false;
            // local is relative to the pivot (center); convert to top-left origin
            float px = local.x + rt.rect.width * rt.pivot.x, py = rt.rect.height * (1f - rt.pivot.y) - local.y;
            fractional = new Vector2(px / Cell, py / Cell);
            cell = new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(fractional.x), 0, cols - 1), Mathf.Clamp(Mathf.FloorToInt(fractional.y), 0, rows - 1));
            return true;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!InputEnabled) return;
            if (!CellAt(e, out var c, out _)) return;
            dragging = true; start = end = c; lastLen = 1;
            live.gameObject.SetActive(true);
            liveColor = PeekColor(); live.color = new Color(liveColor.r, liveColor.g, liveColor.b, 0.92f);
            UpdateLive();
            Sfx.Tick(0);
            Pop(c.x, c.y);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging || !CellAt(e, out var c, out var frac)) return;
            Vector2 from = new Vector2(start.x + 0.5f, start.y + 0.5f);
            Vector2 d = frac - from;
            Vector2Int newEnd = start;
            if (d.magnitude > 0.55f)
            {
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // y down
                int oct = Mathf.RoundToInt(ang / 45f);
                Vector2Int dir = OctantDir(oct);
                Vector2 dv = new Vector2(dir.x, dir.y);
                int n = Mathf.RoundToInt(Vector2.Dot(d, dv.normalized) / dv.magnitude);
                n = Mathf.Max(0, n);
                // stay inside the board
                while (n > 0)
                {
                    int ex = start.x + dir.x * n, ey = start.y + dir.y * n;
                    if (ex >= 0 && ex < cols && ey >= 0 && ey < rows) break;
                    n--;
                }
                newEnd = new Vector2Int(start.x + dir.x * n, start.y + dir.y * n);
            }
            if (newEnd != end)
            {
                end = newEnd;
                int len = Length(start, end);
                if (len > lastLen) { Sfx.Tick(len - 1); }
                lastLen = len;
                UpdateLive();
                foreach (var cell in Path(start, end)) Pop(cell.x, cell.y);
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            OnSelection?.Invoke(null, Color.white);
            Release();
        }

        static Vector2Int OctantDir(int oct)
        {
            oct = ((oct % 8) + 8) % 8;
            switch (oct)
            {
                case 0: return new Vector2Int(1, 0);
                case 1: return new Vector2Int(1, 1);
                case 2: return new Vector2Int(0, 1);
                case 3: return new Vector2Int(-1, 1);
                case 4: return new Vector2Int(-1, 0);
                case 5: return new Vector2Int(-1, -1);
                case 6: return new Vector2Int(0, -1);
                default: return new Vector2Int(1, -1);
            }
        }

        static int Length(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)) + 1;

        public static List<Vector2Int> Path(Vector2Int a, Vector2Int b)
        {
            var list = new List<Vector2Int>();
            int n = Length(a, b);
            Vector2Int step = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
            for (int i = 0; i < n; i++) list.Add(new Vector2Int(a.x + step.x * i, a.y + step.y * i));
            return list;
        }

        void UpdateLive()
        {
            PlaceCapsule(live.rectTransform, start, end); live.rectTransform.localScale = Vector3.one;
            var cells = Path(start, end);
            OnSelection?.Invoke(new string(cells.Select(c => Puzzle.At(c.x, c.y)).ToArray()), new Color(liveColor.r, liveColor.g, liveColor.b, 1f));
        }

        void Pop(int x, int y)
        {
            var r = letterRt[y * cols + x];
            Fx.Tween(0.16f, t => { if (r != null) r.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * Mathf.PI)); });
        }

        // ---------------- release / matching ----------------
        void Release()
        {
            var cells = Path(start, end);
            if (cells.Count < 2) { CancelLive(); return; }
            string s = new string(cells.Select(c => Puzzle.At(c.x, c.y)).ToArray());
            string r = new string(s.Reverse().ToArray());
            Placement match = null;
            foreach (var p in Puzzle.AllPlacements())
                if (!p.Found && (p.Word == s || p.Word == r)) { match = p; break; }
            if (match == null) { OnWrong?.Invoke(); CancelLive(); return; }

            // lock in: the capsule stays where the player drew it
            match.Found = true;
            Vector2Int a = cells[0], b = cells[cells.Count - 1];
            if (match.Word != s) { var t = a; a = b; b = t; }
            match.X = a.x; match.Y = a.y; match.Dir = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
            if (cells.Count == 1) match.Dir = new Vector2Int(1, 0);
            Color color = match.IsMystery ? (Color)new Color32(0xF5, 0xC4, 0x00, 255) : NextColor();
            LockWord(match, color, true);
            live.gameObject.SetActive(false);
            OnWordFound?.Invoke(match, a, b);
        }

        void CancelLive()
        {
            Sfx.Play(Sfx.Kind.Cancel, 1f, 0.5f);
            var r = live.rectTransform; var img = live;
            Vector3 s0 = r.localScale;
            Fx.Tween(0.18f, t =>
            {
                if (r == null) return;
                r.localScale = new Vector3(Mathf.Lerp(1f, 0.15f, t), Mathf.Lerp(1f, 0.6f, t), 1);
                img.color = new Color(liveColor.r, liveColor.g, liveColor.b, 0.92f * (1 - t));
            }, () => { if (img != null) img.gameObject.SetActive(false); });
        }

        // ---------------- found words ----------------
        public void LockWord(Placement p, Color color, bool animate)
        {
            LastColor = color;
            var cap = MakeCapsule(capsuleLayer, color, "Found_" + p.Word);
            var a = new Vector2Int(p.X, p.Y); var b = p.End;
            PlaceCapsule(cap.rectTransform, a, b);
            capsules.Add(cap.gameObject);
            var path = Path(a, b);
            foreach (var c in path)
            {
                lockedCells.Add(c.y * cols + c.x);
                letters[c.y * cols + c.x].color = Color.white;
            }
            if (animate)
            {
                var r = cap.rectTransform; Vector2 size = r.sizeDelta;
                Fx.Tween(0.28f, t => { if (r != null) r.sizeDelta = new Vector2(size.x * Fx.OutBack(t), size.y * (0.8f + 0.2f * t)); });
                for (int i = 0; i < path.Count; i++)
                {
                    var lr = letterRt[path[i].y * cols + path[i].x];
                    Fx.Tween(0.34f, t => { if (lr != null) lr.localScale = Vector3.one * (1f + 0.22f * Mathf.Sin(t * Mathf.PI)); }, () => { if (lr != null) lr.localScale = Vector3.one; }, i * 0.035f);
                }
            }
        }

        public Vector2 CellWorld(int x, int y)
        {
            // position inside the board's coordinates converted to the canvas-space of the Fx layer
            var world = rt.TransformPoint(CellCenter(x, y) - new Vector2(rt.rect.width * rt.pivot.x, -rt.rect.height * (1 - rt.pivot.y)));
            return world;
        }

        public Vector2 CellCanvas(int x, int y, RectTransform layer)
        {
            var world = rt.TransformPoint(new Vector3(CellCenter(x, y).x - rt.rect.width * rt.pivot.x, CellCenter(x, y).y + rt.rect.height * (1 - rt.pivot.y), 0));
            return layer.InverseTransformPoint(world);
        }

        // ---------------- hints / marks ----------------
        void ClearMarksOf(string tag)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
                if (marks[i] == null || marks[i].name.StartsWith(tag)) { if (marks[i] != null) Destroy(marks[i]); marks.RemoveAt(i); }
        }

        public void MarkCell(int x, int y, Color color, string tag)
        {
            var ring = UI.Img(markLayer, "spark_ring", tag + "_" + x + "_" + y);
            ring.color = color;
            var r = ring.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.anchoredPosition = CellCenter(x, y); r.sizeDelta = new Vector2(Cell * 1.05f, Cell * 1.05f);
            ring.gameObject.AddComponent<Pulse>().Amount = 0.12f;
            marks.Add(ring.gameObject);
            Fx.PopIn(r, 0.3f);
        }

        public void MarkWord(Placement p, Color color)
        {
            var cap = MakeCapsule(markLayer, color, "Finder_" + p.Word);
            cap.color = new Color(color.r, color.g, color.b, 0.6f);
            PlaceCapsule(cap.rectTransform, new Vector2Int(p.X, p.Y), p.End);
            cap.gameObject.AddComponent<Pulse>().Amount = 0.05f;
            marks.Add(cap.gameObject);
        }

        public void ClearMarks() { foreach (var m in marks) if (m != null) Destroy(m); marks.Clear(); }

        // ---------------- shuffle ----------------
        /// <summary>Re-reads the puzzle letters after a shuffle and animates the change. Found words are not touched.</summary>
        public void Refresh(bool animate)
        {
            ClearMarks();
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    int idx = y * cols + x;
                    if (lockedCells.Contains(idx)) continue;
                    var t = letters[idx]; var r = letterRt[idx]; char ch = Puzzle.At(x, y);
                    if (!animate) { t.text = ch.ToString(); continue; }
                    float delay = (x + y) * 0.035f;
                    Fx.Tween(0.3f, k =>
                    {
                        if (r == null) return;
                        float s = k < 0.5f ? 1f - k * 2f : (k - 0.5f) * 2f;
                        r.localScale = new Vector3(s, 1f, 1f);
                        if (k >= 0.5f && t.text != ch.ToString()) t.text = ch.ToString();
                    }, () => { if (r != null) r.localScale = Vector3.one; }, delay);
                }
        }

        public void SetLettersDim(bool dim, float alpha = 0.35f)
        {
            for (int i = 0; i < letters.Length; i++)
                if (!lockedCells.Contains(i)) { var c = letters[i].color; c.a = dim ? alpha : 1f; letters[i].color = c; }
        }
    }
}
