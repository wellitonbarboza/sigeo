#!/usr/bin/env bash
# QualyHub — publicação automática pelo GitHub Actions (sem senha guardada: OIDC/federação). Uso no Cloud Shell:
#   bash configurar-github.sh
# Cria um aplicativo no Entra ID só para publicar, dá a ele "Website Contributor" SOMENTE no site (não na assinatura)
# e a credencial federada para o repositório/branch main. No fim mostra os valores para colar em
# GitHub › Settings › Secrets and variables › Actions.
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
WEB="${WEB:-$(az webapp list -g "$RG" --query "[?name=='qualyhub'] | [0].name" -o tsv)}"
REPO="${REPO:-}"
[[ -n "$REPO" ]] || read -r -p "Repositório GitHub (usuario/repositorio): " REPO
[[ "$REPO" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || { echo "Formato inválido."; exit 1; }
confirmar() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
confirmar "Criar o aplicativo 'qualyhub-github-publicar' com acesso só ao site $WEB?" || exit 0
APP=$(az ad app list --display-name qualyhub-github-publicar --query "[0].appId" -o tsv)
[[ -n "$APP" ]] || APP=$(az ad app create --display-name qualyhub-github-publicar --query appId -o tsv)
az ad sp create --id "$APP" -o none 2>/dev/null || true
az ad app federated-credential list --id "$APP" --query "[?name=='main'].name" -o tsv | grep -q main || \
az ad app federated-credential create --id "$APP" --parameters "{\"name\":\"main\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"repo:$REPO:environment:producao\",\"audiences\":[\"api://AzureADTokenExchange\"]}" -o none
az role assignment create --assignee "$APP" --role "Website Contributor" --scope "$(az webapp show -g "$RG" -n "$WEB" --query id -o tsv)" -o none
az role assignment create --assignee "$APP" --role "Reader" --scope "$(az group show -n "$RG" --query id -o tsv)" -o none
echo
echo "Cole em GitHub › Settings › Secrets and variables › Actions:"
echo "  Secrets:   AZURE_CLIENT_ID=$APP"
echo "             AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "             AZURE_SUBSCRIPTION_ID=$(az account show --query id -o tsv)"
echo "  Variables: AZURE_WEBAPP=$WEB   AZURE_RG=$RG"
echo "E crie o ambiente 'producao' em Settings › Environments (marque revisor obrigatório se quiser aprovar cada publicação)."

# Com a GitHub CLI (já vem no Cloud Shell) os segredos, variáveis e o ambiente "producao" são cadastrados daqui mesmo.
TENANT=$(az account show --query tenantId -o tsv); SUBID=$(az account show --query id -o tsv)
if command -v gh >/dev/null && confirmar "Cadastrar segredos, variáveis e o ambiente 'producao' direto no GitHub (login no GitHub pelo navegador)?"; then
  gh auth status -h github.com >/dev/null 2>&1 || gh auth login -h github.com -p https -w
  gh api -X PUT "repos/$REPO/environments/producao" --silent
  gh secret set AZURE_CLIENT_ID -R "$REPO" -b "$APP"
  gh secret set AZURE_TENANT_ID -R "$REPO" -b "$TENANT"
  gh secret set AZURE_SUBSCRIPTION_ID -R "$REPO" -b "$SUBID"
  gh variable set AZURE_WEBAPP -R "$REPO" -b "$WEB"
  gh variable set AZURE_RG -R "$REPO" -b "$RG"
  echo "GitHub configurado. Próximos envios para a main publicam sozinhos (Actions › Publicar)."
fi
