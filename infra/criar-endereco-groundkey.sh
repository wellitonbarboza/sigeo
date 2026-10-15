#!/usr/bin/env bash
# =====================================================================================
# Groundkey — cria o endereço https://groundkey.azurewebsites.net e faz o antigo
# (https://qualyhub.azurewebsites.net) redirecionar para ele.
# O nome de um App Service não pode ser trocado; por isso criamos um segundo site
# "groundkey" no MESMO plano (sem custo extra no B1), com as mesmas configurações,
# permissões e banco. O site antigo recebe a mesma versão do app com
# Groundkey__RedirecionarPara: links de e-mail e favoritos antigos continuam funcionando,
# e as rotinas (avisos por e-mail, rotina das 6h30) passam a rodar só no site novo.
# Uso no Cloud Shell (Bash), com os 3 arquivos enviados:
#   cat groundkey-app.part0 groundkey-app.part1 groundkey-app.part2 > groundkey-app.zip
#   rm -rf groundkey && unzip -o groundkey-app.zip -d groundkey && cd groundkey && bash criar-endereco-groundkey.sh
# Cada etapa pede confirmação (s/N). Pode rodar de novo: é idempotente.
# =====================================================================================
set -euo pipefail
cd "$(dirname "$0")"

RG="${RG:-rg-qualyhub-prd}"
NOVO="${NOVO:-groundkey}"
ANTIGO="${ANTIGO:-qualyhub}"
GRUPO_DBA="QualyHub-DBA"
ok() { read -r -p "  → $1 Confirma? (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
titulo() { echo; echo "================ $1 ================"; }

titulo "1/7 Site atual"
az webapp show -g "$RG" -n "$ANTIGO" -o none 2>/dev/null || ANTIGO=$(az webapp list -g "$RG" --query "[?name!='$NOVO'] | [0].name" -o tsv)
[[ -n "$ANTIGO" ]] || { echo "Nenhum site encontrado em $RG."; exit 1; }
PLANO=$(az webapp show -g "$RG" -n "$ANTIGO" --query serverFarmId -o tsv)
echo "  Site atual: $ANTIGO · plano: ${PLANO##*/}"
echo "  Novo endereço: https://$NOVO.azurewebsites.net"

titulo "2/7 Criar o site '$NOVO' no mesmo plano"
if az webapp show -g "$RG" -n "$NOVO" -o none 2>/dev/null; then echo "  Já existe."
else
  ok "Criar o App Service '$NOVO' (mesmo plano, sem custo adicional)." || exit 1
  az webapp create -g "$RG" -p "$PLANO" -n "$NOVO" --runtime "DOTNETCORE:10.0" --assign-identity "[system]" -o none \
    || { echo "  O nome '$NOVO' não está disponível no Azure. Rode com NOVO=groundkey-app bash criar-endereco-groundkey.sh"; exit 1; }
fi

titulo "3/7 Copiar configurações (com os nomes novos)"
ok "Copiar configurações, conexão com o banco e ajustes (HTTPS, WebSockets, Always On, /saude). Chaves QualyHub__* viram Groundkey__*." || exit 1
az webapp identity assign -g "$RG" -n "$NOVO" -o none
az webapp config appsettings list -g "$RG" -n "$ANTIGO" -o json \
  | python3 -c 'import json,sys; d=json.load(sys.stdin); print(json.dumps([{"name": x["name"].replace("QualyHub__","Groundkey__",1), "value": x["value"], "slotSetting": False} for x in d if not x["name"].endswith("RedirecionarPara")]))' \
  > /tmp/gk-settings.json
az webapp config appsettings set -g "$RG" -n "$NOVO" --settings @/tmp/gk-settings.json -o none
rm -f /tmp/gk-settings.json
# A CLI devolve o valor como texto ou como objeto {value}, conforme a versão — aceita os dois.
CONN=""
for NOME in Groundkey QualyHub; do
  [[ -n "$CONN" ]] && break
  CONN=$(az webapp config connection-string list -g "$RG" -n "$ANTIGO" --query "[?name=='$NOME'] | [0].value" -o tsv)
  [[ "$CONN" == *"Server="* || "$CONN" == *"server="* ]] || CONN=$(az webapp config connection-string list -g "$RG" -n "$ANTIGO" --query "[?name=='$NOME'] | [0].value.value" -o tsv)
done
[[ -n "$CONN" ]] || { echo "  Não achei a conexão com o banco no site $ANTIGO. Pare aqui e me avise."; exit 1; }
az webapp config connection-string set -g "$RG" -n "$NOVO" -t SQLAzure --settings Groundkey="$CONN" -o none
echo "  Conexão com o banco copiada."
az webapp config set -g "$RG" -n "$NOVO" --startup-file "dotnet Groundkey.Web.dll" --web-sockets-enabled true --http20-enabled true \
  --min-tls-version 1.2 --ftps-state Disabled --always-on "$(az webapp config show -g "$RG" -n "$ANTIGO" --query alwaysOn -o tsv)" -o none
az webapp update -g "$RG" -n "$NOVO" --https-only true --client-affinity-enabled true -o none
az webapp config set -g "$RG" -n "$NOVO" --generic-configurations '{"healthCheckPath": "/saude"}' -o none
az webapp config appsettings set -g "$RG" -n "$NOVO" --settings Groundkey__EnderecoSite="https://$NOVO.azurewebsites.net" -o none

titulo "4/7 Permissões da identidade do novo site"
ok "Dar ao novo site acesso ao Key Vault, Storage, banco (grupo $GRUPO_DBA), IA e comunicação (os que existirem)." || exit 1
MI=$(az webapp identity show -g "$RG" -n "$NOVO" --query principalId -o tsv)
papel() { az role assignment create --assignee-object-id "$MI" --assignee-principal-type ServicePrincipal --role "$1" --scope "$2" -o none 2>/dev/null || true; }
KV=$(az keyvault list -g "$RG" --query "[0].id" -o tsv); [[ -n "$KV" ]] && papel "Key Vault Secrets User" "$KV"
ST=$(az storage account list -g "$RG" --query "[0].id" -o tsv); [[ -n "$ST" ]] && papel "Storage Blob Data Contributor" "$ST"
for id in $(az cognitiveservices account list -g "$RG" --query "[?kind=='OpenAI'].id" -o tsv); do papel "Cognitive Services OpenAI User" "$id"; done
for id in $(az cognitiveservices account list -g "$RG" --query "[?kind=='FormRecognizer'].id" -o tsv); do papel "Cognitive Services User" "$id"; done
for id in $(az resource list -g "$RG" --resource-type Microsoft.Communication/communicationServices --query "[].id" -o tsv); do papel "Contributor" "$id"; done
GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv)
az ad group member check --group "$GRUPO_ID" --member-id "$MI" --query value -o tsv | grep -qi true || az ad group member add --group "$GRUPO_ID" --member-id "$MI"
echo "  Aguardando as permissões propagarem (40 s)…"; sleep 40

