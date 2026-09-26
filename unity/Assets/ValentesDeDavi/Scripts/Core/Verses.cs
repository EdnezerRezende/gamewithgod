using System.Collections.Generic;

namespace Valentes
{
    /// <summary>
    /// Textos bíblicos da fase. PROVISÓRIOS: revisar contra a edição da Almeida em domínio público escolhida.
    /// </summary>
    public static class Verses
    {
        public struct Verse
        {
            public string text;
            public string reference;
            public Verse(string t, string r) { text = t; reference = r; }
        }

        static readonly Dictionary<string, Verse> all = new Dictionary<string, Verse>
        {
            { "v3",  new Verse("E os filisteus estavam num monte de um lado, e os israelitas estavam num monte do outro lado; e o vale entre eles.", "1 Samuel 17:3") },
            { "v4",  new Verse("Então saiu do arraial dos filisteus um homem guerreiro, cujo nome era Golias, de Gate, que tinha de altura seis côvados e um palmo.", "1 Samuel 17:4") },
            { "v10", new Verse("Disse mais o filisteu: Hoje desafio as companhias de Israel, dizendo: Dai-me um homem, para que ambos pelejemos.", "1 Samuel 17:10") },
            { "v11", new Verse("Ouvindo então Saul e todo o Israel estas palavras do filisteu, espantaram-se, e temeram muito.", "1 Samuel 17:11") },
            { "v33", new Verse("Porém Saul disse a Davi: Contra este filisteu não poderás ir para pelejar com ele; pois tu ainda és moço.", "1 Samuel 17:33") },
            { "v34", new Verse("Então disse Davi a Saul: Teu servo apascentava as ovelhas de seu pai; e vinha um leão, ou um urso, e tomava uma ovelha do rebanho; e eu saía após ele, e o feria, e livrava-a da sua boca.", "1 Samuel 17:34-35") },
            { "v37", new Verse("O Senhor me livrou da mão do leão, e da do urso; ele me livrará da mão deste filisteu.", "1 Samuel 17:37") },
            { "v39", new Verse("E Saul vestiu a Davi de suas vestes... Então disse Davi a Saul: Não posso andar com isto, pois nunca o usei.", "1 Samuel 17:38-39") },
            { "v40", new Verse("E tomou o seu cajado na mão, e escolheu para si cinco seixos do ribeiro, e pô-los no alforje de pastor.", "1 Samuel 17:40") },
            { "v43", new Verse("Disse, pois, o filisteu a Davi: Sou eu algum cão, para tu vires a mim com paus?", "1 Samuel 17:43") },
            { "v45", new Verse("Davi, porém, disse ao filisteu: Tu vens a mim com espada, e com lança, e com escudo; porém eu venho a ti em nome do Senhor dos Exércitos.", "1 Samuel 17:45") },
            { "v47", new Verse("...porque do Senhor é a guerra, e ele vos entregará na nossa mão.", "1 Samuel 17:47") },
            { "v48", new Verse("...Davi se apressou, e correu ao combate, a encontrar-se com o filisteu.", "1 Samuel 17:48") },
            { "v49", new Verse("E Davi pôs a mão no alforje, e tomou dali uma pedra, e com a funda lha atirou, e feriu o filisteu na testa; e caiu sobre o seu rosto em terra.", "1 Samuel 17:49") },
            { "v51", new Verse("...vendo os filisteus que o seu campeão era morto, fugiram.", "1 Samuel 17:51") },
            { "f2v8",   new Verse("Estes são os nomes dos valentes que Davi teve...", "2 Samuel 23:8") },
            { "f2v11a", new Verse("E depois dele Samá, filho de Agé, o hararita; e ajuntaram-se os filisteus em tropa, e havia ali um pedaço de campo cheio de lentilhas...", "2 Samuel 23:11") },
            { "f2v11b", new Verse("...e o povo fugiu de diante dos filisteus.", "2 Samuel 23:11") },
            { "f2v12a", new Verse("Este, porém, se pôs no meio daquele pedaço de campo, e o defendeu, e feriu os filisteus;", "2 Samuel 23:12") },
            { "f2v12b", new Verse("...e o Senhor operou um grande livramento.", "2 Samuel 23:12") },
            { "v46", new Verse("...e toda a terra saberá que há Deus em Israel.", "1 Samuel 17:46") },
        };

        public static Verse Get(string key) { return all[key]; }
    }
}
