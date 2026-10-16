#!/usr/bin/env bash
# =====================================================================================
# SIGEO — cria o endereço https://sigeo.azurewebsites.net e faz os antigos
# (https://qualyhub.azurewebsites.net e, se existir, https://groundkey.azurewebsites.net) redirecionarem para ele.
# O nome de um App Service não pode ser trocado; por isso criamos um site "sigeo" no MESMO plano
# (sem custo extra no B1), com as mesmas configurações, permissões e banco. Os sites antigos recebem a mesma
# versão do app com Sigeo__RedirecionarPara: links de e-mail e favoritos antigos continuam funcionando,
# e as rotinas (avisos por e-mail, rotina das 6h30) passam a rodar só no site novo.
# Uso no Cloud Shell (Bash), com os 3 arquivos enviados:
#   cat sigeo-app.part0 sigeo-app.part1 sigeo-app.part2 > sigeo-app.zip
#   rm -rf sigeo && unzip -o sigeo-app.zip -d sigeo && cd sigeo && bash criar-endereco-sigeo.sh
# Cada etapa pede confirmação (s/N). Pode rodar de novo: é idempotente.
# =====================================================================================
set -euo pipefail
cd "$(dirname "$0")"

RG="${RG:-rg-qualyhub-prd}"
NOVO="${NOVO:-sigeo}"
GRUPO_DBA="QualyHub-DBA"
ok() { read -r -p "  → $1 Confirma? (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
titulo() { echo; echo "================ $1 ================"; }
redireciona() { [[ -n "$(az webapp config appsettings list -g "$RG" -n "$1" --query "[?ends_with(name,'__RedirecionarPara')].value | [0]" -o tsv)" ]]; }

titulo "1/7 Site atual"
# Origem das configurações: o site que está atendendo hoje (groundkey, se já foi criado e não redireciona; senão qualyhub).
ORIGEM=""
for s in groundkey qualyhub; do
  az webapp show -g "$RG" -n "$s" -o none 2>/dev/null || continue
  redireciona "$s" && continue
  ORIGEM="$s"; break
done
[[ -n "$ORIGEM" ]] || ORIGEM=$(az webapp list -g "$RG" --query "[?name!='$NOVO'] | [0].name" -o tsv)
[[ -n "$ORIGEM" ]] || { echo "Nenhum site encontrado em $RG."; exit 1; }
ANTIGOS=$(for s in qualyhub groundkey "$ORIGEM"; do az webapp show -g "$RG" -n "$s" -o none 2>/dev/null && echo "$s"; done | sort -u | tr '\n' ' ')
PLANO=$(az webapp show -g "$RG" -n "$ORIGEM" --query serverFarmId -o tsv)
echo "  Site em uso: $ORIGEM · plano: ${PLANO##*/}"
echo "  Sites que vão redirecionar: $ANTIGOS"
echo "  Novo endereço: https://$NOVO.azurewebsites.net"

titulo "2/7 Criar o site '$NOVO' no mesmo plano"
if az webapp show -g "$RG" -n "$NOVO" -o none 2>/dev/null; then echo "  Já existe."
else
  ok "Criar o App Service '$NOVO' (mesmo plano, sem custo adicional)." || exit 1
  az webapp create -g "$RG" -p "$PLANO" -n "$NOVO" --runtime "DOTNETCORE:10.0" --assign-identity "[system]" -o none \
    || { echo "  O nome '$NOVO' não está disponível no Azure. Rode com NOVO=sigeo-app bash criar-endereco-sigeo.sh"; exit 1; }
fi

titulo "3/7 Copiar configurações (com os nomes novos)"
ok "Copiar de '$ORIGEM' as configurações, a conexão com o banco e os ajustes (HTTPS, WebSockets, Always On, /saude). Chaves QualyHub__* e Groundkey__* viram Sigeo__*." || exit 1
az webapp identity assign -g "$RG" -n "$NOVO" -o none
az webapp config appsettings list -g "$RG" -n "$ORIGEM" -o json \
  | python3 -c '
import json,sys
d=json.load(sys.stdin); out={}
for x in d:
    n=x["name"]
    if n.endswith("RedirecionarPara"): continue
    for velho in ("QualyHub__","Groundkey__"):
        if n.startswith(velho): n="Sigeo__"+n[len(velho):]
    if n not in out or x["name"].startswith("Sigeo__"): out[n]=x["value"]
print(json.dumps([{"name":k,"value":v,"slotSetting":False} for k,v in out.items()]))' \
  > /tmp/sigeo-settings.json
az webapp config appsettings set -g "$RG" -n "$NOVO" --settings @/tmp/sigeo-settings.json -o none
rm -f /tmp/sigeo-settings.json
# A CLI devolve o valor como texto ou como objeto {value}, conforme a versão — aceita os dois.
CONN=""
for NOME in Sigeo Groundkey QualyHub; do
  [[ -n "$CONN" ]] && break
  CONN=$(az webapp config connection-string list -g "$RG" -n "$ORIGEM" --query "[?name=='$NOME'] | [0].value" -o tsv)
  [[ "$CONN" == *"Server="* || "$CONN" == *"server="* ]] || CONN=$(az webapp config connection-string list -g "$RG" -n "$ORIGEM" --query "[?name=='$NOME'] | [0].value.value" -o tsv)
done
[[ -n "$CONN" ]] || { echo "  Não achei a conexão com o banco no site $ORIGEM. Pare aqui e me avise."; exit 1; }
az webapp config connection-string set -g "$RG" -n "$NOVO" -t SQLAzure --settings Sigeo="$CONN" -o none
echo "  Conexão com o banco copiada."
az webapp config set -g "$RG" -n "$NOVO" --startup-file "dotnet Sigeo.Web.dll" --web-sockets-enabled true --http20-enabled true \
  --min-tls-version 1.2 --ftps-state Disabled --always-on "$(az webapp config show -g "$RG" -n "$ORIGEM" --query alwaysOn -o tsv)" -o none
az webapp update -g "$RG" -n "$NOVO" --https-only true --client-affinity-enabled true -o none
az webapp config set -g "$RG" -n "$NOVO" --generic-configurations '{"healthCheckPath": "/saude"}' -o none
az webapp config appsettings set -g "$RG" -n "$NOVO" --settings Sigeo__EnderecoSite="https://$NOVO.azurewebsites.net" -o none

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
[[ "$c" == "200" ]] || { echo "  O site novo não respondeu. Os antigos seguem normais; me mande o resultado."; exit 1; }
echo "  Abra https://$NOVO.azurewebsites.net, entre com seu login e confira antes de seguir."

titulo "6/7 Endereços antigos redirecionam para o novo"
if ok "Fazer $ANTIGOS redirecionar(em) para https://$NOVO.azurewebsites.net (links antigos continuam valendo)."; then
  for s in $ANTIGOS; do
    [[ "$s" == "$NOVO" ]] && continue
    # Tira a marca antiga de redirecionamento (Groundkey__/QualyHub__) para valer só a nova.
    VELHAS=$(az webapp config appsettings list -g "$RG" -n "$s" --query "[?ends_with(name,'__RedirecionarPara')].name" -o tsv | tr '\n' ' ')
    [[ -n "${VELHAS// /}" ]] && az webapp config appsettings delete -g "$RG" -n "$s" --setting-names $VELHAS -o none
    az webapp config appsettings set -g "$RG" -n "$s" --settings Sigeo__RedirecionarPara="https://$NOVO.azurewebsites.net" -o none
    az webapp config set -g "$RG" -n "$s" --startup-file "dotnet Sigeo.Web.dll" -o none
    az webapp deploy -g "$RG" -n "$s" --src-path app.zip --type zip -o none
    sleep 20
    echo "  $s: $(curl -s -o /dev/null -w "%{http_code} → %{redirect_url}" "https://$s.azurewebsites.net/")"
  done
fi

titulo "7/7 Publicação automática (GitHub)"
echo "  Se usa o GitHub Actions, rode depois: bash configurar-github.sh  (passa a publicar no site '$NOVO')."
echo
echo "  Pronto: https://$NOVO.azurewebsites.net"
