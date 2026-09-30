using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordQuest
{
    /// <summary>Small animation helpers (tweens, particle bursts, flying coins, floating text). No plugins needed.</summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        void Awake() { I = this; }

        // ---------- tweens ----------
        public static float OutCubic(float t) { t = 1 - Mathf.Clamp01(t); return 1 - t * t * t; }
        public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        public static float OutBack(float t) { t = Mathf.Clamp01(t); float c = 1.70158f; return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2); }

        public static Coroutine Run(IEnumerator e) => I != null ? I.StartCoroutine(e) : null;
        public static void Stop(Coroutine c) { if (I != null && c != null) I.StopCoroutine(c); }

        public static Coroutine Tween(float dur, Action<float> step, Action done = null, float delay = 0f)
        {
            return Run(TweenRoutine(dur, step, done, delay));
        }

        static IEnumerator TweenRoutine(float dur, Action<float> step, Action done, float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            float t = 0;
            while (t < dur)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
            done?.Invoke();
        }

        public static void PopIn(RectTransform rt, float dur = 0.35f, float delay = 0f)
        {
            if (rt == null) return;
            rt.localScale = Vector3.zero;
            Tween(dur, t => { if (rt != null) rt.localScale = Vector3.one * OutBack(t); }, null, delay);
        }

        public static void Punch(RectTransform rt, float amount = 0.18f, float dur = 0.3f)
        {
            if (rt == null) return;
            Vector3 b = rt.localScale;
            Tween(dur, t => { if (rt != null) rt.localScale = b * (1 + amount * Mathf.Sin(t * Mathf.PI)); }, () => { if (rt != null) rt.localScale = b; });
        }

        public static void Fade(CanvasGroup g, float to, float dur = 0.25f, Action done = null)
        {
            float from = g.alpha;
            Tween(dur, t => { if (g != null) g.alpha = Mathf.Lerp(from, to, t); }, done);
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
            var sp = Art.Get(sprite);
            for (int i = 0; i < count; i++)
            {
                var img = UI.Icon(layer, sprite, size * UnityEngine.Random.Range(0.6f, 1.2f), "P");
                img.color = color; img.sprite = sp;
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

        /// <summary>Small icons fly from one point to another in a curve, then `onArrive` is called once per icon.</summary>
        public static void Fly(Transform layer, Vector2 from, Vector2 to, string sprite, int count, Action onEach = null, Action onAll = null, float size = 64f, float duration = 0.9f, float stagger = 0.06f)
        {
            if (I == null) { onAll?.Invoke(); return; }
            int left = count;
            for (int i = 0; i < count; i++)
            {
                var img = UI.Icon(layer, sprite, size, "Fly");
                var rt = img.rectTransform; rt.anchoredPosition = from;
                Vector2 mid = (from + to) / 2 + new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(60f, 320f));
                Tween(duration, t =>
                {
                    if (rt == null) return;
                    float e = InOut(t);
                    Vector2 a = Vector2.Lerp(from, mid, e), b = Vector2.Lerp(mid, to, e);
                    rt.anchoredPosition = Vector2.Lerp(a, b, e);
                    rt.localScale = Vector3.one * (t < 0.2f ? OutBack(t / 0.2f) : Mathf.Lerp(1f, 0.7f, t));
                }, () =>
                {
                    if (rt != null) Destroy(rt.gameObject);
                    onEach?.Invoke();
                    if (--left == 0) onAll?.Invoke();
                }, i * stagger);
            }
            if (count == 0) onAll?.Invoke();
        }

        public static void FloatText(Transform layer, Vector2 pos, string text, Color color, int size = 56)
        {
            var t = UI.Label(layer, text, size, color, TextAnchor.MiddleCenter, true);
            var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500, 90);
            Tween(0.9f, k => { if (rt == null) return; rt.anchoredPosition = pos + new Vector2(0, 110 * OutCubic(k)); var c = t.color; c.a = 1 - Mathf.Clamp01((k - 0.6f) / 0.4f); t.color = c; rt.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(k * Mathf.PI)); },
                () => { if (rt != null) Destroy(rt.gameObject); });
        }

        /// <summary>Counts a number text from `from` to `to`.</summary>
        public static void CountUp(Text label, int from, int to, string format = "{0}", float dur = 0.8f)
        {
            Tween(dur, t => { if (label != null) label.text = string.Format(format, Mathf.RoundToInt(Mathf.Lerp(from, to, OutCubic(t)))); });
        }
    }
}
