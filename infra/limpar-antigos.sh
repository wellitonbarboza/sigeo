#!/usr/bin/env bash
# =====================================================================================
# SIGEO — limpeza final: deixa no Azure só o que o SIGEO usa e apaga o que ficou do QualyHub/Groundkey.
# Rode SÓ DEPOIS de: criar-endereco-sigeo.sh (site "sigeo" no ar) + teste de login em https://sigeo.azurewebsites.net.
# Uso no Cloud Shell:   bash limpar-antigos.sh
# Só mexe no grupo de recursos $RG (padrão rg-qualyhub-prd). Outros grupos: veja com inventario-limpeza.sh.
#
# MANTÉM (é o que o SIGEO usa — os nomes antigos não podem ser trocados no Azure sem recriar):
#   - o site "sigeo" e o plano do App Service dele
#   - o banco que o site "sigeo" usa (com TODOS os dados) e o servidor SQL
#   - Key Vault, Storage (anexos), Log Analytics/App Insights, IA, Comunicação, alertas e orçamento
#   - o grupo de recursos rg-qualyhub-prd e o grupo QualyHub-DBA (são só nomes; renomear exigiria recriar tudo)
# APAGA (cada item pergunta antes; sem "s", nada acontece):
#   - sites antigos: qualyhub, groundkey, app-qualyhub-prd-… e qualquer outro site que não seja o "sigeo"
#     → os endereços antigos (qualyhub.azurewebsites.net / groundkey…) PARAM DE FUNCIONAR (links velhos de e-mail quebram)
#   - bancos que o "sigeo" NÃO usa (restauráveis pelo portal por 7 dias depois de apagados)
#   - planos de App Service que ficarem sem nenhum site (eles cobram mesmo vazios)
#   - permissões que sobram das identidades dos sites apagados
# =====================================================================================
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
NOVO="${NOVO:-sigeo}"
GRUPO_DBA="QualyHub-DBA"
confirmar() { read -r -p "  → $1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
titulo() { echo; echo "================ $1 ================"; }

titulo "1/5 Conferir o SIGEO antes de apagar qualquer coisa"
az webapp show -g "$RG" -n "$NOVO" -o none 2>/dev/null || { echo "  O site '$NOVO' não existe. Rode criar-endereco-sigeo.sh primeiro. Nada foi apagado."; exit 1; }
URL="https://$(az webapp show -g "$RG" -n "$NOVO" --query defaultHostName -o tsv)"
c=$(curl -s -o /dev/null -w "%{http_code}" "$URL/saude" || true)
echo "  $URL/saude → $c"
[[ "$c" == "200" ]] || { echo "  O SIGEO não está respondendo. Nada foi apagado."; exit 1; }
[[ -z "$(az webapp config appsettings list -g "$RG" -n "$NOVO" --query "[?ends_with(name,'__RedirecionarPara')].value | [0]" -o tsv)" ]] \
  || { echo "  O site '$NOVO' está configurado para redirecionar — não é o site principal. Nada foi apagado."; exit 1; }
PLANO_NOVO=$(az webapp show -g "$RG" -n "$NOVO" --query serverFarmId -o tsv)
CONN=""
for NOME in Sigeo Groundkey QualyHub; do
  [[ -n "$CONN" ]] && break
  CONN=$(az webapp config connection-string list -g "$RG" -n "$NOVO" --query "[?name=='$NOME'] | [0].value" -o tsv 2>/dev/null || true)
  [[ "$CONN" == *"atabase="* || "$CONN" == *"atalog="* ]] || CONN=$(az webapp config connection-string list -g "$RG" -n "$NOVO" --query "[?name=='$NOME'] | [0].value.value" -o tsv 2>/dev/null || true)
done
DB_USO=$(echo "$CONN" | sed -n 's/.*\(Initial Catalog\|Database\)=\([^;]*\).*/\2/Ip')
SRV_USO=$(echo "$CONN" | sed -n 's/.*\(Server\|Data Source\)=\(tcp:\)\{0,1\}\([^.,;]*\).*/\3/Ip' | tr '[:upper:]' '[:lower:]')
[[ -n "$DB_USO" && -n "$SRV_USO" ]] || { echo "  Não consegui ler qual banco o SIGEO usa. Nada foi apagado — me mande esta tela."; exit 1; }
echo "  SIGEO usa o banco: $SRV_USO/$DB_USO  (será MANTIDO)"
echo "  Plano do SIGEO: ${PLANO_NOVO##*/}  (será MANTIDO)"
echo
echo "  Antes de seguir: entrou em $URL com seu login e viu suas obras/dados? Se não, pare aqui (Ctrl+C)."
confirmar "Confirmo que o SIGEO novo está funcionando com os meus dados." || { echo "  Nada foi apagado."; exit 0; }

titulo "2/5 Sites antigos"
APAGADOS_MI=()
for s in $(az webapp list -g "$RG" --query "[?name!='$NOVO'].[resourceGroup,name]" -o tsv | tr '\t' '|'); do
  g="${s%%|*}"; n="${s#*|}"
  echo "  Site: $g/$n  ($(az webapp show -g "$g" -n "$n" --query state -o tsv))  https://$n.azurewebsites.net"
  if confirmar "Apagar o site '$n'? O endereço dele para de funcionar."; then
    MI=$(az webapp identity show -g "$g" -n "$n" --query principalId -o tsv 2>/dev/null || true)
    [[ -n "$MI" ]] && APAGADOS_MI+=("$MI")
    az webapp delete -g "$g" -n "$n" --keep-empty-plan -o none && echo "    apagado"
  fi
done
[[ $(az webapp list -g "$RG" --query "length([?name!='$NOVO'])" -o tsv) == "0" ]] && echo "  Nenhum site antigo restante."

titulo "3/5 Bancos que o SIGEO não usa"
for srv in $(az sql server list -g "$RG" --query "[].[resourceGroup,name]" -o tsv | tr '\t' '|'); do
  g="${srv%%|*}"; n="${srv#*|}"
  for d in $(az sql db list -g "$g" -s "$n" --query "[?name!='master'].name" -o tsv); do
    if [[ "$n" == "$SRV_USO" && "$d" == "$DB_USO" ]]; then echo "  Banco: $n/$d  (EM USO PELO SIGEO — mantido)"; continue; fi
    echo "  Banco: $g/$n/$d  (o SIGEO não usa)"
    confirmar "Apagar o banco '$d'? (restaurável pelo portal por 7 dias)" && az sql db delete -g "$g" -s "$n" -n "$d" --yes -o none && echo "    apagado"
  done
  if [[ "$n" != "$SRV_USO" && -z "$(az sql db list -g "$g" -s "$n" --query "[?name!='master'].name" -o tsv)" ]]; then
    confirmar "O servidor SQL '$n' ficou vazio. Apagar o servidor?" && az sql server delete -g "$g" -n "$n" --yes -o none && echo "    apagado"
  fi
done

titulo "4/5 Planos de App Service sem site"
for p in $(az appservice plan list -g "$RG" --query "[].[resourceGroup,name,id]" -o tsv | tr '\t' '|'); do
  g="${p%%|*}"; r="${p#*|}"; n="${r%%|*}"; id="${r#*|}"
  [[ "$id" == "$PLANO_NOVO" ]] && { echo "  Plano: $n (do SIGEO — mantido)"; continue; }
  qtd=$(az webapp list --query "length([?serverFarmId=='$id'])" -o tsv)
  if [[ "$qtd" == "0" ]]; then
    echo "  Plano: $g/$n — sem nenhum site (cobra mesmo vazio)"
    confirmar "Apagar o plano '$n'?" && az appservice plan delete -g "$g" -n "$n" --yes -o none && echo "    apagado"
  else echo "  Plano: $n — ainda tem $qtd site(s), mantido"; fi
done

titulo "5/5 Permissões que sobraram dos sites apagados"
if (( ${#APAGADOS_MI[@]} )); then
  GRUPO_ID=$(az ad group show --group "$GRUPO_DBA" --query id -o tsv 2>/dev/null || true)
  for mi in "${APAGADOS_MI[@]}"; do
    for a in $(az role assignment list --all --query "[?principalId=='$mi'].id" -o tsv 2>/dev/null); do az role assignment delete --ids "$a" -o none || true; done
    [[ -n "$GRUPO_ID" ]] && az ad group member remove --group "$GRUPO_ID" --member-id "$mi" 2>/dev/null || true
  done
  echo "  Permissões das identidades apagadas removidas."
else echo "  Nada a limpar."; fi

echo
echo "Ficou no Azure:"
az resource list -g "$RG" --query "sort_by([].{tipo:type, nome:name}, &tipo)" -o table
echo
echo "Pronto. Endereço único: $URL"
echo "Se usa o GitHub Actions, confira que a variável AZURE_WEBAPP = $NOVO (bash configurar-github.sh ajusta)."
