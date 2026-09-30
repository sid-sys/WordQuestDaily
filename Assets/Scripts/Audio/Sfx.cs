using System.Collections.Generic;
using UnityEngine;

namespace WordQuest
{
    /// <summary>
    /// Sound effects made in code (no audio files needed). Every sound is a tiny synthesized clip,
    /// so the app stays small and there is nothing to license.
    /// </summary>
    public static class Sfx
    {
        public enum Kind { Click, Tick, Cancel, Found, Mystery, Complete, Coin, Power, Error, Whoosh, Pop, Level }

        const int Rate = 44100;
        static AudioSource source;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static void Init(AudioSource src) { source = src; }

        public static void Play(Kind kind, float pitch = 1f, float volume = 1f)
        {
            if (source == null || SaveSystem.Data == null || !SaveSystem.Data.sound) return;
            source.pitch = 1f;
            source.PlayOneShot(Get(kind, pitch), volume);
        }

        /// <summary>Rising tick while dragging over letters: each letter is one step higher.</summary>
        public static void Tick(int step) => Play(Kind.Tick, Mathf.Pow(2f, Mathf.Min(step, 14) / 12f), 0.6f);

        public static void Buzz(bool strong = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (SaveSystem.Data != null && SaveSystem.Data.haptics) Handheld.Vibrate();
#endif
        }

        static AudioClip Get(Kind kind, float pitch)
        {
            string key = kind + "_" + Mathf.RoundToInt(pitch * 100);
            if (cache.TryGetValue(key, out var c)) return c;
            c = Build(kind, pitch);
            cache[key] = c;
            return c;
        }

        // --- tiny synth ---
        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        static void Tone(float[] b, float start, float dur, float f0, float f1, float vol, float decay = 6f, bool square = false)
        {
            int s = Mathf.RoundToInt(start * Rate), n = Mathf.RoundToInt(dur * Rate);
            double ph = 0;
            for (int i = 0; i < n && s + i < b.Length; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                ph += 2 * Mathf.PI * f / Rate;
                float w = square ? Mathf.Sign(Mathf.Sin((float)ph)) * 0.5f : Mathf.Sin((float)ph);
                float env = Mathf.Exp(-decay * t) * Mathf.Min(1f, i / 80f);
                b[s + i] += w * env * vol;
            }
        }

        static void Noise(float[] b, float start, float dur, float vol, float lowpass0, float lowpass1)
        {
            int s = Mathf.RoundToInt(start * Rate), n = Mathf.RoundToInt(dur * Rate);
            var rng = new System.Random(7);
            float y = 0;
            for (int i = 0; i < n && s + i < b.Length; i++)
            {
                float t = i / (float)n;
                float a = Mathf.Lerp(lowpass0, lowpass1, t);
                float x = (float)(rng.NextDouble() * 2 - 1);
                y += a * (x - y);
                b[s + i] += y * vol * Mathf.Sin(Mathf.PI * t);
            }
        }

        static AudioClip Build(Kind kind, float p)
        {
            float[] b;
            switch (kind)
            {
                case Kind.Click: b = Buf(0.07f); Tone(b, 0, 0.06f, 900 * p, 700 * p, 0.35f, 8); break;
                case Kind.Tick: b = Buf(0.06f); Tone(b, 0, 0.05f, 520 * p, 520 * p, 0.4f, 9); break;
                case Kind.Cancel: b = Buf(0.22f); Noise(b, 0, 0.2f, 0.7f, 0.5f, 0.05f); break;
                case Kind.Whoosh: b = Buf(0.4f); Noise(b, 0, 0.38f, 0.8f, 0.03f, 0.35f); break;
                case Kind.Pop: b = Buf(0.1f); Tone(b, 0, 0.09f, 300 * p, 700 * p, 0.6f, 7); break;
                case Kind.Found:
                    b = Buf(0.45f);
                    Tone(b, 0, 0.1f, 360 * p, 760 * p, 0.55f, 6);
                    Tone(b, 0.04f, 0.3f, 1320 * p, 1320 * p, 0.28f, 5);
                    Tone(b, 0.09f, 0.3f, 1760 * p, 1760 * p, 0.22f, 5);
                    Tone(b, 0.15f, 0.28f, 2349 * p, 2349 * p, 0.14f, 6);
                    break;
                case Kind.Mystery:
                    b = Buf(1.1f);
                    for (int i = 0; i < 8; i++) Tone(b, i * 0.07f, 0.35f, 500 * Mathf.Pow(2, i / 6f), 500 * Mathf.Pow(2, i / 6f), 0.2f, 4);
                    Tone(b, 0.6f, 0.5f, 1568, 1568, 0.3f, 3); Tone(b, 0.62f, 0.48f, 2093, 2093, 0.25f, 3); Tone(b, 0.64f, 0.46f, 2637, 2637, 0.18f, 3);
                    break;
                case Kind.Complete:
                    b = Buf(1.0f);
                    Tone(b, 0.00f, 0.3f, 523, 523, 0.4f, 3); Tone(b, 0.16f, 0.3f, 659, 659, 0.4f, 3);
                    Tone(b, 0.32f, 0.3f, 784, 784, 0.4f, 3); Tone(b, 0.5f, 0.5f, 1047, 1047, 0.45f, 2.5f);
                    Tone(b, 0.5f, 0.5f, 1319, 1319, 0.2f, 2.5f);
                    break;
                case Kind.Level:
                    b = Buf(0.9f);
                    for (int i = 0; i < 5; i++) Tone(b, i * 0.09f, 0.3f, 660 * Mathf.Pow(2, i * 2 / 12f), 660 * Mathf.Pow(2, i * 2 / 12f), 0.35f, 3);
                    break;
                case Kind.Coin:
                    b = Buf(0.35f);
                    Tone(b, 0, 0.18f, 1318 * p, 1318 * p, 0.4f, 6); Tone(b, 0.07f, 0.25f, 1760 * p, 1760 * p, 0.4f, 6);
                    break;
                case Kind.Power:
                    b = Buf(0.6f);
                    Noise(b, 0, 0.25f, 0.4f, 0.02f, 0.3f);
                    Tone(b, 0.15f, 0.4f, 700 * p, 1400 * p, 0.35f, 4); Tone(b, 0.25f, 0.3f, 1800 * p, 2200 * p, 0.2f, 5);
                    break;
                default:
                    b = Buf(0.25f); Tone(b, 0, 0.22f, 220, 150, 0.4f, 5, true); break;
            }
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i], -0.95f, 0.95f);
            var clip = AudioClip.Create(kind.ToString(), b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }
    }
}
