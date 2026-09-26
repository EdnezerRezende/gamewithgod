#!/bin/sh
# Gera publicar/web/index.html (página completa) a partir do protótipo.
# Uso: sh publicar/gerar-web.sh
set -e
cd "$(dirname "$0")/.."
SRC=prototipo/valentes-de-davi.html
OUT=publicar/web/index.html
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
