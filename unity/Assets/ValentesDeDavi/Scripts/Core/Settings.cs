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

        public static bool Music
        {
            get { return PlayerPrefs.GetInt(MusicKey, 1) == 1; }
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
