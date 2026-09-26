using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// A batalha de Pas-Damim: os homens de Israel sobem a colina e Eleazar escolhe descer; três linhas
    /// filisteias com estandartes; a trombeta de reforços; a resistência final e o grande livramento.
    /// </summary>
    public class ValleyBattle : MonoBehaviour
    {
        public enum Phase { Idle, Rise, Line, Inter, Final, Done }

        class Man { public Rig rig; public Vector3 target; public float bend, cool; }

        public PlayerController player;
        public EleazarSword sword;
        public UI ui;
        public Action<string> onFall;
        public Action onDeliverance;

        public Phase phase = Phase.Idle;
        public int line, deaths, repelled;
        public float hold, time, courageStart = 70f, prayProgress, bannerZ = -999f;
        public bool rose, retreated, hornUsed, stuckAtEnd, forceEngage;

        readonly List<Man> soldiers = new List<Man>(), allies = new List<Man>();
        readonly List<GameObject> banners = new List<GameObject>();
        float riseT, interT, spawnT, prayCool, doneT = -1f, lastZ;
        bool fallen;

        public Fase3Params P { get { return Fase3Params.Current; } }
        public bool Active { get { return phase != Phase.Idle && phase != Phase.Done; } }
        public float HoldFraction { get { return phase == Phase.Final ? hold / P.holdSeconds : -1f; } }
        public bool Retreating { get { return Active && phase != Phase.Rise && bannerZ > -999f && player.Position.z < bannerZ - 4f; } }

        public string WaveLabel
        {
            get
            {
                switch (phase)
                {
                    case Phase.Rise: return "Os homens de Israel sobem";
                    case Phase.Final: return "Resista até o fim";
                    case Phase.Inter: return "Avance";
                    case Phase.Line: return "Linha " + line + " de 3";
                }
                return "";
            }
        }

        public void ClearEnemies()
        {
            foreach (Foe f in Foe.All.ToArray()) if (!f.training) Destroy(f.gameObject);
            foreach (Arrow a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);
        }

        public void ClearAll()
        {
            ClearEnemies();
            foreach (Man a in allies) if (a.rig.root != null) Destroy(a.rig.root.gameObject);
            allies.Clear();
            foreach (GameObject b in banners) if (b != null) Destroy(b);
            banners.Clear();
        }

        // ------------------------------------------------------------------ soldados de Israel

        static Man MakeMan(Transform parent, string name, int i, Vector3 pos)
        {
            Color[] robes = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52) };
            Rig r = Models.Human(parent, name, 1.75f, robes[i % 3]);
            GameObject spear = U.Cyl(r.root, new Vector3(0.32f, 1.2f, 0.1f), 0.03f, 2.4f, U.Hex(0x5a3d22));
            spear.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            pos.y = PasDamim.Height(pos.x, pos.z);
            r.root.position = pos;
            return new Man { rig = r, target = pos };
        }

        /// <summary>Os soldados de Israel em volta de um ponto (retângulo x, z).</summary>
        public void PlaceSoldiers(float x0, float x1, float z0, float z1)
        {
            foreach (Man s in soldiers) if (s.rig.root != null) Destroy(s.rig.root.gameObject);
            soldiers.Clear();
            for (int i = 0; i < 14; i++)
            {
                Man m = MakeMan(transform, "Soldado de Israel", i, new Vector3(U.Rand(x0, x1), 0f, U.Rand(z0, z1)));
                m.rig.root.rotation = Quaternion.Euler(0f, U.Rand(-30f, 30f), 0f);
                soldiers.Add(m);
            }
        }

        public void SendSoldiers(float z0, float z1)
        {
            foreach (Man s in soldiers) s.target = new Vector3(U.Rand(-8f, 8f), 0f, U.Rand(z0, z1));
        }

        public void UpdateSoldiers(float dt, float speed)
        {
            foreach (Man s in soldiers)
            {
                Vector3 p = s.rig.root.position, d = s.target - p; d.y = 0f;
                if (d.magnitude > 0.5f)
                {
                    p += d.normalized * speed * dt;
                    s.rig.root.rotation = Quaternion.Euler(0f, U.YawTo(d.x, d.z), 0f);
                    for (int j = 0; j < 2; j++) s.rig.legs[j].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 10f + j * Mathf.PI) * 35f, 0f, 0f);
                }
                else
                {
                    for (int j = 0; j < 2; j++) s.rig.legs[j].localRotation = Quaternion.identity;
                    // Recolhendo os despojos: abaixa e levanta.
                    s.rig.root.rotation = Quaternion.Euler(s.bend * Mathf.Abs(Mathf.Sin(Time.time * 1.5f + p.x)) * Mathf.Rad2Deg, s.rig.root.eulerAngles.y, 0f);
                }
                p.y = PasDamim.Height(p.x, p.z);
                s.rig.root.position = p;
            }
        }

        // ------------------------------------------------------------------ fluxo

        public void Begin(int fromLine)
        {
            ClearEnemies();
            fallen = false; doneT = -1f;
            player.health = 100f;
            prayProgress = 0f;
            sword.Cancel();
            sword.trainingMode = false;
            sword.finalStand = false;
            if (fromLine == 0)
            {
                rose = retreated = hornUsed = stuckAtEnd = forceEngage = false;
                deaths = repelled = 0; time = 0f; riseT = 0f; bannerZ = -999f;
                sword.ResetState();
                ClearAll();
                player.courage = courageStart;
                player.Place(new Vector3(0f, 0f, -30f), 0f, -0.05f);
                PlaceSoldiers(-8f, 8f, -36f, -24f);
                SendSoldiers(-50f, -42f);
                SpawnLine(1);
                phase = Phase.Rise;
                ui.SetObjective("Os homens de Israel sobem", "Os soldados recuam colina acima. Alguém precisa descer ao vale.");
                Sfx.Play("roar", 0.4f, 1.6f);
                ui.Toast("\"Quem vai descer?\"", 3f);
            }
            else
            {
                float z = fromLine == 1 ? -8f : PasDamim.Lines[fromLine - 2] + 2f;
                player.Place(new Vector3(PasDamim.CenterX(z), 0f, z), 0f, 0f);
                sword.fatigue = sword.stuck ? 100f : Mathf.Min(sword.fatigue, 40f);
                SpawnLine(fromLine);
                forceEngage = true;
            }
            lastZ = player.Position.z;
        }

        /// <summary>Recomeça a resistência final do zero (ao cair nela).</summary>
        public void BeginFinal()
        {
            Begin(3);
            ClearEnemies();
            bannerZ = Mathf.Max(bannerZ, PasDamim.Lines[2]);
            StartFinal();
        }

        void SpawnLine(int n)
        {
            line = n; phase = Phase.Line; forceEngage = false;
            int c = P.lineCounts[n - 1];
            float z0 = PasDamim.Lines[n - 1] + 8f;
            List<KeyValuePair<FoeType, Vector2>> L = new List<KeyValuePair<FoeType, Vector2>>();
            Action<FoeType, float, float> add = (t, x, z) => L.Add(new KeyValuePair<FoeType, Vector2>(t, new Vector2(x, z)));
            if (n == 1) for (int i = 0; i < c; i++) add(FoeType.Lanceiro, -7f + (i % 6) * 2.8f + U.Rand(-0.5f, 0.5f), z0 + (i / 6) * 2.5f);
            if (n == 2)
            {
                int f = Mathf.RoundToInt(c * 0.6f);
                for (int i = 0; i < f; i++) add(FoeType.Falange, -6f + (i % 6) * 2.2f, z0 + (i / 6) * 1.8f);
                for (int i = f; i < c; i++) add(i % 2 == 1 ? FoeType.Arqueiro : FoeType.Lanceiro, U.Rand(-7f, 7f), z0 + 6f + U.Rand(0f, 3f));
            }
            if (n == 3)
            {
                add(FoeType.Porta, 0f, z0 + 4f);
                int f = Mathf.RoundToInt(c * 0.35f), a = Mathf.RoundToInt(c * 0.2f);
                for (int i = 0; i < f; i++) add(FoeType.Falange, -6f + (i % 6) * 2.2f, z0);
                for (int i = 0; i < a; i++) add(FoeType.Arqueiro, U.Rand(-7f, 7f), z0 + 7f);
                for (int i = f + a + 1; i < c; i++) add(FoeType.Lanceiro, U.Rand(-7f, 7f), z0 + 2f + U.Rand(0f, 3f));
            }
            foreach (var e in L) Foe.Spawn(e.Key, new Vector3(e.Value.x + PasDamim.CenterX(e.Value.y), 0f, e.Value.y), transform);
            string[] txt = {
                "Lanceiros filisteus em linha. Use a sequência rápido, rápido, forte.",
                "Uma falange: escudos lado a lado. Golpe forte ou a sequência completa abre brecha; ou ataque pelo lado.",
                "A última linha, com o porta-estandarte. Derrube-o e os filisteus em volta perdem o ânimo." };
            ui.SetObjective("Avance · Linha " + n + " de 3", txt[n - 1]);
        }

        void PlantBanner(float z)
        {
            float x = PasDamim.CenterX(z) + 5f;
            GameObject g = new GameObject("Estandarte de Israel");
            g.transform.SetParent(transform, false);
            g.transform.position = new Vector3(x, PasDamim.Height(x, z), z);
            U.Cyl(g.transform, new Vector3(0f, 1.7f, 0f), 0.04f, 3.4f, U.Hex(0x4e3620));
            U.Box(g.transform, new Vector3(0.45f, 3.1f, 0f), new Vector3(0.9f, 0.6f, 0.02f), U.Hex(0x3d5a8a));
            banners.Add(g);
            bannerZ = z;
            ui.Toast("O estandarte de Israel foi plantado.", 1.8f);
            Sfx.Play("horn", 0.6f);
        }

        void StartFinal()
        {
            phase = Phase.Final; hold = 0f; spawnT = 1f;
            sword.finalStand = true;
            ui.SetObjective("Resista até o fim", sword.stuck
                ? "A mão está pegada à espada. Continue lutando: o marcador avança enquanto você fica de pé e não recua."
                : "Uma tropa grande desce o vale. O cansaço vai chegar ao máximo: continue lutando.");
            ui.Toast("Resista!", 2f);
            Sfx.Play("roar", 0.4f, 1.6f);
        }

        int AliveCount()
        {
            int n = 0;
            foreach (Foe f in Foe.All) if (f.Alive && !f.training) n++;
            return n;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (doneT > 0f) { doneT -= dt; if (doneT <= 0f) Deliverance(); }
            if (!Active || fallen) return;

            time += dt;
            UpdateSoldiers(dt, 3.5f);
            UpdateAllies(dt);
            UpdatePrayer(dt);
            Vector3 pp = player.Position;

            if (phase == Phase.Rise)
            {
                riseT += dt;
                if (pp.z > PasDamim.RiseLineZ)
                {
                    rose = true; player.courage = 100f;
                    ui.Toast("Eleazar se levantou.", 1.6f);
                    phase = Phase.Line; forceEngage = false;
                    ui.SetObjective("Avance · Linha 1 de 3", "Lanceiros filisteus em linha. Use a sequência rápido, rápido, forte.");
                }
                else if (riseT > 20f)
                {
                    phase = Phase.Line; forceEngage = true;
                    ui.Toast("Os filisteus sobem a colina.", 2f);
                    ui.SetObjective("Avance · Linha 1 de 3", "Os filisteus vieram até você. Lute.");
                }
                lastZ = pp.z;
                return;
            }

            // Avançar vale abaixo dá coragem; recuar para trás do estandarte tira.
            if (pp.z > lastZ) player.courage = Mathf.Clamp(player.courage + (pp.z - lastZ) * 1.2f, 0f, 100f);
            lastZ = pp.z;
            if (Retreating)
            {
                player.courage = Mathf.Clamp(player.courage - 4f * dt, 0f, 100f);
                if (!retreated) { retreated = true; ui.Toast("Você recuou para trás do estandarte.", 2f); }
            }
            int near = 0;
            foreach (Foe f in Foe.All) if (f.Alive && !f.training && Vector3.Distance(f.transform.position, pp) < 4f) near++;
            if (near >= 3) player.courage = Mathf.Clamp(player.courage - 3f * dt, 0f, 100f);

            switch (phase)
            {
                case Phase.Line:
                    if (AliveCount() == 0)
                    {
                        PlantBanner(PasDamim.Lines[line - 1]);
                        if (line < 3) { phase = Phase.Inter; interT = 4f; ui.SetObjective("Avance", "A linha caiu. Siga vale abaixo até a próxima."); }
                        else StartFinal();
                    }
                    break;
                case Phase.Inter:
                    interT -= dt;
                    if (interT <= 0f) SpawnLine(line + 1);
                    break;
                case Phase.Final:
                    spawnT -= dt;
                    if (spawnT <= 0f && AliveCount() < 10)
                    {
                        float r = UnityEngine.Random.value, z = pp.z + U.Rand(18f, 26f), x = U.Rand(-7f, 7f) + PasDamim.CenterX(z);
                        Foe f = Foe.Spawn(r < 0.45f ? FoeType.Lanceiro : r < 0.75f ? FoeType.Falange : FoeType.Arqueiro, new Vector3(x, 0f, z), transform);
                        f.state = Foe.St.Advance;
                        spawnT = 2f;
                    }
                    // O marcador enche mais rápido quanto mais perto do fim do vale.
                    if (player.health > 0f && pp.z >= bannerZ - 4f) hold += dt * (1f + Mathf.Clamp01((pp.z - PasDamim.Lines[2]) / 30f));
                    if (hold >= P.holdSeconds)
                    {
                        if (!sword.stuck) sword.HandSticks();
                        phase = Phase.Done;
                        doneT = 2.5f;
                    }
                    break;
            }
        }

        void UpdatePrayer(float dt)
        {
            prayCool = Mathf.Max(0f, prayCool - dt);
            bool still = GameInput.Move().sqrMagnitude < 0.01f && !sword.Charging;
            if (GameInput.PrayHeld() && still)
            {
                if (prayCool > 0f)
                {
                    if (prayProgress == 0f) { ui.Toast("Aguarde " + Mathf.CeilToInt(prayCool) + " s para orar de novo.", 1f); prayProgress = -1f; }
                    return;
                }
                if (prayProgress <= 0f) { prayProgress = 0f; Sfx.Play("perfect", 0.5f, 0.5f); }
                sword.praying = true;
                prayProgress += dt;
                if (prayProgress >= 2f)
                {
                    player.courage = Mathf.Clamp(player.courage + 35f, 0f, 100f);
                    prayCool = 10f; prayProgress = 0f; sword.praying = false;
                    ui.Toast("Eleazar orou e encontrou coragem.", 1.8f);
                }
            }
            else { sword.praying = false; prayProgress = 0f; }
        }

        // ------------------------------------------------------------------ a trombeta

        /// <summary>Três soldados descem para ajudar (e o item "Lutou sem esperar o povo" se perde).</summary>
        public void BlowHorn()
        {
            if (hornUsed) return;
            hornUsed = true;
            Sfx.Play("horn");
            ui.Toast("Três soldados descem para ajudar.", 2f);
            Vector3 p = player.Position;
            for (int i = 0; i < 3; i++)
            {
                Man m = MakeMan(transform, "Soldado de Israel (reforço)", i, new Vector3(p.x + (i - 1) * 2f, 0f, p.z - 8f));
                m.cool = U.Rand(0.5f, 1.2f);
                allies.Add(m);
            }
        }

        void UpdateAllies(float dt)
        {
            foreach (Man a in allies)
            {
                Vector3 p = a.rig.root.position;
                Foe best = null; float bd = 1e9f;
                foreach (Foe f in Foe.All)
                {
                    if (!f.Alive || f.training) continue;
                    float d = Vector3.Distance(p, f.transform.position);
                    if (d < bd) { bd = d; best = f; }
                }
                a.cool -= dt;
                Vector3 tgt = best != null ? best.transform.position : player.Position + new Vector3(2f, 0f, -2f);
                Vector3 dv = tgt - p; dv.y = 0f;
                if (dv.magnitude > 1.8f)
                {
                    p += dv.normalized * 3.5f * dt;
                    for (int j = 0; j < 2; j++) a.rig.legs[j].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 10f + j * Mathf.PI) * 35f, 0f, 0f);
                }
                if (dv.sqrMagnitude > 0.0001f) a.rig.root.rotation = Quaternion.Euler(0f, U.YawTo(dv.x, dv.z), 0f);
                p.y = PasDamim.Height(p.x, p.z);
                a.rig.root.position = p;
                if (best != null && dv.magnitude < 2.2f && a.cool <= 0f) { a.cool = 1.2f; best.Hit(SwingKind.Quick, p, false); }
            }
        }

        // ------------------------------------------------------------------ queda e livramento

        public void Fall(string reason)
        {
            if (fallen || !Active) return;
            fallen = true; deaths++;
            sword.Cancel();
            if (onFall != null) onFall(reason);
        }

        /// <summary>Onde recomeçar depois de cair (Valente volta à linha 1).</summary>
        public int RestartLine() { return P.restartAtLine1 ? 1 : Mathf.Clamp(line, 1, 3); }
        public bool RestartsFinal { get { return phase == Phase.Final && !P.restartAtLine1; } }

        void Deliverance()
        {
            doneT = -1f;
            stuckAtEnd = sword.stuck;
            foreach (Foe f in Foe.All.ToArray()) if (!f.training) f.Flee(true);
            Sfx.Play("roar", 1f, 0.5f);
            // O povo desce a colina só para recolher os despojos.
            Vector3 p = player.Position;
            for (int i = 0; i < soldiers.Count; i++)
            {
                float a = i / (float)soldiers.Count * Mathf.PI * 2f;
                Vector3 s = new Vector3(p.x + Mathf.Sin(a) * 22f, 0f, p.z - 24f + Mathf.Cos(a) * 4f);
                s.y = PasDamim.Height(s.x, s.z);
                soldiers[i].rig.root.position = s;
                soldiers[i].target = new Vector3(p.x + Mathf.Sin(a) * U.Rand(3f, 9f), 0f, p.z + Mathf.Cos(a) * U.Rand(3f, 9f));
                soldiers[i].bend = 0.5f;
            }
            if (onDeliverance != null) onDeliverance();
        }
    }
}
