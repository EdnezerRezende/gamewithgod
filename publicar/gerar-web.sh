#!/bin/sh
# Gera as páginas completas do site (publicar/web) a partir dos protótipos.
# Uso: sh publicar/gerar-web.sh
set -e
# Domínio cadastrado no Plausible (troque se o site ganhar um endereço próprio):
PLAUSIBLE_DOMAIN=${PLAUSIBLE_DOMAIN:-valentes-de-davi-with-god.netlify.app}
cd "$(dirname "$0")/.."
gen() {
  SRC=$1; OUT=$2
  {
    printf '<!doctype html>\n<html lang="pt-BR">\n<head>\n<meta charset="utf-8">\n'
    printf '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
    printf '<style>body{margin:0}</style>\n'
    # Estatísticas anônimas do Plausible (sem cookies). O domínio é o do site no Plausible.
    printf '<script defer data-domain="%s" src="https://plausible.io/js/script.js"></script>\n' "$PLAUSIBLE_DOMAIN"
    printf '<script>window.plausible=window.plausible||function(){(window.plausible.q=window.plausible.q||[]).push(arguments)}</script>\n'
    sed -n '1p' "$SRC"
    printf '</head>\n<body>\n'
    sed '1d' "$SRC"
    printf '\n</body>\n</html>\n'
  } > "$OUT"
  echo "Gerado $OUT"
}
gen prototipo/valentes-de-davi.html publicar/web/index.html
gen prototipo/fase2-sama.html publicar/web/fase2.html
gen prototipo/fase3-eleazar.html publicar/web/fase3.html
gen prototipo/fase4-agua-de-belem.html publicar/web/fase4.html
gen prototipo/fase5-benaia.html publicar/web/fase5.html
gen prototipo/fase6-abisai.html publicar/web/fase6.html
gen prototipo/fase7-josebe.html publicar/web/fase7.html
