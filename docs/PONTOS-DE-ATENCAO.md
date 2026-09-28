# QualyHub — Pontos de atenção e como foram tratados

## 1. Custo do Azure

**Risco:** o App Service e o Azure SQL têm custo fixo mensal maior que o Supabase no início.

**O que já está no código de infraestrutura (`infra/main.bicep`):**

| Medida | Efeito |
|---|---|
| Um único plano **App Service B1** para Web **e** Functions | Não há segundo plano nem Functions Premium |
| **Azure SQL serverless** (`GP_S_Gen5`, 0,5–1 vCore, **auto-pause em 60 min**) | Fora do expediente o banco pausa e o custo de computação cai para zero |
| Storage **LRS** com regra de ciclo de vida: evidências e imports vão para **Cool (90 dias)** e **Cold (1 ano)** | Fotos de FVS/FVM crescem rápido, e o armazenamento barato absorve isso |
| Log Analytics com **teto diário de 1 GB** e retenção de 30 dias; App Insights com amostragem de 50% | Log não vira surpresa na fatura |
| **Orçamento mensal** com alertas em 80% e 100% (real) e 100% (previsto) | Aviso antes de estourar |

**Na prática:**
- Rode a [calculadora do Azure](https://azure.microsoft.com/pricing/calculator/) com esse desenho **antes** de fechar o preço com a N Dalmina, porque os valores mudam e dependem de câmbio.
- O auto-pause gera uma **espera de alguns segundos** na primeira consulta depois da pausa. O `EnableRetryOnFailure` já trata isso. Se incomodar em horário comercial, agende um "ping" às 7h via Functions ou use `sqlAutoPauseMinutos = -1` só no horário de uso.
- **Quando escalar:** CPU do plano acima de 70% de forma sustentada, ou mais de ~50 usuários simultâneos no Blazor Server. O caminho é B2, depois P0v3. É só mudar `appServiceSku`.

## 2. Offline no app de campo (Blazor WebAssembly)

**Risco:** sincronização, conflitos e upload de fotos em fila são a parte mais trabalhosa do projeto.

**O que o modelo já resolve:**
- **Id gerado no dispositivo** (`Guid v7`) em `Ficha`, `FichaResposta` e `FichaEvidencia`. Reenviar a mesma ficha não duplica (sincronização idempotente).
- **`CapturadoEm` × `SincronizadoEm`**: o auditor vê a hora real da inspeção em campo e a hora em que chegou ao servidor. Isso é o que prova que a ficha **não foi preenchida retroativamente**.
- **`RowVersion`** (concorrência otimista) em todas as entidades auditáveis.
- **Ficha imutável após "Enviada"** (`Ficha.Editavel`). Isso **elimina a maior parte dos conflitos**: só rascunho é editável, e rascunho só existe em um dispositivo.
- **Hash SHA-256 por foto**: garante integridade da evidência e permite retomar upload interrompido.

**Regra de escopo (não negociar):** offline **somente** para FVS e FVM. Aprovações, medições, cadastros e SGQ são **online** (Blazor Server). Isso mantém a complexidade sob controle.

**Estratégia de conflito (etapa 4):**
1. O rascunho vive apenas no dispositivo (IndexedDB).
2. Ao enviar, o servidor aceita se o Id não existe. Se o Id já existe, devolve sucesso (idempotente).
3. Fotos vão por fila própria. A ficha fica "Enviada – evidências pendentes" até todas as fotos subirem.
4. Uma ficha enviada **não recebe edição**. Correção é feita por reinspeção.

## 3. Ecossistema (Azure/.NET × Supabase)

**Risco:** manter duas stacks aumenta o custo de manutenção.

**Decisão registrada em `docs/ADR-001-ecossistema.md`.** Em resumo, o QualyHub vira a **linha corporativa (.NET/Azure)** para clientes que já estão no Microsoft 365. Os micro-SaaS e apps pessoais seguem em Supabase. O domínio (`QualyHub.Domain`) não depende de infraestrutura, o que permite reaproveitar regras.

## 4. LGPD

| Dado | Tratamento |
|---|---|
| **CPF de funcionário** | Nunca em claro. `CpfHash` usa HMAC-SHA256 com chave no Key Vault (permite casar na importação) e `CpfMascarado` serve só para exibição |
| **ASO** | Só **Apto/Inapto + validade**. **Nunca** CID, exames ou diagnóstico. O arquivo, se enviado, vai para o container `sensivel`, com acesso restrito |
| **Minimização** | Funcionário não tem endereço, dados bancários nem dados de saúde no modelo |
| **Região** | `brazilsouth`, com os dados mantidos no Brasil |
| **Acesso** | Entra ID + filtro por obra + menor privilégio no SQL (`qualyhub_app` sem DELETE) |
| **Retenção** | `DocumentoSgq.PrazoRetencaoAnos` e versões de blob removidas após 1 ano (ajustável) |

**Pendências contratuais:** definir no contrato com a N Dalmina quem é o **controlador** (N Dalmina) e quem é o **operador** (você/QualyHub), com o prazo de retenção dos registros da qualidade e o descarte ao fim do contrato.

⚠️ **Nunca rotacione a chave `CpfHmacChave`** sem reprocessar os hashes. Os funcionários deixariam de casar na importação.

## 5. Prazo da Caixa

**Risco:** o auditor quer **histórico de registros**, não só o sistema funcionando no dia da auditoria.

**Ordem de entrega recomendada (maior valor de auditoria primeiro):**
1. FVS/FVM (campo): começa a gerar evidência imediatamente.
2. Documentos do SGQ (lista mestra, revisões, obsoletos).
3. Fornecedores (documentos, qualificação e avaliação).
4. Aprovações de suprimentos com travas.
5. Medições com trava de FVS.

**Plano B:** se a etapa 4 (offline) atrasar, as FVS/FVM rodam **online** no Blazor Server pelo celular enquanto o PWA é finalizado. O registro vale do mesmo jeito.

## 6. Exports do ERP

É a **primeira validação técnica**. O importador aceita .xlsx e .csv, tolera variações de cabeçalho (acento, caixa, espaços) e formatos brasileiros (1.234,56 · 31/12/2026 · CNPJ com máscara). Os mapeamentos ficam salvos por layout. O que pedir está em `docs/EXPORTS-ERP.md`.
