using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Fase 1: Davi × Golias (1 Samuel 17). Monta o mundo e conduz o fluxo:
    /// abertura → lembrança do pastor (treino) → armadura → ribeiro → duelo → vitória → resultado.
    /// Colocado num único objeto da cena criada pelo menu "Valentes de Davi" do editor.
    /// </summary>
    public class Game : MonoBehaviour
    {
        enum Mode { Menu, Cine, Training, Duel, Result }
        enum Kind { Full, TrainingOnly, DuelOnly }

        [Tooltip("Material-base do pipeline (URP Lit). Criado automaticamente pelo editor.")]
        public Material baseMaterial;
        public Material skyMaterial;
        public PanelSettings panelSettings;

        public static bool Paused;

        World world;
        Camera cam;
        PlayerController player;
        Sling sling;
        Goliath goliath;
        Duel duel;
        Training training;
        Cutscene cutscene;
        UI ui;

        Mode mode = Mode.Menu;
        Kind kind = Kind.Full;
        bool trainingDone, seenDuelCine;
        Training.Medal medal;
        int trainingScore, courageStart = 55;
        bool armor;
        List<float> chosenStones = new List<float>();
        float savedTimeScale = 1f, slowUntil, victoryT;
        Transform cineFlock;

        // ------------------------------------------------------------------ montagem

        void Start()
        {
            Paused = false;
            Time.timeScale = 1f;
#if !ENABLE_INPUT_SYSTEM
            Input.simulateMouseWithTouches = false;   // toque não vira clique (os controles de toque cuidam disso)
#endif
            Mats.Init(baseMaterial);
            Sfx.Create(transform);
            Music.Create(transform);
            world = new World(transform, skyMaterial);
            Fx.SetWorld(world);

            GameObject camGo = new GameObject("Câmera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            ui = new UI(transform, panelSettings);

            GameObject p = new GameObject("Davi");
            p.transform.SetParent(transform, false);
            player = p.AddComponent<PlayerController>();
            player.cam = cam;
            player.world = world;
            sling = p.AddComponent<Sling>();
            sling.player = player;
            sling.world = world;
            sling.ui = ui;
            sling.Build();
            sling.SetVisible(false);

            goliath = Goliath.Build(world.valley.transform);
            goliath.player = player;
            goliath.world = world;
            goliath.ui = ui;

            duel = gameObject.AddComponent<Duel>();
            duel.goliath = goliath;
            duel.player = player;
            duel.sling = sling;
            duel.ui = ui;
            duel.BuildHitZones();
            duel.onWin = OnDuelWon;
            duel.onLose = OnDuelLost;
            goliath.duel = duel;

            training = gameObject.AddComponent<Training>();
            training.world = world;
            training.player = player;
            training.ui = ui;
            training.onFinished = OnTrainingFinished;

            cutscene = gameObject.AddComponent<Cutscene>();
            cutscene.cam = cam;
            cutscene.ui = ui;

            ShowMenu();
        }

        // ------------------------------------------------------------------ ciclo

        void Update()
        {
            ui.Tick(Time.unscaledDeltaTime);
            if (slowUntil > 0f && Time.unscaledTime >= slowUntil && !Paused) { slowUntil = 0f; Time.timeScale = 1f; }

            bool playing = (mode == Mode.Training || mode == Mode.Duel) && !cutscene.Playing && !duel.Over;
            if (!Paused && (playing || cutscene.Playing) && GameInput.PausePressed()) ShowPause();

            // Notebook com tela de toque: voltar a usar o mouse desliga os controles de toque.
            if (TouchControls.Active && !Application.isMobilePlatform && GameInput.MouseUsed()) TouchControls.Active = false;
            bool control = playing && !Paused && !ui.OverlayOpen;
            bool lockCursor = control && !TouchControls.Active;   // no toque não há cursor para travar
            UnityEngine.Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !lockCursor;
            player.controlling = control;
            sling.SetVisible(playing && !Paused);
            ui.ShowHud(playing && !ui.OverlayOpen);
            ui.ShowTouch(playing && !Paused && !ui.OverlayOpen);
            if (playing) { ui.DrawGauge(sling); ui.SetCrosshair(sling.aimOnTarget); }

            if (GameInput.MusicPressed())
            {
                Settings.Music = !Settings.Music;
                ui.Toast(Settings.Music ? "Música ligada" : "Música desligada", 1.2f);
            }
            if (GameInput.AimHelpPressed())
            {
                Settings.AimHelp = !Settings.AimHelp;
                ui.Toast(Settings.AimHelp ? "Ajuda de mira ligada" : "Ajuda de mira desligada", 1.2f);
            }

            if (Paused) return;
            if (mode == Mode.Menu)
            {
                float a = Time.time * 0.05f;
                cam.transform.position = new Vector3(Mathf.Sin(a) * 48f, 14f, Mathf.Cos(a) * 48f - 6f);
                cam.transform.LookAt(new Vector3(0f, 3f, 4f));
                goliath.Pace(Time.time);
            }
            if (duel.won)
            {
                victoryT += Time.deltaTime;
                if (victoryT > 2f) world.philistines.offset.z = Mathf.Min(70f, world.philistines.offset.z + 7f * Time.deltaTime);
                if (victoryT > 2.5f) world.israel.offset.z = Mathf.Min(45f, world.israel.offset.z + 4.5f * Time.deltaTime);
            }
        }

        static void Look(Camera c, Vector3 pos, Vector3 target)
        {
            c.transform.position = pos;
            c.transform.LookAt(target);
        }

        void ClearProjectiles()
        {
            foreach (Stone s in FindObjectsByType<Stone>(FindObjectsSortMode.None)) Destroy(s.gameObject);
            foreach (Javelin j in FindObjectsByType<Javelin>(FindObjectsSortMode.None)) Destroy(j.gameObject);
        }

        /// <summary>Tons de memória (sépia) para a lembrança dos campos de Belém.</summary>
        void SetFlashbackLook(bool on)
        {
            world.sun.color = on ? U.Hex(0xf2d3a0) : U.Hex(0xffc988);
            RenderSettings.fogColor = on ? U.Hex(0xc8ae84) : U.Hex(0xd39a62);
            RenderSettings.ambientEquatorColor = on ? U.Hex(0xcdb58a) : U.Hex(0xd9a070);
        }

        // ------------------------------------------------------------------ menu e pausa

        void ShowMenu()
        {
            Paused = false;
            Time.timeScale = 1f;
            slowUntil = 0f;
            cutscene.Abort();
            training.ClearAll();
            ClearProjectiles();
            if (cineFlock != null) Destroy(cineFlock.gameObject);
            PlaceDuel();
            goliath.state = Goliath.State.Idle;
            mode = Mode.Menu;
            Music.Set("menu");

            Card c = ui.OpenCard();
            c.Eyebrow("Fase 1 · Davi × Golias");
            c.Title("Os Valentes de Davi", 64);
            c.Lede("1 Samuel 17. Treine a funda nos campos de Belém, escolha as pedras no ribeiro e enfrente o gigante no Vale de Elá.");
            VisualElement grid = c.Choices();
            foreach (Difficulty d in Difficulty.All)
            {
                Difficulty dd = d;
                Card.Choice(grid, d.name, d.description, d == Difficulty.Current, () => { Difficulty.Current = dd; ShowMenu(); });
            }
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase completa", true, StartFull);
            Card.Btn(row, "Só o treino", false, () => { kind = Kind.TrainingOnly; ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Só o duelo", false, StartDuelOnly);
            SettingsRow(c, ShowMenu);
            Card.Controls(c,
                new[] { "Mouse", "Segurar botão esquerdo", "W A S D", "Setas", "Esc", "Enter", "H", "M" },
                new[] { "Olhar e mirar", "Girar a funda; soltar na faixa dourada é o tiro perfeito", "Andar · Shift para correr",
                        "Olhar sem mouse", "Pausa: continuar, recomeçar ou voltar ao menu", "Pular a cena animada",
                        "Ajuda de mira: trajetória e marcador de onde a pedra vai cair", "Ligar ou desligar a música" });
            c.Note("Modelos e sons provisórios. Os versículos são provisórios (Almeida, domínio público).");
        }

        /// <summary>Botões de música e ajuda de mira; ao trocar, redesenha a tela atual.</summary>
        void SettingsRow(Card c, Action redraw)
        {
            VisualElement row = c.Row();
            Card.Btn(row, "Música: " + (Settings.Music ? "ligada" : "desligada"), false, () => { Settings.Music = !Settings.Music; redraw(); });
            Card.Btn(row, "Ajuda de mira: " + (Settings.AimHelp ? "ligada" : "desligada"), false, () => { Settings.AimHelp = !Settings.AimHelp; redraw(); });
        }

        void ShowPause()
        {
            Paused = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            sling.Cancel();
            RenderPause();
        }

        void RenderPause()
        {
            bool inCine = cutscene.Playing;
            Card c = ui.OpenCard();
            c.Eyebrow("Pausa · " + (inCine ? "Cena animada" : mode == Mode.Training ? "Treino" : "Duelo") + " · " + Difficulty.Current.name);
            c.Title("Jogo pausado");
            VisualElement row = c.Row();
            Card.Btn(row, "Continuar", true, Resume);
            if (inCine) Card.Btn(row, "Pular a cena", false, () => { Resume(); cutscene.End(); });
            else Card.Btn(row, mode == Mode.Training ? "Recomeçar o treino" : "Recomeçar o duelo", false, () =>
            {
                Resume();
                if (mode == Mode.Training) StartTraining();
                else { ClearProjectiles(); PlaceDuel(); BeginDuel(); }
            });
            Card.Btn(row, "Voltar ao menu", false, ShowMenu);
            SettingsRow(c, RenderPause);
            c.Note("No duelo, recomeçar mantém a armadura e as pedras escolhidas.");
        }

        void Resume()
        {
            Paused = false;
            Time.timeScale = savedTimeScale;
            ui.CloseOverlay();
        }

        // ------------------------------------------------------------------ fluxo

        void StartFull()
        {
            kind = Kind.Full;
            trainingDone = false;
            ui.CloseOverlay();
            IntroCine(() => FlashbackCine(StartTraining));
        }

        void StartDuelOnly()
        {
            kind = Kind.DuelOnly;
            trainingDone = false;
            courageStart = 55 + (Difficulty.Current.level == DifficultyLevel.Pastor ? 10 : 0);
            GoArmor();
        }

        void StartTraining()
        {
            Music.Set("training");
            ClearProjectiles();
            if (cineFlock != null) Destroy(cineFlock.gameObject);
            world.Show(World.Area.Field);
            SetFlashbackLook(true);
            player.Place(new Vector3(0f, 0f, 3f), Mathf.PI, 0.02f);
            player.clampPosition = pos =>
            {
                Vector3 d = pos - new Vector3(0f, pos.y, 2f);
                if (d.magnitude > 14f) pos = new Vector3(0f, pos.y, 2f) + d.normalized * 14f;
                return pos;
            };
            player.courage = 70f;
            player.armor = false;
            player.health = 100f;
            sling.canThrow = () => true;
            sling.takeStone = null;
            sling.onThrow = s => training.shots++;
            training.Begin();
            mode = Mode.Training;
        }

        void OnTrainingFinished(Training t)
        {
            mode = Mode.Result;
            trainingDone = true;
            medal = t.medal;
            trainingScore = t.score;
            courageStart = t.StartingCourage();
            string[] names = { "Bronze", "Prata", "Ouro" };
            Color[] colors = { U.Hex(0xc9804a), U.Hex(0xd6d9dc), U.Hex(0xf0c24f) };

            Card c = ui.OpenCard();
            c.Eyebrow("Treino concluído · " + Difficulty.Current.name);
            c.Title("Os campos de Belém");
            c.Medal("Medalha de " + names[(int)medal], colors[(int)medal]);
            c.Tally(
                new[] { "Pedras atiradas", "Precisão", "Ovelhas salvas", "Coragem inicial no duelo", "Pontos" },
                new[] { t.shots.ToString(), Mathf.RoundToInt(t.accuracy * 100f) + "%", t.savedSheep + " de 5", courageStart.ToString(), t.score.ToString() },
                true);
            c.Verse("v37");
            VisualElement row = c.Row();
            Card.Btn(row, kind == Kind.Full ? "Continuar" : "Seguir para o duelo", true, GoArmor);
            Card.Btn(row, "Repetir o treino", false, () => { ui.CloseOverlay(); StartTraining(); });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void GoArmor()
        {
            mode = Mode.Result;
            training.ClearAll();
            PlaceDuel();
            Card c = ui.OpenCard();
            c.Eyebrow("Antes do duelo");
            c.Title("A armadura de Saul");
            c.Verse("v39");
            VisualElement grid = c.Choices();
            Card.Choice(grid, "Vestir a armadura", "Recebe metade do dano, mas anda devagar e a mira balança mais.", false, () => { armor = true; GoStones(); });
            Card.Choice(grid, "Recusar, como Davi", "Leve e ágil. Conta para a Fidelidade ao relato.", false, () => { armor = false; GoStones(); });
            Card.Btn(c.Row(), "Voltar ao menu", false, ShowMenu);
        }

        void GoStones()
        {
            mode = Mode.Result;
            float[] vals = { U.Rand(0.86f, 0.98f), U.Rand(0.84f, 0.96f), U.Rand(0.8f, 0.93f), U.Rand(0.5f, 0.68f),
                             U.Rand(0.48f, 0.66f), U.Rand(0.52f, 0.7f), U.Rand(0.2f, 0.38f), U.Rand(0.22f, 0.4f) };
            for (int i = vals.Length - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); float tmp = vals[i]; vals[i] = vals[j]; vals[j] = tmp; }
            Texture2D[] imgs = new Texture2D[vals.Length];
            for (int i = 0; i < vals.Length; i++) imgs[i] = StoneArt.Draw(vals[i], i * 1.37f + U.Rand(0f, 5f));

            List<int> picked = new List<int>();
            Card c = ui.OpenCard();
            c.Eyebrow("O ribeiro");
            c.Title("Cinco seixos lisos");
            c.Verse("v40");
            c.Lede("Escolha 5 pedras. As lisas e arredondadas voam mais retas; as ásperas desviam. A ordem da escolha é a ordem de uso.");
            Button ok = null;
            List<Button> buttons = null;
            buttons = c.Stones(imgs, i =>
            {
                int at = picked.IndexOf(i);
                if (at >= 0) picked.RemoveAt(at);
                else if (picked.Count < 5) picked.Add(i);
                for (int k = 0; k < buttons.Count; k++) Card.MarkStone(buttons[k], picked.IndexOf(k) + 1);
                ok.SetEnabled(picked.Count == 5);
                ok.text = "Guardar no alforje (" + picked.Count + "/5)";
            });
            VisualElement row = c.Row();
            ok = Card.Btn(row, "Guardar no alforje (0/5)", true, () =>
            {
                chosenStones.Clear();
                foreach (int i in picked) chosenStones.Add(vals[i]);
                ui.CloseOverlay();
                if (kind != Kind.DuelOnly || !seenDuelCine) { seenDuelCine = true; DuelCine(BeginDuel); }
                else { PlaceDuel(); BeginDuel(); }
            });
            ok.SetEnabled(false);
            Card.Btn(row, "Voltar à armadura", false, GoArmor);
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void PlaceDuel()
        {
            world.Show(World.Area.Valley);
            SetFlashbackLook(false);
            goliath.Place(new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -28f));
            goliath.state = Goliath.State.Idle;
            player.Place(new Vector3(0f, 0f, -28f), 0f, 0.08f);
            world.philistines.offset = Vector3.zero;
            world.israel.offset = Vector3.zero;
            world.israel.afraid = false;
            duel.won = duel.lost = false;
            victoryT = 0f;
        }

        void BeginDuel()
        {
            Music.Set("duel");
            ClearProjectiles();
            player.clampPosition = duel.ClampPlayer;
            duel.Begin(chosenStones, courageStart, armor);
            mode = Mode.Duel;
        }

        void OnDuelWon()
        {
            Music.Set("victory");
            Time.timeScale = 0.35f;
            slowUntil = Time.unscaledTime + 1.6f;
            Transform g = goliath.rig.root;
            cutscene.Play(new List<Shot>
            {
                new Shot("v49", 4.5f, (c, p) => Look(c, g.position + new Vector3(4.5f - p * 1.5f, 1.6f + p * 0.5f, -2.5f), g.position + Vector3.up * 1.4f)),
                new Shot("v51", 4.5f, (c, p) => Look(c, new Vector3(-14f + p * 6f, 16f, 8f), new Vector3(0f, 10f, 80f))) { start = () => Sfx.Play("cheer") },
                new Shot("v47", 4f, (c, p) => Look(c, new Vector3(0f, 4f + p * 18f, -38f - p * 12f), new Vector3(0f, 2f, 22f))),
            }, ShowResults);
        }

        void OnDuelLost(string reason)
        {
            Music.Set("quiet");
            mode = Mode.Result;
            StartCoroutine(ShowDefeat(reason));
        }

        IEnumerator ShowDefeat(string reason)
        {
            yield return new WaitForSecondsRealtime(0.9f);
            bool brook = Difficulty.Current.restartAtBrook;
            Card c = ui.OpenCard();
            c.Eyebrow("Duelo · " + Difficulty.Current.name);
            c.Title("Golias prevaleceu desta vez");
            c.Lede(reason + " " + (brook
                ? "No nível Valente, você volta ao ribeiro para escolher as pedras de novo."
                : "Você recomeça no início do duelo, com as mesmas pedras."));
            c.Verse("v37");
            VisualElement row = c.Row();
            Card.Btn(row, "Tentar de novo", true, () =>
            {
                if (brook) GoStones();
                else { ui.CloseOverlay(); PlaceDuel(); BeginDuel(); }
            });
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        void ShowResults()
        {
            mode = Mode.Result;
            Difficulty d = Difficulty.Current;
            float avg = 0f;
            foreach (float s in chosenStones) avg += s;
            avg /= Mathf.Max(1, chosenStones.Count);
            bool[] ok = { !armor, avg >= 0.72f, duel.approach >= 14f, duel.used == 1 };
            string[] txt = { "Recusou a armadura de Saul", "Escolheu pedras lisas no ribeiro", "Correu em direção a Golias", "Venceu com uma única pedra" };
            string[] refs = { "17:39", "17:40", "17:48", "17:49" };
            int nf = 0;
            foreach (bool b in ok) if (b) nf++;
            int duelPts = 1000 + (5 - duel.used) * 150 + Mathf.RoundToInt(Mathf.Max(0f, player.health)) * 4 + Mathf.Max(0, Mathf.RoundToInt(400f - duel.time * 3f));
            int fidPts = nf * 250, train = trainingDone ? trainingScore : 0;
            int total = Mathf.RoundToInt((train + duelPts + fidPts) * d.scoreMultiplier);
            int stars = 1 + (nf >= 2 ? 1 : 0) + (nf == 4 ? 1 : 0);
            string[] medals = { "bronze", "prata", "ouro" };

            Card c = ui.OpenCard();
            c.Eyebrow("Vitória no Vale de Elá · " + d.name);
            c.Title("Golias caiu sobre o seu rosto");
            c.Stars(stars);
            c.Tally(
                new[] { "Treino" + (trainingDone ? " (" + medals[(int)medal] + ")" : " (não jogado)"),
                        "Duelo · " + duel.used + (duel.used == 1 ? " pedra · " : " pedras · ") + Mathf.RoundToInt(duel.time) + " s",
                        "Fidelidade ao relato · " + nf + " de 4", "Multiplicador " + d.name, "Total" },
                new[] { train.ToString(), duelPts.ToString(), fidPts.ToString(), "×" + d.scoreMultiplier.ToString("0.#"), total.ToString() },
                true);
            for (int i = 0; i < 4; i++) c.Check(ok[i], txt[i], refs[i]);
            c.Space(16);
            c.Verse("v46");
            VisualElement row = c.Row();
            Card.Btn(row, "Jogar a fase de novo", true, StartFull);
            Card.Btn(row, "Repetir só o duelo", false, StartDuelOnly);
            Card.Btn(row, "Menu", false, ShowMenu);
        }

        // ------------------------------------------------------------------ cenas animadas

        void IntroCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            PlaceDuel();
            Transform g = goliath.rig.root;
            Shot s1 = new Shot("v3", 6f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-75f, -15f, p), Mathf.Lerp(34f, 24f, p), Mathf.Lerp(-40f, -62f, p)), new Vector3(0f, 4f, 8f)));
            s1.update = p => goliath.Pace(Time.time);
            Shot s2 = new Shot("v4", 6f, (c, p) =>
            {
                float a = Mathf.Lerp(-0.9f, 0.7f, p);
                Look(c, g.position + new Vector3(Mathf.Sin(a) * 8f, 0.9f, -Mathf.Cos(a) * 8f), g.position + Vector3.up * 2.3f);
            });
            s2.update = p => goliath.Pace(Time.time);
            Shot s3 = new Shot("v10", 5.5f, (c, p) => Look(c, g.position + new Vector3(0.8f, 2.3f, -3.4f + p * 0.6f), g.position + Vector3.up * 2.9f));
            s3.start = () => { Sfx.Play("roar"); };
            s3.update = p => { goliath.Pace(Time.time); goliath.RoarPose(Mathf.Min(1f, p * 4f)); };
            Shot s4 = new Shot("v11", 5.5f, (c, p) => Look(c, new Vector3(10f - p * 4f, World.ValleyHeight(10f, -86f) + 2.6f, -90f), new Vector3(0f, 3f, 6f)));
            s4.update = p => { goliath.Pace(Time.time); world.israel.afraid = true; };
            cutscene.Play(new List<Shot> { s1, s2, s3, s4 }, () => { world.israel.afraid = false; then(); });
        }

        void FlashbackCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            world.Show(World.Area.Field);
            SetFlashbackLook(true);
            if (cineFlock != null) Destroy(cineFlock.gameObject);
            cineFlock = U.Pivot(world.field.transform, "Rebanho (cena)", Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                Transform s = Models.Sheep(cineFlock);
                float a = i / 5f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.8f, 0f, -4f + Mathf.Sin(a) * 1.3f);
                p.y = World.FieldHeight(p.x, p.z);
                s.position = p;
                s.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
            cutscene.Play(new List<Shot>
            {
                new Shot("v33", 5.5f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-12f, -6f, p), 6f, Mathf.Lerp(10f, 8f, p)), new Vector3(0f, 0.6f, -4f))),
                new Shot("v34", 8f, (c, p) => Look(c, new Vector3(Mathf.Lerp(-4f, 5f, p), Mathf.Lerp(3.5f, 2.4f, p), Mathf.Lerp(6f, 4f, p)), new Vector3(0f, 0.6f, -6f))),
            }, then);
        }

        void DuelCine(Action then)
        {
            Music.Set("cine");
            mode = Mode.Cine;
            PlaceDuel();
            Transform g = goliath.rig.root;
            Vector3 dp = player.Position;
            Shot s1 = new Shot("v43", 5f, (c, p) => Look(c, g.position + new Vector3(1.6f, 2.4f, -3.6f + p * 0.4f), g.position + Vector3.up * 2.9f));
            s1.update = p => goliath.rig.head.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            cutscene.Play(new List<Shot>
            {
                s1,
                new Shot("v45", 7f, (c, p) => Look(c, dp + new Vector3(0.7f, 1.85f, -1.3f + p * 1.2f), g.position + Vector3.up * 2.2f)),
                new Shot("v48", 4.5f, (c, p) => Look(c, new Vector3(24f - p * 6f, 5f, -2f), new Vector3(0f, 2f, 0f))),
            }, then);
        }
    }
}
