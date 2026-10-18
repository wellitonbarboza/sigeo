#!/usr/bin/env bash
# SIGEO — publica só uma nova versão do app (a infraestrutura já existe). Uso no Cloud Shell:
#   unzip -o sigeo-app.zip -d sigeo && cd sigeo && bash atualizar-app.sh
# O app aplica as migrations do banco sozinho ao iniciar (Banco__MigrarAoIniciar=true).
set -euo pipefail
cd "$(dirname "$0")"
RG="${RG:-rg-qualyhub-prd}"
# Prefere o site "sigeo" (criar-endereco-sigeo.sh); sem ele, "groundkey" ou o antigo "qualyhub".
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?starts_with(name,'sigeo')].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
[[ -n "$WEB" ]] || WEB=$(az webapp list -g "$RG" --query "[0].name" -o tsv)
URL="https://$(az webapp show -g "$RG" -n "$WEB" --query defaultHostName -o tsv)"
read -r -p "Publicar a nova versão em $WEB ($URL)? (s/N) " r; [[ "$r" =~ ^[sS]$ ]] || exit 1
# Login agora é por usuário e senha do SIGEO: remove configurações antigas do login Microsoft (se existirem).
ANTIGAS=$(az webapp config appsettings list -g "$RG" -n "$WEB" --query "[?starts_with(name,'AzureAd__')].name" -o tsv | tr '\n' ' ')
[[ -n "${ANTIGAS// /}" ]] && az webapp config appsettings delete -g "$RG" -n "$WEB" --setting-names $ANTIGAS -o none
# O executável passou a se chamar Sigeo.Web.dll (antes QualyHub.Web.dll).
az webapp config set -g "$RG" -n "$WEB" --startup-file "dotnet Sigeo.Web.dll" -o none
az webapp deploy -g "$RG" -n "$WEB" --src-path app.zip --type zip -o none
echo "Aguardando o site subir e atualizar o banco…"
for i in $(seq 1 30); do c=$(curl -s -o /dev/null -w "%{http_code}" "$URL/saude" || true); [[ "$c" == "200" ]] && break; sleep 10; done
echo "/saude → $c · $URL"
echo "Administrador sem senha ainda? Abra $URL/conta/primeiro-acesso com o código do Key Vault:"
echo "  az keyvault secret show --vault-name \$(az keyvault list -g $RG --query '[0].name' -o tsv) -n CodigoConfiguracaoInicial --query value -o tsv"
