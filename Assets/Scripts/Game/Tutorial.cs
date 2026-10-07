using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>Hand tutorial: level 1 teaches swiping across the letters.</summary>
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
        void BeginTutorials()
        {
            var g = game; if (g == null || g.Spec.IsDaily) return;
            var d = SaveSystem.Data;
            if (!d.swipeTutorialDone) { DOVirtual.DelayedCall(0.7f, StartSwipeTutorial).SetUpdate(true); return; }
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
            SetSlotMessage(null, false);
        }

        /// <summary>A word was found: the swipe lesson is over, a power-up lesson may follow.</summary>
        void TutorialWordFound()
        {
            var g = game; if (g == null || g.Tut == null || g.Tut.Kind != 1) return;
            EndTutorial();
            var d = SaveSystem.Data; d.swipeTutorialDone = true; Progress.Notify();
        }
    }
}
