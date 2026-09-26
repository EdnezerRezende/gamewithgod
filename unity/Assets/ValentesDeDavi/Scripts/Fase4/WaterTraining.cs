using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino da fase 4, diante da caverna: 1) levar os companheiros às marcas e mandar segurar;
    /// 2) passar por quatro marcas com o cântaro cheio sob flechas sem ponta; 3) proteger os companheiros
    /// de três instrutores e levantar quem cair.
    /// </summary>
    public class WaterTraining : MonoBehaviour
    {
        public PlayerController player;
        public ValenteArms arms;
        public UI ui;
        public Action<WaterTraining> onFinished;
        /// <summary>Pede ao jogo os companheiros novos (limpa os antigos).</summary>
        public Action spawnCompanions;

        public int score, stage, marks, waterLeft, revives;
        public string medal;
        readonly List<GameObject> props = new List<GameObject>();
        readonly List<Transform> rings = new List<Transform>(), gates = new List<Transform>();
        readonly List<Material> gateMats = new List<Material>();
        int ri, gi;
        float stT, endT = -1f;
        bool running;

        static Vector3 C { get { return Refaim.TrainingCenter; } }

        public void Begin()
        {
            score = marks = waterLeft = revives = 0;
            Clear();
            running = true;
            arms.ResetState();
            arms.trainingMode = true;
            arms.onDefense = OnDefense;
            spawnCompanions();
            foreach (Companion c in Companion.All) c.onRevived = () => { if (stage == 2) { score += 100; revives++; ui.Toast("Levantou o companheiro! +100", 1f); } };
            CampFoe.Ctx.onDown = f => { if (stage == 2 && f.training) { score += 150; ui.Toast("O instrutor se rendeu. +150", 1f); } };
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            foreach (GameObject g in props) if (g != null) Destroy(g);
            props.Clear(); rings.Clear(); gates.Clear(); gateMats.Clear();
            foreach (CampFoe f in CampFoe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            foreach (Arrow a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);
            if (arms != null) { arms.trainingMode = false; arms.onDefense = null; arms.carrying = false; }
        }

        Transform Ring(Vector3 pos)
        {
            Transform r = U.Pivot(null, "Marca", new Vector3(pos.x, Refaim.Height(pos.x, pos.z) + 0.05f, pos.z));
            Material m = Mats.New(U.Hex(0xecbd6a));
            Mats.SetEmission(m, U.Hex(0xecbd6a) * 0.8f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                GameObject s = U.Box(r, new Vector3(Mathf.Sin(a) * 2.6f, 0f, Mathf.Cos(a) * 2.6f), new Vector3(0.9f, 0.06f, 0.14f), Color.white);
                s.transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg + 90f, 0f);
                s.GetComponent<Renderer>().sharedMaterial = m;
            }
            r.gameObject.SetActive(false);
            props.Add(r.gameObject);
            return r;
        }

        Transform Gate(Vector3 pos)
        {
            Transform g = U.Pivot(null, "Marca do percurso", new Vector3(pos.x, Refaim.Height(pos.x, pos.z), pos.z));
            for (int s = -1; s <= 1; s += 2) U.Cyl(g, new Vector3(s * 1.3f, 1f, 0f), 0.06f, 2f, U.Hex(0x5a3f27));
            GameObject cloth = U.Box(g, new Vector3(0f, 1.9f, 0f), new Vector3(2.6f, 0.25f, 0.04f), U.Hex(0x6d5a3a));
            Material m = Mats.New(U.Hex(0x6d5a3a));
            cloth.GetComponent<Renderer>().sharedMaterial = m;
            gateMats.Add(m);
            props.Add(g.gameObject);
            return g;
        }

        void Setup(int n)
        {
            stage = n; endT = -1f; stT = 0f;
            foreach (CampFoe f in CampFoe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            arms.carrying = false; arms.water = 0f;
            foreach (Companion c in Companion.All) if (c.down) c.Revive();
            Companion.Follow = true;
            if (n == 0)
            {
                ui.SetObjective("Treino 1 de 3: ordens", "Leve os dois até a marca dourada e, lá dentro, mande segurar (Q ou Ordem). Depois chame de volta com \"Comigo\".");
                rings.Add(Ring(C + new Vector3(-8f, 0f, 6f))); rings.Add(Ring(C + new Vector3(8f, 0f, 3f))); rings.Add(Ring(C + new Vector3(0f, 0f, -8f)));
                ri = 0; rings[0].gameObject.SetActive(true);
            }
            else if (n == 1)
            {
                ui.SetObjective("Treino 2 de 3: o cântaro", "Passe pelas quatro marcas com o cântaro cheio. Correr, desviar e ser atingido derramam água. Botão direito apara as flechas sem ponta.");
                arms.carrying = true; arms.water = 100f;
                Vector2[] gp = { new Vector2(-8f, -8f), new Vector2(8f, -4f), new Vector2(-8f, 4f), new Vector2(8f, 9f) };
                foreach (Vector2 v in gp) gates.Add(Gate(C + new Vector3(v.x, 0f, v.y)));
                gi = 0; PaintGates();
                CampFoe a = CampFoe.Spawn(CampFoeType.Arqueiro, C + new Vector3(0f, 0f, 13f), transform, CampFoe.St.Advance);
                a.training = true; a.fixedPlace = true;
            }
            else
            {
                ui.SetObjective("Treino 3 de 3: os três juntos", "Três instrutores atacam os seus companheiros. Proteja-os; se um cair, chegue perto e segure E (Ação) para levantá-lo.");
                Vector2[] ip = { new Vector2(-6f, 8f), new Vector2(6f, 8f), new Vector2(0f, 11f) };
                foreach (Vector2 v in ip)
                {
                    CampFoe f = CampFoe.Spawn(CampFoeType.Instrutor, C + new Vector3(v.x, 0f, v.y), transform, CampFoe.St.Watch);
                    f.training = true;
                }
            }
        }

        void PaintGates()
        {
            for (int i = 0; i < gateMats.Count; i++)
                Mats.SetColor(gateMats[i], i == gi ? U.Hex(0xecbd6a) : i < gi ? U.Hex(0x93a35a) : U.Hex(0x6d5a3a));
        }

        void OnDefense(string kind)
        {
            if (kind == "parry") { int pts = stage == 2 ? 50 : 40; score += pts; ui.Toast("Aparou! +" + pts, 0.9f); }
            else if (kind == "hit" && stage == 1) ui.Toast("A flecha sem ponta derramou água.", 1f);
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            stT += dt;
            Vector3 pp = player.Position;

            if (stage == 0 && ri < rings.Count)
            {
                Vector3 c = rings[ri].position;
                bool inside = Flat(pp - c) < 3f;
                foreach (Companion a in Companion.All) if (Flat(a.transform.position - c) >= 3.6f) inside = false;
                if (inside && !Companion.Follow)
                {
                    int pts = 200 + Mathf.Max(0, Mathf.RoundToInt(100f - stT * 4f));
                    score += pts; marks++; ui.Toast("Seguraram a marca! +" + pts, 1.2f);
                    rings[ri].gameObject.SetActive(false);
                    ri++; stT = 0f;
                    if (ri < rings.Count) rings[ri].gameObject.SetActive(true); else endT = 1f;
                }
            }
            else if (stage == 1)
            {
                float lim = Mathf.Max(0f, 45f - stT);
                ui.SetObjective("Treino 2 de 3: o cântaro", "Marca " + Mathf.Min(gi + 1, 4) + " de 4 · " + Mathf.CeilToInt(lim) + " s · água " + Mathf.RoundToInt(arms.water) + "%. Correr, desviar e ser atingido derramam água.");
                if (gi < gates.Count && Flat(pp - gates[gi].position) < 1.8f)
                {
                    gi++; ui.Toast("Marca " + gi + " de 4", 0.8f); Sfx.Play("thud", 0.6f); PaintGates();
                }
                if ((gi >= 4 || lim <= 0f || arms.water <= 0f) && endT < 0f)
                {
                    int pts = gi * 60 + Mathf.RoundToInt(arms.water * 6f);
                    score += pts; waterLeft = Mathf.RoundToInt(arms.water);
                    ui.Toast("Percurso: " + gi + " de 4 marcas, " + waterLeft + "% de água. +" + pts, 2f);
                    endT = 1.2f;
                    foreach (CampFoe f in CampFoe.All.ToArray()) if (f.training) Destroy(f.gameObject);
                }
            }
            else if (stage == 2)
            {
                int left = 0;
                foreach (CampFoe f in CampFoe.All) if (f.training && f.Alive) left++;
                if ((left == 0 || stT > 60f) && endT < 0f)
                {
                    int pts = Mathf.Max(0, Mathf.RoundToInt(300f - stT * 5f));
                    score += pts; if (pts > 0) ui.Toast("Tempo +" + pts, 1f);
                    endT = 1.2f;
                }
            }
            if (endT > 0f)
            {
                endT -= dt;
                if (endT <= 0f) { if (stage < 2) Setup(stage + 1); else Finish(); }
            }
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        void Finish()
        {
            running = false;
            medal = score >= 2000 ? "ouro" : score >= 1300 ? "prata" : "bronze";
            Clear();
            if (onFinished != null) onFinished(this);
        }

        public int StartingCourage()
        {
            int c = medal == "ouro" ? 80 : medal == "prata" ? 68 : 55;
            return c + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
        }
    }
}
