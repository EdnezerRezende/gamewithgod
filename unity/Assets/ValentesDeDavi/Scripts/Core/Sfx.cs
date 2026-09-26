using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Efeitos sonoros sintetizados em tempo de execução (provisórios até termos áudio gravado).
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;
        AudioSource[] sources;
        int next;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int Rate = 44100;

        public static void Create(Transform parent)
        {
            GameObject g = new GameObject("Sfx");
            g.transform.SetParent(parent, false);
            instance = g.AddComponent<Sfx>();
            instance.Build();
        }

        void Build()
        {
            sources = new AudioSource[10];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }
            System.Random r = new System.Random(7);
            clips["whoosh"] = Noise("whoosh", 0.18f, 700f, 0.05f, r);
            clips["throw"] = Noise("throw", 0.28f, 1500f, 0.01f, r);
            clips["clay"] = Mix("clay", Noise("a", 0.35f, 2600f, 0.005f, r), Tone("b", 330f, 0.12f, Wave.Triangle, 0f, 0.4f));
            clips["clang"] = Mix("clang", Tone("a", 540f, 1f, Wave.Sine, 0f, 1f), Tone("b", 1210f, 0.8f, Wave.Sine, 0f, 0.5f),
                                 Tone("c", 1830f, 0.6f, Wave.Sine, 0f, 0.33f), Tone("d", 2690f, 0.45f, Wave.Sine, 0f, 0.25f));
            clips["thud"] = Mix("thud", Tone("a", 95f, 0.22f, Wave.Sine, 45f, 1f), Noise("b", 0.1f, 300f, 0.005f, r));
            clips["wood"] = Mix("wood", Tone("a", 170f, 0.14f, Wave.Triangle, 0f, 1f), Noise("b", 0.08f, 900f, 0.005f, r));
            clips["roar"] = Mix("roar", Tone("a", 78f, 1.5f, Wave.Saw, 48f, 1f, 0.12f), Noise("b", 1.3f, 240f, 0.1f, r));
            clips["growl"] = Tone("growl", 120f, 0.8f, Wave.Saw, 70f, 0.8f, 0.06f);
            clips["tick"] = Tone("tick", 1900f, 0.05f, Wave.Sine, 0f, 0.4f);
            clips["perfect"] = Tone("perfect", 880f, 0.18f, Wave.Triangle, 0f, 0.6f);
            clips["hurt"] = Tone("hurt", 170f, 0.3f, Wave.Square, 80f, 0.5f);
            clips["bleat"] = Tone("bleat", 620f, 0.35f, Wave.Saw, 480f, 0.3f);
            clips["cheer"] = Noise("cheer", 3f, 900f, 0.6f, r);
            clips["dart"] = Noise("dart", 0.4f, 700f, 0.02f, r);
            clips["shofar"] = Tone("shofar", 147f, 1.6f, Wave.Saw, 222f, 0.7f, 0.15f);
            clips["heart"] = Tone("heart", 62f, 0.16f, Wave.Sine, 48f, 1f, 0.01f);
            clips["horn"] = Tone("horn", 196f, 1.8f, Wave.Saw, 262f, 0.6f, 0.2f);
        }

        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (instance == null) return;
            AudioClip c;
            if (!instance.clips.TryGetValue(name, out c)) return;
            AudioSource s = instance.sources[instance.next];
            instance.next = (instance.next + 1) % instance.sources.Length;
            s.pitch = pitch;
            s.PlayOneShot(c, volume);
        }

        enum Wave { Sine, Triangle, Saw, Square }

        static float Env(float t, float dur, float attack)
        {
            if (t < attack) return t / Mathf.Max(attack, 0.0001f);
            return Mathf.Exp(-5f * (t - attack) / Mathf.Max(dur - attack, 0.0001f));
        }

        static AudioClip Tone(string n, float freq, float dur, Wave w, float slideTo, float gain, float attack = 0.005f)
        {
            int len = Mathf.CeilToInt(dur * Rate);
            float[] d = new float[len];
            float ph = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float f = slideTo > 0f ? freq * Mathf.Pow(slideTo / freq, t / dur) : freq;
                ph += f / Rate;
                float p = ph - Mathf.Floor(ph), v;
                switch (w)
                {
                    case Wave.Triangle: v = 4f * Mathf.Abs(p - 0.5f) - 1f; break;
                    case Wave.Saw: v = 2f * p - 1f; break;
                    case Wave.Square: v = p < 0.5f ? 1f : -1f; break;
                    default: v = Mathf.Sin(p * Mathf.PI * 2f); break;
                }
                d[i] = v * gain * 0.35f * Env(t, dur, attack);
            }
            AudioClip c = AudioClip.Create(n, len, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>Ruído filtrado de forma simples (passa-banda aproximado) em torno de uma frequência.</summary>
        static AudioClip Noise(string n, float dur, float center, float attack, System.Random r)
        {
            int len = Mathf.CeilToInt(dur * Rate);
            float[] d = new float[len];
            float lp = 0f, hp = 0f, prev = 0f;
            float a = Mathf.Clamp01(2f * Mathf.PI * center * 1.6f / Rate);
            float b = Mathf.Clamp01(2f * Mathf.PI * center * 0.5f / Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float x = (float)(r.NextDouble() * 2.0 - 1.0);
                lp += a * (x - lp);
                hp = (1f - b) * (hp + lp - prev);
                prev = lp;
                d[i] = hp * 0.9f * Env(t, dur, attack);
            }
            AudioClip c = AudioClip.Create(n, len, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Mix(string n, params AudioClip[] parts)
        {
            int len = 0;
            foreach (AudioClip p in parts) len = Mathf.Max(len, p.samples);
            float[] d = new float[len];
            foreach (AudioClip p in parts)
            {
                float[] s = new float[p.samples];
                p.GetData(s, 0);
                for (int i = 0; i < s.Length; i++) d[i] += s[i];
            }
            AudioClip c = AudioClip.Create(n, len, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
