using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public enum AdPlacement { Coins, DoubleReward, Hint }

    /// <summary>
    /// One place to ask for a rewarded video. In the Editor and in development builds a simple test ad is shown.
    /// With the Google Mobile Ads plugin installed (define WQ_ADMOB) real ads are used.
    /// </summary>
    public static class Ads
    {
        public static bool IsReady(AdPlacement placement)
        {
            if (SaveSystem.Data != null && SaveSystem.Data.adsRemoved && false) return false;
#if WQ_ADMOB
            return AdMobService.IsRewardedReady(placement);
#else
            return Application.isEditor || Debug.isDebugBuild;
#endif
        }

        public static void ShowRewarded(AdPlacement placement, Action<bool> done)
        {
#if WQ_ADMOB
            AdMobService.ShowRewarded(placement, done);
#else
            if (Application.isEditor || Debug.isDebugBuild) GameApp.I.ShowTestAd(done); else done?.Invoke(false);
#endif
        }

        /// <summary>Full-screen ad between levels. Never shown to players who bought Remove Ads.</summary>
        public static void MaybeShowInterstitial(Action next)
        {
            var d = SaveSystem.Data;
            d.levelsSinceInterstitial++;
            bool allowed = !d.adsRemoved && d.levelsCompleted >= 4 && d.levelsSinceInterstitial >= 3;
#if WQ_ADMOB
            if (allowed && AdMobService.ShowInterstitial(next)) { d.levelsSinceInterstitial = 0; SaveSystem.Save(); return; }
#endif
            SaveSystem.Save();
            next?.Invoke();
        }
    }

    public partial class GameApp
    {
        /// <summary>Test ad used in the Editor and development builds.</summary>
        public void ShowTestAd(Action<bool> done)
        {
            var root = UI.Stretch(UI.Node(popupLayer, "TestAd"));
            var bg = UI.Solid(root, new Color(0.05f, 0.05f, 0.1f, 0.96f), "Bg", true);
            UI.Stretch(bg.rectTransform);
            var t = UI.Label(root, "TEST AD", 90, Color.white);
            UI.Place(t.rectTransform, 0.5f, 0.5f, 0, 120, 900, 120);
            var s = UI.Label(root, "Reward in 3", 50, Palette.Yellow);
            UI.Place(s.rectTransform, 0.5f, 0.5f, 0, -20, 900, 80);
            float left = 3f;
            Fx.Tween(3f, k =>
            {
                left = 3f * (1 - k);
                if (s != null) s.text = "Reward in " + Mathf.CeilToInt(left);
            }, () =>
            {
                if (root == null) return;
                s.text = "Done!";
                var b = UI.Pill(root, "btn_green", "CLOSE", 420, 120, () => { Destroy(root.gameObject); done?.Invoke(true); }, 52);
                UI.Place((RectTransform)b.transform, 0.5f, 0.5f, 0, -220, 420, 120);
            });
        }
    }
}
