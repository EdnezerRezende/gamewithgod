namespace Valentes
{
    /// <summary>Parâmetros da fase 6 para cada dificuldade (ver docs/fases/06-abisai-lanca-contra-trezentos.md).</summary>
    public class Fase6Params
    {
        public string description;
        public float timeLimit;     // segundos para chegar aos trezentos
        public int maxFoes;         // filisteus ao mesmo tempo
        public float parryWindow;
        public float giantHp;
        public float davidHp;
        public float damage;
        public bool restartAtThree; // ao cair: volta aos trezentos (Valente)
        public float scoreMultiplier;

        static readonly Fase6Params pastor = new Fase6Params
        {
            description = "Sete minutos para os trezentos, menos filisteus e Davi mais resistente.",
            timeLimit = 420f, maxFoes = 10, parryWindow = 0.45f, giantHp = 12f, davidHp = 150f, damage = 0.6f, restartAtThree = false, scoreMultiplier = 1f
        };
        static readonly Fase6Params guerreiro = new Fase6Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            timeLimit = 360f, maxFoes = 14, parryWindow = 0.3f, giantHp = 16f, davidHp = 100f, damage = 1f, restartAtThree = false, scoreMultiplier = 1.5f
        };
        static readonly Fase6Params valente = new Fase6Params
        {
            description = "Cinco minutos, multidão maior e Davi mais frágil.",
            timeLimit = 300f, maxFoes = 18, parryWindow = 0.18f, giantHp = 20f, davidHp = 70f, damage = 1.4f, restartAtThree = true, scoreMultiplier = 2f
        };

        public static Fase6Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase6Params Current { get { return For(Difficulty.Current.level); } }
    }
}
