using UnityEngine;

namespace WordQuest
{
    /// <summary>
    /// AdMob ids. Editor and development builds always use Google's test ads, release builds use the real ones,
    /// so a test tap can never hurt the AdMob account.
    /// </summary>
    public static class AdIds
    {
        public const string AndroidAppId = "ca-app-pub-2778983812131153~6626345378";

        const string RealInterstitial = "ca-app-pub-2778983812131153/1109371372";
        const string RealRewarded = "ca-app-pub-2778983812131153/7183825139";       // coins, double reward and hint share this unit
        const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
        const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";

        public static bool UseTest => Application.isEditor || Debug.isDebugBuild;
        public static string Interstitial => UseTest ? TestInterstitial : RealInterstitial;
        public static string Rewarded(AdPlacement placement) => UseTest ? TestRewarded : RealRewarded;
    }
}
