using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valentes
{
    /// <summary>
    /// Fases do jogo e o progresso do jogador. A fase seguinte só abre depois de vencer a anterior;
    /// vitórias, estrelas e recordes ficam no PlayerPrefs. Para testes, o menu
    /// "Valentes de Davi → Liberar todas as fases" do editor desliga a trava.
    /// </summary>
    public static class Progress
    {
        public class Phase
        {
            public int n;
            public string title, reference, scene;
            public Phase(int n, string title, string reference, string scene) { this.n = n; this.title = title; this.reference = reference; this.scene = scene; }
            /// <summary>Fases ainda sem cena na Unity aparecem no mapa como "em breve".</summary>
            public bool Built { get { return !string.IsNullOrEmpty(scene); } }
        }

        public static readonly Phase[] All =
        {
            new Phase(1, "Davi × Golias", "1 Samuel 17", "Fase1_DaviGolias"),
            new Phase(2, "Samá e o campo de lentilhas", "2 Samuel 23:11-12", "Fase2_Sama"),
            new Phase(3, "Eleazar e a mão pegada à espada", "2 Samuel 23:9-10", "Fase3_Eleazar"),
            new Phase(4, "Os três valentes e a água de Belém", "2 Samuel 23:13-17", "Fase4_Agua"),
            new Phase(5, "Benaia: o leão na cova e o egípcio", "2 Samuel 23:20-23", "Fase5_Benaia"),
            new Phase(6, "Abisai: a lança contra trezentos", "2 Samuel 23:18-19; 21:15-17", "Fase6_Abisai"),
            new Phase(7, "Josebe-Bassebete: oitocentos de uma vez", "2 Samuel 23:8", "Fase7_Josebe"),
        };

        const string UnlockKey = "valentes.unlockAll";
        static string Key(int n, string field) { return "valentes.fase" + n + "." + field; }

        public static bool UnlockAll
        {
            get { return PlayerPrefs.GetInt(UnlockKey, 0) == 1; }
            set { PlayerPrefs.SetInt(UnlockKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static Phase Get(int n) { return n >= 1 && n <= All.Length ? All[n - 1] : null; }
        public static bool Won(int n) { return PlayerPrefs.GetInt(Key(n, "won"), 0) == 1; }
        public static int Stars(int n) { return PlayerPrefs.GetInt(Key(n, "stars"), 0); }
        public static int Best(int n) { return PlayerPrefs.GetInt(Key(n, "best"), 0); }
        public static bool IsOpen(int n) { return n == 1 || UnlockAll || Won(n - 1); }

        /// <summary>Registra a vitória. Devolve true se ela acabou de liberar a fase seguinte.</summary>
        public static bool SaveWin(int n, int stars, int score)
        {
            bool before = IsOpen(n + 1);
            PlayerPrefs.SetInt(Key(n, "won"), 1);
            PlayerPrefs.SetInt(Key(n, "stars"), Mathf.Max(Stars(n), stars));
            PlayerPrefs.SetInt(Key(n, "best"), Mathf.Max(Best(n), score));
            PlayerPrefs.Save();
            return Get(n + 1) != null && !before && IsOpen(n + 1);
        }

        public static void Reset()
        {
            foreach (Phase p in All) { PlayerPrefs.DeleteKey(Key(p.n, "won")); PlayerPrefs.DeleteKey(Key(p.n, "stars")); PlayerPrefs.DeleteKey(Key(p.n, "best")); }
            PlayerPrefs.DeleteKey(UnlockKey);
            PlayerPrefs.Save();
        }

        public static void Load(Phase p)
        {
            if (p == null || !p.Built) return;
            Game.Paused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(p.scene);
        }
    }
}
