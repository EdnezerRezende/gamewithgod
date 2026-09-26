namespace Valentes
{
    /// <summary>Parâmetros da fase 2 para cada dificuldade (ver docs/fases/02-sama-campo-de-lentilhas.md).</summary>
    public class Fase2Params
    {
        public string description;
        public float parryWindow;   // segundos após erguer o escudo em que o bloqueio vira aparada
        public int[] waveCounts;    // inimigos por onda (1 a 4)
        public float damage;        // multiplicador do dano dos filisteus
        public float fire;          // velocidade do fogo e do desgaste do campo
        public float holdSeconds;   // tempo de "Permaneça" até o livramento
        public float outLoss;       // coragem perdida por segundo fora do campo
        public int capAlive;        // máximo de inimigos de pé na onda final
        public bool restartAtWave3; // ao cair: volta à onda 3 (Valente)
        public float scoreMultiplier;

        static readonly Fase2Params pastor = new Fase2Params
        {
            description = "Bloqueio perfeito com folga, ondas menores e livramento em 90 s.",
            parryWindow = 0.5f, waveCounts = new[] { 6, 7, 8, 10 }, damage = 0.6f, fire = 0.6f, holdSeconds = 90f,
            outLoss = 3f, capAlive = 7, restartAtWave3 = false, scoreMultiplier = 1f
        };
        static readonly Fase2Params guerreiro = new Fase2Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            parryWindow = 0.3f, waveCounts = new[] { 8, 10, 11, 14 }, damage = 1f, fire = 1f, holdSeconds = 120f,
            outLoss = 6f, capAlive = 9, restartAtWave3 = false, scoreMultiplier = 1.5f
        };
        static readonly Fase2Params valente = new Fase2Params
        {
            description = "Janela de bloqueio curta, ondas grandes e livramento em 150 s.",
            parryWindow = 0.18f, waveCounts = new[] { 10, 12, 14, 18 }, damage = 1.4f, fire = 1.4f, holdSeconds = 150f,
            outLoss = 10f, capAlive = 12, restartAtWave3 = true, scoreMultiplier = 2f
        };

        public static Fase2Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase2Params Current { get { return For(Difficulty.Current.level); } }
    }
}
