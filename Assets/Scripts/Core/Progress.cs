using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordQuest
{
    public class LevelResult
    {
        public LevelSpec Spec;
        public int Stars;
        public bool FirstClear, MysteryFound, NoPowerups;
        public int Coins, Xp, BonusCoins;
        public int NewWords;
        public string MysteryWord;
        public bool MysteryIsNew;
        public int LevelUps;
        public List<string> Lines = new List<string>();     // extra reward lines: "+250 coins (5 level bonus)", "Free Shuffle" ...
        public int DailyScore;
        public int WordsFound;
    }

    /// <summary>All changes to the player's progress go through here so rules stay in one place.</summary>
    public static class Progress
    {
        public static event Action Changed;
        static PlayerData D => SaveSystem.Data;
        public static void Notify() { SaveSystem.Save(); Changed?.Invoke(); }

        // ---------------- coins / xp ----------------
        public static void AddCoins(int n) { D.coins += n; Notify(); }
        public static bool Spend(int n)
        {
            if (D.coins < n) return false;
            D.coins -= n; Notify(); return true;
        }

        /// <summary>Adds XP and returns how many player levels were gained.</summary>
        public static int AddXp(int n, List<string> lines = null)
        {
            int ups = 0;
            D.xp += n;
            while (D.xp >= Economy.XpForLevel(D.playerLevel))
            {
                D.xp -= Economy.XpForLevel(D.playerLevel);
                D.playerLevel++; ups++;
                D.coins += Economy.LevelUpCoins;
                lines?.Add($"Level {D.playerLevel}! +{Economy.LevelUpCoins} coins");
                UnlockByLevel(lines);
            }
            return ups;
        }

        static void UnlockByLevel(List<string> lines)
        {
            for (int i = 0; i < Economy.ThemeLevel.Length; i++)
                if (!D.themeOwned[i] && D.playerLevel >= Economy.ThemeLevel[i]) { D.themeOwned[i] = true; lines?.Add(Economy.ThemeNames[i] + " board unlocked"); }
            for (int i = 0; i < Economy.EffectLevel.Length; i++)
                if (!D.effectOwned[i] && D.playerLevel >= Economy.EffectLevel[i]) { D.effectOwned[i] = true; lines?.Add(Economy.EffectNames[i] + " effect unlocked"); }
            for (int i = 0; i < Economy.AvatarLevel.Length; i++)
                if (!D.avatarOwned[i] && D.playerLevel >= Economy.AvatarLevel[i] && i != 9) { D.avatarOwned[i] = true; lines?.Add(Economy.AvatarNames[i] + " avatar unlocked"); }
            for (int i = 0; i < Economy.RingLevel.Length; i++)
                if (!D.ringOwned[i] && D.playerLevel >= Economy.RingLevel[i]) { D.ringOwned[i] = true; lines?.Add(Economy.RingNames[i] + " frame unlocked"); }
        }

        public static float XpFraction => Mathf.Clamp01(D.xp / (float)Economy.XpForLevel(D.playerLevel));

        // ---------------- levels ----------------
        public static int TotalStars() => D.stars.Sum();
        public static bool IsUnlocked(int level) => level <= D.highestUnlocked;
        public static int NextLevel => Mathf.Min(D.highestUnlocked, Levels.Total);

        public static LevelResult CompleteLevel(LevelSpec spec, Puzzle puzzle, bool mysteryFound, int powerupsUsed)
        {
            var r = new LevelResult { Spec = spec, MysteryFound = mysteryFound, NoPowerups = powerupsUsed == 0 };
            r.WordsFound = puzzle.Words.Count + (mysteryFound ? 1 : 0);
            r.Stars = 1 + (mysteryFound ? 1 : 0) + (powerupsUsed == 0 ? 1 : 0);
            bool daily = spec.IsDaily;
            D.gamesPlayed++;
            D.unfinishedLevel = 0;
            D.wordsFound += r.WordsFound;
            if (mysteryFound) D.mysteryFound++;
            if (r.Stars == 3) D.perfectLevels++;

            if (mysteryFound) { r.MysteryWord = puzzle.Mystery.Word; r.MysteryIsNew = Discover(puzzle.CategoryIndex, puzzle.Mystery.Word); }
            foreach (var w in puzzle.Words) if (Discover(puzzle.CategoryIndex, w.Word)) r.NewWords++;
            if (r.MysteryIsNew) r.NewWords++;

            if (!daily)
            {
                r.FirstClear = D.stars[spec.Index] == 0;
                int coins = Economy.BaseCoins(spec.Diff) + Economy.StarBonus(r.Stars);
                if (!r.FirstClear) coins = Mathf.RoundToInt(coins * Economy.ReplayFactor);
                r.Coins = coins;
                D.coins += coins;
                if (r.Stars > D.stars[spec.Index]) D.stars[spec.Index] = r.Stars;
                if (r.FirstClear)
                {
                    D.levelsCompleted++;
                    if (spec.Index >= D.highestUnlocked && spec.Index < Levels.Total) D.highestUnlocked = spec.Index + 1;
                    GrantMilestones(r);
                }
                r.Xp = Economy.XpForFinish(r.Stars) / (r.FirstClear ? 1 : 2);
                D.leaguePoints += 15;
                D.missions.levelsToday++;
                if (D.missions.levelsToday >= 3) D.missions.threeLevels = true;
            }
            else
            {
                r.DailyScore = 1000 + 100 * puzzle.Words.Count + (mysteryFound ? 500 : 0) + (powerupsUsed == 0 ? 300 : 0) + Mathf.Min(D.streak, 30) * 20;
                r.FirstClear = D.dailyQuestDoneDate != Clock.TodayKey;
                if (r.FirstClear)
                {
                    D.dailyQuestDoneDate = Clock.TodayKey;
                    D.dailyQuestScore = r.DailyScore;
                    D.dailyDone++;
                    r.Coins = 50 + (r.Stars == 3 ? 100 : r.Stars == 2 ? 25 : 0);
                    D.coins += r.Coins;
                    r.Xp = 100 + 25 * r.Stars;
                    D.leaguePoints += r.DailyScore / 10;
                    D.missions.playedDaily = true;
                    RegisterStreak(r);
                }
                else
                {
                    r.Coins = 0; r.Xp = 10;
                    if (r.DailyScore > D.dailyQuestScore) { D.leaguePoints += (r.DailyScore - D.dailyQuestScore) / 10; D.dailyQuestScore = r.DailyScore; }
                }
            }
            if (mysteryFound) D.missions.foundMystery = true;
            if (powerupsUsed == 0) D.missions.noPowerups = true;
            r.LevelUps = AddXp(r.Xp, r.Lines);
            CheckMissions(r);
            Notify();
            return r;
        }

        static void GrantMilestones(LevelResult r)
        {
            int n = D.levelsCompleted;
            if (n % Economy.MilestoneEvery == 0)
            {
                D.coins += Economy.MilestoneCoins; r.BonusCoins += Economy.MilestoneCoins;
                r.Lines.Add($"+{Economy.MilestoneCoins} coins ({n} levels done)");
            }
            for (int i = 0; i < PowerUps.All.Length; i++)
                if (n % PowerUps.FreeEvery[i] == 0)
                {
                    var p = PowerUps.All[i];
                    if (PowerUps.Add(D, p)) r.Lines.Add("Free " + PowerUps.Names[i]);
                    else { D.coins += PowerUps.Cost[i] / 2; r.BonusCoins += PowerUps.Cost[i] / 2; r.Lines.Add($"{PowerUps.Names[i]} is full: +{PowerUps.Cost[i] / 2} coins"); }
                }
        }

        // ---------------- collection ----------------
        public static CatWords CatEntry(int cat)
        {
            var e = D.collection.FirstOrDefault(c => c.cat == cat);
            if (e == null) { e = new CatWords { cat = cat }; D.collection.Add(e); }
            return e;
        }
        public static bool Discover(int cat, string word)
        {
            var e = CatEntry(cat);
            if (e.words.Contains(word)) return false;
            e.words.Add(word); return true;
        }
        public static int Found(int cat) => CatEntry(cat).words.Count;
        public static int TotalIn(int cat) => WordBank.Get(cat).Words.Length;
        public static int TotalDiscovered() => D.collection.Sum(c => c.words.Count);
        public static bool CollectionComplete(int cat) => Found(cat) >= TotalIn(cat);
        public static int CollectionsComplete() { int n = 0; for (int i = 0; i < WordBank.Count; i++) if (CollectionComplete(i)) n++; return n; }

        static readonly float[] CollectionSteps = { 0.25f, 0.5f, 0.75f, 1f };
        static readonly int[] CollectionStepCoins = { 250, 250, 500, 1000 };
        /// <summary>Returns the step (0-3) that can be claimed for this category, or -1.</summary>
        public static int ClaimableStep(int cat)
        {
            var e = CatEntry(cat);
            float f = Found(cat) / (float)TotalIn(cat);
            return e.claimedStep < 4 && f >= CollectionSteps[e.claimedStep] ? e.claimedStep : -1;
        }
        public static int ClaimStep(int cat)
        {
            int s = ClaimableStep(cat);
            if (s < 0) return 0;
            CatEntry(cat).claimedStep = s + 1;
            D.coins += CollectionStepCoins[s];
            Notify();
            return CollectionStepCoins[s];
        }
        public static string StepLabel(int cat)
        {
            var e = CatEntry(cat);
            if (e.claimedStep >= 4) return "Complete";
            return $"{Mathf.RoundToInt(CollectionSteps[e.claimedStep] * 100)}%: {CollectionStepCoins[e.claimedStep]} coins";
        }

        // ---------------- daily reward (once a day, resets at 00:00) ----------------
        public static bool CanClaimDailyReward() => D.lastDailyRewardClaimDate != Clock.TodayKey;
        public static int DailyRewardDayIndex()
        {
            // a gap of more than one day restarts the 7 day cycle
            if (!string.IsNullOrEmpty(D.lastDailyRewardClaimDate) && Clock.DaysBetween(D.lastDailyRewardClaimDate, Clock.TodayKey) > 1) return 0;
            return D.dailyRewardDay % 7;
        }
        public static Economy.Reward ClaimDailyReward()
        {
            int day = DailyRewardDayIndex();
            var r = Economy.Daily[day];
            D.coins += r.Coins;
            if (r.Power >= 0) if (!PowerUps.Add(D, (PowerUp)r.Power, r.PowerCount)) D.coins += PowerUps.Cost[r.Power] / 2;
            D.dailyRewardDay = (day + 1) % 7;
            D.lastDailyRewardClaimDate = Clock.TodayKey;
            Notify();
            return r;
        }

        // ---------------- streak ----------------
        public static readonly int[] StreakSteps = { 3, 7, 14, 30, 60, 100 };
        public static readonly string[] StreakRewards = { "100 coins", "1 Hint", "Silver frame", "Dragon avatar", "500 coins", "Diamond frame" };

        public static int EffectiveStreak()
        {
            if (string.IsNullOrEmpty(D.lastStreakDate)) return 0;
            int gap = Clock.DaysBetween(D.lastStreakDate, Clock.TodayKey);
            if (gap <= 1) return D.streak;
            if (gap == 2 && D.freezes > 0) return D.streak;
            return 0;
        }

        static void RegisterStreak(LevelResult r)
        {
            string today = Clock.TodayKey;
            int gap = Clock.DaysBetween(D.lastStreakDate, today);
            if (gap == 1) D.streak++;
            else if (gap == 2 && D.freezes > 0) { D.freezes--; D.streak++; r.Lines.Add("Streak Freeze saved your streak!"); }
            else D.streak = 1;
            D.lastStreakDate = today;
            if (D.streak > D.bestStreak) D.bestStreak = D.streak;
            if (D.streak % 7 == 0 && D.freezes < 3) { D.freezes++; r.Lines.Add("You earned a Streak Freeze"); }
            for (int i = 0; i < StreakSteps.Length; i++)
            {
                if (D.streak >= StreakSteps[i] && D.streakClaimed < StreakSteps[i])
                {
                    D.streakClaimed = StreakSteps[i];
                    switch (i)
                    {
                        case 0: D.coins += 100; break;
                        case 1: PowerUps.Add(D, PowerUp.Hint); break;
                        case 2: D.ringOwned[1] = true; break;
                        case 3: D.avatarOwned[9] = true; break;
                        case 4: D.coins += 500; break;
                        case 5: D.ringOwned[3] = true; break;
                    }
                    r.Lines.Add($"{StreakSteps[i]} day streak: {StreakRewards[i]}");
                }
            }
        }

        // ---------------- missions ----------------
        public static void EnsureMissionsToday()
        {
            if (D.missions.date == Clock.TodayKey) return;
            D.missions = new MissionState { date = Clock.TodayKey };
            Notify();
        }
        public static int MissionsDone()
        {
            var m = D.missions; return (m.playedDaily ? 1 : 0) + (m.foundMystery ? 1 : 0) + (m.noPowerups ? 1 : 0) + (m.threeLevels ? 1 : 0);
        }
        static void CheckMissions(LevelResult r)
        {
            if (MissionsDone() == 4 && !D.missions.claimed)
            {
                D.missions.claimed = true;
                D.coins += 100; r.BonusCoins += 100;
                r.Lines.Add("Perfect Day! +100 coins");
                r.LevelUps += AddXp(50, r.Lines);
            }
        }

        // ---------------- achievements ----------------
        public class Achievement { public string Name, Desc, Art; public int Goal; public int Coins; public Func<PlayerData, int> Value; }
        public static readonly Achievement[] Achievements =
        {
            new Achievement { Name = "Word Hunter", Desc = "Find 1,000 words", Art = "medal_hunter", Goal = 1000, Coins = 500, Value = d => d.wordsFound },
            new Achievement { Name = "Streak Master", Desc = "Reach a 30 day streak", Art = "medal_streak", Goal = 30, Coins = 500, Value = d => d.bestStreak },
            new Achievement { Name = "Mystery Hunter", Desc = "Find 25 mystery words", Art = "medal_speed", Goal = 25, Coins = 300, Value = d => d.mysteryFound },
            new Achievement { Name = "Explorer", Desc = "Discover 500 different words", Art = "medal_explorer", Goal = 500, Coins = 500, Value = d => d.collection.Sum(c => c.words.Count) },
            new Achievement { Name = "Perfect Week", Desc = "Finish 7 daily quests", Art = "medal_week", Goal = 7, Coins = 300, Value = d => d.dailyDone },
            new Achievement { Name = "Word Master", Desc = "Finish level 100", Art = "medal_master", Goal = 100, Coins = 1000, Value = d => Mathf.Max(0, d.highestUnlocked - 1) },
            new Achievement { Name = "Collector", Desc = "Complete a collection", Art = "medal_collector", Goal = 1, Coins = 500, Value = d => CollectionsComplete() },
            new Achievement { Name = "Star Champion", Desc = "Earn 100 stars", Art = "medal_champion", Goal = 100, Coins = 500, Value = d => d.stars.Sum() },
        };
        public static bool AchievementReady(int i) => !D.achievementClaimed[i] && Achievements[i].Value(D) >= Achievements[i].Goal;
        public static int ReadyAchievements() { int n = 0; for (int i = 0; i < Achievements.Length; i++) if (AchievementReady(i)) n++; return n; }
        public static int ClaimAchievement(int i)
        {
            if (!AchievementReady(i)) return 0;
            D.achievementClaimed[i] = true; D.coins += Achievements[i].Coins; Notify();
            return Achievements[i].Coins;
        }

        // ---------------- bundles ----------------
        /// <summary>Gives the contents of a paid bundle. Returns a short text for the toast.</summary>
        public static string GrantBundle(Bundle b, bool firstTimeOnly = false)
        {
            if (b == null) return "";
            if (firstTimeOnly) { if (D.adFreeKitClaimed) return ""; D.adFreeKitClaimed = true; }
            D.coins += b.Coins;
            for (int i = 0; i < 5; i++) if (b.Powers[i] > 0) PowerUps.Add(D, PowerUps.All[i], b.Powers[i], true);
            if (b.ExclusiveEffect >= 0) D.effectOwned[b.ExclusiveEffect] = true;
            Notify();
            return b.Name;
        }

        // ---------------- rewarded ads ----------------
        public static void RollAdDay()
        {
            if (D.adsDate != Clock.TodayKey) { D.adsDate = Clock.TodayKey; D.rewardedCoinAdsToday = 0; }
        }
        public static int AdCoinClaimsLeft() { RollAdDay(); return Mathf.Max(0, Economy.AdCoinClaimsPerDay - D.rewardedCoinAdsToday); }
    }
}
