# SIGEO — Sistema Integrado de Governança de Empreendimentos e Obras

*Governança imobiliária 360°: da incorporação ao pós-obra.*

Sistema de **suprimentos com qualidade, aprovações por alçada e controle documental do SGQ** para construtoras que buscam **PBQP-H / SiAC**. Complementa o ERP: o ERP segue com orçamento, financeiro e fiscal; o SIGEO controla, aprova e evidencia.

Modelo e telas seguem o mesmo padrão do sistema de referência (Azure + Blazor): menu lateral, tema claro/escuro, grade com agrupamento/filtros/exportação, formulários em janela com etapas e faixa informativa no rodapé.

**Banco começa vazio.** Na primeira execução o sistema pede a empresa e o administrador e aplica uma carga **genérica** (listas, parâmetros, tipos de documento, materiais e serviços controlados e alçadas padrão SiAC). Tudo é editável em *Sistema › Parâmetros e listas* e *Sistema › Usuários e alçadas*. Nenhum dado de cliente vem no código.

## Módulos e telas

Tabelas de cada módulo, colunas e telas que usam: [docs/BANCO.md](docs/BANCO.md).

| Menu | Tela | Pasta do módulo |
|---|---|---|
| Início | Pendências de aprovação, leituras, alertas do SGQ, primeiros passos | `Inicio` |
| Cadastro | Fornecedores · Qualificação · Documentos do fornecedor (validação do portal) · Obras e centros de custo · Produtos · Materiais e serviços controlados | `Cadastros` |
| Suprimentos | Requisição → Orçamento (mapa comparativo) → Pedido / Contrato → Recebimento (FVM) → Medição (notas do prestador) → Liberação de pagamento (3 vias) | `Suprimentos` |
| Qualidade | Documentos do SGQ (árvore, revisões, check-out, etapas com assinatura) · Cópias controladas e solicitações · Lista mestra · Externos · Avaliação semestral · Planos de ação · GRD · Leituras · Registros (retenção) | `Documentos` |
| Qualidade da Obra | Mapa de inspeções · FVS (inspeção, reinspeção, liberação) · Modelos de FVS/FVM · Não conformidades (RNC) · Ações corretivas · Equipamentos de medição (calibração) · Auditorias internas · Tela de campo `/campo/fvs/{id}` e QR do local `/campo/local/{qr}` | `Qualidade` |
| Segurança e Pessoas | Colaboradores (próprios e terceiros, CPF mascarado) · Exigências por função · Documentos do colaborador · Treinamentos · Entrega de EPI (CA) · Inspeções de segurança · Permissão de trabalho · Ocorrências e acidentes · Painel SST | `Sst` |
| Painéis | Minhas pendências (com delegação e tarefas) · Painel de indicadores | `Inicio · Sistema` |
| Engenharia | Empreendimento (condomínio, torres, SPE) · Tipologias e kits · Composições e orçamento · Mão de obra (apropriação) · Custo realizado | `Engenharia` |
| Planejamento | Redes e cronograma (Gantt) · Médio e curto prazo (Last Planner) · Acompanhamento · Diário de obra | `Planejamento` (telas em `Engenharia`) |
| Financeiro e Incorporação | Títulos, bancos e fluxo de caixa · Espelho de vendas, contratos, recebíveis e RET | `Financeiro` · `Incorporacao` |
| Gestão | Metas do setor, da pessoa e da equipe (placar) | `Colaboracao` |
| Colaboração | Tarefas internas (prazo, prorrogação justificada, confirmação de leitura, origem RNC/AC) · Notas por registro (histórico imutável) | `Colaboracao` |
| Sistema | Usuários, limites, alçadas e delegações · Parâmetros e listas · Permissões do SGQ · Log de auditoria | `Sistema` |
| Público (sem login) | Portal do fornecedor `/portal/{token}` · Verificação de cópia `/qr/{token}` · Confirmação de GRD `/grd/confirmar/{token}` | `Publico · PortalFornecedor` |

## Regras que a auditoria vai olhar

