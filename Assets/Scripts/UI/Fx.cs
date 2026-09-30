using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>Animation helpers built on DOTween (pop-ins, punches, fades, counters, flying coins) plus a tiny particle system.</summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        void Awake()
        {
            I = this;
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(800, 100);
        }

        // ---------- easing (kept for callers that build their own curves) ----------
        public static float OutCubic(float t) { t = 1 - Mathf.Clamp01(t); return 1 - t * t * t; }
        public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        public static float OutBack(float t) { t = Mathf.Clamp01(t); float c = 1.70158f; return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2); }

        public static Coroutine Run(IEnumerator e) => I != null ? I.StartCoroutine(e) : null;

        /// <summary>Runs `step(0..1)` over `dur` seconds (real time). Optional delay and completion callback.</summary>
        public static Tween Tween(float dur, Action<float> step, Action done = null, float delay = 0f)
        {
            var t = DOVirtual.Float(0f, 1f, Mathf.Max(0.001f, dur), v => step(v)).SetDelay(delay).SetUpdate(true).SetEase(Ease.Linear);
            if (done != null) t.OnComplete(() => done());
            return t;
        }

        public static void PopIn(RectTransform rt, float dur = 0.35f, float delay = 0f)
        {
            if (rt == null) return;
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, dur).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).SetLink(rt.gameObject);
        }

        public static void Punch(RectTransform rt, float amount = 0.18f, float dur = 0.3f)
        {
            if (rt == null) return;
            rt.DOKill(true);
            rt.DOPunchScale(Vector3.one * amount, dur, 1, 0.5f).SetUpdate(true).SetLink(rt.gameObject);
        }

        public static void Fade(CanvasGroup g, float to, float dur = 0.25f, Action done = null)
        {
            var t = g.DOFade(to, dur).SetUpdate(true).SetLink(g.gameObject);
            if (done != null) t.OnComplete(() => done());
        }

        /// <summary>Counts a number text from `from` to `to`.</summary>
        public static Tween CountUp(Text label, int from, int to, string format = "{0}", float dur = 0.8f)
        {
            return DOVirtual.Int(from, to, dur, v => { if (label != null) label.text = string.Format(format, v); }).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(label.gameObject);
        }

        // ---------- particles ----------
        class P { public RectTransform rt; public Image img; public Vector2 vel; public float life, age, gravity, spin, startScale; public Color color; }
        readonly List<P> ps = new List<P>();

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int i = ps.Count - 1; i >= 0; i--)
            {
                var p = ps[i];
                p.age += dt;
                if (p.rt == null || p.age >= p.life) { if (p.rt != null) Destroy(p.rt.gameObject); ps.RemoveAt(i); continue; }
                p.vel.y -= p.gravity * dt;
                p.rt.anchoredPosition += p.vel * dt;
                p.rt.localRotation = Quaternion.Euler(0, 0, p.rt.localEulerAngles.z + p.spin * dt);
                float t = p.age / p.life;
                p.rt.localScale = Vector3.one * p.startScale * (t < 0.15f ? t / 0.15f : Mathf.Lerp(1f, 0.2f, (t - 0.15f) / 0.85f));
                var c = p.color; c.a *= t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f; p.img.color = c;
            }
        }

        public static void Burst(Transform layer, Vector2 pos, string sprite, Color color, int count, float speed = 420f, float life = 0.7f, float size = 46f, float gravity = 500f)
        {
            if (I == null || layer == null) return;
            for (int i = 0; i < count; i++)
            {
                var img = UI.Icon(layer, sprite, size * UnityEngine.Random.Range(0.6f, 1.2f), "P");
                img.color = color;
                var rt = img.rectTransform; rt.anchoredPosition = pos;
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2), s = speed * UnityEngine.Random.Range(0.35f, 1f);
                I.ps.Add(new P { rt = rt, img = img, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, life = life * UnityEngine.Random.Range(0.7f, 1.2f), gravity = gravity, spin = UnityEngine.Random.Range(-240f, 240f), startScale = 1f, color = color });
            }
        }

        public static void Confetti(Transform layer, float width, float top, int count = 60)
        {
            if (I == null) return;
            string[] keys = { "conf_pink", "conf_yellow", "conf_blue" };
            for (int i = 0; i < count; i++)
            {
                var img = UI.Icon(layer, keys[i % 3], UnityEngine.Random.Range(28f, 46f), "C");
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-width / 2, width / 2), top);
                I.ps.Add(new P { rt = rt, img = img, vel = new Vector2(UnityEngine.Random.Range(-160f, 160f), UnityEngine.Random.Range(-200f, 300f)), life = UnityEngine.Random.Range(1.6f, 2.6f), gravity = 260f, spin = UnityEngine.Random.Range(-360f, 360f), startScale = 1f, color = Color.white });
            }
        }

        /// <summary>Small icons fly from one point to another along a curve; `onEach` fires per icon, `onAll` at the end.</summary>
        public static void Fly(Transform layer, Vector2 from, Vector2 to, string sprite, int count, Action onEach = null, Action onAll = null, float size = 64f, float duration = 0.9f, float stagger = 0.06f)
        {
            if (I == null || count <= 0) { onAll?.Invoke(); return; }
            int left = count;
            for (int i = 0; i < count; i++)
            {
                var img = UI.Icon(layer, sprite, size, "Fly");
                var rt = img.rectTransform; rt.anchoredPosition = from; rt.localScale = Vector3.zero;
                Vector2 mid = (from + to) / 2 + new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(60f, 320f));
                DOVirtual.Float(0f, 1f, duration, t =>
                {
                    if (rt == null) return;
                    Vector2 a = Vector2.Lerp(from, mid, t), b = Vector2.Lerp(mid, to, t);
                    rt.anchoredPosition = Vector2.Lerp(a, b, t);
                    rt.localScale = Vector3.one * (t < 0.2f ? OutBack(t / 0.2f) : Mathf.Lerp(1f, 0.7f, t));
                }).SetDelay(i * stagger).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(rt.gameObject).OnComplete(() =>
                {
                    if (rt != null) Destroy(rt.gameObject);
                    onEach?.Invoke();
                    if (--left == 0) onAll?.Invoke();
                });
            }
        }

        public static void FloatText(Transform layer, Vector2 pos, string text, Color color, int size = 56)
        {
            var t = UI.Label(layer, text, size, color, TextAnchor.MiddleCenter, true);
            var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500, 90); rt.localScale = Vector3.zero;
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(rt.gameObject);
            seq.Append(rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            seq.Join(rt.DOAnchorPosY(pos.y + 110, 0.9f).SetEase(Ease.OutCubic));
            seq.Insert(0.55f, t.DOFade(0f, 0.35f));
            seq.OnComplete(() => { if (rt != null) Destroy(rt.gameObject); });
        }
    }
}
