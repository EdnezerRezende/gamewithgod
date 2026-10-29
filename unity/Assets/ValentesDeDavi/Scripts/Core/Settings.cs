using UnityEngine;

namespace Valentes
{
    /// <summary>Preferências do jogador guardadas entre sessões.</summary>
    public static class Settings
    {
        const string AimKey = "valentes.aimHelp", MusicKey = "valentes.music";

        /// <summary>Linha da trajetória, marcador de impacto e mira dourada sobre alvos.</summary>
        public static bool AimHelp
        {
            get { return PlayerPrefs.GetInt(AimKey, 1) == 1; }
            set { PlayerPrefs.SetInt(AimKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Narration
        {
            get { return PlayerPrefs.GetInt("valentes.narration", 1) == 1; }
            set { PlayerPrefs.SetInt("valentes.narration", value ? 1 : 0); PlayerPrefs.Save(); if (!value) Valentes.Narration.Stop(); }
        }

        public static bool Music
        {
            get { return PlayerPrefs.GetInt(MusicKey, 1) == 1; }
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // ---------------------------------------------------------------- volumes (0 a 1)

        public static float MusicVolume { get { return Get("valentes.musicVolume", 1f); } set { Set("valentes.musicVolume", value); } }
        public static float SfxVolume { get { return Get("valentes.sfxVolume", 1f); } set { Set("valentes.sfxVolume", value); } }
        public static float NarrationVolume { get { return Get("valentes.narrationVolume", 1f); } set { Set("valentes.narrationVolume", value); } }

        // ---------------------------------------------------------------- olhar

        public const float SensitivityMin = 0.3f, SensitivityMax = 2.5f;

        /// <summary>Multiplicador da sensibilidade do mouse, do controle e do arrastar na tela (1 = padrão).</summary>
        public static float Sensitivity
        {
            get { return Mathf.Clamp(Get("valentes.sensitivity", 1f), SensitivityMin, SensitivityMax); }
            set { Set("valentes.sensitivity", Mathf.Clamp(value, SensitivityMin, SensitivityMax)); }
        }

        /// <summary>Inverte o olhar vertical (para cima, o olhar desce).</summary>
        public static bool InvertY
        {
            get { return PlayerPrefs.GetInt("valentes.invertY", 0) == 1; }
            set { PlayerPrefs.SetInt("valentes.invertY", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // ---------------------------------------------------------------- tela

        public static int Quality
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt("valentes.quality", QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1)); }
            set { PlayerPrefs.SetInt("valentes.quality", value); PlayerPrefs.Save(); QualitySettings.SetQualityLevel(value, true); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (PlayerPrefs.HasKey("valentes.quality")) QualitySettings.SetQualityLevel(Quality, true);
        }

        static float Get(string key, float fallback) { return PlayerPrefs.GetFloat(key, fallback); }
        static void Set(string key, float v) { PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save(); }
    }
}
