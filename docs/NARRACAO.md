# Narração dos versículos

Os versículos das cenas animadas e das telas são lidos em voz alta.

- **Gravações atuais:** geradas no ElevenLabs (voz "Bruno Zulian Profissional", modelo
  `eleven_multilingual_v2`), uma por chave do roteiro abaixo. Voz masculina, grave e calma, com
  volume normalizado (-16 LUFS) e meio segundo de silêncio nas pontas.
  - Unity: `unity/Assets/ValentesDeDavi/Resources/Narracao/<chave>.ogg`
  - Site: `publicar/web/narracao/<fase><chave>.mp3` (ex.: `f2v11a.mp3`; a fase 1 não tem prefixo).
    Os protótipos em `prototipo/` usam o mesmo caminho relativo `narracao/`, por isso tocam a gravação
    só depois de gerados em `publicar/web` (`sh publicar/gerar-web.sh`).
- **Sem a gravação**, o site cai na voz do próprio navegador (procura uma voz masculina em português,
  tom mais grave e ritmo lento) e a Unity mostra só o texto.

Para trocar a voz ou regravar: gere um áudio por linha da tabela com o texto exato da coluna "Texto",
converta com o `ffmpeg` (abaixo) e substitua os arquivos pelas mesmas chaves.

```sh
# mp3 do ElevenLabs -> ogg (Unity) e mp3 (site), mono 44,1 kHz, normalizado, 0,5 s de silêncio nas pontas
F="loudnorm=I=-16:TP=-1.5:LRA=11,adelay=500,apad=pad_dur=0.5"
ffmpeg -i v49.mp3 -af "$F" -ac 1 -ar 44100 -c:a libvorbis -q:a 4 unity/Assets/ValentesDeDavi/Resources/Narracao/v49.ogg
ffmpeg -i v49.mp3 -af "$F" -ac 1 -ar 44100 -c:a libmp3lame -b:a 64k publicar/web/narracao/v49.mp3
```

## A voz

- Masculina, grave e suave, sem dramatizar: a leitura de quem conta com reverência.
- Ritmo calmo, perto de 2,3 palavras por segundo.
- Nas reticências (...), uma pausa curta; o texto começa ou termina no meio do versículo.
- Sem música nem efeitos na gravação: o jogo abaixa a própria música durante a leitura.

## Formato dos arquivos

- Um arquivo por versículo, com o nome da **chave** da tabela: por exemplo, `v49.ogg`.
- Unity: `.ogg` ou `.wav`, mono, 44,1 kHz, em `Assets/ValentesDeDavi/Resources/Narracao/`. O jogo
  encontra sozinho; a cena espera a leitura terminar antes de passar para o próximo plano.
- Site: `.mp3` mono em `publicar/web/narracao/`. A cena estima a duração pelo número de palavras
  (0,45 s por palavra + 2,5 s), então gravações bem mais lentas que isso podem ser cortadas.

## Como gravar

1. **Você mesmo ou alguém da igreja:** um microfone USB simples num cômodo silencioso já dá bom
   resultado. O Audacity (gratuito) grava e exporta em `.ogg`.
2. **Locutor profissional:** plataformas de locução freelancer cobram por palavra ou por minuto.
3. **Voz gerada por IA:** serviços de voz sintética em português produzem vozes masculinas graves.
   Antes de usar num jogo que será vendido, confirme que o plano contratado permite uso comercial.

Os textos abaixo são provisórios (Almeida em domínio público) e devem ser revisados contra a
edição escolhida; se mudarem, regrave as chaves afetadas.

## Roteiro

