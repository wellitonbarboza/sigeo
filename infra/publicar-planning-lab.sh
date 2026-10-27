#!/usr/bin/env bash
# Ferramenta privada (Planning Lab ou Simulador de Sequência) num caminho escondido do endereço do SIGEO.
# O que estiver no zip substitui o que estava publicado no caminho (os arquivos antigos são apagados).
# Não é módulo: sem menu, sem banco. Só abre para quem estiver logado no SIGEO com e-mail liberado.
# Os arquivos ficam no contêiner privado "ferramentas" (prefixo planning-lab/). Atualizar o Planning Lab
# NÃO republica o SIGEO: rode este script de novo com o zip novo.
#
# Uso no Cloud Shell (ou na VM, depois de az login):
#   bash publicar-planning-lab.sh PlanningLab_publish_wwwroot.zip [seu-email-de-login-no-sigeo]
#
# Variáveis opcionais: RG (padrão rg-qualyhub-prd), WEB (site), ST (conta de armazenamento), CAMINHO (forçar o caminho).
set -euo pipefail
ZIP="${1:?Informe o zip do Planning Lab (PlanningLab_publish_wwwroot.zip)}"
EMAIL_ARG="${2:-}"
RG="${RG:-rg-qualyhub-prd}"
CONT="ferramentas"
PREFIXO="planning-lab"

WEB="${WEB:-$(az webapp list -g "$RG" --query "([?starts_with(name,'sigeo')].name)[0]" -o tsv)}"
[[ -n "$WEB" ]] || { echo "Site do SIGEO não encontrado em $RG."; exit 1; }
URL="https://$(az webapp show -g "$RG" -n "$WEB" --query defaultHostName -o tsv)"
ST="${ST:-$(az storage account list -g "$RG" --query "[0].name" -o tsv)}"
[[ -n "$ST" ]] || { echo "Conta de armazenamento não encontrada em $RG."; exit 1; }

# 1) Conferir o pacote
TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT
unzip -q "$ZIP" -d "$TMP"
SRC="$TMP/wwwroot"; [[ -d "$SRC" ]] || SRC="$TMP"
find "$SRC" \( -name '*.br' -o -name '*.gz' \) -delete   # o SIGEO entrega os arquivos sem compressão prévia
[[ -f "$SRC/index.html" ]] || { echo "Zip inválido: esperado index.html (na raiz ou em wwwroot/)."; exit 1; }
QTD=$(find "$SRC" -type f | wc -l | tr -d ' ')

# 2) Caminho escondido e e-mails liberados (mantém os atuais se já existirem)
ATUAL_CAM=$(az webapp config appsettings list -g "$RG" -n "$WEB" --query "[?name=='PlanningLab__Caminho'].value | [0]" -o tsv)
ATUAL_EM=$(az webapp config appsettings list -g "$RG" -n "$WEB" --query "[?name=='PlanningLab__Emails'].value | [0]" -o tsv)
CAM="${CAMINHO:-${ATUAL_CAM:-/x/pl-$(openssl rand -hex 6)}}"
EM="${EMAIL_ARG:-$ATUAL_EM}"
if [[ -z "$EM" ]]; then read -r -p "E-mail do seu login no SIGEO (só ele poderá abrir): " EM; fi
[[ "$EM" == *@* ]] || { echo "E-mail inválido."; exit 1; }

echo
echo "Site ............ $WEB ($URL)"
echo "Armazenamento ... $ST / contêiner privado '$CONT' / $PREFIXO/ ($QTD arquivos)"
echo "Endereço ........ $URL$CAM/"
echo "Liberado para ... $EM"
read -r -p "Publicar? (s/N) " r; [[ "$r" =~ ^[sS]$ ]] || exit 1

# 3) Contêiner privado (plano de gerenciamento: não precisa de chave da conta, que é desativada)
az storage container-rm create -g "$RG" --storage-account "$ST" -n "$CONT" --public-access off -o none 2>/dev/null || true

# 4) Sua permissão de escrita, só neste contêiner (a do site já existe: Blob Data Contributor na conta)
ME=$(az ad signed-in-user show --query id -o tsv)
SCOPE="$(az storage account show -g "$RG" -n "$ST" --query id -o tsv)/blobServices/default/containers/$CONT"
az role assignment create --assignee-object-id "$ME" --assignee-principal-type User \
  --role "Storage Blob Data Contributor" --scope "$SCOPE" -o none 2>/dev/null || true

# 5) Envio (a permissão nova pode levar alguns minutos para valer: tenta de novo)
echo "Enviando arquivos…"
for i in $(seq 1 20); do
  if az storage blob upload-batch --account-name "$ST" --auth-mode login -d "$CONT" --destination-path "$PREFIXO" \
       -s "$SRC" --overwrite --only-show-errors -o none 2>/tmp/pl-upload.err; then OK=1; break; fi
  OK=0; echo "  aguardando a permissão propagar ($i/20)…"; sleep 20
done
[[ "$OK" == 1 ]] || { cat /tmp/pl-upload.err; exit 1; }

# 6) Remove arquivos de versões antigas que não existem mais no pacote novo
NOVOS=$(cd "$SRC" && find . -type f | sed "s#^\./#$PREFIXO/#" | sort)
ANTIGOS=$(az storage blob list --account-name "$ST" --auth-mode login -c "$CONT" --prefix "$PREFIXO/" --query "[].name" -o tsv --num-results "*" | sort)
comm -13 <(echo "$NOVOS") <(echo "$ANTIGOS") | while read -r b; do
  [[ -n "$b" ]] && az storage blob delete --account-name "$ST" --auth-mode login -c "$CONT" -n "$b" -o none
done

# 7) Configuração do site (só grava se mudou: gravar reinicia o SIGEO)
if [[ "$CAM" != "$ATUAL_CAM" || "$EM" != "$ATUAL_EM" ]]; then
  az webapp config appsettings set -g "$RG" -n "$WEB" -o none --settings "PlanningLab__Caminho=$CAM" "PlanningLab__Emails=$EM"
  echo "Configuração gravada (o SIGEO reinicia em ~1 min)."
fi

echo
echo "Pronto. Planning Lab: $URL$CAM/"
echo "Entre no SIGEO com $EM e abra o endereço acima (salve nos favoritos ou instale pelo ícone do navegador)."
echo "Para trocar o endereço: CAMINHO=/x/pl-novo123 bash publicar-planning-lab.sh $ZIP"
echo "Para desligar: az webapp config appsettings delete -g $RG -n $WEB --setting-names PlanningLab__Caminho"
