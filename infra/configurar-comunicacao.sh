#!/usr/bin/env bash
# SIGEO — Azure Communication Services (e-mail e WhatsApp) para a Área do Fornecedor.
# Uso no Cloud Shell (Bash), depois de publicar o app:
#   bash configurar-comunicacao.sh
# Cria (se não existirem):
#   1. recurso Communication Services   (acs-<app>)       — data location Brazil
#   2. recurso Email Communication      (acsmail-<app>)    + domínio gerenciado pelo Azure (xxxx.azurecomm.net)
#   3. liga o domínio ao recurso de comunicação
#   4. dá à identidade gerenciada do App Service o papel "Contributor" SÓ no recurso de comunicação
#      (o SDK de e-mail com identidade gerenciada exige esse papel; nada de chave/connection string guardada)
#   5. grava Sigeo__Comunicacao__Endpoint e Sigeo__Comunicacao__EmailRemetente no App Service
# Cada etapa pede confirmação. WhatsApp: veja as instruções no fim (exige conta WhatsApp Business/Meta).
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?name=='sigeo'].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
[[ -n "$WEB" ]] || WEB=$(az webapp list -g "$RG" --query "[0].name" -o tsv)
ACS="${ACS:-acs-$WEB}"
MAIL="${MAIL:-acsmail-$WEB}"
LOCAL_DADOS="${LOCAL_DADOS:-Brazil}"

confirmar() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }

az extension add --name communication --upgrade -o none 2>/dev/null || true
echo "Grupo: $RG · App: $WEB · Comunicação: $ACS · E-mail: $MAIL · dados em: $LOCAL_DADOS"

# 1) Communication Services
if ! az communication show -g "$RG" -n "$ACS" -o none 2>/dev/null; then
  confirmar "Criar o recurso Communication Services '$ACS'?" || exit 1
  az communication create -g "$RG" -n "$ACS" --location global --data-location "$LOCAL_DADOS" -o none
fi
ACS_ID=$(az communication show -g "$RG" -n "$ACS" --query id -o tsv)
ENDPOINT="https://$(az communication show -g "$RG" -n "$ACS" --query hostName -o tsv)"

# 2) Email Communication + domínio gerenciado pelo Azure
if ! az communication email show -g "$RG" -n "$MAIL" -o none 2>/dev/null; then
  confirmar "Criar o recurso de e-mail '$MAIL' com domínio gerenciado pelo Azure?" || exit 1
  az communication email create -g "$RG" -n "$MAIL" --location global --data-location "$LOCAL_DADOS" -o none
fi
if ! az communication email domain show -g "$RG" --email-service-name "$MAIL" -n AzureManagedDomain -o none 2>/dev/null; then
  az communication email domain create -g "$RG" --email-service-name "$MAIL" -n AzureManagedDomain \
    --location global --domain-management AzureManaged -o none
fi
DOM_ID=$(az communication email domain show -g "$RG" --email-service-name "$MAIL" -n AzureManagedDomain --query id -o tsv)
DOM=$(az communication email domain show -g "$RG" --email-service-name "$MAIL" -n AzureManagedDomain --query mailFromSenderDomain -o tsv)
REMETENTE="${REMETENTE:-DoNotReply@$DOM}"

# 3) Liga o domínio ao recurso de comunicação
if ! az communication show -g "$RG" -n "$ACS" --query "linkedDomains" -o tsv | grep -qi "$DOM_ID"; then
  confirmar "Ligar o domínio $DOM ao '$ACS'?" || exit 1
  az communication update -g "$RG" -n "$ACS" --linked-domains "$DOM_ID" -o none
fi

# 4) Papel da identidade gerenciada do App Service (escopo: só o recurso de comunicação)
PRINCIPAL=$(az webapp identity show -g "$RG" -n "$WEB" --query principalId -o tsv)
if [[ -z "$(az role assignment list --assignee "$PRINCIPAL" --scope "$ACS_ID" --role Contributor --query '[0].id' -o tsv)" ]]; then
  confirmar "Dar à identidade do app '$WEB' o papel Contributor somente no recurso '$ACS'?" || exit 1
  az role assignment create --assignee-object-id "$PRINCIPAL" --assignee-principal-type ServicePrincipal \
    --role Contributor --scope "$ACS_ID" -o none
fi

# 5) Configuração do app
confirmar "Gravar Endpoint=$ENDPOINT e Remetente=$REMETENTE no app '$WEB' (o app reinicia)?" || exit 1
az webapp config appsettings set -g "$RG" -n "$WEB" -o none --settings \
  Sigeo__Comunicacao__Endpoint="$ENDPOINT" \
  Sigeo__Comunicacao__EmailRemetente="$REMETENTE"

cat <<TXT

E-mail configurado: remetente $REMETENTE (endpoint $ENDPOINT).
A atribuição de papel pode levar alguns minutos para valer. Teste em /fornecedor com um CNPJ que tenha contato com e-mail.

──── WhatsApp (opcional, fica desligado até configurar) ────
1. Portal do Azure → recurso '$ACS' → Advanced Messaging → Channels → Connect → WhatsApp.
   Entre com a conta Meta da empresa, escolha/crie a conta WhatsApp Business e o número (não pode estar em uso
   no app WhatsApp comum). Copie o "Channel ID" (um GUID).
2. No WhatsApp Manager (Meta), crie e aguarde aprovar dois modelos, idioma Português (BR):
   a) Categoria AUTENTICAÇÃO (ex.: nome "qualyhub_codigo"): corpo padrão de código + botão "Copiar código".
   b) Categoria UTILIDADE (ex.: nome "qualyhub_convite"), corpo com 3 variáveis, nesta ordem:
      "{{1}} convida sua empresa: {{2}}. Acesse {{3}} e entre com o CNPJ e o código enviado ao contato."
3. Grave no app (troque os valores):
   az webapp config appsettings set -g $RG -n $WEB -o none --settings \\
     Sigeo__Comunicacao__WhatsAppCanalId=<channel-id> \\
     Sigeo__Comunicacao__WhatsAppModeloCodigo=qualyhub_codigo \\
     Sigeo__Comunicacao__WhatsAppModeloConvite=qualyhub_convite \\
     Sigeo__Comunicacao__WhatsAppIdioma=pt_BR
TXT
