namespace Valentes
{
    /// <summary>Parâmetros da fase 5 para cada dificuldade (ver docs/fases/05-benaia-leao-na-cova.md).</summary>
    public class Fase5Params
    {
        public string description;
        public int lionHp;          // vida do leão (em golpes rápidos)
        public float pounceWarn;    // tempo do aviso do bote (agacha e rosna)
        public float parryWindow;   // segundos depois de apertar o botão direito em que o golpe é aparado
        public float disarmSeconds; // tempo segurando Ação para arrancar a lança
        public float egyptianHp;    // vida do egípcio
        public float damage;        // multiplicador do dano recebido
        public bool restartAtLion;  // ao cair: volta ao leão (Valente)
        public float scoreMultiplier;

        static readonly Fase5Params pastor = new Fase5Params
        {
            description = "Leão mais fraco, aviso do bote longo e janela de aparar folgada.",
            lionHp = 10, pounceWarn = 1f, parryWindow = 0.45f, disarmSeconds = 0.8f, egyptianHp = 6f, damage = 0.6f, restartAtLion = false, scoreMultiplier = 1f
        };
        static readonly Fase5Params guerreiro = new Fase5Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            lionHp = 14, pounceWarn = 0.8f, parryWindow = 0.3f, disarmSeconds = 1f, egyptianHp = 8f, damage = 1f, restartAtLion = false, scoreMultiplier = 1.5f
        };
        static readonly Fase5Params valente = new Fase5Params
        {
            description = "Leão forte, aviso curto e o egípcio mais resistente.",
            lionHp = 18, pounceWarn = 0.6f, parryWindow = 0.18f, disarmSeconds = 1.3f, egyptianHp = 10f, damage = 1.4f, restartAtLion = true, scoreMultiplier = 2f
        };

        public static Fase5Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase5Params Current { get { return For(Difficulty.Current.level); } }
    }
}
