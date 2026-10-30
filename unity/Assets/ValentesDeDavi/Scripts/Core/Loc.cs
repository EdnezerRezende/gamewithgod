using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valentes
{
    /// <summary>
    /// Idiomas do jogo. O texto-fonte do código está em português; para cada outro idioma há um arquivo em
    /// Resources/Idiomas/&lt;código&gt;.txt com linhas "português TAB tradução" (\n = quebra de linha). Uma linha que
    /// começa com "~" é um pedaço de texto que o código monta somando partes (por exemplo "Onda " + n + " de 4"):
    /// esses pedaços são trocados dentro do texto já montado. O que não tiver tradução aparece em português
    /// (e, no editor, vai para o Console como aviso, uma vez). Os versículos têm tradução própria em Verses.cs e
    /// a narração fica em Resources/Narracao/&lt;código&gt;/&lt;chave&gt;.
    /// </summary>
    public static class Loc
    {
        public const string Portuguese = "pt", English = "en";

        static string loaded;
        static readonly Dictionary<string, string> exact = new Dictionary<string, string>();
        static readonly List<KeyValuePair<string, string>> pieces = new List<KeyValuePair<string, string>>();
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static readonly HashSet<string> warned = new HashSet<string>();
        static readonly HashSet<string> already = new HashSet<string>();

        /// <summary>Marca um texto que já está no idioma atual (versículos), para o T deixá-lo como está.</summary>
        public static void Done(string s) { already.Add(s); }

        /// <summary>Código do idioma atual ("pt", "en").</summary>
        public static string Language { get { return Settings.Language; } }
        public static bool IsPortuguese { get { return Language == Portuguese; } }

        /// <summary>Texto do código (em português) no idioma atual.</summary>
        public static string T(string s)
        {
            if (string.IsNullOrEmpty(s) || IsPortuguese) return s;
            Load();
            string r;
            if (cache.TryGetValue(s, out r)) return r;
            r = Translate(s);
            if (cache.Count > 4000) cache.Clear();
            cache[s] = r;
            return r;
        }

        static string Translate(string s)
        {
            string r;
            if (already.Contains(s)) return s;
            if (exact.TryGetValue(s, out r)) return Fix(r);
            string o = s;
            bool changed = false;
            foreach (KeyValuePair<string, string> p in pieces)
                if (o.IndexOf(p.Key, System.StringComparison.Ordinal) >= 0) { o = o.Replace(p.Key, p.Value); changed = true; }
            if (!changed && Regex.IsMatch(s, "[A-Za-zÀ-ÿ]{3,}")) Warn(s);
            return Fix(o);
        }

        /// <summary>Referências bíblicas: "2 Sm 23:8" vira "2 Sam 23:8", "1 Cr" vira "1 Chr", "Crônicas" vira "Chronicles".</summary>
        static string Fix(string s)
        {
            if (Language != English) return s;
            s = Regex.Replace(s, @"\b([12]) Sm\b", "$1 Sam");
            s = Regex.Replace(s, @"\b1 Cr\b", "1 Chr");
            return s.Replace("Crônicas", "Chronicles");
        }

        /// <summary>Traduz e põe em maiúsculas (a maiúscula vem depois, para achar a tradução); o resultado não é traduzido de novo.</summary>
        public static string Upper(string s) { string u = T(s).ToUpperInvariant(); Done(u); return u; }

        /// <summary>Referência de versículo no idioma atual.</summary>
        public static string Ref(string reference) { return IsPortuguese ? reference : Fix(reference); }

        static void Warn(string s)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (warned.Add(s)) Debug.LogWarning("Valentes de Davi: sem tradução (" + Language + "): " + s.Replace("\n", "\\n"));
#endif
        }

        static void Load()
        {
            if (loaded == Language) return;
            loaded = Language;
            exact.Clear(); pieces.Clear(); cache.Clear();
            TextAsset t = Resources.Load<TextAsset>("Idiomas/" + Language);
            if (t == null) { Debug.LogWarning("Valentes de Davi: arquivo de idioma não encontrado: Idiomas/" + Language); return; }
            foreach (string raw in t.text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string pt = Unescape(line.Substring(0, tab)), en = Unescape(line.Substring(tab + 1));
                if (pt.StartsWith("~")) { pieces.Add(new KeyValuePair<string, string>(pt.Substring(1), en)); }
                else exact[pt] = en;
            }
            pieces.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
        }

        static string Unescape(string s) { return s.Replace("\\n", "\n").Replace("\\t", "\t"); }

        /// <summary>Idioma inicial: português se o sistema estiver em português, senão inglês.</summary>
        public static string FromSystem()
        {
            return Application.systemLanguage == SystemLanguage.Portuguese ? Portuguese : English;
        }
    }
}
