#!/usr/bin/env bash
# SIGEO — credenciais da API do SIEG de uma construtora (cada uma tem o seu contrato). Uso no Cloud Shell:
#   bash configurar-sieg.sh
# Pede o código da empresa (aparece na tela Suprimentos › Notas fiscais) e as 3 credenciais da API nova do SIEG
# (painel do SIEG › API: API Key, Client ID e Secret Key). Grava no Key Vault — nunca em arquivo — e reinicia o site.
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?starts_with(name,'sigeo')].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
KV=$(az keyvault list -g "$RG" --query "[0].name" -o tsv)
read -r -p "Código da empresa (GUID mostrado na tela de notas fiscais): " EMP
[[ "$EMP" =~ ^[0-9a-fA-F-]{36}$ ]] || { echo "Código inválido."; exit 1; }
EMP=$(echo "$EMP" | tr 'A-F' 'a-f')
read -r -s -p "SIEG API Key: " K; echo
read -r -s -p "SIEG Client ID: " C; echo
read -r -s -p "SIEG Secret Key: " S; echo
for par in "ApiKey:$K" "ClientId:$C" "SecretKey:$S"; do
  nome="${par%%:*}"; valor="${par#*:}"
  az keyvault secret set --vault-name "$KV" -n "Sieg--Empresas--$EMP--$nome" --value "$valor" -o none
done
unset K C S
echo "Credenciais gravadas no cofre $KV."
read -r -p "Reiniciar o site agora para valer? (s/N) " r
[[ "$r" =~ ^[sS]$ ]] && az webapp restart -g "$RG" -n "$WEB" -o none && echo "ok — a busca automática roda às 6h30 e o botão 'Buscar no SIEG' aparece na tela."
