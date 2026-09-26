namespace Valentes
{
    /// <summary>Parâmetros da fase 7 para cada dificuldade (ver docs/fases/07-josebe-bassebete-oitocentos.md).</summary>
    public class Fase7Params
    {
        public string description;
        public float timeLimit;     // segundos para chegar aos oitocentos
        public int maxFoes;         // filisteus ao mesmo tempo
        public int shieldEvery;     // um escudeiro a cada N filisteus
        public float parryWindow;
        public float damage;
        public float scoreMultiplier;

        static readonly Fase7Params pastor = new Fase7Params
        {
            description = "Dez minutos para os oitocentos, menos filisteus e menos escudeiros.",
            timeLimit = 600f, maxFoes = 14, shieldEvery = 5, parryWindow = 0.45f, damage = 0.6f, scoreMultiplier = 1f
        };
        static readonly Fase7Params guerreiro = new Fase7Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            timeLimit = 540f, maxFoes = 18, shieldEvery = 4, parryWindow = 0.3f, damage = 1f, scoreMultiplier = 1.5f
        };
        static readonly Fase7Params valente = new Fase7Params
        {
            description = "Oito minutos, multidão maior e mais escudeiros.",
            timeLimit = 480f, maxFoes = 22, shieldEvery = 3, parryWindow = 0.18f, damage = 1.4f, scoreMultiplier = 2f
        };

        public static Fase7Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase7Params Current { get { return For(Difficulty.Current.level); } }
    }
}
