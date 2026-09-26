using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Trilha de fundo gerada no início do jogo: tambor de mão, alaúde e bordão grave, na escala de Ré
    /// "hijaz" (sabor do Oriente Médio). Cada clima é um trecho em laço; a troca entre climas é suave.
    /// Provisória até termos a trilha gravada.
    /// </summary>
    public class Music : MonoBehaviour
    {
        class Mood
        {
            public float bpm, melody, drone;
            public string drum, slap;
            public bool major;
        }

        static Music instance;
        const int Rate = 44100;
        const float Volume = 0.5f;
        static readonly int[] Hijaz = { 0, 1, 4, 5, 7, 8, 10 }, Major = { 0, 2, 4, 5, 7, 9, 11 };

        readonly Dictionary<string, Mood> moods = new Dictionary<string, Mood>
        {
            { "menu",     new Mood { bpm = 70,  drum = "x.....x.....x...", slap = "....x.......x...", melody = 0.35f, drone = 0.10f } },
            { "cine",     new Mood { bpm = 62,  drum = "x...............", slap = "................", melody = 0.22f, drone = 0.13f } },
            { "training", new Mood { bpm = 96,  drum = "x..x..x.x..x..x.", slap = "..x.x.....x.x.x.", melody = 0.5f,  drone = 0.07f } },
            { "duel",     new Mood { bpm = 120, drum = "x.x...x.x.x..xx.", slap = ".x.xx..x.x.xx.xx", melody = 0.6f,  drone = 0.13f } },
            { "victory",  new Mood { bpm = 84,  drum = "x...x...x...x.x.", slap = "..x...x...x...xx", melody = 0.75f, drone = 0.10f, major = true } },
            { "quiet",    new Mood { bpm = 60,  drum = "................", slap = "................", melody = 0.15f, drone = 0.08f } },
        };
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource a, b;
        AudioSource current;
        string mood;

        public static void Create(Transform parent)
        {
            GameObject g = new GameObject("Música");
            g.transform.SetParent(parent, false);
            instance = g.AddComponent<Music>();
            instance.a = g.AddComponent<AudioSource>();
            instance.b = g.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { instance.a, instance.b })
            {
                s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.ignoreListenerPause = true;
            }
            System.Random r = new System.Random(17);
            foreach (KeyValuePair<string, Mood> kv in instance.moods) instance.clips[kv.Key] = Render(kv.Key, kv.Value, r);
        }

        static bool ducked;

        /// <summary>Abaixa a música enquanto a narração fala.</summary>
        public static void Duck(bool on) { ducked = on; }

        public static void Set(string name)
        {
            if (instance == null || instance.mood == name) return;
            string prev = instance.mood;
            instance.mood = name;
            AudioSource next = instance.current == instance.a ? instance.b : instance.a;
            next.clip = instance.clips[name];
            next.volume = 0f;
            next.Play();
            instance.current = next;
            if ((name == "duel" || name == "victory") && prev != name) Sfx.Play("shofar", 0.9f);
        }

        void Update()
        {
            float target = Settings.Music ? Volume * (ducked ? 0.35f : 1f) : 0f, k = Mathf.Clamp01(Time.unscaledDeltaTime / 1.2f);
            foreach (AudioSource s in new[] { a, b })
            {
                float goal = s == current ? target : 0f;
                s.volume = Mathf.MoveTowards(s.volume, goal, k * Volume);
                if (s != current && s.isPlaying && s.volume <= 0.001f) s.Stop();
            }
        }

        // ------------------------------------------------------------------ síntese

        static float Freq(Mood m, int i)
        {
            int[] sc = m.major ? Major : Hijaz;
            return 146.83f * Mathf.Pow(2f, (sc[i % 7] + 12 * (i / 7)) / 12f);
        }

        static AudioClip Render(string name, Mood m, System.Random r)
        {
            const int Bars = 4;
            float step = 60f / m.bpm / 4f;
            int steps = 16 * Bars, len = Mathf.CeilToInt(steps * step * Rate);
            float[] d = new float[len];

            // Bordão: Ré e Lá graves com filtro que "respira" devagar.
            float lp = 0f, p1 = 0f, p2 = 0f, p3 = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                p1 += 73.42f / Rate; p2 += 110f / Rate; p3 += 146.83f / Rate;
                float tri = 4f * Mathf.Abs(p1 - Mathf.Floor(p1) - 0.5f) - 1f;
                float saw = 2f * (p2 - Mathf.Floor(p2)) - 1f + 0.35f * (2f * (p3 - Mathf.Floor(p3)) - 1f);
                float cut = 480f + 180f * Mathf.Sin(t * 2f * Mathf.PI * 0.07f);
                float alpha = Mathf.Clamp01(2f * Mathf.PI * cut / Rate);
                lp += alpha * ((tri + saw) - lp);
                d[i] += lp * m.drone * 0.9f;
            }

            int idx = 4;
            for (int s = 0; s < steps; s++)
            {
                int start = Mathf.FloorToInt(s * step * Rate), ps = s % 16;
                if (m.drum[ps] == 'x') AddTone(d, start, ps % 8 == 0 ? 105f : 90f, 42f, 0.32f, ps % 8 == 0 ? 0.55f : 0.38f, 0);
                if (m.slap[ps] == 'x') AddNoise(d, start, 0.07f, 0.16f, r);
                if (ps % 2 == 0 && r.NextDouble() < m.melody * (ps % 4 == 0 ? 1f : 0.55f))
                {
                    int[] moves = { -2, -1, -1, 0, 1, 1, 2 };
                    idx = Mathf.Clamp(idx + moves[r.Next(moves.Length)], 2, 12);
                    float f = Freq(m, idx);
                    AddTone(d, start, f, 0f, 0.55f, 0.16f, 1);
                    AddTone(d, start, f * 2f, 0f, 0.18f, 0.03f, 2);
                }
            }

            float peak = 0.0001f;
            for (int i = 0; i < len; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float norm = 0.8f / peak;
            for (int i = 0; i < len; i++) d[i] *= norm;
            AudioClip c = AudioClip.Create("Música " + name, len, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>wave: 0 seno (tambor), 1 triângulo (alaúde), 2 dente de serra (brilho).</summary>
        static void AddTone(float[] d, int start, float freq, float slideTo, float dur, float gain, int wave)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = (start + i) % d.Length;   // o fim do laço volta para o início
                float t = i / (float)Rate;
                float f = slideTo > 0f ? freq * Mathf.Pow(slideTo / freq, t / dur) : freq;
                ph += f / Rate;
                float p = ph - Mathf.Floor(ph), v;
                if (wave == 1) v = 4f * Mathf.Abs(p - 0.5f) - 1f;
                else if (wave == 2) v = 2f * p - 1f;
                else v = Mathf.Sin(p * Mathf.PI * 2f);
                float env = t < 0.005f ? t / 0.005f : Mathf.Exp(-5f * t / dur);
                d[j] += v * gain * env;
            }
        }

        static void AddNoise(float[] d, int start, float dur, float gain, System.Random r)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            float lp = 0f, prev = 0f, hp = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = (start + i) % d.Length;
                float x = (float)(r.NextDouble() * 2.0 - 1.0);
                lp += 0.4f * (x - lp);
                hp = 0.8f * (hp + lp - prev);
                prev = lp;
                d[j] += hp * gain * Mathf.Exp(-5f * i / (float)n);
            }
        }
    }
}
