using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>
    /// Hand tutorials. Level 1 teaches swiping; then one power-up is taught per step (Hint on level 1, Shuffle on 3,
    /// Reveal Letter on 7, Word Finder on 12, Reveal Word on 18). A power-up stays locked until its tutorial is done.
    /// </summary>
    public class TutorialState
    {
        public int Kind;            // 1 = swipe, 2 = power-up
        public int Power;
        public RectTransform Hand;
        public Sequence Seq;
        public GameObject Extras;   // preview capsule, ring, glow
    }

    public partial class GameApp
    {
        static readonly string[] PowerTutorialText =
        {
            "Tap Shuffle to move the words you have not found!",
            "Stuck? Tap the magnifier for a hint!",
            "Tap Reveal Letter to see one letter of a word!",
            "Tap Word Finder to see where a word starts!",
            "Tap Reveal Word to find a whole word for you!",
        };

        /// <summary>Which power-up should be taught on this level (or -1).</summary>
        int DuePowerTutorial(int level)
        {
            var d = SaveSystem.Data;
            for (int i = 0; i < PowerUps.All.Length; i++)
                if (!d.powerUnlocked[i] && level >= PowerUps.UnlockLevel[i]) return i;
            return -1;
        }

        void BeginTutorials()
        {
            var g = game; if (g == null || g.Spec.IsDaily) return;
            var d = SaveSystem.Data;
            if (!d.swipeTutorialDone) { DOVirtual.DelayedCall(0.7f, StartSwipeTutorial).SetUpdate(true); return; }
            DOVirtual.DelayedCall(0.9f, TryPowerTutorial).SetUpdate(true);
        }

        void StartSwipeTutorial()
        {
            var g = game; if (g == null || g.Finished) return;
            Placement best = null;
            foreach (var w in g.Puzzle.Words) if (!w.Found && (best == null || w.Length < best.Length)) best = w;
            if (best == null) return;
            var layer = g.TutLayer;
            var grid = g.Grid;
            Vector2 a = grid.CellCanvas(best.X, best.Y, layer), b = grid.CellCanvas(best.End.x, best.End.y, layer);

            var t = new TutorialState { Kind = 1 };
            var preview = UI.FlatPill(layer, new Color(1f, 0.85f, 0.2f, 0.85f), "TutPreview");
            var pr = preview.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            t.Extras = preview.gameObject;
            var hand = MakeHand(layer);
            t.Hand = hand;
            float capH = grid.Cell * 0.8f;
            Action<Vector2> setPreview = pos =>
            {
                Vector2 mid = (a + pos) / 2f; float len = (pos - a).magnitude + capH * 1.05f;
                pr.anchoredPosition = mid; pr.sizeDelta = new Vector2(len, capH);
                pr.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pos.y - a.y, pos.x - a.x) * Mathf.Rad2Deg);
            };
            var handImg = hand.GetComponent<Image>();
            var seq = DOTween.Sequence().SetLoops(-1).SetUpdate(true).SetLink(layer.gameObject);
            seq.AppendCallback(() => { hand.anchoredPosition = a; hand.localScale = Vector3.one; handImg.color = Color.white; preview.color = new Color(1f, 0.85f, 0.2f, 0f); setPreview(a); });
            seq.Append(hand.DOScale(0.86f, 0.18f));
            seq.AppendCallback(() => preview.color = new Color(1f, 0.85f, 0.2f, 0.85f));
            seq.Append(DOVirtual.Float(0f, 1f, 1.2f, v => { var pos = Vector2.Lerp(a, b, v); hand.anchoredPosition = pos; setPreview(pos); }).SetEase(Ease.InOutSine));
            seq.Append(handImg.DOFade(0f, 0.25f));
            seq.Join(preview.DOFade(0f, 0.25f));
            seq.AppendInterval(0.6f);
            t.Seq = seq;
            g.Tut = t;
            SetSlotMessage("Swipe across the letters to find a word!", false);
        }

        void TryPowerTutorial()
        {
            var g = game; if (g == null || g.Finished || g.Tut != null || g.Spec.IsDaily) return;
            int i = DuePowerTutorial(g.Spec.Index);
            if (i < 0) return;
            var d = SaveSystem.Data;
            if (d.powers[i] <= 0) d.powers[i] = 1;     // a free one to try
            Progress.Notify();
            RefreshPowerBar();
            var layer = g.TutLayer;
            var icon = g.PowerIcons[i].rectTransform;
            Vector2 target = layer.InverseTransformPoint(icon.position);
            var t = new TutorialState { Kind = 2, Power = i };

            var glow = UI.Img(layer, "spark_glow", "TutGlow"); glow.color = new Color(1f, 0.9f, 0.3f, 0.55f);
            var gr = glow.rectTransform; gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f); gr.anchoredPosition = target; gr.sizeDelta = new Vector2(150, 150);
            gr.DOScale(1.25f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gr.gameObject);
            var ring = UI.Img(layer, "spark_ring", "TutRing"); ring.color = new Color(1, 1, 1, 0);
            var rr = ring.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f); rr.anchoredPosition = target; rr.sizeDelta = new Vector2(120, 120);
            t.Extras = glow.gameObject; ring.transform.SetParent(glow.transform, false); rr.anchoredPosition = Vector2.zero; rr.sizeDelta = new Vector2(120, 120);

            var hand = MakeHand(layer); t.Hand = hand;
            var handImg = hand.GetComponent<Image>();
            Vector2 from = target + new Vector2(90, -190), tip = target + new Vector2(6, -6);
            var seq = DOTween.Sequence().SetLoops(-1).SetUpdate(true).SetLink(layer.gameObject);
            seq.AppendCallback(() => { hand.anchoredPosition = from; hand.localScale = Vector3.one; handImg.color = new Color(1, 1, 1, 0); ring.color = new Color(1, 1, 1, 0); rr.localScale = Vector3.one * 0.6f; });
            seq.Append(handImg.DOFade(1f, 0.2f));
            seq.Join(hand.DOAnchorPos(tip, 0.6f).SetEase(Ease.OutCubic));
            seq.Append(hand.DOScale(0.82f, 0.16f));
            seq.AppendCallback(() => ring.color = new Color(1, 1, 1, 0.9f));
            seq.Append(rr.DOScale(1.6f, 0.45f).SetEase(Ease.OutCubic));
            seq.Join(ring.DOFade(0f, 0.45f));
            seq.Join(hand.DOScale(1f, 0.25f));
            seq.AppendInterval(0.5f);
            seq.Append(handImg.DOFade(0f, 0.2f));
            seq.AppendInterval(0.2f);
            t.Seq = seq;
            g.Tut = t;
            RefreshPowerBar();
            SetSlotMessage(PowerTutorialText[i], false);
        }

        RectTransform MakeHand(RectTransform layer)
        {
            var img = UI.Img(layer, "hand_point", "TutHand");
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.9f, 0.93f);   // the fingertip
            rt.sizeDelta = new Vector2(130, 153);
            return rt;
        }

        void EndTutorial()
        {
            var g = game; if (g == null || g.Tut == null) return;
            g.Tut.Seq?.Kill();
            if (g.Tut.Hand != null) Destroy(g.Tut.Hand.gameObject);
            if (g.Tut.Extras != null) Destroy(g.Tut.Extras);
            g.Tut = null;
            RefreshPowerBar();
            SetSlotMessage(null, false);
        }

        /// <summary>A word was found: the swipe lesson is over, a power-up lesson may follow.</summary>
        void TutorialWordFound()
        {
            var g = game; if (g == null || g.Tut == null || g.Tut.Kind != 1) return;
            EndTutorial();
            var d = SaveSystem.Data; d.swipeTutorialDone = true; Progress.Notify();
            DOVirtual.DelayedCall(1.4f, TryPowerTutorial).SetUpdate(true);
        }

        /// <summary>The taught power-up was used: unlock it for good.</summary>
        void TutorialPowerUsed(PowerUp pu)
        {
            var g = game; if (g == null || g.Tut == null || g.Tut.Kind != 2 || g.Tut.Power != (int)pu) return;
            EndTutorial();
            SaveSystem.Data.powerUnlocked[(int)pu] = true; Progress.Notify();
            RefreshPowerBar();
            Sfx.Play(Sfx.Kind.Level);
            SetSlotMessage(PowerUps.Names[(int)pu] + " unlocked!", false);
            DOVirtual.DelayedCall(2.4f, () => { if (game != null && game.Tut == null && game.Msg == PowerUps.Names[(int)pu] + " unlocked!") SetSlotMessage(null, false); }).SetUpdate(true);
        }
    }
}
