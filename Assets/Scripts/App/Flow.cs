using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        // =============== starting a level ===============
        /// <summary>No pre-level screen any more: tapping a level starts it.</summary>
        public void OpenPrep(LevelSpec spec) { StartGame(spec); }

        static LevelSpec NextSpecAfter(LevelSpec spec)
        {
            if (spec.IsDaily) return Levels.Get(Progress.NextLevel);
            return Levels.Get(Mathf.Min(spec.Index + 1, Levels.Total));
        }

        // =============== level complete ===============
        void ShowResult(LevelResult r)
        {
            var spec = r.Spec; var d = SaveSystem.Data;
            var cat = WordBank.Get(game.Puzzle.CategoryIndex);
            bool canDouble = r.Coins > 0 && Ads.IsReady(AdPlacement.DoubleReward);

            // vertical plan (all sizes known up front so nothing can overlap)
            const float starsH = 200, wordsH = 60, mysteryH = 56, rewardH = 190, gap = 26, collH = 92;
            float top = starsH + wordsH + mysteryH + rewardH + gap + collH;
            float bottom = 112 + (canDouble ? 96 + 18 : 0) + (spec.IsDaily ? 80 + 18 : 0);
            float h = top + bottom + 30 + 36 + 96 + 36;
            var p = OpenPopup(spec.IsDaily ? "DAILY COMPLETE!" : "LEVEL COMPLETE!", 900, h, false, "ribbon_pink");
            var c = p.Content;
            float y = 0;

            // stars
            for (int i = 0; i < 3; i++)
            {
                bool got = i < r.Stars;
                float sz = i == 1 ? 184 : 146;
                var s = UI.Icon(c, got ? "star" : "star_empty", sz, "S" + i);
                UI.Place(s.rectTransform, 0.5f, 1, (i - 1) * 196, -(starsH / 2) + (i == 1 ? 6 : -14), sz, sz);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - i) * 10);
                if (!got) s.color = new Color(1, 1, 1, 0.6f);
                else { Fx.PopIn(s.rectTransform, 0.4f, 0.25f + i * 0.28f); StartCoroutine(StarSfx(0.25f + i * 0.28f, i)); }
            }
            y += starsH;
            var words = UI.Label(c, $"{r.WordsFound} words found", 46, Palette.Ink); UI.Place(words.rectTransform, 0.5f, 1, 0, -(y + wordsH / 2), 800, 56); y += wordsH;
            string myst = r.MysteryFound ? $"Mystery: {r.MysteryWord}" : "Mystery word not found";
            var mt = UI.Label(c, myst, 34, r.MysteryFound ? Palette.Purple : Palette.InkSoft); UI.Place(mt.rectTransform, 0.5f, 1, 0, -(y + mysteryH / 2), 800, 44); y += mysteryH;

            // coins + xp
            var row = UI.Sliced(c, "card_a", rewardH, "Rewards"); UI.Place(row.rectTransform, 0.5f, 1, 0, -(y + rewardH / 2), 820, rewardH);
            var ci = UI.Icon(row.transform, "coin", 92, "C"); UI.Place(ci.rectTransform, 0, 0.5f, 100, 26, 92, 92);
            var coinT = UI.Label(row.transform, "+0", 60, Palette.Orange, TextAnchor.MiddleLeft, false); UI.PlaceL(coinT.rectTransform, 0, 0.5f, 168, 26, 240, 80);
            var xi = UI.Icon(row.transform, "xp", 80, "X"); UI.Place(xi.rectTransform, 0.5f, 0.5f, 60, 26, 80, 80);
            var xpT = UI.Label(row.transform, "+" + r.Xp + " XP", 44, Palette.Blue, TextAnchor.MiddleLeft, false); UI.PlaceL(xpT.rectTransform, 0.5f, 0.5f, 118, 26, 230, 60);
            var xb = UI.ProgressBar(row.transform, 700, 28, Palette.Blue, "Xp"); UI.Place(xb.Root, 0.5f, 0, 0, 34, 700, 28); xb.Root.sizeDelta = new Vector2(700, 28); xb.Set(Progress.XpFraction);
            Fx.CountUp(coinT, 0, r.Coins, "+{0}", 0.9f);
            y += rewardH + gap;

            // collection (levels) or score (daily)
            if (spec.IsDaily)
            {
                var sc = UI.Label(c, $"Score {r.DailyScore:N0}", 48, Palette.Ink); UI.Place(sc.rectTransform, 0.5f, 1, 0, -(y + collH / 2), 800, 60);
            }
            else
            {
                int f = Progress.Found(game.Puzzle.CategoryIndex), tot = Progress.TotalIn(game.Puzzle.CategoryIndex);
                var cbi = UI.Icon(c, cat.ArtKey, 70, "CI"); UI.Place(cbi.rectTransform, 0.5f, 1, -330, -(y + collH / 2), 70, 70);
                var cb = UI.ProgressBar(c, 640, 44, Palette.Green, "Coll"); UI.Place(cb.Root, 0.5f, 1, 60, -(y + collH / 2), 640, 44); cb.Root.sizeDelta = new Vector2(640, 44); cb.Set(f / (float)tot);
                var cl = UI.Label(cb.Root, $"{cat.Name}  {f}/{tot}", 26, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(cl.rectTransform);
            }

            // buttons (from the bottom up)
            float by = 56;
            var next = UI.Pill(c, "btn_green", "NEXT LEVEL", 640, 112, () =>
            {
                p.Close();
                var nspec = NextSpecAfter(spec);
                bool done = !spec.IsDaily && spec.Index >= Levels.Total;
                Ads.MaybeShowInterstitial(() => { if (done) { EndGame(); ShowTab(Tab.Home); } else StartGame(nspec); });
            }, 52);
            UI.Place((RectTransform)next.transform, 0.5f, 0, 0, by, 640, 112); by += 112 / 2 + 18 + 96 / 2;
            if (canDouble)
            {
                var dbl = UI.Pill(c, "btn_yellow", "x2 COINS  (AD)", 560, 96, null, 40);
                UI.Place((RectTransform)dbl.transform, 0.5f, 0, 0, by, 560, 96);
                dbl.onClick.RemoveAllListeners();
                dbl.onClick.AddListener(() =>
                {
                    dbl.interactable = false;
                    Ads.ShowRewarded(AdPlacement.DoubleReward, ok =>
                    {
                        if (!ok) { if (dbl != null) dbl.interactable = true; Toast("No ad ready. Try again soon.", Palette.Red); return; }
                        Progress.AddCoins(r.Coins); Sfx.Play(Sfx.Kind.Coin);
                        if (dbl != null) dbl.gameObject.SetActive(false);
                        Fx.CountUp(coinT, r.Coins, r.Coins * 2, "+{0}", 0.6f);
                        Fx.Fly(fxLayer, Vector2.zero, CoinTarget(), "coin", 8);
                    });
                });
                by += 96 / 2 + 18 + 80 / 2;
            }
            if (spec.IsDaily)
            {
                var share = UI.Pill(c, "btn_purple", "SHARE", 300, 80, () =>
                {
                    GUIUtility.systemCopyBuffer = $"Word Quest Daily {Clock.TodayKey}\n{new string('⭐', r.Stars)}  {r.DailyScore:N0} points";
                    Toast("Result copied! Paste it to share.", Palette.Green);
                }, 38);
                UI.Place((RectTransform)share.transform, 0.5f, 0, 0, by, 300, 80);
            }

            if (r.Coins > 0) Fx.Fly(fxLayer, new Vector2(0, 250), CoinTarget(), "coin", Mathf.Clamp(r.Coins / 25, 3, 10), () => Sfx.Play(Sfx.Kind.Coin, 1f + UnityEngine.Random.value * 0.3f, 0.4f), null, 60f, 1.0f, 0.07f);
            if (r.LevelUps > 0) { Sfx.Play(Sfx.Kind.Level); Fx.Burst(fxLayer, new Vector2(0, 200), "spark_star", Palette.Yellow, 14, 520f, 0.9f, 52f); }
            if (r.Stars == 3) Fx.Confetti(fxLayer, 1000, canvasRt.rect.height / 2 + 40, 30);
        }

        IEnumerator StarSfx(float delay, int i)
        {
            yield return new WaitForSecondsRealtime(delay + 0.1f);
            Sfx.Play(Sfx.Kind.Coin, 0.9f + i * 0.2f, 0.7f);
            Fx.Burst(fxLayer, new Vector2((i - 1) * 200, 250), "spark_star", Palette.Yellow, 8, 320f, 0.6f, 40f);
        }
    }
}
