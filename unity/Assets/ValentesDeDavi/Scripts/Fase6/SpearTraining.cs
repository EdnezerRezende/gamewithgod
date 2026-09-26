using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino da fase 6, "a lança do chefe": 1) varrer grupos de bonecos com o golpe forte; 2) estocar dois
    /// bonecos em fila de uma vez; 3) aparar o golpe que o instrutor dá num boneco "Davi".
    /// </summary>
    public class SpearTraining : MonoBehaviour
    {
        class Dummy { public Transform root; public Material mat; public int group; public bool lit, hit; }

        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Action<SpearTraining> onFinished;

        public int score, stage, sweeps, lines, blocks;
        public string medal;
        readonly List<Dummy> dummies = new List<Dummy>();
        Figure inst;
        Transform davidDummy;
        float groupT, endT = -1f, it;
        int rounds, strikes;
        bool running, winding;

        static Vector3 C { get { return Battlefield.Training; } }

        public void Begin()
        {
            score = sweeps = lines = blocks = 0;
            Clear();
            running = true;
            arms.trainingMode = true;
            arms.onSwing = OnSwing;
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            foreach (Dummy d in dummies) if (d.root != null) Destroy(d.root.gameObject);
            dummies.Clear();
            if (inst != null) { Destroy(inst.root.gameObject); inst = null; }
            if (davidDummy != null) { Destroy(davidDummy.gameObject); davidDummy = null; }
            if (arms != null) { arms.trainingMode = false; if (arms.onSwing == (Action<SwingKind>)OnSwing) arms.onSwing = null; }
        }

        Dummy MakeDummy(Vector3 pos, Color color)
        {
            pos.y = Battlefield.Height(pos.x, pos.z);
            Transform root = new GameObject("Boneco").transform;
            root.SetParent(transform, false);
            root.position = pos;
            U.Cyl(root, new Vector3(0f, 0.8f, 0f), 0.05f, 1.6f, U.Hex(0x5a3f27));
            Material m = Mats.New(color);
            U.Cyl(root, new Vector3(0f, 1.2f, 0f), 0.28f, 0.9f, color).GetComponent<Renderer>().sharedMaterial = m;
            U.Sph(root, new Vector3(0f, 1.8f, 0f), 0.18f, color).GetComponent<Renderer>().sharedMaterial = m;
            return new Dummy { root = root, mat = m };
        }

        void LightGroup(int g)
        {
            foreach (Dummy d in dummies) { d.lit = d.group == g; d.hit = false; Mats.SetEmission(d.mat, d.lit ? U.Hex(0xffc15a) : Color.black); }
            groupT = Difficulty.Current.level == DifficultyLevel.Valente ? 3f : 4f;
        }

        void Setup(int n)
        {
            stage = n; endT = -1f; rounds = 0;
            foreach (Dummy d in dummies) if (d.root != null) Destroy(d.root.gameObject);
            dummies.Clear();
            if (inst != null) { Destroy(inst.root.gameObject); inst = null; }
            if (davidDummy != null) { Destroy(davidDummy.gameObject); davidDummy = null; }
            arms.weapon = BenaiaWeapon.Spear;
            if (n == 0)
            {
                ui.SetObjective("Treino 1 de 3: a varredura", "Um grupo de bonecos brilha em volta de você. Segure e solte o golpe para varrer todos de uma vez.");
                for (int g = 0; g < 4; g++)
                {
                    float a = g * Mathf.PI / 2f + Mathf.PI / 4f;
                    for (int i = 0; i < 3; i++)
                    {
                        float b = a + (i - 1) * 0.35f;
                        Dummy d = MakeDummy(C + new Vector3(Mathf.Sin(b) * 2.6f, 0f, Mathf.Cos(b) * 2.6f), U.Hex(0xd9c27a));
                        d.group = g; dummies.Add(d);
                    }
                }
                LightGroup(0);
            }
            else if (n == 1)
            {
                ui.SetObjective("Treino 2 de 3: a estocada", "Dois bonecos em fila acendem. Alinhe e estoque (clique): a lança atravessa os dois.");
                for (int g = 0; g < 3; g++)
                {
                    float a = (g - 1) * 0.9f;
                    for (int i = 0; i < 2; i++)
                    {
                        float r = 2.2f + i * 1.2f;
                        Dummy d = MakeDummy(C + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r), U.Hex(0xd9c27a));
                        d.group = g; dummies.Add(d);
                    }
                }
                LightGroup(0);
            }
            else
            {
                ui.SetObjective("Treino 3 de 3: entre os dois", "O instrutor ataca o boneco \"Davi\". Fique perto do instrutor, de frente para ele, e apare o golpe (botão direito) que ia para o boneco.");
                davidDummy = MakeDummy(C + new Vector3(0f, 0f, 3f), U.Hex(0x3d5a8a)).root;
                inst = Figure.Man(transform, "Instrutor", U.Hex(0x6e5a3a), false, 2.2f);
                inst.Spear();
                Vector3 p = C + new Vector3(3f, 0f, 7f);
                inst.root.position = new Vector3(p.x, Battlefield.Height(p.x, p.z), p.z);
                it = 2f; strikes = 0; winding = false;
            }
        }

        void OnSwing(SwingKind kind)
        {
            if (stage > 1) return;
            List<Dummy> lit = new List<Dummy>();
            foreach (Dummy d in dummies) if (d.lit && !d.hit) lit.Add(d);
            foreach (Dummy d in SpearHits.Pick(arms, lit, x => x.root.position))
            {
                d.hit = true; Mats.SetEmission(d.mat, Color.black);
                Sfx.Play("thud");
                Fx.Burst(d.root.position + Vector3.up * 1.2f, U.Hex(0xd9c27a), 5, 2f);
                score += stage == 0 ? (kind == SwingKind.Heavy ? 40 : 10) : 50;
            }
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (stage <= 1)
            {
                groupT -= dt;
                int litN = 0, hitN = 0;
                foreach (Dummy d in dummies) if (d.lit) { litN++; if (d.hit) hitN++; }
                bool done = litN > 0 && hitN == litN;
                if (litN > 0 && (done || groupT <= 0f))
                {
                    if (stage == 0 && done) { sweeps++; ui.Toast("Todos de uma vez! (" + hitN + ")", 0.8f); }
                    if (stage == 1 && hitN == 2) { lines++; score += 50; ui.Toast("Atravessou os dois! +50", 0.9f); }
                    if (!done) ui.Toast("Muito devagar!", 0.7f);
                    rounds++;
                    int max = stage == 0 ? 8 : 9, groups = stage == 0 ? 4 : 3;
                    if (rounds >= max) { foreach (Dummy d in dummies) { d.lit = false; Mats.SetEmission(d.mat, Color.black); } if (endT < 0f) endT = 1f; }
                    else LightGroup(UnityEngine.Random.Range(0, groups));
                }
            }
            else if (inst != null)
            {
                Vector3 p = inst.root.position, tp = davidDummy.position;
                inst.root.rotation = Quaternion.Euler(0f, U.YawTo(tp.x - p.x, tp.z - p.z), 0f);
                if (!winding)
                {
                    it -= dt;
                    if (it <= 0f && strikes < 6) { winding = true; it = 0f; Sfx.Play("roar", 0.25f, 1.8f); }
                    if (strikes >= 6 && endT < 0f) endT = 1f;
                }
                else
                {
                    it += dt;
                    inst.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, it / 0.5f) * 126f, 0f, 0f);
                    if (it >= 1f)
                    {
                        inst.armR.localRotation = Quaternion.Euler(-34f, 0f, 0f);
                        strikes++; winding = false; it = 2.2f;
                        if (arms.TryParryFor(p, 3.8f)) { blocks++; score += 150; ui.Toast("Aparou o golpe que ia para \"Davi\"! +150", 1.2f); }
                        else { Sfx.Play("thud"); ui.Toast("O golpe acertou o boneco \"Davi\".", 1f); Fx.Burst(tp + Vector3.up * 1.2f, U.Hex(0x3d5a8a), 5, 2f); }
                    }
                }
                if (!winding) inst.armR.localRotation = Quaternion.Slerp(inst.armR.localRotation, Quaternion.identity, dt * 3f);
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
            medal = score >= 1600 ? "ouro" : score >= 1000 ? "prata" : "bronze";
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
