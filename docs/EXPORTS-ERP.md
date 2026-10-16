# Exports do ERP necessários para o SIGEO

**Como enviar:** exporte **.xlsx** (preferível) ou **.csv** direto das telas/relatórios do ERP. **Não é necessário compartilhar login e senha.** Os arquivos bastam e são mais seguros. Para os testes, 20 a 50 linhas reais por arquivo são suficientes. **Funcionários podem vir com CPF e nome fictícios.**

Para cada arquivo, informe também: o caminho no ERP (menu → relatório), se há filtro de obra/período e a frequência desejada (diária, semanal, por medição).

| # | Arquivo | Campos mínimos | Uso no SIGEO |
|---|---|---|---|
| 1 | **Fornecedores** | Código, CPF/CNPJ, Razão social, Nome fantasia, E-mail, Telefone, Cidade, UF | Base para documentos, qualificação e avaliação |
| 2 | **Obras / centros de custo** | Código, Nome, Endereço, Datas | Estrutura e filtro de acesso |
| 3 | **Pedidos de compra** (cabeçalho + itens) | Nº pedido, Data, Obra, Fornecedor (CNPJ), Valor total · Itens: código do insumo, descrição, unidade, quantidade, valor unitário | Aprovação por alçada + trava de fornecedor qualificado |
| 4 | **Insumos / catálogo de materiais** | Código do insumo, Descrição, Unidade, Grupo | Marcar quais são **materiais controlados** (FVM) |
| 5 | **Contratos de empreiteiros** | Nº contrato, Fornecedor, Obra, Escopo, Valor | Vincula serviço controlado ao prestador |
| 6 | **Medições** (cabeçalho + itens) | Nº medição, Contrato, Período, Valor · Itens: código do item/EAP, descrição, quantidade, valor, **local (se houver)** | Trava de FVS aprovada antes de aprovar |
| 7 | **Orçamento / EAP** | Código do item, Descrição, Unidade | Cruzar item de medição com **serviço controlado** |
| 8 | **Funcionários** (se o ERP tiver) | Nome, CPF, Função, Empresa (próprio/terceiro) | Documentos, NRs e acesso à obra |

## Perguntas técnicas para a N Dalmina / ERP
1. O ERP tem **API** ou exportação agendada? Se tiver, a importação pode ser automatizada via Azure Functions.
2. A medição traz o **local** (torre/pavimento/unidade)? Sem ele, a trava de FVS vale por serviço, não por local.
3. O código de insumo e o código de item de orçamento são **estáveis** entre obras?
4. Quem hoje aprova pedidos e medições, e com quais alçadas de valor?
