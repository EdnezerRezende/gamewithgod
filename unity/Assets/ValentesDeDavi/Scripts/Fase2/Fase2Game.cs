using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 2: Samá e o campo de lentilhas (2 Samuel 23:11-12). Monta o mundo e conduz o fluxo:
    /// abertura → treino no acampamento → o povo foge → quatro ondas → permanecer → livramento → resultado.
    /// </summary>
    public class Fase2Game : MonoBehaviour
    {
        public const string SceneName = "Fase2_Sama";
        enum Mode { Menu, Cine, Training, Battle, Result }
        enum Kind { Full, TrainingOnly, BattleOnly }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        Sling sling;
        SwordShield sword;
        LentilField field;
        Battle battle;
        CampTraining training;
        Cutscene cutscene;
        UI ui;
        Army troop;

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        bool slingMode, modal;
        string medal;
        int trainingScore;
        float savedTimeScale = 1f;

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
            world = World.ForLentilField(transform, skyMaterial);
            Fx.SetWorld(world);

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);
            field = LentilField.Build(transform);
            troop = Army.Build(transform, "Filisteus", 160, 84f, 104f,
                new[] { U.Hex(0x8c2f22), U.Hex(0x9b5a2a), U.Hex(0x7a2a1f), U.Hex(0xa06a38) }, World.LentilHeight);

            GameObject p = new GameObject("Samá");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            sling = p.AddComponent<Sling>();
            sling.player = player; sling.world = world; sling.ui = ui;
            sling.Build();
            sword = p.AddComponent<SwordShield>();
            sword.player = player; sword.ui = ui;
            sword.Build();

            battle = gameObject.AddComponent<Battle>();
            battle.player = player; battle.sword = sword; battle.field = field; battle.ui = ui;
            battle.Wire();
            battle.onFled = OnFled;
            battle.onFall = OnFall;
            battle.onDeliverance = OnDeliverance;
            sword.onPlayerDown = reason => battle.Fall(reason);

            training = gameObject.AddComponent<CampTraining>();
            training.player = player; training.sword = sword; training.field = field; training.ui = ui;
            training.setSling = SetWeapon;
            training.onFinished = OnTrainingFinished;

            Philistine.Ctx = new Philistine.Context { player = player, defender = sword, field = field, ui = ui, trample = battle.Trample, onDown = OnPhilistineDown };

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · Q · trocar arma · botão direito · escudo · F · orar · H · mira · M · música");
            ShowMenu();
        }

        void OnPhilistineDown(Philistine p) { if (!p.training) battle.repelled++; }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            bool playing = (mode == Mode.Training || mode == Mode.Battle) && !cutscene.Playing && !modal && battle.phase != Battle.Phase.Done;
            if (!Game.Paused && (playing || cutscene.Playing) && GameInput.PausePressed()) ShowPause();

            if (TouchControls.Active && !Application.isMobilePlatform && GameInput.MouseUsed()) TouchControls.Active = false;
            bool control = playing && !Game.Paused && !ui.OverlayOpen;
            bool lockCursor = control && !TouchControls.Active;
            UnityEngine.Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !lockCursor;
            player.controlling = control;
            sling.enabled = slingMode;
            sling.SetVisible(control && slingMode);
            sword.active = control && !slingMode;
            sword.SetVisible(control && !slingMode);
            player.speedMultiplier = sword.praying ? 0f : sword.ShieldUp ? 0.55f : 1f;
            player.eyeHeight = Mathf.Lerp(player.eyeHeight, sword.praying ? 1.0f : PlayerController.EyeHeight, Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            player.swayScale = slingMode ? 1f : 0.4f;
            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Game.Paused && !ui.OverlayOpen);
            TouchControls.SetCombatButtons(true);
            TouchControls.SetFireLabel(slingMode ? "Funda" : "Golpe");

            if (control)
            {
                if (GameInput.SwapPressed()) SetWeapon(!slingMode);
                if (GameInput.Weapon1Pressed()) SetWeapon(false);
                if (GameInput.Weapon2Pressed()) SetWeapon(true);
            }
            if (GameInput.NarrationPressed()) { Settings.Narration = !Settings.Narration; ui.Toast(Settings.Narration ? "Narração ligada" : "Narração desligada", 1.2f); }
            if (GameInput.MusicPressed()) { Settings.Music = !Settings.Music; ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f); }
            if (GameInput.AimHelpPressed()) { Settings.AimHelp = !Settings.AimHelp; ui.Toast(Settings.AimHelp ? "Ajuda de mira ligada" : "Ajuda de mira desligada", 1.2f); }

            if (playing) UpdateHud();

            if (mode == Mode.Menu && !Game.Paused)
            {
                float a = Time.time * 0.05f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 30f, 12f, Mathf.Cos(a) * 30f);
                cam.transform.LookAt(new Vector3(0f, 1f, 0f));
            }
            if (mode == Mode.Training) player.clampPosition = CampTraining.Clamp;
        }

        void UpdateHud()
        {
            if (mode == Mode.Battle)
            {
                string weapon = slingMode ? "Funda" : "Espada e escudo";
                bool warn = !LentilField.InField(player.Position) && battle.phase != Battle.Phase.Flee;
                ui.SetBattleStats(player.health, player.courage, battle.integrity, battle.stonesLeft, weapon, battle.WaveLabel, battle.HoldFraction, warn);
            }
            if (battle.prayProgress > 0f) ui.DrawRing(battle.prayProgress / 2f, UI.BronzeHi, "orando...");
            else if (slingMode) { ui.DrawGauge(sling); ui.SetCrosshair(sling.aimOnTarget); }
            else
            {
                ui.SetCrosshair(false);
                string t = sword.ShieldUp ? "escudo erguido" : sword.Charging ? (sword.ChargeFraction >= 1f ? "golpe forte!" : "segure: forte") : sword.Ready ? "clique: golpe" : "";
                ui.DrawRing(sword.Charging ? sword.ChargeFraction : 0f, sword.ChargeFraction >= 1f ? UI.BronzeHi : UI.Olive, t);
            }
        }

        void SetWeapon(bool useSling)
        {
            if (slingMode == useSling) return;
            slingMode = useSling;
            sling.Cancel(); sword.Cancel();
            ui.Toast(useSling ? "Funda" : "Espada e escudo", 0.9f);
        }

        static void Look(Camera c, Vector3 pos, Vector3 target) { c.transform.position = pos; c.transform.LookAt(target); }

        // ------------------------------------------------------------------ telas

        void ShowMenu()
        {
            Game.Paused = false; modal = false;
            Time.timeScale = 1f;
            cutscene.Abort();
            training.Clear();
            battle.phase = Battle.Phase.Idle;
            battle.ClearEnemies();
            field.ResetField();
            battle.PlaceSoldiers(transform);
            mode = Mode.Menu;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 2 · Samá");
            c.Title("Samá e o campo de lentilhas", 56);
            c.Lede("2 Samuel 23:11-12. Todos fogem; Samá fica no meio do campo e o defende de espada e escudo, até o grande livramento.");
            c.PhaseMap(2);
            if (!Progress.IsOpen(2))
            {
                c.Locked(2);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase2Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a batalha", false, StartBattleOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Segurar botão direito", "Q · 1 · 2", "F (segurar)", "W A S D · Shift", "Esc · H · M" },
                new[] { "Golpe rápido · segure e solte para o golpe forte (quebra escudos)", "Erguer o escudo · no instante do golpe, apara",
                        "Trocar entre espada e funda", "Orar por 2 s, parado: recupera coragem", "Andar · correr", "Pausa · ajuda de mira · música" });
            c.Note("No celular: direcional, arrastar para olhar e os botões Golpe, Escudo, ⇄ e Orar. Modelos, sons e versículos provisórios.");
        }

        void SettingsRow(Card c, Action redraw)
        {
            VisualElement row = c.Row();
            Card.Btn(row, "Música: " + (Settings.Music ? "ligada" : "desligada"), false, () => { Settings.Music = !Settings.Music; redraw(); });
            Card.Btn(row, "Ajuda de mira: " + (Settings.AimHelp ? "ligada" : "desligada"), false, () => { Settings.AimHelp = !Settings.AimHelp; redraw(); });
            Card.Btn(row, "Narração: " + (Settings.Narration ? "ligada" : "desligada"), false, () => { Settings.Narration = !Settings.Narration; redraw(); });
            Card.Btn(row, "Como jogar", false, () => Tutorial.Show(ui, null, redraw));
        }

        void ShowPause()
        {
            Game.Paused = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            sling.Cancel(); sword.Cancel();
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
                if (mode == Mode.Training) StartTraining(); else StartBattle(0);
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

        void StartBattleOnly()
        {
            kind = Kind.BattleOnly; medal = null;
            battle.courageStart = 65 + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
            ui.CloseOverlay();
            StartBattle(0);
        }

        void StartTraining()
        {
            Music.Set("training");
            battle.phase = Battle.Phase.Idle;
            battle.ClearEnemies();
            player.Place(CampTraining.Center, 0f, 0f);
            player.health = 100f; player.courage = 70f;
            sling.canThrow = () => true;
            sling.takeStone = null;
            sling.onThrow = null;
            training.Begin();
            mode = Mode.Training;
        }

        void OnTrainingFinished(CampTraining t)
        {
            mode = Mode.Result;
            medal = t.medal; trainingScore = t.score;
            battle.courageStart = t.StartingCourage();
            Philistine.Ctx.onDown = OnPhilistineDown;
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("O acampamento dos valentes");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Flechas aparadas", "Flechas bloqueadas", "Coragem inicial na batalha", "Pontos" },
                    new[] { t.parried + " de 8", t.blocked + " de 8", battle.courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f2v12a");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a batalha", true, () => { ui.CloseOverlay(); StartBattle(0); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartBattle(int fromWave)
        {
            Music.Set("duel");
            training.Clear();
            modal = false;
            Philistine.Ctx.onDown = OnPhilistineDown;
            player.clampPosition = ClampBattle;
            sling.canThrow = () => battle.stonesLeft > 0;
            sling.takeStone = () => { battle.stonesLeft--; return 0.85f; };
            sling.onThrow = null;
            SetWeapon(false);
            battle.Begin(fromWave);
            mode = Mode.Battle;
        }

        static Vector3 ClampBattle(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, -55f, 55f);
            p.z = Mathf.Clamp(p.z, -84f, 48f);
            return p;
        }

        void OnFled()
        {
            modal = true;
            sword.Cancel(); sling.Cancel();
            Card c = ui.OpenCard();
            c.Eyebrow("A escolha");
            c.Title("Você fugiu com o povo");
            c.Lede("Atrás de você, o campo ficou sem ninguém. O relato diz outra coisa:");
            c.Verse("f2v12a");
            VisualElement row = c.Row();
            Card.Btn(row, "Voltar ao meio do campo", true, () => { ui.CloseOverlay(); modal = false; player.Place(new Vector3(0f, 0f, -2f), 0f, 0f); });
            Card.Btn(row, "Menu", false, ShowMenu);
            c.Note("Voltar é permitido, mas o item de Fidelidade \"Não fugiu com o povo\" fica perdido nesta partida.");
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
            bool final = battle.WasFinal && !battle.P.restartAtWave3;
            int w = battle.RestartWave();
            Card c = ui.OpenCard();
            c.Eyebrow("Batalha · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede(final ? "Você recomeça a resistência final do zero. O campo mantém o que sobrou, com pelo menos 40%."
                         : "Você recomeça na onda " + w + ". O campo mantém o que sobrou, com pelo menos 40%.");
            c.Verse("f2v12a");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () =>
            {
                ui.CloseOverlay(); modal = false;
                Music.Set("duel");
                Philistine.Ctx.onDown = OnPhilistineDown;
                if (final) battle.BeginFinal(); else battle.Begin(w);
            });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void OnDeliverance()
        {
            Music.Set("victory");
            sword.Cancel(); sling.Cancel();
            battle.BringSoldiersBack();
            cutscene.Play(new List<Shot>
            {
                new Shot("f2v12b", 5f, (c, p) => Look(c, new Vector3(Mathf.Sin(p * 1.2f) * 14f, 3f + p * 8f, Mathf.Cos(p * 1.2f) * 14f), new Vector3(0f, 1f, 0f))),
                new Shot("f2v12a", 5f, (c, p) => Look(c, new Vector3(-10f + p * 4f, 5f, -16f), new Vector3(0f, 1f, 0f))) { update = p => battle.UpdateSoldiers(Time.deltaTime, new Vector3(0f, 0f, -4f), 2.5f) },
            }, ShowResults);
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase2Params par = Fase2Params.Current;
            bool[] ok = { !battle.fled, battle.fieldTime > 0f && battle.inFieldTime / battle.fieldTime >= 0.9f, battle.integrity >= 50f, battle.deaths == 0 };
            string[] txt = { "Não fugiu com o povo", "Ficou no meio do campo", "O campo continuou de pé", "Permaneceu até o livramento sem cair" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int defense = Mathf.RoundToInt(battle.integrity * 10f) + battle.repelled * 40 + sword.parries * 25;
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + defense + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            bool unlocked = Progress.SaveWin(2, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("O campo de lentilhas · " + Difficulty.Current.name);
            c.Title("O Senhor operou um grande livramento");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Defesa · campo " + Mathf.RoundToInt(battle.integrity) + "% · " + battle.repelled + " afastados · " + sword.parries + " aparadas",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), defense.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], i == 0 ? "2 Sm 23:11" : "2 Sm 23:12");
            c.Space(16);
            c.Verse("f2v12b");
            c.Note("Relato paralelo: 1 Crônicas 11:12-14 descreve uma batalha parecida num campo de cevada, com Eleazar ao lado de Davi.");
            c.NextPhase(2, unlocked);
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só a batalha", false, StartBattleOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cena de abertura

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            battle.PlaceSoldiers(transform);
            Shot s3 = new Shot("f2v11b", 5f, (c, p) => Look(c, new Vector3(8f, 3f, 4f), new Vector3(0f, 1f, -30f)));
            s3.start = () => battle.RunSoldiers();
            s3.update = p => battle.UpdateSoldiers(Time.deltaTime, LentilField.Camp, 6f);
            cutscene.Play(new List<Shot>
            {
                new Shot("f2v8", 5.5f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-40f, -20f, p), Mathf.Lerp(26f, 18f, p), Mathf.Lerp(-40f, -30f, p)), Vector3.zero)),
                new Shot("f2v11a", 6.5f, (c, p) => Look(c, new Vector3(Mathf.Lerp(6f, -6f, p), 4f, Mathf.Lerp(-20f, -16f, p)), new Vector3(0f, 6f, 90f))),
                s3,
            }, then);
        }
    }
}
