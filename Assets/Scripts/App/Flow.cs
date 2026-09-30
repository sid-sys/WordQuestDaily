using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        // =============== before a level ===============
        public void OpenPrep(LevelSpec spec)
        {
            var d = SaveSystem.Data;
            var cat = WordBank.Get(spec.Category);
            var p = OpenPopup(spec.IsDaily ? "DAILY QUEST" : "LEVEL " + spec.Index, 940, 1400, true, spec.IsDaily ? "ribbon_pink" : "ribbon_green");
            var c = p.Content;

            var icon = UI.Icon(c, cat.ArtKey, 190, "Cat"); UI.Place(icon.rectTransform, 0.5f, 1, -280, -130, 190, 190);
            var name = UI.Label(c, cat.Name.ToUpper(), 56, Palette.Ink, TextAnchor.MiddleLeft); UI.Place(name.rectTransform, 0.5f, 1, 130, -90, 520, 70);
            var diff = UI.Label(c, Levels.DiffName(spec.Diff).ToUpper(), 40, Levels.DiffColor(spec.Diff), TextAnchor.MiddleLeft); UI.Place(diff.rectTransform, 0.5f, 1, 130, -150, 520, 50);
            var size = UI.Label(c, $"{spec.Cols} x {spec.Rows} board  -  {spec.WordCount} words + mystery", 30, Palette.InkSoft, TextAnchor.MiddleLeft); UI.Place(size.rectTransform, 0.5f, 1, 130 + 40, -204, 640, 40);

            // stars / objectives
            var goals = new[] { $"Find all {spec.WordCount} words", "Find the Mystery Word", "Win without power-ups" };
            for (int i = 0; i < 3; i++)
            {
                bool got = !spec.IsDaily && d.stars[spec.Index] > i;
                var s = UI.Icon(c, got ? "star" : "star_empty", 78, "S" + i);
                UI.Place(s.rectTransform, 0.5f, 1, -350, -350 - i * 96, 78, 78);
                var t = UI.Label(c, goals[i], 40, Palette.Ink, TextAnchor.MiddleLeft); UI.Place(t.rectTransform, 0.5f, 1, 60, -350 - i * 96, 700, 60);
                int bonus = Economy.StarBonus(i + 1);
                if (!spec.IsDaily && bonus > 0) { var bt = UI.Label(c, "+" + bonus, 34, Palette.Orange, TextAnchor.MiddleRight); UI.Place(bt.rectTransform, 1, 1, -60, -350 - i * 96, 130, 50); }
            }

            // reward preview
            int coins = spec.IsDaily ? 50 : Economy.BaseCoins(spec.Diff);
            bool replay = !spec.IsDaily && d.stars[spec.Index] > 0;
            if (replay) coins = Mathf.RoundToInt(coins * Economy.ReplayFactor);
            var rw = UI.Sliced(c, "chip", 90, "Reward"); rw.color = new Color(1f, 0.92f, 0.6f);
            UI.Place(rw.rectTransform, 0.5f, 1, 0, -690, 640, 96);
            var ci = UI.Icon(rw.transform, "coin", 76, "C"); UI.Place(ci.rectTransform, 0, 0.5f, 60, 0, 76, 76);
            var ct = UI.Label(rw.transform, replay ? $"{coins} coins (replay)" : $"{coins}+ coins", 42, Palette.Ink, TextAnchor.MiddleLeft); UI.Place(ct.rectTransform, 0, 0.5f, 130 + 200, 0, 400, 56);

            // power-ups you own
            var ph = UI.Label(c, "YOUR POWER-UPS", 36, Palette.InkSoft); UI.Place(ph.rectTransform, 0.5f, 1, 0, -790, 700, 46);
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var cell = UI.Node(c, "P" + i); UI.Place(cell, 0.5f, 1, (i - 2) * 160, -890, 150, 150);
                var ic = UI.Icon(cell, PowerUps.Art[i], 110, "I"); UI.Place(ic.rectTransform, 0.5f, 0.5f, 0, 10, 110, 110);
                UI.Click(ic, () => { p.Close(); ShowBuyPower((PowerUp)idx); });
                var n = PowerUps.Count(d, PowerUps.All[i]);
                var lb = UI.Label(cell, "x" + n, 36, n > 0 ? Palette.Ink : Palette.Red, TextAnchor.MiddleCenter); UI.Place(lb.rectTransform, 0.5f, 0, 0, 10, 140, 40);
            }
            var shop = UI.Pill(c, "btn_yellow", "+ SHOP", 300, 80, () => { p.Close(); ShowShop(null, () => OpenPrep(spec)); }, 36);
            UI.Place((RectTransform)shop.transform, 0.5f, 1, 0, -1060, 300, 80);

            var play = UI.Pill(c, "btn_green", "PLAY", 560, 130, () => StartGame(spec), 66);
            UI.Place((RectTransform)play.transform, 0.5f, 0, 0, 50, 560, 130);
            play.gameObject.AddComponent<Pulse>().Amount = 0.04f;
        }

        // =============== level complete ===============
        void ShowResult(LevelResult r)
        {
            var spec = r.Spec; var d = SaveSystem.Data;
            var cat = WordBank.Get(game.Puzzle.CategoryIndex);
            int lines = Mathf.Min(r.Lines.Count, 2);
            float h = 1160 + lines * 54;
            var p = OpenPopup(spec.IsDaily ? "DAILY COMPLETE!" : "LEVEL COMPLETE!", 900, h, false, "ribbon_pink");
            var c = p.Content;
            float y = 50;

            // stars
            for (int i = 0; i < 3; i++)
            {
                int idx = i; bool got = i < r.Stars;
                var s = UI.Icon(c, got ? "star" : "star_empty", i == 1 ? 190 : 150, "S" + i);
                UI.Place(s.rectTransform, 0.5f, 1, (i - 1) * 200, -y - (i == 1 ? 90 : 110), i == 1 ? 190 : 150, i == 1 ? 190 : 150);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 10);
                if (!got) s.color = new Color(1, 1, 1, 0.6f);
                else { Fx.PopIn(s.rectTransform, 0.4f, 0.25f + i * 0.28f); int k = i; Fx.Tween(0.01f, t => { }, () => { }, 0.25f + k * 0.28f); StartCoroutine(StarSfx(0.25f + i * 0.28f, i)); }
            }
            y += 200;
            var words = UI.Label(c, $"{r.WordsFound} words found", 46, Palette.Ink); UI.Place(words.rectTransform, 0.5f, 1, 0, -y - 20, 800, 60); y += 66;
            string myst = r.MysteryFound ? $"Mystery: {r.MysteryWord}" : "Mystery word not found";
            var mt = UI.Label(c, myst, 34, r.MysteryFound ? Palette.Purple : Palette.InkSoft); UI.Place(mt.rectTransform, 0.5f, 1, 0, -y - 16, 800, 46); y += 60;

            // coins + xp row
            var row = UI.Sliced(c, "card_a", 150, "Rewards"); UI.Place(row.rectTransform, 0.5f, 1, 0, -y - 90, 800, 170);
            var ci = UI.Icon(row.transform, "coin", 100, "C"); UI.Place(ci.rectTransform, 0, 0.5f, 100, 14, 100, 100);
            var coinT = UI.Label(row.transform, "+0", 64, Palette.Orange, TextAnchor.MiddleLeft, false); UI.Place(coinT.rectTransform, 0, 0.5f, 340, 14, 300, 80);
            int coinTotal = spec.IsDaily ? r.Coins : r.Coins + 0;
            var xi = UI.Icon(row.transform, "xp", 84, "X"); UI.Place(xi.rectTransform, 1, 0.5f, -270, 14, 84, 84);
            var xpT = UI.Label(row.transform, "+" + r.Xp + " XP", 44, Palette.Blue, TextAnchor.MiddleLeft, false); UI.Place(xpT.rectTransform, 1, 0.5f, -130, 14, 220, 60);
            var xb = UI.ProgressBar(row.transform, 500, 30, Palette.Blue, "Xp"); UI.Place(xb.Root, 0.5f, 0, 0, 20, 700, 30); xb.Root.sizeDelta = new Vector2(700, 30); xb.Set(Progress.XpFraction);
            var xl = UI.Label(xb.Root, $"LEVEL {d.playerLevel}", 22, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(xl.rectTransform);
            Fx.CountUp(coinT, 0, coinTotal, "+{0}", 0.9f);
            y += 190;

            if (spec.IsDaily)
            {
                var sc = UI.Label(c, $"Score {r.DailyScore:N0}", 44, Palette.Ink); UI.Place(sc.rectTransform, 0.5f, 1, 0, -y - 20, 800, 56); y += 56;
                int streak = Progress.EffectiveStreak();
                var st = UI.Label(c, $"Streak: {streak} days", 38, Palette.Orange); UI.Place(st.rectTransform, 0.5f, 1, 0, -y - 16, 800, 50); y += 54;
            }
            else
            {
                int f = Progress.Found(game.Puzzle.CategoryIndex), tot = Progress.TotalIn(game.Puzzle.CategoryIndex);
                var cb = UI.ProgressBar(c, 640, 40, Palette.Green, "Coll"); UI.Place(cb.Root, 0.5f, 1, 60, -y - 24, 640, 40); cb.Set(f / (float)tot);
                var cl = UI.Label(cb.Root, $"{cat.Name}  {f}/{tot}", 26, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(cl.rectTransform);
                var cbi = UI.Icon(c, cat.ArtKey, 66, "CI"); UI.Place(cbi.rectTransform, 0.5f, 1, -330, -y - 24, 66, 66);
                if (r.NewWords > 0) { var nw = UI.Label(c, $"{r.NewWords} new words for your collection!", 30, Palette.Green); UI.Place(nw.rectTransform, 0.5f, 1, 0, -y - 82, 800, 40); }
                y += 100;
            }
            for (int i = 0; i < lines; i++)
            {
                var t = UI.Label(c, r.Lines[i], 34, Palette.Purple); UI.Place(t.rectTransform, 0.5f, 1, 0, -y - 18, 820, 46); y += 50;
            }

            // buttons
            bool canDouble = r.Coins > 0 && Ads.IsReady(AdPlacement.DoubleReward);
            Button dbl = null;
            if (canDouble)
            {
                dbl = UI.Pill(c, "btn_yellow", $"x2 COINS  (AD)", 560, 92, null, 38);
                UI.Place((RectTransform)dbl.transform, 0.5f, 0, 0, 180, 560, 92);
                dbl.onClick.RemoveAllListeners();
                dbl.onClick.AddListener(() =>
                {
                    dbl.interactable = false;
                    Ads.ShowRewarded(AdPlacement.DoubleReward, ok =>
                    {
                        if (!ok) { dbl.interactable = true; Toast("No ad ready. Try again soon.", Palette.Red); return; }
                        Progress.AddCoins(r.Coins); Sfx.Play(Sfx.Kind.Coin);
                        if (dbl != null) { dbl.gameObject.SetActive(false); }
                        Fx.CountUp(coinT, r.Coins, r.Coins * 2, "+{0}", 0.6f);
                        Fx.Fly(fxLayer, Vector2.zero, CoinTarget(), "coin", 8);
                    });
                });
                dbl.gameObject.AddComponent<Pulse>().Amount = 0.04f;
            }
            bool hasNext = !spec.IsDaily && spec.Index < Levels.Total;
            var next = UI.Pill(c, "btn_green", hasNext ? "NEXT LEVEL" : "CONTINUE", 470, 110, () =>
            {
                p.Close();
                Ads.MaybeShowInterstitial(() =>
                {
                    if (hasNext) { var nspec = Levels.Get(spec.Index + 1); EndGame(); ShowTab(Tab.Levels); OpenPrep(nspec); }
                    else { EndGame(); ShowTab(Tab.Home); }
                });
            }, 50);
            UI.Place((RectTransform)next.transform, 0.5f, 0, 130, 50, 470, 110);
            var home = UI.Pill(c, "btn_blue", "HOME", 260, 110, () => { p.Close(); EndGame(); ShowTab(Tab.Home); }, 42);
            UI.Place((RectTransform)home.transform, 0.5f, 0, -300, 50, 260, 110);

            if (r.Coins > 0) Fx.Fly(fxLayer, new Vector2(0, 250), CoinTarget(), "coin", Mathf.Clamp(r.Coins / 25, 3, 10), () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.3f, 0.4f), null, 60f, 1.0f, 0.07f);
            if (r.LevelUps > 0) StartCoroutine(LevelUpToast(r.LevelUps));
            if (r.Stars == 3) Fx.Confetti(fxLayer, 1000, canvasRt.rect.height / 2 + 40, 30);
        }

        IEnumerator StarSfx(float delay, int i)
        {
            yield return new WaitForSecondsRealtime(delay + 0.1f);
            Sfx.Play(Sfx.Kind.Coin, 0.9f + i * 0.2f, 0.7f);
            Fx.Burst(fxLayer, new Vector2((i - 1) * 200, 250), "spark_star", Palette.Yellow, 8, 320f, 0.6f, 40f);
        }

        IEnumerator LevelUpToast(int n)
        {
            yield return new WaitForSecondsRealtime(1.3f);
            Sfx.Play(Sfx.Kind.Level);
            Toast($"LEVEL UP!  You are level {SaveSystem.Data.playerLevel}", Palette.Yellow);
        }
    }
}
