using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Fase 1, "Chegada ao acampamento" (1 Samuel 17:17-30): Davi chega com grãos tostados, pães e queijos para os
    /// irmãos e anda em primeira pessoa entre tendas e fogueiras. Os soldados comentam a recompensa do rei e Eliabe
    /// o repreende; depois, Davi é levado a Saul (a cena seguinte). Textos provisórios (Almeida, domínio público).
    /// </summary>
    public class Camp : MonoBehaviour
    {
        public static readonly Vector3 Center = new Vector3(0f, 0f, -40f);
        public const float Radius = 21f;

        class Line { public string who, quote, reference; }
        class Npc { public string id; public Figure fig; public bool said; }

        public PlayerController player;
        public UI ui;
        public Goliath goliath;
        public Action onFinished;

        Transform root, ring;
        readonly List<Npc> npcs = new List<Npc>();
        readonly List<Transform> flames = new List<Transform>();
        readonly Queue<Line> queue = new Queue<Line>();
        float talkT, endT = -1f;
        bool running, eliabeDone;

        static readonly Dictionary<string, Line[]> Talk = new Dictionary<string, Line[]>
        {
            { "s1", new[] { new Line { who = "Soldado", quote = "Vistes aquele homem que subiu? Subiu para afrontar a Israel; e ao que o ferir, o rei o enriquecerá com grandes riquezas,", reference = "1 Samuel 17:25" } } },
            { "s2", new[] {
                new Line { who = "Soldado", quote = "...e lhe dará sua filha, e a casa de seu pai fará livre em Israel.", reference = "1 Samuel 17:25" },
                new Line { who = "Davi", quote = "Quem é, pois, este filisteu incircunciso, para afrontar os exércitos do Deus vivo?", reference = "1 Samuel 17:26" } } },
            { "s3", new[] { new Line { who = "Soldado", quote = "Quarenta dias que ele desafia de manhã e de tarde, e ninguém sobe contra ele. O medo tomou o acampamento.", reference = "1 Samuel 17:16, 24" } } },
            { "eliabe", new[] {
                new Line { who = "Eliabe", quote = "Por que desceste aqui? E com quem deixaste aquelas poucas ovelhas no deserto? Bem conheço a tua soberba, e a malícia do teu coração.", reference = "1 Samuel 17:28" },
                new Line { who = "Davi", quote = "Que fiz eu agora? Não foi isto só uma palavra?", reference = "1 Samuel 17:29" } } },
        };

        public static Camp Build(Transform parent)
        {
            GameObject g = new GameObject("Acampamento de Israel");
            g.transform.SetParent(parent, false);
            Camp c = g.AddComponent<Camp>();
            c.root = g.transform;
            c.BuildScenery();
            g.SetActive(false);
            return c;
        }

        static Vector3 At(float x, float z)
        {
            Vector3 p = Center + new Vector3(x, 0f, z);
            p.y = World.ValleyHeight(p.x, p.z);
            return p;
        }

        void BuildScenery()
        {
            System.Random r = new System.Random(5);
            float[,] tents = { { -12, -6 }, { -14, 6 }, { 12, -8 }, { 14, 5 }, { -5, 13 }, { 7, 14 }, { -3, -12 } };
            for (int i = 0; i < tents.GetLength(0); i++) Tent(At(tents[i, 0], tents[i, 1]), 1f, false, (float)r.NextDouble() * 40f);
            Tent(At(5f, 8f), 1.3f, true, 10f);   // a tenda de Eliabe
            float[,] fires = { { -5, -3 }, { 4, -4 }, { -8, 8 } };
            for (int i = 0; i < fires.GetLength(0); i++)
            {
                Vector3 p = At(fires[i, 0], fires[i, 1]);
                for (int k = 0; k < 7; k++)
                {
                    float a = k / 7f * Mathf.PI * 2f;
                    U.Sph(root, p + new Vector3(Mathf.Cos(a) * 0.5f, 0.1f, Mathf.Sin(a) * 0.5f), 0.16f, U.Hex(0x7d7566));
                }
                GameObject fl = U.Prim(PrimitiveType.Capsule, root, p + Vector3.up * 0.5f, new Vector3(0.45f, 0.45f, 0.45f), Color.white);
                fl.GetComponent<Renderer>().sharedMaterial = Refaim.FlameMat;
                flames.Add(fl.transform);
            }
            // (id, x, z, túnica, direção em radianos, mais alto)
            object[][] people =
            {
                new object[] { "s1", -4f, -6f, 0x6f6a3c, 0.6f, false }, new object[] { "s2", 6f, -1f, 0x7d5f3a, -0.9f, false }, new object[] { "s3", -9f, 4f, 0x8a7a52, 1.4f, false },
                new object[] { "eliabe", 3f, 5f, 0x3d5a8a, 2.9f, true },
                new object[] { "", -2f, 6f, 0x5e5638, 0.3f, false }, new object[] { "", 10f, -5f, 0x6f6a3c, 2.2f, false }, new object[] { "", -11f, -2f, 0x7d5f3a, -0.4f, false },
            };
            foreach (object[] n in people)
            {
                string id = (string)n[0];
                Figure f = Figure.Man(root, id == "eliabe" ? "Eliabe" : "Soldado de Israel", U.Hex((int)n[3]), false, (bool)n[5] ? 1.82f : 1.75f);
                Vector3 p = At((float)n[1], (float)n[2]);
                f.root.position = p;
                f.root.rotation = Quaternion.Euler(0f, (float)n[4] * Mathf.Rad2Deg, 0f);
                npcs.Add(new Npc { id = id, fig = f });
            }
            ring = U.Cyl(root, Vector3.zero, 0.6f, 0.02f, U.Hex(0xffd166)).transform;
            Mats.SetEmission(ring.GetComponent<Renderer>().sharedMaterial, U.Hex(0xffd166));
        }

        void Tent(Vector3 p, float scale, bool red, float yaw)
        {
            Transform t = U.Pivot(root, "Tenda", p);
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            // Pirâmide de quatro lados: uma pilha de caixas que estreita para cima.
            Color c = U.Hex(0xd9c9a0);
            for (int i = 0; i < 4; i++)
            {
                float w = (3.0f - i * 0.7f) * scale, h = 0.65f * scale;
                U.Box(t, new Vector3(0f, h * (i + 0.5f), 0f), new Vector3(w, h, w), c);
            }
            if (red) U.Box(t, new Vector3(0f, 0.3f * scale, 0f), new Vector3(3.15f * scale, 0.3f * scale, 3.15f * scale), U.Hex(0x8c2f22));
        }

        public void Begin()
        {
            gameObject.SetActive(true);
            queue.Clear(); talkT = 0f; endT = -1f; eliabeDone = false; running = true;
            foreach (Npc n in npcs) n.said = false;
            Npc e = npcs.Find(n => n.id == "eliabe");
            ring.position = e.fig.root.position + Vector3.up * 0.06f;
            ring.gameObject.SetActive(true);
            ui.SetObjective("Chegada ao acampamento", "Davi trouxe grãos tostados, pães e queijos aos irmãos (17:17-18). Leve o farnel a Eliabe, na tenda com a faixa vermelha. Ouça os soldados no caminho.");
            Sfx.Play("roar", 0.5f, 1.3f);
            player.shake = 0.8f;
            ui.Toast("Ao longe, Golias grita o desafio.", 2.4f);
        }

        public void Hide() { running = false; queue.Clear(); gameObject.SetActive(false); }

        void Update()
        {
            if (!running || Game.Paused) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            goliath.Pace(Time.time);
            for (int i = 0; i < flames.Count; i++)
                flames[i].localScale = new Vector3(0.45f + 0.05f * Mathf.Sin(Time.time * 7f + i), 0.45f + 0.12f * Mathf.Sin(Time.time * 9f + i * 2f), 0.45f + 0.05f * Mathf.Sin(Time.time * 7f + i));
            ring.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.time * 4f));
            ring.gameObject.SetActive(!eliabeDone);

            if (talkT > 0f) { talkT -= dt; if (talkT <= 0f) { if (queue.Count == 0 && eliabeDone && endT < 0f) endT = 1.2f; } }
            if (endT > 0f)
            {
                endT -= dt;
                if (endT <= 0f) { running = false; Action f = onFinished; Hide(); if (f != null) f(); }
                return;
            }
            if (talkT <= 0f && queue.Count > 0) Show(queue.Dequeue());
            if (talkT <= 0f && queue.Count == 0)
            {
                foreach (Npc n in npcs)
                {
                    if (string.IsNullOrEmpty(n.id) || n.said) continue;
                    Vector3 d = player.Position - n.fig.root.position; d.y = 0f;
                    if (d.magnitude >= 4.2f) continue;
                    n.said = true;
                    n.fig.root.rotation = Quaternion.Euler(0f, U.YawTo(d.x, d.z), 0f);
                    foreach (Line l in Talk[n.id]) queue.Enqueue(l);
                    if (n.id == "eliabe") { eliabeDone = true; ui.SetObjective("Chegada ao acampamento", "\"Davi é levado a Saul.\""); }
                    break;
                }
            }
        }

        void Show(Line l)
        {
            int words = l.quote.Split(' ').Length;
            talkT = Mathf.Max(4.2f, Settings.Narration ? words * 0.45f + 2.5f : 4.2f);
            ui.Toast(l.who.ToUpperInvariant() + "\n“" + l.quote + "”\n" + l.reference, talkT);
        }
    }
}
