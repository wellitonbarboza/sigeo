#!/usr/bin/env bash
# =====================================================================================
# QualyHub — provisionamento e primeira publicação no Azure
# Pré-requisitos: Azure CLI (az), .NET 10 SDK, dotnet-ef, sqlcmd (go-sqlcmd) e permissão
# de Owner/Contributor + User Access Administrator no resource group.
# Uso: ./deploy.sh <assinatura> [ambiente=prd] [grupo-recursos=rg-qualyhub-prd]
# =====================================================================================
set -euo pipefail

ASSINATURA="${1:?Informe o ID ou nome da assinatura}"
AMBIENTE="${2:-prd}"
RG="${3:-rg-qualyhub-$AMBIENTE}"
LOCAL="brazilsouth"
GRUPO_DBA="QualyHub-DBA"
DIR="$(cd "$(dirname "$0")" && pwd)"

echo "==> 1/7 Login e assinatura"
az account show >/dev/null 2>&1 || az login
az account set --subscription "$ASSINATURA"

echo "==> 2/7 Grupo do Entra ID administrador do SQL ($GRUPO_DBA)"
GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv 2>/dev/null || true)
if [[ -z "$GRUPO_ID" ]]; then
  GRUPO_ID=$(az ad group create --display-name "$GRUPO_DBA" --mail-nickname "$GRUPO_DBA" --query id -o tsv)
  az ad group member add --group "$GRUPO_ID" --member-id "$(az ad signed-in-user show --query id -o tsv)"
fi

echo "==> 3/7 Resource group $RG"
az group create -n "$RG" -l "$LOCAL" --tags sistema=QualyHub ambiente="$AMBIENTE" -o none

echo "==> 4/7 Infraestrutura (Bicep)"
SAIDA=$(az deployment group create -g "$RG" -n "qualyhub-$(date +%Y%m%d%H%M)" \
  --template-file "$DIR/main.bicep" \
  --parameters "$DIR/main.$AMBIENTE.bicepparam" \
  --parameters ambiente="$AMBIENTE" sqlAdminGrupoObjectId="$GRUPO_ID" sqlAdminGrupoNome="$GRUPO_DBA" \
               orcamentoInicio="$(date +%Y-%m-01)" \
  --query properties.outputs -o json)

val() { echo "$SAIDA" | python3 -c "import sys,json; print(json.load(sys.stdin)['$1']['value'])"; }
WEB=$(val webAppNome); FUNC=$(val functionAppNome); SQLSRV=$(val sqlServidor)
DB=$(val sqlBanco); KV=$(val keyVaultNome)

echo "==> 5/7 Segredo HMAC do CPF (LGPD) no Key Vault"
# Quem roda precisa de 'Key Vault Secrets Officer' no cofre (RBAC).
az role assignment create --assignee "$(az ad signed-in-user show --query id -o tsv)" \
  --role "Key Vault Secrets Officer" --scope "$(az keyvault show -n "$KV" --query id -o tsv)" -o none 2>/dev/null || true
sleep 20
if ! az keyvault secret show --vault-name "$KV" -n CpfHmacChave >/dev/null 2>&1; then
  az keyvault secret set --vault-name "$KV" -n CpfHmacChave --value "$(openssl rand -base64 48)" -o none
  echo "    Chave criada. NUNCA rotacione sem reprocessar os hashes de CPF."
fi

echo "==> 6/7 Banco: migrations + usuários das Managed Identities"
MEU_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create -g "$RG" -s "${SQLSRV%%.*}" -n deploy-temp --start-ip-address "$MEU_IP" --end-ip-address "$MEU_IP" -o none
export QH_SQL="Server=tcp:$SQLSRV,1433;Database=$DB;Authentication=Active Directory Default;Encrypt=True;Connect Timeout=90"
dotnet ef database update -p "$DIR/../src/QualyHub.Infrastructure" -s "$DIR/../src/QualyHub.Infrastructure"
sed -e "s/{{WEB}}/$WEB/g" -e "s/{{FUNC}}/$FUNC/g" "$DIR/sql/usuarios-identidades.sql" > /tmp/qh-usuarios.sql
sqlcmd -S "$SQLSRV" -d "$DB" --authentication-method ActiveDirectoryDefault -i /tmp/qh-usuarios.sql
az sql server firewall-rule delete -g "$RG" -s "${SQLSRV%%.*}" -n deploy-temp -o none

echo "==> 7/7 Concluído"
echo "    Web:       $(val webAppUrl)"
echo "    Functions: $FUNC"
echo "    SQL:       $SQLSRV / $DB"
echo "    Próximo: publicar a aplicação (dotnet publish + az webapp deploy) e registrar o app no Entra ID."
