using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordQuest.EditorTools
{
    /// <summary>
    /// Helpers for automated play tests in the Editor (used through the Unity MCP/CLI eval command).
    /// Play mode must be running. Frames are stepped by hand because the Editor stalls when it is not focused.
    /// </summary>
    public static class TestHarness
    {
        public static void Advance(int frames, int sleepMs = 16)
        {
            for (int i = 0; i < frames; i++) { EditorApplication.Step(); if (sleepMs > 0) Thread.Sleep(sleepMs); }
        }

        public static void Settle(int frames = 45)
        {
            Advance(frames);
            for (int i = 0; i < 6; i++) { EditorApplication.Step(); Thread.Sleep(20); InternalEditorUtility.RepaintAllViews(); }
        }

        static class InternalEditorUtility { public static void RepaintAllViews() { UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); } }

        public static void Boot()
        {
            EditorApplication.isPaused = true;
            Advance(200);
            var go = GameObject.Find("Popups");
            if (go != null) foreach (Transform t in go.transform) UnityEngine.Object.Destroy(t.gameObject);
            Advance(10);
        }

        static void Call(string method, params object[] args)
        {
            var m = typeof(GameApp).GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(GameApp.I, args);
        }

        public static string Lint(string label)
        {
            Settle();
            // only check what the player can really see: the top popup, or the screen when there is none
            var pops = GameObject.Find("Popups");
            Transform root = GameObject.Find("Canvas").transform;
            if (pops != null && pops.transform.childCount > 0) root = pops.transform.GetChild(pops.transform.childCount - 1);
            else { var safe = root.Find("SafeArea"); if (safe != null) root = safe; }
            return "== " + label + ": " + UiLint.Run(root);
        }

        /// <summary>Opens every screen and popup and reports overlaps / edge problems.</summary>
        public static string LintAll()
        {
            var app = GameApp.I; var sb = new StringBuilder();
            foreach (Tab t in Enum.GetValues(typeof(Tab))) { app.ShowTab(t); sb.AppendLine(Lint("tab " + t)); }
            app.ShowTab(Tab.Home);
            void Pop(string name, Action open) { open(); sb.AppendLine(Lint(name)); app.CloseAllPopups(); Advance(14); }
            Pop("shop", () => app.ShowShop());
            Pop("league week", () => app.ShowLeague(false));
            Pop("league today", () => app.ShowLeague(true));
            Pop("settings", () => app.ShowSettings());
            Pop("how to", () => app.ShowHowTo());
            Pop("daily reward", () => app.ShowDailyReward());
            Pop("category", () => Call("ShowCategory", 0));
            return sb.ToString();
        }

        public static string LintGame(int level)
        {
            var app = GameApp.I; var sb = new StringBuilder();
            app.StartGame(level == 0 ? Levels.Daily(Clock.Today) : Levels.Get(level));
            sb.AppendLine(Lint("game L" + level + " start"));
            var grid = UnityEngine.Object.FindFirstObjectByType<WordGridView>();
            Swipe(grid, grid.Puzzle.Words[0]);
            Swipe(grid, grid.Puzzle.Words[1]);
            sb.AppendLine(Lint("game L" + level + " after 2 words"));
            Call("ShowPause");
            sb.AppendLine(Lint("pause popup"));
            app.CloseAllPopups(); Advance(14);
            return sb.ToString();
        }

        public static void Swipe(WordGridView grid, Placement w)
        {
            var fx = GameApp.I.FxLayer;
            PointerEventData At(int x, int y)
            {
                var c = grid.CellCanvas(x, y, fx); var world = fx.TransformPoint(new Vector3(c.x, c.y, 0));
                return new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, world) };
            }
            grid.OnPointerDown(At(w.X, w.Y)); EditorApplication.Step();
            for (int i = 1; i < w.Length; i++) { var c = w.Cell(i); grid.OnDrag(At(c.x, c.y)); EditorApplication.Step(); Thread.Sleep(10); }
            grid.OnPointerUp(null);
            Advance(12);
        }

        /// <summary>Plays a whole level with simulated swipes, leaving the result popup open.</summary>
        public static string PlayThrough(int level, bool findMystery = true)
        {
            var app = GameApp.I;
            app.StartGame(level == 0 ? Levels.Daily(Clock.Today) : Levels.Get(level));
            Advance(40);
            var grid = UnityEngine.Object.FindFirstObjectByType<WordGridView>();
            foreach (var w in grid.Puzzle.Words.ToList()) Swipe(grid, w);
            Advance(30);
            if (findMystery) Swipe(grid, grid.Puzzle.Mystery);
            Advance(findMystery ? 330 : 10);
            if (!findMystery) { var m = typeof(GameApp).GetMethod("SkipMystery", BindingFlags.NonPublic | BindingFlags.Instance); m.Invoke(app, null); Advance(120); }
            return "coins=" + SaveSystem.Data.coins + " level=" + SaveSystem.Data.playerLevel;
        }

        public static void Click(string buttonName)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (b.name == buttonName && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return; }
            throw new Exception("button not found: " + buttonName);
        }
    }
}
