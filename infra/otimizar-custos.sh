#!/usr/bin/env bash
# SIGEO — ajustes de custo e operação no ambiente que já existe (sem recriar nada). Uso no Cloud Shell:
#   unzip -o sigeo-app.zip -d sigeo && cd sigeo && bash otimizar-custos.sh
# Cada etapa pergunta antes. Nada é apagado sem confirmação.
#   1. Para o site antigo (app-qualyhub-prd-…) que divide o plano com o "qualyhub" e mantinha o banco acordado
#   2. Teto do Log Analytics: 0,3 GB/dia (telemetria amostrada cabe nos 5 GB grátis/mês)
#   3. Retenção longa de backup do banco: 1 por mês por 12 meses, 1 por ano por 5 anos (auditoria SiAC)
#   4. Segredo CodigoPlataforma no Key Vault (cadastrar outras construtoras em /plataforma/nova-construtora)
#   5. Alertas por e-mail: CPU do banco, memória do servidor, erros 5xx
#   6. Mostra o uso do banco (S0/S1: % de DTU; serverless: se está pausando)
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
confirmar() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?starts_with(name,'sigeo')].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
PLANO=$(az appservice plan list -g "$RG" --query "[0].id" -o tsv)
SQL=$(az sql server list -g "$RG" --query "[0].name" -o tsv)
DB=$(az sql db list -g "$RG" -s "$SQL" --query "[?name!='master'] | [0].name" -o tsv)
LOGS=$(az monitor log-analytics workspace list -g "$RG" --query "[0].name" -o tsv)
KV=$(az keyvault list -g "$RG" --query "[0].name" -o tsv)
echo "Grupo $RG · site $WEB · banco $SQL/$DB · logs $LOGS · cofre $KV"

# 1) Site antigo
for s in $(az webapp list -g "$RG" --query "[?name!='$WEB' && state=='Running'].name" -o tsv); do
  [[ -n "$(az webapp config appsettings list -g "$RG" -n "$s" --query "[?ends_with(name,'__RedirecionarPara')].value | [0]" -o tsv)" ]] && { echo "  $s redireciona o endereço antigo: mantido"; continue; }
  confirmar "Parar o site antigo '$s' (o '$WEB' continua no ar)?" && az webapp stop -g "$RG" -n "$s" -o none && echo "  parado: $s"
done

# 2) Teto de log
confirmar "Teto do Log Analytics '$LOGS' para 0,3 GB/dia?" && \
  az monitor log-analytics workspace update -g "$RG" -n "$LOGS" --quota 0.3 -o none && echo "  ok"

# 3) Retenção longa de backup
confirmar "Retenção longa de backup (12 meses mensal, 5 anos anual) no banco '$DB'?" && \
  az sql db ltr-policy set -g "$RG" -s "$SQL" -n "$DB" --monthly-retention P12M --yearly-retention P5Y --week-of-year 1 -o none && echo "  ok"

# 4) Código da plataforma
if ! az keyvault secret show --vault-name "$KV" -n CodigoPlataforma -o none 2>/dev/null; then
  confirmar "Criar o segredo CodigoPlataforma no Key Vault?" && \
    az keyvault secret set --vault-name "$KV" -n CodigoPlataforma --value "$(openssl rand -hex 16)" -o none && echo "  ok"
fi

# 5) Alertas
if confirmar "Criar alertas por e-mail (CPU do banco, memória do servidor, erros 5xx)?"; then
  read -r -p "  E-mail que recebe os alertas: " EMAIL
  az monitor action-group create -g "$RG" -n ag-qualyhub --short-name qualyhub --action email principal "$EMAIL" -o none
  AG=$(az monitor action-group show -g "$RG" -n ag-qualyhub --query id -o tsv)
  DBID=$(az sql db show -g "$RG" -s "$SQL" -n "$DB" --query id -o tsv)
  WEBID=$(az webapp show -g "$RG" -n "$WEB" --query id -o tsv)
  az monitor metrics alert create -g "$RG" -n al-sql-cpu --scopes "$DBID" --condition "avg cpu_percent > 85" --window-size 15m --evaluation-frequency 5m --action "$AG" --description "Banco acima de 85% de CPU: avaliar plano maior (S0→S1 ou mais vCores)" -o none
  az monitor metrics alert create -g "$RG" -n al-web-memoria --scopes "$PLANO" --condition "avg MemoryPercentage > 85" --window-size 15m --evaluation-frequency 5m --action "$AG" --description "Servidor acima de 85% de memória: avaliar P0v3" -o none
  az monitor metrics alert create -g "$RG" -n al-web-5xx --scopes "$WEBID" --condition "total Http5xx > 10" --window-size 15m --evaluation-frequency 5m --action "$AG" --description "Mais de 10 erros 5xx em 15 min" -o none
  echo "  ok"
fi

# 6) Uso do banco
TIER=$(az sql db show -g "$RG" -s "$SQL" -n "$DB" --query sku.tier -o tsv)
DBID=$(az sql db show -g "$RG" -s "$SQL" -n "$DB" --query id -o tsv)
if [[ "$TIER" == "Standard" || "$TIER" == "Basic" ]]; then
  echo "Uso de DTU do banco por hora nas últimas 24 h (média acima de ~70% com frequência → subir de S1 para S2):"
  az monitor metrics list --resource "$DBID" --metric dtu_consumption_percent --interval PT1H --offset 1d \
    --query "value[0].timeseries[0].data[].{hora:timeStamp, media:average}" -o table || true
else
  echo "CPU faturada do banco nas últimas 24 h (zeros à noite = pausando):"
  az monitor metrics list --resource "$DBID" --metric app_cpu_billed \
    --interval PT1H --offset 1d --query "value[0].timeseries[0].data[].{hora:timeStamp, vcoreSeg:total}" -o table || true
fi
