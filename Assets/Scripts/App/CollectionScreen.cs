using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        void BuildCollection(RectTransform b)
        {
            var d = SaveSystem.Data;
            TopBar(b);
            var head = UI.Node(b, "Header");
            head.anchorMin = new Vector2(0, 1); head.anchorMax = new Vector2(1, 1); head.pivot = new Vector2(0.5f, 1);
            head.anchoredPosition = new Vector2(0, -150); head.sizeDelta = new Vector2(0, 190);
            var title = UI.Label(head, "COLLECTION", 74, Color.white, TextAnchor.MiddleCenter, true);
            UI.Place(title.rectTransform, 0.5f, 1, 0, -44, 800, 90);
            int all = WordBank.All.Sum(c => c.Words.Length), have = Progress.TotalDiscovered();
            var bar = UI.ProgressBar(head, 820, 52, Palette.Green, "All");
            UI.Place(bar.Root, 0.5f, 1, 0, -140, 820, 52); bar.Set(have / (float)all);
            var bt = UI.Label(bar.Root, $"{have} / {all} words discovered", 30, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(bt.rectTransform);

            RectTransform content;
            var sr = Scroll(b, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 350);
            float y = 10;
            for (int i = 0; i < WordBank.Count; i++)
            {
                int cat = i;
                var c = WordBank.Get(i);
                var row = RowAt(content, ref y, 230);
                var bg = UI.Sliced(row, "card_a", 230, "Card", true); UI.Stretch(bg.rectTransform);
                UI.Click(bg, () => ShowCategory(cat));
                var ic = UI.Icon(row, c.ArtKey, 150, "Icon"); UI.Place(ic.rectTransform, 0, 0.5f, 110, 0, 150, 150);
                var nm = UI.Label(row, c.Name.ToUpper(), 44, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(nm.rectTransform, 0, 1, 230, -50, 560, 56);
                int f = Progress.Found(cat), tot = Progress.TotalIn(cat);
                var pb = UI.ProgressBar(row, 520, 46, Palette.Green, "P"); UI.Place(pb.Root, 0, 0.5f, 230 + 260, -4, 520, 46);
                pb.Set(f / (float)tot);
                var pt = UI.Label(pb.Root, $"{f} / {tot}", 28, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(pt.rectTransform);
                int step = Progress.ClaimableStep(cat);
                if (step >= 0)
                {
                    var claim = UI.Pill(row, "btn_yellow", "CLAIM", 200, 84, () =>
                    {
                        int coins = Progress.ClaimStep(cat);
                        if (coins > 0) { Sfx.Play(Sfx.Kind.Coin); Fx.Fly(fxLayer, Vector2.zero, CoinTarget(), "coin", 8, null, () => { Toast($"+{coins} coins!", Palette.Yellow); ShowTab(Tab.Collection); }); }
                    }, 38);
                    UI.Place((RectTransform)claim.transform, 1, 0.5f, -130, -50, 200, 84);
                    claim.gameObject.AddComponent<Pulse>().Amount = 0.06f;
                }
                var next = UI.Label(row, Progress.StepLabel(cat), 26, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(next.rectTransform, 0, 0, 230, 44, 520, 34);
                y += 14;
            }
            EndScroll(content, y);
        }

        void ShowCategory(int cat)
        {
            var c = WordBank.Get(cat);
            var p = OpenPopup(c.Name.ToUpper(), 980, 1500);
            int f = Progress.Found(cat), tot = Progress.TotalIn(cat);
            var bar = UI.ProgressBar(p.Content, 780, 50, Palette.Green, "P"); UI.Place(bar.Root, 0.5f, 1, 0, -40, 780, 50); bar.Set(f / (float)tot);
            var bt = UI.Label(bar.Root, $"{f} / {tot} words", 30, Color.white, TextAnchor.MiddleCenter, true); UI.Stretch(bt.rectTransform);
            RectTransform content;
            var sr = Scroll(p.Content, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 100);
            var found = Progress.CatEntry(cat).words;
            var words = c.Words.OrderBy(w => w.Length).ThenBy(w => w).ToList();
            float x = 0, y = 10, w0 = 860, rowH = 84;
            var line = UI.Node(content, "Line"); line.anchorMin = line.anchorMax = new Vector2(0.5f, 1); line.pivot = new Vector2(0.5f, 1);
            foreach (var w in words)
            {
                bool got = found.Contains(w);
                string txt = got ? w : new string('?', w.Length);
                float cw = Mathf.Max(120, txt.Length * 30 + 50);
                if (x + cw > w0) { x = 0; y += rowH + 10; }
                var chip = UI.Sliced(content, "btn_white", 70, "Chip");
                chip.color = got ? new Color(0.75f, 0.95f, 0.78f) : new Color(1, 1, 1, 0.85f);
                UI.Place(chip.rectTransform, 0.5f, 1, -w0 / 2 + x + cw / 2, -(y + rowH / 2), cw, 72);
                var t = UI.Label(chip.transform, txt, 32, got ? Palette.Ink : Palette.Grey, TextAnchor.MiddleCenter); UI.Stretch(t.rectTransform);
                x += cw + 12;
            }
            EndScroll(content, y + rowH + 20);
        }
    }
}
