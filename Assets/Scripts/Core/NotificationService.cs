using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace WordQuest
{
    /// <summary>
    /// Local push notifications (no server needed). When the player leaves the app a small plan is scheduled:
    /// at most ONE notification per calendar day, chosen by priority, never more than a handful ahead:
    ///   1 streak about to end (evening)  2 daily puzzle (morning)  3 daily reward  4 league week  5 unfinished level
    ///   6 collection nearly complete     7 coming-back messages after 3 / 7 / 14 / 30 days away
    /// Opening the app cancels everything and the plan is made again next time.
    /// </summary>
    public static class NotificationService
    {
        public class Plan { public DateTime When; public int Priority; public string Title, Text, Tag; }

        const string Channel = "wq_main";
        const string Title = "Word Quest Daily";

        static string Pick(int seed, params string[] options) => options[Mathf.Abs(seed) % options.Length];

        /// <summary>Builds the schedule for the next days. Pure logic: easy to test in the Editor.</summary>
        public static List<Plan> BuildPlan(PlayerData d, DateTime now)
        {
            var all = new List<Plan>();
            int streak = Progress.EffectiveStreak();
            bool dailyDone = d.dailyQuestDoneDate == Clock.KeyOf(now);
            bool rewardWaiting = d.lastDailyRewardClaimDate != Clock.KeyOf(now);
            int seed = now.DayOfYear;

            for (int i = 0; i < 6; i++)
            {
                var day = now.Date.AddDays(i);
                var morning = day.AddHours(9);
                var evening = day.AddHours(19);
                bool todayDone = i == 0 && dailyDone;

                // 2 daily puzzle, every morning
                if (i <= 3 && morning > now.AddMinutes(30) && !(i == 0 && dailyDone))
                {
                    string text = Pick(seed + i,
                        "A new Word Quest is ready! Can you find all the hidden words?",
                        "Today's puzzle is waiting. Your streak starts with one word.",
                        "A fresh puzzle has arrived. No timer, just find the words.",
                        "New words, new puzzle, new chance to beat your score.");
                    if (rewardWaiting && i == 0) text = "Your daily reward is ready, and today's puzzle is waiting.";
                    all.Add(new Plan { When = morning, Priority = 2, Title = Title, Text = text, Tag = "daily" });
                }
                // 1 streak about to end (evening): only if the player has a streak and has not played the daily yet
                if (i <= 1 && streak > 0 && evening > now.AddMinutes(30) && !(i == 0 && todayDone))
                {
                    string text = Pick(seed + i,
                        $"Your {streak}-day streak is waiting for you.",
                        $"One puzzle keeps your {streak}-day streak alive.",
                        $"{streak} days in a row. Today makes it {streak + 1}.",
                        "Before the day ends, keep your streak alive.");
                    if (rewardWaiting && i == 0) text = $"Your daily reward is ready, and your {streak}-day streak is waiting.";
                    all.Add(new Plan { When = evening, Priority = 1, Title = Title, Text = text, Tag = "streak" });
                }
                // 4 weekly league: Monday morning starts, Sunday evening ends
                if (day.DayOfWeek == DayOfWeek.Monday && morning > now.AddMinutes(30))
                    all.Add(new Plan { When = morning.AddMinutes(10), Priority = 2, Title = Title, Text = Pick(seed, "A new league week has begun. Time to climb!", "New week, fresh leaderboard. How high can you go?"), Tag = "league" });
                if (day.DayOfWeek == DayOfWeek.Sunday && evening > now.AddMinutes(30) && d.leaguePoints > 0)
                    all.Add(new Plan { When = evening.AddMinutes(30), Priority = 1, Title = Title, Text = $"Your league ends tonight. You are currently #{League.MyRank}.", Tag = "leagueend" });
            }

            // 5 unfinished level: once, a few hours after leaving (not at night)
            if (d.unfinishedLevel > 0)
            {
                var t = now.AddHours(4);
                if (t.Hour >= 22) t = t.Date.AddHours(20); else if (t.Hour < 8) t = t.Date.AddHours(9);
                all.Add(new Plan { When = t, Priority = 5, Title = Title, Text = Pick(seed, $"Level {d.unfinishedLevel} is waiting for you.", $"You were close! Finish Level {d.unfinishedLevel}.", "One puzzle, a few words, and you are done."), Tag = "unfinished" });
            }

            // 3 free power-up milestone
            int toNext = LevelsToNextPowerUp(d);
            if (toNext > 0 && toNext <= 3)
                all.Add(new Plan { When = now.Date.AddDays(1).AddHours(12), Priority = 6, Title = Title, Text = $"Complete {toNext} more level{(toNext == 1 ? "" : "s")} to earn a free power-up.", Tag = "milestone" });

            // 6 collection nearly complete
            for (int c = 0; c < WordBank.Count; c++)
            {
                int left = Progress.TotalIn(c) - Progress.Found(c);
                if (left > 0 && left <= 3)
                {
                    all.Add(new Plan { When = now.Date.AddDays(2).AddHours(17), Priority = 6, Title = Title, Text = $"You are only {left} word{(left == 1 ? "" : "s")} away from completing {WordBank.Get(c).Name}!", Tag = "collection" });
                    break;
                }
            }

            // 7 coming-back ladder (a different message the longer the player stays away)
            string who = streak > 0 ? $"Your {streak}-day streak is waiting for a comeback." : "Your next puzzle is ready.";
            AddAway(all, now, 3, who);
            AddAway(all, now, 7, "It has been a few days. There are new words waiting to be discovered.");
            AddAway(all, now, 14, "We have a few rewards waiting for your return.");
            AddAway(all, now, 30, "A lot of words have appeared since you left. Your next puzzle is ready.");

            // at most one per calendar day: keep the highest priority (lowest number), then the earliest
            var picked = all.GroupBy(p => p.When.Date)
                            .Select(g => g.OrderBy(p => p.Priority).ThenBy(p => p.When).First())
                            .OrderBy(p => p.When).Take(10).ToList();
            return picked;
        }

        static void AddAway(List<Plan> all, DateTime now, int days, string text) =>
            all.Add(new Plan { When = now.Date.AddDays(days).AddHours(10), Priority = 7, Title = Title, Text = text, Tag = "away" + days });

        static int LevelsToNextPowerUp(PlayerData d)
        {
            int every = PowerUps.FreeEvery[(int)PowerUp.Word];
            return every - (d.levelsCompleted % every);
        }

        // =============== Android glue ===============
#if UNITY_ANDROID && !UNITY_EDITOR
        static bool channelMade;
        static void EnsureChannel()
        {
            if (channelMade) return;
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = Channel, Name = "Word Quest reminders", Importance = Importance.Default,
                Description = "Daily puzzle, streak and reward reminders"
            });
            channelMade = true;
        }

        /// <summary>Shows the Android 13+ permission dialog (once). Older Android needs no permission.</summary>
        public static void AskPermission()
        {
            var d = SaveSystem.Data; if (d == null || d.notifAsked) return;
            d.notifAsked = true; SaveSystem.Save();
            var req = new PermissionRequest();
            GameApp.I.StartCoroutine(WaitFor(req));
        }
        static System.Collections.IEnumerator WaitFor(PermissionRequest r) { while (r.Status == PermissionStatus.RequestPending) yield return null; }

        public static void Schedule()
        {
            var d = SaveSystem.Data; if (d == null) return;
            CancelAll();
            if (!d.notifOn) return;
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed) return;
            EnsureChannel();
            foreach (var p in BuildPlan(d, DateTime.Now))
            {
                var n = new AndroidNotification { Title = p.Title, Text = p.Text, FireTime = p.When, SmallIcon = "icon_small", LargeIcon = "icon_large", ShouldAutoCancel = true };
                AndroidNotificationCenter.SendNotification(n, Channel);
            }
            if (Debug.isDebugBuild)   // test builds only: one extra notification 25 seconds after the app is left
                AndroidNotificationCenter.SendNotification(new AndroidNotification { Title = Title, Text = "Test: notifications work!", FireTime = DateTime.Now.AddSeconds(25), SmallIcon = "icon_small", LargeIcon = "icon_large", ShouldAutoCancel = true }, Channel);
        }

        public static void CancelAll()
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }
#else
        public static void AskPermission() { if (SaveSystem.Data != null) SaveSystem.Data.notifAsked = true; }
        public static void Schedule() { }
        public static void CancelAll() { }
#endif
    }
}
