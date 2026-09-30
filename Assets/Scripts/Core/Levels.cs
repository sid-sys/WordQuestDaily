using System;
using UnityEngine;

namespace WordQuest
{
    public enum Difficulty { Beginner, Easy, Medium, Hard, Expert }

    /// <summary>Everything the puzzle generator needs to build one level.</summary>
    public class LevelSpec
    {
        public int Index;               // 1-based level number, 0 for the daily quest
        public Difficulty Diff;
        public int Cols, Rows;
        public int WordCount;
        public int MinLen, MaxLen;
        public Vector2Int[] Dirs;
        public int Category;
        public int Seed;
        public bool IsDaily;
        public string DailyDate;
        public int Overlap;             // 2 = high, 1 = medium, 0 = low
        public string Title => IsDaily ? "DAILY QUEST" : "LEVEL " + Index;
    }

    /// <summary>The level plan: Beginner 6x7 up to Expert 10x10+, no timer.</summary>
    public static class Levels
    {
        public const int Total = 200;
        public const int PerChapter = 25;

        public static readonly Vector2Int E = new Vector2Int(1, 0), S = new Vector2Int(0, 1), SE = new Vector2Int(1, 1),
            NE = new Vector2Int(1, -1), W = new Vector2Int(-1, 0), N = new Vector2Int(0, -1),
            SW = new Vector2Int(-1, 1), NW = new Vector2Int(-1, -1);

        static readonly string[] ChapterNames =
        {
            "Getting Started", "Word Explorer", "Word Hunter", "Word Master",
            "Expert I", "Expert II", "Expert III", "Grand Master"
        };

        public static int Chapter(int level) => Mathf.Clamp((level - 1) / PerChapter, 0, ChapterNames.Length - 1);
        public static string ChapterName(int level) => ChapterNames[Chapter(level)];
        public static int ChapterCount => Total / PerChapter;

        public static Difficulty DiffOf(int level)
        {
            if (level <= 25) return Difficulty.Beginner;
            if (level <= 50) return Difficulty.Easy;
            if (level <= 75) return Difficulty.Medium;
            if (level <= 100) return Difficulty.Hard;
            return Difficulty.Expert;
        }

        public static string DiffName(Difficulty d) => d.ToString();

        public static Color DiffColor(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner: return new Color(0.30f, 0.78f, 0.36f);
                case Difficulty.Easy: return new Color(0.20f, 0.72f, 0.62f);
                case Difficulty.Medium: return new Color(0.98f, 0.76f, 0.16f);
                case Difficulty.Hard: return new Color(0.98f, 0.50f, 0.15f);
                default: return new Color(0.90f, 0.25f, 0.30f);
            }
        }

        static int Lerp(int a, int b, float t) => Mathf.RoundToInt(Mathf.Lerp(a, b, Mathf.Clamp01(t)));

        public static LevelSpec Get(int level)
        {
            level = Mathf.Clamp(level, 1, Total);
            var d = DiffOf(level);
            var s = new LevelSpec { Index = level, Diff = d, Seed = level * 7919 + 13 };
            float t;
            switch (d)
            {
                case Difficulty.Beginner:
                    t = (level - 1) / 24f;
                    s.Cols = 6; s.Rows = 7;
                    s.WordCount = Lerp(4, 6, t); s.MinLen = 3; s.MaxLen = Lerp(4, 5, t);
                    s.Dirs = new[] { E, S }; s.Overlap = 2; break;
                case Difficulty.Easy:
                    t = (level - 26) / 24f;
                    s.Cols = 7; s.Rows = 7;
                    s.WordCount = Lerp(5, 7, t); s.MinLen = 3; s.MaxLen = Lerp(5, 6, t);
                    s.Dirs = level < 40 ? new[] { E, S, SE } : new[] { E, S, SE, W, N }; s.Overlap = 2; break;
                case Difficulty.Medium:
                    t = (level - 51) / 24f;
                    s.Cols = 8; s.Rows = 8;
                    s.WordCount = Lerp(6, 9, t); s.MinLen = 4; s.MaxLen = Lerp(6, 7, t);
                    s.Dirs = new[] { E, S, SE, NE, W, N }; s.Overlap = 1; break;
                case Difficulty.Hard:
                    t = (level - 76) / 24f;
                    s.Cols = 9; s.Rows = 9;
                    s.WordCount = Lerp(8, 11, t); s.MinLen = 4; s.MaxLen = Lerp(7, 8, t);
                    s.Dirs = new[] { E, S, SE, NE, W, N, SW, NW }; s.Overlap = 1; break;
                default:
                    t = (level - 101) / 99f;
                    int size = level <= 150 ? 10 : 11;
                    s.Cols = size; s.Rows = size;
                    s.WordCount = Lerp(10, 15, t); s.MinLen = 5; s.MaxLen = Lerp(8, 10, t);
                    s.Dirs = new[] { E, S, SE, NE, W, N, SW, NW }; s.Overlap = 0; break;
            }
            s.Category = (level - 1) % WordBank.Count;
            return s;
        }

        // ---- Daily quest: everyone gets the same puzzle for the same date ----
        public static int DateSeed(string date)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in date) h = h * 31 + c;
                return Mathf.Abs(h);
            }
        }

        public static string DailyTheme(DateTime d)
        {
            switch (d.DayOfWeek)
            {
                case DayOfWeek.Monday: return "Animals";
                case DayOfWeek.Tuesday: return "Food";
                case DayOfWeek.Wednesday: return "Places";
                case DayOfWeek.Thursday: return "Movies";
                case DayOfWeek.Friday: return "Random";
                case DayOfWeek.Saturday: return "Challenge";
                default: return "Championship";
            }
        }

        public static LevelSpec Daily(DateTime date)
        {
            string key = date.ToString("yyyy-MM-dd");
            int seed = DateSeed(key);
            int cat;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday: cat = 0; break;
                case DayOfWeek.Tuesday: cat = 1; break;
                case DayOfWeek.Wednesday: cat = 2; break;
                case DayOfWeek.Thursday: cat = 6; break;
                default: cat = seed % WordBank.Count; break;
            }
            int day = (int)date.DayOfWeek; // Sunday = 0
            LevelSpec s;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday: s = Get(30); break;         // easy
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday: s = Get(60); break;        // medium
                case DayOfWeek.Friday:
                case DayOfWeek.Saturday: s = Get(90); break;        // hard
                default: s = Get(130); break;                       // Sunday: championship
            }
            s.Index = 0; s.IsDaily = true; s.DailyDate = key; s.Seed = seed; s.Category = cat;
            return s;
        }
    }
}
