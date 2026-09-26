#!/bin/sh
# Gera as páginas completas do site (publicar/web) a partir dos protótipos.
# Uso: sh publicar/gerar-web.sh
set -e
cd "$(dirname "$0")/.."
gen() {
  SRC=$1; OUT=$2
  {
    printf '<!doctype html>\n<html lang="pt-BR">\n<head>\n<meta charset="utf-8">\n'
    printf '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
    printf '<style>body{margin:0}</style>\n'
    sed -n '1p' "$SRC"
    printf '</head>\n<body>\n'
    sed '1d' "$SRC"
    printf '\n</body>\n</html>\n'
  } > "$OUT"
  echo "Gerado $OUT"
}
gen prototipo/valentes-de-davi.html publicar/web/index.html
gen prototipo/fase2-sama.html publicar/web/fase2.html
