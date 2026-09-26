using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 5: Benaia (2 Samuel 23:20-23). Abertura → treino no campo da guarda → o dia da neve e a
    /// cova do leão → o egípcio (cajado, arrancar a lança, vencer com ela) → Davi o põe sobre a sua
    /// guarda → resultado.
    /// </summary>
    public class Fase5Game : MonoBehaviour
    {
        public const string SceneName = "Fase5_Benaia";
        enum Mode { Menu, Cine, Training, Mission, Result }
        enum Kind { Full, TrainingOnly, MissionOnly }
        enum Part { None, Snow, Pit, LionDone, Egypt, Done }

        class Act { public string id, label, doing; public float need; }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        BenaiaArms arms;
        PitLion lion;
        Egyptian egy;
        GuardTraining training;
        Cutscene cutscene;
        UI ui;
        ParticleSystem snowFx;
        Figure hero;

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        Part part = Part.None;
        bool modal, snowing;
        string medal;
        int trainingScore, deaths;
        float savedTimeScale = 1f, actT, time, courageStart = 70f, prayProgress, prayCool, rockCool;
        Act act;
        // Fidelidade
        bool waited, tookSword, spearKill, warnedSnow, warnedRocks, warnedSword;
        int rocksThrown;

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
            world = World.ForBenaia(transform, skyMaterial);
            Fx.SetWorld(world);

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();
            snowFx = Snowland.MakeSnow(cam.transform);

            ui = new UI(transform, panelSettings);

            GameObject p = new GameObject("Benaia");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            arms = p.AddComponent<BenaiaArms>();
            arms.player = player; arms.ui = ui;
            arms.Build();
            arms.onPlayerDown = OnFall;

            lion = PitLion.Build(transform);
            lion.player = player; lion.arms = arms; lion.ui = ui;
            lion.fighting = () => mode == Mode.Mission && part == Part.Pit && !modal && !Game.Paused;
            lion.onDown = OnLionDown;

            egy = Egyptian.Build(transform);
            egy.player = player; egy.arms = arms; egy.ui = ui;
            egy.fighting = () => mode == Mode.Mission && part == Part.Egypt && !modal && !Game.Paused && !cutscene.Playing;
            egy.onDown = OnEgyptianDown;
            egy.gameObject.SetActive(false);

            training = gameObject.AddComponent<GuardTraining>();
            training.player = player; training.arms = arms; training.ui = ui;
            training.onFinished = OnTrainingFinished;

            hero = Figure.Man(transform, "Benaia", U.Hex(0x7a6a48), false);
            hero.Spear();
            hero.root.gameObject.SetActive(false);

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · botão direito · escudo e aparar · Espaço · desviar · E · ação · F · orar · M · música · N · narração");
            ShowMenu();
        }

        void SetWeather(bool snow)
        {
            Snowland.SetWeather(snow, world.sun, skyMaterial != null ? skyMaterial : RenderSettings.skybox);
            snowing = snow;
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            if (Snowland.shelterFlame != null) Snowland.shelterFlame.localScale = new Vector3(0.4f, 0.45f * (1f + 0.25f * Mathf.Sin(Time.time * 11f)), 0.4f);
            bool snowVisible = snowing && cam.transform.position.x < Snowland.PlainX / 2f;
            if (snowVisible && !snowFx.isPlaying) snowFx.Play(); else if (!snowVisible && snowFx.isPlaying) snowFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            bool playing = (mode == Mode.Training || mode == Mode.Mission) && !cutscene.Playing && !modal && part != Part.Done && part != Part.LionDone;
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
            player.clampPosition = ClampPlayer;

            if (control) { UpdateAction(Time.deltaTime); if (mode == Mode.Mission) { time += Time.deltaTime; UpdatePrayer(Time.deltaTime); UpdateSlip(); } }
            else { act = null; actT = 0f; }
            rockCool = Mathf.Max(0f, rockCool - Time.deltaTime);

            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Game.Paused && !ui.OverlayOpen);
            TouchControls.SwapIsDash = false;
            TouchControls.SetCombatButtons(true);
            TouchControls.ShowSwap(false);
            TouchControls.SetFireLabel("Golpe");
            TouchControls.SetShieldLabel(arms.weapon == BenaiaWeapon.Sword ? "Escudo" : "Aparar");
            TouchControls.ShowPhase4Buttons(true, act != null ? act.label : null);

            if (GameInput.NarrationPressed()) { Settings.Narration = !Settings.Narration; ui.Toast(Settings.Narration ? "Narração ligada" : "Narração desligada", 1.2f); }
            if (GameInput.MusicPressed()) { Settings.Music = !Settings.Music; ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f); }

            if (playing) UpdateHud();

            if (mode == Mode.Mission && part == Part.Snow && control && lion.Alive && Flat(player.Position - Snowland.Pit) < Snowland.PitRadius - 0.2f) EnterPit();

            if (mode == Mode.Menu && !Game.Paused)
            {
                float a = Time.time * 0.05f;
                cam.transform.position = Snowland.Pit + new Vector3(Mathf.Sin(a) * 18f, 9f, Mathf.Cos(a) * 18f);
                cam.transform.LookAt(Snowland.Pit + Vector3.down * 1.5f);
            }
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        Vector3 ClampPlayer(Vector3 p)
        {
            if (mode == Mode.Training) return Snowland.KeepIn(p, Snowland.Training, 13f);
            if (part == Part.Snow || part == Part.LionDone) return Snowland.KeepIn(p, Vector3.zero, 45f);
            if (part == Part.Pit) return Snowland.KeepIn(p, Snowland.Pit, Snowland.PitRadius - 0.4f);
            if (part == Part.Egypt || part == Part.Done) return Snowland.KeepIn(p, Snowland.Arena, 22f);
            return p;
        }

        /// <summary>Na neve o chão escorrega: é difícil parar e mudar de direção.</summary>
        void UpdateSlip()
        {
            bool slip = snowing && player.Position.x < Snowland.PlainX / 2f;
            Vector2 mv = GameInput.Move();
            if (slip && mv.sqrMagnitude < 0.01f && player.velocity.magnitude > 0.3f && !player.Dashing)
                player.Dash(player.velocity * Mathf.Exp(-2.2f * Time.deltaTime), Time.deltaTime * 1.01f);
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
                    ui.Toast("Benaia orou e encontrou coragem.", 1.8f);
                }
            }
            else { arms.praying = false; prayProgress = 0f; }
        }

        Act CurrentAction()
        {
            Vector3 pp = player.Position;
            if (mode == Mode.Training && training.Disarmable) return new Act { id = "tdisarm", need = Fase5Params.Current.disarmSeconds, label = "Arrancar a lança", doing = "arrancando..." };
            if (mode != Mode.Mission) return null;
            if (part == Part.Snow)
            {
                if (snowing && Flat(pp - Snowland.Shelter) < 2.8f) return new Act { id = "shelter", need = 2f, label = "Esperar a neve passar", doing = "esperando..." };
                if (lion.Alive && Flat(pp - Snowland.Rocks) < 2.2f && rockCool <= 0f) return new Act { id = "rocks", need = 0.5f, label = "Atirar uma pedra", doing = "..." };
            }
            if (part == Part.Egypt)
            {
                if (egy.Disarmable && Flat(pp - egy.transform.position) < 3.3f) return new Act { id = "disarm", need = Fase5Params.Current.disarmSeconds, label = "Arrancar a lança", doing = "arrancando..." };
                if (arms.weapon == BenaiaWeapon.Staff && egy.armed && Flat(pp - Snowland.Rack) < 2.2f) return new Act { id = "rack", need = 0.6f, label = "Pegar a espada", doing = "pegando..." };
            }
            return null;
        }

        void UpdateAction(float dt)
        {
            Act a = CurrentAction();
            if (a == null || act == null || a.id != act.id) actT = 0f;
            act = a;
            if (act == null || !GameInput.ActionHeld() || GameInput.Move().sqrMagnitude > 0.01f) { actT = 0f; return; }
            if (act.id == "shelter" && !warnedSnow) { warnedSnow = true; actT = 0f; Confirm("shelter"); return; }
            if (act.id == "rocks" && !warnedRocks) { warnedRocks = true; actT = 0f; Confirm("rocks"); return; }
            if (act.id == "rack" && !warnedSword) { warnedSword = true; actT = 0f; Confirm("rack"); return; }
            actT += dt;
            if (actT < act.need) return;
            actT = 0f;
            Do(act.id);
        }

        void Do(string id)
        {
            switch (id)
            {
                case "shelter": SetWeather(true); snowing = false; waited = true; Sfx.Play("wind", 0.5f); ui.Toast("A neve parou. O chão ficou firme.", 2f); break;
                case "rocks": ThrowRock(); break;
                case "rack": arms.weapon = BenaiaWeapon.RackSword; tookSword = true; Snowland.rackSword.gameObject.SetActive(false); ui.Toast("Benaia pegou a espada.", 1.4f); break;
                case "disarm": egy.Disarm(); arms.weapon = BenaiaWeapon.Spear; ui.SetObjective("O egípcio sem a lança", "Agora ele vem com os punhos, mais rápido e de perto. Mantenha distância e estoque com a lança dele."); break;
                case "tdisarm": training.Disarm(); break;
            }
        }

        /// <summary>Pedra atirada da borda: acerta o leão lá embaixo, sem risco (e contra o relato).</summary>
        void ThrowRock()
        {
            rocksThrown++; rockCool = 0.6f;
            Sfx.Play("throw");
            StartCoroutine(RockFlight(cam.transform.position, lion.transform.position + Vector3.up));
        }

        IEnumerator RockFlight(Vector3 a, Vector3 b)
        {
            GameObject r = U.Box(null, a, Vector3.one * 0.3f, U.Hex(0x8a8274));
            float T = 0.9f, t = 0f;
            Vector3 v = (b - a - 0.5f * Vector3.down * Stone.Gravity * T * T) / T;
            while (t < T)
            {
                t += Time.deltaTime;
                r.transform.position = a + v * t + 0.5f * Vector3.down * Stone.Gravity * t * t;
                yield return null;
            }
            Destroy(r);
            if (lion.Alive) { lion.Damage(1f); Sfx.Play("growl"); }
        }

        void Confirm(string kind)
        {
            string eb, h, lede, verse, yes, no, item;
            if (kind == "shelter") { eb = "O abrigo"; h = "Esperar a neve passar?"; lede = "Aqui dentro está quente, e sem neve o chão fica firme. Mas o texto guarda um detalhe:"; verse = "f5vsnow"; yes = "Esperar a neve passar"; no = "Ir agora, na neve"; item = "No tempo da neve"; }
            else if (kind == "rocks") { eb = "A borda da cova"; h = "Atirar pedras de cima?"; lede = "Daqui o leão não alcança você. Mas o texto conta outra coisa:"; verse = "f5vpit"; yes = "Atirar pedras"; no = "Descer à cova"; item = "Desceu à cova"; }
            else { eb = "O suporte de armas"; h = "Pegar a espada?"; lede = "Com a espada a luta fica mais fácil. Mas o texto diz:"; verse = "f5vstaff"; yes = "Pegar a espada"; no = "Ficar com o cajado"; item = "Desceu a ele com um cajado"; }
            modal = true;
            arms.Cancel();
            Card c = ui.OpenCard();
            c.Eyebrow(eb);
            c.Title(h);
            c.Lede(lede);
            c.Verse(verse);
            VisualElement row = c.Row();
            Card.Btn(row, yes, false, () => { ui.CloseOverlay(); modal = false; Do(kind); });
            Card.Btn(row, no, true, () => { ui.CloseOverlay(); modal = false; });
            c.Note("Escolher \"" + yes + "\" faz perder o item de Fidelidade \"" + item + "\".");
        }

        void UpdateHud()
        {
            bool m = mode == Mode.Mission;
            string wave = !m ? "" : part == Part.Snow ? (snowing ? "O dia da neve" : "A neve passou") : part == Part.Pit ? "O leão" : part == Part.Egypt ? (egy.armed ? "O egípcio" : "O egípcio sem a lança") : "";
            float boss = !m ? -1f : part == Part.Pit ? lion.hp / Mathf.Max(1f, lion.max) : part == Part.Egypt ? egy.hp / Mathf.Max(1f, egy.max) : -1f;
            ui.SetFase5Stats(m, training.score, player.health, player.courage, wave, boss);
            if (prayProgress > 0f) { ui.DrawRing(prayProgress / 2f, UI.BronzeHi, "orando..."); return; }
            if (act != null && actT > 0f) { ui.DrawRing(actT / act.need, U.Hex(0x6aa6cf), act.doing); return; }
            if (act != null) { ui.DrawRing(0f, UI.Olive, (TouchControls.Active ? "Ação: " : "E: ") + act.label); return; }
            string w = arms.weapon == BenaiaWeapon.Staff ? "cajado" : arms.weapon == BenaiaWeapon.Spear ? "lança" : "espada";
            string t = arms.Charging ? (arms.ChargeFraction >= 1f ? "golpe forte!" : "segure: forte") : arms.Crit ? "crítico!" : arms.ShieldUp ? "escudo erguido" : w;
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
            part = Part.None;
            mode = Mode.Menu;
            hero.root.gameObject.SetActive(false);
            SetWeather(true);
            lion.Place(); egy.gameObject.SetActive(false);
            arms.weapon = BenaiaWeapon.Sword;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 5 · Benaia");
            c.Title("Benaia: o leão na cova e o egípcio", 50);
            c.Lede("2 Samuel 23:20-23. Num dia de neve, Benaia desce à cova onde está um leão. Depois desce ao egípcio gigante levando só um cajado, arranca-lhe a lança e vence com ela.");
            c.PhaseMap(5);
            if (!Progress.IsOpen(5))
            {
                c.Locked(5);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase5Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a missão", false, StartMissionOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Botão direito", "Espaço", "E (segurar)", "F (segurar) · Shift", "Esc · M · N" },
                new[] { "Golpe (espada, cajado ou lança) · segure e solte para o golpe forte", "Segurar: escudo (com a espada) · no instante do golpe: aparar",
                        "Desviar: o bote do leão e a varredura do egípcio", "Ação: arrancar a lança do egípcio desequilibrado · outras escolhas do lugar",
                        "Orar · correr (na neve o chão escorrega)", "Pausa · música · narração" });
            c.Note("No celular: direcional, arrastar para olhar e os botões Golpe, Escudo/Aparar, Desviar, Orar e Ação. Modelos, sons e versículos provisórios.");
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
                if (mode == Mode.Training) StartTraining(); else if (part == Part.Egypt) StartEgypt(); else StartLion(false);
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
            courageStart = 65 + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
            ui.CloseOverlay();
            SnowCine(() => StartLion(true));
        }

        void StartTraining()
        {
            Music.Set("training");
            part = Part.None;
            SetWeather(false);
            lion.gameObject.SetActive(false); egy.gameObject.SetActive(false);
            mode = Mode.Training;
            player.Place(Snowland.Training + new Vector3(0f, 0f, -3f), 0f, 0f);
            player.health = 100f; player.courage = 70f;
            training.Begin();
        }

        void OnTrainingFinished(GuardTraining t)
        {
            mode = Mode.Result;
            medal = t.medal; trainingScore = t.score;
            courageStart = t.StartingCourage();
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("O campo da guarda");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Botes evitados · contra-ataques", "Lanças arrancadas", "Coragem inicial", "Pontos" },
                    new[] { t.escaped + " · " + t.counters, t.disarms.ToString(), courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f5v20a");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a missão", true, () => { ui.CloseOverlay(); SnowCine(() => StartLion(true)); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartLion(bool fresh)
        {
            Music.Set("quiet");
            training.Clear();
            modal = false; mode = Mode.Mission;
            if (fresh)
            {
                waited = tookSword = spearKill = warnedSnow = warnedRocks = warnedSword = false;
                rocksThrown = 0; deaths = 0; time = 0f;
                arms.ResetCounters();
            }
            SetWeather(true);
            snowing = !waited;
            arms.weapon = BenaiaWeapon.Sword;
            arms.onSwing = k => { if (part == Part.Pit && lion.Alive && arms.InReach(lion.transform.position)) lion.Hit(k); };
            player.health = 100f;
            player.courage = fresh ? courageStart : Mathf.Max(player.courage, courageStart * 0.8f);
            lion.Place(); egy.gameObject.SetActive(false);
            player.Place(new Vector3(0f, 0f, -24f), 0f, 0f);
            part = Part.Snow;
            ui.SetObjective("O dia da neve", "Um leão caiu na cova junto ao caminho. Da borda há pedras; do lado, um abrigo com fogo. Benaia desceu.");
            ui.Toast("\"Um leão caiu na cova! Ninguém desce aí.\"", 2.6f);
        }

        void EnterPit()
        {
            part = Part.Pit;
            lion.StartFight();
            Music.Set("duel");
            ui.Toast("A neve desliza atrás de você. Não há volta.", 2f);
            ui.SetObjective("O leão na cova", "Quando ele agachar e rosnar, vem o bote: desvie para o lado (Espaço). Depois do bote ele fica atordoado e os golpes valem o dobro. Escudo contra a patada.");
        }

        void OnLionDown()
        {
            ui.Toast("O leão caiu.", 2f);
            ui.SetObjective("O leão caiu", rocksThrown > 0 ? "O leão caiu, mas não da maneira que o texto conta." : "\"...e feriu um leão no meio de uma cova, no tempo da neve.\"");
            if (mode == Mode.Mission) { part = Part.LionDone; StartCoroutine(After(2.6f, EgyptCine)); }
        }

        IEnumerator After(float s, Action a) { yield return new WaitForSeconds(s); if (mode == Mode.Mission || mode == Mode.Cine) a(); }

        void StartEgypt()
        {
            Music.Set("duel");
            modal = false; mode = Mode.Mission;
            SetWeather(false);
            lion.gameObject.SetActive(false);
            arms.weapon = BenaiaWeapon.Staff;
            arms.onSwing = k => { if (part == Part.Egypt && egy.Alive && arms.InReach(egy.transform.position + Vector3.up)) egy.Hit(k, arms.weapon); };
            Snowland.rackSword.gameObject.SetActive(true);
            player.health = 100f; player.courage = Mathf.Max(player.courage, 60f);
            egy.Place();
            player.Place(Snowland.Arena + new Vector3(0f, 0f, -12f), 0f, 0f);
            part = Part.Egypt;
            ui.SetObjective("O egípcio", "Ele alcança longe com a lança. Apare a estocada com o cajado (botão direito) e, com ele desequilibrado, segure E para arrancar a lança. Da varredura, desvie.");
        }

        void OnEgyptianDown(bool withSpear)
        {
            spearKill = withSpear;
            ui.Toast("O egípcio caiu.", 2f);
            ui.SetObjective("O egípcio caiu", withSpear ? "\"...e o matou com a sua própria lança.\"" : "O egípcio caiu, mas não com a lança dele.");
            part = Part.Done;
            StartCoroutine(After(2.4f, EndCine));
        }

        void OnFall(string reason)
        {
            if (modal || mode != Mode.Mission) return;
            modal = true; deaths++;
            Music.Set("quiet");
            StartCoroutine(ShowFall(reason));
        }

        IEnumerator ShowFall(string reason)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            bool toLion = Fase5Params.Current.restartAtLion || part == Part.Pit || part == Part.Snow;
            Card c = ui.OpenCard();
            c.Eyebrow("Missão · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede(toLion ? "Você recomeça no caminho da cova do leão." : "Você recomeça a luta com o egípcio.");
            c.Verse(toLion ? "f5v20b" : "f5v21");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () => { ui.CloseOverlay(); if (toLion) StartLion(false); else StartEgypt(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cenas

        void HeroWalk(float dt, Vector3 target)
        {
            Vector3 p = hero.root.position, d = target - p; d.y = 0f;
            if (d.magnitude > 0.3f) { p += d.normalized * 2.2f * dt; hero.root.rotation = Quaternion.LookRotation(d); hero.Walk(2.2f); } else hero.Stand();
            p.y = Snowland.Height(p.x, p.z);
            hero.root.position = p;
        }

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine; part = Part.None;
            SetWeather(false);
            lion.gameObject.SetActive(false); egy.gameObject.SetActive(false);
            hero.root.gameObject.SetActive(true);
            hero.root.position = new Vector3(Snowland.PlainX + 24f, Snowland.Height(Snowland.PlainX + 24f, 0f), 0f);
            float X = Snowland.PlainX;
            cutscene.Play(new List<Shot>
            {
                new Shot("f5v20a", 8f, (c, p) => Look(c, new Vector3(X + 20f + p * 6f, 4f + p, -6f + p * 4f), new Vector3(X + 32f, 1f, 14f))) { update = p => HeroWalk(Time.deltaTime, new Vector3(X + 36f, 0f, 22f)) },
            }, () => { hero.root.gameObject.SetActive(false); then(); });
        }

        void SnowCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine; part = Part.None;
            SetWeather(true);
            lion.Place(); egy.gameObject.SetActive(false);
            Vector3 pit = Snowland.Pit;
            cutscene.Play(new List<Shot>
            {
                new Shot("f5v20b", 7f, (c, p) => Look(c, new Vector3(Mathf.Sin(p * 1.2f) * 16f, 9f - p * 3f, pit.z - 16f + p * 4f), pit + Vector3.down * 1.5f)),
            }, then);
        }

        void EgyptCine()
        {
            Music.Set("cine");
            mode = Mode.Cine;
            SetWeather(false);
            lion.gameObject.SetActive(false);
            egy.Place();
            Vector3 a = Snowland.Arena;
            cutscene.Play(new List<Shot>
            {
                new Shot("f5v21", 9f, (c, p) => Look(c, a + new Vector3(6f - p * 9f, 2.2f + p * 1.5f, -4f + p * 2f), a + new Vector3(0f, 2.4f, 6f))),
            }, StartEgypt);
        }

        void EndCine()
        {
            Music.Set("victory");
            mode = Mode.Cine;
            arms.Cancel();
            egy.gameObject.SetActive(false);
            Vector3 camp = Snowland.Camp;
            hero.root.gameObject.SetActive(true);
            hero.root.position = new Vector3(camp.x, Snowland.Height(camp.x, camp.z + 22f), camp.z + 22f);
            Figure d = Snowland.david;
            d.root.rotation = Quaternion.identity;
            Vector3 spot = new Vector3(camp.x, 0f, camp.z + 8f);
            Shot s2 = new Shot("f5v23", 8f, (c, p) => Look(c, camp + new Vector3(-4f + p * 2f, 2f + p, 13f + p * 2f), camp + new Vector3(0f, 1.5f, 7f)));
            s2.update = p =>
            {
                HeroWalk(Time.deltaTime, spot);
                d.armR.localRotation = Quaternion.Euler(-Mathf.Min(1f, p * 3f) * 110f, 0f, 0f);
                foreach (Figure g in Snowland.guards)
                {
                    float want = g.root.position.x < camp.x ? 125f : -125f;
                    g.root.rotation = Quaternion.Slerp(g.root.rotation, Quaternion.Euler(0f, want, 0f), Time.deltaTime * 2f);
                }
            };
            cutscene.Play(new List<Shot>
            {
                new Shot("f5v22", 7f, (c, p) => Look(c, camp + new Vector3(7f - p * 3f, 2.6f, 16f - p * 3f), camp + new Vector3(0f, 1.4f, 10f))) { update = p => HeroWalk(Time.deltaTime, spot) },
                s2,
            }, () => { hero.root.gameObject.SetActive(false); d.armR.localRotation = Quaternion.identity; ShowResults(); });
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase5Params par = Fase5Params.Current;
            bool[] ok = { !waited, rocksThrown == 0, !tookSword, spearKill };
            string[] txt = { "No tempo da neve (não esperou a neve passar)", "Desceu à cova (não atirou pedras de cima)", "Desceu ao egípcio com um cajado", "Matou-o com a sua própria lança" };
            string[] refs = { "2 Sm 23:20", "2 Sm 23:20", "2 Sm 23:21", "2 Sm 23:21" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int bosses = Mathf.Max(0, 1000 + arms.parries * 30 + arms.dodges * 20 + Mathf.Max(0, Mathf.RoundToInt(800f - time * 2f)) - deaths * 100);
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + bosses + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            bool unlocked = Progress.SaveWin(5, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("Benaia · " + Difficulty.Current.name);
            c.Title("Davi o pôs sobre a sua guarda");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Leão e egípcio · " + arms.parries + " aparadas · " + arms.dodges + " desvios · " + Mathf.RoundToInt(time) + " s",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), bosses.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], refs[i]);
            c.Space(16);
            c.Verse("f5v22");
            c.Note("Relato paralelo: 1 Crônicas 11:22-25 (o egípcio tinha cinco côvados de altura).");
            c.NextPhase(5, unlocked);
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só a missão", false, StartMissionOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }
    }
}
