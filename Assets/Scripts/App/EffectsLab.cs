using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    public partial class GameApp
    {
        static readonly string[] EffectBlurb = { "Rainbow capsules + stars", "Hot colors + flames", "Yellow and blue + bolts", "Soft pastels + bubbles", "Every color + confetti" };
        static readonly string[] SoundLabels = { "Tap", "Letter tick", "Cancel", "Word found", "Mystery word", "Level complete", "Coins", "Power-up", "Error", "Whoosh", "Pop", "Level up" };
        static readonly Sfx.Kind[] SoundKinds =
        {
            Sfx.Kind.Click, Sfx.Kind.Tick, Sfx.Kind.Cancel, Sfx.Kind.Found, Sfx.Kind.Mystery, Sfx.Kind.Complete,
            Sfx.Kind.Coin, Sfx.Kind.Power, Sfx.Kind.Error, Sfx.Kind.Whoosh, Sfx.Kind.Pop, Sfx.Kind.Level
        };

        // ---- demo strip pieces (rebuilt with the popup) ----
        Image labCapsule; Text[] labLetters; RectTransform labStrip;

        /// <summary>Test area for every word effect (colors, particles) and every sound. Shows what is in use.</summary>
        public void ShowEffectsLab()
        {
            var p = OpenPopup("EFFECTS LAB", 980, 1700, true, "ribbon_pink");
            RectTransform content;
            var sr = Scroll(p.Content, out content);
            UI.Stretch((RectTransform)sr.transform, 0, 0, 0, 0);
            BuildLab(p, content);
        }

        void BuildLab(Popup p, RectTransform content)
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            var d = SaveSystem.Data;
            float y = 6, w = 880;

            // ---- demo strip ----
            var strip = RowAt(content, ref y, 170, w);
            var card = UI.Round(strip, Color.white, 34, "Demo"); UI.Stretch(card.rectTransform);
            card.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.18f);
            labStrip = strip;
            labCapsule = UI.FlatPill(strip, new Color(1, 1, 1, 0), "DemoCapsule");
            var cr = labCapsule.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f); cr.sizeDelta = new Vector2(700, 112); cr.anchoredPosition = Vector2.zero;
            string word = "WORDS"; labLetters = new Text[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                var t = UI.Label(strip, word[i].ToString(), 92, Color.black, TextAnchor.MiddleCenter);
                UI.Place(t.rectTransform, 0.5f, 0.5f, (i - 2) * 132, 0, 120, 120);
                labLetters[i] = t;
            }
            y += 14;
            var tip = UI.Label(content, "Tap TEST to see and hear an effect", 30, Palette.InkSoft, TextAnchor.MiddleCenter, false);
            UI.Place(tip.rectTransform, 0.5f, 1, 0, -(y + 20), 860, 40); y += 56;

            // ---- word effects ----
            LabHeader(content, ref y, "WORD EFFECTS");
            for (int e = 0; e < 5; e++)
            {
                int eff = e;
                var row = ListRow(content, ref y, 140, w);
                for (int k = 0; k < 4; k++)
                {
                    var dot = UI.Node(row, "Sw" + k).gameObject.AddComponent<Image>();
                    dot.sprite = Shapes.Circle(); dot.color = WordGridView.ColorFor(eff, k); dot.raycastTarget = false;
                    UI.Place(dot.rectTransform, 0, 0.5f, 52 + k * 46, 0, 38, 38);
                }
                var nm = UI.Label(row, Economy.EffectNames[eff], 38, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(nm.rectTransform, 0, 0.5f, 250, 18, 250, 50);
                var bl = UI.Label(row, EffectBlurb[eff], 22, Palette.InkSoft, TextAnchor.MiddleLeft); UI.PlaceL(bl.rectTransform, 0, 0.5f, 250, -24, 260, 30);
                bool own = d.effectOwned[eff], inUse = d.effect == eff;
                var test = UI.Pill(row, "btn_yellow", "TEST", 140, 66, () => PlayLabDemo(eff), 32);
                UI.Place((RectTransform)test.transform, 1, 0.5f, -310, 0, 140, 66);
                if (inUse)
                {
                    var tag = UI.Pill(row, "btn_green", "IN USE", 170, 66, () => { }, 30);
                    UI.Place((RectTransform)tag.transform, 1, 0.5f, -108, 0, 170, 66);
                    tag.interactable = false;
                }
                else
                {
                    var use = UI.Pill(row, own ? "btn_blue" : "btn_grey", own ? "USE" : "Lv " + Economy.EffectLevel[eff], 170, 66, () =>
                    {
                        if (!SaveSystem.Data.effectOwned[eff]) { Toast($"Reach level {Economy.EffectLevel[eff]} to unlock", Palette.Yellow); Sfx.Play(Sfx.Kind.Error); return; }
                        SaveSystem.Data.effect = eff; Progress.Notify(); BuildLab(p, content);
                    }, 30);
                    UI.Place((RectTransform)use.transform, 1, 0.5f, -108, 0, 170, 66);
                }
            }

            // ---- sound style ----
            y += 10;
            LabHeader(content, ref y, "SOUND STYLE");
            for (int s = 0; s < Sfx.StyleNames.Length; s++)
            {
                int style = s;
                var row = ListRow(content, ref y, 120, w);
                var nm = UI.Label(row, Sfx.StyleNames[style], 38, Palette.Ink, TextAnchor.MiddleLeft); UI.PlaceL(nm.rectTransform, 0, 0.5f, 46, 0, 320, 50);
                var test = UI.Pill(row, "btn_yellow", "TEST", 140, 66, () => PreviewSoundStyle(style), 32);
                UI.Place((RectTransform)test.transform, 1, 0.5f, -310, 0, 140, 66);
                if (d.sfxStyle == style)
                {
                    var tag = UI.Pill(row, "btn_green", "IN USE", 170, 66, () => { }, 30);
                    UI.Place((RectTransform)tag.transform, 1, 0.5f, -108, 0, 170, 66); tag.interactable = false;
                }
                else
                {
                    var use = UI.Pill(row, "btn_blue", "USE", 170, 66, () => { SaveSystem.Data.sfxStyle = style; Progress.Notify(); PreviewSoundStyle(style); BuildLab(p, content); }, 30);
                    UI.Place((RectTransform)use.transform, 1, 0.5f, -108, 0, 170, 66);
                }
            }

            // ---- every sound ----
            y += 10;
            LabHeader(content, ref y, "ALL SOUNDS  (style: " + Sfx.StyleNames[d.sfxStyle] + ")");
            for (int i = 0; i < SoundKinds.Length; i += 2)
            {
                var row = RowAt(content, ref y, 92, w);
                for (int c = 0; c < 2 && i + c < SoundKinds.Length; c++)
                {
                    int idx = i + c;
                    var b = UI.Pill(row, "btn_purple", SoundLabels[idx], 430, 78, () => PlayLabSound(idx), 30);
                    UI.Place((RectTransform)b.transform, 0, 0.5f, 215 + c * 450, 0, 430, 78);
                }
            }
            y += 20;
            EndScroll(content, y);
        }

        void LabHeader(RectTransform content, ref float y, string text)
        {
            var h = UI.Label(content, text, 40, Palette.Ink, TextAnchor.MiddleCenter, false);
            UI.Place(h.rectTransform, 0.5f, 1, 0, -(y + 30), 860, 54); y += 70;
        }

        void PlayLabSound(int idx)
        {
            var k = SoundKinds[idx];
            if (k == Sfx.Kind.Tick) { for (int i = 0; i < 5; i++) { int s = i; DOVirtual.DelayedCall(i * 0.09f, () => Sfx.Tick(s)).SetUpdate(true); } }
            else Sfx.Play(k);
            if (k == Sfx.Kind.Coin) Fx.Burst(fxLayer, Vector2.zero, "coin", Color.white, 6, 380f, 0.7f, 50f);
        }

        void PreviewSoundStyle(int style)
        {
            var d = SaveSystem.Data; int old = d.sfxStyle;
            d.sfxStyle = style;
            Sfx.Play(Sfx.Kind.Found);
            DOVirtual.DelayedCall(0.35f, () => { d.sfxStyle = style; Sfx.Play(Sfx.Kind.Coin); d.sfxStyle = old; }).SetUpdate(true);
            d.sfxStyle = old;
        }

        /// <summary>Plays one word effect on the demo strip: capsule in the effect colors, white letters, particles, sound.</summary>
        void PlayLabDemo(int eff)
        {
            if (labCapsule == null || labStrip == null) return;
            var cap = labCapsule; var cr = cap.rectTransform;
            cap.DOKill(); cr.DOKill();
            Color col = WordGridView.ColorFor(eff, 0);
            cap.color = new Color(col.r, col.g, col.b, 0.92f);
            cr.sizeDelta = new Vector2(120, 112);
            cr.DOSizeDelta(new Vector2(700, 112), 0.38f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(cr.gameObject);
            foreach (var t in labLetters) { t.DOKill(); t.color = Color.white; Fx.Punch(t.rectTransform, 0.2f, 0.35f); }
            Vector2 end = fxLayer.InverseTransformPoint(labLetters[labLetters.Length - 1].transform.position);
            Vector2 mid = fxLayer.InverseTransformPoint(labStrip.position);
            string particle = WordGridView.ParticleFor(eff);
            Fx.Burst(fxLayer, end, particle, eff == 4 ? Color.white : Color.white, 14, 420f, 0.7f, 46f);
            Fx.Burst(fxLayer, mid, "spark_star", WordGridView.ColorFor(eff, 2), 8, 300f, 0.6f, 38f);
            Sfx.Play(Sfx.Kind.Found);
            DOVirtual.DelayedCall(1.6f, () =>
            {
                if (cap == null) return;
                cap.DOFade(0f, 0.3f).SetUpdate(true).SetLink(cap.gameObject);
                foreach (var t in labLetters) if (t != null) t.DOColor(Color.black, 0.3f).SetUpdate(true).SetLink(t.gameObject);
            }).SetUpdate(true);
        }
    }
}
