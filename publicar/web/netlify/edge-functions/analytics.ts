// Painel de estatísticas: https://<site>/analytics
//
// Só entra quem souber o usuário e a senha (autenticação básica do navegador). O endereço do painel do
// Plausible nunca vai para o repositório: fica nas variáveis de ambiente do Netlify e só é entregue depois
// da senha.
//
//   ANALYTICS_USER      usuário de quem pode ver o painel
//   ANALYTICS_PASSWORD  senha (use uma senha longa)
//   PLAUSIBLE_SHARE_URL link compartilhado do Plausible (Settings → Visibility → Shared links)
//
// Se alguma estiver faltando, o painel fica fechado para todos.

const encoder = new TextEncoder();

// Comparação em tempo constante, para não vazar a senha pelo tempo de resposta.
async function same(a: string, b: string): Promise<boolean> {
  const [ha, hb] = await Promise.all([
    crypto.subtle.digest("SHA-256", encoder.encode(a)),
    crypto.subtle.digest("SHA-256", encoder.encode(b)),
  ]);
  const x = new Uint8Array(ha), y = new Uint8Array(hb);
  let diff = 0;
  for (let i = 0; i < x.length; i++) diff |= x[i] ^ y[i];
  return diff === 0;
}

const headers = {
  "Cache-Control": "no-store",
  "X-Robots-Tag": "noindex, nofollow",
  "Referrer-Policy": "no-referrer",
};

function deny(): Response {
  return new Response("Acesso restrito.", {
    status: 401,
    headers: { ...headers, "WWW-Authenticate": 'Basic realm="Estatísticas dos Valentes de Davi", charset="UTF-8"' },
  });
}

function escapeAttr(s: string): string {
  return s.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
}

export default async (request: Request): Promise<Response> => {
  // Espaços e quebras de linha colados por engano nas variáveis do Netlify não contam.
  const user = Netlify.env.get("ANALYTICS_USER")?.trim(), pass = Netlify.env.get("ANALYTICS_PASSWORD")?.trim(), share = Netlify.env.get("PLAUSIBLE_SHARE_URL")?.trim();
  if (!user || !pass) {
    return new Response("Painel ainda não configurado.", { status: 503, headers });
  }

  const auth = request.headers.get("authorization") ?? "";
  const m = auth.match(/^Basic\s+(.+)$/i);
  if (!m) return deny();
  // O navegador manda "usuário:senha" em UTF-8; sem decodificar assim, letras com acento nunca batem.
  let sent = "";
  try {
    const bin = atob(m[1].trim());
    sent = new TextDecoder().decode(Uint8Array.from(bin, (c) => c.charCodeAt(0)));
  } catch { return deny(); }
  const i = sent.indexOf(":");
  const okUser = await same(i < 0 ? sent : sent.slice(0, i), user);
  const okPass = await same(i < 0 ? "" : sent.slice(i + 1), pass);
  if (!(okUser && okPass)) return deny();

  if (!share) {
    return new Response("Falta configurar PLAUSIBLE_SHARE_URL no Netlify.", { status: 503, headers });
  }
  const src = share + (share.includes("?") ? "&" : "?") + "embed=true&theme=dark&background=transparent";
  const html = `<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex, nofollow">
<title>Estatísticas · Os Valentes de Davi</title>
<style>
  html,body{margin:0;height:100%;background:#17110b;color:#eadcbd;font-family:system-ui,sans-serif}
  header{padding:12px 16px;font-size:14px;letter-spacing:.08em;text-transform:uppercase;color:#c9a35e}
  iframe{display:block;width:100%;height:calc(100% - 44px);border:0}
</style>
</head>
<body>
<header>Os Valentes de Davi · Estatísticas</header>
<iframe plausible-embed src="${escapeAttr(src)}" scrolling="no" loading="lazy" referrerpolicy="no-referrer"></iframe>
<script async src="https://plausible.io/js/embed.host.js"></script>
<script>document.querySelectorAll('[plausible-embed]').forEach(function(f){f.style.height='calc(100% - 44px)'})</script>
</body>
</html>`;
  return new Response(html, {
    status: 200,
    headers: {
      ...headers,
      "Content-Type": "text/html; charset=utf-8",
      "Content-Security-Policy": "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline' https://plausible.io; frame-src https://plausible.io; connect-src https://plausible.io; frame-ancestors 'none'",
    },
  });
};
