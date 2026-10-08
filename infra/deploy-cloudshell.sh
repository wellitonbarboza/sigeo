#!/usr/bin/env bash
# =====================================================================================
# QualyHub — publicação pelo Azure Cloud Shell (Bash), sem nada instalado no PC.
# Uso (no Cloud Shell, após enviar o pacote qualyhub-deploy.zip pelo botão "Carregar"):
#   unzip -o qualyhub-deploy.zip -d qualyhub && cd qualyhub && bash deploy-cloudshell.sh
# Cada etapa pede confirmação (s/N). Pode rodar de novo: tudo é idempotente.
# Não precisa de sqlcmd nem .NET: o app cria as tabelas ao iniciar, com a identidade
# gerenciada dele como membro do grupo administrador do SQL (QualyHub-DBA).
# =====================================================================================
set -euo pipefail
cd "$(dirname "$0")"

AMBIENTE="${AMBIENTE:-prd}"
RG="${RG:-rg-qualyhub-$AMBIENTE}"
LOCAL="${LOCAL:-brazilsouth}"
GRUPO_DBA="QualyHub-DBA"

ok() { read -r -p "  → $1 Confirma? (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
titulo() { echo; echo "================ $1 ================"; }

titulo "1/7 Assinatura"
az account list --query "[].{Nome:name, Id:id, Padrao:isDefault, Estado:state}" -o table
SUB=$(az account show --query id -o tsv)
read -r -p "  Assinatura a usar [Enter = $SUB]: " s; SUB="${s:-$SUB}"
az account set --subscription "$SUB"
TENANT=$(az account show --query tenantId -o tsv)
EU=$(az ad signed-in-user show --query id -o tsv)
while [[ -z "${EMAIL:-}" || "$EMAIL" != *@*.* ]]; do read -r -p "  E-mail para alerta de custo (obrigatório): " EMAIL; done
read -r -p "  Orçamento mensal (moeda da assinatura) [500]: " ORC; ORC="${ORC:-500}"
echo "  Plano do App Service: B1 (~US\$13/mês, recomendado) ou F1 (gratuito, para teste: dorme sem uso, 60 min CPU/dia)."
read -r -p "  Plano [${SKU:-B1}]: " s; SKU="${s:-${SKU:-B1}}"
echo "  Região atual: $LOCAL (mude com LOCAL=eastus2 bash deploy-cloudshell.sh)"
echo "  Assinatura $SUB · tenant $TENANT · grupo de recursos $RG · região $LOCAL"

titulo "2/7 Grupo administrador do SQL ($GRUPO_DBA)"
GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv 2>/dev/null || true)
if [[ -z "$GRUPO_ID" ]]; then
  ok "Criar o grupo $GRUPO_DBA no Entra ID e incluir você." || exit 1
  GRUPO_ID=$(az ad group create --display-name "$GRUPO_DBA" --mail-nickname "$GRUPO_DBA" --query id -o tsv)
  az ad group member add --group "$GRUPO_ID" --member-id "$EU"
else echo "  Já existe ($GRUPO_ID)."; fi

titulo "3/7 Login"
echo "  Login por usuário e senha do próprio QualyHub (sem conta Microsoft)."

