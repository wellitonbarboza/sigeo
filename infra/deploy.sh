#!/usr/bin/env bash
# =====================================================================================
# QualyHub — provisionamento e publicação no Azure (idempotente: pode rodar de novo)
# Pré-requisitos: Azure CLI (az) logado (az login --use-device-code), .NET 10 SDK,
# sqlcmd (go-sqlcmd), openssl, python3. Permissão Owner (ou Contributor + User Access
# Administrator) na assinatura e permissão para registrar apps no Entra ID.
# Uso: ./deploy.sh <assinatura> <email-alerta> [ambiente=prd] [grupo-recursos=rg-qualyhub-prd]
# Etapas isoladas: ETAPAS="1 2 3" ./deploy.sh ...   (padrão: todas)
# =====================================================================================
set -euo pipefail

ASSINATURA="${1:?Informe o ID ou nome da assinatura}"
EMAIL_ALERTA="${2:?Informe o e-mail que recebe alerta de custo}"
AMBIENTE="${3:-prd}"
RG="${4:-rg-qualyhub-$AMBIENTE}"
LOCAL="brazilsouth"
GRUPO_DBA="QualyHub-DBA"
ETAPAS="${ETAPAS:-1 2 3 4 5 6 7 8}"
DIR="$(cd "$(dirname "$0")" && pwd)"
RAIZ="$DIR/.."
quer() { [[ " $ETAPAS " == *" $1 "* ]]; }

echo "==> 1/8 Assinatura"
az account show >/dev/null 2>&1 || { echo "Faça antes: az login --use-device-code"; exit 1; }
az account set --subscription "$ASSINATURA"
TENANT=$(az account show --query tenantId -o tsv)
EU=$(az ad signed-in-user show --query id -o tsv)

if quer 2; then
echo "==> 2/8 Grupo do Entra ID administrador do SQL ($GRUPO_DBA)"
GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv 2>/dev/null || true)
if [[ -z "$GRUPO_ID" ]]; then
  GRUPO_ID=$(az ad group create --display-name "$GRUPO_DBA" --mail-nickname "$GRUPO_DBA" --query id -o tsv)
  az ad group member add --group "$GRUPO_ID" --member-id "$EU"
fi
fi
GRUPO_ID=${GRUPO_ID:-$(az ad group show --group "$GRUPO_DBA" --query id -o tsv)}

echo "==> 3/8 (login por usuário e senha do próprio QualyHub — sem registro de app no Entra ID)"

if quer 4; then
echo "==> 4/8 Resource group $RG + infraestrutura (Bicep)"
az group create -n "$RG" -l "$LOCAL" --tags sistema=QualyHub ambiente="$AMBIENTE" -o none
az deployment group create -g "$RG" -n "qualyhub-$(date +%Y%m%d%H%M)" \
  --template-file "$DIR/main.bicep" \
  --parameters "$DIR/main.$AMBIENTE.bicepparam" \
  --parameters ambiente="$AMBIENTE" sqlAdminGrupoObjectId="$GRUPO_ID" sqlAdminGrupoNome="$GRUPO_DBA" \
               orcamentoInicio="$(date +%Y-%m-01)" emailsAlerta="[\"$EMAIL_ALERTA\"]" \
               -o none
fi
SAIDA=$(az deployment group list -g "$RG" --query "sort_by([?starts_with(name,'qualyhub-')], &properties.timestamp)[-1].properties.outputs" -o json)
val() { echo "$SAIDA" | python3 -c "import sys,json; print(json.load(sys.stdin)['$1']['value'])"; }
WEB=$(val webAppNome); URL=$(val webAppUrl); SQLSRV=$(val sqlServidor); DB=$(val sqlBanco); KV=$(val keyVaultNome)

if quer 5; then
echo "==> 5/8 Segredos no Key Vault (HMAC do CPF e código da configuração inicial)"
az role assignment create --assignee "$EU" --role "Key Vault Secrets Officer" \
  --scope "$(az keyvault show -n "$KV" --query id -o tsv)" -o none 2>/dev/null || true
sleep 30
if ! az keyvault secret show --vault-name "$KV" -n CpfHmacChave >/dev/null 2>&1; then
  az keyvault secret set --vault-name "$KV" -n CpfHmacChave --value "$(openssl rand -base64 48)" -o none
  echo "    CpfHmacChave criada. NUNCA rotacione sem reprocessar os hashes de CPF."
fi
# Código da plataforma: cadastrar outras construtoras em /plataforma/nova-construtora.
az keyvault secret show --vault-name "$KV" -n CodigoPlataforma >/dev/null 2>&1 || \
  az keyvault secret set --vault-name "$KV" -n CodigoPlataforma --value "$(openssl rand -hex 16)" -o none
if ! az keyvault secret show --vault-name "$KV" -n CodigoConfiguracaoInicial >/dev/null 2>&1; then
  az keyvault secret set --vault-name "$KV" -n CodigoConfiguracaoInicial --value "$(openssl rand -hex 12)" -o none
fi
echo "    Código da configuração inicial: az keyvault secret show --vault-name $KV -n CodigoConfiguracaoInicial --query value -o tsv"
fi

if quer 6; then
echo "==> 6/8 Banco: script idempotente de migrations + usuário da Managed Identity"
MEU_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create -g "$RG" -s "${SQLSRV%%.*}" -n deploy-temp --start-ip-address "$MEU_IP" --end-ip-address "$MEU_IP" -o none
trap 'az sql server firewall-rule delete -g "$RG" -s "${SQLSRV%%.*}" -n deploy-temp -o none || true' EXIT
sqlcmd -S "$SQLSRV" -d "$DB" --authentication-method ActiveDirectoryDefault -l 120 -b -I -i "$RAIZ/db/QualyHub_Inicial.sql"
sed -e "s/{{WEB}}/$WEB/g" "$DIR/sql/usuarios-identidades.sql" > /tmp/qh-usuarios.sql
sqlcmd -S "$SQLSRV" -d "$DB" --authentication-method ActiveDirectoryDefault -b -I -i /tmp/qh-usuarios.sql
fi

if quer 7; then
echo "==> 7/8 Publicação da aplicação"
rm -rf /tmp/qh-pub /tmp/qh-pub.zip
dotnet publish "$RAIZ/src/QualyHub.Web" -c Release -o /tmp/qh-pub
(cd /tmp/qh-pub && zip -qr /tmp/qh-pub.zip .)
az webapp deploy -g "$RG" -n "$WEB" --src-path /tmp/qh-pub.zip --type zip -o none
fi

echo "==> 8/8 Concluído"
echo "    Site:  $URL  (saúde: $URL/saude)"
echo "    SQL:   $SQLSRV / $DB"
echo "    Primeiro acesso → /configuracao-inicial com o código do Key Vault (cria o administrador com login e senha)"
echo "           e o ObjectId do administrador: az ad signed-in-user show --query id -o tsv"
