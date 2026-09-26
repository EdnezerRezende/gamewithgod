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
            { "f3v1cr",  new Verse("Este esteve com Davi em Pas-Damim, quando os filisteus se ajuntaram ali à peleja...", "1 Crônicas 11:13") },
            { "f3v9",    new Verse("E depois dele Eleazar, filho de Dodô, filho de Aoí, entre os três valentes que estavam com Davi, quando provocaram os filisteus que se ajuntaram ali à peleja, e subiram os homens de Israel.", "2 Samuel 23:9") },
            { "f3v10a",  new Verse("Este se levantou, e feriu os filisteus, até que a sua mão se cansou e ficou pegada à espada;", "2 Samuel 23:10") },
            { "f3vhand", new Verse("...até que a sua mão se cansou e ficou pegada à espada.", "2 Samuel 23:10") },
            { "f3v10b",  new Verse("...e naquele dia o Senhor operou um grande livramento; e o povo voltou após ele somente para despojar.", "2 Samuel 23:10") },
            { "f4v13",  new Verse("Também três dos trinta cabeças desceram, e no tempo da sega vieram a Davi, à caverna de Adulão; e a tropa dos filisteus se acampara no vale de Refaim.", "2 Samuel 23:13") },
            { "f4v14",  new Verse("E Davi estava então no lugar forte, e a guarnição dos filisteus estava então em Belém.", "2 Samuel 23:14") },
            { "f4v15",  new Verse("E teve Davi desejo, e disse: Quem me dera beber da água da cisterna de Belém, que está junto à porta!", "2 Samuel 23:15") },
            { "f4vgate", new Verse("...da água da cisterna de Belém, que está junto à porta.", "2 Samuel 23:15") },
            { "f4v16a", new Verse("Então aqueles três valentes romperam pelo arraial dos filisteus, e tiraram água da cisterna de Belém, que está junto à porta, e a tomaram, e a trouxeram a Davi;", "2 Samuel 23:16") },
            { "f4v16b", new Verse("...porém ele não a quis beber, mas derramou-a perante o Senhor.", "2 Samuel 23:16") },
            { "f4v17",  new Verse("E disse: Guarda-me, ó Senhor, de que tal faça; beberia eu o sangue dos homens que foram a risco da sua vida? De maneira que não a quis beber.", "2 Samuel 23:17") },
            { "f4v17b", new Verse("Isto fizeram aqueles três valentes.", "2 Samuel 23:17") },
            { "f5v20a",  new Verse("Também Benaia, filho de Joiada, filho de um homem valente de Cabzeel, grande em obras; este feriu dois fortes leões de Moabe;", "2 Samuel 23:20") },
            { "f5v20b",  new Verse("...e desceu ele, e feriu um leão no meio de uma cova, no tempo da neve.", "2 Samuel 23:20") },
            { "f5v21",   new Verse("Também este feriu um homem egípcio, homem de grande presença; e o egípcio trazia uma lança na mão, porém ele desceu a ele com um cajado, e arrancou a lança da mão do egípcio, e o matou com a sua própria lança.", "2 Samuel 23:21") },
            { "f5vsnow", new Verse("...no tempo da neve.", "2 Samuel 23:20") },
            { "f5vpit",  new Verse("...e desceu ele, e feriu um leão no meio de uma cova...", "2 Samuel 23:20") },
            { "f5vstaff", new Verse("...porém ele desceu a ele com um cajado...", "2 Samuel 23:21") },
            { "f5v22",   new Verse("Estas coisas fez Benaia, filho de Joiada; e teve nome entre os três valentes.", "2 Samuel 23:22") },
            { "f5v23",   new Verse("Dentre os trinta era ele o mais nobre, porém aos três primeiros não chegou; e Davi o pôs sobre a sua guarda.", "2 Samuel 23:23") },
            { "f6v18a",  new Verse("Também Abisai, irmão de Joabe, filho de Zeruia, era chefe de três;", "2 Samuel 23:18") },
            { "f6v18b",  new Verse("...e este alçou a sua lança contra trezentos, e os feriu; e tinha nome entre os três.", "2 Samuel 23:18") },
            { "f6vlanca", new Verse("...e este alçou a sua lança contra trezentos...", "2 Samuel 23:18") },
            { "f6v2115", new Verse("Tiveram mais os filisteus uma peleja contra Israel; e desceu Davi, e com ele os seus servos; e pelejaram contra os filisteus; e Davi se cansou.", "2 Samuel 21:15") },
            { "f6v2116", new Verse("E Isbi-Benobe, que era dos filhos do gigante, o peso de cuja lança tinha trezentos siclos de cobre, e que cingia uma espada nova, intentou ferir a Davi.", "2 Samuel 21:16") },
            { "f6v2117a", new Verse("Porém Abisai, filho de Zeruia, o socorreu, e feriu o filisteu, e o matou;", "2 Samuel 21:17") },
            { "f6v2117b", new Verse("...então os homens de Davi lhe juraram, dizendo: Nunca mais sairás conosco à peleja, para que não apagues a lâmpada de Israel.", "2 Samuel 21:17") },
            { "f6v19",   new Verse("Porventura este não era o mais nobre dentre os três? Portanto foi o seu chefe; porém aos primeiros três não chegou.", "2 Samuel 23:19") },
            { "f7v8a", new Verse("Estes são os nomes dos valentes que Davi teve: Josebe-Bassebete, o taquemonita, o principal dos capitães;", "2 Samuel 23:8") },
            { "f7v8b", new Verse("...este era Adino, o eznita, que se opôs a oitocentos, e os feriu de uma vez.", "2 Samuel 23:8") },
            { "f7v1cr", new Verse("...o qual, brandindo a sua lança contra trezentos, os feriu de uma vez.", "1 Crônicas 11:11") },
            { "f7rc1", new Verse("E Davi pôs a mão no alforje, e tomou dali uma pedra, e com a funda lha atirou, e feriu o filisteu na testa.", "1 Samuel 17:49") },
            { "f7rc2", new Verse("Este, porém, se pôs no meio daquele pedaço de campo, e o defendeu, e feriu os filisteus.", "2 Samuel 23:12") },
            { "f7rc3", new Verse("Este se levantou, e feriu os filisteus, até que a sua mão se cansou e ficou pegada à espada.", "2 Samuel 23:10") },
            { "f7rc4", new Verse("Isto fizeram aqueles três valentes.", "2 Samuel 23:17") },
            { "f7rc5", new Verse("Estas coisas fez Benaia, filho de Joiada; e teve nome entre os três valentes.", "2 Samuel 23:22") },
            { "f7rc6", new Verse("Também Abisai, irmão de Joabe... alçou a sua lança contra trezentos, e os feriu.", "2 Samuel 23:18") },
            { "f7rc7", new Verse("Josebe-Bassebete, o principal dos capitães... se opôs a oitocentos, e os feriu de uma vez.", "2 Samuel 23:8") },
            { "f7v39", new Verse("...Urias, o heteu; trinta e sete ao todo.", "2 Samuel 23:39") },
            { "v46", new Verse("...e toda a terra saberá que há Deus em Israel.", "1 Samuel 17:46") },
        };

        public static Verse Get(string key) { return all[key]; }
    }
}
