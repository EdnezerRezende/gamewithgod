using UnityEngine;

namespace Valentes
{
    public enum DifficultyLevel { Pastor, Guerreiro, Valente }

    /// <summary>Parâmetros de cada nível de dificuldade (ver docs/fases/01-davi-golias.md).</summary>
    public class Difficulty
    {
        public DifficultyLevel level;
        public string name;
        public string description;
        public float openingSeconds;     // tempo em que a testa de Golias fica exposta
        public float sweetArcDegrees;    // tamanho da faixa dourada do giro da funda
        public bool trajectoryPreview;   // linha mostrando o caminho da pedra
        public bool sweetArcVisible;     // false = só o som indica o momento certo
        public float swayMultiplier;     // tremor da mira
        public float roarCourageLoss;
        public float javelinDamage;
        public float spearDamage;
        public float lionSpeed;
        public float foreheadRadius;     // tamanho do alvo da testa
        public bool restartAtBrook;      // ao perder: volta ao ribeiro (true) ou ao início do duelo
        public float scoreMultiplier;
        public float goliathSpeed;
        public float aimAssistDegrees;   // ajuda de mira: a mira "gruda" na testa na abertura (0 = sem)
        public float releaseSpread;      // quanto soltar a funda fora do tempo espalha a pedra

        public static readonly Difficulty Pastor = new Difficulty
        {
            level = DifficultyLevel.Pastor, name = "Pastor",
            description = "Abertura longa, tiro perfeito fácil de ver e linha da trajetória.",
            openingSeconds = 5f, sweetArcDegrees = 140f, trajectoryPreview = true, sweetArcVisible = true,
            swayMultiplier = 0.35f, roarCourageLoss = 8f, javelinDamage = 15f, spearDamage = 20f, lionSpeed = 4.2f,
            foreheadRadius = 0.42f, restartAtBrook = false, scoreMultiplier = 1f, goliathSpeed = 0.8f, aimAssistDegrees = 3.5f, releaseSpread = 0.55f
        };

        public static readonly Difficulty Guerreiro = new Difficulty
        {
            level = DifficultyLevel.Guerreiro, name = "Guerreiro",
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            openingSeconds = 3.5f, sweetArcDegrees = 100f, trajectoryPreview = false, sweetArcVisible = true,
            swayMultiplier = 0.75f, roarCourageLoss = 15f, javelinDamage = 25f, spearDamage = 35f, lionSpeed = 6f,
            foreheadRadius = 0.32f, restartAtBrook = false, scoreMultiplier = 1.5f, goliathSpeed = 0.95f, aimAssistDegrees = 2f, releaseSpread = 0.8f
        };

        public static readonly Difficulty Valente = new Difficulty
        {
            level = DifficultyLevel.Valente, name = "Valente",
            description = "Abertura curta, sem indicador visual: só o som do giro certo.",
            openingSeconds = 2f, sweetArcDegrees = 60f, trajectoryPreview = false, sweetArcVisible = false,
            swayMultiplier = 1.3f, roarCourageLoss = 22f, javelinDamage = 35f, spearDamage = 50f, lionSpeed = 7.5f,
            foreheadRadius = 0.24f, restartAtBrook = true, scoreMultiplier = 2f, goliathSpeed = 1.1f, aimAssistDegrees = 0f, releaseSpread = 1f
        };

        public static readonly Difficulty[] All = { Pastor, Guerreiro, Valente };

        const string PrefKey = "valentes.difficulty";
        static Difficulty current;

        public static Difficulty Current
        {
            get
            {
                if (current == null)
                {
                    int i = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 1), 0, All.Length - 1);
                    current = All[i];
                }
                return current;
            }
            set
            {
                current = value;
                PlayerPrefs.SetInt(PrefKey, (int)value.level);
                PlayerPrefs.Save();
            }
        }
    }
}
