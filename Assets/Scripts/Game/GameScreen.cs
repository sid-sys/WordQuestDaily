using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>Holds everything for the level that is being played.</summary>
    public class GameScreen
    {
        public LevelSpec Spec;
        public Puzzle Puzzle;
        public RectTransform Root;
        public WordGridView Grid;
        public UI.Bar LevelBar;
        public UI.Bar CollectionBar;
        public Text LevelBarText, CollectionText, BannerText;
        public RectTransform Banner;
        public readonly Dictionary<string, RectTransform> Chips = new Dictionary<string, RectTransform>();
        public RectTransform MysteryChip;
        public readonly List<Text> PowerCounts = new List<Text>();
        public readonly List<Image> PowerIcons = new List<Image>();
        public int PowerupsUsed, FoundCount, Shuffles;
        public bool MysteryFound, MysterySkipped, Finished, Busy;
        public float LastFoundTime;
        public Button SkipButton;
        public Image HintBulb;
    }

    public partial class GameApp
    {
        // =============== starting ===============
        public void StartGame(LevelSpec spec)
        {
            CloseAllPopups();
            if (game != null) EndGame();
            var puzzle = PuzzleGenerator.Build(spec);
            game = new GameScreen { Spec = spec, Puzzle = puzzle };
            body.gameObject.SetActive(false); tabBar.gameObject.SetActive(false);
            gameLayer.gameObject.SetActive(true);
            foreach (Transform c in gameLayer) Destroy(c.gameObject);
            SetBackground(SaveSystem.Data.theme);
            BuildGame();
            if (!SaveSystem.Data.tutorialDone)
            {
                SaveSystem.Data.tutorialDone = true; SaveSystem.Save();
                Toast("Swipe across letters to find the words!", Palette.Yellow);
            }
        }

        void EndGame()
        {
            if (game == null) return;
            foreach (Transform c in gameLayer) Destroy(c.gameObject);
            game = null;
        }

        public void LeaveGame(bool confirm)
        {
            if (game == null) return;
            if (confirm && !game.Finished && game.FoundCount > 0)
            {
                var p = OpenPopup("LEAVE LEVEL?", 860, 560, true);
                var t = UI.Wrapped(p.Content, "Your progress on this board will be lost.", 40, Palette.Ink, 720);
                UI.Place(t.rectTransform, 0.5f, 1, 0, -120, 720, 120);
                var stay = UI.Pill(p.Content, "btn_green", "KEEP PLAYING", 640, 100, p.Close, 44);
                UI.Place((RectTransform)stay.transform, 0.5f, 0, 0, 160, 640, 100);
                var leave = UI.Pill(p.Content, "btn_red", "LEAVE", 640, 100, () => { p.Close(); LeaveGame(false); }, 44);
                UI.Place((RectTransform)leave.transform, 0.5f, 0, 0, 40, 640, 100);
                return;
            }
            EndGame();
            ShowTab(tab);
        }

        // =============== layout ===============
        void BuildGame()
        {
            var g = game; var spec = g.Spec; var puzzle = g.Puzzle; var d = SaveSystem.Data;
            g.Root = UI.Stretch(UI.Node(gameLayer, "Level"));
            var root = g.Root;
            float rootH = safe.rect.height;

            // --- top bar ---
            var back = UI.Icon(root, "rb_back", 100, "Back"); UI.Place(back.rectTransform, 0, 1, 80, -80, 100, 100);
            UI.Click(back, () => LeaveGame(true));
            var title = UI.Label(root, spec.Title, 70, Color.white, TextAnchor.MiddleCenter, true);
            UI.Place(title.rectTransform, 0.5f, 1, 0, -70, 560, 90);
            var cat = WordBank.Get(puzzle.CategoryIndex);
            var catLabel = UI.Label(root, $"{cat.Name.ToUpper()}  -  {Levels.DiffName(spec.Diff).ToUpper()}", 34, Color.white, TextAnchor.MiddleCenter, true);
            UI.Place(catLabel.rectTransform, 0.5f, 1, 0, -130, 700, 44);
            g.HintBulb = UI.Icon(root, "rb_bulb", 100, "Bulb"); UI.Place(g.HintBulb.rectTransform, 1, 1, -80, -80, 100, 100);
            UI.Click(g.HintBulb, () => UsePower(PowerUp.Hint));

            // --- progress bars ---
            g.LevelBar = UI.ProgressBar(root, 820, 54, Palette.Green, "LevelBar");
            UI.Place(g.LevelBar.Root, 0.5f, 1, 0, -205, 820, 54);
            g.LevelBarText = UI.Label(g.LevelBar.Root, "", 32, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.LevelBarText.rectTransform);
            g.CollectionBar = UI.ProgressBar(root, 560, 34, Palette.Blue, "CollectionBar");
            UI.Place(g.CollectionBar.Root, 0.5f, 1, 0, -262, 560, 34);
            g.CollectionText = UI.Label(g.CollectionBar.Root, "", 22, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.CollectionText.rectTransform);
            var colIcon = UI.Icon(root, cat.ArtKey, 56, "CatIcon"); UI.Place(colIcon.rectTransform, 0.5f, 1, -330, -262, 56, 56);

            // --- word chips ---
            float chipsTop = 318;
            float chipsBottom = BuildChips(root, chipsTop);

            // --- board ---
            float powerH = 230;
            float availH = rootH - chipsBottom - powerH - 60, availW = 960;
            float cell = Mathf.Min(availW / puzzle.Cols, availH / puzzle.Rows, 150f);
            float bw = cell * puzzle.Cols, bh = cell * puzzle.Rows;
            var boardHolder = UI.Node(root, "Board");
            float boardCenterY = -(chipsBottom + 30 + availH / 2);
            UI.Place(boardHolder, 0.5f, 1, 0, boardCenterY, bw + 70, bh + 70);
            var frame = UI.Sliced(boardHolder, "frame_" + ThemeKey(d.theme), 140, "Frame");
            UI.Stretch(frame.rectTransform, -50, -50, -50, -50);
            frame.pixelsPerUnitMultiplier = frame.sprite != null ? frame.sprite.border.x / 70f : 1f;
            var panel = UI.Sliced(boardHolder, "card_b", 150, "Panel");
            UI.Stretch(panel.rectTransform);
            panel.color = new Color(1f, 0.98f, 0.93f, 1f);
            var gridRt = UI.Node(boardHolder, "Grid");
            UI.Place(gridRt, 0.5f, 0.5f, 0, 0, bw, bh);
            g.Grid = gridRt.gameObject.AddComponent<WordGridView>();
            g.Grid.Build(puzzle, cell);
            g.Grid.OnWordFound = OnWordFound;
            g.Grid.OnWrong = () => { };
            Fx.PopIn(boardHolder, 0.4f);

            // --- banner for mystery prompt ---
            g.Banner = UI.Node(root, "Banner");
            UI.Place(g.Banner, 0.5f, 0, 0, powerH + 50, 900, 120);
            var bi = UI.Sliced(g.Banner, "ribbon_pink", 120, "Bg"); bi.preserveAspect = false; bi.type = Image.Type.Simple; UI.Stretch(bi.rectTransform);
            g.BannerText = UI.Label(g.Banner, "", 38, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.BannerText.rectTransform, 30, 8, 190, 0);
            g.SkipButton = UI.Pill(g.Banner, "btn_grey", "SKIP", 170, 76, SkipMystery, 34);
            UI.Place((RectTransform)g.SkipButton.transform, 1, 0.5f, -120, 6, 170, 76);
            g.Banner.gameObject.SetActive(false);

            // --- power-ups ---
            BuildPowerBar(root, powerH);
            UpdateProgress(false);
        }

        float BuildChips(RectTransform root, float top)
        {
            var g = game; var words = g.Puzzle.Words;
            var holder = UI.Node(root, "Chips");
            UI.Place(holder, 0.5f, 1, 0, -top, 1000, 10);
            // measure
            float maxW = 1000, chipH = 64, gap = 12, size = words.Count > 11 ? 28 : 32;
            var items = new List<(string word, float w)>();
            foreach (var w in words) items.Add((w.Word, Mathf.Max(110, w.Word.Length * size * 0.62f + 44)));
            if (g.Puzzle.Mystery != null) items.Add(("?", Mathf.Max(110, g.Puzzle.Mystery.Word.Length * size * 0.62f + 44)));
            var rows = new List<List<(string word, float w)>> { new List<(string word, float w)>() };
            float cur = 0;
            foreach (var it in items)
            {
                if (cur + it.w > maxW && rows[rows.Count - 1].Count > 0) { rows.Add(new List<(string word, float w)>()); cur = 0; }
                rows[rows.Count - 1].Add(it); cur += it.w + gap;
            }
            for (int r = 0; r < rows.Count; r++)
            {
                float rowW = rows[r].Sum(i => i.w) + gap * (rows[r].Count - 1);
                float x = -rowW / 2;
                foreach (var it in rows[r])
                {
                    bool mystery = it.word == "?";
                    var chip = UI.Sliced(holder, "chip", chipH, "Chip_" + it.word);
                    chip.color = mystery ? new Color(0.72f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.92f);
                    UI.Place(chip.rectTransform, 0.5f, 1, x + it.w / 2, -(r * (chipH + gap) + chipH / 2), it.w, chipH);
                    string label = mystery ? new string('?', g.Puzzle.Mystery.Word.Length) : it.word;
                    var t = UI.Label(chip.transform, label, (int)size, mystery ? Color.white : Palette.Ink, TextAnchor.MiddleCenter, mystery);
                    UI.Stretch(t.rectTransform);
                    if (mystery) { g.MysteryChip = chip.rectTransform; chip.gameObject.AddComponent<Pulse>().Amount = 0.03f; }
                    else g.Chips[it.word] = chip.rectTransform;
                    x += it.w + gap;
                }
            }
            return top + rows.Count * (chipH + gap) + 6;
        }

        void BuildPowerBar(RectTransform root, float h)
        {
            var g = game; var d = SaveSystem.Data;
            var bar = UI.Node(root, "PowerBar");
            UI.Place(bar, 0.5f, 0, 0, h / 2 - 20, 1040, h);
            var bg = UI.Sliced(bar, "navbar", 190, "Bg"); UI.Stretch(bg.rectTransform, 0, 20, 0, 20);
            for (int i = 0; i < 5; i++)
            {
                int idx = i; var pu = PowerUps.All[i];
                var cell = UI.Node(bar, "Pw" + i);
                UI.Place(cell, 0, 0.5f, 104 + i * 208, 0, 200, h);
                var icon = UI.Icon(cell, PowerUps.Art[i], 112, "Icon"); UI.Place(icon.rectTransform, 0.5f, 0.5f, 0, 24, 112, 112);
                UI.Click(icon, () => UsePower(pu));
                g.PowerIcons.Add(icon);
                var badge = UI.Icon(cell, "spark_glow", 52, "Badge"); badge.color = Palette.Red; UI.Place(badge.rectTransform, 0.5f, 0.5f, 52, 76, 52, 52);
                var cnt = UI.Label(badge.transform, "", 30, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(cnt.rectTransform);
                g.PowerCounts.Add(cnt);
                var price = UI.Label(cell, PowerUps.Names[i].ToUpper(), 20, Color.white, TextAnchor.MiddleCenter, false);
                UI.Place(price.rectTransform, 0.5f, 0, 0, 52, 200, 28);
                price.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            RefreshPowerBar();
        }

        void RefreshPowerBar()
        {
            var g = game; if (g == null) return; var d = SaveSystem.Data;
            for (int i = 0; i < 5; i++)
            {
                int n = d.powers[i];
                g.PowerCounts[i].text = n > 0 ? n.ToString() : "+";
                g.PowerCounts[i].transform.parent.GetComponent<Image>().color = n > 0 ? Palette.Red : Palette.Green;
                g.PowerIcons[i].color = n > 0 ? Color.white : new Color(1, 1, 1, 0.65f);
            }
        }

        // =============== progress ===============
        void UpdateProgress(bool animate)
        {
            var g = game; if (g == null) return;
            int total = g.Puzzle.Words.Count;
            int found = g.Puzzle.Words.Count(w => w.Found);
            g.FoundCount = found + (g.MysteryFound ? 1 : 0);
            g.LevelBarText.text = $"{found} / {total} words";
            g.LevelBar.Set(found / (float)total);
            int cat = g.Puzzle.CategoryIndex;
            g.CollectionText.text = $"{Progress.Found(cat)} / {Progress.TotalIn(cat)}";
            g.CollectionBar.Set(Progress.Found(cat) / (float)Progress.TotalIn(cat));
            if (animate) Fx.Punch(g.LevelBar.Root, 0.06f, 0.3f);
        }

        // =============== a word was found ===============
        void OnWordFound(Placement p, Vector2Int a, Vector2Int b)
        {
            var g = game; if (g == null) return;
            bool isNew = Progress.Discover(g.Puzzle.CategoryIndex, p.Word);
            g.LastFoundTime = Time.unscaledTime;
            Progress.Notify();
            var grid = g.Grid;
            Vector2 endPos = grid.CellCanvas(b.x, b.y, fxLayer), midPos = (grid.CellCanvas(a.x, a.y, fxLayer) + endPos) / 2;
            float pitch = 1f + 0.06f * g.Puzzle.Words.Count(w => w.Found);
            Sfx.Play(Sfx.Kind.Found, Mathf.Min(pitch, 1.5f));
            Sfx.Buzz();
            Fx.Burst(fxLayer, endPos, grid.EffectParticle(), Color.white, 12, 380f, 0.65f, 44f);
            Fx.Burst(fxLayer, midPos, "spark_star", Palette.Yellow, 8, 300f, 0.6f, 38f);

            if (p.IsMystery)
            {
                g.MysteryFound = true;
                StartCoroutine(MysteryRoutine(p, isNew));
                return;
            }
            // chip: green + check + strike
            if (g.Chips.TryGetValue(p.Word, out var chip))
            {
                var img = chip.GetComponent<Image>(); img.color = new Color(0.62f, 0.9f, 0.66f, 0.95f);
                var t = chip.GetComponentInChildren<Text>(); t.color = new Color(0.25f, 0.45f, 0.3f);
                var strike = UI.Solid(chip, new Color(0.2f, 0.4f, 0.25f, 0.9f), "Strike");
                UI.Place(strike.rectTransform, 0.5f, 0.5f, 0, 0, 0, 5);
                float full = chip.sizeDelta.x - 40;
                Fx.Tween(0.3f, k => { if (strike != null) strike.rectTransform.sizeDelta = new Vector2(full * k, 5); });
                Fx.Punch(chip, 0.18f, 0.35f);
                Vector2 chipPos = fxLayer.InverseTransformPoint(chip.position);
                Fx.Burst(fxLayer, chipPos, "spark_star", Palette.Green, 5, 220f, 0.5f, 30f);
            }
            Fx.FloatText(fxLayer, midPos, isNew ? "NEW WORD!" : "+1 WORD", isNew ? Palette.Yellow : Color.white, 48);
            UpdateProgress(true);
            CheckFinish();
        }

        void CheckFinish()
        {
            var g = game; if (g == null || g.Finished) return;
            bool normalDone = g.Puzzle.Words.All(w => w.Found);
            if (!normalDone) return;
            if (g.Puzzle.Mystery == null || g.MysteryFound || g.MysterySkipped) { StartCoroutine(FinishRoutine()); return; }
            // all listed words done: invite the player to find the mystery word
            g.Banner.gameObject.SetActive(true);
            g.BannerText.text = $"FIND THE MYSTERY WORD!  ({g.Puzzle.Mystery.Word.Length} letters)";
            Fx.PopIn(g.Banner, 0.4f);
            Sfx.Play(Sfx.Kind.Mystery, 1f, 0.6f);
            if (g.MysteryChip != null) Fx.Punch(g.MysteryChip, 0.3f, 0.6f);
        }

        void SkipMystery()
        {
            var g = game; if (g == null || g.Finished) return;
            g.MysterySkipped = true; g.Banner.gameObject.SetActive(false);
            CheckFinish();
        }

        // =============== mystery word reveal ===============
        IEnumerator MysteryRoutine(Placement p, bool isNew)
        {
            var g = game; if (g == null) yield break;
            g.Busy = true; g.Grid.InputEnabled = false;
            g.Banner.gameObject.SetActive(false);
            Sfx.Play(Sfx.Kind.Mystery);
            // dark veil + big word
            var veil = UI.Solid(fxLayer, new Color(0.03f, 0.05f, 0.15f, 0f), "Veil");
            UI.Stretch(veil.rectTransform);
            Fx.Tween(0.3f, t => { if (veil != null) veil.color = new Color(0.03f, 0.05f, 0.15f, 0.7f * t); });
            var rib = UI.Img(fxLayer, "ribbon_pink", "Mystery"); rib.preserveAspect = false;
            UI.Place(rib.rectTransform, 0.5f, 0.5f, 0, 280, 820, 130);
            var rt = UI.Label(rib.transform, "MYSTERY WORD!", 66, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(rt.rectTransform, 0, 8, 0, 0);
            Fx.PopIn(rib.rectTransform, 0.4f);
            var word = UI.Label(fxLayer, "", 130, Palette.Yellow, TextAnchor.MiddleCenter, true);
            UI.Place(word.rectTransform, 0.5f, 0.5f, 0, 60, 1040, 180);
            word.resizeTextForBestFit = true; word.resizeTextMinSize = 50; word.resizeTextMaxSize = 130;
            word.horizontalOverflow = HorizontalWrapMode.Wrap; word.verticalOverflow = VerticalWrapMode.Truncate;
            // letters spin, then lock in one by one
            string target = p.Word; var rng = new System.Random(3);
            for (int i = 0; i < target.Length; i++)
            {
                float t = 0;
                while (t < 0.22f)
                {
                    t += Time.unscaledDeltaTime;
                    var sb = new System.Text.StringBuilder(target.Substring(0, i));
                    for (int j = i; j < target.Length; j++) sb.Append((char)('A' + rng.Next(26)));
                    word.text = sb.ToString();
                    yield return null;
                }
                Sfx.Play(Sfx.Kind.Tick, 1f + i * 0.08f, 0.7f);
                Fx.Punch(word.rectTransform, 0.06f, 0.15f);
            }
            word.text = target;
            Sfx.Play(Sfx.Kind.Found, 1.2f);
            Fx.Burst(fxLayer, new Vector2(0, 60), "spark_star", Palette.Yellow, 24, 640f, 1f, 54f);
            Fx.Burst(fxLayer, new Vector2(0, 60), "conf_pink", Color.white, 14, 520f, 1.2f, 40f);
            Fx.Punch(word.rectTransform, 0.25f, 0.5f);
            var sub = UI.Label(fxLayer, isNew ? "NEW WORD DISCOVERED!" : "COLLECTION +1", 52, isNew ? Palette.Green : Color.white, TextAnchor.MiddleCenter, true);
            UI.Place(sub.rectTransform, 0.5f, 0.5f, 0, -90, 900, 80);
            Fx.PopIn(sub.rectTransform, 0.4f);
            yield return new WaitForSecondsRealtime(1.1f);
            // fly the word to the collection bar
            Vector2 target2 = fxLayer.InverseTransformPoint(g.CollectionBar.Root.position);
            Fx.Fly(fxLayer, new Vector2(0, 60), target2, "spark_star", 8, null, null, 56f, 0.7f, 0.03f);
            Fx.Tween(0.4f, t => { if (veil != null) veil.color = new Color(0.03f, 0.05f, 0.15f, 0.7f * (1 - t)); });
            Fx.Tween(0.4f, t => { if (word != null) word.color = new Color(word.color.r, word.color.g, word.color.b, 1 - t); if (rt != null) rt.color = new Color(1, 1, 1, 1 - t); if (sub != null) sub.color = new Color(sub.color.r, sub.color.g, sub.color.b, 1 - t); });
            yield return new WaitForSecondsRealtime(0.5f);
            if (veil != null) Destroy(veil.gameObject);
            if (rib != null) Destroy(rib.gameObject);
            if (word != null) Destroy(word.gameObject);
            if (sub != null) Destroy(sub.gameObject);
            if (g.MysteryChip != null)
            {
                var chip = g.MysteryChip; var img = chip.GetComponent<Image>(); img.color = new Color(1f, 0.85f, 0.3f);
                var pu = chip.GetComponent<Pulse>(); if (pu != null) Destroy(pu);
                chip.GetComponentInChildren<Text>().text = target; chip.GetComponentInChildren<Text>().color = Palette.Ink;
            }
            UpdateProgress(true);
            g.Busy = false; g.Grid.InputEnabled = true;
            if (g.Puzzle.Words.All(w => w.Found)) StartCoroutine(FinishRoutine());
        }

        // =============== finishing ===============
        IEnumerator FinishRoutine()
        {
            var g = game; if (g == null || g.Finished) yield break;
            g.Finished = true; g.Grid.InputEnabled = false;
            yield return new WaitForSecondsRealtime(0.55f);
            Sfx.Play(Sfx.Kind.Complete);
            Fx.Confetti(fxLayer, 1000, canvasRt.rect.height / 2 + 40, 40);
            yield return new WaitForSecondsRealtime(0.45f);
            var result = Progress.CompleteLevel(g.Spec, g.Puzzle, g.MysteryFound, g.PowerupsUsed);
            ShowResult(result);
        }

        // =============== power-ups ===============
        void UsePower(PowerUp pu)
        {
            var g = game; if (g == null || g.Finished || g.Busy) return;
            var d = SaveSystem.Data;
            if (PowerUps.Count(d, pu) <= 0) { ShowBuyPower(pu); return; }
            var remaining = g.Puzzle.AllPlacements().Where(w => !w.Found).ToList();
            var normal = remaining.Where(w => !w.IsMystery).ToList();
            if (remaining.Count == 0) return;
            var pool = normal.Count > 0 ? normal : remaining;
            var rng = new System.Random(g.Puzzle.Words.Count * 31 + g.PowerupsUsed * 7 + (int)(Time.unscaledTime * 10));
            var target = pool[rng.Next(pool.Count)];
            var grid = g.Grid;
            switch (pu)
            {
                case PowerUp.Hint:
                    grid.ClearMarks();
                    grid.MarkCell(target.X, target.Y, Palette.Yellow, "Hint");
                    Fx.Burst(fxLayer, grid.CellCanvas(target.X, target.Y, fxLayer), "spark_star", Palette.Yellow, 8, 260f, 0.6f);
                    break;
                case PowerUp.Letter:
                    {
                        int k = target.Length > 1 ? rng.Next(1, target.Length) : 0;
                        var c = target.Cell(k);
                        grid.MarkCell(c.x, c.y, new Color(0.4f, 0.75f, 1f), "Letter");
                        Fx.Burst(fxLayer, grid.CellCanvas(c.x, c.y, fxLayer), "spark_star", new Color(0.4f, 0.75f, 1f), 8, 260f, 0.6f);
                        break;
                    }
                case PowerUp.Finder:
                    grid.ClearMarks();
                    grid.MarkWord(target, Palette.Purple);
                    break;
                case PowerUp.Word:
                    {
                        target.Found = true;
                        Color color = target.IsMystery ? (Color)new Color32(0xFF, 0xC8, 0x2E, 255) : grid.NextColor();
                        grid.ClearMarks();
                        grid.LockWord(target, color, true);
                        OnWordFound(target, new Vector2Int(target.X, target.Y), target.End);
                        break;
                    }
                case PowerUp.Shuffle:
                    {
                        if (!PuzzleGenerator.Reshuffle(g.Puzzle, g.Spec, g.Spec.Seed + 97 * (++g.Shuffles))) { Toast("Could not shuffle", Palette.Red); return; }
                        grid.Refresh(true);
                        Sfx.Play(Sfx.Kind.Whoosh);
                        break;
                    }
            }
            PowerUps.Use(d, pu);
            g.PowerupsUsed++;
            Progress.Notify();
            Sfx.Play(pu == PowerUp.Shuffle ? Sfx.Kind.Whoosh : Sfx.Kind.Power, 1f + (int)pu * 0.1f);
            RefreshPowerBar();
            Fx.Punch(g.PowerIcons[(int)pu].rectTransform, 0.25f, 0.35f);
        }

        void ShowBuyPower(PowerUp pu)
        {
            int i = (int)pu; var d = SaveSystem.Data;
            var p = OpenPopup(PowerUps.Names[i].ToUpper(), 880, 820, true, "ribbon_green");
            var ic = UI.Icon(p.Content, PowerUps.Art[i], 200, "Icon"); UI.Place(ic.rectTransform, 0.5f, 1, 0, -150, 200, 200);
            var t = UI.Wrapped(p.Content, PowerUps.Info[i], 40, Palette.Ink, 720); UI.Place(t.rectTransform, 0.5f, 1, 0, -320, 720, 100);
            bool full = PowerUps.IsFull(d, pu);
            var own = UI.Label(p.Content, $"You have {PowerUps.Count(d, pu)}  (max {PowerUps.Max[i]})", 34, Palette.InkSoft); UI.Place(own.rectTransform, 0.5f, 1, 0, -400, 720, 44);
            var buy = UI.Pill(p.Content, full ? "btn_grey" : d.coins >= PowerUps.Cost[i] ? "btn_green" : "btn_grey", full ? "FULL" : $"BUY  {PowerUps.Cost[i]}", 560, 110, () =>
            {
                if (PowerUps.IsFull(d, pu)) return;
                if (!Progress.Spend(PowerUps.Cost[i])) { p.Close(); ShowShop(); return; }
                PowerUps.Add(d, pu); Progress.Notify(); Sfx.Play(Sfx.Kind.Coin); RefreshPowerBar(); p.Close();
            }, 50);
            UI.Place((RectTransform)buy.transform, 0.5f, 0, 0, 150, 560, 110);
            var shop = UI.Pill(p.Content, "btn_yellow", "OPEN SHOP", 560, 90, () => { p.Close(); ShowShop(null, RefreshPowerBar); }, 40);
            UI.Place((RectTransform)shop.transform, 0.5f, 0, 0, 40, 560, 90);
        }
    }
}
