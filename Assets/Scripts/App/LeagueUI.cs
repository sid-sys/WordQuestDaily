using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        // =============== league + daily leaderboard ===============
        public void ShowLeague(bool daily)
        {
            var d = SaveSystem.Data;
            var p = OpenPopup(daily ? "TODAY'S RANKING" : League.Tiers[d.leagueTier].ToUpper() + " LEAGUE", 980, 1560, true, daily ? "ribbon_pink" : "ribbon_green");
            var sub = UI.Label(p.Content, daily ? "Everybody plays the same puzzle" : $"{7 - League.DaysIntoWeek} days left  -  Top 3 move up, bottom 5 move down", 30, Palette.InkSoft);
            UI.Place(sub.rectTransform, 0.5f, 1, 0, -40, 880, 44);
            var tabs = UI.Node(p.Content, "Tabs");
            var t1 = UI.Pill(tabs, daily ? "btn_grey" : "btn_green", "THIS WEEK", 380, 80, () => { p.Close(); ShowLeague(false); }, 34);
            var t2 = UI.Pill(tabs, daily ? "btn_green" : "btn_grey", "TODAY", 380, 80, () => { p.Close(); ShowLeague(true); }, 34);
            UI.Place((RectTransform)t1.transform, 0.5f, 1, -200, -120, 380, 80);
            UI.Place((RectTransform)t2.transform, 0.5f, 1, 200, -120, 380, 80);

            RectTransform content;
            var sr = Scroll(p.Content, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 190);
            var rows = daily ? League.DailyBoard() : League.Standings();
            float y = 6, youY = 0;
            foreach (var r in rows)
            {
                var row = ListRow(content, ref y, 116, 880);
                var bg = row.GetChild(0).GetComponent<Image>();
                if (r.IsYou) { bg.color = new Color(1f, 0.93f, 0.55f); youY = y - 130; }
                else if (!daily && r.Rank <= 3) bg.color = new Color(0.82f, 0.96f, 0.82f);
                else if (!daily && r.Rank > 15) bg.color = new Color(1f, 0.85f, 0.85f);
                string medal = r.Rank == 1 ? "crown" : r.Rank <= 3 ? "trophy" : null;
                if (medal != null) { var m = UI.Icon(row, medal, 70, "M"); UI.Place(m.rectTransform, 0, 0.5f, 60, 0, 70, 70); }
                else { var n = UI.Label(row, r.Rank.ToString(), 40, Palette.InkSoft); UI.Place(n.rectTransform, 0, 0.5f, 60, 0, 90, 50); }
                var av = Avatar(row, r.Avatar, 0, 88); UI.Place(av, 0, 0.5f, 170, 0, 88, 88);
                var nm = UI.Label(row, r.IsYou ? r.Name + " (you)" : r.Name, 38, Palette.Ink, TextAnchor.MiddleLeft); UI.Place(nm.rectTransform, 0, 0.5f, 240 + 190, 0, 380, 50);
                var pt = UI.Label(row, r.Points.ToString("N0"), 40, Palette.Ink, TextAnchor.MiddleRight); UI.Place(pt.rectTransform, 1, 0.5f, -130, 0, 200, 50);
            }
            EndScroll(content, y);
            float viewH = 1560 - 96 - 190;
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(youY - viewH / 2, 0, Mathf.Max(0, y + 60 - viewH)));
        }

        public void ShowLeagueResult(League.WeekResult w)
        {
            var p = OpenPopup("WEEKLY LEAGUE", 900, 820, true, "ribbon_pink", () => { if (Progress.CanClaimDailyReward()) ShowDailyReward(); });
            var ic = UI.Icon(p.Content, "league", 240, "Shield"); UI.Place(ic.rectTransform, 0.5f, 1, 0, -170, 240, 240);
            ic.color = Color.Lerp(Color.white, League.TierColors[w.NewTier], 0.5f);
            var t = UI.Label(p.Content, w.Text, 56, Palette.Ink); UI.Place(t.rectTransform, 0.5f, 1, 0, -370, 820, 70);
            var r = UI.Label(p.Content, $"Last week you finished #{w.Rank}", 40, Palette.InkSoft); UI.Place(r.rectTransform, 0.5f, 1, 0, -440, 820, 50);
            if (w.Coins > 0) { var c = UI.Label(p.Content, $"+{w.Coins} coins", 56, Palette.Orange); UI.Place(c.rectTransform, 0.5f, 1, 0, -520, 820, 70); }
            var ok = UI.Pill(p.Content, "btn_green", "AWESOME", 420, 110, p.Close, 50); UI.Place((RectTransform)ok.transform, 0.5f, 0, 0, 40, 420, 110);
            Sfx.Play(w.NewTier > w.OldTier ? Sfx.Kind.Level : Sfx.Kind.Coin);
        }

        // =============== splash ===============
        void ShowSplash(Action done)
        {
            var root = UI.Stretch(UI.Node(popupLayer, "Splash"));
            var bg = UI.Img(root, "brand_splash", "Bg", true); bg.preserveAspect = false; UI.Stretch(bg.rectTransform);
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 1024f / 1536f;
            var mark = UI.Img(root, "logo_mark", "Mark"); UI.Place(mark.rectTransform, 0.5f, 0.5f, 0, 260, 520, 600);
            var title = UI.Label(root, "WORD QUEST", 130, Color.white, TextAnchor.MiddleCenter, true); UI.Place(title.rectTransform, 0.5f, 0.5f, 0, -120, 1000, 150);
            var sub = UI.Label(root, "DAILY", 90, Palette.Yellow, TextAnchor.MiddleCenter, true); UI.Place(sub.rectTransform, 0.5f, 0.5f, 0, -240, 700, 110);
            var bar = UI.ProgressBar(root, 600, 46, Palette.Green, "Loading"); UI.Place(bar.Root, 0.5f, 0, 0, 240, 600, 46);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            Fx.PopIn(mark.rectTransform, 0.6f);
            const float hold = 1.6f;
            Fx.Tween(hold, k => { if (bar != null) bar.Set(k); }, () =>
            {
                done?.Invoke();
                Fx.Fade(cg, 0f, 0.35f, () => { if (root != null) Destroy(root.gameObject); });
            });
        }
    }
}
