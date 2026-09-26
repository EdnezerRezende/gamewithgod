using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Treino nos campos de Belém (lembrança de Davi, 1 Sm 17:34-35):
    /// 1) jarros parados, 2) jarros balançando, 3) leões atacando o rebanho.
    /// </summary>
    public class Training : MonoBehaviour
    {
        public enum Medal { Bronze, Prata, Ouro }

        class Jar { public Transform t; public bool alive; public Transform pivot; public LineRenderer rope; public float amp, w, ph; }
        class Sheep { public Transform t; public bool alive; public float ph; }
        class Lion
        {
            public Rig rig; public string state = "run"; public int hp = 2; public float stagger, deadT;
            public bool active = true; public Sheep carry;
        }

        public World world;
        public PlayerController player;
        public UI ui;
        public Action<Training> onFinished;

        public int score, shots, hits;
        public int stage;
        public Medal medal;
        public int savedSheep;
        public float accuracy;

        static readonly Vector3 Flock = new Vector3(0f, 0f, -4f);
        Transform stageRoot, sheepRoot;
        readonly List<Jar> jars = new List<Jar>();
        readonly List<Sheep> sheep = new List<Sheep>();
        readonly List<Lion> lions = new List<Lion>();
        readonly float[] lionSchedule = { 1f, 7.5f, 13f };
        int spawned;
        float stageTime, endTimer = -1f;
        bool running;

        public void Begin()
        {
            score = shots = hits = 0;
            ClearAll();
            sheepRoot = U.Pivot(world.field.transform, "Rebanho", Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                Transform t = Models.Sheep(sheepRoot);
                float a = i / 5f * Mathf.PI * 2f, r = UnityEngine.Random.Range(1f, 2.4f);
                Vector3 p = Flock + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r * 0.7f);
                p.y = World.FieldHeight(p.x, p.z);
                t.position = p;
                t.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                Sheep s = new Sheep { t = t, alive = true, ph = UnityEngine.Random.Range(0f, 6f) };
                sheep.Add(s);
                HitZone.Sphere(t, "ovelha", new Vector3(0f, 0.6f, 0f), 0.45f, (st, p2) => HitSheep(s));
            }
            running = true;
            SetupStage(0);
        }

        public void ClearAll()
        {
            running = false;
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            if (sheepRoot != null) Destroy(sheepRoot.gameObject);
            jars.Clear(); sheep.Clear(); lions.Clear();
        }

        void HitSheep(Sheep s)
        {
            if (!s.alive) return;
            Sfx.Play("bleat");
            score = Mathf.Max(0, score - 50);
            ui.Toast("Cuidado com as ovelhas! −50");
            Fx.Burst(s.t.position + Vector3.up * 0.6f, U.Hex(0xe8e1cf), 6, 1.5f, 0.8f, 0.8f);
        }

        void SetupStage(int n)
        {
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            stageRoot = U.Pivot(world.field.transform, "Etapa " + (n + 1), Vector3.zero);
            jars.Clear();
            stage = n;
            endTimer = -1f;
            stageTime = 0f;
            if (n == 0)
            {
                ui.SetObjective("Etapa 1 de 3: jarros parados", "Acerte os 5 jarros. Segure o botão para girar a funda e solte na faixa dourada.");
                float[,] pos = { { -6, -12 }, { -2, -16 }, { 3, -13 }, { 7, -19 }, { 0, -24 } };
                for (int i = 0; i < 5; i++)
                {
                    float x = pos[i, 0], z = pos[i, 1], gy = World.FieldHeight(x, z);
                    U.Cyl(stageRoot, new Vector3(x, gy + 0.57f, z), 0.055f, 1.15f, U.Hex(0x5a3f27));
                    Transform t = Models.Jar(stageRoot);
                    t.position = new Vector3(x, gy + 1.35f, z);
                    AddJar(t, null, 100);
                }
            }
            else if (n == 1)
            {
                ui.SetObjective("Etapa 2 de 3: jarros em movimento", "Acerte os 4 jarros balançando. Antecipe o movimento.");
                float[,] pos = { { -7, -15 }, { 4, -17 }, { -1, -22 }, { 9, -25 } };
                float speedScale = Difficulty.Current.lionSpeed / 6f * 0.5f + 0.5f;
                for (int i = 0; i < 4; i++)
                {
                    float x = pos[i, 0], z = pos[i, 1], len = 2.8f;
                    Transform tree = Models.Tree(stageRoot, len);
                    tree.position = new Vector3(x - len + 0.3f, World.FieldHeight(x - len + 0.3f, z), z);
                    Transform pivot = U.Pivot(stageRoot, "Galho", new Vector3(x, tree.position.y + 3.35f, z));
                    Transform t = Models.Jar(stageRoot);
                    LineRenderer rope = new GameObject("Corda").AddComponent<LineRenderer>();
                    rope.transform.SetParent(stageRoot, false);
                    rope.positionCount = 2; rope.widthMultiplier = 0.02f; rope.sharedMaterial = Mats.Get(U.Hex(0x3a2616));
                    Jar j = AddJar(t, pivot, 150);
                    j.rope = rope;
                    j.amp = UnityEngine.Random.Range(0.55f, 0.85f);
                    j.w = UnityEngine.Random.Range(1.3f, 2.1f) * speedScale;
                    j.ph = UnityEngine.Random.Range(0f, 6f);
                }
            }
            else
            {
                ui.SetObjective("Etapa 3 de 3: o leão", "Defenda o rebanho. Na cabeça, uma pedra basta; no corpo, são duas.");
                lions.Clear();
                spawned = 0;
            }
        }

        Jar AddJar(Transform t, Transform pivot, int points)
        {
            Jar j = new Jar { t = t, alive = true, pivot = pivot };
            jars.Add(j);
            HitZone jz = HitZone.Sphere(t, "jarro", Vector3.zero, 0.3f, (s, p) =>
            {
                if (!j.alive) return;
                j.alive = false;
                j.t.gameObject.SetActive(false);
                if (j.rope != null) j.rope.enabled = false;
                Sfx.Play("clay");
                Fx.Burst(j.t.position, U.Hex(0xb5603a), 14, 3.2f);
                score += points;
                hits++;
            });
            jz.counts = () => j.alive;
            return j;
        }

        void SpawnLion(int i)
        {
            float[] angles = { -0.7f, 0.6f, 0f };
            float a = angles[i] + UnityEngine.Random.Range(-0.15f, 0.15f), R = 38f;
            Rig rig = Models.Lion(stageRoot);
            Vector3 p = Flock + new Vector3(Mathf.Sin(a) * R, 0f, -Mathf.Cos(a) * R);
            p.y = World.FieldHeight(p.x, p.z);
            rig.root.position = p;
            Lion L = new Lion { rig = rig };
            lions.Add(L);
            Sfx.Play("growl");
            ui.Toast("Um leão vem para o rebanho!", 1.6f);
            HitZone.Sphere(rig.head, "cabeça do leão", Vector3.zero, 0.36f / 0.5f, (s, hp) => HitLion(L, true)).counts = () => L.state != "dead";
            HitZone.Box(rig.root, "corpo do leão", new Vector3(0f, 0.78f, 0f), new Vector3(0.6f, 0.6f, 1.3f), (s, hp) => HitLion(L, false)).counts = () => L.state != "dead";
        }

        void HitLion(Lion L, bool head)
        {
            if (L.state == "dead") return;
            hits++;
            L.hp--;
            if (head || L.hp <= 0)
            {
                L.state = "dead";
                Sfx.Play("thud");
                Fx.Burst(L.rig.head.position, U.Hex(0x6a4323), 8, 2f);
                score += head ? 300 : 200;
                ui.Toast(head ? "Na cabeça! +300" : "Derrubado! +200", 1.4f);
                if (L.carry != null)
                {
                    Sheep s = L.carry;
                    s.alive = true;
                    s.t.gameObject.SetActive(true);
                    Vector3 p = L.rig.root.position + Vector3.right;
                    p.y = World.FieldHeight(p.x, p.z);
                    s.t.position = p;
                    score += 150;
                    ui.Toast("Você livrou a ovelha da boca do leão! +150", 2.6f);
                    Sfx.Play("bleat");
                    L.carry = null;
                }
            }
            else
            {
                L.stagger = 0.6f;
                Sfx.Play("growl");
                Fx.Burst(L.rig.body.position, U.Hex(0xb98a4b), 5, 1.6f);
                ui.Toast("Ferido. Mais uma pedra!", 1.2f);
            }
        }

        void UpdateLions(float dt)
        {
            float baseSpeed = Difficulty.Current.lionSpeed;
            foreach (Lion L in lions)
            {
                if (!L.active) continue;
                Transform r = L.rig.root;
                if (L.state == "dead")
                {
                    L.deadT += dt;
                    Vector3 e = r.localEulerAngles;
                    r.localRotation = Quaternion.Euler(0f, e.y, Mathf.Min(90f, L.deadT * 290f));
                    continue;
                }
                float spd = baseSpeed * (L.stagger > 0f ? 0.25f : 1f);
                L.stagger = Mathf.Max(0f, L.stagger - dt);
                Vector3 p = r.position, target;
                if (L.state == "run")
                {
                    Sheep best = null;
                    float bd = 1e9f;
                    foreach (Sheep s in sheep)
                    {
                        if (!s.alive) continue;
                        float d = Vector3.Distance(U.Flat(s.t.position), U.Flat(p));
                        if (d < bd) { bd = d; best = s; }
                    }
                    if (best == null) { L.state = "flee"; continue; }
                    target = best.t.position;
                    if (bd < 1.1f)
                    {
                        best.alive = false;
                        best.t.gameObject.SetActive(false);
                        L.carry = best;
                        L.state = "flee";
                        Sfx.Play("bleat");
                        ui.Toast("O leão levou uma ovelha! Derrube-o antes que fuja.", 2.4f);
                    }
                }
                else
                {
                    Vector3 away = U.Flat(p - Flock);
                    float ad = away.magnitude;
                    target = p + away.normalized * 10f;
                    spd *= 1.05f;
                    if (ad > 44f)
                    {
                        L.active = false;
                        r.gameObject.SetActive(false);
                        continue;
                    }
                }
                Vector3 dir = U.Flat(target - p);
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    p += dir * spd * dt;
                    p.y = World.FieldHeight(p.x, p.z);
                    r.position = p;
                    r.rotation = Quaternion.Euler(0f, U.YawTo(dir.x, dir.z), 0f);
                }
                for (int i = 0; i < 4; i++)
                    L.rig.legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * spd * 2.4f + (i % 2 == 1 ? Mathf.PI : 0f) + (i > 1 ? Mathf.PI / 2f : 0f)) * 40f, 0f, 0f);
            }
        }

        void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            stageTime += dt;
            if (stage == 1)
            {
                foreach (Jar j in jars)
                {
                    float th = j.amp * Mathf.Sin(Time.time * j.w + j.ph), L = 1.7f;
                    j.t.position = j.pivot.position + new Vector3(Mathf.Sin(th) * L, -Mathf.Cos(th) * L - 0.3f, 0f);
                    j.rope.SetPosition(0, j.pivot.position);
                    j.rope.SetPosition(1, j.t.position + Vector3.up * 0.3f);
                }
            }
            foreach (Sheep s in sheep)
                if (s.alive)
                {
                    Vector3 p = s.t.position;
                    p.y = World.FieldHeight(p.x, p.z) + Mathf.Abs(Mathf.Sin(Time.time * 1.3f + s.ph)) * 0.02f;
                    s.t.position = p;
                }
            if (stage == 2)
            {
                while (spawned < 3 && stageTime >= lionSchedule[spawned]) SpawnLion(spawned++);
                UpdateLions(dt);
            }

            bool done;
            if (stage < 2) { done = true; foreach (Jar j in jars) if (j.alive) done = false; }
            else { done = spawned == 3; foreach (Lion L in lions) if (L.active && L.state != "dead") done = false; }
            if (done && endTimer < 0f) { endTimer = 1.6f; if (stage < 2) ui.Toast("Etapa concluída"); }
            if (endTimer > 0f)
            {
                endTimer -= dt;
                if (endTimer <= 0f)
                {
                    if (stage < 2) SetupStage(stage + 1);
                    else Finish();
                }
            }
            ui.SetTrainingStats(score);
        }

        void Finish()
        {
            running = false;
            savedSheep = 0;
            foreach (Sheep s in sheep) if (s.alive) savedSheep++;
            accuracy = shots > 0 ? hits / (float)shots : 0f;
            score += savedSheep * 100 + Mathf.RoundToInt(accuracy * 300f);
            medal = score >= 2100 ? Medal.Ouro : score >= 1400 ? Medal.Prata : Medal.Bronze;
            if (onFinished != null) onFinished(this);
        }

        public int StartingCourage()
        {
            int c = medal == Medal.Ouro ? 72 : medal == Medal.Prata ? 58 : 45;
            return c + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
        }
    }
}
