using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 6: Abisai (2 Samuel 23:18-19; 21:15-17). Abertura → treino → a lança contra trezentos →
    /// Davi se cansou → o socorro contra Isbi-Benobe → "Nunca mais sairás conosco" → resultado.
    /// </summary>
    public class Fase6Game : MonoBehaviour
    {
        public const string SceneName = "Fase6_Abisai";
        enum Mode { Menu, Cine, Training, Mission, Result }
        enum Kind { Full, TrainingOnly, MissionOnly }
        enum Part { None, Three, ThreeDone, Rescue, Done }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        BenaiaArms arms;
        IshbiBenob giant;
        SpearTraining training;
        Cutscene cutscene;
        UI ui;
        Figure david, hero;
        readonly List<Figure> servants = new List<Figure>(), three = new List<Figure>();

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        Part part = Part.None;
        bool modal;
        string medal;
        int trainingScore, deaths, felled;
        float savedTimeScale = 1f, time, timeLeft, spawnT, courageStart = 70f, prayProgress, prayCool, davidHp;
        // Fidelidade
        bool reached300, usedSword, arrived, late, davidHurt, warnedSword;

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
            world = World.ForAbisai(transform, skyMaterial);
            Fx.SetWorld(world);

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);

            GameObject p = new GameObject("Abisai");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            arms = p.AddComponent<BenaiaArms>();
            arms.player = player; arms.ui = ui;
            arms.sweepSpear = true;
            arms.Build();
            arms.weapon = BenaiaWeapon.Spear;
            arms.onPlayerDown = r => OnFall("Abisai caiu.");

            david = Figure.Man(transform, "Davi", U.Hex(0x3d5a8a), false, 1.78f);
            david.RoundShield();
            david.root.gameObject.SetActive(false);
            hero = Figure.Man(transform, "Abisai", U.Hex(0x7a6a48), false);
            hero.Spear();
            hero.root.gameObject.SetActive(false);
            Color[] robes = { U.Hex(0x6f6a3c), U.Hex(0x7d5f3a), U.Hex(0x8a7a52) };
            for (int i = 0; i < 6; i++) { Figure m = Figure.Man(transform, "Servo de Davi", robes[i % 3], false); m.Spear(); m.root.gameObject.SetActive(false); servants.Add(m); }
            for (int i = 0; i < 3; i++) { Figure m = Figure.Man(transform, "Um dos três", robes[i], false); m.Spear(); m.root.gameObject.SetActive(false); three.Add(m); }

            giant = IshbiBenob.Build(transform);
            giant.player = player; giant.arms = arms; giant.ui = ui; giant.david = david.root;
            giant.fighting = () => mode == Mode.Mission && part == Part.Rescue && !modal && !Game.Paused;
            giant.hurtDavid = d => HurtDavid(d, true);
            giant.onFirstBlowAtDavid = () => { if (!arrived) { late = true; ui.Toast("Abisai ainda não chegou!", 1.4f); } };
            giant.onDown = OnGiantDown;
            giant.gameObject.SetActive(false);

            HordeFoe.Ctx = new HordeFoe.Context
            {
                player = player, arms = arms,
                fighting = () => mode == Mode.Mission && (part == Part.Three || part == Part.Rescue) && !modal && !Game.Paused,
                davidPos = () => david.root.position,
                hurtDavid = d => HurtDavid(d, false),
                onFelled = f => { if (part == Part.Three) { felled++; if (felled % 50 == 0 && felled < 300) ui.Toast(felled + " de 300!", 1.2f); } },
            };

            training = gameObject.AddComponent<SpearTraining>();
            training.player = player; training.arms = arms; training.ui = ui;
            training.onFinished = OnTrainingFinished;

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · botão direito · aparar · Espaço · desviar · Q · espada/lança · F · orar · M · música · N · narração");
            ShowMenu();
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            arms.parryWindowOverride = Fase6Params.Current.parryWindow;
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
                    Regenerate(Time.deltaTime);
                    if (part == Part.Three) UpdateThree(Time.deltaTime);
                    else if (part == Part.Rescue) UpdateRescue(Time.deltaTime);
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
                float a = Time.time * 0.05f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 26f, 8f, Mathf.Cos(a) * 26f);
                cam.transform.LookAt(new Vector3(0f, 1f, 0f));
            }
        }

        Vector3 ClampPlayer(Vector3 p)
        {
            if (mode == Mode.Training) return Battlefield.KeepIn(p, Battlefield.Training, 13f);
            if (part == Part.Rescue || part == Part.Done) return Battlefield.KeepIn(p, Battlefield.Rescue, 24f);
            return Battlefield.KeepIn(p, Battlefield.Three, 30f);
        }

        void SwapWeapon()
        {
            bool toSword = arms.weapon == BenaiaWeapon.Spear;
            if (toSword && mode == Mode.Mission && part == Part.Three && !warnedSword)
            {
                warnedSword = true; modal = true; arms.Cancel();
                Card c = ui.OpenCard();
                c.Eyebrow("A espada na cintura");
                c.Title("Trocar a lança pela espada?");
                c.Lede("De perto, a espada é mais rápida. Mas o texto diz:");
                c.Verse("f6vlanca");
                VisualElement row = c.Row();
                Card.Btn(row, "Usar a espada", false, () => { ui.CloseOverlay(); modal = false; arms.weapon = BenaiaWeapon.RackSword; usedSword = true; ui.Toast("Espada", 0.8f); });
                Card.Btn(row, "Ficar com a lança", true, () => { ui.CloseOverlay(); modal = false; });
                c.Note("Usar a espada na luta contra os trezentos faz perder o item de Fidelidade \"Com a sua lança\".");
                return;
            }
            arms.weapon = toSword ? BenaiaWeapon.RackSword : BenaiaWeapon.Spear;
            if (toSword && mode == Mode.Mission && part == Part.Three) usedSword = true;
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
                    ui.Toast("Abisai orou e encontrou coragem.", 1.8f);
                }
            }
            else { arms.praying = false; prayProgress = 0f; }
        }

        /// <summary>Sem inimigos perto, a vida volta aos poucos.</summary>
        void Regenerate(float dt)
        {
            foreach (HordeFoe f in HordeFoe.All) if (f.Alive && Vector3.Distance(f.transform.position, player.Position) < 5f) return;
            player.health = Mathf.Min(100f, player.health + 1.5f * dt);
        }

        void OnSwing(SwingKind k)
        {
            List<Component> targets = new List<Component>();
            foreach (HordeFoe f in HordeFoe.All) if (f.Alive) targets.Add(f);
            if (part == Part.Rescue && giant.Alive) targets.Add(giant);
            List<Component> hits = SpearHits.Pick(arms, targets, c => c.transform.position);
            foreach (Component c in hits)
            {
                if (c is HordeFoe) ((HordeFoe)c).Hit(k);
                else if (c is IshbiBenob) ((IshbiBenob)c).Hit(k, arms.weapon);
            }
            if (hits.Count >= 3 && arms.Sweeping) ui.Toast("Varredura! " + hits.Count + " de uma vez", 0.8f);
        }

        void UpdateHud()
        {
            bool m = mode == Mode.Mission;
            string wave = !m ? "" : part == Part.Three ? "Feridos: " + felled + " de 300 · " + Mathf.FloorToInt(timeLeft / 60f) + ":" + Mathf.FloorToInt(timeLeft % 60f).ToString("00") : part == Part.Rescue ? "Isbi-Benobe" : "";
            ui.SetFase6Stats(m, training.score, player.health, player.courage, wave,
                m && part == Part.Rescue ? giant.hp / Mathf.Max(1f, giant.max) : -1f,
                m && part == Part.Rescue ? davidHp / Fase6Params.Current.davidHp : -1f);
            if (prayProgress > 0f) { ui.DrawRing(prayProgress / 2f, UI.BronzeHi, "orando..."); return; }
            string t = arms.Charging ? (arms.ChargeFraction >= 1f ? (arms.weapon == BenaiaWeapon.Spear ? "varredura!" : "golpe forte!") : "segure: forte")
                : arms.Crit ? "crítico!" : arms.weapon == BenaiaWeapon.Spear ? "lança" : "espada";
            ui.DrawRing(arms.Charging ? arms.ChargeFraction : 0f, arms.ChargeFraction >= 1f ? UI.BronzeHi : UI.Olive, t);
        }

        static void Look(Camera c, Vector3 pos, Vector3 target) { c.transform.position = pos; c.transform.LookAt(target); }

        void ClearFoes() { foreach (HordeFoe f in HordeFoe.All.ToArray()) Destroy(f.gameObject); }

        // ------------------------------------------------------------------ telas

        void ShowMenu()
        {
            Game.Paused = false; modal = false;
            Time.timeScale = 1f;
            cutscene.Abort();
            training.Clear();
            ClearFoes();
            part = Part.None; mode = Mode.Menu;
            giant.gameObject.SetActive(false); david.root.gameObject.SetActive(false); hero.root.gameObject.SetActive(false);
            foreach (Figure f in servants) f.root.gameObject.SetActive(false);
            foreach (Figure f in three) f.root.gameObject.SetActive(false);
            arms.weapon = BenaiaWeapon.Spear;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 6 · Abisai");
            c.Title("Abisai: a lança contra trezentos", 50);
            c.Lede("2 Samuel 23:18-19 e 21:15-17. Abisai ergue a lança contra trezentos. Noutra batalha, quando Davi se cansa e o gigante Isbi-Benobe vai matá-lo, Abisai o socorre.");
            c.PhaseMap(6);
            if (!Progress.IsOpen(6))
            {
                c.Locked(6);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase6Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a missão", false, StartMissionOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Botão direito", "Espaço", "Q", "F (segurar) · Shift", "Esc · M · N" },
                new[] { "Estocada da lança (atravessa até dois) · segure e solte: varredura que acerta todos em volta",
                        "Aparar no instante do golpe (também o golpe que vai para Davi, se você estiver perto do gigante)",
                        "Desviar", "Trocar entre a lança e a espada da cintura", "Orar · correr", "Pausa · música · narração" });
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
            c.Eyebrow("Pausa · " + (inCine ? "Cena animada" : mode == Mode.Training ? "Treino" : "Missão") + " · " + Difficulty.Current.name);
            c.Title("Jogo pausado");
            VisualElement row = c.Row();
            Card.Btn(row, "Continuar", true, Resume);
            if (inCine) Card.Btn(row, "Pular a cena", false, () => { Resume(); cutscene.End(); });
            else Card.Btn(row, mode == Mode.Training ? "Recomeçar o treino" : "Recomeçar a missão", false, () =>
            {
                Resume();
                if (mode == Mode.Training) StartTraining(); else if (part == Part.Rescue) StartRescue(); else StartThree(false);
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
            StartThree(true);
        }

        void StartTraining()
        {
            Music.Set("training");
            part = Part.None;
            ClearFoes();
            giant.gameObject.SetActive(false); david.root.gameObject.SetActive(false);
            mode = Mode.Training;
            player.Place(Battlefield.Training + new Vector3(0f, 0f, -1f), 0f, 0f);
            player.health = 100f; player.courage = 70f;
            training.Begin();
        }

        void OnTrainingFinished(SpearTraining t)
        {
            mode = Mode.Result;
            medal = t.medal; trainingScore = t.score;
            courageStart = t.StartingCourage();
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("A lança do chefe");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Varreduras completas", "Estocadas que atravessaram dois", "Golpes aparados por \"Davi\"", "Coragem inicial", "Pontos" },
                    new[] { t.sweeps.ToString(), t.lines.ToString(), t.blocks + " de 6", courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f6v18a");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a missão", true, () => { ui.CloseOverlay(); StartThree(true); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartThree(bool fresh)
        {
            Music.Set("duel");
            training.Clear();
            ClearFoes();
            modal = false; mode = Mode.Mission;
            if (fresh)
            {
                reached300 = usedSword = arrived = late = davidHurt = warnedSword = false;
                deaths = 0; time = 0f;
                arms.ResetCounters();
            }
            felled = 0; timeLeft = Fase6Params.Current.timeLimit; spawnT = 1f;
            if (!usedSword) arms.weapon = BenaiaWeapon.Spear;
            arms.onSwing = OnSwing;
            giant.gameObject.SetActive(false); david.root.gameObject.SetActive(false);
            player.health = 100f;
            player.courage = fresh ? courageStart : Mathf.Max(player.courage, courageStart * 0.8f);
            player.Place(Battlefield.Three, 0f, 0f);
            part = Part.Three;
            ui.SetObjective("A lança contra trezentos", "Os filisteus chegam em grupos. Estocada (clique) atravessa até dois; segure e solte para varrer todos em volta. Sem inimigos perto, a vida volta.");
            ui.Toast("\"Alçou a sua lança contra trezentos...\"", 2.4f);
        }

        void UpdateThree(float dt)
        {
            timeLeft -= dt;
            spawnT -= dt;
            int alive = 0;
            foreach (HordeFoe f in HordeFoe.All) if (f.Alive) alive++;
            int cap = Fase6Params.Current.maxFoes;
            if (spawnT <= 0f && alive < cap)
            {
                int n = Mathf.Min(cap - alive, UnityEngine.Random.Range(3, 7));
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                for (int i = 0; i < n; i++)
                {
                    float b = a + UnityEngine.Random.Range(-0.35f, 0.35f), r = UnityEngine.Random.Range(22f, 27f);
                    HordeFoe.Spawn(transform, player.Position + new Vector3(Mathf.Sin(b) * r, 0f, Mathf.Cos(b) * r), false);
                }
                spawnT = UnityEngine.Random.Range(2.2f, 3.4f);
            }
            if (felled >= 300) { reached300 = true; EndThree("Trezentos! \"...e os feriu; e tinha nome entre os três.\""); }
            else if (timeLeft <= 0f) EndThree("Os homens de Israel chegam. A luta termina antes dos trezentos.");
        }

        void EndThree(string msg)
        {
            part = Part.ThreeDone;
            ui.Toast(msg, 3f);
            ui.SetObjective("A lança contra trezentos", msg);
            foreach (HordeFoe f in HordeFoe.All) f.Flee();
            StartCoroutine(After(2.6f, TiredCine));
        }

        IEnumerator After(float s, Action a) { yield return new WaitForSeconds(s); if (mode == Mode.Mission) a(); }

        void PlaceDavid()
        {
            davidHp = Fase6Params.Current.davidHp;
            Vector3 d = Battlefield.Rescue + new Vector3(0f, 0f, 4f);
            david.root.position = new Vector3(d.x, Battlefield.Height(d.x, d.z), d.z);
            david.root.rotation = Quaternion.identity;
            david.root.gameObject.SetActive(true);
        }

        void StartRescue()
        {
            Music.Set("duel");
            ClearFoes();
            modal = false; mode = Mode.Mission;
            arms.onSwing = OnSwing;
            arms.weapon = BenaiaWeapon.Spear;
            player.health = 100f; player.courage = Mathf.Max(player.courage, 60f);
            PlaceDavid();
            giant.Place(Battlefield.Rescue + new Vector3(14f, 0f, 16f));
            arrived = false; late = false; davidHurt = false;
            foreach (Figure f in servants) f.root.gameObject.SetActive(false);
            player.Place(Battlefield.Rescue + new Vector3(-6f, 0f, -22f), 0f, 0f);
            for (int i = 0; i < 4; i++)
                HordeFoe.Spawn(transform, Battlefield.Rescue + new Vector3(UnityEngine.Random.Range(-14f, 14f), 0f, UnityEngine.Random.Range(10f, 18f)), true);
            part = Part.Rescue; spawnT = 6f;
            ui.SetObjective("O socorro", "Isbi-Benobe vai atrás de Davi. Corra até ele e fique entre os dois: perto do gigante e de frente para ele, apare (botão direito) o golpe que ia para Davi.");
            ui.Toast("Davi está cansado!", 2f);
        }

        void UpdateRescue(float dt)
        {
            // Davi, cansado, recua devagar para longe do gigante.
            Vector3 p = david.root.position, g = giant.transform.position;
            Vector3 away = p - g; away.y = 0f;
            if (away.magnitude < 9f && giant.Alive) { p += away.normalized * 1.1f * dt; david.Walk(1.1f); } else david.Stand();
            p = Battlefield.KeepIn(p, Battlefield.Rescue, 18f);
            p.y = Battlefield.Height(p.x, p.z);
            david.root.position = p;
            Vector3 look = g - p; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) david.root.rotation = Quaternion.Euler(7f + Mathf.Sin(Time.time * 2f) * 2f, U.YawTo(look.x, look.z), 0f);
            if (!arrived && Vector3.Distance(player.Position, p) < 4.5f) { arrived = true; if (!late) ui.Toast("Abisai chegou ao lado de Davi.", 1.4f); }
            spawnT -= dt;
            int alive = 0;
            foreach (HordeFoe f in HordeFoe.All) if (f.Alive) alive++;
            if (spawnT <= 0f && alive < 4)
            {
                HordeFoe.Spawn(transform, Battlefield.Rescue + new Vector3(UnityEngine.Random.Range(-16f, 16f), 0f, UnityEngine.Random.value < 0.5f ? -18f : 18f), UnityEngine.Random.value < 0.6f);
                spawnT = UnityEngine.Random.Range(6f, 9f);
            }
        }

        void HurtDavid(float dmg, bool byGiant)
        {
            if (mode != Mode.Mission || part != Part.Rescue) return;
            davidHp -= dmg;
            Sfx.Play("thud");
            Fx.Burst(david.root.position + Vector3.up * 1.2f, U.Hex(0x3d5a8a), 4, 1.8f, 0.6f, 0.6f);
            if (byGiant) { davidHurt = true; ui.Toast("O gigante acertou Davi!", 1.4f); }
            if (davidHp <= 0f) OnFall("Davi caiu.");
        }

        void OnGiantDown()
        {
            ui.Toast("Isbi-Benobe caiu.", 2f);
            ui.SetObjective("O gigante caiu", "\"Porém Abisai, filho de Zeruia, o socorreu, e feriu o filisteu...\"");
            part = Part.Done;
            foreach (HordeFoe f in HordeFoe.All) f.Flee();
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
            bool toThree = Fase6Params.Current.restartAtThree || part == Part.Three;
            Card c = ui.OpenCard();
            c.Eyebrow("Missão · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede(toThree ? "Você recomeça a luta contra os trezentos." : "Você recomeça o socorro a Davi.");
            c.Verse(toThree ? "f6vlanca" : "f6v2117a");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () => { ui.CloseOverlay(); if (toThree) StartThree(false); else StartRescue(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cenas

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine; part = Part.None;
            ClearFoes();
            Vector3 a = Battlefield.Three;
            hero.root.gameObject.SetActive(true);
            hero.root.position = new Vector3(a.x, Battlefield.Height(a.x, a.z - 6f), a.z - 6f);
            hero.root.rotation = Quaternion.identity;
            for (int i = 0; i < 3; i++)
            {
                float x = a.x + (i - 1) * 1.6f, z = a.z - 8.5f;
                three[i].root.gameObject.SetActive(true);
                three[i].root.position = new Vector3(x, Battlefield.Height(x, z), z);
                three[i].root.rotation = Quaternion.identity;
            }
            cutscene.Play(new List<Shot>
            {
                new Shot("f6v18a", 6.5f, (c, p) => Look(c, a + new Vector3(5f - p * 3f, 2.4f, 2f - p * 2f), a + new Vector3(0f, 1.4f, -7f))),
            }, () => { hero.root.gameObject.SetActive(false); foreach (Figure f in three) f.root.gameObject.SetActive(false); then(); });
        }

        void TiredCine()
        {
            Music.Set("cine");
            mode = Mode.Cine;
            ClearFoes();
            PlaceDavid();
            giant.Place(Battlefield.Rescue + new Vector3(14f, 0f, 16f));
            Vector3 bf = Battlefield.Rescue;
            for (int i = 0; i < servants.Count; i++)
            {
                float a = i / (float)servants.Count * Mathf.PI * 2f, x = bf.x + Mathf.Cos(a) * 5f, z = bf.z + 4f + Mathf.Sin(a) * 5f;
                servants[i].root.gameObject.SetActive(true);
                servants[i].root.position = new Vector3(x, Battlefield.Height(x, z), z);
                servants[i].root.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
            Shot s1 = new Shot("f6v2115", 7f, (c, p) => Look(c, bf + new Vector3(5f - p * 2f, 2.2f, -4f + p * 2f), bf + new Vector3(0f, 1.2f, 4f)));
            s1.update = p => david.root.rotation = Quaternion.Euler(Mathf.Min(20f, p * 30f), 0f, 0f);
            Shot s2 = new Shot("f6v2116", 7.5f, (c, p) => Look(c, bf + new Vector3(-6f, 1.6f, 2f + p * 2f), giant.transform.position + Vector3.up * 3.4f));
            s2.update = p =>
            {
                Vector3 g = giant.transform.position;
                g.z -= 1.2f * Time.deltaTime; g.y = Battlefield.Height(g.x, g.z);
                giant.transform.position = g;
            };
            cutscene.Play(new List<Shot> { s1, s2 }, StartRescue);
        }

        void EndCine()
        {
            Music.Set("victory");
            mode = Mode.Cine;
            arms.Cancel();
            ClearFoes();
            Vector3 dp = david.root.position;
            david.root.rotation = Quaternion.identity;
            hero.root.gameObject.SetActive(true);
            hero.root.position = new Vector3(dp.x + 1.5f, Battlefield.Height(dp.x + 1.5f, dp.z - 3f), dp.z - 3f);
            for (int i = 0; i < servants.Count; i++)
            {
                float a = i / (float)servants.Count * Mathf.PI * 2f, x = dp.x + Mathf.Cos(a) * 2.6f, z = dp.z + Mathf.Sin(a) * 2.6f;
                servants[i].root.gameObject.SetActive(true);
                servants[i].root.position = new Vector3(x, Battlefield.Height(x, z), z);
                servants[i].root.rotation = Quaternion.Euler(0f, U.YawTo(dp.x - x, dp.z - z), 0f);
            }
            Shot oath = new Shot("f6v2117b", 8f, (c, p) => Look(c, dp + new Vector3(-3f + p * 2f, 2.2f + p, -4.5f), dp + Vector3.up * 1.4f));
            oath.update = p => { foreach (Figure f in servants) f.armR.localRotation = Quaternion.Euler(-Mathf.Min(1f, p * 3f) * 150f, 0f, 0f); };
            cutscene.Play(new List<Shot>
            {
                new Shot("f6v2117a", 6f, (c, p) => Look(c, dp + new Vector3(7f - p * 2f, 2.6f, -6f + p * 2f), giant.transform.position + Vector3.up)),
                oath,
                new Shot("f6v18b", 6f, (c, p) => Look(c, dp + new Vector3(4f, 1.9f, -3f + p), hero.root.position + Vector3.up * 1.6f)),
            }, () =>
            {
                hero.root.gameObject.SetActive(false);
                foreach (Figure f in servants) { f.root.gameObject.SetActive(false); f.armR.localRotation = Quaternion.identity; }
                ShowResults();
            });
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase6Params par = Fase6Params.Current;
            bool[] ok = { reached300, !usedSword, !late, !davidHurt };
            string[] txt = { "Alçou a sua lança contra trezentos", "Com a sua lança (não usou a espada contra os trezentos)",
                             "Socorreu a tempo (chegou a Davi antes do primeiro golpe)", "Davi não foi ferido pelo gigante" };
            string[] refs = { "2 Sm 23:18", "2 Sm 23:18", "2 Sm 21:17", "2 Sm 21:17" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int fight = Mathf.Max(0, felled * 3 + arms.parries * 30 + Mathf.RoundToInt(Mathf.Max(0f, davidHp) / par.davidHp * 400f) + Mathf.Max(0, Mathf.RoundToInt(600f - time)) - deaths * 100);
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + fight + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            bool unlocked = Progress.SaveWin(6, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("Abisai · " + Difficulty.Current.name);
            c.Title("Nunca mais sairás conosco à peleja");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Combate · " + felled + " feridos · " + arms.parries + " aparadas · Davi com " + Mathf.Max(0, Mathf.RoundToInt(davidHp)) + " de vida",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), fight.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], refs[i]);
            c.Space(16);
            c.Verse("f6v19");
            c.Note("Relato paralelo: 1 Crônicas 11:20-21. O socorro a Davi está em 2 Samuel 21:15-17.");
            c.NextPhase(6, unlocked);
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só a missão", false, StartMissionOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }
    }
}
