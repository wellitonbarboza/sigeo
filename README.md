# QualyHub

Camada de **qualidade, aprovações e controle documental** que complementa o ERP **Obra Prima** para a certificação **PBQP-H / SiAC Nível B**. Primeiro cliente: **N Dalmina**.

O Obra Prima continua sendo o ERP (orçamento, compras, financeiro, medições). O QualyHub importa os dados básicos dele, **controla, aprova e evidencia**, e amarra o SGQ ao fluxo operacional.

## O que está nesta entrega (etapas 1 e 2)

| Etapa | Conteúdo | Status |
|---|---|---|
| 1. Modelo de dados | Domínio, EF Core 10, auditoria, travas de qualidade, importação | ✅ compila, migration gerada, 10 testes passando |
| 2. Provisionamento Azure | Bicep + script de deploy + permissões SQL | ✅ Bicep validado |
| 3. Web (Blazor Server) | Escritório: aprovações, medições, cadastros, SGQ, painel | próxima |
| 4. Campo (Blazor WASM PWA) | FVS/FVM offline | seguinte |
| 5. Functions | Alertas de vencimento, importação assíncrona | seguinte |

## Estrutura

```
QualyHub/
├── QualyHub.slnx
├── src/
│   ├── QualyHub.Domain/            # Entidades e regras (sem dependência de infraestrutura)
│   │   ├── Common/                 # Base auditável, enums, RegistroAuditoria
│   │   ├── Estrutura/              # Empresa, Obra, Local, Usuário, Alçada
│   │   ├── Cadastros/              # Fornecedor, Funcionário, Documentos, Serviço/Material controlado
│   │   ├── Suprimentos/            # Pedido (importado do Obra Prima)
│   │   ├── Medicoes/               # Medição (importada do Obra Prima)
│   │   ├── Qualidade/              # Construtor de formulários, FVS/FVM, RNC
│   │   ├── Sgq/                    # Lista mestra, revisões, distribuição/obsoletos
│   │   ├── Aprovacoes/             # Motor genérico de aprovação por alçada
│   │   ├── Importacao/             # Layout, lote e linhas (staging)
│   │   └── Regras/                 # TravasQualidade: pedido, medição, recebimento, acesso
│   └── QualyHub.Infrastructure/
│       ├── Dados/                  # DbContext, configurações, AuditoriaInterceptor, Migracoes/
│       ├── Importacao/             # Leitor .xlsx/.csv, importador de fornecedores, ProtecaoCpf
│       └── DependencyInjection.cs
├── tests/QualyHub.Tests/           # Testes das travas, aprovação e conversões
├── db/QualyHub_Inicial.sql         # Script SQL idempotente (para revisão/DBA)
├── infra/
│   ├── main.bicep                  # App Service, Functions, Azure SQL serverless, Storage, Key Vault, App Insights, orçamento
│   ├── main.prd.bicepparam
│   ├── deploy.sh                   # Provisiona, cria segredo, aplica migrations e usuários SQL
│   └── sql/usuarios-identidades.sql
└── docs/
    ├── PONTOS-DE-ATENCAO.md        # Custo, offline, ecossistema, LGPD, prazo da Caixa
    ├── ADR-001-ecossistema.md      # Decisão Azure/.NET x Supabase
    └── EXPORTS-OBRA-PRIMA.md       # O que pedir de export à N Dalmina
```

## Decisões de arquitetura que importam para a auditoria

- **Rastreabilidade em duas camadas.** O `AuditoriaInterceptor` grava quem, quando e antes/depois em `qh.Auditoria` (append-only, com `DENY UPDATE` no banco). As entidades críticas (fornecedor, documentos, pedido, medição, fichas, RNC, SGQ, aprovações) usam **temporal tables** e provam o estado em qualquer data.
- **Nada some.** Exclusão é sempre lógica, e o banco não concede `DELETE` no schema.
- **Travas de qualidade** (`Domain/Regras/TravasQualidade.cs`):
  - Pedido de **material controlado** exige fornecedor qualificado e sem documento bloqueante vencido.
  - **Medição** de serviço controlado exige **FVS aprovada** do mesmo serviço e do mesmo local.
  - **Recebimento** de material controlado só é aceito via **FVM** liberada.
  - **Acesso de funcionário** à obra exige documentos/NRs válidos.
- **Qualificação do fornecedor nunca vem da importação.** É decisão da Qualidade dentro do QualyHub.
- **Fichas imutáveis após envio.** Correção é feita por **reinspeção**, com uma nova ficha vinculada à original.
- **Filtros globais** por empresa (multiempresa pronto) e por obra (usuário só vê as obras dele).

## Rodar localmente

```bash
# SQL Server local
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Dev@12345' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

dotnet tool install --global dotnet-ef --version 10.0.12
dotnet build
dotnet test
dotnet ef database update -p src/QualyHub.Infrastructure -s src/QualyHub.Infrastructure
```

## Provisionar no Azure

```bash
cd infra
# edite main.prd.bicepparam (e-mails do alerta, orçamento)
./deploy.sh "<assinatura>" prd rg-qualyhub-prd
```

Veja `docs/PONTOS-DE-ATENCAO.md` antes do primeiro deploy.
