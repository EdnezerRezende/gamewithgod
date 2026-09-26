using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// A batalha no campo de lentilhas: o povo foge (e Samá pode fugir junto), quatro ondas de
    /// filisteus, integridade do campo, oração e a resistência final até o grande livramento.
    /// </summary>
    public class Battle : MonoBehaviour
    {
        public enum Phase { Idle, Flee, Wave, Interval, Final, Done }

        public PlayerController player;
        public SwordShield sword;
        public LentilField field;
        public UI ui;
        public Action onFled;
        public Action<string> onFall;
        public Action onDeliverance;

        public Phase phase = Phase.Idle;
        public int wave, stonesLeft = 5, deaths, repelled;
        public float integrity = 100f, hold, fieldTime, inFieldTime, courageStart = 70f;
        public bool fled;
        public float prayProgress;

        readonly List<PhilType> queue = new List<PhilType>();
        readonly List<Rig> soldiers = new List<Rig>();
        readonly List<float> soldierDelay = new List<float>();
        float spawnT, intervalT, fleeT, prayCool;
        bool soldiersRunning, fallen;

        public Fase2Params P { get { return Fase2Params.Current; } }
        public bool Active { get { return phase != Phase.Idle && phase != Phase.Done; } }
        public float HoldFraction { get { return phase == Phase.Final ? hold / P.holdSeconds : -1f; } }

        public string WaveLabel
        {
            get
            {
                switch (phase)
                {
                    case Phase.Flee: return "O povo foge";
                    case Phase.Final: return "Permaneça no meio do campo";
                    case Phase.Interval: return "Próxima onda em " + Mathf.CeilToInt(intervalT) + " s";
                    case Phase.Wave: return "Onda " + wave + " de 4";
                }
                return "";
            }
        }

        public void Wire()
        {
            field.onBurn = amount => { if (Active) integrity -= amount; };
            field.onExtinguished = () => { if (Active) { player.courage = Mathf.Clamp(player.courage + 4f, 0f, 100f); ui.Toast("Fogo apagado.", 1f); } };
            field.stomper = () => player.Position;
        }

        public void ClearEnemies()
        {
            foreach (Philistine p in Philistine.All.ToArray()) Destroy(p.gameObject);
            foreach (Arrow a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);
        }

        // ------------------------------------------------------------------ soldados de Israel

        public void PlaceSoldiers(Transform parent)
        {
            foreach (Rig r in soldiers) if (r.root != null) Destroy(r.root.gameObject);
            soldiers.Clear(); soldierDelay.Clear();
            Color[] robes = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52) };
            for (int i = 0; i < 10; i++)
            {
                float a = UnityEngine.Random.Range(-2.4f, 2.4f), r = UnityEngine.Random.Range(4f, 12f);
                Rig s = Models.Human(parent, "Soldado de Israel", 1.75f, robes[i % 3]);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                s.root.position = new Vector3(x, World.LentilHeight(x, z), z);
                s.root.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                soldiers.Add(s);
                soldierDelay.Add(UnityEngine.Random.Range(0f, 2.5f));
            }
            soldiersRunning = false;
        }

        public void RunSoldiers() { soldiersRunning = true; }

        public void UpdateSoldiers(float dt, Vector3 target, float speed)
        {
            for (int i = 0; i < soldiers.Count; i++)
            {
                Rig s = soldiers[i];
                if (s.root == null || !s.root.gameObject.activeSelf) continue;
                soldierDelay[i] -= dt;
                if (soldierDelay[i] > 0f) continue;
                Vector3 p = s.root.position, d = target - p; d.y = 0f;
                if (d.magnitude > 2f)
                {
                    p += d.normalized * speed * dt;
                    p.y = World.LentilHeight(p.x, p.z);
                    s.root.position = p;
                    s.root.rotation = Quaternion.Euler(0f, U.YawTo(d.x, d.z), 0f);
                    for (int j = 0; j < 2; j++) s.legs[j].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 16f + j * Mathf.PI) * 40f, 0f, 0f);
                }
                else if (target == LentilField.Camp) s.root.gameObject.SetActive(false);
            }
        }

        /// <summary>Depois do livramento, os que fugiram voltam e encontram o campo defendido.</summary>
        public void BringSoldiersBack()
        {
            for (int i = 0; i < soldiers.Count; i++)
            {
                Rig s = soldiers[i];
                float a = i / (float)soldiers.Count * Mathf.PI - Mathf.PI / 2f;
                float x = Mathf.Sin(a) * 20f, z = -30f - Mathf.Cos(a) * 4f;
                s.root.position = new Vector3(x, World.LentilHeight(x, z), z);
                s.root.gameObject.SetActive(true);
                soldierDelay[i] = 0f;
            }
        }

        // ------------------------------------------------------------------ fluxo

        public void Begin(int fromWave)
        {
            ClearEnemies();
            fallen = false;
            player.health = 100f;
            stonesLeft = 5;
            prayProgress = 0f;
            if (fromWave == 0)
            {
                integrity = 100f; fled = false; fieldTime = inFieldTime = 0f; deaths = 0; repelled = 0; sword.parries = 0;
                field.ResetField();
                player.courage = courageStart;
                player.Place(new Vector3(0f, 0f, 2f), 0f, 0f);
                PlaceSoldiers(transform);
                RunSoldiers();
                phase = Phase.Flee; fleeT = 9f; wave = 0;
                ui.SetObjective("O povo foge", "Os soldados de Israel correm para o acampamento. Você pode fugir com eles ou ficar no meio do campo.");
                Sfx.Play("bleat", 0.6f, 0.5f);
                ui.Toast("\"Fuja, Samá! Eles são muitos!\"", 3f);
                for (int i = 0; i < 3; i++)
                {
                    float a = UnityEngine.Random.Range(-0.5f, 0.5f);
                    Philistine p = Philistine.Spawn(PhilType.Lanceiro, new Vector3(Mathf.Sin(a) * 58f, 0f, Mathf.Cos(a) * 58f), transform);
                    p.speed *= 0.6f;
                }
            }
            else
            {
                integrity = Mathf.Max(integrity, 40f);
                player.courage = Mathf.Max(player.courage, courageStart * 0.8f);
                player.Place(Vector3.zero, 0f, 0f);
                BeginWave(fromWave);
            }
        }

        public void BeginFinal()
        {
            ClearEnemies();
            fallen = false;
            player.health = 100f;
            integrity = Mathf.Max(integrity, 40f);
            player.Place(Vector3.zero, 0f, 0f);
            wave = 4;
            StartFinal();
        }

        void BeginWave(int n)
        {
            wave = n; phase = Phase.Wave;
            queue.Clear();
            queue.AddRange(WaveList(n));
            spawnT = 0.5f;
            string[] txt = {
                "Lanceiros avançam sobre o campo.",
                "Arqueiros atiram de longe: escudo nas flechas, funda neles.",
                "Escudeiros e portadores de tocha, por dois lados. Golpe forte quebra o escudo.",
                "Todos juntos, com um capitão. Apare os golpes dele." };
            ui.SetObjective("Defenda o campo · Onda " + n + " de 4", txt[n - 1]);
            ui.Toast("Onda " + n, 1.4f);
        }

        void StartFinal()
        {
            phase = Phase.Final; hold = 0f; spawnT = 1f;
            ui.SetObjective("Permaneça no meio do campo", "São muitos demais. Não saia das lentilhas e não caia: o marcador só avança assim. Com pouca coragem, segure F (ou Orar) para orar.");
            ui.Toast("Permaneça!", 2f);
        }

        List<PhilType> WaveList(int n)
        {
            int c = P.waveCounts[n - 1];
            List<PhilType> L = new List<PhilType>();
            Action<PhilType, int> pick = (t, k) => { for (int i = 0; i < k; i++) L.Add(t); };
            if (n == 1) pick(PhilType.Lanceiro, c);
            if (n == 2) { int a = Mathf.RoundToInt(c * 0.4f); pick(PhilType.Arqueiro, a); pick(PhilType.Lanceiro, c - a); }
            if (n == 3) { int s = Mathf.RoundToInt(c * 0.4f), t = Mathf.RoundToInt(c * 0.3f); pick(PhilType.Escudeiro, s); pick(PhilType.Tocha, t); pick(PhilType.Lanceiro, c - s - t); }
            if (n == 4)
            {
                int r = c - 1, s = Mathf.RoundToInt(r * 0.3f), a = Mathf.RoundToInt(r * 0.2f);
                pick(PhilType.Escudeiro, s); pick(PhilType.Arqueiro, a); pick(PhilType.Tocha, a); pick(PhilType.Lanceiro, r - s - 2 * a);
            }
            for (int i = L.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); PhilType tmp = L[i]; L[i] = L[j]; L[j] = tmp; }
            if (n == 4) L.Add(PhilType.Capitao);
            return L;
        }

        Vector3 SpawnPoint(int n)
        {
            float a = n == 3 ? (UnityEngine.Random.value < 0.5f ? Mathf.PI / 2f : -Mathf.PI / 2f) + UnityEngine.Random.Range(-0.4f, 0.4f)
                             : UnityEngine.Random.Range(-1.6f, 1.6f);
            return new Vector3(Mathf.Sin(a) * 55f, 0f, Mathf.Cos(a) * 55f);
        }

        int AliveCount()
        {
            int n = 0;
            foreach (Philistine p in Philistine.All) if (p.Alive && !p.training) n++;
            return n;
        }

        void Update()
        {
            if (!Active || fallen) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool inField = LentilField.InField(player.Position), inCenter = LentilField.InCenter(player.Position);
            if (phase != Phase.Flee)
            {
                fieldTime += dt;
                if (inField) inFieldTime += dt;
                player.courage = Mathf.Clamp(player.courage + (inCenter ? 2f : inField ? 0.6f : -P.outLoss) * dt, 0f, 100f);
            }
            field.HighlightCenter(inCenter);
            UpdatePrayer(dt);

            // Fugir com o povo
            if (player.Position.z < -24f) { fled = true; if (onFled != null) onFled(); return; }

            switch (phase)
            {
                case Phase.Flee:
                    fleeT -= dt;
                    if (soldiersRunning) UpdateSoldiers(dt, LentilField.Camp, 6f);
                    if (fleeT <= 0f) { foreach (Rig s in soldiers) s.root.gameObject.SetActive(false); BeginWave(1); }
                    break;
                case Phase.Wave:
                    spawnT -= dt;
                    if (queue.Count > 0 && spawnT <= 0f)
                    {
                        Philistine.Spawn(queue[0], SpawnPoint(wave), transform);
                        queue.RemoveAt(0);
                        spawnT = 1.6f;
                    }
                    if (queue.Count == 0 && AliveCount() == 0)
                    {
                        if (wave < 4)
                        {
                            phase = Phase.Interval; intervalT = 12f; stonesLeft = 5;
                            ui.SetObjective("Respire", "Suas pedras foram recolhidas (5). Apague o fogo que restar e volte ao meio do campo.");
                        }
                        else StartFinal();
                    }
                    break;
                case Phase.Interval:
                    intervalT -= dt;
                    if (intervalT <= 0f) BeginWave(wave + 1);
                    break;
                case Phase.Final:
                    spawnT -= dt;
                    if (spawnT <= 0f && AliveCount() < P.capAlive)
                    {
                        float r = UnityEngine.Random.value;
                        PhilType t = r < 0.45f ? PhilType.Lanceiro : r < 0.65f ? PhilType.Escudeiro : r < 0.82f ? PhilType.Arqueiro : PhilType.Tocha;
                        float a = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
                        Philistine.Spawn(t, new Vector3(Mathf.Sin(a) * 55f, 0f, Mathf.Cos(a) * 55f), transform);
                        spawnT = 2.2f;
                    }
                    if (inField && player.health > 0f) hold += dt;
                    if (stonesLeft < 5 && UnityEngine.Random.value < dt * 0.08f) stonesLeft++;
                    if (hold >= P.holdSeconds) Deliverance();
                    break;
            }
            if (integrity <= 0f) Fall("O campo foi tomado.");
        }

        void UpdatePrayer(float dt)
        {
            prayCool = Mathf.Max(0f, prayCool - dt);
            bool still = GameInput.Move().sqrMagnitude < 0.01f && !sword.ShieldUp && !sword.Charging;
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
                    ui.Toast("Samá orou e encontrou coragem.", 1.8f);
                }
            }
            else { sword.praying = false; prayProgress = 0f; }
        }

        public void Fall(string reason)
        {
            if (fallen || phase == Phase.Done) return;
            fallen = true; deaths++;
            sword.Cancel();
            if (onFall != null) onFall(reason);
        }

        public void Trample(float amount) { if (Active && !fallen) integrity -= amount; }

        public int RestartWave()
        {
            int w = Mathf.Max(1, wave);
            return P.restartAtWave3 && w > 3 ? 3 : w;
        }

        public bool WasFinal { get { return phase == Phase.Final; } }

        void Deliverance()
        {
            phase = Phase.Done;
            foreach (Philistine p in Philistine.All.ToArray()) p.Flee(true);
            Sfx.Play("roar", 1f, 0.5f);
            if (onDeliverance != null) onDeliverance();
        }
    }
}
