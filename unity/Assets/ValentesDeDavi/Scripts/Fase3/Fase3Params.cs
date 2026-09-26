namespace Valentes
{
    /// <summary>Parâmetros da fase 3 para cada dificuldade (ver docs/fases/03-eleazar-mao-pegada-a-espada.md).</summary>
    public class Fase3Params
    {
        public string description;
        public float parryWindow;    // segundos depois de apertar "aparar" em que o golpe inimigo é aparado
        public float fatigue;        // multiplicador do cansaço por golpe
        public float recovery;       // cansaço recuperado por segundo sem atacar
        public int[] lineCounts;     // inimigos por linha (1 a 3)
        public float damage;         // multiplicador do dano dos filisteus
        public float holdSeconds;    // resistência final
        public bool restartAtLine1;  // ao cair: volta à linha 1 (Valente)
        public float scoreMultiplier;

        static readonly Fase3Params pastor = new Fase3Params
        {
            description = "Janela de aparar folgada, cansaço lento e resistência de 60 s.",
            parryWindow = 0.45f, fatigue = 0.7f, recovery = 14f, lineCounts = new[] { 6, 8, 10 }, damage = 0.6f,
            holdSeconds = 60f, restartAtLine1 = false, scoreMultiplier = 1f
        };
        static readonly Fase3Params guerreiro = new Fase3Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            parryWindow = 0.3f, fatigue = 1f, recovery = 10f, lineCounts = new[] { 8, 11, 14 }, damage = 1f,
            holdSeconds = 90f, restartAtLine1 = false, scoreMultiplier = 1.5f
        };
        static readonly Fase3Params valente = new Fase3Params
        {
            description = "Janela de aparar curta, cansaço rápido e resistência de 120 s.",
            parryWindow = 0.18f, fatigue = 1.3f, recovery = 7f, lineCounts = new[] { 10, 14, 18 }, damage = 1.4f,
            holdSeconds = 120f, restartAtLine1 = true, scoreMultiplier = 2f
        };

        public static Fase3Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase3Params Current { get { return For(Difficulty.Current.level); } }
    }
}
