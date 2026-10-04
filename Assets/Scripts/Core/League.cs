using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordQuest
{
    /// <summary>
    /// Weekly league with 19 simulated rivals. No server is needed: rival scores come from a seeded formula,
    /// so they are the same every time you look. Top 3 move up, bottom 5 move down, the week resets on Monday.
    /// </summary>
    public static class League
    {
        public static readonly string[] Tiers = { "Bronze", "Silver", "Gold", "Platinum", "Diamond" };
        public static readonly Color[] TierColors =
        {
            new Color(0.80f, 0.50f, 0.25f), new Color(0.72f, 0.76f, 0.84f), new Color(1f, 0.80f, 0.20f),
            new Color(0.45f, 0.85f, 0.85f), new Color(0.45f, 0.65f, 1f)
        };
        static readonly string[] Names =
        {
            "Alex", "Sarah", "Mike", "Luna", "Ravi", "Mia", "Omar", "Zoe", "Kenji", "Nora", "Leo", "Priya", "Sam",
            "Ivy", "Diego", "Anna", "Hugo", "Lily", "Noah", "Ella", "Arjun", "Ruby", "Finn", "Maya"
        };

        public class Row { public string Name; public int Avatar; public int Points; public bool IsYou; public int Rank; }

        static PlayerData D => SaveSystem.Data;

        static List<Row> Rivals(string week, int tier, int daysIntoWeek)
        {
            var rng = new System.Random(Levels.DateSeed(week) + tier * 977);
            var list = new List<Row>();
            for (int i = 0; i < 19; i++)
            {
                float strength = 0.45f + (float)rng.NextDouble() * 1.1f + tier * 0.12f;
                int pts = 0;
                for (int day = 0; day <= daysIntoWeek; day++)
                    pts += Mathf.RoundToInt((60f + (float)rng.NextDouble() * 220f) * strength);
                list.Add(new Row { Name = Names[(i * 5 + tier * 3 + Levels.DateSeed(week)) % Names.Length], Avatar = rng.Next(12), Points = pts });
            }
            return list;
        }

        public static string DaysLeftText() { int n = Mathf.Max(1, 7 - DaysIntoWeek); return n + (n == 1 ? " day left" : " days left"); }

        public static int DaysIntoWeek => (int)(Clock.Today - DateTime.Parse(Clock.WeekKey)).TotalDays;

        public static List<Row> Standings()
        {
            var rows = Rivals(Clock.WeekKey, D.leagueTier, DaysIntoWeek);
            rows.Add(new Row { Name = D.name, Avatar = D.avatar, Points = D.leaguePoints, IsYou = true });
            rows = rows.OrderByDescending(r => r.Points).ThenBy(r => r.IsYou ? 0 : 1).ToList();
            for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;
            return rows;
        }

        public static int MyRank => Standings().First(r => r.IsYou).Rank;

        public class WeekResult { public int OldTier, NewTier, Rank, Coins; public string Text; }

        /// <summary>Call on start. If a new week began, settle the old one and return the result (or null).</summary>
        public static WeekResult SettleIfNeeded()
        {
            string week = Clock.WeekKey;
            if (string.IsNullOrEmpty(D.leagueWeek)) { D.leagueWeek = week; D.leaguePoints = 0; SaveSystem.Save(); return null; }
            if (D.leagueWeek == week) return null;

            var rows = Rivals(D.leagueWeek, D.leagueTier, 6);
            rows.Add(new Row { Name = D.name, Points = D.leaguePoints, IsYou = true });
            rows = rows.OrderByDescending(r => r.Points).ThenBy(r => r.IsYou ? 0 : 1).ToList();
            int rank = rows.FindIndex(r => r.IsYou) + 1;
            var res = new WeekResult { OldTier = D.leagueTier, Rank = rank };
            int tier = D.leagueTier;
            if (D.leaguePoints > 0)
            {
                if (rank <= 3 && tier < 4) tier++;
                else if (rank > 15 && tier > 0) tier--;
                res.Coins = rank == 1 ? 500 : rank == 2 ? 300 : rank == 3 ? 200 : rank <= 10 ? 100 : 0;
            }
            res.NewTier = tier;
            res.Text = tier > res.OldTier ? "Promoted to " + Tiers[tier] + "!" : tier < res.OldTier ? "Moved down to " + Tiers[tier] : "You stay in " + Tiers[tier];
            D.coins += res.Coins;
            D.leagueTier = tier; D.leagueWeek = week; D.leaguePoints = 0;
            SaveSystem.Save();
            return res;
        }

        /// <summary>Daily quest leaderboard: fixed rivals for today plus you.</summary>
        public static List<Row> DailyBoard()
        {
            var rng = new System.Random(Levels.DateSeed(Clock.TodayKey));
            var list = new List<Row>();
            for (int i = 0; i < 24; i++)
                list.Add(new Row { Name = Names[i], Avatar = rng.Next(12), Points = 1800 + rng.Next(0, 2600) });
            bool done = D.dailyQuestDoneDate == Clock.TodayKey;
            list.Add(new Row { Name = D.name, Avatar = D.avatar, Points = done ? D.dailyQuestScore : 0, IsYou = true });
            list = list.OrderByDescending(r => r.Points).ToList();
            for (int i = 0; i < list.Count; i++) list[i].Rank = i + 1;
            return list;
        }
    }
}
