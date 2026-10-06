# Projeto Unity: como abrir e jogar

O projeto Unity fica em `unity/` e o código do jogo em `unity/Assets/ValentesDeDavi/`. Ele monta a Fase 1 (Davi × Golias)
inteira por código, com modelos provisórios feitos de formas simples, para testar a jogabilidade
enquanto a arte definitiva não existe.

## Primeira vez

A pasta `unity/` **é o projeto Unity completo** (`Assets`, `Packages/manifest.json` e
`ProjectSettings`). Não precisa criar projeto no Hub nem copiar pastas: basta abrir.

### Com o conector do Unity (CLI `unity`)

```bash
unity --version || curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
unity auth status && unity license status      # se faltar: unity auth login / unity license activate
unity install lts --yes --accept-eula          # Unity 6 LTS (uma vez; demora alguns minutos)
sh unity/abrir.sh                              # abre o projeto com o editor LTS instalado
```

Com o editor aberto, o conector controla a cena ao vivo:

```bash
(cd unity && unity pipeline install)          # uma vez: pacote com.unity.pipeline
unity status                                   # estado "ready" = editor conectado
unity command                                  # comandos que o editor expõe
unity command editor_play                      # entra em Play
```

### Pelo Unity Hub

**Add → Add project from disk** e escolha a pasta `unity/` do repositório. Se o Hub pedir uma
versão que não está instalada, troque pelo Unity 6 instalado: o projeto não depende de uma
versão exata.

### O que acontece ao abrir

1. O Package Manager baixa o URP e o Input System (`Packages/manifest.json`).
2. O menu **Valentes de Davi** configura o URP sozinho (`Generated/UrpPipeline.asset`), cria as
   cenas das sete fases em `Assets/ValentesDeDavi/Scenes/` e abre a Fase 1.
3. Aperte **Play**. Clique na janela Game para o mouse ser capturado.

Se a cena não abrir sozinha, use o menu **Valentes de Davi → Criar ou atualizar a cena da Fase 1**.
Se os materiais ficarem rosa, use **Valentes de Davi → Configurar o URP**.

Na primeira abertura a Unity gera os arquivos `.meta`; faça um commit com eles junto dos
arquivos a que pertencem. `Library/`, `Temp/`, `Logs/` e afins já estão no `.gitignore`.

A **Fase 2** (Samá e o campo de lentilhas), a **Fase 3** (Eleazar e a mão pegada à espada) a **Fase 4** (os três valentes e a água de Belém) a **Fase 5** (Benaia: o leão na cova e o egípcio) a **Fase 6** (Abisai: a lança contra trezentos) e a **Fase 7**, a final (Josebe-Bassebete: oitocentos de uma vez), têm cenas próprias: `Fase2_Sama.unity`, `Fase3_Eleazar.unity`, `Fase4_Agua.unity`, `Fase5_Benaia.unity`, `Fase6_Abisai.unity` e `Fase7_Josebe.unity`. Todas ficam nas Build Settings, em ordem. As fases 4 (noite) e 5 (neve) mudam o céu e usam materiais próprios (`Generated/CeuNoite.mat` e `Generated/CeuNeve.mat`).

### Mapa das fases e trava

O menu de cada fase mostra todas as fases em cartões (atual, liberada, concluída com estrelas e
recorde, ou trancada). A fase seguinte só abre depois de vencer a anterior; vencer é chegar à tela
de resultados. O progresso fica no `PlayerPrefs` (`Scripts/Core/Progress.cs`).

Para testar sem vencer tudo, use o menu **Valentes de Davi → Progresso → Liberar todas as fases
(testes)**. **Apagar o progresso** tranca tudo de novo.

## Controles

| Tecla | Ação |
|---|---|
| Mouse | Olhar e mirar |
| Segurar botão esquerdo | Girar a funda; soltar na faixa dourada é o tiro perfeito |
| W A S D · Shift | Andar · correr |
| Setas | Olhar sem mouse |
| Esc ou P | Pausa (continuar, recomeçar, voltar ao menu) |
| Enter | Pular a cena animada |
| H | Ajuda de mira: trajetória, marcador de impacto e mira dourada sobre alvos |
| M | Ligar ou desligar a música |
| N | Ligar ou desligar a narração |
| Clique durante a cena | Avançar o plano |

No celular ou tablet (e no navegador do celular), aparecem controles na tela: direcional à
esquerda para andar (empurrar até a borda corre), arrastar na metade direita para olhar, botão
**Funda** (segurar para girar, soltar para atirar) e botão **❚❚** de pausa. Tocar avança a cena
animada. Eles ficam em `Scripts/UI/TouchControls.cs`.

### Controles da Fase 3

| Tecla | Ação |
|---|---|
| Clique esquerdo | Golpe; três rápidos seguidos formam a sequência |
| Segurar e soltar | Golpe forte (abre a falange) |
| Botão direito | Aparar no instante do golpe inimigo (o próximo golpe é crítico) |
| Espaço | Desviar |
| F (segurar) · T | Orar · tocar a trombeta |

No toque, os botões viram **Golpe**, **Aparar**, **Desviar**, **Orar** e **Trombeta**.

### Controles da Fase 4

| Tecla | Ação |
|---|---|
| Clique esquerdo | Golpe; três rápidos formam a sequência; segurar e soltar é o golpe forte |
| Botão direito | Segurar: escudo · apertar no instante do golpe: aparar (com o cântaro, só aparar) |
| Espaço | Desviar (com o cântaro, derrama um pouco) |
| Q | Ordem aos companheiros: "Comigo" ou "Segurem aqui" |
| E (segurar) | Ação: pegar o cântaro, tirar água, levantar um companheiro caído |
| F (segurar) | Orar |

