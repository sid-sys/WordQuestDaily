using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        void BuildLevels(RectTransform b)
        {
            var d = SaveSystem.Data;
            TopBar(b);

            // header: total progress
            var head = UI.Node(b, "Header");
            head.anchorMin = new Vector2(0, 1); head.anchorMax = new Vector2(1, 1); head.pivot = new Vector2(0.5f, 1);
            head.anchoredPosition = new Vector2(0, -150); head.sizeDelta = new Vector2(0, 190);
            var title = UI.Label(head, "LEVELS", 74, Color.white, TextAnchor.MiddleCenter, true);
            UI.Place(title.rectTransform, 0.5f, 1, 0, -44, 600, 90);
            int done = Mathf.Max(0, d.highestUnlocked - 1);
            var bar = UI.ProgressBar(head, 720, 52, Palette.Green, "Total");
            UI.Place(bar.Root, 0.5f, 1, -70, -140, 720, 52);
            bar.Set(done / (float)Levels.Total);
            var bt = UI.Label(bar.Root, $"{done} / {Levels.Total} levels completed", 30, Palette.Ink, TextAnchor.MiddleCenter, false); UI.Stretch(bt.rectTransform);
            var stars = UI.Icon(head, "star", 60, "Star"); UI.Place(stars.rectTransform, 0.5f, 1, 340, -140, 60, 60);
            var stl = UI.Label(head, Progress.TotalStars().ToString(), 40, Color.white, TextAnchor.MiddleLeft, true); UI.PlaceL(stl.rectTransform, 0.5f, 1, 380, -140, 120, 50);

            RectTransform content;
            var sr = Scroll(b, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 350);
            float y = 10;
            int cur = Progress.NextLevel;
            float curY = 0;
            for (int ch = 0; ch < Levels.ChapterCount; ch++)
            {
                int first = ch * Levels.PerChapter + 1;
                var spec = Levels.Get(first);
                var hr = RowAt(content, ref y, 170);
                var rib = UI.Img(hr, ch % 2 == 0 ? "ribbon_green" : "ribbon_pink", "Ch"); rib.preserveAspect = false;
                UI.Place(rib.rectTransform, 0.5f, 0.5f, 0, 8, 900, 150);
                var ct = UI.Label(rib.transform, $"CHAPTER {ch + 1}: {Levels.ChapterName(first).ToUpper()}", 42, Color.white, TextAnchor.MiddleCenter, true);
                UI.Stretch(ct.rectTransform, 110, 16, 110, 0);
                var dt = UI.Label(hr, $"{Levels.DiffName(spec.Diff)}  -  {spec.Cols}x{spec.Rows} boards", 30, Color.white, TextAnchor.MiddleCenter, true);
                UI.Place(dt.rectTransform, 0.5f, 0, 0, 20, 700, 40);
                for (int r = 0; r < 5; r++)
                {
                    var row = RowAt(content, ref y, 256);
                    for (int c = 0; c < 5; c++)
                    {
                        int lv = first + r * 5 + c;
                        if (lv > Levels.Total) break;
                        BuildNode(row, lv, (c - 2) * 196f, cur);
                        if (lv == cur) curY = y - 256;
                    }
                }
                y += 20;
            }
            EndScroll(content, y);
            // start with the current level in view
            float viewH = ((RectTransform)sr.transform).rect.height;
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(curY - viewH / 2 + 115, 0, Mathf.Max(0, y - viewH)));
        }

        void BuildNode(RectTransform row, int lv, float x, int cur)
        {
            var d = SaveSystem.Data;
            bool locked = lv > d.highestUnlocked, isCur = lv == cur, done = d.stars[lv] > 0;
            string art = locked ? "node_locked" : isCur ? "node_current" : "node_done";
            float size = isCur ? 164 : 140;
            var n = UI.Icon(row, art, size, "Level" + lv);
            UI.Place(n.rectTransform, 0.5f, 1, x, -85, size, size);
            var t = UI.Label(n.transform, isCur ? lv.ToString() : "", 58, Palette.Ink, TextAnchor.MiddleCenter, false);
            UI.Stretch(t.rectTransform, 0, 8, 0, 0);
            if (isCur)
            {
                var ring = UI.Img(row, "spark_ring", "Pulse"); ring.color = new Color(1, 1, 0.7f, 0.9f);
                UI.Place(ring.rectTransform, 0.5f, 1, x, -85, 210, 210);
                ring.transform.SetAsFirstSibling();
            }
            if (!isCur)
            {
                var nb = UI.Label(row, lv.ToString(), 34, Color.white, TextAnchor.MiddleCenter, true);
                UI.Place(nb.rectTransform, 0.5f, 1, x, locked ? -190 : -232, 120, 36);
            }
            if (!locked)
            {
                for (int s = 0; s < 3; s++)
                {
                    var st = UI.Icon(row, s < d.stars[lv] ? "star" : "star_empty", 42, "S");
                    UI.Place(st.rectTransform, 0.5f, 1, x + (s - 1) * 44, -190, 42, 42);
                    if (s >= d.stars[lv]) st.color = new Color(1, 1, 1, 0.55f);
                }
            }
            if (lv % Economy.MilestoneEvery == 0)
            {
                var ch = UI.Icon(row, "chest", 62, "Chest"); UI.Place(ch.rectTransform, 0.5f, 1, x + 62, -32, 62, 62);
                if (locked) ch.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            }
            int lvl = lv;
            UI.Click(n, () =>
            {
                if (lvl > SaveSystem.Data.highestUnlocked) { Toast($"Finish level {SaveSystem.Data.highestUnlocked} first", Palette.Yellow); Sfx.Play(Sfx.Kind.Error); return; }
                OpenPrep(Levels.Get(lvl));
            });
        }
    }
}
