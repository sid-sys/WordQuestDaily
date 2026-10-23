using UnityEngine;

namespace WordQuest
{
    public enum PowerUp { Shuffle, Hint, Letter, Finder, Word }   // only Word is used: it is "the Hint" (finds a whole word)

    /// <summary>
    /// The game has no coins. The only currency is the Hint (it finds a whole word for the player).
    /// Faucets: start 4, +1 per first-time level (+2 every 5th level, +3 every 10th, +1 more for 3 stars), daily quest, daily reward,
    /// rewarded ads (2 per ad, 5 a day), missions, streaks, collections, achievements, weekly league. Sink: using a hint.
    /// </summary>
    public static class PowerUps
    {
        public const int HintIndex = 4;
        public static readonly string[] Names = { "Shuffle", "Hint", "Reveal Letter", "Word Finder", "Hint" };
        public static readonly string[] Info = { "", "", "", "", "Finds a whole word for you" };
        public static readonly string[] Art = { "pu_shuffle", "pu_hint", "pu_letter", "pu_finder", "pu_hint" };   // the Hint is a magnifying glass
        public static readonly PowerUp[] All = { PowerUp.Word };

        public static int Count(PlayerData d, PowerUp p) => d.powers[(int)p];
        public static void Add(PlayerData d, PowerUp p, int n = 1) { d.powers[(int)p] += n; }
        public static bool Use(PlayerData d, PowerUp p)
        {
            if (d.powers[(int)p] <= 0) return false;
            d.powers[(int)p]--; return true;
        }
    }

    /// <summary>
    /// Real-money hint packs. Fixed contents. Product ids stay the same as in Play Console (the names inside the console may be updated).
    /// </summary>
    public class Bundle
    {
        public string Id, Name, Ribbon, Art, Strip;
        public int Hints;
        public int ExclusiveEffect = -1;             // unlocks a word effect
        public string TargetPrice;
    }

    public static class Bundles
    {
        public static readonly Bundle[] All =
        {
            new Bundle { Id = "bundle_starter", Name = "Starter Pack", Ribbon = "SPECIAL OFFER", Art = "pu_hint", Strip = "btn_purple", Hints = 10, TargetPrice = "Rs 99" },
            new Bundle { Id = "bundle_player", Name = "Player Pack", Art = "pu_hint", Strip = "btn_yellow", Hints = 35, TargetPrice = "Rs 299" },
            new Bundle { Id = "bundle_power", Name = "Power Pack", Ribbon = "BEST VALUE", Art = "pu_hint", Strip = "btn_blue", Hints = 80, TargetPrice = "Rs 599" },
            new Bundle { Id = "bundle_mega", Name = "Mega Pack", Art = "pu_hint", Strip = "btn_pink", Hints = 150, ExclusiveEffect = 4, TargetPrice = "Rs 999" },
        };
        public const string AdFreePlus = "remove_ads_plus";     // remove ads + a small hint kit (non-consumable, kit given once)
        public static readonly Bundle AdFreeKit = new Bundle { Id = AdFreePlus, Name = "Ad-Free Plus", Hints = 10, TargetPrice = "Rs 399" };
        public static Bundle Find(string id) { foreach (var b in All) if (b.Id == id) return b; return id == AdFreePlus ? AdFreeKit : null; }
    }

    public static class Economy
    {
        // ---- hints for finishing a level ----
        public const int StartHints = 4;
        /// <summary>First clear only (replays pay nothing). 1 hint, 2 on every 5th level, 3 on every 10th, +1 for three stars.</summary>
        public static int LevelHints(int levelsDone, int stars) => (levelsDone % 10 == 0 ? 3 : levelsDone % 5 == 0 ? 2 : 1) + (stars >= 3 ? 1 : 0);
        public const int DailyQuestHints = 2;           // +1 for three stars
        public const int PerfectDayHints = 2;
        public static readonly int[] CollectionStepHints = { 1, 1, 2, 3 };
        public static int LeagueHints(int rank) => rank == 1 ? 5 : rank == 2 ? 3 : rank == 3 ? 2 : rank <= 10 ? 1 : 0;

        // ---- XP ----
        public static int XpForLevel(int level) => 100 + 50 * (level - 1);     // XP needed to go from `level` to the next
        public static int XpForFinish(int stars) => 60 + 20 * stars;

        // ---- rewarded ads ----
        public const int AdHints = 2;
        public const int AdClaimsPerDay = 5;

        // ---- daily reward, 7 day cycle, shown once per calendar day ----
        public struct Reward { public int Hints; public string Label; }
        // a gift only on day 3 and day 7 (other days: nothing, the cycle just moves on)
        public static readonly Reward[] Daily =
        {
            new Reward { Hints = 0, Label = "Keep playing" },
            new Reward { Hints = 0, Label = "Keep playing" },
            new Reward { Hints = 1, Label = "1 Hint" },
            new Reward { Hints = 0, Label = "Keep playing" },
            new Reward { Hints = 0, Label = "Keep playing" },
            new Reward { Hints = 0, Label = "Keep playing" },
            new Reward { Hints = 3, Label = "3 Hints" },
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
