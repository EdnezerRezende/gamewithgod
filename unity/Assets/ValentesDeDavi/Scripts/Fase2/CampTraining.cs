using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino no acampamento dos valentes: 1) bonecos de palha (golpe rápido e forte),
    /// 2) flechas sem ponta (bloquear e aparar), 3) portadores de tocha (funda).
    /// </summary>
    public class CampTraining : MonoBehaviour
    {
        public static readonly Vector3 Center = new Vector3(0f, 0f, -62f);

        class Dummy { public Transform root; public Material mat; public GameObject board; public bool heavy; }

        public PlayerController player;
        public SwordShield sword;
        public LentilField field;
        public UI ui;
        public Action<CampTraining> onFinished;
        /// <summary>Pede ao jogo para trocar de arma (espada = false, funda = true).</summary>
        public Action<bool> setSling;

        public int score, stage, parried, blocked;
        public string medal;
        readonly List<Dummy> dummies = new List<Dummy>();
        readonly List<GameObject> props = new List<GameObject>();
        int lit = -1, lits, arrowsLeft, arrowsDone, torchesDone, spawned;
        float litT, endT = -1f, spawnT;
        bool running;

        public void Begin()
        {
            score = parried = blocked = 0;
            Clear();
            running = true;
            sword.trainingMode = true;
            sword.extraSwing = SwingAtDummy;
            sword.onDefense = OnDefense;
            Setup(0);
        }

        public void Clear()
        {
            running = false;
            foreach (GameObject g in props) if (g != null) Destroy(g);
            props.Clear();
            dummies.Clear();
            foreach (Philistine p in Philistine.All.ToArray()) if (p.training) Destroy(p.gameObject);
            if (sword != null) { sword.trainingMode = false; sword.extraSwing = null; sword.onDefense = null; }
        }

        void Setup(int n)
        {
            stage = n; endT = -1f;
            foreach (Philistine p in Philistine.All.ToArray()) if (p.training) Destroy(p.gameObject);
            if (n == 0)
            {
                ui.SetObjective("Treino 1 de 3: bonecos de palha", "Golpeie o boneco que brilhar. Clique: golpe rápido. Segure e solte: golpe forte (para os que têm tábua).");
                setSling(false);
                for (int i = 0; i < 5; i++)
                {
                    float a = (i / 4f - 0.5f) * 1.6f;
                    Vector3 pos = Center + new Vector3(Mathf.Sin(a) * 3.2f, 0f, Mathf.Cos(a) * 3.2f);
                    pos.y = World.LentilHeight(pos.x, pos.z);
                    Transform root = new GameObject("Boneco").transform;
                    root.position = pos;
                    root.rotation = Quaternion.LookRotation(new Vector3(Center.x - pos.x, 0f, Center.z - pos.z));
                    props.Add(root.gameObject);
                    U.Cyl(root, new Vector3(0f, 0.8f, 0f), 0.05f, 1.6f, U.Hex(0x5a3f27));
                    Material m = Mats.New(U.Hex(0xd9c27a));
                    GameObject body = U.Cyl(root, new Vector3(0f, 1.2f, 0f), 0.28f, 0.9f, U.Hex(0xd9c27a));
                    GameObject head = U.Sph(root, new Vector3(0f, 1.8f, 0f), 0.18f, U.Hex(0xd9c27a));
                    body.GetComponent<Renderer>().sharedMaterial = m;
                    head.GetComponent<Renderer>().sharedMaterial = m;
                    GameObject board = U.Box(root, new Vector3(0f, 1.2f, 0.3f), new Vector3(0.5f, 0.6f, 0.05f), U.Hex(0x6d4a2b));
                    board.SetActive(false);
                    dummies.Add(new Dummy { root = root, mat = m, board = board });
                }
                lits = 0; lit = -1; litT = 0.8f;
            }
            else if (n == 1)
            {
                ui.SetObjective("Treino 2 de 3: flechas sem ponta", "O instrutor vai atirar. Segure o botão direito (ou Escudo) virado para ele; erguer no instante certo apara.");
                setSling(false);
                arrowsLeft = 8; arrowsDone = 0;
                Philistine ins = Philistine.Spawn(PhilType.Arqueiro, Center + new Vector3(0f, 0f, 16f), transform);
                ins.training = true;
                ins.MarkAsInstructor();
                ins.allowShot = () => { if (arrowsLeft <= 0) return false; arrowsLeft--; return true; };
                ins.onArrowMissed = () => { if (stage == 1) { arrowsDone++; ui.Toast("A flecha passou longe.", 0.8f); } };
            }
            else
            {
                ui.SetObjective("Treino 3 de 3: tochas", "Com a funda (Q troca de arma), derrube os portadores de tocha antes que alcancem os fardos.");
                setSling(true);
                torchesDone = 0; spawned = 0; spawnT = 1f;
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    Vector3 pos = Center + new Vector3(Mathf.Cos(a) * 7f, 0f, Mathf.Sin(a) * 7f);
                    GameObject b = U.Cyl(null, new Vector3(pos.x, World.LentilHeight(pos.x, pos.z) + 0.5f, pos.z), 0.5f, 0.8f, U.Hex(0xd8c27a));
                    b.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    props.Add(b);
                }
            }
        }

        bool SwingAtDummy(bool heavy)
        {
            if (stage != 0 || lit < 0) return false;
            Dummy d = dummies[lit];
            Vector3 dp = d.root.position - player.Position; dp.y = 0f;
            if (dp.magnitude > 3f || !sword.Facing(d.root.position, 60f)) return false;
            if (d.heavy && !heavy) { Sfx.Play("wood"); ui.Toast("Este tem uma tábua: segure para o golpe forte.", 1.4f); return true; }
            Sfx.Play("thud");
            Fx.Burst(d.root.position + Vector3.up * 1.2f, U.Hex(0xd9c27a), 8, 2.4f);
            score += 100;
            ui.Toast("+100", 0.6f);
            Unlight();
            return true;
        }

        void Unlight()
        {
            if (lit >= 0) { Dummy d = dummies[lit]; Mats.SetEmission(d.mat, Color.black); d.board.SetActive(false); }
            lit = -1; litT = 0f;
        }

        void OnDefense(bool? parry)
        {
            if (stage != 1) return;
            arrowsDone++;
            if (parry == true) { score += 150; parried++; ui.Toast("Aparou! +150", 0.9f); }
            else if (parry == false) { score += 80; blocked++; ui.Toast("Bloqueou +80", 0.9f); }
            else ui.Toast("A flecha sem ponta acertou Samá.", 1f);
        }

        void OnTorchDown(Philistine p)
        {
            if (!p.training || p.type != PhilType.Tocha) return;
            score += 120; torchesDone++;
            ui.Toast("Tocha derrubada! +120", 1f);
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (Philistine.Ctx != null) Philistine.Ctx.onDown = OnTorchDown;

            if (stage == 0)
            {
                litT -= dt;
                if (litT <= 0f)
                {
                    if (lit >= 0) { ui.Toast("Muito devagar!", 0.7f); Unlight(); litT = 0.5f; }
                    else if (lits < 8)
                    {
                        lit = UnityEngine.Random.Range(0, 5);
                        Dummy d = dummies[lit];
                        d.heavy = lits >= 5;
                        d.board.SetActive(d.heavy);
                        Mats.SetEmission(d.mat, U.Hex(0xffc15a) * 0.9f);
                        lits++;
                        litT = Difficulty.Current.level == DifficultyLevel.Valente ? 2f : 2.6f;
                    }
                    else if (endT < 0f) endT = 1f;
                }
            }
            else if (stage == 1)
            {
                if (arrowsLeft <= 0 && arrowsDone >= 8 && endT < 0f) endT = 1.2f;
            }
            else
            {
                spawnT -= dt;
                if (spawned < 5 && spawnT <= 0f)
                {
                    float a = Mathf.PI + (spawned - 2) * 0.55f;
                    Vector3 pos = Center + new Vector3(Mathf.Sin(a) * 30f, 0f, -Mathf.Cos(a) * 30f);
                    Philistine p = Philistine.Spawn(PhilType.Tocha, pos, transform);
                    p.training = true;
                    p.trainingCenter = Center;
                    p.trainingGoalRadius = 7.4f;
                    p.speed = Difficulty.Current.level == DifficultyLevel.Pastor ? 2.6f : Difficulty.Current.level == DifficultyLevel.Guerreiro ? 3.3f : 4f;
                    p.onTorchReached = q => { torchesDone++; ui.Toast("Um fardo pegou fogo.", 1.2f); field.Ignite(q.transform.position); };
                    spawned++; spawnT = 3.2f;
                }
                if (spawned >= 5 && torchesDone >= 5 && endT < 0f) endT = 1.5f;
            }
            if (endT > 0f)
            {
                endT -= dt;
                if (endT <= 0f) { if (stage < 2) Setup(stage + 1); else Finish(); }
            }
            ui.SetTrainingStats(score);
        }

        void Finish()
        {
            running = false;
            medal = score >= 1900 ? "ouro" : score >= 1200 ? "prata" : "bronze";
            Clear();
            field.ResetField();
            if (onFinished != null) onFinished(this);
        }

        public int StartingCourage()
        {
            int c = medal == "ouro" ? 80 : medal == "prata" ? 68 : 55;
            return c + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
        }

        /// <summary>Mantém Samá perto do centro do treino.</summary>
        public static Vector3 Clamp(Vector3 p)
        {
            Vector3 d = p - Center; d.y = 0f;
            if (d.magnitude > 6f) p = Center + d.normalized * 6f;
            return p;
        }
    }
}
