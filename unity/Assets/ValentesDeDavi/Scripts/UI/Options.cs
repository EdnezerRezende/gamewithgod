using System;
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

        /// <summary>Abre as opções; "back" desenha a tela anterior de novo.</summary>
        public static void Show(UI ui, Action back)
        {
            Action again = () => Show(ui, back);
            Card c = ui.OpenCard();
            c.Eyebrow("Opções");
            c.Title("Opções");

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