- **Alçadas configuráveis por login**: etapas por documento/valor/obra (congeladas no envio), limite de valor por usuário, **delegação** com período e **segregação** (quem solicita/elabora não aprova).
- **Travas de qualidade** (`Domain/Regras`): vencedor da cotação qualificado; pedido/contrato só com fornecedor qualificado e documentos válidos; recebimento de controlado com lote + documentos + FVM; medição de serviço controlado exige FVS aprovada; pagamento em 3 vias.
- **Avaliação do prestador** na medição (pesos e faixas parametrizados) gera plano de ação automático; FVM reprovada também.
- **Documentos do SGQ**: elaboração → análise crítica → aprovação → publicação; revisão publicada é imutável; a anterior vira obsoleta e as cópias físicas entram em recolhimento; leitura obrigatória gerada na publicação; todo acesso vai para o log.
- **Qualidade da obra**: FVS não conforme abre RNC e bloqueia a etapa seguinte do local; medição de serviço controlado só com **FVS liberada**; FVS só com equipamento calibrado; lote em quarentena até o ensaio; revisão da ITS coloca o modelo em revisão.
- **SST**: situação do colaborador calculada pelas exigências da função (ASO, NR, integração, EPI); PT só com executantes aptos para a atividade; EPI com CA vencido é bloqueado.
- **Cotação com 3 economias** (vs. ofertado, vs. último preço pago, vs. preço de referência) e **proposta do fornecedor pelo link** `/cotacao/{token}` (sem login; preços, prazo, frete, condição e validade).
- **Aditivos de contrato**: limite de 25% (parametrizável), aprovação por alçada e aplicação no saldo do contrato.
- **Rastreabilidade**: `AuditoriaInterceptor` (antes/depois) + tabelas temporais; exclusão sempre lógica.

## Estrutura

Mesmo nome de módulo em todas as camadas (Suprimentos, Cadastros, Engenharia, Planejamento, Qualidade, Documentos, Sst, Financeiro, Incorporacao, Fiscal, Colaboracao, Sistema…).

```
src/Sigeo.Domain/<Módulo>/          entidades (= tabelas) e regras; um arquivo por tipo; enums do módulo em Enums.cs
src/Sigeo.Application/<Módulo>/     *Servico (grava, regras do processo) e *Consultas (leituras de cada tela)
src/Sigeo.Infrastructure/Dados/     DbContext, Configuracoes/<Módulo>.cs (tamanhos, índices) e Migracoes/
src/Sigeo.Contratos/                catálogo de telas e perfis de acesso (menu e permissões)
src/Sigeo.Web/Components/Pages/<Módulo>/   telas (.razor + .razor.cs); a tela não abre o banco
src/Sigeo.Web.Client/               parte que roda no navegador (WebAssembly)
tests/Sigeo.Tests/<Módulo>/         testes das regras
tests/Sigeo.Tests.Integracao/<Módulo>/  testes no SQL Server real (inclui todas as consultas de tela)
docs/                               ARQUITETURA.md · BANCO.md (mapa do banco) · VSCODE.md (como trabalhar no código)
db/Sigeo_Migracoes.sql              script idempotente de todas as migrações (revisão/DBA)
infra/                              Bicep e scripts do Azure (Cloud Shell)
tools/mapa-banco/gerar.py           gera docs/BANCO.md a partir do código
```

## Rodar localmente (VS Code)

Passo a passo em [docs/VSCODE.md](docs/VSCODE.md). Resumo:

```bash
SIGEO_SQL_SENHA='<senha-forte>' docker compose up -d sql      # SQL Server local
# crie src/Sigeo.Web/appsettings.Development.local.json com ConnectionStrings:Sigeo (fica fora do Git)
dotnet build && dotnet test
dotnet run --project src/Sigeo.Web                              # ou F5 no VS Code
```

Em desenvolvimento a migração é aplicada ao iniciar e o banco vazio abre a configuração inicial (empresa + administrador com senha). O login é por usuário e senha do SIGEO.

## Publicar no Azure

```bash
az login --use-device-code            # feito pela própria pessoa responsável (nunca automatizado)
cd infra && ./deploy.sh "<assinatura>" "<email-alerta-custo>"
```

O `deploy.sh` é idempotente e roda em 8 etapas (pode isolar com `ETAPAS="4 7"`): grupo DBA do SQL → registro do app → Bicep (App Service B1, Azure SQL DTU Standard S1, Storage, Key Vault, App Insights, orçamento) → segredos no Key Vault → script `db/Sigeo_Migracoes.sql` + usuário da Managed Identity → `dotnet publish` + `az webapp deploy`. Para só atualizar a versão use `infra/atualizar-app.sh` (ver `infra/LEIA-ME-SUBIR.txt`).

Proteções de produção (o app **recusa iniciar** sem elas):
- **Login por usuário e senha do SIGEO** (senha com política mínima, troca no primeiro acesso, bloqueio por tentativas).
- **`CpfHmacChave`** no Key Vault (hash estável do CPF — LGPD).
- **Configuração inicial protegida** pelo segredo `CodigoConfiguracaoInicial` do Key Vault.
- Arquivos no **Azure Blob** com Managed Identity (contêineres `documentos`, `sgq`, `evidencias`, `sensivel` para ASO, `importacoes`); nada é sobrescrito (versionamento + lixeira de 30 dias).
- Health check em `/saude`.

## Pendências conhecidas

- Envio de e-mails (pedido, GRD, portal, alertas) ainda é registro manual; entra com as Azure Functions.
- App de campo (FVS/FVM offline) e importação assíncrona do ERP: próximas etapas.
