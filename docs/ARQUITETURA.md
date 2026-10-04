# QualyHub — Arquitetura (outubro/2026)

## Camadas

```
QualyHub.Web (Blazor Server)  ──►  QualyHub.Application (casos de uso)  ──►  QualyHub.Domain (regras puras)
        │                                   ▲
        └──────►  QualyHub.Infrastructure ──┘  (implementa as abstrações da Application)
```

| Projeto | O que mora aqui | O que NÃO mora aqui |
|---|---|---|
| **Domain** | entidades, value objects, regras puras (travas, rateio, motor do orçamento, gerador de locais) | EF, SQL, Azure |
| **Application** | serviços de caso de uso (`Servicos/`), abstrações (`Abstracoes/`: `IQualyHubDb`, `IFabricaDb`, `IFabricaSistema`, `IUsuarioAtual`, `IArmazenamento`), utilitários (`Comum/`), carga padrão (`Carga/`) | provedor de banco, migrations, Blob, planilhas |
| **Infrastructure** | `QualyHubDbContext` (implementa `IQualyHubDb`), configurações EF, migrations, interceptor de auditoria, `FabricaDb`/`FabricaSistema`, armazenamento (Blob/local), importadores de planilha | regra de negócio |
| **Web** | telas `.razor`, componentes, autenticação, endpoints de arquivo | gravação no banco (toda gravação passa por serviço) |

## Regras de convivência
- **Tela não grava no banco.** Lê com `Fabrica.CriarLeitura()` (sem rastreamento) e grava chamando um serviço da Application dentro de `Executar(...)`.
- **Serviço confere perfil** com `Permissoes.ExigirAsync` quando a ação é restrita (a tela só esconde o botão).
- **Transação** com `IFabricaDb.EmTransacaoAsync(...)` — compatível com a nova tentativa do Azure SQL (cada tentativa usa um contexto novo).
- **Sem MediatR e sem repositório genérico**: `DbSet` já é repositório e `SaveChanges` a unidade de trabalho.
- **Consultas**: `SplitQuery` é o padrão (sem produto cartesiano); listas grandes paginam no banco (`Skip/Take`).
- **Cancelamento**: as telas passam `Cancelamento` (da `PaginaBase`) para serviços e consultas.
- **Erros**: regra de negócio = `RegraDeNegocioException`/`ValidacaoException` (mensagem na tela; 409 nos endpoints, em Problem Details); erro inesperado é registrado e não derruba a tela (`ErrorBoundary` + `Executar`).

## Suprimentos (um serviço por processo)
`RequisicoesServico`, `CotacoesServico` (inclui gerar pedido/contrato), `PedidosServico`, `ContratosServico` (inclui aditivo), `RecebimentosServico`, `TransferenciasServico`, `MedicoesServico`, `PagamentosServico`, `AvaliacaoFornecedoresServico`.

## Frontend
- Blazor Server interativo **sem pré-renderização** (cada tela consulta o banco uma vez).
- Componentes base: `Pagina`, `Grade` (teclado, filtros guardados por tela), `Campo` (rótulo ligado ao campo), `Erros` (anunciado), `Modal`, `Confirmacao`, `Selo` (ícone + cor), `SeletorObra`, `SeletorLocais`, `SeletorDemanda`, `QuebraPorLocal`, `Paginacao`.
- CSS: `app.css` (tema, componentes, utilitárias, celular) + `css/telas-*.css` por área.

## Testes
- `tests/QualyHub.Tests`: unitários (domínio e validações dos serviços).
- `tests/QualyHub.Tests.Integracao`: cenários com SQL Server real; rodam quando a variável `QH_TEST_SQL` existe (veja o LEIAME da pasta).

## Área do Fornecedor (`/fornecedor`)
- Entrada pública por **CNPJ/CPF + código de 6 dígitos** enviado a um **contato cadastrado** escolhido pela pessoa (e-mail ou WhatsApp). Código: 10 min, uso único, 5 tentativas, guardado só como hash (`CodigosAcessoFornecedor`); limites por CNPJ (3/h), por IP (10/h) e por requisição (60 por 5 min).
- Sessão em cookie próprio (`qualyhub-fornecedor`, 8 h, sem renovação) — separado do login interno; telas internas exigem a claim de empresa. O circuito do Blazor junta as duas identidades e cada tela confere a sua.
- Depois de entrar: escolhe o CNPJ/construtora (o do documento e os outros em que o mesmo e-mail/celular verificado é contato), vê as cotações com situação (não enviada / enviada / encerrada) e preenche ou edita a proposta; aba "Cadastro e documentos".
- Envio por `IEnvioMensagens` (Application) → `EnvioAcs` (Azure Communication Services, identidade gerenciada). Desenvolvimento: `EnvioLocalLog` (código no log). Produção sem configuração: canais indisponíveis. Configuração: `infra/configurar-comunicacao.sh`.
- Links antigos (`/portal/{token}` e `/cotacao/{chave}`) continuam valendo até vencer; a equipe agora usa "Enviar convite"/"Convidar" (contato + canal).
