using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Valentes
{
    /// <summary>
    /// "Como jogar": explica a ideia do jogo (o texto é a missão e a fidelidade ao relato), a
    /// estrutura das fases, a coragem e os controles. Aparece antes da primeira partida completa.
    /// </summary>
    public static class Tutorial
    {
        const string SeenKey = "valentes.tutorial";

        struct Page { public string title, text, verse, reference; public string[] checks; }

        static readonly Page[] Pages =
        {
            new Page { title = "Bem-vindo, valente",
                text = "Os Valentes de Davi conta, em primeira pessoa, as histórias dos guerreiros de Davi registradas na Bíblia. Cada fase é um relato que você vive por dentro: você é o valente.",
                verse = "Estes são os nomes dos valentes que Davi teve...", reference = "2 Samuel 23:8" },
            new Page { title = "O texto é a sua missão",
                text = "Toda fase começa com o texto bíblico, lido em voz alta. Preste atenção nele: o que o texto diz que o herói fez é o que a fase vai pedir de você.",
                checks = new[] { "Davi recusou a armadura de Saul e venceu Golias com uma única pedra.", "Samá ficou no meio do campo de lentilhas quando todo o povo fugiu." } },
            new Page { title = "Fidelidade ao relato",
                text = "Vencer não basta. Cada fase tem conquistas de Fidelidade ao relato: fazer o que o texto conta. Elas valem pontos, e só a fidelidade completa dá as três estrelas. Às vezes o caminho fácil vai contra o texto, como vestir a armadura ou fugir com o povo. O jogo deixa você escolher, mas sempre lembra o que está escrito." },
            new Page { title = "Como cada fase funciona",
                text = "1. Abertura: o texto e a cena.  2. Treino: pratique a habilidade; a medalha define sua coragem inicial.  3. Missão: viva o relato.  4. Resultado: fidelidade, pontos e o versículo final." },
            new Page { title = "Coragem",
                text = "O medo é real: com pouca coragem, a mira treme e os golpes ficam lentos. Você ganha coragem agindo como no texto: avançando contra o gigante, permanecendo na posição, orando. Não é magia: é o que acontece dentro do herói quando ele confia em Deus e age." },
            new Page { title = "Controles",
                text = "Computador: mouse para olhar e mirar, W A S D para andar, botão esquerdo para usar a arma (segurar muda o golpe ou gira a funda).  Celular: direcional à esquerda, arraste à direita para olhar e botões de ação à direita, melhor na horizontal.  Sempre: Esc pausa, H ajuda de mira, M música, N narração." },
        };

        public static bool Seen { get { return PlayerPrefs.GetInt(SeenKey, 0) == 1; } }

        /// <summary>Mostra o tutorial. "then" roda ao terminar (ou pular); sem "then", "close" roda ao fechar.</summary>
        public static void Show(UI ui, Action then, Action close, int i = 0)
        {
            Page p = Pages[i];
            bool last = i == Pages.Length - 1;
            Card c = ui.OpenCard();
            c.Eyebrow("Como jogar · " + (i + 1) + " de " + Pages.Length);
            c.Title(p.title);
            c.Lede(p.text);
            if (p.checks != null) { foreach (string t in p.checks) c.Check(true, t, ""); c.Space(12); }
            if (p.verse != null)
            {
                VisualElement v = new VisualElement();
                v.style.borderLeftColor = UI.Bronze; v.style.borderLeftWidth = 3f; v.style.paddingLeft = 16f; v.style.marginBottom = 22f;
                UI.Text(v, p.verse, 20, UI.Parch, false, true);
                UI.Text(v, p.reference.ToUpperInvariant(), 13, UI.BronzeHi).style.marginTop = 6f;
                c.el.Add(v);
            }
            Action done = () =>
            {
                PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save();
                if (then != null) then(); else if (close != null) close();
            };
            VisualElement row = c.Row();
            if (i > 0) Card.Btn(row, "Voltar", false, () => Show(ui, then, close, i - 1));
            Card.Btn(row, last ? (then != null ? "Começar" : "Fechar") : "Próximo", true, () => { if (last) done(); else Show(ui, then, close, i + 1); });
            if (!last) Card.Btn(row, then != null ? "Pular e começar" : "Fechar", false, done);
        }
    }
}
