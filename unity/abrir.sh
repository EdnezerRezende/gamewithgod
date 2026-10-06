#!/bin/sh
# Abre o projeto com o conector do Unity (CLI `unity`). Uso: sh unity/abrir.sh [versão]
# Sem versão, usa o Unity 6 LTS instalado (veja docs/UNITY.md).
cd "$(dirname "$0")"
exec unity open . --editor-version "${1:-lts}"
