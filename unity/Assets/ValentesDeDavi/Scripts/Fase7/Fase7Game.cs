using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 7 (final): Josebe-Bassebete (2 Samuel 23:8; 1 Crônicas 11:11). Abertura → treino → os oitocentos
    /// no desfiladeiro → a lista dos valentes → resultado e galeria de todas as fases.
    /// </summary>
    public class Fase7Game : MonoBehaviour
    {
        public const string SceneName = "Fase7_Josebe";
        enum Mode { Menu, Cine, Training, Mission, Result }
        enum Kind { Full, TrainingOnly, MissionOnly }
        enum Part { None, Eight, Done }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        BenaiaArms arms;
        CaptainGuard guard;
        GorgeTraining training;
        Cutscene cutscene;
        UI ui;
        Figure captain;
        readonly List<Figure> heroes = new List<Figure>(), israel = new List<Figure>();
        readonly List<Vector3> israelHome = new List<Vector3>();
        Transform throne;
        const float ThroneZ = 33f;
        /// <summary>Davi no centro; os valentes dos dois lados (Josebe-Bassebete à direita do rei).</summary>
        static readonly float[] HeroX = { 0f, -2.1f, 3.4f, -3.4f, 4.7f, -4.7f, 2.1f };

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        Part part = Part.None;
        bool modal;
        string medal;
        int trainingScore, deaths, felled, bonusAt, rank;
        float savedTimeScale = 1f, time, timeLeft, spawnT, archerT, courageStart = 70f, prayProgress, prayCool;
        // Fidelidade
        bool eight, usedSword, fell, retreated, warnedSword;

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
            world = World.ForJosebe(transform, skyMaterial);
            Fx.SetWorld(world);
            Arrow.ground = Gorge.Height;

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);

            GameObject p = new GameObject("Josebe-Bassebete");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            arms = p.AddComponent<BenaiaArms>();
            arms.player = player; arms.ui = ui;
            arms.sweepSpear = true;
            arms.Build();
            arms.weapon = BenaiaWeapon.Spear;
            arms.onPlayerDown = r => OnFall("O capitão caiu.");
            guard = new CaptainGuard { arms = arms };

            BuildIsrael();
            BuildHeroes();
            BuildThrone();
            captain = Figure.Man(transform, "O capitão", U.Hex(0x5e5638), false);
            captain.Spear();
            captain.root.gameObject.SetActive(false);

            training = gameObject.AddComponent<GorgeTraining>();
            training.player = player; training.arms = arms; training.ui = ui;
            training.onFinished = OnTrainingFinished;
            guard.onArrow = training.OnArrow;

            GorgeFoe.Ctx = new GorgeFoe.Context
            {
                player = player, arms = arms, ui = ui,
                fighting = () => (mode == Mode.Mission && part == Part.Eight || mode == Mode.Training) && !modal && !Game.Paused,
                onFelled = OnFelled,
            };
            CliffArcher.Guard = guard;
            CliffArcher.Playing = () => (mode == Mode.Mission && part == Part.Eight || mode == Mode.Training) && !modal && !Game.Paused && !cutscene.Playing;

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · botão direito · aparar · Espaço · desviar · Q · espada/lança · F · orar · M · música · N · narração");
            ShowMenu();
        }

        void BuildIsrael()
        {
            Transform army = U.Pivot(transform, "Exército de Israel", Vector3.zero);
            int[] robes = { 0x6f6a3c, 0x7d5f3a, 0x8a7a52, 0x5e5638 };
            System.Random r = new System.Random(8);
            for (int i = 0; i < 24; i++)
            {
                Figure m = Figure.Man(army, "Soldado", U.Hex(robes[i % 4]), false);
                m.Spear();
                float x = -5f + (i % 8) * 1.4f + ((float)r.NextDouble() - 0.5f) * 0.6f, z = -24f - (i / 8) * 1.8f;
                m.root.position = new Vector3(x, Gorge.Height(x, z), z);
                israel.Add(m); israelHome.Add(m.root.position);
            }
        }

        void IsraelHome()
        {
            for (int i = 0; i < israel.Count; i++) { israel[i].root.position = israelHome[i]; israel[i].root.rotation = Quaternion.identity; }
        }

        /// <summary>O trono do rei no fim do desfiladeiro (só aparece na cena final).</summary>
        void BuildThrone()
        {
            throne = U.Pivot(transform, "Trono de Davi", new Vector3(0f, Gorge.Height(0f, ThroneZ), ThroneZ));
            Color wood = U.Hex(0x6b4a2a), gold = U.Hex(0xd9a93a);
            U.Box(throne, new Vector3(0f, 0.25f, 0f), new Vector3(3.4f, 0.5f, 2.4f), U.Hex(0x9a8a70));
            U.Box(throne, new Vector3(0f, 0.75f, -0.1f), new Vector3(1f, 0.5f, 0.8f), wood);
            U.Box(throne, new Vector3(0f, 1.3f, 0.45f), new Vector3(1f, 1.6f, 0.15f), wood);
            for (int s = -1; s <= 1; s += 2) U.Sph(throne, new Vector3(s * 0.45f, 2.1f, 0.45f), 0.09f, gold, 0.8f, 0.65f);
            U.Box(throne, new Vector3(0f, 0.01f, -3.6f), new Vector3(1.2f, 0.02f, 5f), U.Hex(0x7a2a2a));
            throne.gameObject.SetActive(false);
        }

        /// <summary>A fila dos valentes das sete fases, para a cena final.</summary>
        void BuildHeroes()
        {
            string[] names = { "Davi, o rei", "Samá", "Eleazar", "Um dos três", "Benaia", "Abisai", "Josebe-Bassebete" };
            int[] robes = { 0x5a2f66, 0x6f6a3c, 0x7d5f3a, 0x6f6a3c, 0x7a6a48, 0x8a7a52, 0x5e5638 };
            for (int i = 0; i < names.Length; i++)
            {
                Figure m = Figure.Man(transform, names[i], U.Hex(robes[i]), false);
                switch (i)
                {
                    case 0:                                                                                         // a coroa e o cetro
                    {
                        Color gold = U.Hex(0xd9a93a);
                        U.Cyl(m.root, new Vector3(0f, 1.78f, 0f), 0.15f, 0.1f, gold, 0.8f, 0.65f);
                        for (int t = 0; t < 6; t++)
                        {
                            float a = t / 6f * Mathf.PI * 2f;
                            U.Box(m.root, new Vector3(Mathf.Sin(a) * 0.14f, 1.86f, Mathf.Cos(a) * 0.14f), new Vector3(0.04f, 0.08f, 0.02f), gold, 0.8f, 0.65f)
                                .transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
                        }
                        U.Cyl(m.armR, new Vector3(0f, -0.55f, 0.2f), 0.02f, 0.9f, gold, 0.8f, 0.65f).transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                        break;
                    }
                    case 1: m.Sword(); m.RoundShield(); break;
                    case 2: m.Sword(); break;
                    case 3: Figure.Jar(m.armL).localPosition = new Vector3(0f, -0.7f, 0.15f); break;            // o cântaro
                    default: m.Spear(); break;                                                                    // lanças
                }
                m.root.gameObject.SetActive(false);
                heroes.Add(m);
            }
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            arms.parryWindowOverride = Fase7Params.Current.parryWindow;
            bool playing = (mode == Mode.Training || mode == Mode.Mission) && !cutscene.Playing && !modal && part != Part.Done;
            if (!Game.Paused && (playing || cutscene.Playing) && GameInput.PausePressed()) ShowPause();

            if (TouchControls.Active && !Application.isMobilePlatform && GameInput.MouseUsed()) TouchControls.Active = false;
            bool control = playing && !Game.Paused && !ui.OverlayOpen;
            bool lockCursor = control && !TouchControls.Active;
            UnityEngine.Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !lockCursor;
            player.controlling = control;
            arms.active = control;
            arms.SetVisible(control);
            player.speedMultiplier = arms.praying ? 0f : 1f;
            player.eyeHeight = Mathf.Lerp(player.eyeHeight, arms.praying ? 1.0f : PlayerController.EyeHeight, Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            player.swayScale = 0.4f;
            player.clampPosition = ClampPlayer;

            if (control)
            {
                if (GameInput.SwapPressed()) SwapWeapon();
                if (mode == Mode.Mission)
                {
                    time += Time.deltaTime;
                    UpdatePrayer(Time.deltaTime);
                    if (part == Part.Eight) UpdateEight(Time.deltaTime);
                }
            }

            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Game.Paused && !ui.OverlayOpen);
            TouchControls.SwapIsDash = false;
            TouchControls.SetCombatButtons(true);
            TouchControls.ShowSwap(true);
            TouchControls.SetFireLabel("Golpe");
            TouchControls.SetShieldLabel("Aparar");
            TouchControls.SetSwapLabel(arms.weapon == BenaiaWeapon.Spear ? "Espada" : "Lança", 15f);
            TouchControls.ShowPhase4Buttons(true, null);
            TouchControls.ShowActButton(false);

            if (GameInput.NarrationPressed()) { Settings.Narration = !Settings.Narration; ui.Toast(Settings.Narration ? "Narração ligada" : "Narração desligada", 1.2f); }
            if (GameInput.MusicPressed()) { Settings.Music = !Settings.Music; ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f); }

            if (playing) UpdateHud();

            if (mode == Mode.Menu && !Game.Paused)
            {
                float a = Time.time * 0.08f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 3f, 5f, -14f + Mathf.Cos(a) * 2f);
                cam.transform.LookAt(new Vector3(0f, 2f, 30f));
            }
        }

        Vector3 ClampPlayer(Vector3 p)
        {
            if (mode == Mode.Training)
            {
                Vector3 d = p - Gorge.Training; d.y = 0f;
                if (d.magnitude > 14f) { Vector3 q = Gorge.Training + d.normalized * 14f; q.y = p.y; return q; }
                return p;
            }
            return Gorge.KeepInGorge(p);
        }

        void SwapWeapon()
        {
            bool toSword = arms.weapon == BenaiaWeapon.Spear;
            if (toSword && mode == Mode.Mission && part == Part.Eight && !warnedSword)
            {
                warnedSword = true; modal = true; arms.Cancel();
                Card c = ui.OpenCard();
                c.Eyebrow("A espada na cintura");
                c.Title("Trocar a lança pela espada?");
                c.Lede("De perto, a espada é mais rápida. Mas o relato de Crônicas diz:");
                c.Verse("f7v1cr");
                VisualElement row = c.Row();
                Card.Btn(row, "Usar a espada", false, () => { ui.CloseOverlay(); modal = false; arms.weapon = BenaiaWeapon.RackSword; usedSword = true; ui.Toast("Espada", 0.8f); });
                Card.Btn(row, "Ficar com a lança", true, () => { ui.CloseOverlay(); modal = false; });
                c.Note("Usar a espada contra os oitocentos faz perder o item de Fidelidade \"Brandindo a sua lança\".");
                return;
            }
            arms.weapon = toSword ? BenaiaWeapon.RackSword : BenaiaWeapon.Spear;
            if (toSword && mode == Mode.Mission && part == Part.Eight) usedSword = true;
            ui.Toast(toSword ? "Espada" : "Lança", 0.8f);
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
                    ui.Toast("O capitão orou e encontrou coragem.", 1.8f);
                }
            }
            else { arms.praying = false; prayProgress = 0f; }
        }

        void OnSwing(SwingKind k)
        {
            List<GorgeFoe> hits = SpearHits.Pick(arms, GorgeFoe.All.FindAll(f => f.Alive), f => f.transform.position);
            foreach (GorgeFoe f in hits) f.Hit(k);
            if (hits.Count >= 3 && arms.Sweeping) ui.Toast("Varredura! " + hits.Count + " de uma vez", 0.8f);
        }

        void OnFelled(GorgeFoe f)
        {
            if (mode == Mode.Training) { training.OnFelled(f); return; }
            if (part != Part.Eight) return;
            felled++;
            // Sem vida voltando sozinha: só um pouco a cada cem feridos.
            if (felled >= bonusAt)
            {
                bonusAt += 100;
                player.health = Mathf.Min(100f, player.health + 20f);
                if (felled < 800) ui.Toast(felled + " de 800! A força volta um pouco.", 1.4f);
            }
        }

        void UpdateHud()
        {
            bool m = mode == Mode.Mission;
            string wave = m && part == Part.Eight ? "Feridos: " + felled + " de 800 · " + Mathf.FloorToInt(timeLeft / 60f) + ":" + Mathf.FloorToInt(timeLeft % 60f).ToString("00") : "";
            ui.SetFase6Stats(m, training.score, player.health, player.courage, wave, -1f, -1f);
            if (prayProgress > 0f) { ui.DrawRing(prayProgress / 2f, UI.BronzeHi, "orando..."); return; }
            string t = arms.Charging ? (arms.ChargeFraction >= 1f ? (arms.weapon == BenaiaWeapon.Spear ? "varredura!" : "golpe forte!") : "segure: forte")
                : arms.Crit ? "crítico!" : arms.weapon == BenaiaWeapon.Spear ? "lança" : "espada";
            ui.DrawRing(arms.Charging ? arms.ChargeFraction : 0f, arms.ChargeFraction >= 1f ? UI.BronzeHi : UI.Olive, t);
        }

        static void Look(Camera c, Vector3 pos, Vector3 target) { c.transform.position = pos; c.transform.LookAt(target); }

        void ClearFoes()
        {
            foreach (GorgeFoe f in GorgeFoe.All.ToArray()) Destroy(f.gameObject);
            CliffArcher.ClearAll();
        }

        void HideFigures()
        {
            captain.root.gameObject.SetActive(false);
            foreach (Figure h in heroes) h.root.gameObject.SetActive(false);
            throne.gameObject.SetActive(false);
            IsraelHome();
        }

        // ------------------------------------------------------------------ telas

        void ShowMenu()
        {
            Game.Paused = false; modal = false;
            Time.timeScale = 1f;
            cutscene.Abort();
            training.Clear();
            ClearFoes();
            HideFigures();
            part = Part.None; mode = Mode.Menu;
            arms.weapon = BenaiaWeapon.Spear;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 7 · Fase final");
            c.Title("Josebe-Bassebete: oitocentos de uma vez", 50);
            c.Lede("2 Samuel 23:8. O principal dos capitães de Davi se opõe sozinho a oitocentos filisteus num desfiladeiro e os fere de uma só vez. No fim, a lista dos valentes.");
            c.PhaseMap(7);
            if (!Progress.IsOpen(7))
            {
                c.Locked(7);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase7Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a batalha", false, StartMissionOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Botão direito", "Espaço", "Q", "F (segurar) · Shift", "Esc · M · N" },
                new[] { "Estocada da lança · segure e solte: varredura (derruba a fileira e os escudeiros)",
                        "Aparar no instante do golpe (também as flechas)", "Desviar",
                        "Trocar entre a lança e a espada da cintura", "Orar · correr", "Pausa · música · narração" });
            c.Note("No celular: direcional, arrastar para olhar e os botões Golpe, Aparar, Desviar, Espada/Lança e Orar. Modelos, sons e versículos provisórios.");
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
            c.Eyebrow("Pausa · " + (inCine ? "Cena animada" : mode == Mode.Training ? "Treino" : "Batalha") + " · " + Difficulty.Current.name);
            c.Title("Jogo pausado");
            VisualElement row = c.Row();
            Card.Btn(row, "Continuar", true, Resume);
            if (inCine) Card.Btn(row, "Pular a cena", false, () => { Resume(); cutscene.End(); });
            else Card.Btn(row, mode == Mode.Training ? "Recomeçar o treino" : "Recomeçar a batalha", false, () =>
            {
                Resume();
                if (mode == Mode.Training) StartTraining(); else StartEight(true);
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
            StartEight(true);
        }

        void StartTraining()
        {
            Music.Set("training");
            part = Part.None;
            ClearFoes();
            HideFigures();
            mode = Mode.Training;
            player.Place(Gorge.Training + new Vector3(0f, 0f, -2f), 0f, 0f);
            player.health = 100f; player.courage = 70f;
            GorgeFoe.ResetHint();
            training.Begin();
        }

        void OnTrainingFinished(GorgeTraining t)
        {
            mode = Mode.Result;
            medal = t.medal; trainingScore = t.score;
            courageStart = t.StartingCourage();
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("A prova do capitão");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Escudeiros derrubados", "Flechas aparadas", "Flechas desviadas", "Coragem inicial", "Pontos" },
                    new[] { t.shields + " de 6", t.parried.ToString(), t.dodged.ToString(), courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f7v8a");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a batalha", true, () => { ui.CloseOverlay(); StartEight(true); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartEight(bool fresh)
        {
            Music.Set("duel");
            training.Clear();
            ClearFoes();
            HideFigures();
            modal = false; mode = Mode.Mission;
            if (fresh)
            {
                eight = usedSword = fell = retreated = warnedSword = false;
                deaths = 0; time = 0f; felled = 0; bonusAt = 100; rank = 0;
                timeLeft = Fase7Params.Current.timeLimit;
                arms.ResetCounters();
                GorgeFoe.ResetHint();
                player.Place(new Vector3(0f, 0f, Gorge.StoneZ + 2f), 0f, 0f);
                player.courage = courageStart;
            }
            if (!usedSword) arms.weapon = BenaiaWeapon.Spear;
            arms.onSwing = OnSwing;
            player.health = 100f;
            spawnT = 1f; archerT = 12f;
            part = Part.Eight;
            ui.SetObjective("Os oitocentos", "As fileiras vêm pelo desfiladeiro. Varredura contra a fileira, golpe forte contra escudos; apare as flechas das encostas. Não recue para trás da pedra.");
            ui.Toast("\"...que se opôs a oitocentos...\"", 2.4f);
        }

        void UpdateEight(float dt)
        {
            timeLeft -= dt; spawnT -= dt; archerT -= dt;
            Fase7Params par = Fase7Params.Current;
            int alive = 0;
            foreach (GorgeFoe f in GorgeFoe.All) if (f.Alive) alive++;
            if (spawnT <= 0f && alive < par.maxFoes)
            {
                int n = Mathf.Min(par.maxFoes - alive, UnityEngine.Random.Range(4, 7));
                float z = player.Position.z + 22f;
                for (int i = 0; i < n; i++)
                {
                    rank++;
                    float x = -4.5f + i * 9f / Mathf.Max(1, n - 1) + UnityEngine.Random.Range(-0.3f, 0.3f);
                    GorgeFoe f = GorgeFoe.Spawn(transform, new Vector3(x, 0f, z + UnityEngine.Random.Range(-0.5f, 0.5f)), rank % par.shieldEvery == 0);
                    f.HoldFor(UnityEngine.Random.Range(0f, 0.4f));
                }
                spawnT = UnityEngine.Random.Range(2.4f, 3.2f);
            }
            if (archerT <= 0f)
            {
                float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                CliffArcher.Spawn(transform, new Vector3(side * 9.5f, 0f, player.Position.z + UnityEngine.Random.Range(10f, 18f)), false);
                archerT = UnityEngine.Random.Range(14f, 20f);
            }
            // "Se opôs": recuar para trás da pedra tira Coragem.
            if (player.Position.z < Gorge.StoneZ - 2f)
            {
                player.courage = Mathf.Clamp(player.courage - 5f * dt, 0f, 100f);
                if (!retreated) { retreated = true; ui.Toast("Você recuou para trás da pedra.", 2f); }
            }
            if (felled >= 800) { eight = true; EndEight("Oitocentos! \"...e os feriu de uma vez.\""); }
            else if (timeLeft <= 0f) EndEight("O exército de Israel avança. A batalha termina antes dos oitocentos.");
        }

        void EndEight(string msg)
        {
            part = Part.Done;
            ui.Toast(msg, 3f);
            ui.SetObjective("Os oitocentos", msg);
            foreach (GorgeFoe f in GorgeFoe.All) f.Flee();
            CliffArcher.ClearAll();
            StartCoroutine(After(2.6f, RollCall));
        }

        IEnumerator After(float s, Action a) { yield return new WaitForSeconds(s); if (mode == Mode.Mission) a(); }

        void OnFall(string reason)
        {
            if (modal || mode != Mode.Mission) return;
            modal = true; deaths++; fell = true;
            Music.Set("quiet");
            StartCoroutine(ShowFall(reason));
        }

        IEnumerator ShowFall(string reason)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            Card c = ui.OpenCard();
            c.Eyebrow("Batalha · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede("Levante-se: a batalha continua de onde parou (" + felled + " de 800). O item \"De uma vez\" ficou perdido.");
            c.Verse("f7v8b");
            VisualElement row = c.Row();
            Card.Btn(row, "Levantar e continuar", true, () => { ui.CloseOverlay(); ClearFoes(); StartEight(false); });
            Card.Btn(row, "Recomeçar do zero", false, () => { ui.CloseOverlay(); StartEight(true); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cenas

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine; part = Part.None;
            ClearFoes();
            captain.root.gameObject.SetActive(true);
            captain.root.position = new Vector3(0f, Gorge.Height(0f, -20f), -20f);
            captain.root.rotation = Quaternion.identity;
            Action<float> walk = p =>
            {
                Vector3 q = captain.root.position;
                if (q.z < Gorge.StoneZ) { q.z += 2f * Time.deltaTime; q.y = Gorge.Height(q.x, q.z); captain.root.position = q; captain.Walk(2f); }
                else captain.Stand();
            };
            Shot s1 = new Shot("f7v8a", 7f, (c, p) => Look(c, new Vector3(4f - p * 2f, 3f + p, -30f + p * 6f), new Vector3(0f, 1.5f, captain.root.position.z + 4f)));
            s1.update = walk;
            Shot s2 = new Shot("f7v8b", 6f, (c, p) => Look(c, new Vector3(-3f, 1.8f, Gorge.StoneZ - 4f + p), new Vector3(0f, 3f, 60f)));
            s2.update = walk;
            cutscene.Play(new List<Shot> { s1, s2 }, () => { captain.root.gameObject.SetActive(false); then(); });
        }

        Shot HeroShot(int i, string verse)
        {
            return new Shot(verse, 5f, (c, p) =>
            {
                Vector3 h = heroes[i].root.position;
                Look(c, new Vector3(h.x + 0.6f - p * 1.2f, h.y + 1.7f, h.z - 3.2f), new Vector3(h.x, h.y + 1.3f, h.z));
            });
        }

        /// <summary>
        /// A lista dos valentes: Davi, o rei, no trono; os valentes das fases ao lado dele, um a um; o exército
        /// de Israel diante do rei; e "trinta e sete ao todo".
        /// </summary>
        void RollCall()
        {
            Music.Set("victory");
            mode = Mode.Cine;
            arms.Cancel();
            ClearFoes();
            throne.gameObject.SetActive(true);
            for (int i = 0; i < heroes.Count; i++)
            {
                Figure h = heroes[i];
                h.root.gameObject.SetActive(true);
                h.root.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (i == 0)
                {
                    // Sentado no trono.
                    h.root.position = new Vector3(0f, Gorge.Height(0f, ThroneZ) + 0.22f, ThroneZ - 0.1f);
                    foreach (Transform l in h.legs) l.localRotation = Quaternion.Euler(-72f, 0f, 0f);
                }
                else
                {
                    float x = HeroX[i], z = ThroneZ - 0.4f;
                    h.root.position = new Vector3(x, Gorge.Height(x, z), z);
                }
            }
            // O exército diante do rei, em dois blocos, com um corredor no meio.
            for (int i = 0; i < israel.Count; i++)
            {
                float side = i % 2 == 1 ? 1f : -1f; int j = i / 2;
                float x = side * (2.2f + (j % 3) * 1.3f), z = 22f - (j / 3) * 1.6f;
                israel[i].root.position = new Vector3(x, Gorge.Height(x, z), z);
                israel[i].root.rotation = Quaternion.identity;
            }
            Figure king = heroes[0];
            List<Shot> shots = new List<Shot>();
            shots.Add(new Shot("f7rc0", 6f, (c, p) => Look(c, new Vector3(0f, 2.2f + p * 0.6f, ThroneZ - 10f + p * 2f), new Vector3(0f, 1.6f, ThroneZ))));
            for (int i = 1; i < heroes.Count; i++) shots.Add(HeroShot(i, "f7rc" + (i + 1)));
            shots.Add(new Shot("f7rc1", 7f, (c, p) =>
            {
                Vector3 h = king.root.position;
                Look(c, new Vector3(0.8f - p * 1.6f, h.y + 1.6f + p * 0.3f, h.z - 3.6f + p * 0.8f), new Vector3(h.x, h.y + 1.5f, h.z));
            }));
            shots.Add(new Shot("f7v39", 8f, (c, p) => Look(c, new Vector3(0f, 3f + p * 4f, ThroneZ - 7f - p * 12f), new Vector3(0f, 1.4f, ThroneZ))));
            cutscene.Play(shots, () =>
            {
                foreach (Transform l in king.legs) l.localRotation = Quaternion.identity;
                HideFigures();
                ShowResults();
            });
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase7Params par = Fase7Params.Current;
            bool[] ok = { eight, !fell, !retreated, !usedSword };
            string[] txt = { "Opôs-se a oitocentos (chegou aos 800 dentro do tempo)", "E os feriu de uma vez (sem cair)",
                             "Não recuou para trás da pedra", "Brandindo a sua lança (não usou a espada)" };
            string[] refs = { "2 Sm 23:8", "2 Sm 23:8", "2 Sm 23:8", "1 Cr 11:11" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int fight = Mathf.Max(0, felled * 2 + arms.parries * 30 + Mathf.RoundToInt(Mathf.Max(0f, player.health)) * 3 + Mathf.Max(0, Mathf.RoundToInt(timeLeft)) - deaths * 100);
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + fight + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            Progress.SaveWin(7, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("Fase final · " + Difficulty.Current.name);
            c.Title("E os feriu de uma vez");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Batalha · " + felled + " feridos · " + arms.parries + " aparadas · " + Mathf.RoundToInt(time) + " s",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), fight.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], refs[i]);
            c.Space(16);
            c.Verse("f7v39");
            c.Note("2 Samuel diz oitocentos; 1 Crônicas 11:11 diz trezentos e chama o capitão de Jasobeão.");
            c.Gallery();
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só a batalha", false, StartMissionOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }
    }
}
