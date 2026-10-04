using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        // =============== league + daily leaderboard (a tab) ===============
        public void ShowLeague(bool daily) { LeagueDaily = daily; ShowTab(Tab.League); }

        void BuildLeagueTab(RectTransform b)
        {
            var d = SaveSystem.Data;
            bool daily = LeagueDaily;
            TopBar(b, true, false);
            var title = UI.Label(b, daily ? "Today's Ranking" : League.Tiers[d.leagueTier] + " League", 64, Color.white, TextAnchor.MiddleCenter, true);
            title.GetComponent<Outline>().effectColor = new Color32(0x12, 0x4A, 0xA8, 255);
            UI.Place(title.rectTransform, 0.5f, 1, 0, -190, 900, 80);
            var sub = UI.Label(b, daily ? "Everybody plays the same puzzle" : $"{League.DaysLeftText()}  -  Top 3 move up, bottom 5 move down", 28, Color.white, TextAnchor.MiddleCenter, true);
            sub.GetComponent<Outline>().effectColor = new Color32(0x12, 0x4A, 0xA8, 255);
            UI.Place(sub.rectTransform, 0.5f, 1, 0, -250, 1000, 40);
            var t1 = UI.Pill(b, daily ? "btn_grey" : "btn_green", "THIS WEEK", 360, 80, () => ShowLeague(false), 34);
            var t2 = UI.Pill(b, daily ? "btn_green" : "btn_grey", "TODAY", 360, 80, () => ShowLeague(true), 34);
            UI.Place((RectTransform)t1.transform, 0.5f, 1, -190, -330, 360, 80);
            UI.Place((RectTransform)t2.transform, 0.5f, 1, 190, -330, 360, 80);

            RectTransform content;
            var sr = Scroll(b, out content);
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -390);
            var rows = daily ? League.DailyBoard() : League.Standings();
            float y = 6, youY = 0;
            foreach (var r in rows)
            {
                var row = ListRow(content, ref y, 116, 980);
                var bg = row.GetChild(0).GetComponent<Image>();
                if (r.IsYou) { UI.ApplyStyle(bg, "card_gold", 116); youY = y - 130; }
                else if (!daily && r.Rank <= 3) UI.ApplyStyle(bg, "card_green", 116);
                else if (!daily && r.Rank > 15) UI.ApplyStyle(bg, "card_red", 116);
                string medal = r.Rank == 1 ? "crown" : r.Rank <= 3 ? "trophy" : null;
                if (medal != null) { var m = UI.Icon(row, medal, 70, "M"); UI.Place(m.rectTransform, 0, 0.5f, 70, 0, 70, 70); }
                else { var n = UI.Label(row, r.Rank.ToString(), 40, Palette.InkSoft); UI.Place(n.rectTransform, 0, 0.5f, 70, 0, 90, 50); }
                var av = Avatar(row, r.Avatar, 0, 88); UI.Place(av, 0, 0.5f, 180, 0, 88, 88);
                var nm = UI.Label(row, r.IsYou ? r.Name + " (you)" : r.Name, 38, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(nm.rectTransform, 0, 0.5f, 250, 0, 460, 50);
                var pt = UI.Label(row, r.Points.ToString("N0"), 40, Palette.Ink, TextAnchor.MiddleRight); UI.Place(pt.rectTransform, 1, 0.5f, -150, 0, 200, 50);
            }
            EndScroll(content, y);
            float viewH = ((RectTransform)sr.transform).rect.height > 10 ? ((RectTransform)sr.transform).rect.height : 1300;
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(youY - viewH / 2, 0, Mathf.Max(0, y + 60 - viewH)));
        }

        public void ShowLeagueResult(League.WeekResult w)
        {
            var p = OpenPopup("WEEKLY LEAGUE", 900, 820, true, "ribbon_pink", () => { if (Progress.CanClaimDailyReward()) ShowDailyReward(); });
            var ic = UI.Icon(p.Content, "league", 240, "Shield"); UI.Place(ic.rectTransform, 0.5f, 1, 0, -170, 240, 240);
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
            var mark = UI.Img(root, "logo_wordmark", "Logo"); UI.Place(mark.rectTransform, 0.5f, 0.5f, 0, 90, 900, 710);
            var bar = UI.ProgressBar(root, 620, 50, Palette.Green, "Loading"); UI.Place(bar.Root, 0.5f, 0, 0, 260, 620, 50);
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
