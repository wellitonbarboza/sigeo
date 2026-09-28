#!/usr/bin/env bash
# =====================================================================================
# QualyHub — cria o endereço curto https://qualyhub.azurewebsites.net
# O nome de um App Service não pode ser trocado; por isso criamos um segundo site
# chamado "qualyhub" no MESMO plano (sem custo extra no B1), com as mesmas configurações,
# permissões e banco, e publicamos o app nele. O site antigo continua no ar até você
# confirmar que o novo está ok (a última etapa oferece desligá-lo).
# Uso no Cloud Shell (Bash), com o qualyhub-app.zip enviado:
#   unzip -o ~/qualyhub-app.zip -d ~/qualyhub-app && cd ~/qualyhub-app && bash criar-link-qualyhub.sh
# Cada etapa pede confirmação (s/N). Pode rodar de novo: é idempotente.
# =====================================================================================
set -euo pipefail
cd "$(dirname "$0")"

RG="${RG:-rg-qualyhub-prd}"
NOVO="${NOVO:-qualyhub}"
GRUPO_DBA="QualyHub-DBA"
ok() { read -r -p "  → $1 Confirma? (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
titulo() { echo; echo "================ $1 ================"; }

titulo "1/6 Site atual"
ANTIGO=$(az webapp list -g "$RG" --query "[?name!='$NOVO'] | [0].name" -o tsv)
[[ -n "$ANTIGO" ]] || { echo "Nenhum site encontrado em $RG."; exit 1; }
PLANO=$(az webapp show -g "$RG" -n "$ANTIGO" --query serverFarmId -o tsv)
echo "  Site atual: $ANTIGO · plano: ${PLANO##*/}"
echo "  Novo endereço: https://$NOVO.azurewebsites.net"

titulo "2/6 Criar o site '$NOVO' no mesmo plano"
if az webapp show -g "$RG" -n "$NOVO" -o none 2>/dev/null; then echo "  Já existe."
else
  ok "Criar o App Service '$NOVO' (mesmo plano, sem custo adicional)." || exit 1
  az webapp create -g "$RG" -p "$PLANO" -n "$NOVO" --runtime "DOTNETCORE:10.0" --assign-identity "[system]" -o none \
    || { echo "  O nome '$NOVO' não está disponível no Azure. Rode com NOVO=qualyhub-app bash criar-link-qualyhub.sh"; exit 1; }
fi

titulo "3/6 Copiar configurações do site atual"
ok "Copiar configurações, cadeia de conexão e ajustes (HTTPS, WebSockets, Always On, /saude)." || exit 1
az webapp identity assign -g "$RG" -n "$NOVO" -o none
az webapp config appsettings list -g "$RG" -n "$ANTIGO" -o json > /tmp/qh-settings.json
az webapp config appsettings set -g "$RG" -n "$NOVO" --settings @/tmp/qh-settings.json -o none
CONN=$(az webapp config connection-string list -g "$RG" -n "$ANTIGO" --query "[?name=='QualyHub'].value.value | [0]" -o tsv)
[[ -n "$CONN" ]] && az webapp config connection-string set -g "$RG" -n "$NOVO" -t SQLAzure --settings QualyHub="$CONN" -o none
az webapp config set -g "$RG" -n "$NOVO" --startup-file "dotnet QualyHub.Web.dll" --web-sockets-enabled true --http20-enabled true \
  --min-tls-version 1.2 --ftps-state Disabled --always-on "$(az webapp config show -g "$RG" -n "$ANTIGO" --query alwaysOn -o tsv)" -o none
az webapp update -g "$RG" -n "$NOVO" --https-only true --client-affinity-enabled true -o none
az webapp config set -g "$RG" -n "$NOVO" --generic-configurations '{"healthCheckPath": "/saude"}' -o none
rm -f /tmp/qh-settings.json

titulo "4/6 Permissões da identidade do novo site"
ok "Dar ao novo site acesso ao Key Vault, ao Storage e ao banco (grupo $GRUPO_DBA)." || exit 1
MI=$(az webapp identity show -g "$RG" -n "$NOVO" --query principalId -o tsv)
KV=$(az keyvault list -g "$RG" --query "[0].id" -o tsv)
ST=$(az storage account list -g "$RG" --query "[0].id" -o tsv)
az role assignment create --assignee-object-id "$MI" --assignee-principal-type ServicePrincipal --role "Key Vault Secrets User" --scope "$KV" -o none 2>/dev/null || true
az role assignment create --assignee-object-id "$MI" --assignee-principal-type ServicePrincipal --role "Storage Blob Data Contributor" --scope "$ST" -o none 2>/dev/null || true
GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv)
az ad group member check --group "$GRUPO_ID" --member-id "$MI" --query value -o tsv | grep -qi true || az ad group member add --group "$GRUPO_ID" --member-id "$MI"

titulo "5/6 Login Microsoft no novo endereço"
CLIENT_ID=$(az webapp config appsettings list -g "$RG" -n "$ANTIGO" --query "[?name=='AzureAd__ClientId'].value | [0]" -o tsv)
URIS=$(az ad app show --id "$CLIENT_ID" --query "web.redirectUris" -o tsv | tr '\n' ' ')
NOVA_URI="https://$NOVO.azurewebsites.net/signin-oidc"
if [[ " $URIS " == *" $NOVA_URI "* ]]; then echo "  Endereço de retorno já cadastrado."
else
  ok "Incluir $NOVA_URI no registro do app (mantém o endereço antigo)." || exit 1
  # shellcheck disable=SC2086
  az ad app update --id "$CLIENT_ID" --web-redirect-uris $URIS "$NOVA_URI" -o none
fi
echo "  Aguardando as permissões propagarem (40 s)…"; sleep 40

titulo "6/6 Publicar e conferir"
ok "Publicar app.zip em https://$NOVO.azurewebsites.net." || exit 1
az webapp deploy -g "$RG" -n "$NOVO" --src-path app.zip --type zip -o none
for i in $(seq 1 30); do c=$(curl -s -o /dev/null -w "%{http_code}" "https://$NOVO.azurewebsites.net/saude" || true); [[ "$c" == "200" ]] && break; sleep 10; done
echo "  /saude → $c"
echo
echo "  Pronto: https://$NOVO.azurewebsites.net  (entre com a conta Microsoft)"
echo "  O site antigo ($ANTIGO) continua no ar. Depois de conferir o novo endereço:"
if ok "Parar o site antigo $ANTIGO agora (pode religar com: az webapp start -g $RG -n $ANTIGO)."; then
  az webapp stop -g "$RG" -n "$ANTIGO" -o none && echo "  $ANTIGO parado."
fi
