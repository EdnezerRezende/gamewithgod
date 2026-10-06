# Trocar os bonecos por modelos 3D (Unity)

Os personagens do jogo são montados com primitivas (cilindros e esferas). O código está pronto para
trocá-los por modelos 3D de verdade **sem mudar a lógica do jogo**: basta colocar um prefab com o nome
certo em `Assets/ValentesDeDavi/Resources/Modelos/` e o jogo passa a usá-lo.

Este roteiro usa o **Mixamo** (Adobe), que oferece personagens e animações gratuitos, já com esqueleto.
Vale qualquer modelo humanoide com esqueleto (Asset Store, Blender), desde que importado como **Humanoid**.

## Como o código faz a troca

| Peça | O que acontece com um modelo |
|---|---|
| Corpo de primitivas | É apagado; o prefab entra no lugar (`ModelSkin.Attach`) |
| Pivôs das mãos (onde ficam lança, espada, escudo, tocha, cântaro) | São presos aos ossos das mãos do modelo; as armas seguem a animação |
| Cabeça de Golias (a testa, o alvo, e o anel dourado) | Presa ao osso da cabeça do modelo |
| Andar, parar | O jogo manda a velocidade para o Animator (`Velocidade`) |
| Ataque, queda, levantar | Gatilhos `Ataque`, `Cair`, `Levantar` no Animator |
| Balanço de pernas e braços por código | Continua acontecendo em pivôs vazios, sem efeito visual |

Arquivos: `Scripts/World/ModelSkin.cs` (tempo de jogo) e `Editor/ModelosSetup.cs` (menu do editor).

## Nomes que o jogo procura

| Nome do prefab | Quem usa |
|---|---|
| `Filisteu` | Todos os filisteus comuns (fases 2 a 7): lanceiros, escudeiros, arqueiros, sentinelas, fileiras |
| `Israelita` | Soldados e aliados de Israel, instrutores das fases 4 a 7, os servos de Davi |
| `Golias` | O gigante da fase 1 |
| `Escudeiro` | O escudeiro de Golias |
| `Instrutor` | O instrutor das fases 2 e 3 |
| Nome do personagem (`Davi`, `Davi, o rei`, `Samá`, `Eleazar`, `Benaia`, `Abisai`, `Josebe-Bassebete`) | Opcional: se existir, aquele personagem usa o modelo dele; senão, cai em `Israelita` |

Com os quatro primeiros quase todos os bonecos já ficam trocados. Davi em primeira pessoa não precisa
de modelo (só as mãos, que continuam como estão).

## Roteiro

### 1. Baixar do Mixamo

1. Entre em mixamo.com com uma conta Adobe (gratuita).
2. **Personagens:** escolha um para cada nome da tabela. Sugestão: um soldado para `Filisteu`, outro
   para `Israelita`, um grandalhão para `Golias` (o jogo aumenta a escala) e um homem comum para
   `Escudeiro`. Baixe cada um com *Format: FBX for Unity*, *Pose: T-pose*.
3. **Animações** (baixe uma vez, com *Skin: Without Skin*, *Format: FBX for Unity*), e dê a elas
   estes nomes de arquivo:
   - `Parado` (uma Idle),
   - `Andar` (Walking),
   - `Correr` (Running),
   - `Ataque` (um golpe de lança ou espada, por exemplo *Stab* ou *Slash*),
   - `Cair` (uma Death ou Falling),
   - `Levantar` (Getting Up).
   Marque *In Place* quando a animação oferecer essa opção, para o modelo não sair andando sozinho.

### 2. Importar na Unity

1. Menu **Valentes de Davi → Modelos → 1. Criar pastas dos modelos**.
2. Arraste os personagens para `Assets/ValentesDeDavi/Modelos/<Nome>/` e as animações para
   `Assets/ValentesDeDavi/Modelos/Animacoes/`.
