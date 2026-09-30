using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordQuest
{
    public class Placement
    {
        public string Word;
        public int X, Y;             // first letter
        public Vector2Int Dir;
        public bool IsMystery;
        public bool Found;
        public int Length => Word.Length;
        public Vector2Int Cell(int i) => new Vector2Int(X + Dir.x * i, Y + Dir.y * i);
        public Vector2Int End => Cell(Word.Length - 1);
    }

    public class Puzzle
    {
        public int Cols, Rows;
        public char[] Letters;
        public List<Placement> Words = new List<Placement>();   // list shown to the player
        public Placement Mystery;                                 // hidden bonus word
        public int CategoryIndex;

        public char At(int x, int y) => Letters[y * Cols + x];
        public IEnumerable<Placement> AllPlacements()
        {
            foreach (var w in Words) yield return w;
            if (Mystery != null) yield return Mystery;
        }
    }

    /// <summary>Builds seeded puzzles: the same seed always gives the same board.</summary>
    public static class PuzzleGenerator
    {
        const string Filler = "EEEEEEEEEEEEAAAAAAAAARRRRRRIIIIIIIIOOOOOOOTTTTTTNNNNNNSSSSSSLLLLCCCCUUUDDDPPPMMMHHHGGBBFFYYWKVXZJQ";

        public static Puzzle Build(LevelSpec spec)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var rng = new System.Random(spec.Seed + attempt * 101);
                var p = TryBuild(spec, rng, attempt >= 10 ? Mathf.Max(3, spec.WordCount - (attempt - 9) / 3) : spec.WordCount);
                if (p != null) return p;
            }
            throw new Exception("Could not build puzzle for seed " + spec.Seed);
        }

        static Puzzle TryBuild(LevelSpec spec, System.Random rng, int wordCount)
        {
            var cat = WordBank.Get(spec.Category);
            int maxDim = Mathf.Max(spec.Cols, spec.Rows);
            int maxLen = Mathf.Min(spec.MaxLen, maxDim);
            var pool = cat.Words.Where(w => w.Length >= spec.MinLen && w.Length <= maxLen).ToList();
            var mysteryPool = cat.Words.Where(w => w.Length >= Mathf.Max(5, Mathf.Min(maxLen, 6)) && w.Length <= Mathf.Min(maxDim, 10)).ToList();
            Shuffle(pool, rng); Shuffle(mysteryPool, rng);
            if (pool.Count < wordCount || mysteryPool.Count == 0) return null;

            string mystery = mysteryPool[0];
            var chosen = new List<string>();
            foreach (var w in pool)
            {
                if (chosen.Count >= wordCount) break;
                if (w == mystery || Overlaps(w, chosen) || Overlaps(w, mystery)) continue;
                chosen.Add(w);
            }
            if (chosen.Count < wordCount) return null;

            var puzzle = new Puzzle { Cols = spec.Cols, Rows = spec.Rows, Letters = new char[spec.Cols * spec.Rows], CategoryIndex = spec.Category };
            var free = new bool[puzzle.Letters.Length];
            for (int i = 0; i < free.Length; i++) free[i] = true;

            var order = chosen.OrderByDescending(w => w.Length).ToList();
            order.Insert(Mathf.Min(1, order.Count), mystery); // place the mystery early so it always fits
            foreach (var w in order)
            {
                var pl = Place(puzzle, w, spec, rng, free, null);
                if (pl == null) return null;
                pl.IsMystery = w == mystery;
                if (pl.IsMystery) puzzle.Mystery = pl; else puzzle.Words.Add(pl);
            }
            FillRest(puzzle, rng);
            if (HasStrays(puzzle)) return null;
            puzzle.Words = puzzle.Words.OrderBy(w => w.Word).ToList();
            return puzzle;
        }

        static bool Overlaps(string a, List<string> others) => others.Any(o => Overlaps(a, o));
        static bool Overlaps(string a, string b) => a.Contains(b) || b.Contains(a) || Reverse(a).Contains(b) || b.Contains(Reverse(a));
        static string Reverse(string s) { var c = s.ToCharArray(); Array.Reverse(c); return new string(c); }

        /// <summary>Tries many random spots and picks the best for the wanted overlap. Cells in `blocked` are never used.</summary>
        static Placement Place(Puzzle p, string word, LevelSpec spec, System.Random rng, bool[] free, bool[] blocked)
        {
            Placement best = null; int bestScore = int.MinValue;
            for (int tries = 0; tries < 120; tries++)
            {
                var dir = spec.Dirs[rng.Next(spec.Dirs.Length)];
                int x = rng.Next(p.Cols), y = rng.Next(p.Rows);
                int ex = x + dir.x * (word.Length - 1), ey = y + dir.y * (word.Length - 1);
                if (ex < 0 || ex >= p.Cols || ey < 0 || ey >= p.Rows) continue;
                int shared = 0; bool ok = true;
                for (int i = 0; i < word.Length && ok; i++)
                {
                    int idx = (y + dir.y * i) * p.Cols + (x + dir.x * i);
                    if (blocked != null && blocked[idx]) { ok = false; break; }
                    if (free[idx]) continue;
                    if (p.Letters[idx] == word[i]) shared++; else ok = false;
                }
                if (!ok || shared == word.Length) continue;
                int score = spec.Overlap == 2 ? shared * 3 : spec.Overlap == 1 ? -Mathf.Abs(shared - 1) : -shared * 3;
                score += rng.Next(3);
                if (score > bestScore) { bestScore = score; best = new Placement { Word = word, X = x, Y = y, Dir = dir }; }
                if (tries > 40 && best != null) break;
            }
            if (best == null) return null;
            for (int i = 0; i < word.Length; i++)
            {
                var c = best.Cell(i);
                int idx = c.y * p.Cols + c.x;
                p.Letters[idx] = word[i]; free[idx] = false;
            }
            return best;
        }

        static void FillRest(Puzzle p, System.Random rng)
        {
            for (int i = 0; i < p.Letters.Length; i++)
                if (p.Letters[i] == '\0') p.Letters[i] = Filler[rng.Next(Filler.Length)];
        }

        /// <summary>True if a listed word also appears somewhere it was not placed (would make a puzzle unfair).</summary>
        static bool HasStrays(Puzzle p)
        {
            var dirs = new[] { Levels.E, Levels.S, Levels.SE, Levels.NE, Levels.W, Levels.N, Levels.SW, Levels.NW };
            foreach (var w in p.AllPlacements())
            {
                int found = 0;
                for (int y = 0; y < p.Rows; y++)
                    for (int x = 0; x < p.Cols; x++)
                        foreach (var d in dirs)
                            if (Matches(p, w.Word, x, y, d)) found++;
                if (found > 1) return true;
            }
            return false;
        }

        static bool Matches(Puzzle p, string word, int x, int y, Vector2Int d)
        {
            int ex = x + d.x * (word.Length - 1), ey = y + d.y * (word.Length - 1);
            if (ex < 0 || ex >= p.Cols || ey < 0 || ey >= p.Rows) return false;
            for (int i = 0; i < word.Length; i++)
                if (p.At(x + d.x * i, y + d.y * i) != word[i]) return false;
            return true;
        }

        /// <summary>Shuffle power-up: found words stay where they are; every word still to find moves somewhere new.</summary>
        public static bool Reshuffle(Puzzle p, LevelSpec spec, int seed)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var rng = new System.Random(seed + attempt * 37);
                var letters = new char[p.Letters.Length];
                var free = new bool[p.Letters.Length];
                var blocked = new bool[p.Letters.Length];
                for (int i = 0; i < free.Length; i++) free[i] = true;
                foreach (var w in p.AllPlacements().Where(w => w.Found))
                    for (int i = 0; i < w.Length; i++)
                    {
                        var c = w.Cell(i); int idx = c.y * p.Cols + c.x;
                        letters[idx] = w.Word[i]; free[idx] = false;
                    }
                var temp = new Puzzle { Cols = p.Cols, Rows = p.Rows, Letters = letters };
                var moved = new List<(Placement old, Placement fresh)>();
                bool ok = true;
                foreach (var w in p.AllPlacements().Where(w => !w.Found).OrderByDescending(w => w.Length))
                {
                    var pl = Place(temp, w.Word, spec, rng, free, null);
                    if (pl == null) { ok = false; break; }
                    moved.Add((w, pl));
                }
                if (!ok) continue;
                FillRest(temp, rng);
                p.Letters = temp.Letters;
                foreach (var (old, fresh) in moved) { old.X = fresh.X; old.Y = fresh.Y; old.Dir = fresh.Dir; }
                return true;
            }
            return false;
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
