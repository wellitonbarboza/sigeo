#!/usr/bin/env bash
# SIGEO — sobe TUDO numa única sessão do Cloud Shell, na ordem certa. Cada passo pergunta antes (Enter = pula).
#   cat sigeo-app.part* > sigeo-app.zip && rm -rf sigeo && unzip -o sigeo-app.zip -d sigeo && cd sigeo && bash subir-tudo.sh
# Ordem: chave da Syncfusion → IA → SIEG → comunicação → publicar o app (migrations sozinhas) → custos → GitHub → inventário.
set -uo pipefail
cd "$(dirname "$0")"
RG="${RG:-rg-qualyhub-prd}"; export RG
WEB="${WEB:-$(az webapp list -g "$RG" --query "([?name=='sigeo'].name)[0] || ([?name=='groundkey'].name)[0] || ([?name=='qualyhub'].name)[0]" -o tsv)}"
[[ -n "$WEB" ]] || WEB=$(az webapp list -g "$RG" --query "[0].name" -o tsv); export WEB
KV=$(az keyvault list -g "$RG" --query '[0].name' -o tsv)
sim() { read -r -p "$1 (s/N) " r; [[ "$r" =~ ^[sS]$ ]]; }
passo() { echo; echo "════════ $1 ════════"; }
config() { az webapp config appsettings list -g "$RG" -n "$WEB" --query "[?name=='$1'].value | [0]" -o tsv 2>/dev/null; }
echo "Grupo $RG · site $WEB · Key Vault $KV"

passo "1/8 Chave da licença Syncfusion (Gantt do planejamento)"
if az keyvault secret show --vault-name "$KV" -n Syncfusion--ChaveLicenca -o none 2>/dev/null; then
  echo "Já existe no Key Vault."; sim "Trocar a chave?" && TROCAR=1 || TROCAR=0
else TROCAR=1; fi
if [[ "$TROCAR" == 1 ]]; then
  read -r -s -p "Cole a chave (Enter para pular — o Gantt mostra o aviso de avaliação até ter a chave): " CHAVE; echo
  [[ -n "$CHAVE" ]] && az keyvault secret set --vault-name "$KV" -n Syncfusion--ChaveLicenca --value "$CHAVE" -o none && echo "  gravada (vale no próximo início do site)"
  unset CHAVE
fi

passo "2/8 IA (Azure OpenAI + Document Intelligence)"
if [[ -n "$(config IA__Endpoint)" ]]; then echo "IA já configurada."; sim "Rodar de novo mesmo assim?" && bash configurar-ia.sh
else sim "Configurar a IA agora?" && bash configurar-ia.sh; fi

passo "3/8 SIEG (notas fiscais de entrada)"
sim "Já tem a API Key, o Client ID e a Secret Key do SIEG?" && bash configurar-sieg.sh

passo "4/8 Comunicação (e-mail/WhatsApp da Área do Fornecedor)"
if [[ -n "$(config Sigeo__Comunicacao__Endpoint)$(config Groundkey__Comunicacao__Endpoint)$(config QualyHub__Comunicacao__Endpoint)" ]]; then echo "Comunicação já configurada."
else sim "Configurar o envio de e-mail agora?" && bash configurar-comunicacao.sh; fi

passo "5/8 Publicar a nova versão do app (o banco é atualizado sozinho)"
bash atualizar-app.sh || { echo "Publicação não concluída — os passos seguintes podem esperar."; }

passo "6/8 Custos (plano do banco S1, teto de log, alertas)"
sim "Mudar o banco para S1 (DTU, custo fixo, 250 GB)?" && bash mudar-plano-banco.sh
sim "Rodar a otimização de custos?" && bash otimizar-custos.sh

passo "7/8 Publicação automática pelo GitHub"
if sim "Ligar a publicação automática (repositório wellitonbarboza/qualyhub)?"; then
  read -r -p "Repositório [wellitonbarboza/qualyhub]: " R; REPO="${R:-wellitonbarboza/qualyhub}" bash configurar-github.sh
fi

passo "8/8 Inventário de recursos (só lista; exclusões pedem confirmação)"
sim "Ver o inventário agora?" && bash inventario-limpeza.sh

URL="https://$(az webapp show -g "$RG" -n "$WEB" --query defaultHostName -o tsv)"
echo; echo "Pronto. Confira: $URL/saude · $URL/engenharia/planejamento · $URL/notas-fiscais"
