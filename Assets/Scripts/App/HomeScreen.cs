using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        Text dailyTimer;

        void BuildHome(RectTransform b)
        {
            var d = SaveSystem.Data;
            Progress.EnsureMissionsToday();
            TopBar(b);
            RectTransform content;
            var sr = Scroll(b, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 160);
            float y = 10;

            // ---- title + streak ----
            var top = RowAt(content, ref y, 260);
            var mark = UI.Img(top, "logo_mark", "Logo");
            UI.Place(mark.rectTransform, 0.5f, 0.5f, -330, 0, 200, 230);
            var title = UI.Label(top, "WORD QUEST", 96, Color.white, TextAnchor.MiddleLeft, true);
            UI.PlaceL(title.rectTransform, 0.5f, 0.5f, -210, 40, 700, 110);
            var sub = UI.Label(top, "DAILY", 58, Palette.Yellow, TextAnchor.MiddleLeft, true);
            UI.PlaceL(sub.rectTransform, 0.5f, 0.5f, -205, -50, 300, 70);

            int streak = Progress.EffectiveStreak();
            var streakRow = RowAt(content, ref y, 110);
            var sp = UI.Sliced(streakRow, "chip", 100, "Streak");
            sp.color = new Color(0.10f, 0.15f, 0.32f, 0.92f);
            UI.Place(sp.rectTransform, 0.5f, 0.5f, 0, 0, 640, 100);
            var fl = UI.Icon(sp.transform, "flame", 70, "Flame"); UI.Place(fl.rectTransform, 0, 0.5f, 62, 2, 70, 70);
            var st = UI.Label(sp.transform, streak > 0 ? $"{streak} DAY STREAK" : "START YOUR STREAK", 44, Color.white, TextAnchor.MiddleLeft);
            UI.PlaceL(st.rectTransform, 0, 0.5f, 116, 0, 420, 60);
            var fr = UI.Icon(sp.transform, "freeze", 76, "Freeze"); UI.Place(fr.rectTransform, 1, 0.5f, -82, 2, 76, 76);
            var frt = UI.Label(sp.transform, "x" + d.freezes, 32, Color.white); UI.Place(frt.rectTransform, 1, 0.5f, -30, -20, 60, 40);
            y += 50;

            // ---- daily quest card ----
            var q = Levels.Daily(Clock.Today);
            bool done = d.dailyQuestDoneDate == Clock.TodayKey;
            var dq = RowAt(content, ref y, 470);
            var dqBg = UI.Sliced(dq, "card_b", 300, "Daily");
            UI.Stretch(dqBg.rectTransform);
            var rib = UI.Img(dq, "ribbon_pink", "Ribbon"); rib.preserveAspect = false;
            UI.Place(rib.rectTransform, 0.5f, 1, 0, 8, 620, 110);
            var rt = UI.Label(rib.transform, "DAILY QUEST", 50, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(rt.rectTransform, 0, 6, 0, 0);
            var catArt = UI.Icon(dq, WordBank.Get(q.Category).ArtKey, 190, "Cat"); UI.Place(catArt.rectTransform, 0, 0.5f, 170, -20, 190, 190);
            var theme = UI.Label(dq, Levels.DailyTheme(Clock.Today).ToUpper() + " DAY", 44, Palette.Ink, TextAnchor.MiddleLeft);
            UI.PlaceL(theme.rectTransform, 0, 0.5f, 300, 50, 660, 56);
            var meta = UI.Label(dq, $"{WordBank.Get(q.Category).Name}  -  {q.Cols}x{q.Rows}  -  {q.WordCount} words", 30, Palette.InkSoft, TextAnchor.MiddleLeft);
            UI.PlaceL(meta.rectTransform, 0, 0.5f, 300, 0, 680, 40);
            var dif = UI.Label(dq, Levels.DiffName(q.Diff).ToUpper(), 30, Levels.DiffColor(q.Diff), TextAnchor.MiddleLeft, false);
            UI.PlaceL(dif.rectTransform, 0, 0.5f, 300, -42, 640, 40);
            if (!done)
            {
                var play = UI.Pill(dq, "btn_green", "PLAY", 460, 120, () => OpenPrep(q), 60);
                UI.Place((RectTransform)play.transform, 0.5f, 0, 0, 72, 460, 120);
                Fx.Tween(1.6f, t => { }, null);
            }
            else
            {
                var ok = UI.Label(dq, $"Completed!  Score {d.dailyQuestScore:N0}", 42, Palette.Green, TextAnchor.MiddleCenter, false);
                UI.Place(ok.rectTransform, 0.5f, 0, 0, 104, 800, 56);
                dailyTimer = UI.Label(dq, "", 32, Palette.InkSoft);
                UI.Place(dailyTimer.rectTransform, 0.5f, 0, 0, 56, 800, 44);
                UpdateDailyTimer();
                var lb = UI.Pill(dq, "btn_purple", "TODAY'S RANKING", 480, 80, () => ShowLeague(true), 32);
                UI.Place((RectTransform)lb.transform, 0.5f, 0, 300 + 70, 300, 480, 80);
                lb.gameObject.SetActive(false);
            }
            y += 24;

            // ---- continue level ----
            int next = Progress.NextLevel;
            var cont = RowAt(content, ref y, 300);
            var cbg = UI.Sliced(cont, "card_a", 300, "Continue"); UI.Stretch(cbg.rectTransform);
            var spec = Levels.Get(next);
            var lvT = UI.Label(cont, $"LEVEL {next}", 60, Palette.Ink, TextAnchor.MiddleLeft);
            UI.PlaceL(lvT.rectTransform, 0, 1, 300, -70, 560, 74);
            var chap = UI.Label(cont, $"{Levels.ChapterName(next)} - {Levels.DiffName(spec.Diff)}", 32, Levels.DiffColor(spec.Diff), TextAnchor.MiddleLeft);
            UI.PlaceL(chap.rectTransform, 0, 1, 300, -128, 640, 44);
            var bar = UI.ProgressBar(cont, 560, 40, null, "Chapter");
            UI.Place(bar.Root, 0, 1, 300 + 320, -176, 640, 40);
            bar.Root.sizeDelta = new Vector2(640, 40);
            int ch = Levels.Chapter(next), first = ch * Levels.PerChapter + 1, doneInCh = Mathf.Clamp(next - first, 0, Levels.PerChapter);
            bar.Set(doneInCh / (float)Levels.PerChapter);
            var bt = UI.Label(bar.Root, $"{doneInCh}/{Levels.PerChapter}", 26, Color.white); UI.Stretch(bt.rectTransform);
            var nodeIcon = UI.Icon(cont, "node_current", 220, "Node"); UI.Place(nodeIcon.rectTransform, 0, 0.5f, 160, 0, 220, 220);
            var go = UI.Pill(cont, "btn_green", "CONTINUE", 420, 96, () => OpenPrep(spec), 46);
            UI.Place((RectTransform)go.transform, 0, 0, 300 + 200, 54, 400, 96);
            y += 24;

            // ---- collection + league cards (everything measured from the top of the card) ----
            var two = RowAt(content, ref y, 300);
            var col = UI.Sliced(two, "card_a", 300, "Collection", true); col.rectTransform.sizeDelta = new Vector2(488, 300);
            UI.Place(col.rectTransform, 0, 0.5f, 244, 0, 488, 300);
            UI.Click(col, () => ShowTab(Tab.Collection));
            var bk = UI.Icon(col.transform, "tab_collection", 116, "Book"); UI.Place(bk.rectTransform, 0.5f, 1, 0, -86, 116, 116);
            var ct = UI.Label(col.transform, "COLLECTION", 34, Palette.Ink); UI.Place(ct.rectTransform, 0.5f, 1, 0, -174, 420, 44);
            var cc = UI.Label(col.transform, $"{Progress.TotalDiscovered()} words found", 28, Palette.InkSoft); UI.Place(cc.rectTransform, 0.5f, 1, 0, -216, 420, 38);
            int all = 0; foreach (var c in WordBank.All) all += c.Words.Length;
            var cb = UI.ProgressBar(col.transform, 380, 30, Palette.Green, "P"); UI.Place(cb.Root, 0.5f, 1, 0, -262, 380, 30); cb.Root.sizeDelta = new Vector2(380, 30); cb.Set(Progress.TotalDiscovered() / (float)all);

            var lg = UI.Sliced(two, "card_a", 300, "League", true); lg.rectTransform.sizeDelta = new Vector2(488, 300);
            UI.Place(lg.rectTransform, 1, 0.5f, -244, 0, 488, 300);
            UI.Click(lg, () => ShowLeague(false));
            var sh = UI.Icon(lg.transform, "league", 116, "Shield"); UI.Place(sh.rectTransform, 0.5f, 1, 0, -86, 116, 116); sh.color = Color.Lerp(Color.white, League.TierColors[d.leagueTier], 0.45f);
            var lt = UI.Label(lg.transform, League.Tiers[d.leagueTier].ToUpper() + " LEAGUE", 34, Palette.Ink); UI.Place(lt.rectTransform, 0.5f, 1, 0, -174, 440, 44);
            var lr = UI.Label(lg.transform, $"Rank #{League.MyRank}", 30, Palette.InkSoft); UI.Place(lr.rectTransform, 0.5f, 1, 0, -216, 420, 38);
            var ld = UI.Label(lg.transform, $"{7 - League.DaysIntoWeek} days left", 28, Palette.InkSoft); UI.Place(ld.rectTransform, 0.5f, 1, 0, -254, 420, 36);
            y += 24;

            // ---- today's missions ----
            var ms = RowAt(content, ref y, 380);
            var mbg = UI.Sliced(ms, "card_a", 300, "Missions"); UI.Stretch(mbg.rectTransform);
            var mh = UI.Label(ms, "TODAY'S MISSIONS", 40, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(mh.rectTransform, 0, 1, 150, -50, 560, 50);
            var mp = UI.Label(ms, $"{Progress.MissionsDone()}/4", 40, Palette.Green, TextAnchor.MiddleRight); UI.Place(mp.rectTransform, 1, 1, -130, -50, 200, 50);
            var m = d.missions;
            string[] names = { "Finish the Daily Quest", "Find a Mystery Word", "Win with no power-ups", "Complete 3 levels" };
            bool[] st4 = { m.playedDaily, m.foundMystery, m.noPowerups, m.threeLevels };
            for (int i = 0; i < 4; i++)
            {
                var ic = UI.Icon(ms, st4[i] ? "check" : "star_empty", 52, "M" + i);
                UI.Place(ic.rectTransform, 0, 1, 70, -120 - i * 58, 52, 52);
                var nl = UI.Label(ms, names[i], 34, st4[i] ? Palette.Green : Palette.Ink, TextAnchor.MiddleLeft);
                UI.PlaceL(nl.rectTransform, 0, 1, 110, -120 - i * 58, 700, 48);
            }
            var perfect = UI.Label(ms, m.claimed ? "Perfect Day done!" : "All 4 = Perfect Day: +100 coins", 30, m.claimed ? Palette.Green : Palette.InkSoft);
            UI.Place(perfect.rectTransform, 0.5f, 0, 0, 30, 800, 40);
            var missIcon = UI.Icon(ms, "calendar", 110, "Cal"); UI.Place(missIcon.rectTransform, 0, 1, 76, -56, 90, 90);
            y += 20;
            EndScroll(content, y);
        }

        void UpdateDailyTimer()
        {
            if (dailyTimer == null) return;
            var t = Clock.UntilMidnight;
            dailyTimer.text = $"New quest in {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        float timerTick;
        void Update()
        {
            if (dailyTimer != null)
            {
                timerTick += Time.unscaledDeltaTime;
                if (timerTick > 0.5f) { timerTick = 0; UpdateDailyTimer(); }
            }
        }
    }
}
