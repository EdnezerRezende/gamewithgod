using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino da fase 3, "o ritmo da espada": 1) bonecos pedem a sequência rápido, rápido, forte;
    /// 2) o instrutor ataca com um bastão (aparar ou desviar); 3) 40 s golpeando um poste sem deixar
    /// o Cansaço chegar ao máximo.
    /// </summary>
    public class SwordTraining : MonoBehaviour
    {
        class Dummy { public Transform root; public Material mat; }

        public PlayerController player;
        public EleazarSword sword;
        public UI ui;
        public Action<SwordTraining> onFinished;

        public int score, stage, parried, dodged;
        public string medal;
        readonly List<Dummy> dummies = new List<Dummy>();
        readonly List<GameObject> props = new List<GameObject>();
        readonly List<bool> seqHeavy = new List<bool>();
        int seqI, lit = -1, attacksLeft, defended;
        float litT, endT = -1f, enduranceT;
        bool running;

        static Vector3 C { get { return PasDamim.TrainingCenter; } }

        public void Begin()
        {
            score = parried = dodged = 0;
            Clear();
            running = true;
            sword.ResetState();
            sword.trainingMode = true;
            sword.extraSwing = SwingAtDummy;
            sword.onDefense = OnDefense;
            sword.onExhausted = () => { if (stage == 2) { score = Mathf.Max(0, score - 100); ui.Toast("Sem fôlego! Respire entre os golpes. −100", 1.6f); } };
            Foe.Ctx.instructorAttack = () => { if (attacksLeft <= 0) return false; attacksLeft--; return true; };
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            foreach (GameObject g in props) if (g != null) Destroy(g);
            props.Clear();
            dummies.Clear();
            foreach (Foe f in Foe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            if (sword != null) { sword.trainingMode = false; sword.extraSwing = null; sword.onDefense = null; sword.onExhausted = null; }
        }

        Dummy MakeDummy(Vector3 pos)
        {
            pos.y = PasDamim.Height(pos.x, pos.z);
            Transform root = new GameObject("Boneco").transform;
            root.position = pos;
            root.rotation = Quaternion.LookRotation(new Vector3(C.x - pos.x, 0f, C.z - pos.z));
            props.Add(root.gameObject);
            U.Cyl(root, new Vector3(0f, 0.8f, 0f), 0.05f, 1.6f, U.Hex(0x5a3f27));
            Material m = Mats.New(U.Hex(0xd9c27a));
            U.Cyl(root, new Vector3(0f, 1.2f, 0f), 0.28f, 0.9f, U.Hex(0xd9c27a)).GetComponent<Renderer>().sharedMaterial = m;
            U.Sph(root, new Vector3(0f, 1.8f, 0f), 0.18f, U.Hex(0xd9c27a)).GetComponent<Renderer>().sharedMaterial = m;
            Dummy d = new Dummy { root = root, mat = m };
            dummies.Add(d);
            return d;
        }

        void Setup(int n)
        {
            stage = n; endT = -1f;
            sword.fatigue = 0f;
            foreach (Foe f in Foe.All.ToArray()) if (f.training) Destroy(f.gameObject);
            if (n == 0)
            {
                ui.SetObjective("Treino 1 de 3: a sequência", "Acerte o boneco que brilhar com o golpe pedido: rápido, rápido, forte. A sequência completa acerta mais longe e mais forte.");
                for (int i = 0; i < 3; i++)
                {
                    float a = (i - 1) * 0.6f;
                    MakeDummy(C + new Vector3(Mathf.Sin(a) * 3f, 0f, Mathf.Cos(a) * 3f));
                }
                seqHeavy.Clear();
                for (int r = 0; r < 4; r++) { seqHeavy.Add(false); seqHeavy.Add(false); seqHeavy.Add(true); }
                seqI = 0; lit = -1; litT = 0.8f;
            }
            else if (n == 1)
            {
                ui.SetObjective("Treino 2 de 3: aparar e desviar", "O instrutor ataca com um bastão. Botão direito (Aparar) no instante do golpe, ou Espaço (Desviar).");
                foreach (Dummy d in dummies) Destroy(d.root.gameObject);
                dummies.Clear();
                attacksLeft = 8; defended = 0;
                Foe ins = Foe.Spawn(FoeType.Instrutor, C + new Vector3(0f, 0f, 4f), transform);
                ins.training = true;
                ins.state = Foe.St.Hold;
            }
            else
            {
                ui.SetObjective("Treino 3 de 3: o fôlego", "Golpeie o poste por 40 s sem deixar o Cansaço chegar ao máximo. Respire entre as sequências.");
                MakeDummy(C + new Vector3(0f, 0f, 2.2f));
                enduranceT = 40f;
            }
        }

        void SwingAtDummy(SwingKind kind)
        {
            if (stage == 0 && lit >= 0)
            {
                Dummy d = dummies[lit];
                if (!InReach(d)) return;
                bool heavy = kind != SwingKind.Quick, want = seqHeavy[seqI];
                if (heavy != want) { Sfx.Play("wood"); ui.Toast("Agora é golpe " + (want ? "forte" : "rápido") + ".", 1f); return; }
                Sfx.Play("thud");
                Fx.Burst(d.root.position + Vector3.up * 1.2f, U.Hex(0xd9c27a), 8, 2.4f);
                score += 80; seqI++;
                Mats.SetEmission(d.mat, Color.black); lit = -1; litT = 0.25f;
            }
            else if (stage == 2 && dummies.Count > 0 && InReach(dummies[0]))
            {
                Sfx.Play("thud", 0.7f);
                Fx.Burst(dummies[0].root.position + Vector3.up * 1.2f, U.Hex(0xd9c27a), 4, 2f);
                score += 10;
            }
        }

        bool InReach(Dummy d)
        {
            Vector3 dp = d.root.position - player.Position; dp.y = 0f;
            return dp.magnitude < 3.3f && sword.Facing(d.root.position, 60f);
        }

        void OnDefense(string kind)
        {
            if (stage != 1) return;
            defended++;
            if (kind == "parry") { score += 150; parried++; ui.Toast("Aparou! +150", 0.9f); }
            else if (kind == "dodge") { score += 100; dodged++; ui.Toast("Desviou +100", 0.9f); }
            else ui.Toast("O bastão acertou Eleazar.", 1f);
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (stage == 0)
            {
                litT -= dt;
                if (lit >= 0 && litT <= 0f)
                {
                    ui.Toast("Muito devagar!", 0.7f);
                    Mats.SetEmission(dummies[lit].mat, Color.black);
                    lit = -1; seqI++; litT = 0.4f;
                }
                else if (lit < 0 && litT <= 0f)
                {
                    if (seqI < seqHeavy.Count)
                    {
                        lit = UnityEngine.Random.Range(0, dummies.Count);
                        bool heavy = seqHeavy[seqI];
                        Mats.SetEmission(dummies[lit].mat, U.Hex(0xffc15a) * (heavy ? 1.4f : 0.8f));
                        litT = Difficulty.Current.level == DifficultyLevel.Valente ? 2f : 2.8f;
                        ui.Toast(heavy ? "Golpe forte" : "Golpe rápido", 1.2f);
                    }
                    else if (endT < 0f) endT = 1f;
                }
            }
            else if (stage == 1)
            {
                if (attacksLeft <= 0 && defended >= 8 && endT < 0f) endT = 1.2f;
            }
            else
            {
                enduranceT -= dt;
                ui.SetObjective("Treino 3 de 3: o fôlego", "Faltam " + Mathf.CeilToInt(Mathf.Max(0f, enduranceT)) + " s. Golpeie sem deixar o Cansaço chegar ao máximo.");
                if (enduranceT <= 0f && endT < 0f) endT = 0.8f;
            }
            if (endT > 0f)
            {
                endT -= dt;
                if (endT <= 0f) { if (stage < 2) Setup(stage + 1); else Finish(); }
            }
        }

        void Finish()
        {
            running = false;
            medal = score >= 2000 ? "ouro" : score >= 1300 ? "prata" : "bronze";
            Clear();
            sword.fatigue = 0f;
            if (onFinished != null) onFinished(this);
        }

        public int StartingCourage()
        {
            int c = medal == "ouro" ? 80 : medal == "prata" ? 68 : 55;
            return c + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
        }
    }
}
