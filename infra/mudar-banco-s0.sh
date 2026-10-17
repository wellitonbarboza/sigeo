#!/usr/bin/env bash
# =====================================================================================
# SIGEO — muda o plano do banco Azure SQL para DTU Standard S0 (custo fixo mensal, sem pausa).
# Uso no Cloud Shell, dentro da pasta do pacote:   bash mudar-banco-s0.sh
#   PLANO=S1 bash mudar-banco-s0.sh        → usa S1 (20 DTU) em vez de S0 (10 DTU)
#   PLANO=Serverless bash mudar-banco-s0.sh → volta para vCore serverless (pausa sem uso)
# O que acontece:
#   - A troca é ONLINE: o banco continua no ar; no fim há uma reconexão de alguns segundos.
#   - Dados, usuários, permissões (Managed Identity), backups e endereço NÃO mudam.
#   - Pode levar de minutos a ~1 hora, conforme o tamanho do banco.
#   - Pode voltar a qualquer momento (mesmo comando com PLANO=Serverless).
# Pede confirmação antes de alterar. Antes de mudar, mostra o tamanho e o uso de CPU dos últimos 7 dias.
# =====================================================================================
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
PLANO="${PLANO:-S0}"
SQL=$(az sql server list -g "$RG" --query "[0].name" -o tsv)
DB=$(az sql db list -g "$RG" -s "$SQL" --query "[?name!='master'] | [0].name" -o tsv)
[[ -n "$SQL" && -n "$DB" ]] || { echo "Banco não encontrado em $RG."; exit 1; }

echo "Banco: $SQL/$DB"
az sql db show -g "$RG" -s "$SQL" -n "$DB" --query "{plano:currentServiceObjectiveName, camada:sku.tier, vCoresMax:sku.capacity, pausaMin:autoPauseDelay, tamanhoMaxGB:maxSizeBytes}" -o table
DBID=$(az sql db show -g "$RG" -s "$SQL" -n "$DB" --query id -o tsv)
USADO=$(az monitor metrics list --resource "$DBID" --metric storage --interval PT1H --offset 1h --query "value[0].timeseries[0].data[-1].maximum" -o tsv 2>/dev/null || true)
[[ -n "$USADO" ]] && echo "Espaço usado: $(awk "BEGIN{printf \"%.2f\", $USADO/1073741824}") GB"
echo "CPU nos últimos 7 dias (média e pico por dia, %):"
az monitor metrics list --resource "$DBID" --metric cpu_percent --interval P1D --offset 7d --aggregation Average Maximum \
  --query "value[0].timeseries[0].data[].{dia:timeStamp, media:average, pico:maximum}" -o table 2>/dev/null || true
echo
case "$PLANO" in
  S0) echo "S0 = 10 DTU (≈ 1/4 de vCore), até 250 GB. Atende poucas pessoas simultâneas; telas pesadas (planejamento, importações grandes) ficam mais lentas.";;
  S1) echo "S1 = 20 DTU, até 250 GB. Dobro do S0, custo ~2x.";;
  S2) echo "S2 = 50 DTU, até 250 GB.";;
  Serverless) echo "Serverless = vCore General Purpose, 0,5 a 1 vCore, pausa após 60 min sem uso.";;
  *) echo "PLANO inválido: $PLANO (use S0, S1, S2 ou Serverless)"; exit 1;;
esac
read -r -p "Mudar $DB para $PLANO agora? (s/N) " r; [[ "$r" =~ ^[sS]$ ]] || exit 0

if [[ "$PLANO" == "Serverless" ]]; then
  az sql db update -g "$RG" -s "$SQL" -n "$DB" --edition GeneralPurpose --compute-model Serverless --family Gen5 \
    --capacity 1 --min-capacity 0.5 --auto-pause-delay 60 -o none
else
  az sql db update -g "$RG" -s "$SQL" -n "$DB" --edition Standard --service-objective "$PLANO" -o none
fi
echo "Pedido enviado. Situação atual:"
az sql db show -g "$RG" -s "$SQL" -n "$DB" --query "{plano:currentServiceObjectiveName, pedido:requestedServiceObjectiveName, status:status}" -o table
echo
echo "Se 'plano' ainda mostrar o anterior, a troca está em andamento (o site segue funcionando)."
echo "Acompanhe com:  az sql db show -g $RG -s $SQL -n $DB --query currentServiceObjectiveName -o tsv"
echo "Depois de 1 a 2 semanas, confira o uso: Portal › banco › Métricas › 'DTU percentage'. Média acima de ~70% → S1."
