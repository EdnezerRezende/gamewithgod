# Checklist para publicar na Steam

Legenda: ✅ já temos · 🟡 parcial · ⬜ falta. Valores e regras da Steam mudam: confira no
[Steamworks](https://partner.steamgames.com/doc/gettingstarted) antes de pagar ou enviar.

## Visão geral e ordem sugerida

1. **Agora:** deixar o jogo jogável de ponta a ponta no editor e arrumar o que a Steam e os jogadores esperam (seção 2).
2. **Em paralelo:** conta no Steamworks, taxa e página da loja ("Em breve"). A página precisa estar no ar
   **pelo menos 2 semanas** antes do lançamento e juntar desejos na lista (wishlist).
3. **Antes de pedir revisão:** modelos e artes finais (seção 4), build de teste e revisão da página e do build.
4. **Lançamento:** escolher a data, aviso aos seguidores, acompanhar os números (`docs/ANALYTICS.md`).

Dica: o **itch.io** (já documentado em `publicar/ITCH.md`) é um bom primeiro passo: dá para soltar uma demo
das fases 1 e 2 e ouvir os jogadores enquanto a página da Steam junta desejos.

## 1. Conta e taxas na Steam

- [ ] Conta no **Steamworks** (partner.steamgames.com), com a identidade da pessoa ou da empresa que vai receber.
- [ ] **Dados fiscais e bancários** (a Steam pede entrevista de impostos e conta para pagamento; para quem é de
      fora dos EUA, o formulário W-8BEN). Pessoa física costuma ser possível; confirme com um contador o que
      declarar no Brasil.
- [ ] **Taxa de US$ 100 por jogo** (Steam Direct), que a Steam devolve quando o jogo passa de US$ 1.000 de vendas.
- [ ] Decidir **preço** (ou gratuito) e os **preços regionais** (a Steam sugere por região).
- [ ] Decidir quem é a **editora/desenvolvedora** exibida na loja (hoje o projeto usa `GameWithGod`).

## 2. O jogo em si

### Já temos
- ✅ 7 fases jogáveis no Unity (código), menu, mapa de fases com trava, três dificuldades, pausa, tutorial.
- ✅ Narração gravada, música e sons sintetizados, versículos nas cenas.
- ✅ Progresso salvo localmente (PlayerPrefs).

### Falta para o PC
- [ ] **Rodar tudo no editor.** O código das fases 2 a 7, do acampamento e da troca de modelos só foi compilado;
      ninguém abriu as cenas ainda. Esta é a prioridade: jogar cada fase até o fim e corrigir o que aparecer.
- [ ] **Equilíbrio de dificuldade** jogado de verdade (contadores 300/800, alarme, vida do leão e do urso).
- [x] **Botão "Sair do jogo"** nos menus e na pausa (escrito; falta jogar no editor).
- [x] **Opções** (menu e pausa): volume de música, efeitos e narração, sensibilidade, olhar vertical invertido,
      ajuda de mira, tela cheia/janela e qualidade gráfica (escrito; falta jogar no editor). Também tem resolução e sincronia vertical.
- [ ] **Teclas configuráveis** ou, pelo menos, tela de controles clara.
- [x] **Controle (gamepad):** analógicos andam e olham, RT/RB golpe ou funda, LT/LB escudo, A desviar, X trocar,
      Y orar, B ação, Start pausa, menus navegáveis (escrito para o Input System; falta testar com um controle de
      verdade). A interface mostra os ícones dos botões do controle (A, B, X, Y, gatilhos, analógicos, D-pad, Start) quando o jogador usa o controle; para o selo "Verificado" do Steam Deck ainda falta testar no aparelho e conferir o tamanho do texto na tela de 7 polegadas.
- [ ] **Idiomas:** português já; inglês amplia muito o público (a Steam deixa marcar os idiomas da interface,
      do áudio e das legendas). O texto bíblico em inglês precisa de uma edição de domínio público.
- [ ] **Salvar e continuar:** conferir progresso, recordes e dificuldade escolhida ao fechar e abrir o jogo.
- [ ] **Desempenho:** 60 quadros por segundo em um computador modesto e no Steam Deck; testar em hardware real.
- [ ] **Relatório de erros** (log em arquivo e, se quiser, um serviço de crash).
- [ ] **Testadores externos:** pelo menos 10 pessoas jogando a versão de build, não do editor.
- [ ] **Tela de créditos** e licenças de terceiros.
- [ ] Sem texto provisório: os versículos ("provisórios") conferidos contra a edição escolhida (seção 5).

### Integração Steam (opcional no início, mas esperada)
- [ ] **Steamworks SDK** no Unity (Steamworks.NET ou Facepunch.Steamworks): iniciar a Steam, nome do jogador.
- [ ] **Conquistas** (achievements): por exemplo, vencer cada fase, as 3 estrelas, "De uma vez", "Oitocentos".
      Combinam com os itens de "Fidelidade ao relato".
- [ ] **Steam Cloud** para o progresso (ou salvar em arquivo para a nuvem funcionar).
- [ ] **Overlay** da Steam funcionando (Shift+Tab) e jogo que pausa sozinho quando a janela perde o foco.

## 3. Build

- [ ] Fechar a versão do Unity (hoje 6000.3.0f1) e não trocar de versão perto do lançamento.
- [ ] **Windows 64 bits** (principal). Linux (Steam Deck roda via Proton) e macOS são opcionais.
- [ ] Revisar o *Player Settings*: nome, empresa, versão (hoje 0.1), **ícone do aplicativo**, resolução padrão,
      cursor travado, tela de abertura (*splash*).
- [ ] Build de **release** (não de desenvolvimento), com o *Scripting Backend* IL2CPP se for preciso desempenho.
- [ ] Enviar o build com o **SteamPipe** (`steamcmd` e os arquivos de configuração de *depot*), primeiro para um
      *branch* de teste e só depois para o *default*.
- [ ] Pasta do jogo **sem** arquivos de desenvolvimento (`_DoNotShip`, símbolos).
- [ ] Antivírus: testar o executável e, se possível, assinar o código.

## 4. Página da loja (Steamworks)

- [ ] **Nome** e **descrição curta** (até 300 caracteres) e descrição longa com as imagens.
- [ ] **Imagens da cápsula** (tamanhos exigidos pela Steam): cabeçalho 920×430, cápsula pequena 462×174, cápsula
      principal 1232×706, vertical 748×896, e as da biblioteca (600×900, herói 3840×1240 e logo).
- [ ] **Capturas de tela** (no mínimo 5, 1920×1080), **sem** texto de "provisório".
- [ ] **Vídeo de apresentação** (trailer de 30 a 60 segundos, com a jogabilidade nos primeiros 10).
- [ ] **Tags e gênero** (ação, primeira pessoa, bíblico, aventura), idioma, controles e requisitos mínimos.
- [ ] **Questionário de classificação etária** (a Steam usa o IARC) e declarações de conteúdo (violência: fantasia,
      sem sangue). Marcar com cuidado: o público jovem não precisa ser "para crianças" se o conteúdo é de ação.
- [ ] **Página "Em breve"** publicada e pedindo desejos; **3 a 5 dias úteis** para a Steam revisar a página e o build.
- [ ] **Política de privacidade** (URL) se o jogo ou o site coletar dados (o site usa Plausible, sem cookies).
- [ ] Conta de **suporte**: e-mail e, se quiser, uma comunidade (Discord ou o fórum da Steam).
- [ ] Planejar a data e o **Steam Next Fest** (festival de demos): boa chance de visibilidade com uma demo.

## 5. Direitos e licenças (muito importante)

- [ ] **Texto bíblico:** o jogo usa uma edição da Almeida "de domínio público" (provisória). Confirmar a edição
      exata e a fonte (por exemplo, a Almeida Revista e Corrigida de 1898 ou outra anterior ao copyright),
      registrar em `docs/` e revisar todas as citações. Edições modernas (Revista e Atualizada, Corrigida Fiel,
      NVI etc.) têm direitos autorais e exigem licença.
- [ ] **Narração (ElevenLabs):** o uso comercial das vozes depende do **plano pago** da conta e da licença da
      voz escolhida. Conferir os termos e guardar o comprovante. A conta já apresentou um aviso de "Free Tier
      desativado"; regularizar o plano antes de lançar.
- [ ] **Modelos e animações (Mixamo/Asset Store):** o Mixamo permite uso em jogos comerciais, mas não deixa
      redistribuir os arquivos soltos; guardar a lista do que foi usado e as licenças de cada pacote pago.
- [ ] **Fontes:** o jogo usa a fonte padrão do Unity; se trocar, conferir a licença (as do Google Fonts são livres).
- [ ] **Música e sons:** hoje são sintetizados por código (originais). Se contratar música, exigir licença para jogos.
- [ ] **Unity:** a versão gratuita (Personal) vale até um certo faturamento anual e pode exigir a tela de abertura;
      conferir o limite vigente.
- [ ] **Nome e marca:** pesquisar "Os Valentes de Davi" na Steam, no INPI e na internet para evitar conflito.
- [ ] **Conteúdo religioso:** a Steam aceita; evitar o que possa parecer proselitismo para crianças sem aviso e
      declarar o conteúdo corretamente na classificação. Deus e Jesus não são personagens jogáveis (já é regra
      do projeto).
- [ ] **LGPD/COPPA:** não coletar dados de menores. O site só mede visitas de forma anônima. Se o jogo ganhar
      estatísticas, declarar na política de privacidade.
- [ ] Arquivo `LICENSE` ou aviso de direitos no repositório (hoje não há).

## 6. Antes de apertar "Lançar"

- [ ] Build final enviado e **aprovado** pela revisão da Steam; testar a instalação limpa a partir da Steam.
- [ ] Jogar de ponta a ponta no build de loja, em uma conta sem permissão especial.
- [ ] **Chaves** para quem testou, imprensa e criadores de conteúdo (opcional).
- [ ] Preparar a **primeira atualização** (ajustes com base nos relatos) e o canal de feedback.
- [ ] Combinar o que fazer com os reembolsos: a Steam reembolsa quem jogou menos de 2 horas; o jogo precisa
      passar uma boa impressão logo nos primeiros minutos (a fase 1 e o treino).

## Onde fica cada coisa neste repositório

| Preciso de... | Onde está |
|---|---|
| Código e cenas do jogo | `unity/Assets/ValentesDeDavi/` (`docs/UNITY.md`) |
| Troca dos bonecos por modelos 3D | `docs/MODELOS.md` |
| Narração | `docs/NARRACAO.md` |
| Estatísticas do site | `docs/ANALYTICS.md` |
| Demo no navegador (itch.io) | `publicar/ITCH.md`, `publicar/web/` |

## Primeiros três passos, se quiser começar hoje

1. Abrir o projeto no Unity e jogar as fases de ponta a ponta (me mande o que der erro; eu corrijo).
2. Criar a conta no Steamworks e preencher os dados fiscais (leva dias para ser aprovado).
3. Escolher a edição da Almeida e o plano pago de narração, para o conteúdo estar liberado.
