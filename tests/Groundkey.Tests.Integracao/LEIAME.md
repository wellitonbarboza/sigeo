# Testes de integração (SQL Server real)

Exercitam os serviços da Application contra um SQL Server de verdade, com as migrations, o interceptor de auditoria
e a `FabricaDb` montados como no DI.

## Como rodar

Defina `QH_TEST_SQL` com a connection string de um SQL Server (sem `Database`; o teste cria o próprio banco):

```bash
export QH_TEST_SQL='Server=localhost,1433;User Id=sa;Password=<senha>;TrustServerCertificate=True'
dotnet test tests/Groundkey.Tests.Integracao
```

Sem a variável, todos os testes são **pulados** (`[FatoComBanco]`) e o `dotnet test` da solução continua verde.

## O que acontece

- A fixture cria um banco `QH_Teste_<sufixo aleatório>`, aplica as migrations (`qh.__Migracoes`), cria uma empresa
  fictícia, o usuário de sistema e a carga padrão; no fim apaga o banco.
- Cada teste cria a própria obra, então os cenários são independentes.
- Dados 100% fictícios: CNPJs aleatórios com dígito verificador válido, nomes inventados.

## Cenários

| Arquivo | Cenário |
|---|---|
| `EngenhariaOrcamentoTests` | 1. Geração de torre (contagem, SPE, fração ideal = 1) · 2. Orçamento e demanda de material |
| `SuprimentosTests` | 3. Requisição: excluir item com quebra por local · 4. Contrato: reescalonar locais · 5. Medição: acumulado por local |
| `MaoObraTests` | 6. Custos do mês, réplica, apropriação rateada e fechamento |
| `BancoTests` | 7. Concorrência otimista (RowVersion) · 8. Índices únicos criados pela migration |

Nunca grave a senha do banco em arquivo do repositório.
