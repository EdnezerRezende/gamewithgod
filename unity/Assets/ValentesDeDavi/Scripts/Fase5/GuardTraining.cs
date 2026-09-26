using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino da fase 5, no campo da guarda: 1) o bote — o instrutor corre contra Benaia como o leão;
    /// desviar e contra-atacar; 2) o cajado — aparar a estocada da lança sem ponta e arrancá-la (segurar
    /// Ação); 3) a lança — estocar os bonecos que brilham, mantendo distância.
    /// </summary>
    public class GuardTraining : MonoBehaviour
    {
        class Dummy { public Transform root; public Material mat; }
        enum IS { Ready, Crouch, Charge, Stumble, Back, Wind, Staggered, Rearm }

        public PlayerController player;
        public BenaiaArms arms;
        public UI ui;
        public Action<GuardTraining> onFinished;

        public int score, stage, escaped, counters, disarms;
        public string medal;
        Figure inst;
        Transform instSpear;
        readonly List<Dummy> dummies = new List<Dummy>();
        IS ist;
        float it, stT, endT = -1f, dist, trav, litT;
        int count, lit = -1, seqN;
        bool running, hitDone, hitThis, dodgedThis;
        Vector3 dir;

        static Vector3 C { get { return Snowland.Training; } }
        public bool Disarmable { get { return running && stage == 1 && inst != null && ist == IS.Staggered && Vector3.Distance(inst.root.position, player.Position) < 3.3f; } }

        public void Begin()
        {
            score = escaped = counters = disarms = 0;
            Clear();
            running = true;
            arms.trainingMode = true;
            arms.onSwing = OnSwing;
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            if (inst != null) { Destroy(inst.root.gameObject); inst = null; }
            foreach (Dummy d in dummies) if (d.root != null) Destroy(d.root.gameObject);
            dummies.Clear();
            if (arms != null) { arms.trainingMode = false; if (arms.onSwing == (Action<SwingKind>)OnSwing) arms.onSwing = null; }
        }

        void MakeInstructor(bool withSpear)
        {
            if (inst != null) Destroy(inst.root.gameObject);
            inst = Figure.Man(transform, "Instrutor", U.Hex(0x3d5a8a), false);
            Vector3 p = C + new Vector3(0f, 0f, 8f);
            inst.root.position = new Vector3(p.x, Snowland.Height(p.x, p.z), p.z);
            inst.root.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (withSpear)
            {
                instSpear = U.Pivot(inst.armR, "Lança sem ponta", new Vector3(0f, -0.6f, 0.12f));
                instSpear.localRotation = Quaternion.Euler(14f, 0f, 0f);
                U.Cyl(instSpear, Vector3.zero, 0.03f, 2.2f, U.Hex(0x5a3d22));
                U.Sph(instSpear, new Vector3(0f, 1.15f, 0f), 0.08f, U.Hex(0xcdb892));
            }
            else U.Box(inst.armL, new Vector3(0.1f, -0.35f, 0.32f), new Vector3(0.7f, 1.1f, 0.25f), U.Hex(0xb5a27a));
        }

        void MakeDummy(Vector3 pos)
        {
            pos.y = Snowland.Height(pos.x, pos.z);
            Transform root = new GameObject("Boneco").transform;
            root.SetParent(transform, false);
            root.position = pos;
            root.rotation = Quaternion.LookRotation(new Vector3(C.x - pos.x, 0f, C.z - pos.z));
            U.Cyl(root, new Vector3(0f, 0.8f, 0f), 0.05f, 1.6f, U.Hex(0x5a3f27));
            Material m = Mats.New(U.Hex(0xd9c27a));
            U.Cyl(root, new Vector3(0f, 1.2f, 0f), 0.28f, 0.9f, U.Hex(0xd9c27a)).GetComponent<Renderer>().sharedMaterial = m;
            U.Sph(root, new Vector3(0f, 1.8f, 0f), 0.18f, U.Hex(0xd9c27a)).GetComponent<Renderer>().sharedMaterial = m;
            dummies.Add(new Dummy { root = root, mat = m });
        }

        void Setup(int n)
        {
            stage = n; stT = 0f; endT = -1f; count = 0; ist = IS.Ready; it = 1.2f;
            if (inst != null) { Destroy(inst.root.gameObject); inst = null; }
            foreach (Dummy d in dummies) if (d.root != null) Destroy(d.root.gameObject);
            dummies.Clear();
            if (n == 0)
            {
                arms.weapon = BenaiaWeapon.Sword;
                ui.SetObjective("Treino 1 de 3: o bote", "O instrutor vai correr contra você, como o leão. Quando ele se agachar, desvie para o lado (Espaço) e golpeie enquanto ele se recupera.");
                MakeInstructor(false);
            }
            else if (n == 1)
            {
                arms.weapon = BenaiaWeapon.Staff;
                ui.SetObjective("Treino 2 de 3: o cajado", "Apare a estocada com o cajado (botão direito no instante do golpe). Com ele desequilibrado, chegue perto e segure E para arrancar a lança.");
                MakeInstructor(true);
            }
            else
            {
                arms.weapon = BenaiaWeapon.Spear;
                ui.SetObjective("Treino 3 de 3: a lança", "Estoque o boneco que brilhar. A lança alcança longe: mantenha distância.");
                MakeDummy(C + new Vector3(-3f, 0f, 4.5f)); MakeDummy(C + new Vector3(0f, 0f, 5.2f)); MakeDummy(C + new Vector3(3f, 0f, 4.5f));
                lit = -1; litT = 0.8f; seqN = 0;
            }
        }

        void OnSwing(SwingKind kind)
        {
            if (stage == 0 && inst != null && ist == IS.Stumble && !hitThis && arms.InReach(inst.root.position))
            {
                hitThis = true; score += 100; counters++; Sfx.Play("thud"); ui.Toast("Contra-ataque! +100", 0.9f);
            }
            if (stage == 2 && lit >= 0)
            {
                Dummy d = dummies[lit];
                if (!arms.InReach(d.root.position)) return;
                if (Vector3.Distance(d.root.position, player.Position) < 2.2f) { Sfx.Play("wood"); ui.Toast("Perto demais: a lança precisa de distância.", 1f); return; }
                Sfx.Play("thud");
                Fx.Burst(d.root.position + Vector3.up * 1.2f, U.Hex(0xd9c27a), 8, 2.4f);
                score += 80; Mats.SetEmission(d.mat, Color.black); lit = -1; litT = 0.4f;
                ui.Toast("+80", 0.6f);
            }
        }

        /// <summary>Benaia arrancou a lança do instrutor (segurando Ação).</summary>
        public void Disarm()
        {
            score += 150; disarms++;
            Sfx.Play("pull"); ui.Toast("Arrancou a lança! +150", 1f);
            if (instSpear != null) instSpear.gameObject.SetActive(false);
            ist = IS.Rearm; it = 1.4f;
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            stT += dt;
            Vector3 pp = player.Position;
            if (inst != null)
            {
                Vector3 p = inst.root.position;
                float dP = new Vector2(pp.x - p.x, pp.z - p.z).magnitude;
                Vector3 face = pp - p; face.y = 0f;
                if (stage == 0)
                {
                    switch (ist)
                    {
                        case IS.Ready:
                            it -= dt; if (face.sqrMagnitude > 0.01f) inst.root.rotation = Quaternion.LookRotation(face);
                            if (it <= 0f && count < 6) { ist = IS.Crouch; it = 0f; Sfx.Play("roar", 0.25f, 1.8f); }
                            else if (count >= 6 && endT < 0f) endT = 1f;
                            break;
                        case IS.Crouch:
                            it += dt;
                            p.y = Snowland.Height(p.x, p.z) - 0.15f;
                            if (it >= Fase5Params.Current.pounceWarn + 0.1f) { dir = face; dist = dir.magnitude; dir.Normalize(); trav = 0f; hitDone = false; dodgedThis = false; ist = IS.Charge; count++; }
                            break;
                        case IS.Charge:
                        {
                            float step = 10f * dt;
                            p += dir * step; trav += step; inst.Walk(10f);
                            if (!hitDone && dP < 1.3f)
                            {
                                hitDone = true;
                                arms.ResolveIncoming(p, 0f, "", new Incoming { noParry = true, noBlock = true, onHit = () => ui.Toast("O bote acertou. Desvie quando ele se agachar.", 1.4f), onDodge = () => dodgedThis = true });
                            }
                            if (trav >= dist + 2.5f)
                            {
                                ist = IS.Stumble; it = 1.6f; hitThis = false;
                                if (!hitDone || dodgedThis) { escaped++; score += 60; ui.Toast("Escapou do bote! Agora golpeie. +60", 1f); }
                            }
                            break;
                        }
                        case IS.Stumble:
                            it -= dt;
                            inst.root.rotation = Quaternion.Euler(0f, inst.root.eulerAngles.y, Mathf.Sin(Time.time * 12f) * 6f);
                            if (it <= 0f) { inst.root.rotation = Quaternion.Euler(0f, inst.root.eulerAngles.y, 0f); ist = IS.Back; }
                            break;
                        case IS.Back:
                        {
                            Vector3 home = C + new Vector3(0f, 0f, 8f), d = home - p; d.y = 0f;
                            if (d.magnitude > 0.3f) { p += d.normalized * 3f * dt; inst.Walk(3f); inst.root.rotation = Quaternion.LookRotation(d); }
                            else { ist = IS.Ready; it = 1f; }
                            break;
                        }
                    }
                    if (ist != IS.Crouch) p.y = Snowland.Height(p.x, p.z);
                }
                else if (stage == 1)
                {
                    if (face.sqrMagnitude > 0.01f && ist != IS.Staggered) inst.root.rotation = Quaternion.LookRotation(face);
                    switch (ist)
                    {
                        case IS.Ready:
                            if (dP > 3.6f) { p += face.normalized * 2f * dt; inst.Walk(2f); }
                            it -= dt;
                            if (it <= 0f && dP < 4.6f && count < 8) { ist = IS.Wind; it = 0f; }
                            if (count >= 8 && endT < 0f) endT = 1f;
                            break;
                        case IS.Wind:
                            it += dt;
                            inst.armR.localRotation = Quaternion.Euler(Mathf.Min(1f, it / 0.4f) * 40f, 0f, 0f);
                            if (it >= 0.8f)
                            {
                                inst.armR.localRotation = Quaternion.Euler(-70f, 0f, 0f);
                                count++; ist = IS.Ready; it = 1.6f;
                                if (dP < 4.6f)
                                    arms.ResolveIncoming(p, 0f, "", new Incoming
                                    {
                                        noBlock = true,
                                        onParry = () => { ist = IS.Staggered; it = 1.8f; ui.Toast("Aparou! Segure E para arrancar a lança.", 1.4f); },
                                        onHit = () => ui.Toast("A lança sem ponta acertou. Apare no instante do golpe.", 1.4f),
                                    });
                            }
                            break;
                        case IS.Staggered:
                            it -= dt;
                            inst.root.rotation = Quaternion.Euler(0f, inst.root.eulerAngles.y, Mathf.Sin(Time.time * 12f) * 3.5f);
                            if (it <= 0f) { inst.root.rotation = Quaternion.Euler(0f, inst.root.eulerAngles.y, 0f); ist = IS.Ready; it = 1f; }
                            break;
                        case IS.Rearm:
                            it -= dt;
                            if (it <= 0f) { if (instSpear != null) instSpear.gameObject.SetActive(true); ist = IS.Ready; it = 1f; }
                            break;
                    }
                    inst.armR.localRotation = Quaternion.Slerp(inst.armR.localRotation, Quaternion.identity, dt * 3f);
                    p.y = Snowland.Height(p.x, p.z);
                    if (disarms >= 5 && endT < 0f) endT = 1f;
                }
                inst.root.position = p;
            }
            if (stage == 2)
            {
                litT -= dt;
                if (lit >= 0 && litT <= 0f) { ui.Toast("Muito devagar!", 0.7f); Mats.SetEmission(dummies[lit].mat, Color.black); lit = -1; litT = 0.4f; }
                else if (lit < 0 && litT <= 0f)
                {
                    if (seqN < 9)
                    {
                        lit = UnityEngine.Random.Range(0, dummies.Count);
                        Mats.SetEmission(dummies[lit].mat, U.Hex(0xffc15a) * 1.1f);
                        litT = Difficulty.Current.level == DifficultyLevel.Valente ? 2f : 2.6f; seqN++;
                    }
                    else if (endT < 0f) endT = 1f;
                }
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
            medal = score >= 1900 ? "ouro" : score >= 1200 ? "prata" : "bronze";
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
