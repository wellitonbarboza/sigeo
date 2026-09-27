# QualyHub

Sistema de **suprimentos com qualidade, aprovações por alçada e controle documental do SGQ** para construtoras que buscam **PBQP-H / SiAC**. Complementa o ERP (Obra Prima): o ERP segue com orçamento, financeiro e fiscal; o QualyHub controla, aprova e evidencia.

Modelo e telas seguem o mesmo padrão do sistema de referência (Azure + Blazor): menu lateral, tema claro/escuro, grade com agrupamento/filtros/exportação, formulários em janela com etapas e faixa informativa no rodapé.

**Banco começa vazio.** Na primeira execução o sistema pede a empresa e o administrador e aplica uma carga **genérica** (listas, parâmetros, tipos de documento, materiais e serviços controlados e alçadas padrão SiAC). Tudo é editável em *Sistema › Parâmetros e listas* e *Sistema › Usuários e alçadas*. Nenhum dado de cliente vem no código.

## Módulos e telas

| Menu | Tela | Tabelas |
|---|---|---|
| Início | Pendências de aprovação, leituras, alertas do SGQ, primeiros passos | — |
| Cadastro | Fornecedores · Qualificação · Documentos do fornecedor (validação do portal) · Obras e centros de custo · Produtos · Materiais e serviços controlados | T08, T09, T10 |
| Suprimentos | Requisição → Orçamento (mapa comparativo) → Pedido / Contrato → Recebimento (FVM) → Medição (notas do prestador) → Liberação de pagamento (3 vias) | T01–T07, T11–T15, T18 |
| Qualidade | Documentos do SGQ (árvore, revisões, check-out, etapas com assinatura) · Cópias controladas e solicitações · Lista mestra · Externos · Avaliação semestral · Planos de ação · GRD · Leituras · Registros (retenção) | D01–D13, T16, T17 |
| Qualidade da Obra | Mapa de inspeções · FVS (inspeção, reinspeção, liberação) · Modelos de FVS/FVM · Não conformidades (RNC) · Ações corretivas · Equipamentos de medição (calibração) · Auditorias internas · Tela de campo `/campo/fvs/{id}` e QR do local `/campo/local/{qr}` | Q01–Q15 |
| Segurança e Pessoas | Colaboradores (próprios e terceiros, CPF mascarado) · Exigências por função · Documentos do colaborador · Treinamentos · Entrega de EPI (CA) · Inspeções de segurança · Permissão de trabalho · Ocorrências e acidentes · Painel SST | S01–S13 |
| Painéis | Minhas pendências (com delegação) · Painel de indicadores | T19–T28 |
| Sistema | Usuários, limites, alçadas e delegações · Parâmetros e listas · Permissões do SGQ · Log de auditoria | — |
| Público (sem login) | Portal do fornecedor `/portal/{token}` · Verificação de cópia `/qr/{token}` · Confirmação de GRD `/grd/confirmar/{token}` | — |

## Regras que a auditoria vai olhar

- **Alçadas configuráveis por login**: etapas por documento/valor/obra (congeladas no envio), limite de valor por usuário, **delegação** com período e **segregação** (quem solicita/elabora não aprova).
- **Travas de qualidade** (`Domain/Regras`): vencedor da cotação qualificado; pedido/contrato só com fornecedor qualificado e documentos válidos; recebimento de controlado com lote + documentos + FVM; medição de serviço controlado exige FVS aprovada; pagamento em 3 vias.
- **Avaliação do prestador** na medição (pesos e faixas parametrizados) gera plano de ação automático; FVM reprovada também.
- **Documentos do SGQ**: elaboração → análise crítica → aprovação → publicação; revisão publicada é imutável; a anterior vira obsoleta e as cópias físicas entram em recolhimento; leitura obrigatória gerada na publicação; todo acesso vai para o log.
- **Qualidade da obra**: FVS não conforme abre RNC e bloqueia a etapa seguinte do local; medição de serviço controlado só com **FVS liberada**; FVS só com equipamento calibrado; lote em quarentena até o ensaio; revisão da ITS coloca o modelo em revisão.
- **SST**: situação do colaborador calculada pelas exigências da função (ASO, NR, integração, EPI); PT só com executantes aptos para a atividade; EPI com CA vencido é bloqueado.
- **Aditivos de contrato**: limite de 25% (parametrizável), aprovação por alçada e aplicação no saldo do contrato.
- **Rastreabilidade**: `AuditoriaInterceptor` (antes/depois) + tabelas temporais; exclusão sempre lógica.

## Estrutura

```
src/QualyHub.Domain          entidades e regras (sem dependência de infraestrutura)
src/QualyHub.Infrastructure  EF Core 10 (SQL Server), serviços de caso de uso, carga padrão, migrations
src/QualyHub.Web             Blazor Web App (Interactive Server), login Entra ID ou local de desenvolvimento
tests/QualyHub.Tests         testes das regras (travas, aprovações, suprimentos, documentos)
db/QualyHub_Inicial.sql      script idempotente da migration inicial (revisão/DBA)
infra/                       Bicep + deploy (Azure)
```

## Rodar localmente

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<senha-forte>' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
dotnet build && dotnet test
# ajuste ConnectionStrings:QualyHub em src/QualyHub.Web/appsettings.Development.json (ou variável de ambiente)
dotnet run --project src/QualyHub.Web
```

Em desenvolvimento a migration é aplicada ao iniciar. Sem `AzureAd:ClientId` configurado, o login é local (escolha do usuário cadastrado) — **use só para testes**. Com `AzureAd` preenchido, o login é pelo Microsoft Entra ID.

## Publicar no Azure

```bash
az login --use-device-code            # feito pela própria pessoa responsável (nunca automatizado)
cd infra && ./deploy.sh "<assinatura>" "<email-alerta-custo>"
```

O `deploy.sh` é idempotente e roda em 8 etapas (pode isolar com `ETAPAS="4 7"`): grupo DBA do SQL → registro do app no Entra ID → Bicep (App Service B1, SQL serverless com auto-pause, Storage, Key Vault, App Insights, orçamento) → segredos no Key Vault → script `db/QualyHub_Inicial.sql` + usuário da Managed Identity → `dotnet publish` + `az webapp deploy`.

Proteções de produção (o app **recusa iniciar** sem elas):
- **Login só pelo Entra ID** (`AzureAd:TenantId/ClientId`); o login local fica bloqueado.
- **`CpfHmacChave`** no Key Vault (hash estável do CPF — LGPD).
- **Configuração inicial protegida** pelo segredo `CodigoConfiguracaoInicial` do Key Vault e pelo ObjectId do administrador.
- Arquivos no **Azure Blob** com Managed Identity (contêineres `documentos`, `sgq`, `evidencias`, `sensivel` para ASO, `importacoes`); nada é sobrescrito (versionamento + lixeira de 30 dias).
- Health check em `/saude`.

## Pendências conhecidas

- Envio de e-mails (pedido, GRD, portal, alertas) ainda é registro manual; entra com as Azure Functions.
- App de campo (FVS/FVM offline) e importação assíncrona do ERP: próximas etapas.
