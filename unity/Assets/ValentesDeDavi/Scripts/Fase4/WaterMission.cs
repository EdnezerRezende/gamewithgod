using System;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// A missão da fase 4: a escolha na caverna, romper pelo arraial (ou o desvio pelas colinas), a
    /// cisterna junto à porta (ou o poço no campo), a volta com a água e a chegada a Davi.
    /// </summary>
    public class WaterMission : MonoBehaviour
    {
        public enum Stage { Idle, Choice, Out, Back, Done }

        public PlayerController player;
        public ValenteArms arms;
        public UI ui;
        public Action<string> onFall;
        public Action onEmpty, onDeliver;
        public Action spawnCompanions;

        public Stage stage = Stage.Idle;
        public float alarm, time, courageStart = 70f, prayProgress, waterEnd;
        public int deaths, repelled;
        public bool fieldWell, together, warnedWell, crossed, throughCamp;
        public string checkpoint = "cave";

        float choiceT, spawnT, prayCool;
        bool choiceHint, warnedHills, fallen;

        public Fase4Params P { get { return Fase4Params.Current; } }
        public bool Active { get { return stage == Stage.Out || stage == Stage.Back || stage == Stage.Choice; } }

        public void RaiseAlarm(float amount) { if (stage == Stage.Out || stage == Stage.Back) alarm = Mathf.Clamp(alarm + amount * P.alarm, 0f, 100f); }

        public string Label
        {
            get
            {
                string s = stage == Stage.Choice ? "A caverna de Adulão" : stage == Stage.Out ? "Rumo a Belém" : stage == Stage.Back ? "A volta com a água" : "";
                return s == "" ? "" : s + " · Ordem: " + (Companion.Follow ? "Comigo" : "Segurem");
            }
        }

        public string AllyWarning
        {
            get
            {
                if (stage != Stage.Out && stage != Stage.Back) return null;
                foreach (Companion c in Companion.All) if (c.down) return "Um companheiro caiu. Chegue perto e segure E (Ação) para levantá-lo.";
                foreach (Companion c in Companion.All)
                    if (Vector3.Distance(c.transform.position, player.Position) > 25f)
                        return Companion.Follow ? "Um companheiro ficou para trás." : "Os companheiros ficaram para trás. Q: \"Comigo\".";
                return null;
            }
        }

        public void ClearEnemies()
        {
            foreach (CampFoe f in CampFoe.All.ToArray()) if (!f.training) Destroy(f.gameObject);
            foreach (Arrow a in FindObjectsByType<Arrow>(FindObjectsSortMode.None)) Destroy(a.gameObject);
        }

        /// <summary>Começa a missão: "choice" (na caverna, antes de pegar o cântaro) ou um ponto de recomeço.</summary>
        public void Begin(string at)
        {
            ClearEnemies();
            fallen = false;
            player.health = 100f;
            prayProgress = 0f;
            arms.Cancel(); arms.carrying = false; arms.water = 0f; arms.trainingMode = false;
            Companion.Follow = true;
            if (at == "choice")
            {
                alarm = 0f; time = 0f; deaths = repelled = 0; waterEnd = 0f;
                fieldWell = together = warnedWell = crossed = throughCamp = false;
                choiceT = 0f; choiceHint = warnedHills = false; checkpoint = "cave";
                arms.parries = 0;
                player.courage = courageStart;
                player.Place(new Vector3(1f, 0f, -66.5f), Mathf.PI, -0.08f);
                stage = Stage.Choice;
                ui.SetObjective("Davi não deu ordem", "Ninguém mandou. Se quiser atender ao desejo de Davi, pegue o cântaro vazio na entrada da caverna (segure E).");
            }
            else
            {
                Vector3 spot = at == "cave" ? new Vector3(0f, 0f, -62f) : at == "field" ? new Vector3(0f, 0f, 44f) : Refaim.Cistern + new Vector3(-2f, 0f, -5f);
                player.Place(spot, 0f, 0f);
                alarm = at == "cave" ? alarm * 0.5f : Mathf.Max(alarm, 40f);
                stage = Stage.Out;
                ui.SetObjective(at == "cistern" ? "A cisterna junto à porta" : "Romper pelo arraial",
                    at == "cistern" ? "Tire a água de novo e volte para Adulão." : "A cisterna de Belém fica junto à porta, do outro lado do vale.");
            }
            Populate();
            spawnCompanions();
        }

        /// <summary>O arraial: dorminhocos junto às fogueiras, sentinelas nos postes, tochas de ronda,
        /// poucas patrulhas nas colinas e a guarnição na porta de Belém.</summary>
        void Populate()
        {
            float k = P.camp;
            foreach (Vector3 f in Refaim.Fires)
            {
                if (f.z < Refaim.CampZ0 - 2f) continue;
                int n = Mathf.RoundToInt(2f * k + UnityEngine.Random.Range(-0.4f, 0.4f));
                for (int i = 0; i < n; i++)
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    CampFoe.Spawn(UnityEngine.Random.value < 0.2f ? CampFoeType.Tocha : CampFoeType.Lanceiro, f + new Vector3(Mathf.Cos(a) * 2.2f, 0f, Mathf.Sin(a) * 2.2f), transform, CampFoe.St.Sleep, UnityEngine.Random.Range(0f, 360f));
                }
            }
            foreach (Vector3 p in Refaim.Posts) CampFoe.Spawn(CampFoeType.Sentinela, p + new Vector3(1.5f, 0f, -1.5f), transform, CampFoe.St.Watch, 180f + UnityEngine.Random.Range(-35f, 35f));
            if (k >= 1f) { CampFoe.Spawn(CampFoeType.Sentinela, new Vector3(-9f, 0f, Refaim.CampZ0 - 3f), transform, CampFoe.St.Watch); CampFoe.Spawn(CampFoeType.Sentinela, new Vector3(9f, 0f, Refaim.CampZ0 - 3f), transform, CampFoe.St.Watch); }
            else CampFoe.Spawn(CampFoeType.Sentinela, new Vector3(0f, 0f, Refaim.CampZ0 - 3f), transform, CampFoe.St.Watch);
            for (int i = 0; i < Mathf.RoundToInt(3f * k); i++)
                CampFoe.Spawn(CampFoeType.Tocha, new Vector3(UnityEngine.Random.Range(-14f, 14f), 0f, UnityEngine.Random.Range(Refaim.CampZ0 + 6f, Refaim.CampZ1 - 6f)), transform, CampFoe.St.Watch).patrol = true;
            CampFoe.Spawn(CampFoeType.Lanceiro, new Vector3(36f, 0f, 2f), transform, CampFoe.St.Watch).patrol = true;
            CampFoe.Spawn(CampFoeType.Lanceiro, new Vector3(-37f, 0f, 18f), transform, CampFoe.St.Watch).patrol = true;
            if (k >= 1f) CampFoe.Spawn(CampFoeType.Tocha, new Vector3(38f, 0f, 22f), transform, CampFoe.St.Watch).patrol = true;
            int g = Mathf.RoundToInt(4f * k);
            for (int i = 0; i < g; i++)
                CampFoe.Spawn(CampFoeType.Guarda, new Vector3(-6f + i * 12f / Mathf.Max(1, g - 1), 0f, Refaim.GateZ - 3f - UnityEngine.Random.Range(0f, 1.5f)), transform, CampFoe.St.Watch).wakeAt = 999f;
            for (int s = -1; s <= 1; s += 2)
            {
                CampFoe a = CampFoe.Spawn(CampFoeType.Arqueiro, new Vector3(s * 5f, 0f, Refaim.GateZ - 1.4f), transform, CampFoe.St.Watch);
                a.fixedPlace = true; a.wakeAt = 999f;
            }
            CampFoe.Spawn(CampFoeType.Tocha, new Vector3(1f, 0f, Refaim.GateZ - 6f), transform, CampFoe.St.Watch).wakeAt = 999f;
        }

        public void TakeJar()
        {
            stage = Stage.Out;
            Sfx.Play("roar", 0.3f, 1.8f);
            Music.Set("duel");
            ui.Toast("\"Vamos.\" Os três saem da caverna.", 2f);
            ui.SetObjective("Romper pelo arraial", "A cisterna de Belém fica junto à porta, do outro lado do vale. O caminho do relato passa pelo meio do arraial filisteu. Q: ordens aos companheiros.");
        }

        public void DrawWater(bool field)
        {
            arms.carrying = true; arms.water = 100f;
            fieldWell = field; stage = Stage.Back; checkpoint = field ? "field" : "cistern";
            alarm = Mathf.Max(alarm, 40f); spawnT = 2f;
            Sfx.Play("water");
            ui.Toast(field ? "O cântaro está cheio." : "Água da cisterna de Belém!", 1.8f);
            ui.SetObjective("A volta com a água", "Leve a água a Davi, na caverna de Adulão. Sem escudo: golpes, correria e desvios derramam água. Os companheiros protegem você.");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || fallen || !player.controlling) return;
            if (stage == Stage.Choice)
            {
                choiceT += dt;
                if (choiceT > 25f && !choiceHint) { choiceHint = true; ui.Toast("Os dois companheiros pegam as armas e olham para você.", 3f); }
                return;
            }
            if (stage != Stage.Out && stage != Stage.Back) return;
            time += dt;
            UpdatePrayer(dt);
            Vector3 pp = player.Position;
            if (stage == Stage.Out)
            {
                if (!warnedHills && Mathf.Abs(pp.x) > Refaim.CampHalfWidth + 8f && pp.z > Refaim.CampZ0 - 6f && pp.z < Refaim.CampZ1)
                {
                    warnedHills = true;
                    ui.Toast("O desvio pelas colinas é mais seguro, mas o relato diz: romperam pelo arraial.", 3.2f);
                }
                if (!crossed && pp.z > 5f)
                {
                    crossed = true;
                    throughCamp = Mathf.Abs(pp.x) < Refaim.CampHalfWidth + 4f;
                    if (throughCamp) ui.Toast("Pelo meio do arraial!", 1.4f);
                }
                if (pp.z > 42f && checkpoint == "cave") checkpoint = "field";
                // A guarnição da porta acorda quando os três chegam perto.
                if (Vector3.Distance(pp, Refaim.Cistern) < 16f)
                    foreach (CampFoe f in CampFoe.All) if (f.state == CampFoe.St.Watch && f.transform.position.z > Refaim.GateZ - 10f) f.Wake(false);
            }
            else
            {
                spawnT -= dt;
                int awake = 0;
                foreach (CampFoe f in CampFoe.All) if (f.Awake) awake++;
                if (spawnT <= 0f && pp.z > Refaim.CampZ0 - 10f && awake < Mathf.RoundToInt(5f * P.camp) && alarm > 30f && Refaim.Tents.Count > 0)
                {
                    Vector3 t = Refaim.Tents[UnityEngine.Random.Range(0, Refaim.Tents.Count)];
                    float d = new Vector2(t.x - pp.x, t.z - pp.z).magnitude;
                    if (d < 34f && d > 8f) CampFoe.Spawn(UnityEngine.Random.value < 0.3f ? CampFoeType.Tocha : CampFoeType.Lanceiro, t, transform, CampFoe.St.Advance);
                    spawnT = 3.5f / P.camp;
                }
                if (pp.z < Refaim.CaveZ + 1.5f && Mathf.Abs(pp.x) < 9f) Deliver();
            }
        }

        void UpdatePrayer(float dt)
        {
            prayCool = Mathf.Max(0f, prayCool - dt);
            bool still = GameInput.Move().sqrMagnitude < 0.01f && !arms.Charging;
            if (GameInput.PrayHeld() && still)
            {
                if (prayCool > 0f)
                {
                    if (prayProgress == 0f) { ui.Toast("Aguarde " + Mathf.CeilToInt(prayCool) + " s para orar de novo.", 1f); prayProgress = -1f; }
                    return;
                }
                if (prayProgress <= 0f) { prayProgress = 0f; Sfx.Play("perfect", 0.5f, 0.5f); }
                arms.praying = true;
                prayProgress += dt;
                if (prayProgress >= 2f)
                {
                    player.courage = Mathf.Clamp(player.courage + 35f, 0f, 100f);
                    prayCool = 10f; prayProgress = 0f; arms.praying = false;
                    ui.Toast("O valente orou e encontrou coragem.", 1.8f);
                }
            }
            else { arms.praying = false; prayProgress = 0f; }
        }

        void Deliver()
        {
            stage = Stage.Done;
            waterEnd = arms.water;
            together = true;
            foreach (Companion c in Companion.All) if (c.down || Vector3.Distance(c.transform.position, player.Position) > 20f) together = false;
            if (!together) ui.Toast("Faltou um dos três.", 2f);
            arms.carrying = false;
            foreach (CampFoe f in CampFoe.All.ToArray()) if (f.Awake) f.Flee(false);
            if (onDeliver != null) onDeliver();
        }

        public void Fall(string reason)
        {
            if (fallen || (stage != Stage.Out && stage != Stage.Back)) return;
            fallen = true; deaths++;
            arms.Cancel();
            if (onFall != null) onFall(reason);
        }

        /// <summary>Onde recomeçar depois de cair ou de perder a água.</summary>
        public string RestartPoint(bool afterEmpty)
        {
            if (P.restartAtCave) return "cave";
            if (stage == Stage.Back || afterEmpty) return fieldWell ? "field" : "cistern";
            return checkpoint;
        }
    }
}
