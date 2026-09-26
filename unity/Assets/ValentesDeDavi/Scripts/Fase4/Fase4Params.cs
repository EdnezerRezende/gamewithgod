namespace Valentes
{
    /// <summary>Parâmetros da fase 4 para cada dificuldade (ver docs/fases/04-agua-da-cisterna-de-belem.md).</summary>
    public class Fase4Params
    {
        public string description;
        public float parryWindow;   // segundos depois de apertar o botão direito em que o golpe é aparado
        public float spill;         // água derramada por golpe recebido (%)
        public float allyDamage;    // multiplicador do dano nos companheiros
        public bool selfRevive;     // companheiros caídos levantam sozinhos depois de 8 s
        public float camp;          // quantidade de filisteus no arraial
        public float alarm;         // velocidade do alarme
        public float drawSeconds;   // tempo para tirar a água da cisterna
        public float damage;        // multiplicador do dano dos filisteus
        public bool restartAtCave;  // ao perder a água ou cair: volta ao começo do vale (Valente)
        public float scoreMultiplier;

        static readonly Fase4Params pastor = new Fase4Params
        {
            description = "Companheiros resistentes, alarme lento e 5 s para tirar a água.",
            parryWindow = 0.45f, spill = 8f, allyDamage = 0.5f, selfRevive = true, camp = 0.7f, alarm = 0.6f, drawSeconds = 5f,
            damage = 0.6f, restartAtCave = false, scoreMultiplier = 1f
        };
        static readonly Fase4Params guerreiro = new Fase4Params
        {
            description = "O equilíbrio pensado para a maioria dos jogadores.",
            parryWindow = 0.3f, spill = 12f, allyDamage = 0.8f, selfRevive = false, camp = 1f, alarm = 1f, drawSeconds = 7f,
            damage = 1f, restartAtCave = false, scoreMultiplier = 1.5f
        };
        static readonly Fase4Params valente = new Fase4Params
        {
            description = "Arraial cheio, alarme rápido e 9 s para tirar a água.",
            parryWindow = 0.18f, spill = 18f, allyDamage = 1.1f, selfRevive = false, camp = 1.35f, alarm = 1.4f, drawSeconds = 9f,
            damage = 1.4f, restartAtCave = true, scoreMultiplier = 2f
        };

        public static Fase4Params For(DifficultyLevel l)
        {
            return l == DifficultyLevel.Pastor ? pastor : l == DifficultyLevel.Valente ? valente : guerreiro;
        }

        public static Fase4Params Current { get { return For(Difficulty.Current.level); } }
    }
}