No toque, os botões são **Golpe**, **Escudo/Aparar**, **Desviar**, **Ordem**, **Orar** e **Ação**.

### Controles da Fase 5

| Tecla | Ação |
|---|---|
| Clique esquerdo | Golpe com a arma da vez (espada, cajado ou lança); segurar e soltar é o golpe forte |
| Botão direito | Segurar: escudo (com a espada) · apertar no instante do golpe: aparar |
| Espaço | Desviar (o bote do leão, a varredura do egípcio) |
| E (segurar) | Ação: arrancar a lança do egípcio desequilibrado; esperar a neve passar, atirar pedras, pegar a espada |
| F (segurar) | Orar |

### Controles da Fase 6

| Tecla | Ação |
|---|---|
| Clique esquerdo | Estocada da lança (atravessa até dois); segurar e soltar é a varredura, que acerta todos em volta |
| Botão direito | Aparar no instante do golpe; perto do gigante e de frente para ele, apara também o golpe que ia para Davi |
| Espaço | Desviar |
| Q | Trocar entre a lança e a espada da cintura |
| F (segurar) | Orar |

### Controles da Fase 7

| Tecla | Ação |
|---|---|
| Clique esquerdo | Estocada da lança; segurar e soltar é a varredura, que derruba a fileira e os escudeiros |
| Botão direito | Aparar no instante do golpe (também as flechas dos arqueiros nas encostas) |
| Espaço | Desviar |
| Q | Trocar entre a lança e a espada da cintura |
| F (segurar) | Orar |

## Como o código está organizado

| Pasta | O que tem |
|---|---|
| `Scripts/Core` | Dificuldade, versículos, entrada (Input System novo e antigo), sons sintetizados, materiais |
| `Scripts/World` | Vale de Elá, campos de Belém, céu e luz, exércitos, modelos provisórios |
| `Scripts/World/ModelSkin.cs` | Troca de um boneco de primitivas por um modelo 3D importado (prefab em `Resources/Modelos/<nome>`), com as mãos, a cabeça e as animações ligadas |
| `Scripts/Player` | Davi em primeira pessoa (`PlayerController`) e a funda (`Sling`) |
| `Scripts/Combat` | Pedra, regiões de acerto (`HitZone`) e estilhaços |
| `Scripts/Training` | Treino: jarros parados, jarros balançando e leões |
| `Scripts/Duel` | Golias, escudeiro, dardo e as regras do duelo |
| `Scripts/Flow` | Fluxo da fase (`Game`) e cenas animadas (`Cutscene`) |
| `Scripts/UI` | Interface feita com UI Toolkit: HUD, telas e desenho das pedras |
| `Scripts/Fase2` | Fase 2: campo de lentilhas e fogo, espada e escudo, filisteus, flechas, treino, batalha e o fluxo da fase |
| `Scripts/Fase3` | Fase 3: vale de Pas-Damim, espada de Eleazar (sequência, aparar, desviar, Cansaço e mão pegada), filisteus em linhas, treino, batalha com estandartes e trombeta, e o fluxo da fase |
| `Scripts/Fase4` | Fase 4: vale de Refaim à noite, as mãos do valente (espada, escudo, cântaro), filisteus do arraial com sentinelas e alarme, companheiros com ordens, treino, missão e o fluxo da fase |
| `Scripts/Fase5` | Fase 5: aldeia na neve e planície, neve caindo, as armas de Benaia (espada e escudo, cajado, lança), o leão da cova, o egípcio, treino e o fluxo da fase |
| `Scripts/Fase6` | Fase 6: os campos de batalha, a multidão dos trezentos, Isbi-Benobe, Davi cansado, a regra de alcance da lança, treino e o fluxo da fase (as mãos de Abisai reaproveitam as de Benaia) |
| `Scripts/Fase7` | Fase 7 (final): o desfiladeiro com a pedra do capitão, as fileiras com escudeiros, os arqueiros nas encostas, o treino, a cena final com Davi no trono e os valentes ao lado dele, e a galeria de todas as fases (as mãos do capitão reaproveitam as de Benaia) |
| `Editor` | Configura o URP e cria as cenas das fases e os materiais automaticamente |

Os números de balanceamento (tempo da testa exposta, faixa dourada, dano, velocidades) ficam em
`Scripts/Core/Difficulty.cs`, espelhando a tabela de dificuldade de `docs/fases/01-davi-golias.md`.

## O que é provisório

- **Modelos:** personagens e animais feitos com cilindros, esferas e cubos. Cada modelo em
  `Models.cs` expõe as mesmas articulações (cabeça, braços, pernas) que um modelo definitivo
  precisará ter, para a troca ser direta.
- **Cenas animadas:** planos de câmera por código (`Cutscene.cs`). Serão refeitas com Timeline e
  Cinemachine.
- **Narração:** a Unity toca gravações dos versículos (roteiro e formato em `docs/NARRACAO.md`); sem gravação, a cena mostra só o texto.
- **Tutorial:** "Como jogar" aparece antes da primeira partida completa e fica no menu (`Scripts/UI/Tutorial.cs`).
- **Sons e música:** sintetizados ao iniciar o jogo (`Sfx.cs` e `Music.cs`), até termos áudio gravado, trilha e narração.
- **Versículos:** texto provisório, a revisar contra a edição da Almeida escolhida.

## Próximos passos sugeridos

1. Trocar os bonecos por modelos 3D (Mixamo ou Asset Store): o código já aceita a troca, ver `docs/MODELOS.md` e o menu **Valentes de Davi → Modelos**.
2. Refazer a abertura no Timeline + Cinemachine.
3. Acampamento de Israel e a conversa com Saul (ainda não existem).
4. Etapa do urso no treino.
