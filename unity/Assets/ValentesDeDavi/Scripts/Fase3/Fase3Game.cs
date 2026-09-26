using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 3: Eleazar e a mão pegada à espada (2 Samuel 23:9-10; 1 Crônicas 11:13). Monta o mundo e
    /// conduz o fluxo: abertura → treino → os homens de Israel sobem → três linhas → a mão pegada à
    /// espada e a resistência final → o grande livramento e os despojos → resultado.
    /// </summary>
    public class Fase3Game : MonoBehaviour
    {
        public const string SceneName = "Fase3_Eleazar";
        enum Mode { Menu, Cine, Training, Battle, Result }
        enum Kind { Full, TrainingOnly, BattleOnly }

        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        World world;
        Camera cam;
        PlayerController player;
        EleazarSword sword;
        ValleyBattle battle;
        SwordTraining training;
        Cutscene cutscene;
        UI ui;

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        bool modal;
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
            world = World.ForPasDamim(transform, skyMaterial);
            Fx.SetWorld(world);
            Arrow.ground = PasDamim.Height;

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);
            // A tropa filisteia no fim do vale (vista na abertura).
            Army.Build(transform, "Filisteus", 140, 100f, 125f,
                new[] { U.Hex(0x8c2f22), U.Hex(0x9b5a2a), U.Hex(0x7a2a1f), U.Hex(0xa06a38) }, PasDamim.Height);

            GameObject p = new GameObject("Eleazar");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam; player.world = world;
            sword = p.AddComponent<EleazarSword>();
            sword.player = player; sword.ui = ui;
            sword.Build();
            sword.onHandSticks = OnHandSticks;

            battle = gameObject.AddComponent<ValleyBattle>();
            battle.player = player; battle.sword = sword; battle.ui = ui;
            battle.onFall = OnFall;
            battle.onDeliverance = OnDeliverance;
            sword.onPlayerDown = reason => battle.Fall(reason);

            training = gameObject.AddComponent<SwordTraining>();
            training.player = player; training.sword = sword; training.ui = ui;
            training.onFinished = OnTrainingFinished;

            Foe.Ctx = new Foe.Context
            {
                player = player, sword = sword, ui = ui,
                onDown = f => { if (!f.training) battle.repelled++; },
                forceEngage = () => battle.forceEngage,
            };

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam; cutscene.ui = ui;

            ui.SetHint("Esc · pausa · botão direito · aparar · Espaço · desviar · F · orar · T · trombeta · M · música · N · narração");
            ShowMenu();
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            bool playing = (mode == Mode.Training || mode == Mode.Battle) && !cutscene.Playing && !modal && battle.phase != ValleyBattle.Phase.Done;
            if (!Game.Paused && (playing || cutscene.Playing) && GameInput.PausePressed()) ShowPause();

            if (TouchControls.Active && !Application.isMobilePlatform && GameInput.MouseUsed()) TouchControls.Active = false;
            bool control = playing && !Game.Paused && !ui.OverlayOpen;
            bool lockCursor = control && !TouchControls.Active;
            UnityEngine.Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !lockCursor;
            player.controlling = control;
            sword.active = control;
            sword.SetVisible(control);
            player.speedMultiplier = sword.praying ? 0f : !sword.stuck && sword.fatigue > 60f ? 0.85f : 1f;
            player.eyeHeight = Mathf.Lerp(player.eyeHeight, sword.praying ? 1.0f : PlayerController.EyeHeight, Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
            player.swayScale = 0.4f;
            player.clampPosition = mode == Mode.Training ? (Func<Vector3, Vector3>)PasDamim.ClampTraining : PasDamim.ClampValley;
            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Game.Paused && !ui.OverlayOpen);
            TouchControls.SetCombatButtons(true);
            TouchControls.SetFireLabel("Golpe");
            TouchControls.SetShieldLabel("Aparar");
            TouchControls.SetSwapLabel("Desviar", 15f);
            TouchControls.SwapIsDash = true;
            TouchControls.ShowHorn(mode == Mode.Battle && !battle.hornUsed);

            if (control && mode == Mode.Battle && GameInput.HornPressed()) AskHorn();
            if (GameInput.NarrationPressed()) { Settings.Narration = !Settings.Narration; ui.Toast(Settings.Narration ? "Narração ligada" : "Narração desligada", 1.2f); }
            if (GameInput.MusicPressed()) { Settings.Music = !Settings.Music; ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f); }

            if (playing) UpdateHud();
            else ui.SetFase3Stats(mode == Mode.Battle, training.score, player.health, player.courage, sword.fatigue, "", -1f, false, false);

            if (mode == Mode.Menu && !Game.Paused)
            {
                float a = Time.time * 0.05f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 14f, 12f, Mathf.Cos(a) * 14f + 20f);
                cam.transform.LookAt(new Vector3(0f, 2f, 30f));
            }
        }

        void UpdateHud()
        {
            bool b = mode == Mode.Battle;
            ui.SetFase3Stats(b, training.score, player.health, player.courage, sword.fatigue, b ? battle.WaveLabel : "",
                b ? battle.HoldFraction : -1f, b && battle.Retreating, sword.stuck);
            if (battle.prayProgress > 0f) { ui.DrawRing(battle.prayProgress / 2f, UI.BronzeHi, "orando..."); return; }
            string t = sword.stuck ? "mão pegada à espada"
                : sword.Charging ? (sword.ChargeFraction >= 1f ? "golpe forte!" : "segure: forte")
                : sword.Crit ? "crítico!"
                : sword.comboT > 0f && sword.combo > 0 ? "sequência " + sword.combo + " de 3"
                : "clique: golpe";
            ui.DrawRing(sword.Charging ? sword.ChargeFraction : 0f, sword.ChargeFraction >= 1f ? UI.BronzeHi : UI.Olive, t);
        }

        void OnHandSticks()
        {
            Verses.Verse v = Verses.Get("f3vhand");
            ui.Toast(v.text, 4f);
            Narration.Play("f3vhand");
            ui.SetObjective("A mão pegada à espada", "A espada não sai mais da mão: cada golpe é forte, mas mais lento. Continue lutando.");
        }

        static void Look(Camera c, Vector3 pos, Vector3 target) { c.transform.position = pos; c.transform.LookAt(target); }

        // ------------------------------------------------------------------ telas

        void ShowMenu()
        {
            Game.Paused = false; modal = false;
            Time.timeScale = 1f;
            cutscene.Abort();
            training.Clear();
            battle.phase = ValleyBattle.Phase.Idle;
            battle.ClearAll();
            battle.PlaceSoldiers(-8f, 8f, -36f, -24f);
            sword.ResetState();
            mode = Mode.Menu;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 3 · Eleazar");
            c.Title("Eleazar e a mão pegada à espada", 52);
            c.Lede("2 Samuel 23:9-10. Quando os homens de Israel sobem a colina, Eleazar desce ao vale de Pas-Damim e luta até a mão ficar pegada à espada.");
            c.PhaseMap(3);
            if (!Progress.IsOpen(3))
            {
                c.Locked(3);
                SettingsRow(c, ShowMenu);
                return;
            }
            c.Section("Dificuldade");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, Fase3Params.For(d.level).description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; medal = null; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só a batalha", false, StartBattleOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Clique esquerdo", "Segurar e soltar", "Botão direito", "Espaço", "F (segurar) · T", "W A S D · Shift", "Esc · M · N" },
                new[] { "Golpe · três rápidos seguidos formam a sequência (o terceiro acerta mais longe e mais forte)", "Golpe forte (abre a falange)",
                        "Aparar com a espada no instante do golpe · o próximo golpe é crítico", "Desviar (gasta fôlego)",
                        "Orar · tocar a trombeta", "Andar · correr", "Pausa · música · narração" });
            c.Note("No celular: direcional, arrastar para olhar e os botões Golpe, Aparar, Desviar, Orar e Trombeta. Modelos, sons e versículos provisórios.");
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
            sword.Cancel();
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
            battle.phase = ValleyBattle.Phase.Idle;
            battle.ClearAll();
            player.Place(PasDamim.TrainingCenter, 0f, 0f);
            player.health = 100f; player.courage = 70f;
            training.Begin();
            mode = Mode.Training;
        }

        void OnTrainingFinished(SwordTraining t)
        {
            mode = Mode.Result;
            medal = t.medal; trainingScore = t.score;
            battle.courageStart = t.StartingCourage();
            string[] names = { "bronze", "prata", "ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };
            int mi = Array.IndexOf(names, medal);
            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("O ritmo da espada");
            c.Medal("Medalha de " + char.ToUpper(medal[0]) + medal.Substring(1), colors[mi]);
            c.Tally(new[] { "Ataques aparados", "Ataques desviados", "Coragem inicial na batalha", "Pontos" },
                    new[] { t.parried + " de 8", t.dodged + " de 8", battle.courageStart.ToString("0"), t.score.ToString() }, true);
            c.Verse("f3v10a");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para a batalha", true, () => { ui.CloseOverlay(); StartBattle(0); });
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void StartBattle(int fromLine)
        {
            Music.Set("duel");
            training.Clear();
            modal = false;
            battle.Begin(fromLine);
            mode = Mode.Battle;
        }

        void AskHorn()
        {
            if (battle.hornUsed || !battle.Active) return;
            modal = true;
            sword.Cancel();
            Card c = ui.OpenCard();
            c.Eyebrow("A trombeta");
            c.Title("Chamar o povo para ajudar?");
            c.Lede("Três soldados descerão para lutar ao seu lado. A luta fica mais fácil, mas o relato conta outra coisa:");
            c.Verse("f3v10b");
            VisualElement row = c.Row();
            Card.Btn(row, "Tocar a trombeta", false, () => { ui.CloseOverlay(); modal = false; battle.BlowHorn(); });
            Card.Btn(row, "Continuar sozinho", true, () => { ui.CloseOverlay(); modal = false; });
            c.Note("Tocar a trombeta faz perder o item de Fidelidade \"Lutou sem esperar o povo\".");
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
            bool final = battle.RestartsFinal;
            int line = battle.RestartLine();
            Card c = ui.OpenCard();
            c.Eyebrow("Batalha · " + Difficulty.Current.name);
            c.Title(reason);
            c.Lede((final ? "Você recomeça a resistência final do zero." : "Você recomeça na linha " + line + ".")
                   + (sword.stuck ? " A mão continua pegada à espada." : ""));
            c.Verse("f3v10a");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () =>
            {
                ui.CloseOverlay(); modal = false;
                Music.Set("duel");
                if (final) battle.BeginFinal(); else battle.Begin(line);
            });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void OnDeliverance()
        {
            Music.Set("victory");
            sword.Cancel();
            Vector3 p = player.Position;
            cutscene.Play(new List<Shot>
            {
                new Shot("f3v10b", 6f, (c, k) => Look(c, new Vector3(p.x + Mathf.Sin(k) * 9f, p.y + 3f + k * 3f, p.z - 9f + k * 2f), p + Vector3.up))
                    { update = k => battle.UpdateSoldiers(Time.deltaTime, 3f) },
                new Shot("f3v10a", 5f, (c, k) => Look(c, new Vector3(p.x + 1.2f, p.y + 1.4f, p.z + 1.5f - k * 0.5f), p + new Vector3(0.3f, 1.1f, 0f)))
                    { update = k => battle.UpdateSoldiers(Time.deltaTime, 3f) },
            }, ShowResults);
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Fase3Params par = Fase3Params.Current;
            bool[] ok = { battle.rose, !battle.retreated, battle.stuckAtEnd, !battle.hornUsed };
            string[] txt = { "Levantou-se: desceu ao vale a tempo", "Não recuou para trás de um estandarte",
                             "Lutou até a mão ficar pegada à espada e continuou", "Lutou sem esperar o povo" };
            int nf = 0; foreach (bool b in ok) if (b) nf++;
            int advance = 3 * 300 + battle.repelled * 40 + sword.parries * 30 + Mathf.Max(0, Mathf.RoundToInt(600f - battle.time * 2f));
            int fidPts = nf * 250, train = medal != null ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + advance + fidPts) * par.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            bool unlocked = Progress.SaveWin(3, stars, total);

            Card c = ui.OpenCard();
            c.Eyebrow("Pas-Damim · " + Difficulty.Current.name);
            c.Title("A mão pegada à espada");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (medal != null ? " (" + medal + ")" : " (não jogado)"),
                        "Avanço · 3 linhas · " + battle.repelled + " afastados · " + sword.parries + " aparadas · " + Mathf.RoundToInt(battle.time) + " s",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + Difficulty.Current.name, "Total" },
                new[] { train.ToString(), advance.ToString(), fidPts.ToString(), "×" + par.scoreMultiplier.ToString("0.#"), total.ToString() }, true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], "2 Sm 23:10");
            c.Space(16);
            c.Verse("f3v10b");
            c.Note("Lugar: 1 Crônicas 11:13 diz que foi em Pas-Damim.");
            c.NextPhase(3, unlocked);
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
            battle.ClearAll();
            battle.PlaceSoldiers(-6f, 6f, -4f, 4f);
            Shot s3 = new Shot("f3v9", 7f, (c, p) => Look(c, new Vector3(9f, 4f, Mathf.Lerp(2f, -12f, p)), new Vector3(0f, 2f, -30f)));
            s3.start = () => battle.SendSoldiers(-48f, -38f);
            s3.update = p => battle.UpdateSoldiers(Time.deltaTime, 4f);
            cutscene.Play(new List<Shot>
            {
                new Shot("f2v8", 5.5f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-26f, -14f, p), Mathf.Lerp(22f, 14f, p), Mathf.Lerp(-30f, -10f, p)), new Vector3(0f, 2f, 30f))),
                new Shot("f3v1cr", 6f, (c, p) => Look(c, new Vector3(3f, 3f, Mathf.Lerp(-6f, 6f, p)), new Vector3(0f, 3f, 110f))),
                s3,
            }, then);
        }
    }
}
