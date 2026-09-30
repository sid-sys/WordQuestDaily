using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordQuest
{
    [Serializable] public class CatWords { public int cat; public List<string> words = new List<string>(); public int claimedStep; }

    [Serializable]
    public class MissionState
    {
        public string date;
        public bool playedDaily, foundMystery, noPowerups, threeLevels;
        public int levelsToday;
        public bool claimed;
    }

    [Serializable]
    public class PlayerData
    {
        public string name = "Player";
        public int coins = 300;
        public int xp;
        public int playerLevel = 1;
        public int[] powers = { 2, 2, 1, 1, 0 };          // shuffle, hint, letter, finder, word

        // progress
        public int[] stars = new int[Levels.Total + 1];    // index = level, 0..3
        public int highestUnlocked = 1;
        public int levelsCompleted;                        // unique levels completed at least once
        public int wordsFound, perfectLevels, gamesPlayed, mysteryFound, dailyDone;

        // daily systems (dates are yyyy-MM-dd, local time, reset at 00:00)
        public string lastDailyRewardDate = "";
        public int dailyRewardDay;                         // 0..6 next reward to give
        public string lastDailyRewardClaimDate = "";
        public string dailyQuestDoneDate = "";
        public int dailyQuestScore;
        public int streak, bestStreak;
        public string lastStreakDate = "";
        public int freezes = 1;
        public int streakClaimed;                          // highest streak milestone claimed
        public MissionState missions = new MissionState();

        // collection
        public List<CatWords> collection = new List<CatWords>();

        // league
        public int leagueTier;                             // 0 bronze .. 4 diamond
        public string leagueWeek = "";
        public int leaguePoints;

        // cosmetics
        public int avatar, ring, theme, effect;
        public bool[] avatarOwned = new bool[12];
        public bool[] themeOwned = { true, false, false, false };
        public bool[] effectOwned = { true, false, false, false, false };
        public bool[] ringOwned = { false, false, false, false };
        public bool[] achievementClaimed = new bool[12];

        // ads / purchases
        public bool adsRemoved;
        public string adsDate = "";
        public int rewardedCoinAdsToday;
        public int levelsSinceInterstitial;

        // settings
        public bool sound = true, haptics = true;
        public bool tutorialDone;
        public string installDate = "";
    }

    public static class SaveSystem
    {
        const string Key = "wordquest_save_v2";
        public static PlayerData Data { get; private set; }

        public static void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(Key)) Data = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(Key));
            }
            catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); }
            if (Data == null) { Data = new PlayerData(); Data.avatarOwned[0] = true; Data.installDate = Clock.TodayKey; }
            Fix(Data);
        }

        static void Fix(PlayerData d)
        {
            if (d.stars == null || d.stars.Length < Levels.Total + 1) { var n = new int[Levels.Total + 1]; if (d.stars != null) Array.Copy(d.stars, n, Mathf.Min(d.stars.Length, n.Length)); d.stars = n; }
            if (d.powers == null || d.powers.Length < 5) { var n = new[] { 0, 0, 0, 0, 0 }; if (d.powers != null) Array.Copy(d.powers, n, Mathf.Min(d.powers.Length, 5)); d.powers = n; }
            if (d.avatarOwned == null || d.avatarOwned.Length < 12) d.avatarOwned = new bool[12];
            if (d.themeOwned == null || d.themeOwned.Length < 4) d.themeOwned = new[] { true, false, false, false };
            if (d.effectOwned == null || d.effectOwned.Length < 5) d.effectOwned = new[] { true, false, false, false, false };
            if (d.ringOwned == null || d.ringOwned.Length < 4) d.ringOwned = new bool[4];
            if (d.achievementClaimed == null || d.achievementClaimed.Length < 12) d.achievementClaimed = new bool[12];
            if (d.missions == null) d.missions = new MissionState();
            if (d.collection == null) d.collection = new List<CatWords>();
            d.avatarOwned[0] = true; d.themeOwned[0] = true; d.effectOwned[0] = true;
            if (d.playerLevel < 1) d.playerLevel = 1;
        }

        public static void Save()
        {
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        public static void Reset() { PlayerPrefs.DeleteKey(Key); Data = null; Load(); }
    }

    /// <summary>Local calendar date. Daily things reset at 00:00 device time. `DebugDays` lets tests jump ahead.</summary>
    public static class Clock
    {
        public static int DebugDays;
        public static DateTime Now => DateTime.Now.AddDays(DebugDays);
        public static DateTime Today => Now.Date;
        public static string TodayKey => Today.ToString("yyyy-MM-dd");
        public static string KeyOf(DateTime d) => d.ToString("yyyy-MM-dd");
        public static TimeSpan UntilMidnight => Today.AddDays(1) - Now;
        public static int DaysBetween(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 999;
            return (int)(DateTime.Parse(b) - DateTime.Parse(a)).TotalDays;
        }
        /// <summary>Monday of the current week, used for the weekly league.</summary>
        public static string WeekKey
        {
            get
            {
                var t = Today; int back = ((int)t.DayOfWeek + 6) % 7;
                return KeyOf(t.AddDays(-back));
            }
        }
    }
}
