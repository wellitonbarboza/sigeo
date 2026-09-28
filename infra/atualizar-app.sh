#!/usr/bin/env bash
# QualyHub — publica só uma nova versão do app (a infraestrutura já existe). Uso no Cloud Shell:
#   unzip -o qualyhub-app.zip -d qualyhub && cd qualyhub && bash atualizar-app.sh
# O app aplica as migrations do banco sozinho ao iniciar (Banco__MigrarAoIniciar=true).
set -euo pipefail
cd "$(dirname "$0")"
RG="${RG:-rg-qualyhub-prd}"
# Prefere o endereço curto "qualyhub" quando ele já existe (criado por criar-link-qualyhub.sh).
WEB="${WEB:-$(az webapp list -g "$RG" --query "[?name=='qualyhub'] | [0].name" -o tsv)}"
[[ -n "$WEB" ]] || WEB=$(az webapp list -g "$RG" --query "[0].name" -o tsv)
URL="https://$(az webapp show -g "$RG" -n "$WEB" --query defaultHostName -o tsv)"
read -r -p "Publicar a nova versão em $WEB ($URL)? (s/N) " r; [[ "$r" =~ ^[sS]$ ]] || exit 1
az webapp deploy -g "$RG" -n "$WEB" --src-path app.zip --type zip -o none
echo "Aguardando o site subir e atualizar o banco…"
for i in $(seq 1 30); do c=$(curl -s -o /dev/null -w "%{http_code}" "$URL/saude" || true); [[ "$c" == "200" ]] && break; sleep 10; done
echo "/saude → $c · $URL"
