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
            "Shows the first letter of one word",
            "Shows one letter inside a word",
            "Marks where every word starts",
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
        /// <summary>Free and coin-bought power-ups stop at the inventory cap. Paid bundles (`paid`) may go over it.</summary>
        public static bool Add(PlayerData d, PowerUp p, int n = 1, bool paid = false)
        {
            if (paid) { d.powers[(int)p] += n; return true; }
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

    /// <summary>
    /// Real-money bundles. Fixed contents (never "give me any amount of coins"), and the biggest one is only about 20 levels of income,
    /// so nobody can buy their way through the game. Prices below are the targets for Play Console; the shop shows the store's real price.
    /// </summary>
    public class Bundle
    {
        public string Id, Name, Ribbon, Art, Strip;
        public int Coins; public int[] Powers;       // shuffle, hint, letter, finder, word
        public int ExclusiveEffect = -1;             // unlocks a word effect
        public string TargetPrice;
    }

    public static class Bundles
    {
        public static readonly Bundle[] All =
        {
            new Bundle { Id = "bundle_starter", Name = "Starter Pack", Ribbon = "SPECIAL OFFER", Art = "coin_stack", Strip = "btn_purple", Coins = 500, Powers = new[] { 2, 1, 1, 0, 0 }, TargetPrice = "Rs 99" },
            new Bundle { Id = "bundle_player", Name = "Player Pack", Art = "pack_pile", Strip = "btn_yellow", Coins = 1500, Powers = new[] { 4, 3, 2, 0, 0 }, TargetPrice = "Rs 299" },
            new Bundle { Id = "bundle_power", Name = "Power Pack", Ribbon = "BEST VALUE", Art = "pack_pile", Strip = "btn_blue", Coins = 3000, Powers = new[] { 8, 5, 3, 2, 0 }, TargetPrice = "Rs 599" },
            new Bundle { Id = "bundle_mega", Name = "Mega Pack", Art = "pack_heap", Strip = "btn_pink", Coins = 5000, Powers = new[] { 10, 7, 5, 3, 1 }, ExclusiveEffect = 4, TargetPrice = "Rs 999" },
        };
        public const string AdFreePlus = "remove_ads_plus";     // remove ads + a small starter kit (non-consumable, kit given once)
        public static readonly Bundle AdFreeKit = new Bundle { Id = AdFreePlus, Name = "Ad-Free Plus", Coins = 1000, Powers = new[] { 2, 2, 1, 1, 0 }, TargetPrice = "Rs 399" };
        public static Bundle Find(string id) { foreach (var b in All) if (b.Id == id) return b; return id == AdFreePlus ? AdFreeKit : null; }
    }

    public static class Economy
    {
        // ---- coins for finishing a level ----
        // Level coins = fixed reward x target win rate of that difficulty (rounded to clean numbers).
        //   Beginner 100 x 90% = 90   Easy 150 x 80% = 120   Medium 225 x 70% = 160   Hard 350 x 55% = 195   Expert 500 x 40% = 200
        public static readonly int[] FixedReward = { 100, 150, 225, 350, 500 };
        public static readonly float[] TargetWinRate = { 0.90f, 0.80f, 0.70f, 0.55f, 0.40f };
        public static int BaseCoins(Difficulty d)
        {
            int i = (int)d;
            float raw = FixedReward[i] * TargetWinRate[i];
            return Mathf.FloorToInt(raw / 5f + 0.5f) * 5;
        }
        public static int StarBonus(int stars) => stars >= 3 ? 40 : stars == 2 ? 20 : 0;
        public const float ReplayFactor = 0.25f;     // replaying a finished level pays 25%, so it cannot be farmed
        public const int MilestoneEvery = 5;         // every 5 new levels
        public const int MilestoneCoins = 150;

        // ---- XP ----
        public static int XpForLevel(int level) => 100 + 50 * (level - 1);     // XP needed to go from `level` to the next
        public static int XpForFinish(int stars) => 60 + 20 * stars;
        public const int LevelUpCoins = 100;

        // ---- rewarded ads ----
        public const int AdCoins = 100;
        public const int AdCoinClaimsPerDay = 3;

        // ---- daily reward, 7 day cycle, shown once per calendar day ----
        public struct Reward { public int Coins; public int Power; public int PowerCount; public string Label; }
        public static readonly Reward[] Daily =
        {
            new Reward { Coins = 100, Power = -1, Label = "100 coins" },
            new Reward { Coins = 0, Power = (int)PowerUp.Shuffle, PowerCount = 1, Label = "1 Shuffle" },
            new Reward { Coins = 150, Power = -1, Label = "150 coins" },
            new Reward { Coins = 0, Power = (int)PowerUp.Hint, PowerCount = 1, Label = "1 Hint" },
            new Reward { Coins = 200, Power = -1, Label = "200 coins" },
            new Reward { Coins = 0, Power = (int)PowerUp.Letter, PowerCount = 1, Label = "1 Reveal Letter" },
            new Reward { Coins = 300, Power = (int)PowerUp.Shuffle, PowerCount = 1, Label = "300 coins + Shuffle" },
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
