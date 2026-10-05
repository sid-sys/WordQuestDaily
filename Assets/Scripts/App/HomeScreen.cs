using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>
    /// Home = the level path (like the reference): level cards stacked on a blue road, the current level glowing,
    /// a big PLAY button, and side buttons for the Daily Quest, Missions, daily reward and Remove Ads.
    /// </summary>
    public partial class GameApp
    {
        Text dailyTimer;
        float timerTick;

        void BuildHome(RectTransform b)
        {
            var d = SaveSystem.Data;
            Progress.EnsureMissionsToday();
            int cur = Progress.NextLevel;

            // ---- the road of level cards (scrolls) ----
            RectTransform content;
            var sr = Scroll(b, out content);
            var srt = (RectTransform)sr.transform;
            srt.offsetMin = new Vector2(0, 190);    // keep the Play button area free
            srt.offsetMax = new Vector2(0, -150);   // under the top bar
            const float cardW = 300, cardH = 330, gap = 80, curBoost = 40;
            int lo = Mathf.Max(1, cur - 14), hi = Mathf.Min(Levels.Total, cur + 14);
            float y = 60, curY = 0;
            float centerX = -60;   // the road sits a bit left of center, the buttons float on the right
            for (int lv = hi; lv >= lo; lv--)
            {
                bool isCur = lv == cur;
                float h = isCur ? cardH + curBoost : cardH;
                if (lv > lo)
                {
                    var road = UI.Solid(content, new Color32(0x1B, 0x4F, 0xB0, 255), "Road");
                    var rr = road.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1); rr.pivot = new Vector2(0.5f, 1);
                    rr.anchoredPosition = new Vector2(centerX, -(y + h - 20)); rr.sizeDelta = new Vector2(46, gap + 40);
                    var hl = UI.Solid(road.transform, new Color32(0x6F, 0xC3, 0xFF, 255), "RoadL");
                    UI.Stretch(hl.rectTransform, 0, 0, 36, 0);
                }
                BuildLevelCard(content, lv, cur, centerX, y, isCur ? cardW + 40 : cardW, h);
                if (isCur) curY = y + h / 2;
                y += h + gap;
            }
            EndScroll(content, y);
            float viewH = srt.rect.height > 10 ? srt.rect.height : 1500;
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(curY - viewH * 0.62f, 0, Mathf.Max(0, y - viewH)));

            TopBar(b);

            // ---- floating buttons ----
            SideButton(b, true, 0, "calendar", "Daily", d.dailyQuestDoneDate != Clock.TodayKey, ShowDailyQuest);
            SideButton(b, true, 1, "scroll", Progress.MissionsDone() + "/4", Progress.MissionsDone() < 4, ShowMissions);
            if (!d.adsRemoved) SideButton(b, true, 2, "noads_badge", "", false, ShowAdFree, true);
            SideButton(b, false, 0, "gift", "Gift", Progress.CanClaimDailyReward(), ShowDailyReward);
            int streak = Progress.EffectiveStreak();
            SideButton(b, false, 1, "flame", streak > 0 ? streak + " days" : "Streak", false, () => Toast(streak > 0 ? $"{streak} day streak! Play today to keep it." : "Finish the Daily Quest to start a streak", Palette.Yellow));

            // ---- PLAY ----
            var play = UI.Pill(b, "btn_green", "Play", 640, 150, () => StartGame(Levels.Get(cur)), 78);
            var pr = (RectTransform)play.transform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0); pr.pivot = new Vector2(0.5f, 0);
            pr.anchoredPosition = new Vector2(0, 26); pr.sizeDelta = new Vector2(640, 150);
        }

        /// <summary>An icon button with a small caption, floating at the screen edge.</summary>
        void SideButton(RectTransform parent, bool right, int slot, string art, string caption, bool dot, Action click, bool big = false)
        {
            float size = big ? 150 : 118;
            var root = UI.Node(parent, "Side_" + art);
            root.anchorMin = root.anchorMax = new Vector2(right ? 1 : 0, 1); root.pivot = new Vector2(0.5f, 1);
            root.anchoredPosition = new Vector2(right ? -84 : 84, -170 - slot * 215); root.sizeDelta = new Vector2(150, 190);
            var ic = UI.Icon(root, art, size, "Icon");
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 1); ic.rectTransform.pivot = new Vector2(0.5f, 1);
            ic.rectTransform.anchoredPosition = new Vector2(0, -4); ic.rectTransform.sizeDelta = new Vector2(size, size);
            UI.Click(ic, click);
            if (!string.IsNullOrEmpty(caption))
            {
                var cap = UI.Label(root, caption, 28, Color.white, TextAnchor.MiddleCenter, true);
                cap.GetComponent<Outline>().effectColor = new Color32(0x12, 0x4A, 0xA8, 255);
                cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = new Vector2(0.5f, 0); cap.rectTransform.pivot = new Vector2(0.5f, 0);
                cap.rectTransform.anchoredPosition = new Vector2(0, 4); cap.rectTransform.sizeDelta = new Vector2(190, 36);
            }
            if (dot)
            {
                var ring = UI.Node(root, "Dot").gameObject.AddComponent<Image>(); ring.sprite = Shapes.Circle(); ring.color = Color.white; ring.raycastTarget = false;
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 1); ring.rectTransform.anchoredPosition = new Vector2(size * 0.36f, -size * 0.12f); ring.rectTransform.sizeDelta = new Vector2(42, 42);
                var dd = UI.Node(ring.transform, "In").gameObject.AddComponent<Image>(); dd.sprite = Shapes.Circle(); dd.color = Palette.Red; dd.raycastTarget = false;
                UI.Stretch(dd.rectTransform, 5, 5, 5, 5);
            }
        }

        void BuildLevelCard(RectTransform content, int lv, int cur, float cx, float y, float w, float h)
        {
            var d = SaveSystem.Data;
            bool locked = lv > d.highestUnlocked, isCur = lv == cur;
            var spec = Levels.Get(lv);
            if (isCur)
            {
                var glow = UI.Node(content, "Glow").gameObject.AddComponent<Image>(); glow.sprite = Shapes.Glow(); glow.color = new Color(1, 1, 1, 0.75f); glow.raycastTarget = false;
                var gr = glow.rectTransform; gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1); gr.pivot = new Vector2(0.5f, 0.5f);
                gr.anchoredPosition = new Vector2(cx, -(y + h / 2)); gr.sizeDelta = new Vector2(w + 300, h + 300);
            }
            var card = UI.Sliced(content, isCur ? "lvl_cur" : locked ? "lvl_lock" : "lvl_done", h, "Level" + lv, true);
            var cr = card.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 1); cr.pivot = new Vector2(0.5f, 1);
            cr.anchoredPosition = new Vector2(cx, -y); cr.sizeDelta = new Vector2(w, h);
            var pic = UI.Round(card.transform, new Color32(0xB4, 0xC6, 0xD2, 255), 26, "Pic");
            pic.rectTransform.anchorMin = Vector2.zero; pic.rectTransform.anchorMax = Vector2.one; pic.rectTransform.offsetMin = new Vector2(22, 96); pic.rectTransform.offsetMax = new Vector2(-22, -22);
            float picMid = -(22 + (h - 118) / 2f);
            if (!locked)
            {
                var icon = UI.Icon(card.transform, WordBank.Get(spec.Category).ArtKey, w * 0.36f, "Cat");
                UI.Place(icon.rectTransform, 0.5f, 1, 0, picMid - 14, w * 0.36f, w * 0.36f);
            }
            if (locked)
            {
                var lk = UI.Icon(card.transform, "lock", 86, "Lock"); UI.Place(lk.rectTransform, 0.5f, 1, 0, picMid, 86, 86);
            }
            var strip = UI.Round(card.transform, locked ? new Color32(0x4E, 0x5C, 0x70, 255) : new Color32(0x58, 0xD4, 0x3E, 255), 24, "Strip");
            strip.rectTransform.anchorMin = Vector2.zero; strip.rectTransform.anchorMax = new Vector2(1, 0); strip.rectTransform.offsetMin = new Vector2(16, 14); strip.rectTransform.offsetMax = new Vector2(-16, 86);
            var num = UI.Label(strip.transform, lv.ToString(), 56, Color.white, TextAnchor.MiddleCenter, true);
            num.GetComponent<Outline>().effectColor = locked ? new Color32(0x2A, 0x33, 0x40, 255) : new Color32(0x1B, 0x6B, 0x16, 255);
            UI.Stretch(num.rectTransform);
            if (!locked)
                for (int s = 0; s < 3; s++)
                {
                    var st = UI.Icon(card.transform, s < d.stars[lv] ? "star" : "star_empty", 40, "S");
                    UI.Place(st.rectTransform, 0, 1, 56 + s * 42, -46, 40, 40);
                }
            if (lv % Economy.MilestoneEvery == 0)
            {
                var ch = UI.Icon(card.transform, "chest", 70, "Chest"); UI.Place(ch.rectTransform, 1, 1, -52, -54, 70, 70);
                if (locked) ch.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            }
            int lvl = lv;
            UI.Click(card, () =>
            {
                if (lvl > SaveSystem.Data.highestUnlocked) { Toast($"Finish level {SaveSystem.Data.highestUnlocked} first", Palette.Yellow); Sfx.Play(Sfx.Kind.Error); return; }
                StartGame(Levels.Get(lvl));
            });
        }

        // =============== Daily Quest ===============
        public void ShowDailyQuest()
        {
            var d = SaveSystem.Data;
            var q = Levels.Daily(Clock.Today);
            bool done = d.dailyQuestDoneDate == Clock.TodayKey;
            var p = OpenPopup("DAILY QUEST", 940, 980, true, "ribbon_pink");
            var c = p.Content;
            var icon = UI.Icon(c, WordBank.Get(q.Category).ArtKey, 210, "Cat"); UI.Place(icon.rectTransform, 0.5f, 1, 0, -170, 210, 210);
            var theme = UI.Label(c, WordBank.Get(q.Category).Name.ToUpper(), 56, Palette.Ink); UI.Place(theme.rectTransform, 0.5f, 1, 0, -330, 800, 70);
            var meta = UI.Label(c, $"{WordBank.Get(q.Category).Name}  -  {q.Cols}x{q.Rows}  -  {q.WordCount} words", 34, Palette.InkSoft); UI.Place(meta.rectTransform, 0.5f, 1, 0, -398, 800, 44);
            var dif = UI.Label(c, Levels.DiffName(q.Diff).ToUpper(), 38, Levels.DiffColor(q.Diff)); UI.Place(dif.rectTransform, 0.5f, 1, 0, -450, 800, 48);
            if (!done)
            {
                var info = UI.Label(c, "Same puzzle for everyone today. Keeps your streak!", 30, Palette.InkSoft); UI.Place(info.rectTransform, 0.5f, 1, 0, -520, 840, 44);
                var play = UI.Pill(c, "btn_green", "PLAY", 560, 130, () => { p.Close(); StartGame(q); }, 66);
                UI.Place((RectTransform)play.transform, 0.5f, 0, 0, 60, 560, 130);
            }
            else
            {
                var ok = UI.Label(c, $"Completed!  Score {d.dailyQuestScore:N0}", 46, Palette.Green); UI.Place(ok.rectTransform, 0.5f, 1, 0, -540, 860, 60);
                dailyTimer = UI.Label(c, "", 32, Palette.InkSoft); UI.Place(dailyTimer.rectTransform, 0.5f, 1, 0, -604, 860, 44);
                UpdateDailyTimer();
                var lb = UI.Pill(c, "btn_purple", "TODAY'S RANKING", 560, 110, () => { p.Close(); ShowLeague(true); }, 42);
                UI.Place((RectTransform)lb.transform, 0.5f, 0, 0, 60, 560, 110);
            }
            var oldClose = p.OnClose; p.OnClose = () => { dailyTimer = null; oldClose?.Invoke(); };
        }

        // =============== Missions ===============
        public void ShowMissions()
        {
            var d = SaveSystem.Data; var m = d.missions;
            var p = OpenPopup("MISSIONS", 940, 900, true, "ribbon_orange");
            var c = p.Content;
            string[] names = { "Finish the Daily Quest", "Find a Mystery Word", "Win with no power-ups", "Complete 3 levels" };
            bool[] st = { m.playedDaily, m.foundMystery, m.noPowerups, m.threeLevels };
            for (int i = 0; i < 4; i++)
            {
                var row = UI.Sliced(c, "card_a", 120, "M" + i); UI.Place(row.rectTransform, 0.5f, 1, 0, -(110 + i * 136), 820, 116);
                var ic = UI.Icon(row.transform, st[i] ? "check" : "star_empty", 70, "I"); UI.Place(ic.rectTransform, 0, 0.5f, 62, 0, 70, 70);
                var t = UI.Label(row.transform, names[i], 38, st[i] ? Palette.Green : Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(t.rectTransform, 0, 0.5f, 120, 0, 660, 52);
            }
            var perfect = UI.Label(c, m.claimed ? "Perfect Day done!" : "Do all 4 for a Perfect Day: +100 coins", 36, m.claimed ? Palette.Green : Palette.InkSoft);
            UI.Place(perfect.rectTransform, 0.5f, 0, 0, 120, 860, 50);
            var ok = UI.Pill(c, "btn_green", "OK", 360, 96, p.Close, 48); UI.Place((RectTransform)ok.transform, 0.5f, 0, 0, 14, 360, 96);
        }

        void UpdateDailyTimer()
        {
            if (dailyTimer == null) return;
            var t = Clock.UntilMidnight;
            dailyTimer.text = $"New quest in {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        void Update()
        {
#if WQ_ADMOB
            AdMobService.Tick();
#endif
            if (dailyTimer != null)
            {
                timerTick += Time.unscaledDeltaTime;
                if (timerTick > 0.5f) { timerTick = 0; UpdateDailyTimer(); }
            }
        }
    }
}
