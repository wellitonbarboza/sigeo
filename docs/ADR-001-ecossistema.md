# ADR-001 — Stack do QualyHub: Azure/.NET (Blazor) × Supabase

**Status:** Proposta — decidir antes da etapa 3
**Data:** 25/09/2026

## Contexto
Os apps e micro-SaaS existentes rodam em Supabase. A N Dalmina precisa de um sistema complementar ao Obra Prima para a certificação SiAC Nível B. Foi solicitada a construção em Blazor no Azure.

## Decisão proposta
Criar a **linha corporativa .NET/Azure** com o QualyHub como produto inicial, destinada a clientes que:
- já usam Microsoft 365 / Entra ID (login único, Teams, SharePoint);
- exigem ambiente "corporativo" para auditoria, Caixa ou investidores;
- têm ERP próprio (Obra Prima, Sienge, TOTVS) e precisam de uma camada de qualidade e aprovação.

Os micro-SaaS, apps pessoais e produtos de ticket baixo **continuam em Supabase**.

## Consequências
**Positivas**
- Integração nativa com o M365 e segurança corporativa (Managed Identity, Key Vault, Entra ID).
- Temporal tables do SQL Server com rastreabilidade forte para auditoria.
- C# de ponta a ponta: as mesmas regras rodam no servidor e no app de campo.

**Negativas / mitigação**
- **Duas stacks para manter.** Mitigação: o domínio fica isolado (`QualyHub.Domain` sem dependência de infraestrutura) e há padrões comuns de documentação.
- **Custo fixo maior.** Mitigação: tiers mínimos, serverless com auto-pause e orçamento com alerta (ver PONTOS-DE-ATENCAO.md §1).
- **Multiempresa:** o modelo já nasce com `EmpresaId` e filtro global, então o 2º cliente não exige refatoração.

## Critério para revisar a decisão
Se, em 6 meses, não houver um 2º cliente na linha corporativa, avaliar migrar o QualyHub para Supabase. O domínio é reaproveitável, e o custo é basicamente reescrever a infraestrutura e a UI.
