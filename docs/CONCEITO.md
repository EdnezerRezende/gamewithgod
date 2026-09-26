# Os Valentes de Davi — Documento de Conceito

> Registro das decisões de conceito. Ainda não é o design detalhado.

## Visão

Jogo de ação em **primeira pessoa**, com progressão leve de RPG, baseado nos
guerreiros de elite de Davi (2 Samuel 23; 1 Crônicas 11). Cada fase é o relato
bíblico de um valente, jogado de forma fiel ao texto.

## Definições

| Item | Decisão |
|---|---|
| Plataforma | PC |
| Motor | Unity (URP) |
| Público | Jovem (combate intenso, sem violência explícita/gore) |
| Modo | Um jogador (campanha) |
| Equipe | Desenvolvedor solo |
| Armas | Funda, arco, lança, espada, escudo |
| Cenas | Animadas no próprio motor (Timeline + Cinemachine) |
| Animação de personagens | Mixamo + captura de movimento via celular |
| Texto bíblico | Almeida em edição de domínio público |
| Dificuldade | Selecionável: Pastor, Guerreiro, Valente |

## Estrutura de cada fase

1. **Abertura:** cena animada com o trecho bíblico (curto na tela, passagem
   completa no "Livro"; pode ser pulada ao rejogar). O texto define o objetivo
   da fase.
2. **Treino:** pratica a habilidade que a fase vai exigir (ex.: tiro ao alvo
   com funda). Rende pontos e medalhas (bronze/prata/ouro) e uma pequena
   vantagem na missão. Funciona também como tutorial.
3. **Missão:** ação em primeira pessoa com objetivo tirado do relato.
4. **Resultado:** pontuação (treino + missão + bônus de **Fidelidade ao
   relato**) e versículo de fechamento com a lição da fase.

## Fases candidatas

| Valente | Passagem | Tipo de jogabilidade |
|---|---|---|
| Davi × Golias | 1 Sm 17 | Precisão com funda / chefão |
| Samá | 2 Sm 23:11-12 | Defender a posição no campo |
| Eleazar | 2 Sm 23:9-10 | Combate de espada contra ondas de inimigos |
| Benaia | 2 Sm 23:20 | Sobrevivência contra o leão no poço |
| Os três valentes | 2 Sm 23:13-17 | Romper pelo arraial em grupo e trazer a água de Belém |
| Abisai | 2 Sm 23:18-19; 21:15-17 | A lança contra trezentos e o socorro a Davi contra o gigante |
| Josebe-Bassebete (fase final) | 2 Sm 23:8 | Um contra oitocentos no desfiladeiro; encerra com a lista dos valentes |

## Regras de conteúdo

- Deus e Jesus nunca são personagens jogáveis.
- A fé é mecânica (oração, obediência, confiança), não "magia".
- Missões respeitam o que o texto descreve.

## Primeiro alvo: fatia vertical

Uma fase completa de **Davi × Golias** (abertura animada, treino de funda,
missão e resultado). Serve como prova de conceito, trailer e material para
buscar apoio.
