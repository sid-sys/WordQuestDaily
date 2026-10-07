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
        public Text RevealCount; public Image RevealBadge; public RectTransform RevealButton;
        public int PowerupsUsed, FoundCount, Shuffles;
        public bool MysteryFound, MysterySkipped, Finished, Busy;
        public float LastFoundTime;
        public Button SkipButton;
        public Image CurrentPill; public Text CurrentText;
        public RectTransform TutLayer; public TutorialState Tut;
        public string Msg, PillText; public Color PillColor; public bool Dragging;
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
            if (!spec.IsDaily) { SaveSystem.Data.unfinishedLevel = spec.Index; SaveSystem.Data.unfinishedDate = Clock.TodayKey; }
            body.gameObject.SetActive(false); tabBar.gameObject.SetActive(false);
            gameLayer.gameObject.SetActive(true);
            foreach (Transform c in gameLayer) Destroy(c.gameObject);
            SetBackground(SaveSystem.Data.theme);
            BuildGame();
            BeginTutorials();
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

        // =============== pause menu ===============
        void ShowPause()
        {
            var g = game; if (g == null || g.Finished) return;
            var p = OpenPopup("PAUSED", 820, 720, true);
            void Btn(string art, string text, int i, Action a)
            {
                var b = UI.Pill(p.Content, art, text, 520, 112, a, 48);
                UI.Place((RectTransform)b.transform, 0.5f, 1, 0, -(76 + i * 144), 520, 112);
            }
            Btn("btn_green", "CONTINUE", 0, p.Close);
            Btn("btn_yellow", "RESTART", 1, () => { var spec = g.Spec; CloseAllPopups(); StartGame(spec); });
            Btn("btn_blue", "HOME", 2, () => { CloseAllPopups(); EndGame(); ShowTab(Tab.Home); });
        }

        /// <summary>Pause button: blue tile with a white pause symbol (like the reference).</summary>
        RectTransform MakePauseButton(Transform parent, float size, Action onClick)
        {
            var b = UI.GlyphButton(parent, "g_pause", new Color32(0x2F, 0x8D, 0xF0, 255), size, "Pause", 0.52f);
            UI.Click(b, onClick);
            return b.rectTransform;
        }

        // =============== layout ===============
        void BuildGame()
        {
            var g = game; var spec = g.Spec; var puzzle = g.Puzzle; var d = SaveSystem.Data;
            g.Root = UI.Stretch(UI.Node(gameLayer, "Level"));
            var root = g.Root;
            float rootH = safe.rect.height;

            // --- HUD: pause top left, coins, level pill, Reveal Word top right ---
            MakePauseButton(root, 96, ShowPause);
            UI.Place((RectTransform)root.Find("Pause"), 0, 1, 70, -72, 96, 96);
            var coins = CoinPill(root, false);
            UI.Place(coins, 0, 1, 130 + 150, -72, 300, 92);
            var lvPill = UI.Pill(root, "btn_blue", spec.IsDaily ? "Daily" : "Level " + spec.Index, 290, 84, () => { }, 46);
            UI.Place((RectTransform)lvPill.transform, 0.5f, 1, 150, -72, 290, 84);
            lvPill.transition = Selectable.Transition.None;
            var cat = WordBank.Get(puzzle.CategoryIndex);
            BuildRevealButton(root);

            // --- two progress bars side by side: this level, and the word collection ---
            g.LevelBar = UI.ProgressBar(root, 520, 44, Palette.Green, "LevelBar");
            UI.Place(g.LevelBar.Root, 0, 1, 40 + 260, -170, 520, 44);
            g.LevelBarText = UI.Label(g.LevelBar.Root, "", 28, Palette.Ink, TextAnchor.MiddleCenter, false); UI.Stretch(g.LevelBarText.rectTransform);
            var colIcon = UI.Icon(root, cat.ArtKey, 52, "CatIcon"); UI.Place(colIcon.rectTransform, 0, 1, 600 + 26, -168, 52, 52);
            g.CollectionBar = UI.ProgressBar(root, 380, 38, Palette.Blue, "CollectionBar");
            UI.Place(g.CollectionBar.Root, 0, 1, 660 + 190, -168, 380, 38);
            g.CollectionText = UI.Label(g.CollectionBar.Root, "", 24, Palette.Ink, TextAnchor.MiddleCenter, false); UI.Stretch(g.CollectionText.rectTransform);

            // --- word list: plain bold words in a light panel, struck through when found ---
            float listTop = 206;
            float ls = rootH > 1900 ? 1.22f : 1f;   // tall phones get a bigger word list
            float listBottom = BuildWordList(root, listTop, ls);

            // --- the slot between the list and the board: the swiped word, or a message ---
            float pillY = listBottom + 14;
            g.CurrentPill = UI.Sliced(root, "btn_white", 76, "Current");
            UI.Place(g.CurrentPill.rectTransform, 0.5f, 1, 0, -(pillY + 38), 300, 76);
            g.CurrentText = UI.Label(g.CurrentPill.transform, "", 38, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.CurrentText.rectTransform, 0, 2, 0, 0);
            g.CurrentPill.gameObject.SetActive(false);
            g.Banner = UI.Node(root, "Banner");
            UI.Place(g.Banner, 0.5f, 1, 0, -(pillY + 38), 960, 76);
            var bi = UI.Sliced(g.Banner, "btn_pink", 76, "Bg"); UI.Stretch(bi.rectTransform);
            g.BannerText = UI.Label(g.Banner, "", 32, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.BannerText.rectTransform, 40, 2, 40, 0);
            g.SkipButton = UI.Pill(g.Banner, "btn_grey", "SKIP", 150, 52, SkipMystery, 28);
            UI.Place((RectTransform)g.SkipButton.transform, 1, 0.5f, -96, 0, 150, 52);
            g.SkipButton.gameObject.SetActive(false);
            g.Banner.gameObject.SetActive(false);

            // --- board: a big white card, as wide as the screen allows ---
            float boardTop = pillY + 76 + 14;
            float availH = rootH - boardTop - 40, availW = 1080 - 40 - 32;
            float cell = Mathf.Min(availW / puzzle.Cols, availH / puzzle.Rows, 190f);
            float bw = cell * puzzle.Cols, bh = cell * puzzle.Rows;
            var boardHolder = UI.Node(root, "Board");
            float extra = Mathf.Max(0f, availH - bh);
            float boardCenterY = -(boardTop + Mathf.Min(extra * 0.45f, 260f) + (bh + 36) / 2f);
            UI.Place(boardHolder, 0.5f, 1, 0, boardCenterY, bw + 36, bh + 36);
            var panel = UI.Round(boardHolder, Palette.PanelGrey, 34, "Panel");
            UI.Stretch(panel.rectTransform);
            var gridRt = UI.Node(boardHolder, "Grid");
            UI.Place(gridRt, 0.5f, 0.5f, 0, 0, bw, bh);
            g.Grid = gridRt.gameObject.AddComponent<WordGridView>();
            g.Grid.Build(puzzle, cell);
            g.Grid.OnWordFound = OnWordFound;
            g.Grid.OnWrong = () => { };
            g.Grid.OnSelection = (text, color) =>
            {
                if (g.CurrentPill == null) return;
                g.Dragging = text != null;
                if (text != null)
                {
                    g.CurrentText.text = text; UI.SetBodyColor(g.CurrentPill, color);
                    g.CurrentPill.rectTransform.sizeDelta = new Vector2(Mathf.Max(240, text.Length * 34 + 110), 76);
                }
                UpdateSlot();
            };
            Fx.PopIn(boardHolder, 0.4f);

            g.TutLayer = UI.Stretch(UI.Node(root, "Tutorial"));
            UpdateProgress(false);
        }

        void SetSlotMessage(string msg, bool skip)
        {
            var g = game; if (g == null) return;
            g.Msg = msg; g.SkipButton.gameObject.SetActive(skip && !string.IsNullOrEmpty(msg));
            g.BannerText.GetComponent<RectTransform>().offsetMax = new Vector2(skip ? -190 : -30, 0);
            g.BannerText.text = msg ?? "";
            UpdateSlot();
        }

        void UpdateSlot()
        {
            var g = game; if (g == null || g.CurrentPill == null) return;
            bool showMsg = !g.Dragging && !string.IsNullOrEmpty(g.Msg);
            if (showMsg && !g.Banner.gameObject.activeSelf) Fx.PopIn((RectTransform)g.Banner, 0.3f);
            g.Banner.gameObject.SetActive(showMsg);
            bool showPill = g.Dragging || (!showMsg && !string.IsNullOrEmpty(g.PillText));
            g.CurrentPill.gameObject.SetActive(showPill);
            if (showPill && !g.Dragging)
            {
                g.CurrentText.text = g.PillText; UI.SetBodyColor(g.CurrentPill, g.PillColor);
                g.CurrentPill.rectTransform.sizeDelta = new Vector2(Mathf.Max(240, g.PillText.Length * 34 + 110), 76);
            }
        }

        /// <summary>Light panel with the words in columns. Returns the bottom y of the panel.</summary>
        float BuildWordList(RectTransform root, float top, float scale)
        {
            var g = game;
            var words = g.Puzzle.Words.Select(w => w.Word).ToList();
            if (g.Puzzle.Mystery != null) words.Add("?");
            int n = words.Count;
            int cols = n <= 6 ? 2 : n <= 12 ? 3 : 4;
            int rows = Mathf.CeilToInt(n / (float)cols);
            float rowH = (n > 12 ? 44f : 52f) * scale, padY = 18 * scale;
            float w = 1000, h = rows * rowH + padY * 2;
            var panel = UI.Round(root, Palette.PanelGrey, 36, "WordList");
            UI.Place(panel.rectTransform, 0.5f, 1, 0, -(top + h / 2), w, h);
            int fontSize = Mathf.RoundToInt((n > 12 ? 30 : 36) * scale);
            for (int i = 0; i < n; i++)
            {
                int r = i / cols, c = i % cols;
                bool mystery = words[i] == "?";
                var cell = UI.Node(panel.transform, "W_" + words[i]);
                float cw = (w - 40) / cols;
                UI.Place(cell, 0, 1, 20 + cw * (c + 0.5f), -(padY + rowH * (r + 0.5f)), cw, rowH);
                string label = mystery ? new string('?', g.Puzzle.Mystery.Word.Length) : words[i];
                var t = UI.Label(cell, label, fontSize, mystery ? Palette.Purple : Palette.WordTodo, TextAnchor.MiddleCenter, false);
                UI.Stretch(t.rectTransform);
                if (mystery) { g.MysteryChip = cell; }
                else g.Chips[words[i]] = cell;
            }
            return top + h;
        }

        /// <summary>Reveal Word, the only power-up: a bulb button top right with the number you own.</summary>
        void BuildRevealButton(RectTransform root)
        {
            var g = game;
            var btn = UI.GlyphButton(root, "g_bulb", new Color32(0xF5, 0xA6, 0x23, 255), 96, "Reveal", 0.6f);
            UI.Place(btn.rectTransform, 1, 1, -70, -72, 96, 96);
            UI.Click(btn, () => UsePower(PowerUp.Word));
            g.RevealButton = btn.rectTransform;
            var rim = UI.Node(btn.transform, "BadgeRim").gameObject.AddComponent<Image>(); rim.sprite = Shapes.Circle(); rim.color = Color.white; rim.raycastTarget = false;
            rim.rectTransform.anchorMin = rim.rectTransform.anchorMax = new Vector2(0, 0); rim.rectTransform.anchoredPosition = new Vector2(8, 8); rim.rectTransform.sizeDelta = new Vector2(52, 52);
            var badge = UI.Node(btn.transform, "Badge").gameObject.AddComponent<Image>();
            badge.sprite = Shapes.Circle(); badge.color = Palette.Red; badge.raycastTarget = false;
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0, 0); badge.rectTransform.anchoredPosition = new Vector2(8, 8); badge.rectTransform.sizeDelta = new Vector2(42, 42);
            g.RevealBadge = badge;
            g.RevealCount = UI.Label(badge.transform, "", 26, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(g.RevealCount.rectTransform);
            g.RevealCount.GetComponent<Outline>().effectColor = new Color32(0x70, 0x10, 0x20, 255);
            RefreshPowerBar();
        }

        void RefreshPowerBar()
        {
            var g = game; if (g == null || g.RevealCount == null) return;
            int n = SaveSystem.Data.powers[(int)PowerUp.Word];
            g.RevealCount.text = n > 0 ? n.ToString() : "+";
            g.RevealBadge.color = n > 0 ? Palette.Red : Palette.Green;
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
            TutorialWordFound();
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
            // keep the found word visible in the slot between the list and the board
            g.PillText = p.Word; g.PillColor = grid.LastColor;
            UpdateSlot();
            if (g.CurrentPill != null && g.CurrentPill.gameObject.activeSelf) Fx.Punch(g.CurrentPill.rectTransform, 0.15f, 0.3f);
            // a found word turns black in the list
            if (g.Chips.TryGetValue(p.Word, out var chip))
            {
                chip.GetComponentInChildren<Text>().color = Color.black;
                Fx.Punch(chip, 0.18f, 0.35f);
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
            SetSlotMessage($"FIND THE MYSTERY WORD!  ({g.Puzzle.Mystery.Word.Length} letters)", true);
            Sfx.Play(Sfx.Kind.Mystery, 1f, 0.6f);
            if (g.MysteryChip != null) Fx.Punch(g.MysteryChip, 0.3f, 0.6f);
        }

        void SkipMystery()
        {
            var g = game; if (g == null || g.Finished) return;
            g.MysterySkipped = true; SetSlotMessage(null, false);
            CheckFinish();
        }

        // =============== mystery word reveal ===============
        IEnumerator MysteryRoutine(Placement p, bool isNew)
        {
            var g = game; if (g == null) yield break;
            g.Busy = true; g.Grid.InputEnabled = false;
            SetSlotMessage(null, false);
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
                var chip = g.MysteryChip;
                var pu = chip.GetComponent<Pulse>(); if (pu != null) Destroy(pu);
                var mt = chip.GetComponentInChildren<Text>(); mt.text = target; mt.color = Color.black;
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
            HoldCoins(2.6f);
            var result = Progress.CompleteLevel(g.Spec, g.Puzzle, g.MysteryFound, g.PowerupsUsed);
            ShowResult(result);
        }

        // =============== power-ups ===============
        void UsePower(PowerUp pu)
        {
            var g = game; if (g == null || g.Finished || g.Busy || pu != PowerUp.Word) return;
            var d = SaveSystem.Data;
            if (PowerUps.Count(d, pu) <= 0) { ShowBuyPower(pu); return; }
            var remaining = g.Puzzle.AllPlacements().Where(w => !w.Found).ToList();
            var normal = remaining.Where(w => !w.IsMystery).ToList();
            if (remaining.Count == 0) return;
            var pool = normal.Count > 0 ? normal : remaining;
            var rng = new System.Random(g.Puzzle.Words.Count * 31 + g.PowerupsUsed * 7 + (int)(Time.unscaledTime * 10));
            var target = pool[rng.Next(pool.Count)];
            var grid = g.Grid;
            target.Found = true;
            Color color = target.IsMystery ? (Color)new Color32(0xFF, 0xC8, 0x2E, 255) : grid.NextColor();
            grid.ClearMarks();
            grid.LockWord(target, color, true);
            OnWordFound(target, new Vector2Int(target.X, target.Y), target.End);
            PowerUps.Use(d, pu);
            g.PowerupsUsed++;
            Progress.Notify();
            Sfx.Play(Sfx.Kind.Power, 1.4f);
            RefreshPowerBar();
            Fx.Punch(g.RevealButton, 0.25f, 0.35f);
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