titulo "5/7 Publicar no endereço novo"
ok "Publicar app.zip em https://$NOVO.azurewebsites.net." || exit 1
az webapp deploy -g "$RG" -n "$NOVO" --src-path app.zip --type zip -o none
for i in $(seq 1 30); do c=$(curl -s -o /dev/null -w "%{http_code}" "https://$NOVO.azurewebsites.net/saude" || true); [[ "$c" == "200" ]] && break; sleep 10; done
echo "  /saude → $c"
[[ "$c" == "200" ]] || { echo "  O site novo não respondeu. O antigo segue normal; me mande o resultado."; exit 1; }
echo "  Abra https://$NOVO.azurewebsites.net, entre com seu login e confira antes de seguir."

titulo "6/7 Endereço antigo redireciona para o novo"
if ok "Fazer https://$ANTIGO.azurewebsites.net redirecionar para https://$NOVO.azurewebsites.net (links antigos continuam valendo)."; then
  az webapp config appsettings set -g "$RG" -n "$ANTIGO" --settings Groundkey__RedirecionarPara="https://$NOVO.azurewebsites.net" -o none
  az webapp config set -g "$RG" -n "$ANTIGO" --startup-file "dotnet Groundkey.Web.dll" -o none
  az webapp deploy -g "$RG" -n "$ANTIGO" --src-path app.zip --type zip -o none
  sleep 20
  echo "  Teste: $(curl -s -o /dev/null -w "%{http_code} → %{redirect_url}" "https://$ANTIGO.azurewebsites.net/")"
fi

titulo "7/7 Publicação automática (GitHub)"
echo "  Se usa o GitHub Actions, rode depois: bash configurar-github.sh  (passa a publicar no site '$NOVO')."
echo
echo "  Pronto: https://$NOVO.azurewebsites.net"
