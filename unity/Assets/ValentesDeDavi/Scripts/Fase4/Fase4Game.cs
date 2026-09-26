using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 4: os três valentes e a água de Belém (2 Samuel 23:13-17). Monta o mundo e conduz o fluxo:
    /// abertura → treino → o desejo de Davi → romper pelo arraial → a cisterna → a volta com a água →
    /// Davi derrama a água perante o Senhor → resultado.
    /// </summary>
    public class Fase4Game : MonoBehaviour
    {
        public const string SceneName = "Fase4_Agua";
        enum Mode { Menu, Cine, Training, Mission, Result }
        enum Kind { Full, TrainingOnly, MissionOnly }

        class Act { public string id, label, doing; public float need; public Companion ally; }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        ValenteArms arms;
        WaterMission mission;
        WaterTraining training;
        Cutscene cutscene;
        UI ui;
        Figure david;
        Transform davidJar, jarProp;
        readonly List<Figure> cineMen = new List<Figure>();

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        bool modal;
        string medal;
        int trainingScore;
        float savedTimeScale = 1f, actT;
        Act act;

        void Start()
        {
            Game.Paused = false;
            Time.timeScale = 1f;
#if !ENABLE_INPUT_SYSTEM
            Input.simulateMouseWithTouches = false;
#endif
            Mats.Init(baseMaterial);
            Sfx.Create(transform);
            Music.Create(transform);
            Narration.Create(transform);
            world = World.ForRefaim(transform, skyMaterial);
            Fx.SetWorld(world);
            Arrow.ground = Refaim.Height;

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);

            // Davi, seus homens e o cântaro vazio à entrada da caverna
            david = Figure.Man(transform, "Davi", U.Hex(0x3d5a8a), false, 1.78f);
            davidJar = Figure.Jar(david.armR);
            davidJar.localPosition = new Vector3(0f, -0.7f, 0.15f);
            PlaceDavid(true);
            for (int i = 0; i < 6; i++)
            {
                Figure m = Figure.Man(transform, "Homem de Davi", i % 3 == 0 ? U.Hex(0x6f6a3c) : i % 3 == 1 ? U.Hex(0x7d5f3a) : U.Hex(0x8a7a52), false);
                float x = (i < 3 ? -1f : 1f) * UnityEngine.Random.Range(3.5f, 6f), z = UnityEngine.Random.Range(-76f, -72f);
                m.root.position = new Vector3(x, Refaim.Height(x, z), z);
                m.root.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-35f, 35f), 0f);
                if (i % 2 == 1) m.Spear();
            }
            jarProp = Figure.Jar(transform);
            PlaceJar(true);

            GameObject p = new GameObject("O valente");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            arms = p.AddComponent<ValenteArms>();
            arms.player = player; arms.ui = ui;
            arms.Build();

            mission = gameObject.AddComponent<WaterMission>();
            mission.player = player; mission.arms = arms; mission.ui = ui;
            mission.onFall = OnFall;
            mission.onDeliver = OnDeliver;
            mission.spawnCompanions = SpawnCompanions;
            arms.onPlayerDown = reason => mission.Fall(reason);
            arms.onEmpty = () => { if (mode == Mode.Mission && mission.stage == WaterMission.Stage.Back) OnEmpty(); };

            training = gameObject.AddComponent<WaterTraining>();
            training.player = player; training.arms = arms; training.ui = ui;
            training.onFinished = OnTrainingFinished;
            training.spawnCompanions = SpawnCompanions;

            CampFoe.Ctx = new CampFoe.Context
            {
                player = player, arms = arms, ui = ui,
                alarm = () => mission.alarm,
                raiseAlarm = mission.RaiseAlarm,
                onDown = f => { if (!f.training) mission.repelled++; },
                hunting = () => mission.stage == WaterMission.Stage.Out || mission.stage == WaterMission.Stage.Back,
                inTraining = () => mode == Mode.Training,
            };

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · botão direito · escudo · Espaço · desviar · Q · ordem · E · ação · F · orar · M · música · N · narração");
            ShowMenu();
        }

        void PlaceDavid(bool sitting)
        {
            Vector3 d = Refaim.DavidPos;
            david.root.position = new Vector3(d.x, Refaim.Height(d.x, d.z) - (sitting ? 0.45f : 0f), d.z);
            david.root.rotation = Quaternion.Euler(0f, sitting ? 9f : 0f, 0f);
            foreach (Transform l in david.legs) l.localRotation = Quaternion.Euler(sitting ? -75f : 0f, 0f, 0f);
            david.armR.localRotation = Quaternion.identity;
            davidJar.localRotation = Quaternion.identity;
            davidJar.gameObject.SetActive(false);
        }

        void PlaceJar(bool v)
        {
            Vector3 j = Refaim.JarPos;
            jarProp.position = new Vector3(j.x, Refaim.Height(j.x, j.z) + 0.23f, j.z);
            jarProp.gameObject.SetActive(v);
        }

        void SpawnCompanions()
        {
            foreach (Companion c in Companion.All.ToArray()) Destroy(c.gameObject);
            Companion.All.Clear();
            Companion.Follow = true;
            for (int s = -1; s <= 1; s += 2)
                Companion.Spawn(transform, player, ui, s, player.Position + player.Right * s * 2.2f - player.Forward * 1.5f, mode == Mode.Training);
        }

        void ClearCompanions()
        {
            foreach (Companion c in Companion.All.ToArray()) Destroy(c.gameObject);
            Companion.All.Clear();
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            Refaim.Flicker();
            bool playing = (mode == Mode.Training || mode == Mode.Mission) && !cutscene.Playing && !modal && mission.stage != WaterMission.Stage.Done;
            if (!Game.Paused && (playing || cutscene.Playing) && GameInput.PausePressed()) ShowPause();

            if (TouchControls.Active && !Application.isMobilePlatform && GameInput.MouseUsed()) TouchControls.Active = false;
            bool control = playing && !Game.Paused && !ui.OverlayOpen;
            bool lockCursor = control && !TouchControls.Active;
            UnityEngine.Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !lockCursor;
            player.controlling = control;
            arms.active = control;
            arms.SetVisible(control);
            player.speedMultiplier = arms.praying || actT > 0f ? 0f : arms.ShieldUp ? 0.55f : 1f;
            player.eyeHeight = Mathf.Lerp(player.eyeHeight, arms.praying || actT > 0f ? 1.0f : PlayerController.EyeHeight, Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            player.swayScale = 0.4f;
            player.clampPosition = mode == Mode.Training ? (Func<Vector3, Vector3>)Refaim.ClampTraining : Refaim.Clamp;

            if (control) UpdateAction(Time.deltaTime); else { act = null; actT = 0f; }
            if (control && GameInput.OrderPressed()) ToggleOrder();

            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Game.Paused && !ui.OverlayOpen);
            TouchControls.SwapIsDash = false;
            TouchControls.SetCombatButtons(true);
            TouchControls.SetFireLabel("Golpe");
            TouchControls.SetShieldLabel(arms.carrying ? "Aparar" : "Escudo");
            TouchControls.SetSwapLabel("Ordem", 15f);
            TouchControls.ShowPhase4Buttons(true, act != null ? act.label : null);

            if (GameInput.NarrationPressed()) { Settings.Narration = !Settings.Narration; ui.Toast(Settings.Narration ? "Narração ligada" : "Narração desligada", 1.2f); }
            if (GameInput.MusicPressed()) { Settings.Music = !Settings.Music; ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f); }

            if (playing) UpdateHud();

            if (mode == Mode.Menu && !Game.Paused)
            {
                float a = Time.time * 0.05f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 34f, 18f, Mathf.Cos(a) * 34f + 4f);
                cam.transform.LookAt(new Vector3(0f, 2f, 6f));
            }
        }

        void ToggleOrder()
        {
            Companion.Follow = !Companion.Follow;
            if (!Companion.Follow) foreach (Companion c in Companion.All) c.holdPos = c.transform.position;
            Sfx.Play("roar", 0.25f, 1.9f);
            ui.Toast(Companion.Follow ? "\"Comigo!\" — os dois seguem você." : "\"Segurem aqui!\" — os dois defendem este ponto.", 1.6f);
        }

        Act CurrentAction()
        {
            Vector3 pp = player.Position;
            foreach (Companion c in Companion.All)
                if (c.down && Vector3.Distance(c.transform.position, pp) < 2.4f) return new Act { id = "revive", ally = c, need = 2.5f, label = "Levantar", doing = "levantando..." };
            if (mode != Mode.Mission) return null;
            if (mission.stage == WaterMission.Stage.Choice && jarProp.gameObject.activeSelf && Flat(pp - Refaim.JarPos) < 2.2f)
                return new Act { id = "jar", need = 0.6f, label = "Pegar o cântaro", doing = "pegando..." };
            if (mission.stage == WaterMission.Stage.Out)
            {
                if (Flat(pp - Refaim.Cistern) < 2.6f) return new Act { id = "cistern", need = mission.P.drawSeconds, label = "Tirar água", doing = "tirando água..." };
                if (Flat(pp - Refaim.FieldWell) < 2.4f) return new Act { id = "well", need = 2.5f, label = "Tirar água do poço", doing = "tirando água..." };
            }
            return null;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>Ação contextual (segurar E): pegar o cântaro, tirar água, levantar um companheiro.</summary>
        void UpdateAction(float dt)
        {
            Act a = CurrentAction();
            if (a == null || act == null || a.id != act.id) actT = 0f;
            act = a;
            if (act == null || !GameInput.ActionHeld() || GameInput.Move().sqrMagnitude > 0.01f) { actT = 0f; return; }
            if (act.id == "well" && !mission.warnedWell) { actT = 0f; AskWell(); return; }
            actT += dt;
            if (actT < act.need) return;
            actT = 0f;
            switch (act.id)
            {
                case "revive": act.ally.Revive(); break;
                case "jar": PlaceJar(false); mission.TakeJar(); break;
                case "cistern": mission.DrawWater(false); break;
                case "well": mission.DrawWater(true); break;
            }
        }

        void UpdateHud()
        {
            bool m = mode == Mode.Mission;
            ui.SetFase4Stats(m, training.score, player.health, player.courage, arms.carrying, arms.water,
                m && mission.stage != WaterMission.Stage.Choice, mission.alarm,
                m ? mission.Label : "Ordem: " + (Companion.Follow ? "Comigo" : "Segurem"), m ? mission.AllyWarning : null);
            if (mission.prayProgress > 0f) { ui.DrawRing(mission.prayProgress / 2f, UI.BronzeHi, "orando..."); return; }
            if (act != null && actT > 0f) { ui.DrawRing(actT / act.need, U.Hex(0x6aa6cf), act.doing); return; }
            if (act != null) { ui.DrawRing(0f, UI.Olive, (TouchControls.Active ? "Ação: " : "E: ") + act.label); return; }
            string t = arms.Charging ? (arms.ChargeFraction >= 1f ? "golpe forte!" : "segure: forte")
                : arms.Crit ? "crítico!"
                : arms.ShieldUp ? "escudo erguido"
                : arms.comboT > 0f && arms.combo > 0 ? "sequência " + arms.combo + " de 3"
                : "clique: golpe";
            ui.DrawRing(arms.Charging ? arms.ChargeFraction : 0f, arms.ChargeFraction >= 1f ? UI.BronzeHi : UI.Olive, t);
        }

        static void Look(Camera c, Vector3 pos, Vector3 target) { c.transform.position = pos; c.transform.LookAt(target); }

        // ------------------------------------------------------------------ telas

        void ShowMenu()
        {
            Game.Paused = false; modal = false;
            Time.timeScale = 1f;
            cutscene.Abort();
            training.Clear();
            mission.stage = WaterMission.Stage.Idle;
            mission.ClearEnemies();
            ClearCompanions();
            ClearCineMen();
            arms.ResetState();
            PlaceDavid(true); PlaceJar(true);
            mode = Mode.Menu;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 4 · Os três valentes");
            c.Title("Os três valentes e a água de Belém", 50);
            c.Lede("2 Samuel 23:13-17. Davi só diz um desejo. Três valentes rompem pelo arraial filisteu, tiram água da cisterna junto à porta de Belém e a trazem a ele.");
            c.PhaseMap(4);
            if (!Progress.IsOpen(4))
            {
                c.Locked(4);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase4Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a missão", false, StartMissionOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Botão direito", "Espaço", "Q", "E (segurar)", "F (segurar) · Shift", "Esc · M · N" },
                new[] { "Golpe · três rápidos formam a sequência · segure e solte para o golpe forte",
                        "Segurar: escudo · no instante do golpe: aparar (com o cântaro na mão, só aparar)",
                        "Desviar (com o cântaro, derrama um pouco)", "Ordem aos companheiros: \"Comigo\" ou \"Segurem aqui\"",
                        "Ação: pegar o cântaro, tirar água, levantar um companheiro", "Orar · correr (com o cântaro, derrama)", "Pausa · música · narração" });
            c.Note("No celular: direcional, arrastar para olhar e os botões Golpe, Escudo, Desviar, Ordem, Orar e Ação. Modelos, sons e versículos provisórios.");
        }

        void SettingsRow(Card c, Action redraw)
        {
            VisualElement row = c.Row();
            Card.Btn(row, "Música: " + (Settings.Music ? "ligada" : "desligada"), false, () => { Settings.Music = !Settings.Music; redraw(); });
            Card.Btn(row, "Narração: " + (Settings.Narration ? "ligada" : "desligada"), false, () => { Settings.Narration = !Settings.Narration; redraw(); });
            Card.Btn(row, "Como jogar", false, () => Tutorial.Show(ui, null, redraw));
        }

        void ShowPause()
        {
            Game.Paused = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            arms.Cancel();
            RenderPause();
        }

        void RenderPause()
        {
            bool inCine = cutscene.Playing;
            Card c = ui.OpenCard();
            c.Eyebrow("Pausa · " + (inCine ? "Cena animada" : mode == Mode.Training ? "Treino" : "Missão") + " · " + Difficulty.Current.name);
            c.Title("Jogo pausado");
            VisualElement row = c.Row();
            Card.Btn(row, "Continuar", true, Resume);
            if (inCine) Card.Btn(row, "Pular a cena", false, () => { Resume(); cutscene.End(); });
            else Card.Btn(row, mode == Mode.Training ? "Recomeçar o treino" : "Recomeçar a missão", false, () =>
            {
                Resume();
                if (mode == Mode.Training) StartTraining(); else StartMission("choice");
            });
            Card.Btn(row, "Voltar ao menu", false, ShowMenu);
            SettingsRow(c, RenderPause);
        }

        void Resume()
        {
            Game.Paused = false;
            Time.timeScale = savedTimeScale;
            ui.CloseOverlay();
        }

        // ------------------------------------------------------------------ fluxo

        void StartFull()
        {
            if (!Tutorial.Seen) { Tutorial.Show(ui, StartFull, ShowMenu); return; }
            kind = Kind.Full; medal = null;
            ui.CloseOverlay();
            IntroCine(StartTraining);
        }

        void StartMissionOnly()
        {
            kind = Kind.MissionOnly; medal = null;
            mission.courageStart = 65 + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
            ui.CloseOverlay();
            DesireCine(() => StartMission("choice"));
        }

        void StartTraining()
        {
            Music.Set("training");
            mission.stage = WaterMission.Stage.Idle;
            mission.ClearEnemies();
            mode = Mode.Training;
            player.Place(Refaim.TrainingCenter + new Vector3(0f, 0f, -4f), 0f, 0f);
            player.health = 100f; player.courage = 70f;
            PlaceDavid(true); PlaceJar(true);
            training.Begin();
        }

        void OnTrainingFinished(WaterTraining t)
        {
            mode = Mode.Result;
            ClearCompanions();
            medal = t.medal; trainingScore = t.score;
            mission.courageStart = t.StartingCourage();
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("Os três e o cântaro");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Marcas seguradas", "Água no fim do percurso", "Companheiros levantados", "Coragem inicial na missão", "Pontos" },
                    new[] { t.marks + " de 3", t.waterLeft + "%", t.revives.ToString(), mission.courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f4v13");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a missão", true, () => { ui.CloseOverlay(); DesireCine(() => StartMission("choice")); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartMission(string at)
        {
            Music.Set(at == "choice" ? "quiet" : "duel");
            training.Clear();
            modal = false;
            mode = Mode.Mission;
            CampFoe.Ctx.onDown = f => { if (!f.training) mission.repelled++; };
            if (at == "choice") { PlaceJar(true); PlaceDavid(true); } else PlaceJar(false);
            mission.Begin(at);
        }

        void AskWell()
        {
            modal = true;
            arms.Cancel();
            Card c = ui.OpenCard();
            c.Eyebrow("O poço no campo");
            c.Title("Tirar água deste poço?");
            c.Lede("Aqui não há guardas, e a água é a mesma para matar a sede. Mas Davi falou de um lugar:");
            c.Verse("f4vgate");
            VisualElement row = c.Row();
            Card.Btn(row, "Tirar água daqui", false, () => { mission.warnedWell = true; ui.CloseOverlay(); modal = false; });
            Card.Btn(row, "Seguir para a cisterna", true, () => { mission.warnedWell = true; ui.CloseOverlay(); modal = false; ui.Toast("A cisterna fica junto à porta de Belém.", 1.8f); });
            c.Note("Tirar água deste poço faz perder o item de Fidelidade \"Da cisterna junto à porta\".");
        }

        void OnEmpty()
        {
            if (modal) return;
            modal = true;
            arms.Cancel();
            Music.Set("quiet");
            string at = mission.RestartPoint(true);
            Card c = ui.OpenCard();
            c.Eyebrow("A volta · " + Difficulty.Current.name);
            c.Title("O cântaro ficou vazio");
            c.Lede(at == "cave" ? "Vocês voltam ao começo do vale para tentar de novo." : "Vocês voltam para tirar água de novo.");
            c.Verse("f4v16a");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () => { ui.CloseOverlay(); StartMission(at); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void OnFall(string reason)
        {
            modal = true;
            Music.Set("quiet");
            StartCoroutine(ShowFall(reason));
        }

        IEnumerator ShowFall(string reason)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            string at = mission.RestartPoint(false);
            Card c = ui.OpenCard();
            c.Eyebrow("Missão · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede(at == "cave" ? "Vocês recomeçam na saída da caverna, com o cântaro vazio."
                 : at == "field" ? "Vocês recomeçam depois do arraial, com o cântaro vazio."
                 : "Vocês recomeçam perto da cisterna, com o cântaro vazio.");
            c.Verse("f4v17");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () => { ui.CloseOverlay(); StartMission(at); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cenas

        void ClearCineMen()
        {
            foreach (Figure f in cineMen) if (f.root != null) Destroy(f.root.gameObject);
            cineMen.Clear();
        }

        void WalkMen(float dt, float stopZ)
        {
            foreach (Figure f in cineMen)
            {
                Vector3 p = f.root.position;
                if (p.z > stopZ) { p.z -= 2.4f * dt; p.y = Refaim.Height(p.x, p.z); f.root.position = p; f.Walk(2.4f); }
                else f.Stand();
            }
        }

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            mission.ClearEnemies(); ClearCompanions(); ClearCineMen();
            PlaceDavid(true); PlaceJar(true);
            Color[] robes = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52) };
            for (int i = 0; i < 3; i++)
            {
                Figure f = Figure.Man(transform, "Um dos três", robes[i], false);
                float x = -1.5f + i * 1.5f, z = -40f - i * 1.2f;
                f.root.position = new Vector3(x, Refaim.Height(x, z), z);
                f.root.rotation = Quaternion.Euler(0f, 180f, 0f);
                cineMen.Add(f);
            }
            cutscene.Play(new List<Shot>
            {
                new Shot("f4v13", 7f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-9f, -5f, p), Mathf.Lerp(12f, 6f, p), Mathf.Lerp(-36f, -52f, p)), new Vector3(0f, 1.5f, Mathf.Lerp(-44f, -70f, p)))) { update = p => WalkMen(Time.deltaTime, -66f) },
                new Shot("f4v14", 6.5f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-3f, 3f, p), 24f, -64f), new Vector3(0f, 4f, 84f))) { update = p => WalkMen(Time.deltaTime, -66f) },
            }, () => { ClearCineMen(); then(); });
        }

        void DesireCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            mission.ClearEnemies(); ClearCompanions();
            PlaceDavid(true); PlaceJar(true);
            Vector3 dp = david.root.position;
            cutscene.Play(new List<Shot>
            {
                new Shot("f4v15", 7f, (c, p) => Look(c, dp + new Vector3(1.4f - p * 0.8f, 1.7f, 2.2f + p * 1.5f),
                    Vector3.Lerp(dp + Vector3.up * 1.1f, new Vector3(0f, 5f, 84f), p))),
            }, then);
        }

        void OnDeliver()
        {
            Music.Set("victory");
            arms.Cancel();
            ClearCineMen();
            Figure hero = Figure.Man(transform, "O valente", U.Hex(0x8a7a52), false);
            hero.root.position = new Vector3(0.4f, Refaim.Height(0.4f, -70.5f), -70.5f);
            hero.root.rotation = Quaternion.Euler(0f, 180f, 0f);
            cineMen.Add(hero);
            int i = 0;
            foreach (Companion c in Companion.All) { c.PlaceAt(new Vector3(i == 0 ? -1.4f : 2f, 0f, -69.6f), 180f); i++; }
            PlaceDavid(false);
            davidJar.gameObject.SetActive(true);
            Vector3 dp = david.root.position;
            Shot pour = new Shot("f4v16b", 6f, (c, p) => Look(c, dp + new Vector3(2.6f, 1.6f, 3.2f - p * 0.6f), dp + Vector3.up * 1.1f));
            pour.start = () => Sfx.Play("pour");
            pour.update = p =>
            {
                david.armR.localRotation = Quaternion.Euler(-Mathf.Min(1f, p * 3f) * 75f, 0f, 0f);
                davidJar.localRotation = Quaternion.Euler(0f, 0f, Mathf.Min(1f, p * 2.5f) * 110f);
                if (p > 0.25f && p < 0.9f && UnityEngine.Random.value < 0.7f) Fx.Burst(davidJar.position + Vector3.up * 0.1f, U.Hex(0x6aa6cf), 2, 0.5f, 0.45f, 0.7f);
            };
            cutscene.Play(new List<Shot>
            {
                new Shot("f4v16a", 6f, (c, p) => Look(c, new Vector3(6f - p * 2f, dp.y + 2.4f, -61f - p * 2f), new Vector3(0f, dp.y + 1.2f, -71.5f))),
                pour,
                new Shot("f4v17", 7f, (c, p) => Look(c, dp + new Vector3(-1.5f + p * 3f, 1.7f + p, 4f + p * 3f), dp + new Vector3(0f, 1.2f, 1.5f))),
                new Shot("f4v17b", 5f, (c, p) => Look(c, new Vector3(-4f + p * 2f, dp.y + 3f + p * 2f, -60f + p * 3f), new Vector3(0f, 3f, 80f))),
            }, () => { ClearCineMen(); ShowResults(); });
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase4Params par = Fase4Params.Current;
            bool[] ok = { mission.throughCamp, !mission.fieldWell, mission.together, mission.waterEnd >= 60f };
            string[] txt = { "Romperam pelo arraial dos filisteus", "Tiraram água da cisterna junto à porta", "Os três voltaram juntos",
                             "A água chegou a Davi (" + Mathf.RoundToInt(mission.waterEnd) + "%)" };
            string[] refs = { "2 Sm 23:16", "2 Sm 23:16", "2 Sm 23:17", "2 Sm 23:16" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int score = Mathf.RoundToInt(mission.waterEnd * 10f) + mission.repelled * 30 + arms.parries * 25
                        + Mathf.Max(0, Mathf.RoundToInt(300f - mission.alarm * 3f)) + Mathf.Max(0, Mathf.RoundToInt(600f - mission.time));
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + score + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            bool unlocked = Progress.SaveWin(4, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("Adulão · " + Difficulty.Current.name);
            c.Title("Derramou-a perante o Senhor");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Missão · água " + Mathf.RoundToInt(mission.waterEnd) + "% · " + mission.repelled + " afastados · alarme " + Mathf.RoundToInt(mission.alarm) + "% · " + Mathf.RoundToInt(mission.time) + " s",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), score.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], refs[i]);
            c.Space(16);
            c.Verse("f4v17");
            c.Note("Os três não têm nome no texto (\"três dos trinta cabeças\"). Relato paralelo: 1 Crônicas 11:15-19.");
            c.NextPhase(4, unlocked);
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só a missão", false, StartMissionOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }
    }
}
