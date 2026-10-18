#!/usr/bin/env bash
# SIGEO — liga a IA (Azure OpenAI + Document Intelligence) no ambiente que já existe. Uso no Cloud Shell:
#   unzip -o sigeo-app.zip -d sigeo && cd sigeo && bash configurar-ia.sh
# Cada etapa pergunta antes. Sem chaves: acesso só pela identidade gerenciada do App Service.
#   1. Recurso Azure OpenAI (S0, mesma região) e o modelo (Global Standard: paga só o que usar)
#   2. Document Intelligence F0 (500 páginas/mês grátis) para ler laudos e certificados
#   3. Permissões da identidade do site nos dois recursos
#   4. Configurações do site (IA__*) e reinício
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
LOCAL="${LOCAL:-brazilsouth}"
confirmar() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?starts_with(name,'sigeo')].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
SUF=$(echo -n "$RG" | md5sum | cut -c1-6)
OAI="${OAI:-oai-qualyhub-$SUF}"
DOC="${DOC:-doc-qualyhub-$SUF}"
PRINC=$(az webapp identity show -g "$RG" -n "$WEB" --query principalId -o tsv)
echo "Grupo $RG · site $WEB · OpenAI $OAI · Documentos $DOC · região $LOCAL"
[ -z "$PRINC" ] && { echo "O site não tem identidade gerenciada. Ligue com: az webapp identity assign -g $RG -n $WEB"; exit 1; }

# 1) Azure OpenAI
if ! az cognitiveservices account show -g "$RG" -n "$OAI" -o none 2>/dev/null; then
  confirmar "Criar o Azure OpenAI '$OAI' (S0, $LOCAL)?" && \
    az cognitiveservices account create -g "$RG" -n "$OAI" -l "$LOCAL" --kind OpenAI --sku S0 --custom-domain "$OAI" --yes -o none && \
    az resource update -g "$RG" -n "$OAI" --resource-type Microsoft.CognitiveServices/accounts --set properties.disableLocalAuth=true -o none && echo "  ok"
fi
# Modelo: o primeiro disponível nesta ordem (rápido e barato primeiro). Global Standard = sem custo fixo.
MODELO=""; VERSAO=""
for m in gpt-4.1-mini gpt-5-mini gpt-4o-mini; do
  V=$(az cognitiveservices account list-models -g "$RG" -n "$OAI" --query "[?name=='$m' && skus[?name=='GlobalStandard']].version | sort(@) | [-1]" -o tsv 2>/dev/null || true)
  if [ -n "$V" ]; then MODELO="$m"; VERSAO="$V"; break; fi
done
[ -z "$MODELO" ] && { echo "Nenhum modelo (gpt-4.1-mini / gpt-5-mini / gpt-4o-mini) disponível em Global Standard nesta região."; exit 1; }
if ! az cognitiveservices account deployment show -g "$RG" -n "$OAI" --deployment-name "$MODELO" -o none 2>/dev/null; then
  confirmar "Implantar o modelo $MODELO ($VERSAO) em Global Standard (cobra por token usado)?" && \
    az cognitiveservices account deployment create -g "$RG" -n "$OAI" --deployment-name "$MODELO" --model-name "$MODELO" --model-version "$VERSAO" \
      --model-format OpenAI --sku-name GlobalStandard --sku-capacity 50 -o none && echo "  ok"
fi

# 2) Document Intelligence
if ! az cognitiveservices account show -g "$RG" -n "$DOC" -o none 2>/dev/null; then
  confirmar "Criar o Document Intelligence '$DOC' (F0 gratuito, $LOCAL)?" && \
    az cognitiveservices account create -g "$RG" -n "$DOC" -l "$LOCAL" --kind FormRecognizer --sku F0 --custom-domain "$DOC" --yes -o none && \
    az resource update -g "$RG" -n "$DOC" --resource-type Microsoft.CognitiveServices/accounts --set properties.disableLocalAuth=true -o none && echo "  ok"
fi

# 3) Permissões (identidade do site)
if confirmar "Dar ao site acesso de uso aos dois recursos (sem chave)?"; then
  az role assignment create --assignee-object-id "$PRINC" --assignee-principal-type ServicePrincipal --role "Cognitive Services OpenAI User" \
    --scope "$(az cognitiveservices account show -g "$RG" -n "$OAI" --query id -o tsv)" -o none || true
  az role assignment create --assignee-object-id "$PRINC" --assignee-principal-type ServicePrincipal --role "Cognitive Services User" \
    --scope "$(az cognitiveservices account show -g "$RG" -n "$DOC" --query id -o tsv)" -o none || true
  echo "  ok"
fi

# 4) Configuração do site
if confirmar "Gravar a configuração IA__* no site e reiniciar?"; then
  RAC=false; [[ "$MODELO" == gpt-5* ]] && RAC=true
  az webapp config appsettings set -g "$RG" -n "$WEB" -o none --settings \
    IA__Endpoint="https://$OAI.openai.azure.com" IA__Deployment="$MODELO" IA__Raciocinio="$RAC" \
    IA__DocumentosEndpoint="https://$DOC.cognitiveservices.azure.com" IA__LimiteMensalPorEmpresa=3000 IA__AnalisarNotasAutomatico=false
  az webapp restart -g "$RG" -n "$WEB" -o none && echo "  ok — botões de IA aparecem no mapa de cotação, notas fiscais, RNC, produtos e ensaios."
fi
