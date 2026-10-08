#!/usr/bin/env bash
# QualyHub — inventário do Azure e limpeza do que ficou do app antigo. Uso no Cloud Shell:
#   bash inventario-limpeza.sh
# 1) Lista TODOS os recursos da assinatura (grupo, tipo, nome) — para ver o que sobrou de testes e da versão antiga.
# 2) Mostra qual banco o site atual usa e marca os outros bancos / sites / planos como "candidatos".
# 3) Para cada candidato pergunta se apaga. Nada é apagado sem "s". Banco apagado ainda pode ser restaurado pelo
#    portal durante a retenção de backup (7 dias); se quiser guardar para sempre, responda "n" e exporte antes.
set -euo pipefail
RG="${RG:-rg-qualyhub-prd}"
WEB="${WEB:-$(az webapp list -g "$RG" --query "[?name=='qualyhub'] | [0].name" -o tsv)}"
confirmar() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }

echo "== Recursos da assinatura =="
az resource list --query "sort_by([].{grupo:resourceGroup, tipo:type, nome:name, local:location}, &grupo)" -o table

echo; echo "== Site em uso: $WEB =="
CONN=$(az webapp config connection-string list -g "$RG" -n "$WEB" --query "[?name=='QualyHub'].value | [0]" -o tsv 2>/dev/null || true)
DB_USO=$(echo "$CONN" | sed -n 's/.*\(Initial Catalog\|Database\)=\([^;]*\).*/\2/p')
echo "Banco usado pelo site: ${DB_USO:-desconhecido}"

echo; echo "== Candidatos =="
for s in $(az webapp list --query "[?name!='$WEB'].[resourceGroup,name]" -o tsv | tr '\t' '|'); do
  g="${s%%|*}"; n="${s#*|}"
  st=$(az webapp show -g "$g" -n "$n" --query state -o tsv)
  echo "Site: $g/$n ($st)"
  confirmar "  Apagar o site $n? (não apaga banco nem arquivos)" && az webapp delete -g "$g" -n "$n" --keep-empty-plan -o none && echo "  apagado"
done
for srv in $(az sql server list --query "[].[resourceGroup,name]" -o tsv | tr '\t' '|'); do
  g="${srv%%|*}"; n="${srv#*|}"
  for d in $(az sql db list -g "$g" -s "$n" --query "[?name!='master'].name" -o tsv); do
    [ "$d" = "$DB_USO" ] && { echo "Banco: $n/$d  (EM USO — mantido)"; continue; }
    tam=$(az sql db show -g "$g" -s "$n" -n "$d" --query "maxSizeBytes" -o tsv)
    echo "Banco: $g/$n/$d (não usado pelo site atual)"
    confirmar "  Apagar o banco $d? (restaurável pelo portal por 7 dias)" && az sql db delete -g "$g" -s "$n" -n "$d" --yes -o none && echo "  apagado"
  done
done
for p in $(az appservice plan list --query "[].[resourceGroup,name,numberOfSites]" -o tsv | tr '\t' '|'); do
  IFS='|' read -r g n q <<<"$p"
  [ "$q" = "0" ] || continue
  echo "Plano vazio: $g/$n (cobra mesmo sem site)"
  confirmar "  Apagar o plano $n?" && az appservice plan delete -g "$g" -n "$n" --yes -o none && echo "  apagado"
done
echo; echo "Feito. Grupos inteiros de teste (se houver) apague pelo portal depois de conferir a lista acima."