3. Para **cada arquivo FBX** (personagens e animações): selecione, aba **Rig**, *Animation Type* =
   **Humanoid**, *Avatar Definition* = *Create From This Model*, **Apply**.
4. Nas animações, aba **Animation**: marque *Loop Time* em Parado, Andar e Correr; em *Root Transform
   Position (XZ)* marque *Bake Into Pose* para a animação não deslocar o modelo. **Apply**.
5. Se o modelo vier sem texturas (roxo ou cinza): selecione o FBX, aba **Materials**, *Extract
   Textures* e *Extract Materials*. No URP, se ficar rosa, selecione os materiais extraídos e use
   **Edit → Rendering → Materials → Convert Selected Built-in Materials to URP**.

### 3. Criar o controlador de animação

Menu **Valentes de Davi → Modelos → 2. Criar ou atualizar o controlador de animação**.

Ele cria `Generated/Humanoide.controller` com os parâmetros que o jogo usa e procura os clipes pelos
nomes acima na pasta `Animacoes`. O Console diz quais clipes faltaram; coloque-os e rode de novo.

### 4. Criar o prefab de cada modelo

1. Na janela Project, selecione o FBX do personagem.
2. Menu **Valentes de Davi → Modelos → 3. Preparar o modelo selecionado como prefab do jogo**.
3. Digite o nome do jogo (`Filisteu`, `Golias`...). O prefab vai para `Resources/Modelos/<Nome>.prefab`
   já com o Animator, o controlador e o componente **ModelSkin**.
4. **Valentes de Davi → Modelos → Conferir quais modelos existem** mostra o que já está trocado.

### 5. Ajustar no prefab (componente ModelSkin)

Abra o prefab e confira no Play:

| Campo | Para quê |
|---|---|
| `Scale` | Altura. O filisteu de primitivas tem 1,75 m; Golias, 3,4 m × 1,45 (o jogo multiplica). Se o modelo do Mixamo vier em centímetros, o *Scale Factor* na aba Model do FBX resolve (0,01) |
| `Hand Offset R / Hand Euler R` | Posição e giro da arma na mão direita. Ajuste até a lança ficar na palma, apontando para a frente |
| `Hand Offset L / Hand Euler L` | O mesmo para escudo, tocha e cântaro na mão esquerda |
| `Head Offset / Head Euler` | Só em Golias: posição da testa (o alvo) sobre o rosto do modelo. Com a abertura ativa, o anel dourado mostra onde ela está |

Dica: no Play, selecione o boneco na Hierarchy e mexa nos valores do ModelSkin até encaixar; depois
copie os valores para o prefab (o Play não salva).

### 6. Conferir no jogo

- **Fase 1:** a testa de Golias brilha na abertura e a pedra que a acerta vence. As áreas de armadura
  (couraça, cintura, pernas) continuam nas alturas do boneco antigo, então o modelo deve ter altura
  parecida (uns 4,8 m já com a escala do jogo).
- **Fases 2 a 7:** os filisteus andam (Velocidade), atacam (Ataque) e caem (Cair) com as animações.
  Sem modelo, tudo continua com as primitivas, inclusive se só alguns nomes tiverem prefab.

## Licença

Os personagens e animações do Mixamo podem ser usados em jogos, inclusive comerciais, segundo os
termos da Adobe. Conferir os termos vigentes antes de publicar e guardar a lista do que foi usado.

## Limites conhecidos

- O golpe de braço feito por código (`armR.localRotation`) continua girando o pivô preso à mão, então a
  arma gira junto com a animação de ataque. Se incomodar, o jogo pode parar de girar o pivô quando há
  modelo (`fig.Animated`).
- As áreas de acerto (HitZone) de Golias são esferas nas alturas do boneco antigo; um modelo muito
  diferente pode precisar de ajuste em `Duel.BuildHitZones`.
- Os exércitos ao fundo (`Army.cs`, centenas de soldados por instancing) continuam de primitivas de
  propósito: são muitos e ficam longe.
