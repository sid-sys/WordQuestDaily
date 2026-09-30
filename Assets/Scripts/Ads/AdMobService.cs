#if WQ_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace WordQuest
{
    /// <summary>
    /// Google AdMob: consent form (UMP) first, then one rewarded ad and one interstitial kept ready.
    /// If loading fails it tries again every 30 seconds. Real ads only appear in release builds (see AdIds).
    /// </summary>
    public static class AdMobService
    {
        static RewardedAd rewarded;
        static InterstitialAd interstitial;
        static bool started, loadingRewarded, loadingInterstitial;
        static float nextTry;
        public static string Status = "starting";

        public static void Init()
        {
            if (Application.isEditor) { Status = "editor uses the test ad screen"; return; }
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null) Status = "consent: " + updateError.Message;
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Status = "consent form: " + formError.Message;
                    if (ConsentInformation.CanRequestAds()) StartAds(); else Status = "no consent to request ads";
                });
            });
            if (ConsentInformation.CanRequestAds()) StartAds();
        }

        static void StartAds()
        {
            if (started) return;
            started = true; Status = "initializing";
            MobileAds.Initialize(_ => { Status = "ready"; LoadRewarded(); LoadInterstitial(); });
        }

        /// <summary>Call every frame (cheap): retries loading that failed earlier.</summary>
        public static void Tick()
        {
            if (!started || Time.unscaledTime < nextTry) return;
            nextTry = Time.unscaledTime + 30f;
            if (rewarded == null && !loadingRewarded) LoadRewarded();
            if (interstitial == null && !loadingInterstitial) LoadInterstitial();
        }

        static void LoadRewarded()
        {
            loadingRewarded = true;
            RewardedAd.Load(AdIds.Rewarded(AdPlacement.Coins), new AdRequest(), (ad, error) =>
            {
                loadingRewarded = false;
                if (error != null || ad == null) { Status = "rewarded: " + (error != null ? error.GetMessage() : "empty"); return; }
                rewarded = ad;
            });
        }

        static void LoadInterstitial()
        {
            loadingInterstitial = true;
            InterstitialAd.Load(AdIds.Interstitial, new AdRequest(), (ad, error) =>
            {
                loadingInterstitial = false;
                if (error != null || ad == null) { Status = "interstitial: " + (error != null ? error.GetMessage() : "empty"); return; }
                interstitial = ad;
            });
        }

        public static bool IsRewardedReady(AdPlacement placement) => Application.isEditor || (rewarded != null && rewarded.CanShowAd());

        public static void ShowRewarded(AdPlacement placement, Action<bool> done)
        {
            if (Application.isEditor) { GameApp.I.ShowTestAd(done); return; }
            if (rewarded == null || !rewarded.CanShowAd()) { done?.Invoke(false); return; }
            var ad = rewarded; rewarded = null;
            bool earned = false;
            ad.OnAdFullScreenContentClosed += () => { done?.Invoke(earned); LoadRewarded(); };
            ad.OnAdFullScreenContentFailed += err => { done?.Invoke(false); LoadRewarded(); };
            ad.Show(_ => earned = true);
        }

        /// <summary>Shows a full-screen ad if one is ready. Returns true when an ad was shown (the callback continues the game afterwards).</summary>
        public static bool ShowInterstitial(Action next)
        {
            if (Application.isEditor || interstitial == null || !interstitial.CanShowAd()) return false;
            var ad = interstitial; interstitial = null;
            ad.OnAdFullScreenContentClosed += () => { next?.Invoke(); LoadInterstitial(); };
            ad.OnAdFullScreenContentFailed += err => { next?.Invoke(); LoadInterstitial(); };
            ad.Show();
            return true;
        }
    }
}
#endif