| Chave | Referência | Onde aparece | Texto |
|---|---|---|---|
| `v3` | 1 Samuel 17:3 | Cena animada | E os filisteus estavam num monte de um lado, e os israelitas estavam num monte do outro lado; e o vale entre eles. |
| `v4` | 1 Samuel 17:4 | Cena animada | Então saiu do arraial dos filisteus um homem guerreiro, cujo nome era Golias, de Gate, que tinha de altura seis côvados e um palmo. |
| `v10` | 1 Samuel 17:10 | Cena animada | Disse mais o filisteu: Hoje desafio as companhias de Israel, dizendo: Dai-me um homem, para que ambos pelejemos. |
| `v11` | 1 Samuel 17:11 | Cena animada | Ouvindo então Saul e todo o Israel estas palavras do filisteu, espantaram-se, e temeram muito. |
| `v33` | 1 Samuel 17:33 | Cena animada | Porém Saul disse a Davi: Contra este filisteu não poderás ir para pelejar com ele; pois tu ainda és moço. |
| `v34` | 1 Samuel 17:34-35 | Cena animada | Então disse Davi a Saul: Teu servo apascentava as ovelhas de seu pai; e vinha um leão, ou um urso, e tomava uma ovelha do rebanho; e eu saía após ele, e o feria, e livrava-a da sua boca. |
| `v37` | 1 Samuel 17:37 | Telas | O Senhor me livrou da mão do leão, e da do urso; ele me livrará da mão deste filisteu. |
| `v39` | 1 Samuel 17:38-39 | Telas | E Saul vestiu a Davi de suas vestes... Então disse Davi a Saul: Não posso andar com isto, pois nunca o usei. |
| `v40` | 1 Samuel 17:40 | Telas | E tomou o seu cajado na mão, e escolheu para si cinco seixos do ribeiro, e pô-los no alforje de pastor. |
| `v43` | 1 Samuel 17:43 | Cena animada | Disse, pois, o filisteu a Davi: Sou eu algum cão, para tu vires a mim com paus? |
| `v45` | 1 Samuel 17:45 | Cena animada | Davi, porém, disse ao filisteu: Tu vens a mim com espada, e com lança, e com escudo; porém eu venho a ti em nome do Senhor dos Exércitos. |
| `v47` | 1 Samuel 17:47 | Cena animada | ...porque do Senhor é a guerra, e ele vos entregará na nossa mão. |
| `v48` | 1 Samuel 17:48 | Cena animada | ...Davi se apressou, e correu ao combate, a encontrar-se com o filisteu. |
| `v49` | 1 Samuel 17:49 | Cena animada | E Davi pôs a mão no alforje, e tomou dali uma pedra, e com a funda lha atirou, e feriu o filisteu na testa; e caiu sobre o seu rosto em terra. |
| `v51` | 1 Samuel 17:51 | Cena animada | ...vendo os filisteus que o seu campeão era morto, fugiram. |
| `f2v8` | 2 Samuel 23:8 | Cena animada | Estes são os nomes dos valentes que Davi teve... |
| `f2v11a` | 2 Samuel 23:11 | Cena animada | E depois dele Samá, filho de Agé, o hararita; e ajuntaram-se os filisteus em tropa, e havia ali um pedaço de campo cheio de lentilhas... |
| `f2v11b` | 2 Samuel 23:11 | Cena animada | ...e o povo fugiu de diante dos filisteus. |
| `f2v12a` | 2 Samuel 23:12 | Cena animada | Este, porém, se pôs no meio daquele pedaço de campo, e o defendeu, e feriu os filisteus; |
| `f2v12b` | 2 Samuel 23:12 | Cena animada | ...e o Senhor operou um grande livramento. |
| `v46` | 1 Samuel 17:46 | Telas | ...e toda a terra saberá que há Deus em Israel. |
| `f3v1cr` | 1 Crônicas 11:13 | Cena animada | Este esteve com Davi em Pas-Damim, quando os filisteus se ajuntaram ali à peleja... |
| `f3v9` | 2 Samuel 23:9 | Cena animada | E depois dele Eleazar, filho de Dodô, filho de Aoí, entre os três valentes que estavam com Davi, quando provocaram os filisteus que se ajuntaram ali à peleja, e subiram os homens de Israel. |
| `f3v10a` | 2 Samuel 23:10 | Cena animada e telas | Este se levantou, e feriu os filisteus, até que a sua mão se cansou e ficou pegada à espada; |
| `f3vhand` | 2 Samuel 23:10 | Durante a batalha | ...até que a sua mão se cansou e ficou pegada à espada. |
| `f3v10b` | 2 Samuel 23:10 | Cena animada e telas | ...e naquele dia o Senhor operou um grande livramento; e o povo voltou após ele somente para despojar. |
| `f4v13` | 2 Samuel 23:13 | Cena animada e telas | Também três dos trinta cabeças desceram, e no tempo da sega vieram a Davi, à caverna de Adulão; e a tropa dos filisteus se acampara no vale de Refaim. |
| `f4v14` | 2 Samuel 23:14 | Cena animada | E Davi estava então no lugar forte, e a guarnição dos filisteus estava então em Belém. |
| `f4v15` | 2 Samuel 23:15 | Cena animada | E teve Davi desejo, e disse: Quem me dera beber da água da cisterna de Belém, que está junto à porta! |
| `f4vgate` | 2 Samuel 23:15 | Telas | ...da água da cisterna de Belém, que está junto à porta. |
| `f4v16a` | 2 Samuel 23:16 | Cena animada e telas | Então aqueles três valentes romperam pelo arraial dos filisteus, e tiraram água da cisterna de Belém, que está junto à porta, e a tomaram, e a trouxeram a Davi; |
| `f4v16b` | 2 Samuel 23:16 | Cena animada | ...porém ele não a quis beber, mas derramou-a perante o Senhor. |
| `f4v17` | 2 Samuel 23:17 | Cena animada e telas | E disse: Guarda-me, ó Senhor, de que tal faça; beberia eu o sangue dos homens que foram a risco da sua vida? De maneira que não a quis beber. |
| `f4v17b` | 2 Samuel 23:17 | Cena animada | Isto fizeram aqueles três valentes. |
| `f5v20a` | 2 Samuel 23:20 | Cena animada e telas | Também Benaia, filho de Joiada, filho de um homem valente de Cabzeel, grande em obras; este feriu dois fortes leões de Moabe; |
| `f5v20b` | 2 Samuel 23:20 | Cena animada e telas | ...e desceu ele, e feriu um leão no meio de uma cova, no tempo da neve. |
| `f5v21` | 2 Samuel 23:21 | Cena animada e telas | Também este feriu um homem egípcio, homem de grande presença; e o egípcio trazia uma lança na mão, porém ele desceu a ele com um cajado, e arrancou a lança da mão do egípcio, e o matou com a sua própria lança. |
| `f5vsnow` | 2 Samuel 23:20 | Telas | ...no tempo da neve. |
| `f5vpit` | 2 Samuel 23:20 | Telas | ...e desceu ele, e feriu um leão no meio de uma cova... |
| `f5vstaff` | 2 Samuel 23:21 | Telas | ...porém ele desceu a ele com um cajado... |
| `f5v22` | 2 Samuel 23:22 | Cena animada e telas | Estas coisas fez Benaia, filho de Joiada; e teve nome entre os três valentes. |
| `f5v23` | 2 Samuel 23:23 | Cena animada | Dentre os trinta era ele o mais nobre, porém aos três primeiros não chegou; e Davi o pôs sobre a sua guarda. |
| `f6v18a` | 2 Samuel 23:18 | Cena animada e telas | Também Abisai, irmão de Joabe, filho de Zeruia, era chefe de três; |
| `f6v18b` | 2 Samuel 23:18 | Cena animada | ...e este alçou a sua lança contra trezentos, e os feriu; e tinha nome entre os três. |
| `f6vlanca` | 2 Samuel 23:18 | Telas | ...e este alçou a sua lança contra trezentos... |
| `f6v2115` | 2 Samuel 21:15 | Cena animada | Tiveram mais os filisteus uma peleja contra Israel; e desceu Davi, e com ele os seus servos; e pelejaram contra os filisteus; e Davi se cansou. |
| `f6v2116` | 2 Samuel 21:16 | Cena animada | E Isbi-Benobe, que era dos filhos do gigante, o peso de cuja lança tinha trezentos siclos de cobre, e que cingia uma espada nova, intentou ferir a Davi. |
| `f6v2117a` | 2 Samuel 21:17 | Cena animada e telas | Porém Abisai, filho de Zeruia, o socorreu, e feriu o filisteu, e o matou; |
| `f6v2117b` | 2 Samuel 21:17 | Cena animada | ...então os homens de Davi lhe juraram, dizendo: Nunca mais sairás conosco à peleja, para que não apagues a lâmpada de Israel. |
| `f6v19` | 2 Samuel 23:19 | Telas | Porventura este não era o mais nobre dentre os três? Portanto foi o seu chefe; porém aos primeiros três não chegou. |
| `f7v8a` | 2 Samuel 23:8 | Cena animada e telas | Estes são os nomes dos valentes que Davi teve: Josebe-Bassebete, o taquemonita, o principal dos capitães; |
| `f7v8b` | 2 Samuel 23:8 | Cena animada e telas | ...este era Adino, o eznita, que se opôs a oitocentos, e os feriu de uma vez. |
| `f7v1cr` | 1 Crônicas 11:11 | Telas | ...o qual, brandindo a sua lança contra trezentos, os feriu de uma vez. |
| `f7rc0` | 2 Samuel 23:8 | Cena animada | Estes são os nomes dos valentes que Davi teve... |
| `f7rc1` | 2 Samuel 5:12 | Cena animada | E entendeu Davi que o Senhor o confirmara rei sobre Israel, e que exaltara o seu reino por amor do seu povo. |
| `f7rc2` | 2 Samuel 23:12 | Cena animada | Este, porém, se pôs no meio daquele pedaço de campo, e o defendeu, e feriu os filisteus. |
| `f7rc3` | 2 Samuel 23:10 | Cena animada | Este se levantou, e feriu os filisteus, até que a sua mão se cansou e ficou pegada à espada. |
| `f7rc4` | 2 Samuel 23:17 | Cena animada | Isto fizeram aqueles três valentes. |
| `f7rc5` | 2 Samuel 23:22 | Cena animada | Estas coisas fez Benaia, filho de Joiada; e teve nome entre os três valentes. |
| `f7rc6` | 2 Samuel 23:18 | Cena animada | Também Abisai, irmão de Joabe... alçou a sua lança contra trezentos, e os feriu. |
| `f7rc7` | 2 Samuel 23:8 | Cena animada | Josebe-Bassebete, o principal dos capitães... se opôs a oitocentos, e os feriu de uma vez. |
| `f7v39` | 2 Samuel 23:39 | Cena animada e telas | ...Urias, o heteu; trinta e sete ao todo. |
