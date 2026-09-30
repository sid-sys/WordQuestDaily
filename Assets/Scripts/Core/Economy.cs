using UnityEngine;

namespace WordQuest
{
    public enum PowerUp { Shuffle, Hint, Letter, Finder, Word }

    /// <summary>
    /// Power-up numbers. Sweet spot: an average level pays about 125 coins and a shuffle costs 150,
    /// so a player earns about one shuffle every 1.2 levels. Stock is capped so nobody can bulldoze every puzzle.
    /// </summary>
    public static class PowerUps
    {
        public static readonly string[] Names = { "Shuffle", "Hint", "Reveal Letter", "Word Finder", "Reveal Word" };
        public static readonly string[] Info =
        {
            "Moves the words you have not found",
            "Shows the first letter of a word",
            "Shows one letter of a word",
            "Shows where a word is",
            "Finds a whole word for you"
        };
        public static readonly int[] Cost = { 150, 200, 250, 350, 500 };
        public static readonly int[] Max = { 5, 5, 3, 3, 2 };
        public static readonly int[] FreeEvery = { 5, 10, 15, 20, 25 };   // completed levels per free power-up
        public static readonly int[] UnlockLevel = { 3, 1, 7, 12, 18 };   // level whose tutorial unlocks each power-up
        public static readonly string[] Art = { "pu_shuffle", "pu_hint", "pu_letter", "pu_finder", "pu_word" };
        public static readonly PowerUp[] All = { PowerUp.Shuffle, PowerUp.Hint, PowerUp.Letter, PowerUp.Finder, PowerUp.Word };

        public static int Count(PlayerData d, PowerUp p) => d.powers[(int)p];
        public static bool IsFull(PlayerData d, PowerUp p) => d.powers[(int)p] >= Max[(int)p];
        public static bool Add(PlayerData d, PowerUp p, int n = 1)
        {
            if (IsFull(d, p)) return false;
            d.powers[(int)p] = Mathf.Min(Max[(int)p], d.powers[(int)p] + n);
            return true;
        }
        public static bool Use(PlayerData d, PowerUp p)
        {
            if (d.powers[(int)p] <= 0) return false;
            d.powers[(int)p]--; return true;
        }
    }

    public static class Economy
    {
        // ---- coins for finishing a level ----
        public static int BaseCoins(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner: return 75;
                case Difficulty.Easy: return 100;
                case Difficulty.Medium: return 125;
                case Difficulty.Hard: return 150;
                default: return 200;
            }
        }
        public static int StarBonus(int stars) => stars >= 3 ? 50 : stars == 2 ? 25 : 0;
        public const float ReplayFactor = 0.25f;     // replaying a finished level pays 25%, so it cannot be farmed
        public const int MilestoneEvery = 5;         // every 5 new levels
        public const int MilestoneCoins = 250;

        // ---- XP ----
        public static int XpForLevel(int level) => 100 + 50 * (level - 1);     // XP needed to go from `level` to the next
        public static int XpForFinish(int stars) => 60 + 20 * stars;
        public const int LevelUpCoins = 100;

        // ---- rewarded ads ----
        public const int AdCoins = 100;
        public const int AdCoinClaimsPerDay = 4;

        // ---- daily reward, 7 day cycle, shown once per calendar day ----
        public struct Reward { public int Coins; public int Power; public int PowerCount; public string Label; }
        public static readonly Reward[] Daily =
        {
            new Reward { Coins = 100, Power = -1, Label = "100 coins" },
            new Reward { Coins = 150, Power = -1, Label = "150 coins" },
            new Reward { Coins = 200, Power = -1, Label = "200 coins" },
            new Reward { Coins = 0, Power = (int)PowerUp.Hint, PowerCount = 1, Label = "1 Hint" },
            new Reward { Coins = 250, Power = -1, Label = "250 coins" },
            new Reward { Coins = 0, Power = (int)PowerUp.Shuffle, PowerCount = 1, Label = "1 Shuffle" },
            new Reward { Coins = 500, Power = (int)PowerUp.Letter, PowerCount = 1, Label = "500 coins + power-up" },
        };

        // ---- what unlocks at which player level ----
        public static readonly int[] ThemeLevel = { 1, 3, 6, 10 };
        public static readonly int[] EffectLevel = { 1, 4, 8, 12, 16 };
        public static readonly int[] AvatarLevel = { 1, 1, 2, 3, 5, 7, 9, 10, 12, 15, 18, 20 };
        public static readonly int[] RingLevel = { 5, 10, 20, 30 };
        public static readonly string[] ThemeNames = { "Meadow", "Ocean", "Candy", "Night" };
        public static readonly string[] EffectNames = { "Sparkle", "Fire", "Lightning", "Pop", "Rainbow" };
        public static readonly string[] AvatarNames = { "Fox", "Panda", "Cherry", "Owl", "Frog", "Lion", "Bunny", "Penguin", "Koala", "Dragon", "Robot", "Wizard" };
        public static readonly string[] RingNames = { "Bronze", "Silver", "Gold", "Diamond" };
    }
}
