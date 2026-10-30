using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// Tela de Opções (volume, sensibilidade, tela, qualidade e a lista de controles) e o "Sair do jogo".
    /// Aparece nos menus e na pausa de todas as fases, em "Opções" e "Sair do jogo".
    /// </summary>
    public static class Options
    {
        /// <summary>No navegador e no celular não há como "sair": o botão não aparece.</summary>
        public static bool CanQuit
        {
            get { return Application.platform != RuntimePlatform.WebGLPlayer && !Application.isMobilePlatform; }
        }

        public static void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Resolução escolhida agora: o Unity só muda a tela no quadro seguinte, então o botão mostra esta.</summary>
        static Vector2Int? chosen;

        /// <summary>Resoluções que o monitor aceita (pelo menos 960 de largura), da menor para a maior, mais a atual.</summary>
        static List<Vector2Int> Resolutions()
        {
            List<Vector2Int> list = new List<Vector2Int>();
            foreach (Resolution r in Screen.resolutions)
            {
                Vector2Int v = new Vector2Int(r.width, r.height);
                if (r.width >= 960 && !list.Contains(v)) list.Add(v);
            }
            Vector2Int cur = new Vector2Int(Screen.width, Screen.height);
            if (!list.Contains(cur)) list.Add(cur);
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        /// <summary>Abre as opções; "back" desenha a tela anterior de novo.</summary>
        public static void Show(UI ui, Action back)
        {
            Action again = () => Show(ui, back);
            Card c = ui.OpenCard();
            c.Eyebrow("Opções");
            c.Title("Opções");

            c.Section("Idioma");
            VisualElement lang = c.Row();
            string langName = Loc.IsPortuguese ? "Idioma: Português" : "Language: English";
            Loc.Done(langName);
            Card.Btn(lang, langName, false, () => { Settings.Language = Loc.IsPortuguese ? Loc.English : Loc.Portuguese; again(); });
            c.Note("O idioma vale por inteiro a partir da próxima fase.");

            c.Section("Som");
            c.Adjust("Música", () => Settings.MusicVolume, v => Settings.MusicVolume = v, 0.1f);
            c.Adjust("Efeitos", () => Settings.SfxVolume, v => Settings.SfxVolume = v, 0.1f, () => Sfx.Play("clang", 0.6f));
            c.Adjust("Narração", () => Settings.NarrationVolume, v => Settings.NarrationVolume = v, 0.1f);
            VisualElement sound = c.Row();
            Card.Btn(sound, "Música: " + (Settings.Music ? "ligada" : "desligada"), false, () => { Settings.Music = !Settings.Music; again(); });
            Card.Btn(sound, "Narração: " + (Settings.Narration ? "ligada" : "desligada"), false, () => { Settings.Narration = !Settings.Narration; again(); });

            c.Section("Olhar e mira");
            c.Adjust("Sensibilidade", () => Mathf.InverseLerp(Settings.SensitivityMin, Settings.SensitivityMax, Settings.Sensitivity),
                v => Settings.Sensitivity = Mathf.Lerp(Settings.SensitivityMin, Settings.SensitivityMax, v), 0.05f);
            VisualElement look = c.Row();
            Card.Btn(look, "Olhar vertical invertido: " + (Settings.InvertY ? "sim" : "não"), false, () => { Settings.InvertY = !Settings.InvertY; again(); });
            Card.Btn(look, "Ajuda de mira: " + (Settings.AimHelp ? "ligada" : "desligada"), false, () => { Settings.AimHelp = !Settings.AimHelp; again(); });

            if (!Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                c.Section("Tela");
                VisualElement screen = c.Row();
                bool full = Screen.fullScreen;
                Card.Btn(screen, "Tela: " + (full ? "cheia" : "em janela"), false, () =>
                {
                    Screen.fullScreenMode = full ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
                    again();
                });
                List<Vector2Int> res = Resolutions();
                Vector2Int now = chosen ?? new Vector2Int(Screen.width, Screen.height);
                if (chosen.HasValue && chosen.Value.x == Screen.width && chosen.Value.y == Screen.height) chosen = null;
                Card.Btn(screen, "Resolução: " + now.x + "×" + now.y, false, () =>
                {
                    int i = res.IndexOf(now);
                    Vector2Int next = res[(i + 1) % res.Count];
                    Screen.SetResolution(next.x, next.y, Screen.fullScreenMode);
                    chosen = next;
                    again();
                });
                Card.Btn(screen, "Sincronia vertical: " + (QualitySettings.vSyncCount > 0 ? "ligada" : "desligada"), false, () => { Settings.VSync = QualitySettings.vSyncCount == 0; again(); });
                string[] names = QualitySettings.names;
                if (names.Length > 1)
                    Card.Btn(screen, "Qualidade: " + names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, names.Length - 1)], false, () =>
                    {
                        Settings.Quality = (QualitySettings.GetQualityLevel() + 1) % names.Length;
                        again();
                    });
            }

            c.Section("Controles");
            Card.Controls(c,
                new[] { "Mouse e teclado", "W A S D · Shift", "Botão esquerdo · direito", "Espaço · Q · F · E · T", "Esc · H · M · N",
                        "Controle: analógicos", "Controle: RT ou RB · LT ou LB", "Controle: A · X · Y · B", "Controle: clique no analógico · D-pad · Start" },
                new[] { "Olhar e mirar", "Andar · correr", "Golpe ou funda · escudo, aparar e zoom da mira", "Desviar · trocar de arma ou ordem · orar (segurar) · ação (segurar) · trombeta",
                        "Pausa · ajuda de mira · música · narração",
                        "Esquerdo anda, direito olha", "Golpe ou funda · escudo, aparar e zoom da mira", "Desviar · trocar de arma ou ordem · orar (segurar) · ação (segurar)",
                        "Correr · cima trombeta, baixo ajuda de mira · pausa" });
            c.Note("Nos menus, com o controle: D-pad ou analógico esquerdo escolhem, A confirma e esquerda e direita ajustam as barras.");

            VisualElement row = c.Row();
            Card.Btn(row, "Voltar", true, back);
        }
    }
}