titulo "4/7 Infraestrutura (App Service B1, SQL serverless, Storage, Key Vault, App Insights)"
echo "  Custo estimado: ~R\$150–400/mês (SQL pausa sozinho sem uso)."
ok "Criar/atualizar o grupo de recursos $RG e os recursos." || exit 1
az group create -n "$RG" -l "$LOCAL" --tags sistema=QualyHub ambiente="$AMBIENTE" -o none
DEP="qualyhub-$(date +%Y%m%d%H%M)"
if ! az deployment group create -g "$RG" -n "$DEP" --template-file main.json \
      --parameters ambiente="$AMBIENTE" sqlAdminGrupoObjectId="$GRUPO_ID" sqlAdminGrupoNome="$GRUPO_DBA" \
                   orcamentoInicio="$(date +%Y-%m-01)" orcamentoMensal="$ORC" emailsAlerta="[\"$EMAIL\"]" \
                   appServiceSku="$SKU" -o none; then
  echo "  Falhou. Se o erro for no orçamento (Consumption), tentando sem ele…"
  echo "  (Se o erro for 'SubscriptionIsOverQuotaForSku', a assinatura não tem cota para $SKU nesta região:"
  echo "   peça cota em Portal › Cotas › App Service, ou rode de novo com o plano F1.)"
  az deployment group create -g "$RG" -n "$DEP-b" --template-file main.json \
      --parameters ambiente="$AMBIENTE" sqlAdminGrupoObjectId="$GRUPO_ID" sqlAdminGrupoNome="$GRUPO_DBA" criarOrcamento=false \
                   orcamentoInicio="$(date +%Y-%m-01)" emailsAlerta="[\"$EMAIL\"]" appServiceSku="$SKU" -o none
  DEP="$DEP-b"
fi
out() { az deployment group show -g "$RG" -n "$DEP" --query "properties.outputs.$1.value" -o tsv; }
WEB=$(out webAppNome); URL=$(out webAppUrl); KV=$(out keyVaultNome)
echo "  Site: $URL"

titulo "5/7 Permissões e segredos"
ok "Incluir a identidade do site no grupo $GRUPO_DBA (o app cria as tabelas) e gravar os segredos no Key Vault." || exit 1
MI=$(az webapp identity show -g "$RG" -n "$WEB" --query principalId -o tsv)
az ad group member check --group "$GRUPO_ID" --member-id "$MI" --query value -o tsv | grep -qi true || az ad group member add --group "$GRUPO_ID" --member-id "$MI"
az role assignment create --assignee-object-id "$EU" --assignee-principal-type User --role "Key Vault Secrets Officer" \
  --scope "$(az keyvault show -n "$KV" --query id -o tsv)" -o none 2>/dev/null || true
echo "  Aguardando a permissão propagar (40 s)…"; sleep 40
az keyvault secret show --vault-name "$KV" -n CpfHmacChave >/dev/null 2>&1 || \
  az keyvault secret set --vault-name "$KV" -n CpfHmacChave --value "$(openssl rand -base64 48)" -o none
az keyvault secret show --vault-name "$KV" -n CodigoPlataforma >/dev/null 2>&1 || \
  az keyvault secret set --vault-name "$KV" -n CodigoPlataforma --value "$(openssl rand -hex 16)" -o none
az keyvault secret show --vault-name "$KV" -n CodigoConfiguracaoInicial >/dev/null 2>&1 || \
  az keyvault secret set --vault-name "$KV" -n CodigoConfiguracaoInicial --value "$(openssl rand -hex 12)" -o none
az webapp config appsettings set -g "$RG" -n "$WEB" --settings Banco__MigrarAoIniciar=true -o none
az webapp config set -g "$RG" -n "$WEB" --startup-file "dotnet QualyHub.Web.dll" -o none

titulo "6/7 Publicação da aplicação"
ok "Publicar app.zip no site $WEB." || exit 1
az webapp deploy -g "$RG" -n "$WEB" --src-path app.zip --type zip -o none
echo "  Aguardando o site subir e criar as tabelas (pode levar 2–4 min no 1º acesso)…"
for i in $(seq 1 30); do
  c=$(curl -s -o /dev/null -w "%{http_code}" "$URL/saude" || true)
  [[ "$c" == "200" ]] && break; sleep 10
done
echo "  /saude → $c"

titulo "7/7 Primeiro acesso"
echo "  1) Abra $URL/configuracao-inicial"
echo "  2) Código de configuração: $(az keyvault secret show --vault-name "$KV" -n CodigoConfiguracaoInicial --query value -o tsv)"
echo "  3) Cadastre o administrador com login e senha; os demais usuários recebem senha provisória em Sistema › Usuários."
echo "  Logs, se precisar: az webapp log tail -g $RG -n $WEB"
