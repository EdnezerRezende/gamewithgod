# Estatísticas do jogo (Plausible)

O site publicado conta visitas e partidas de forma **anônima**: o Plausible não usa cookies nem guarda
dados pessoais (nem IP), então não precisa de aviso de consentimento. O painel fica em uma página à parte,
protegida por usuário e senha: **`https://<endereço do site>/analytics`**.

## O que é contado

| O quê | Como aparece no Plausible |
|---|---|
| Visitas por página (`/`, `/fase2.html`... `/fase7.html`) | Pages |
| **Jogadores agora** | o número "current visitors" do painel (tempo real) |
| Jogo iniciado | evento `Jogo iniciado` com `fase`, `modo` (completa, só o treino, só o duelo ou só a missão) e `dificuldade` |
| Fase vencida | evento `Fase vencida` com `fase`, `dificuldade` e `estrelas` |
| Caiu (perdeu) | evento `Caiu` com `fase` e `dificuldade` |

Com isso dá para ver o funil de cada fase: quantos começam, quantos vencem e quantos caem, por
dificuldade. As páginas só enviam os eventos pelo site publicado: o script do Plausible entra em
`publicar/gerar-web.sh`; nos protótipos (`prototipo/`) e nos Artifacts as chamadas não fazem nada.

## Configurar (uma vez)

1. **Plausible:** crie a conta em plausible.io e adicione o site com o domínio
   `valentes-de-davi-with-god.netlify.app`. O Plausible entrega um trecho de instalação com um código do
   próprio site (`pa-....js`); ele está em `publicar/gerar-web.sh`. Se o site ganhar outro endereço ou
   o código mudar, troque o trecho lá e gere de novo (`sh publicar/gerar-web.sh`).
2. **Eventos e propriedades:** em *Site Settings → Goals*, crie metas personalizadas com os nomes
   `Jogo iniciado`, `Fase vencida` e `Caiu`. Em *Site Settings → Custom Properties*, cadastre
   `fase`, `modo`, `dificuldade` e `estrelas` (sem isso o Plausible guarda os eventos, mas não deixa
   filtrar por essas propriedades).
3. **Link do painel:** em *Site Settings → Visibility → Shared links*, crie um link (pode marcar a
   senha própria do Plausible também) e copie o endereço inteiro.
4. **Netlify:** em *Project configuration → Environment variables*, crie:

   | Variável | Valor |
   |---|---|
   | `ANALYTICS_USER` | o usuário de quem pode ver o painel |
   | `ANALYTICS_PASSWORD` | uma senha longa |
   | `PLAUSIBLE_SHARE_URL` | o link compartilhado do passo 3 |

   Depois faça um novo deploy (*Deploys → Trigger deploy*) para as variáveis valerem.
5. Abra `https://<site>/analytics`, entre com o usuário e a senha e o painel aparece.

## Como a proteção funciona

- `publicar/web/netlify/edge-functions/analytics.ts` responde em `/analytics`. Sem o usuário e a senha
  certos, devolve 401; com as variáveis faltando, o painel fica **fechado para todos** (503).
- O link do Plausible nunca fica no repositório nem na página pública: só é entregue depois da senha.
- A resposta não é guardada em cache nem indexada por buscadores.
- Para trocar a senha, mude `ANALYTICS_PASSWORD` no Netlify e faça um novo deploy.
- **Senha não entra?** (1) as variáveis só valem depois de um novo deploy; (2) confira se o escopo da variável
  inclui *Functions* ou *Runtime* (o padrão "All scopes" serve) e se o contexto inclui o deploy que você está
  abrindo (produção ou deploy do branch); (3) usuário e senha diferenciam maiúsculas de minúsculas; (4) espaços
  nas pontas são ignorados e letras com acento funcionam.

## Limites e próximos passos

- O Plausible Cloud é pago (há período de teste). Alternativas que funcionam com o mesmo código: o
  Plausible instalado por conta própria (grátis, dá trabalho) ou trocar o script por outro serviço sem
  cookies.
- **Unity:** o Plausible não tem biblioteca para Unity, mas aceita eventos por HTTP
  (`POST https://plausible.io/api/event`). Dá para mandar os mesmos eventos da versão para computador
  (cadastrando outro "site" no Plausible para ela). Hoje a versão Unity não envia nada.
- As páginas dentro de um Artifact do Claude também não enviam nada.
