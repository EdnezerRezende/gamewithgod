# Projeto Unity: como abrir e jogar

O código do jogo fica em `unity/Assets/ValentesDeDavi/`. Ele monta a Fase 1 (Davi × Golias)
inteira por código, com modelos provisórios feitos de formas simples, para testar a jogabilidade
enquanto a arte definitiva não existe.

## Primeira vez

1. Instale o **Unity Hub** e o **Unity 6** (versão LTS mais recente).
2. No Unity Hub, clique em **New project** e escolha o modelo **Universal 3D** (URP).
   Dê o nome `ValentesDeDavi` e crie.
3. Feche a Unity. Copie a pasta `unity/Assets/ValentesDeDavi` deste repositório para dentro da
   pasta `Assets` do projeto criado.
4. Abra o projeto de novo. Na primeira importação, o menu **Valentes de Davi** cria a cena
   `Assets/ValentesDeDavi/Scenes/Fase1_DaviGolias.unity` e a abre sozinho.
5. Aperte **Play**. Clique na janela Game para o mouse ser capturado.

Se a cena não abrir sozinha, use o menu **Valentes de Davi → Criar ou atualizar a cena da Fase 1**.

A **Fase 2** (Samá e o campo de lentilhas) tem cena própria, criada junto: `Assets/ValentesDeDavi/Scenes/Fase2_Sama.unity`. O menu de cada fase tem um botão para ir à outra; as duas ficam nas Build Settings.

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
| Clique durante a cena | Avançar o plano |

No celular ou tablet (e no navegador do celular), aparecem controles na tela: direcional à
esquerda para andar (empurrar até a borda corre), arrastar na metade direita para olhar, botão
**Funda** (segurar para girar, soltar para atirar) e botão **❚❚** de pausa. Tocar avança a cena
animada. Eles ficam em `Scripts/UI/TouchControls.cs`.

## Como o código está organizado

| Pasta | O que tem |
|---|---|
| `Scripts/Core` | Dificuldade, versículos, entrada (Input System novo e antigo), sons sintetizados, materiais |
| `Scripts/World` | Vale de Elá, campos de Belém, céu e luz, exércitos, modelos provisórios |
| `Scripts/Player` | Davi em primeira pessoa (`PlayerController`) e a funda (`Sling`) |
| `Scripts/Combat` | Pedra, regiões de acerto (`HitZone`) e estilhaços |
| `Scripts/Training` | Treino: jarros parados, jarros balançando e leões |
| `Scripts/Duel` | Golias, escudeiro, dardo e as regras do duelo |
| `Scripts/Flow` | Fluxo da fase (`Game`) e cenas animadas (`Cutscene`) |
| `Scripts/UI` | Interface feita com UI Toolkit: HUD, telas e desenho das pedras |
| `Scripts/Fase2` | Fase 2: campo de lentilhas e fogo, espada e escudo, filisteus, flechas, treino, batalha e o fluxo da fase |
| `Editor` | Cria as cenas das duas fases e os materiais automaticamente |

Os números de balanceamento (tempo da testa exposta, faixa dourada, dano, velocidades) ficam em
`Scripts/Core/Difficulty.cs`, espelhando a tabela de dificuldade de `docs/fases/01-davi-golias.md`.

## O que é provisório

- **Modelos:** personagens e animais feitos com cilindros, esferas e cubos. Cada modelo em
  `Models.cs` expõe as mesmas articulações (cabeça, braços, pernas) que um modelo definitivo
  precisará ter, para a troca ser direta.
- **Cenas animadas:** planos de câmera por código (`Cutscene.cs`). Serão refeitas com Timeline e
  Cinemachine.
- **Sons e música:** sintetizados ao iniciar o jogo (`Sfx.cs` e `Music.cs`), até termos áudio gravado, trilha e narração.
- **Versículos:** texto provisório, a revisar contra a edição da Almeida escolhida.

## Próximos passos sugeridos

1. Trocar Golias, Davi e o escudeiro por modelos da Asset Store com animações do Mixamo.
2. Refazer a abertura no Timeline + Cinemachine.
3. Acampamento de Israel e a conversa com Saul (ainda não existem).
4. Etapa do urso no treino.
