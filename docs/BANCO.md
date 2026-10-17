# Mapa do banco (gerado)

> Gerado por `tools/mapa-banco/gerar.py` a partir do snapshot das migrações e das telas. Não edite à mão: rode o script depois de cada migração.

- **196 tabelas** no schema `qh`, em **15 módulos**; **315 ligações** (chaves estrangeiras); **52 com histórico** (tabela temporal).
- **107 telas** (`@page`). Uma tabela = uma entidade de negócio (classe em `src/Sigeo.Domain`), não uma tela. Cada tela lê/grava várias tabelas por meio dos serviços.

## Módulos

| Módulo | Tabelas |
|---|---|
| [Aprovacoes](#aprovacoes) | 4 |
| [Cadastros](#cadastros) | 15 |
| [Colaboracao](#colaboracao) | 11 |
| [Common](#common) | 3 |
| [Documentos](#documentos) | 16 |
| [Engenharia](#engenharia) | 14 |
| [Estrutura](#estrutura) | 15 |
| [Financeiro](#financeiro) | 6 |
| [Fiscal](#fiscal) | 4 |
| [Importacao](#importacao) | 4 |
| [Incorporacao](#incorporacao) | 9 |
| [Planejamento](#planejamento) | 18 |
| [Qualidade](#qualidade) | 17 |
| [Sst](#sst) | 15 |
| [Suprimentos](#suprimentos) | 45 |

## Aprovacoes

```mermaid
erDiagram
    DelegacaoAprovacao {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier SubstitutoUsuarioId FK
        uniqueidentifier TitularUsuarioId FK
    }
    EtapaAprovacao {
        uniqueidentifier Id PK
        uniqueidentifier DelegacaoAprovacaoId FK
        uniqueidentifier SolicitacaoAprovacaoId FK
    }
    LimiteAprovacao {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier UsuarioId FK
    }
    SolicitacaoAprovacao {
        uniqueidentifier Id PK
    }
    DelegacaoAprovacao ||--o{ EtapaAprovacao : "DelegacaoAprovacaoId"
    SolicitacaoAprovacao ||--o{ EtapaAprovacao : "SolicitacaoAprovacaoId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `DelegacoesAprovacao` | 21 | `src/Sigeo.Domain/Aprovacoes/Aprovacoes.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/obras`, `engenharia/tipologias`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `portal/{Token}` +3 |
| `EtapasAprovacao` | 13 | `src/Sigeo.Domain/Aprovacoes/Aprovacoes.cs` | `(início)`, `engenharia/obras`, `engenharia/tipologias` |
| `LimitesAprovacao` ⏱ | 17 | `src/Sigeo.Domain/Aprovacoes/Aprovacoes.cs` | _rotinas internas_ |
| `SolicitacoesAprovacao` ⏱ | 23 | `src/Sigeo.Domain/Aprovacoes/Aprovacoes.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/obras`, `engenharia/tipologias`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `portal/{Token}` +1 |

## Cadastros

```mermaid
erDiagram
    CategoriaProduto {
        uniqueidentifier Id PK
        uniqueidentifier PaiId FK
    }
    CodigoAcessoFornecedor {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
    }
    Fornecedor {
        uniqueidentifier Id PK
    }
    FornecedorContato {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
    }
    FornecedorDocumento {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier TipoDocumentoExigidoId FK
    }
    FornecedorExigencia {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier TipoDocumentoExigidoId FK
    }
    FornecedorGrupo {
        uniqueidentifier Id PK
        uniqueidentifier CategoriaProdutoId FK
        uniqueidentifier FornecedorId FK
    }
    FornecedorQualificacao {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier MaterialControladoId FK
        uniqueidentifier ServicoControladoId FK
    }
    FornecedorServico {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ServicoControladoId FK
    }
    LinkPortalFornecedor {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
    }
    Marca {
        uniqueidentifier Id PK
    }
    MaterialControlado {
        uniqueidentifier Id PK
        uniqueidentifier CategoriaProdutoId FK
    }
    Produto {
        uniqueidentifier Id PK
        uniqueidentifier MaterialControladoId FK
        uniqueidentifier SubstituidoPorId FK
    }
    ServicoControlado {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoItsId FK
    }
    TipoDocumentoExigido {
        uniqueidentifier Id PK
    }
    Fornecedor ||--o{ CodigoAcessoFornecedor : "FornecedorId"
    Fornecedor ||--o{ FornecedorContato : "FornecedorId"
    Fornecedor ||--o{ FornecedorDocumento : "FornecedorId"
    TipoDocumentoExigido ||--o{ FornecedorDocumento : "TipoDocumentoExigidoId"
    Fornecedor ||--o{ FornecedorExigencia : "FornecedorId"
    TipoDocumentoExigido ||--o{ FornecedorExigencia : "TipoDocumentoExigidoId"
    CategoriaProduto ||--o{ FornecedorGrupo : "CategoriaProdutoId"
    Fornecedor ||--o{ FornecedorGrupo : "FornecedorId"
    Fornecedor ||--o{ FornecedorQualificacao : "FornecedorId"
    MaterialControlado ||--o{ FornecedorQualificacao : "MaterialControladoId"
    ServicoControlado ||--o{ FornecedorQualificacao : "ServicoControladoId"
    Fornecedor ||--o{ FornecedorServico : "FornecedorId"
    ServicoControlado ||--o{ FornecedorServico : "ServicoControladoId"
    Fornecedor ||--o{ LinkPortalFornecedor : "FornecedorId"
    CategoriaProduto ||--o{ MaterialControlado : "CategoriaProdutoId"
    MaterialControlado ||--o{ Produto : "MaterialControladoId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `CategoriasProduto` | 17 | `src/Sigeo.Domain/Cadastros/CodigoProduto.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `cotacoes`, `cotacoes/mapa`, `engenharia/custo-realizado`, `engenharia/importar-erp` +7 |
| `CodigosAcessoFornecedor` | 13 | `src/Sigeo.Domain/Cadastros/AcessoFornecedor.cs` | `fornecedor`, `fornecedor/area`, `fornecedor/cotacao/{CotacaoId:guid}` |
| `Fornecedores` ⏱ | 49 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `(início)`, `campo/fvs/{Id:guid}`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/custo-realizado`, `engenharia/importar-erp`, `engenharia/obras` +38 |
| `FornecedorContatos` | 9 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cotacao/{Token:guid}`, `fornecedor`, `fornecedor/area`, `fornecedor/cotacao/{CotacaoId:guid}`, `fornecedores/documentos`, `portal/{Token}` |
| `FornecedorDocumentos` ⏱ | 31 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `medicoes`, `portal/{Token}`, `suprimentos/documentacao-mensal` |
| `FornecedorExigencias` | 4 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | _rotinas internas_ |
| `FornecedorGrupos` | 4 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cotacoes`, `fornecedores`, `fornecedores/exigencias`, `requisicoes` |
| `Qualificacoes` ⏱ | 29 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `fornecedores/avaliacao`, `fornecedores/qualificacao`, `medicoes` |
| `FornecedorServicos` | 3 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cotacoes`, `fornecedores`, `fornecedores/exigencias`, `requisicoes` |
| `LinksPortal` | 22 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cotacao/{Token:guid}`, `fornecedor`, `fornecedor/area`, `fornecedor/cotacao/{CotacaoId:guid}`, `fornecedores/documentos`, `portal/{Token}` |
| `Marcas` | 14 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cadastros/marcas`, `cadastros/produtos`, `recebimentos` |
| `MateriaisControlados` | 32 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `cotacoes`, `engenharia/tipologias`, `fornecedores`, `fornecedores/avaliacao` +10 |
| `Produtos` | 31 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `cotacoes`, `cotacoes/mapa`, `engenharia/composicoes`, `engenharia/custo-realizado` +31 |
| `ServicosControlados` | 19 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cadastros/controlados`, `engenharia/tipologias`, `obra/modelos/{TipoRota}`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios` |
| `TiposDocumento` | 32 | `src/Sigeo.Domain/Cadastros/Cadastros.cs` | `cotacao/{Token:guid}`, `cotacoes`, `fornecedor/area`, `fornecedores`, `fornecedores/documentos`, `fornecedores/exigencias`, `medicoes`, `plataforma/auditoria` +6 |

## Colaboracao

```mermaid
erDiagram
    AvisoEmail {
        uniqueidentifier Id PK
    }
    Conversa {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    LeituraMensagem {
        uniqueidentifier Id PK
        uniqueidentifier MensagemId FK
    }
    MensagemConversa {
        uniqueidentifier Id PK
        uniqueidentifier ConversaId FK
    }
    Meta {
        uniqueidentifier Id PK
    }
    MetaApuracao {
        uniqueidentifier Id PK
        uniqueidentifier MetaId FK
    }
    Nota {
        uniqueidentifier Id PK
    }
    ParticipanteConversa {
        uniqueidentifier Id PK
        uniqueidentifier ConversaId FK
        uniqueidentifier UsuarioId FK
    }
    Tarefa {
        uniqueidentifier Id PK
    }
    TarefaLeitura {
        uniqueidentifier Id PK
        uniqueidentifier TarefaId FK
    }
    TarefaProrrogacao {
        uniqueidentifier Id PK
        uniqueidentifier TarefaId FK
    }
    MensagemConversa ||--o{ LeituraMensagem : "MensagemId"
    Conversa ||--o{ MensagemConversa : "ConversaId"
    Meta ||--o{ MetaApuracao : "MetaId"
    Conversa ||--o{ ParticipanteConversa : "ConversaId"
    Tarefa ||--o{ TarefaLeitura : "TarefaId"
    Tarefa ||--o{ TarefaProrrogacao : "TarefaId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `AvisosEmail` | 23 | `src/Sigeo.Domain/Colaboracao/Avisos.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `portal/{Token}`, `sistema/parametros` |
| `Conversas` ⏱ | 21 | `src/Sigeo.Domain/Colaboracao/Mensagens.cs` | `(início)`, `mensagens` |
| `LeiturasMensagem` | 5 | `src/Sigeo.Domain/Colaboracao/Mensagens.cs` | `(início)`, `mensagens` |
| `MensagensConversa` | 6 | `src/Sigeo.Domain/Colaboracao/Mensagens.cs` | `(início)`, `mensagens` |
| `Metas` | 29 | `src/Sigeo.Domain/Colaboracao/Metas.cs` | `gestao/metas`, `suprimentos/economia`, `suprimentos/painel`, `suprimentos/precos` |
| `MetasApuracao` | 7 | `src/Sigeo.Domain/Colaboracao/Metas.cs` | _rotinas internas_ |
| `Notas` | 13 | `src/Sigeo.Domain/Colaboracao/Colaboracao.cs` | `(início)`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras`, `engenharia/tipologias`, `gestao/metas`, `requisicoes`, `suprimentos/compradores` +3 |
| `ParticipantesConversa` | 16 | `src/Sigeo.Domain/Colaboracao/Mensagens.cs` | `(início)`, `mensagens` |
| `Tarefas` ⏱ | 35 | `src/Sigeo.Domain/Colaboracao/Colaboracao.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/planejamento/imprimir-programacao`, `fornecedor/area`, `fornecedores/documentos`, `gestao/metas`, `mensagens`, `portal/{Token}` +1 |
| `TarefaLeituras` | 5 | `src/Sigeo.Domain/Colaboracao/Colaboracao.cs` | `gestao/metas`, `tarefas` |
| `TarefaProrrogacoes` | 8 | `src/Sigeo.Domain/Colaboracao/Colaboracao.cs` | `gestao/metas`, `tarefas` |

## Common

```mermaid
erDiagram
    Anexo {
        uniqueidentifier Id PK
    }
    RegistroAuditoria {
        bigint Id PK
    }
    Sequencia {
        uniqueidentifier Id PK
    }
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Anexos` | 18 | `src/Sigeo.Domain/Common/Anexo.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/diario/imprimir`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +5 |
| `Auditoria` | 13 | `src/Sigeo.Domain/Common/RegistroAuditoria.cs` | `recebimentos`, `sistema/log` |
| `Sequencias` | 5 | `src/Sigeo.Domain/Common/Anexo.cs` | `(início)`, `cadastros/categorias`, `cadastros/centros-custo`, `cadastros/controlados`, `cadastros/estabelecimentos`, `cadastros/marcas`, `cadastros/produtos`, `campo/fvs/{Id:guid}` +75 |

## Documentos

```mermaid
erDiagram
    CheckoutDocumento {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
    }
    Ciencia {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
        uniqueidentifier UsuarioId FK
    }
    CopiaControlada {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
        uniqueidentifier GrdId FK
        uniqueidentifier ObraId FK
        uniqueidentifier SolicitacaoCopiaId FK
    }
    Documento {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier PastaId FK
        uniqueidentifier RegraRetencaoId FK
        uniqueidentifier RevisaoVigenteId FK
    }
    DocumentoExterno {
        uniqueidentifier Id PK
    }
    DocumentoRelacionado {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoId FK
        uniqueidentifier RelacionadoId FK
    }
    DocumentoRevisao {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoId FK
    }
    EtapaRevisao {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
    }
    EventoDocumento {
        uniqueidentifier Id PK
    }
    Grd {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier PedidoId FK
    }
    GrdItem {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
        uniqueidentifier GrdId FK
    }
    Pasta {
        uniqueidentifier Id PK
        uniqueidentifier PastaPaiId FK
    }
    PermissaoDocumento {
        uniqueidentifier Id PK
        uniqueidentifier UsuarioId FK
    }
    RegraRetencao {
        uniqueidentifier Id PK
    }
    SolicitacaoCopia {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    SolicitacaoCopiaItem {
        uniqueidentifier Id PK
        uniqueidentifier DocumentoRevisaoId FK
        uniqueidentifier SolicitacaoCopiaId FK
    }
    DocumentoRevisao ||--o{ CheckoutDocumento : "DocumentoRevisaoId"
    DocumentoRevisao ||--o{ Ciencia : "DocumentoRevisaoId"
    DocumentoRevisao ||--o{ CopiaControlada : "DocumentoRevisaoId"
    Grd ||--o{ CopiaControlada : "GrdId"
    SolicitacaoCopia ||--o{ CopiaControlada : "SolicitacaoCopiaId"
    Pasta ||--o{ Documento : "PastaId"
    RegraRetencao ||--o{ Documento : "RegraRetencaoId"
    DocumentoRevisao ||--o{ Documento : "RevisaoVigenteId"
    Documento ||--o{ DocumentoRelacionado : "DocumentoId"
    Documento ||--o{ DocumentoRelacionado : "RelacionadoId"
    Documento ||--o{ DocumentoRevisao : "DocumentoId"
    DocumentoRevisao ||--o{ EtapaRevisao : "DocumentoRevisaoId"
    DocumentoRevisao ||--o{ GrdItem : "DocumentoRevisaoId"
    Grd ||--o{ GrdItem : "GrdId"
    DocumentoRevisao ||--o{ SolicitacaoCopiaItem : "DocumentoRevisaoId"
    SolicitacaoCopia ||--o{ SolicitacaoCopiaItem : "SolicitacaoCopiaId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Checkouts` | 17 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `sgq/documentos` |
| `Ciencias` | 17 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `portal/{Token}`, `sgq/documentos`, `sgq/leituras` |
| `Copias` ⏱ | 27 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `qr/{Token}`, `sgq/copias`, `sgq/documentos` |
| `Documentos` ⏱ | 34 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +8 |
| `DocumentosExternos` ⏱ | 21 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `sgq/externos` |
| `DocumentosRelacionados` | 3 | `src/Sigeo.Domain/Documentos/Documentos.cs` | _rotinas internas_ |
| `Revisoes` ⏱ | 34 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `qr/{Token}`, `sgq/copias`, `sgq/grd` |
| `EtapasRevisao` | 11 | `src/Sigeo.Domain/Documentos/Documentos.cs` | _rotinas internas_ |
| `EventosDocumento` | 12 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `sgq/documentos`, `sistema/log`, `sistema/parametros`, `sistema/permissoes`, `sistema/usuarios` |
| `Grds` | 19 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `sgq/grd` |
| `GrdItens` | 3 | `src/Sigeo.Domain/Documentos/Documentos.cs` | _rotinas internas_ |
| `Pastas` | 13 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `sgq/documentos`, `sgq/lista-mestra`, `sistema/permissoes` |
| `Permissoes` ⏱ | 23 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `(início)`, `sistema/parametros`, `sistema/permissoes`, `sistema/usuarios` |
| `RegrasRetencao` | 18 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `sgq/documentos`, `sgq/registros` |
| `SolicitacoesCopia` | 21 | `src/Sigeo.Domain/Documentos/Documentos.cs` | `sgq/copias` |
| `SolicitacaoCopiaItens` | 5 | `src/Sigeo.Domain/Documentos/Documentos.cs` | _rotinas internas_ |

## Engenharia

```mermaid
erDiagram
    Apropriacao {
        uniqueidentifier Id PK
        uniqueidentifier ColaboradorId FK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier EquipeId FK
        uniqueidentifier LocalId FK
    }
    Composicao {
        uniqueidentifier Id PK
        uniqueidentifier ServicoControladoId FK
    }
    ComposicaoItem {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier FuncaoObraId FK
        uniqueidentifier ProdutoId FK
    }
    CustoMaoObraMes {
        uniqueidentifier Id PK
        uniqueidentifier ColaboradorId FK
        uniqueidentifier EquipeId FK
        uniqueidentifier FuncaoObraId FK
    }
    CustoRealizado {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier LocalId FK
        uniqueidentifier PedidoId FK
        uniqueidentifier ProdutoId FK
    }
    Equipe {
        uniqueidentifier Id PK
    }
    EquipeMembro {
        uniqueidentifier Id PK
        uniqueidentifier ColaboradorId FK
        uniqueidentifier EquipeId FK
    }
    Quadra {
        uniqueidentifier Id PK
        uniqueidentifier ImplantacaoId FK
        uniqueidentifier LocalId FK
    }
    ServicoAplicacao {
        uniqueidentifier Id PK
        uniqueidentifier AmbienteModeloId FK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier ObraId FK
        uniqueidentifier TipologiaId FK
    }
    ServicoLocal {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier LocalId FK
        uniqueidentifier SpeId FK
    }
    Spe {
        uniqueidentifier Id PK
        uniqueidentifier QuadraId FK
    }
    Tipologia {
        uniqueidentifier Id PK
    }
    TipologiaAmbiente {
        uniqueidentifier Id PK
        uniqueidentifier TipologiaId FK
    }
    TipologiaMix {
        uniqueidentifier Id PK
        uniqueidentifier ApartamentoTipologiaId FK
        uniqueidentifier TipologiaId FK
    }
    Composicao ||--o{ Apropriacao : "ComposicaoId"
    Equipe ||--o{ Apropriacao : "EquipeId"
    Composicao ||--o{ ComposicaoItem : "ComposicaoId"
    Equipe ||--o{ CustoMaoObraMes : "EquipeId"
    Equipe ||--o{ EquipeMembro : "EquipeId"
    Tipologia ||--o{ Quadra : "ImplantacaoId"
    TipologiaAmbiente ||--o{ ServicoAplicacao : "AmbienteModeloId"
    Composicao ||--o{ ServicoAplicacao : "ComposicaoId"
    Tipologia ||--o{ ServicoAplicacao : "TipologiaId"
    Composicao ||--o{ ServicoLocal : "ComposicaoId"
    Spe ||--o{ ServicoLocal : "SpeId"
    Quadra ||--o{ Spe : "QuadraId"
    Tipologia ||--o{ TipologiaAmbiente : "TipologiaId"
    Tipologia ||--o{ TipologiaMix : "ApartamentoTipologiaId"
    Tipologia ||--o{ TipologiaMix : "TipologiaId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Apropriacoes` | 20 | `src/Sigeo.Domain/Engenharia/MaoObra.cs` | `engenharia/mao-de-obra`, `engenharia/obras`, `engenharia/tipologias`, `medicoes`, `suprimentos/documentacao-mensal` |
| `Composicoes` ⏱ | 22 | `src/Sigeo.Domain/Engenharia/Orcamento.cs` | `(início)`, `engenharia/composicoes`, `engenharia/cronograma`, `engenharia/custo-realizado`, `engenharia/importar-erp`, `engenharia/mao-de-obra`, `engenharia/planejamento`, `engenharia/planejamento/imprimir` +16 |
| `ComposicaoItens` | 11 | `src/Sigeo.Domain/Engenharia/Orcamento.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos` |
| `CustosMaoObra` | 22 | `src/Sigeo.Domain/Engenharia/MaoObra.cs` | `engenharia/mao-de-obra` |
| `CustosRealizados` | 32 | `src/Sigeo.Domain/Engenharia/CustoRealizado.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `engenharia/custo-realizado`, `engenharia/importar-erp` |
| `Equipes` | 15 | `src/Sigeo.Domain/Engenharia/MaoObra.cs` | `engenharia/mao-de-obra`, `gestao/metas` |
| `EquipeMembros` | 5 | `src/Sigeo.Domain/Engenharia/MaoObra.cs` | _rotinas internas_ |
| `Quadras` ⏱ | 26 | `src/Sigeo.Domain/Engenharia/Engenharia.cs` | `engenharia/obras`, `engenharia/tipologias` |
| `ServicoAplicacoes` | 16 | `src/Sigeo.Domain/Engenharia/Orcamento.cs` | `engenharia/composicoes`, `engenharia/obras`, `engenharia/tipologias`, `requisicoes` |
| `ServicosLocais` | 19 | `src/Sigeo.Domain/Engenharia/Orcamento.cs` | `(início)`, `engenharia/composicoes`, `engenharia/cronograma`, `engenharia/custo-realizado`, `engenharia/importar-erp`, `engenharia/mao-de-obra`, `engenharia/obras`, `engenharia/planejamento` +18 |
| `Spes` ⏱ | 24 | `src/Sigeo.Domain/Engenharia/Engenharia.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/diario`, `engenharia/obras`, `engenharia/planejamento/imprimir-programacao`, `engenharia/tipologias`, `equipamentos`, `estoque` +16 |
| `Tipologias` | 24 | `src/Sigeo.Domain/Engenharia/Engenharia.cs` | `engenharia/composicoes`, `engenharia/importar-erp`, `engenharia/obras`, `engenharia/tipologias`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar` +6 |
| `TipologiaAmbientes` | 13 | `src/Sigeo.Domain/Engenharia/Engenharia.cs` | `engenharia/obras`, `engenharia/tipologias` |
| `TipologiaMix` | 7 | `src/Sigeo.Domain/Engenharia/Engenharia.cs` | _rotinas internas_ |

## Estrutura

```mermaid
erDiagram
    Alcada {
        uniqueidentifier Id PK
    }
    CentroCusto {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier PaiId FK
    }
    CredencialUsuario {
        uniqueidentifier Id PK
        uniqueidentifier UsuarioId FK
    }
    Empresa {
        uniqueidentifier Id PK
    }
    Estabelecimento {
        uniqueidentifier Id PK
        uniqueidentifier SpeId FK
    }
    FluxoEstabelecimento {
        uniqueidentifier Id PK
        uniqueidentifier DestinoId FK
        uniqueidentifier OrigemId FK
    }
    ItemLista {
        uniqueidentifier Id PK
    }
    Local {
        uniqueidentifier Id PK
        uniqueidentifier AmbienteModeloId FK
        uniqueidentifier LocalPaiId FK
        uniqueidentifier ObraId FK
        uniqueidentifier SpeId FK
        uniqueidentifier TipologiaId FK
    }
    Obra {
        uniqueidentifier Id PK
        uniqueidentifier EstabelecimentoDonoId FK
    }
    Parametro {
        uniqueidentifier Id PK
    }
    PerfilAcesso {
        uniqueidentifier Id PK
    }
    RegistroPlataforma {
        uniqueidentifier Id PK
    }
    RegraNotificacao {
        uniqueidentifier Id PK
    }
    Usuario {
        uniqueidentifier Id PK
        uniqueidentifier PerfilAcessoId FK
    }
    UsuarioObra {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier UsuarioId FK
    }
    Obra ||--o{ CentroCusto : "ObraId"
    Usuario ||--o{ CredencialUsuario : "UsuarioId"
    Estabelecimento ||--o{ FluxoEstabelecimento : "DestinoId"
    Estabelecimento ||--o{ FluxoEstabelecimento : "OrigemId"
    Obra ||--o{ Local : "ObraId"
    Estabelecimento ||--o{ Obra : "EstabelecimentoDonoId"
    PerfilAcesso ||--o{ Usuario : "PerfilAcessoId"
    Obra ||--o{ UsuarioObra : "ObraId"
    Usuario ||--o{ UsuarioObra : "UsuarioId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Alcadas` | 24 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/obras`, `engenharia/tipologias`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `plataforma/auditoria` +4 |
| `CentrosCusto` | 18 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `cadastros/centros-custo`, `cadastros/estabelecimentos` |
| `CredenciaisUsuario` | 17 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `conta/construtora`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `sistema/usuarios` |
| `Empresas` | 12 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cadastros/categorias`, `cadastros/centros-custo`, `cadastros/controlados`, `cadastros/estabelecimentos`, `cadastros/marcas`, `cadastros/produtos`, `campo/fvs/{Id:guid}` +76 |
| `Estabelecimentos` | 26 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `cadastros/centros-custo`, `cadastros/estabelecimentos`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `equipamentos`, `estoque`, `estoque/kits` +15 |
| `FluxosEstabelecimento` | 16 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `cadastros/centros-custo`, `cadastros/estabelecimentos` |
| `Listas` | 15 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `sistema/parametros` |
| `Locais` | 29 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `contratos`, `engenharia/composicoes`, `engenharia/cronograma`, `engenharia/custo-realizado`, `engenharia/diario/imprimir` +38 |
| `Obras` | 30 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cadastros/produtos`, `contratos`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/cronograma`, `engenharia/custo-realizado` +51 |
| `Parametros` ⏱ | 17 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cadastros/categorias`, `cadastros/centros-custo`, `cadastros/controlados`, `cadastros/estabelecimentos`, `cadastros/marcas`, `cadastros/produtos`, `campo/fvs/{Id:guid}` +76 |
| `PerfisAcesso` | 14 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cadastros/categorias`, `cadastros/centros-custo`, `cadastros/controlados`, `cadastros/estabelecimentos`, `cadastros/marcas`, `cadastros/produtos`, `campo/fvs/{Id:guid}` +72 |
| `RegistrosPlataforma` | 7 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios` |
| `RegrasNotificacao` | 17 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `mensagens`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios` +2 |
| `Usuarios` | 22 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `cadastros/categorias`, `cadastros/centros-custo`, `cadastros/controlados`, `cadastros/estabelecimentos`, `cadastros/marcas`, `cadastros/produtos`, `campo/fvs/{Id:guid}` +76 |
| `UsuariosObras` | 3 | `src/Sigeo.Domain/Estrutura/Estrutura.cs` | `(início)`, `sistema/parametros`, `sistema/permissoes`, `sistema/usuarios` |

## Financeiro

```mermaid
erDiagram
    CondicaoPagamento {
        uniqueidentifier Id PK
    }
    ContaBancaria {
        uniqueidentifier Id PK
        uniqueidentifier EstabelecimentoId FK
    }
    ContaGerencial {
        uniqueidentifier Id PK
    }
    MovimentoBancario {
        uniqueidentifier Id PK
        uniqueidentifier ContaBancariaId FK
        uniqueidentifier TituloParcelaId FK
    }
    Titulo {
        uniqueidentifier Id PK
        uniqueidentifier CentroCustoId FK
        uniqueidentifier CompradorId FK
        uniqueidentifier ContaGerencialId FK
        uniqueidentifier EstabelecimentoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
    }
    TituloParcela {
        uniqueidentifier Id PK
        uniqueidentifier ContaBancariaId FK
        uniqueidentifier FornecedorPermutaId FK
        uniqueidentifier TituloId FK
    }
    ContaBancaria ||--o{ MovimentoBancario : "ContaBancariaId"
    TituloParcela ||--o{ MovimentoBancario : "TituloParcelaId"
    ContaGerencial ||--o{ Titulo : "ContaGerencialId"
    ContaBancaria ||--o{ TituloParcela : "ContaBancariaId"
    Titulo ||--o{ TituloParcela : "TituloId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `CondicoesPagamento` | 14 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/vendas`, `plataforma/auditoria` +2 |
| `ContasBancarias` | 19 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/vendas` |
| `ContasGerenciais` | 14 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +5 |
| `MovimentosBancarios` | 19 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/vendas` |
| `Titulos` ⏱ | 38 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `TituloParcelas` ⏱ | 28 | `src/Sigeo.Domain/Financeiro/Financeiro.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |

## Fiscal

```mermaid
erDiagram
    ImportacaoNota {
        uniqueidentifier Id PK
    }
    NotaFiscalEntrada {
        uniqueidentifier Id PK
        uniqueidentifier PedidoId FK
        uniqueidentifier RecebimentoId FK
    }
    NotaFiscalItem {
        uniqueidentifier Id PK
        uniqueidentifier NotaFiscalId FK
        uniqueidentifier PedidoItemId FK
    }
    UsoIa {
        uniqueidentifier Id PK
    }
    NotaFiscalEntrada ||--o{ NotaFiscalItem : "NotaFiscalId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `ImportacoesNota` | 19 | `src/Sigeo.Domain/Fiscal/NotaFiscal.cs` | `notas-fiscais` |
| `NotasFiscais` | 33 | `src/Sigeo.Domain/Fiscal/NotaFiscal.cs` | `cadastros/produtos`, `contratos/parcelas`, `notas-fiscais`, `obra/rnc`, `recebimentos` |
| `NotaFiscalItens` | 16 | `src/Sigeo.Domain/Fiscal/NotaFiscal.cs` | _rotinas internas_ |
| `UsosIa` | 16 | `src/Sigeo.Domain/Fiscal/UsoIa.cs` | `cadastros/produtos`, `obra/rnc` |

## Importacao

```mermaid
erDiagram
    LayoutImportacao {
        uniqueidentifier Id PK
    }
    LinhaImportacao {
        uniqueidentifier Id PK
        uniqueidentifier LoteImportacaoId FK
    }
    LoteImportacao {
        uniqueidentifier Id PK
    }
    VinculoOrigem {
        uniqueidentifier Id PK
    }
    LoteImportacao ||--o{ LinhaImportacao : "LoteImportacaoId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `LayoutsImportacao` | 17 | `src/Sigeo.Domain/Importacao/Importacao.cs` | _rotinas internas_ |
| `LinhasImportacao` | 8 | `src/Sigeo.Domain/Importacao/Importacao.cs` | _rotinas internas_ |
| `LotesImportacao` | 24 | `src/Sigeo.Domain/Importacao/Importacao.cs` | _rotinas internas_ |
| `VinculosOrigem` | 18 | `src/Sigeo.Domain/Importacao/Importacao.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos` |

## Incorporacao

```mermaid
erDiagram
    AliquotaRegime {
        uniqueidentifier Id PK
    }
    ApuracaoRet {
        uniqueidentifier Id PK
        uniqueidentifier IncorporacaoId FK
        uniqueidentifier TituloId FK
    }
    BlocoCondicaoVenda {
        uniqueidentifier Id PK
        uniqueidentifier ContratoVendaId FK
        uniqueidentifier FornecedorPermutaId FK
    }
    ComissaoVenda {
        uniqueidentifier Id PK
        uniqueidentifier ContratoVendaId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier TituloId FK
    }
    Comprador {
        uniqueidentifier Id PK
    }
    ContratoVenda {
        uniqueidentifier Id PK
        uniqueidentifier CompradorId FK
        uniqueidentifier TituloId FK
        uniqueidentifier UnidadeComercialId FK
    }
    IncorporacaoObra {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    IndiceEconomico {
        uniqueidentifier Id PK
    }
    UnidadeComercial {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
    }
    IncorporacaoObra ||--o{ ApuracaoRet : "IncorporacaoId"
    ContratoVenda ||--o{ BlocoCondicaoVenda : "ContratoVendaId"
    ContratoVenda ||--o{ ComissaoVenda : "ContratoVendaId"
    Comprador ||--o{ ContratoVenda : "CompradorId"
    UnidadeComercial ||--o{ ContratoVenda : "UnidadeComercialId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `AliquotasRegime` | 20 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +5 |
| `ApuracoesRet` | 23 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `BlocosCondicaoVenda` | 8 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | _rotinas internas_ |
| `ComissoesVenda` | 11 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | _rotinas internas_ |
| `Compradores` | 17 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `ContratosVenda` | 26 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `Incorporacoes` | 26 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `IndicesEconomicos` | 13 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |
| `UnidadesComerciais` | 16 | `src/Sigeo.Domain/Incorporacao/Incorporacao.cs` | `engenharia/importar-erp`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar`, `incorporacao/carteira`, `incorporacao/empreendimento`, `incorporacao/espelho` +2 |

## Planejamento

```mermaid
erDiagram
    AtividadeCronograma {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier LocalId FK
        uniqueidentifier PaiId FK
        uniqueidentifier VersaoId FK
    }
    CompromissoSemanal {
        uniqueidentifier Id PK
        uniqueidentifier PlanoId FK
    }
    DesembolsoVersao {
        uniqueidentifier Id PK
        uniqueidentifier VersaoId FK
    }
    DiarioAtividade {
        uniqueidentifier Id PK
        uniqueidentifier DiarioId FK
        uniqueidentifier LocalId FK
    }
    DiarioEfetivo {
        uniqueidentifier Id PK
        uniqueidentifier DiarioId FK
    }
    DiarioEquipamento {
        uniqueidentifier Id PK
        uniqueidentifier DiarioId FK
        uniqueidentifier EquipamentoId FK
    }
    DiarioObra {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    Feriado {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    MedicaoSemanal {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
        uniqueidentifier ObraId FK
    }
    PlanoSemanal {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    RedeLocal {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
        uniqueidentifier RedeId FK
    }
    RedePacote {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier RedeId FK
    }
    RedePacoteLocal {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
        uniqueidentifier PacoteId FK
    }
    RedePlanejamento {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    Restricao {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier ResponsavelId FK
    }
    VersaoCronograma {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    VinculoAtividade {
        uniqueidentifier Id PK
        uniqueidentifier PredecessoraId FK
        uniqueidentifier SucessoraId FK
        uniqueidentifier VersaoId FK
    }
    VinculoRede {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier PacoteDestinoId FK
        uniqueidentifier PacoteOrigemId FK
    }
    VersaoCronograma ||--o{ AtividadeCronograma : "VersaoId"
    PlanoSemanal ||--o{ CompromissoSemanal : "PlanoId"
    VersaoCronograma ||--o{ DesembolsoVersao : "VersaoId"
    DiarioObra ||--o{ DiarioAtividade : "DiarioId"
    DiarioObra ||--o{ DiarioEfetivo : "DiarioId"
    DiarioObra ||--o{ DiarioEquipamento : "DiarioId"
    RedePlanejamento ||--o{ RedeLocal : "RedeId"
    RedePlanejamento ||--o{ RedePacote : "RedeId"
    RedePacote ||--o{ RedePacoteLocal : "PacoteId"
    AtividadeCronograma ||--o{ VinculoAtividade : "PredecessoraId"
    AtividadeCronograma ||--o{ VinculoAtividade : "SucessoraId"
    VersaoCronograma ||--o{ VinculoAtividade : "VersaoId"
    RedePacote ||--o{ VinculoRede : "PacoteDestinoId"
    RedePacote ||--o{ VinculoRede : "PacoteOrigemId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `AtividadesCronograma` | 22 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/obras`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao`, `engenharia/tipologias` |
| `CompromissosSemanais` | 11 | `src/Sigeo.Domain/Planejamento/CurtoPrazo.cs` | _rotinas internas_ |
| `DesembolsoVersao` | 5 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `DiariosObraAtividades` | 6 | `src/Sigeo.Domain/Planejamento/Acompanhamento.cs` | `engenharia/diario` |
| `DiariosObraEfetivo` | 7 | `src/Sigeo.Domain/Planejamento/Acompanhamento.cs` | _rotinas internas_ |
| `DiariosObraEquipamentos` | 6 | `src/Sigeo.Domain/Planejamento/Acompanhamento.cs` | _rotinas internas_ |
| `DiariosObra` ⏱ | 29 | `src/Sigeo.Domain/Planejamento/Acompanhamento.cs` | `engenharia/diario`, `engenharia/diario/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `Feriados` | 13 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `MedicoesSemanais` ⏱ | 25 | `src/Sigeo.Domain/Planejamento/Acompanhamento.cs` | `engenharia/planejamento/imprimir-programacao` |
| `PlanosSemanais` ⏱ | 19 | `src/Sigeo.Domain/Planejamento/CurtoPrazo.cs` | `engenharia/diario`, `engenharia/planejamento/imprimir-programacao`, `estoque/kits`, `gestao/metas` |
| `RedeLocais` | 4 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/obras`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao`, `engenharia/tipologias` |
| `RedePacotes` | 7 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `RedePacoteLocais` | 4 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/obras`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao`, `engenharia/tipologias` |
| `RedesPlanejamento` | 16 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `Restricoes` ⏱ | 27 | `src/Sigeo.Domain/Planejamento/CurtoPrazo.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/diario`, `engenharia/planejamento/imprimir-programacao`, `equipamentos`, `estoque`, `estoque/kits`, `fornecedor/area` +14 |
| `VersoesCronograma` ⏱ | 28 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `(início)`, `engenharia/cronograma`, `engenharia/last-planner`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao`, `mensagens` |
| `VinculosAtividade` | 6 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |
| `VinculosRede` | 7 | `src/Sigeo.Domain/Planejamento/Planejamento.cs` | `engenharia/cronograma`, `engenharia/planejamento`, `engenharia/planejamento/imprimir`, `engenharia/planejamento/imprimir-programacao` |

## Qualidade

```mermaid
erDiagram
    AcaoCorretiva {
        uniqueidentifier Id PK
    }
    AcaoCorretivaItem {
        uniqueidentifier Id PK
        uniqueidentifier AcaoCorretivaId FK
    }
    AplicacaoLote {
        uniqueidentifier Id PK
        uniqueidentifier RecebimentoId FK
    }
    AuditoriaInterna {
        uniqueidentifier Id PK
    }
    Calibracao {
        uniqueidentifier Id PK
        uniqueidentifier EquipamentoMedicaoId FK
    }
    CautelaFerramenta {
        uniqueidentifier Id PK
        uniqueidentifier ColaboradorId FK
        uniqueidentifier EquipamentoMedicaoId FK
        uniqueidentifier FornecedorId FK
    }
    Constatacao {
        uniqueidentifier Id PK
        uniqueidentifier AuditoriaInternaId FK
    }
    EnsaioModelo {
        uniqueidentifier Id PK
        uniqueidentifier ModeloInspecaoId FK
    }
    EnsaioRecebimento {
        uniqueidentifier Id PK
        uniqueidentifier RecebimentoId FK
    }
    EquipamentoMedicao {
        uniqueidentifier Id PK
        uniqueidentifier CautelaColaboradorId FK
        uniqueidentifier CautelaFornecedorId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier TipoMedicaoId FK
    }
    FvmItem {
        uniqueidentifier Id PK
        uniqueidentifier RecebimentoId FK
    }
    Fvs {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
        uniqueidentifier ModeloInspecaoId FK
    }
    FvsItem {
        uniqueidentifier Id PK
        uniqueidentifier FvsId FK
    }
    ItemVerificacao {
        uniqueidentifier Id PK
        uniqueidentifier ModeloInspecaoId FK
    }
    ModeloInspecao {
        uniqueidentifier Id PK
    }
    Rnc {
        uniqueidentifier Id PK
    }
    TipoEquipamentoMedicao {
        uniqueidentifier Id PK
    }
    AcaoCorretiva ||--o{ AcaoCorretivaItem : "AcaoCorretivaId"
    EquipamentoMedicao ||--o{ Calibracao : "EquipamentoMedicaoId"
    EquipamentoMedicao ||--o{ CautelaFerramenta : "EquipamentoMedicaoId"
    AuditoriaInterna ||--o{ Constatacao : "AuditoriaInternaId"
    ModeloInspecao ||--o{ EnsaioModelo : "ModeloInspecaoId"
    TipoEquipamentoMedicao ||--o{ EquipamentoMedicao : "TipoMedicaoId"
    ModeloInspecao ||--o{ Fvs : "ModeloInspecaoId"
    Fvs ||--o{ FvsItem : "FvsId"
    ModeloInspecao ||--o{ ItemVerificacao : "ModeloInspecaoId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `AcoesCorretivas` ⏱ | 27 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `mensagens`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos` +5 |
| `AcaoCorretivaItens` | 8 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | _rotinas internas_ |
| `AplicacoesLote` | 8 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `engenharia/tipologias`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +4 |
| `AuditoriasInternas` ⏱ | 21 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `mensagens`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos` +5 |
| `Calibracoes` | 11 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs`, `obra/modelos/{TipoRota}` +3 |
| `CautelasFerramenta` | 18 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs`, `obra/modelos/{TipoRota}` +3 |
| `Constatacoes` | 8 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | _rotinas internas_ |
| `EnsaiosModelo` | 10 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | _rotinas internas_ |
| `EnsaiosRecebimento` ⏱ | 24 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +4 |
| `Equipamentos` ⏱ | 34 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/equipamentos/etiquetas` +6 |
| `FvmItens` | 13 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `obra/modelos/{TipoRota}` |
| `Fvs` ⏱ | 34 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `engenharia/planejamento/imprimir-programacao`, `engenharia/tipologias`, `medicoes`, `mensagens` +9 |
| `FvsItens` | 16 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `obra/modelos/{TipoRota}` |
| `ItensVerificacao` | 12 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | _rotinas internas_ |
| `ModelosInspecao` ⏱ | 30 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `cadastros/controlados`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +14 |
| `Rncs` ⏱ | 33 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `cotacao/{Token:guid}`, `engenharia/obras`, `engenharia/tipologias`, `fornecedor/area`, `fornecedores/avaliacao` +20 |
| `TiposEquipamentoMedicao` | 17 | `src/Sigeo.Domain/Qualidade/Qualidade.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs`, `obra/modelos/{TipoRota}` +6 |

## Sst

```mermaid
erDiagram
    Colaborador {
        uniqueidentifier Id PK
    }
    DocumentoColaborador {
        uniqueidentifier Id PK
    }
    EntregaEpi {
        uniqueidentifier Id PK
    }
    Epi {
        uniqueidentifier Id PK
    }
    ExecutantePt {
        uniqueidentifier Id PK
        uniqueidentifier PermissaoTrabalhoId FK
    }
    Exigencia {
        uniqueidentifier Id PK
    }
    FuncaoExigencia {
        uniqueidentifier Id PK
        uniqueidentifier ExigenciaId FK
        uniqueidentifier FuncaoObraId FK
    }
    FuncaoObra {
        uniqueidentifier Id PK
    }
    InspecaoSeguranca {
        uniqueidentifier Id PK
    }
    InspecaoSegurancaItem {
        uniqueidentifier Id PK
        uniqueidentifier InspecaoSegurancaId FK
    }
    Ocorrencia {
        uniqueidentifier Id PK
    }
    Participante {
        uniqueidentifier Id PK
        uniqueidentifier TurmaId FK
    }
    PermissaoTrabalho {
        uniqueidentifier Id PK
    }
    Treinamento {
        uniqueidentifier Id PK
    }
    Turma {
        uniqueidentifier Id PK
    }
    PermissaoTrabalho ||--o{ ExecutantePt : "PermissaoTrabalhoId"
    Exigencia ||--o{ FuncaoExigencia : "ExigenciaId"
    FuncaoObra ||--o{ FuncaoExigencia : "FuncaoObraId"
    InspecaoSeguranca ||--o{ InspecaoSegurancaItem : "InspecaoSegurancaId"
    Turma ||--o{ Participante : "TurmaId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Colaboradores` ⏱ | 32 | `src/Sigeo.Domain/Sst/Sst.cs` | `(início)`, `cotacao/{Token:guid}`, `engenharia/mao-de-obra`, `fornecedor/area`, `fornecedores/documentos`, `medicoes`, `portal/{Token}`, `sst/colaboradores` +8 |
| `DocumentosColaborador` ⏱ | 26 | `src/Sigeo.Domain/Sst/Sst.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `portal/{Token}`, `sst/colaboradores`, `sst/documentos`, `sst/epi` +5 |
| `EntregasEpi` ⏱ | 20 | `src/Sigeo.Domain/Sst/Sst.cs` | `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes`, `sst/ocorrencias`, `sst/pt`, `sst/treinamentos` |
| `Epis` | 16 | `src/Sigeo.Domain/Sst/Sst.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias` +4 |
| `ExecutantesPt` | 7 | `src/Sigeo.Domain/Sst/Sst.cs` | _rotinas internas_ |
| `Exigencias` | 20 | `src/Sigeo.Domain/Sst/Sst.cs` | `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `portal/{Token}`, `sst/colaboradores` +7 |
| `FuncaoExigencias` | 4 | `src/Sigeo.Domain/Sst/Sst.cs` | `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes`, `sst/ocorrencias`, `sst/pt`, `sst/treinamentos` |
| `Funcoes` | 14 | `src/Sigeo.Domain/Sst/Sst.cs` | `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `portal/{Token}`, `sst/exigencias` |
| `InspecoesSeguranca` ⏱ | 22 | `src/Sigeo.Domain/Sst/Sst.cs` | `engenharia/obras`, `engenharia/tipologias`, `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes`, `sst/ocorrencias` +2 |
| `InspecaoSegurancaItens` | 13 | `src/Sigeo.Domain/Sst/Sst.cs` | _rotinas internas_ |
| `Ocorrencias` ⏱ | 27 | `src/Sigeo.Domain/Sst/Sst.cs` | `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `engenharia/obras`, `engenharia/tipologias`, `obra/acoes`, `obra/auditorias`, `obra/equipamentos`, `obra/fvs` +4 |
| `Participantes` | 7 | `src/Sigeo.Domain/Sst/Sst.cs` | `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes`, `sst/ocorrencias`, `sst/pt`, `sst/treinamentos` |
| `PermissoesTrabalho` ⏱ | 27 | `src/Sigeo.Domain/Sst/Sst.cs` | `(início)`, `engenharia/obras`, `engenharia/tipologias`, `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes` +3 |
| `Treinamentos` | 18 | `src/Sigeo.Domain/Sst/Sst.cs` | `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios`, `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes` +3 |
| `Turmas` ⏱ | 20 | `src/Sigeo.Domain/Sst/Sst.cs` | `sst/colaboradores`, `sst/documentos`, `sst/epi`, `sst/exigencias`, `sst/inspecoes`, `sst/ocorrencias`, `sst/pt`, `sst/treinamentos` |

## Suprimentos

```mermaid
erDiagram
    Aditivo {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
    }
    AditivoItem {
        uniqueidentifier Id PK
        uniqueidentifier AditivoId FK
    }
    Almoxarifado {
        uniqueidentifier Id PK
        uniqueidentifier EstabelecimentoId FK
        uniqueidentifier ObraId FK
    }
    AusenciaComprador {
        uniqueidentifier Id PK
        uniqueidentifier CarteiraId FK
    }
    AvaliacaoSemestral {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
    }
    CarteiraCategoria {
        uniqueidentifier Id PK
        uniqueidentifier CarteiraId FK
    }
    CarteiraComprador {
        uniqueidentifier Id PK
    }
    CompetenciaDocumental {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
    }
    Contrato {
        uniqueidentifier Id PK
        uniqueidentifier CentroCustoId FK
        uniqueidentifier CotacaoId FK
        uniqueidentifier EstabelecimentoFaturamentoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
        uniqueidentifier RequisicaoId FK
    }
    ContratoItem {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier ContratoId FK
        uniqueidentifier ServicoControladoId FK
    }
    ContratoItemLocal {
        uniqueidentifier Id PK
        uniqueidentifier ContratoItemId FK
        uniqueidentifier LocalId FK
    }
    ContratoServico {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier ServicoControladoId FK
    }
    Cotacao {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorVencedorId FK
        uniqueidentifier ObraId FK
        uniqueidentifier RequisicaoId FK
    }
    CotacaoFornecedor {
        uniqueidentifier Id PK
        uniqueidentifier CotacaoId FK
        uniqueidentifier FornecedorId FK
    }
    CotacaoItem {
        uniqueidentifier Id PK
        uniqueidentifier CotacaoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier RequisicaoItemId FK
    }
    Equipamento {
        uniqueidentifier Id PK
        uniqueidentifier AlmoxarifadoId FK
        uniqueidentifier ContratoId FK
        uniqueidentifier ContratoItemId FK
        uniqueidentifier FamiliaId FK
        uniqueidentifier FornecedorId FK
    }
    EquipamentoDocumento {
        uniqueidentifier Id PK
        uniqueidentifier EquipamentoId FK
    }
    FamiliaEquipamento {
        uniqueidentifier Id PK
    }
    InventarioEstoque {
        uniqueidentifier Id PK
        uniqueidentifier AlmoxarifadoId FK
    }
    InventarioItem {
        uniqueidentifier Id PK
        uniqueidentifier InventarioId FK
        uniqueidentifier ProdutoId FK
    }
    ItemCompetencia {
        uniqueidentifier Id PK
        uniqueidentifier CompetenciaId FK
        uniqueidentifier TipoDocumentoExigidoId FK
    }
    KitMaterial {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
    }
    KitMaterialItem {
        uniqueidentifier Id PK
        uniqueidentifier KitId FK
        uniqueidentifier ProdutoId FK
    }
    LiberacaoPagamento {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier MedicaoId FK
        uniqueidentifier ObraId FK
        uniqueidentifier PedidoId FK
    }
    LoteCompra {
        uniqueidentifier Id PK
        uniqueidentifier ObraId FK
        uniqueidentifier RequisicaoId FK
    }
    Medicao {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier ObraId FK
        uniqueidentifier PlanoAcaoId FK
    }
    MedicaoItem {
        uniqueidentifier Id PK
        uniqueidentifier ContratoItemId FK
        uniqueidentifier LocalId FK
        uniqueidentifier MedicaoId FK
    }
    MovimentoEquipamento {
        uniqueidentifier Id PK
        uniqueidentifier DeAlmoxarifadoId FK
        uniqueidentifier EquipamentoId FK
        uniqueidentifier ParaAlmoxarifadoId FK
    }
    MovimentoEstoque {
        uniqueidentifier Id PK
        uniqueidentifier AlmoxarifadoId FK
        uniqueidentifier ColaboradorId FK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier KitId FK
        uniqueidentifier LocalId FK
        uniqueidentifier ProdutoId FK
    }
    OrdemServico {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier EquipamentoId FK
        uniqueidentifier PlanoId FK
        uniqueidentifier RequisicaoId FK
    }
    ParcelaContrato {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier NotaFiscalId FK
        uniqueidentifier ObraId FK
        uniqueidentifier OrdemServicoId FK
    }
    Pedido {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier CotacaoId FK
        uniqueidentifier EstabelecimentoFaturamentoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
        uniqueidentifier RequisicaoId FK
    }
    PedidoItem {
        uniqueidentifier Id PK
        uniqueidentifier MaterialControladoId FK
        uniqueidentifier PedidoId FK
        uniqueidentifier ProdutoId FK
        uniqueidentifier RequisicaoItemId FK
    }
    PlanoAcaoFornecedor {
        uniqueidentifier Id PK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
    }
    PlanoManutencao {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier EquipamentoId FK
    }
    PropostaVersao {
        uniqueidentifier Id PK
        uniqueidentifier CotacaoFornecedorId FK
    }
    Recebimento {
        uniqueidentifier Id PK
        uniqueidentifier AlmoxarifadoId FK
        uniqueidentifier ContratoId FK
        uniqueidentifier EstabelecimentoId FK
        uniqueidentifier FornecedorId FK
        uniqueidentifier ObraId FK
        uniqueidentifier PedidoId FK
    }
    RecebimentoItem {
        uniqueidentifier Id PK
        uniqueidentifier PedidoItemId FK
        uniqueidentifier ProdutoId FK
        uniqueidentifier RecebimentoId FK
    }
    Requisicao {
        uniqueidentifier Id PK
        uniqueidentifier CentroCustoId FK
        uniqueidentifier EstabelecimentoFaturamentoId FK
        uniqueidentifier ObraId FK
    }
    RequisicaoItem {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier MaterialControladoId FK
        uniqueidentifier ProdutoId FK
        uniqueidentifier RequisicaoId FK
        uniqueidentifier ServicoControladoId FK
    }
    RequisicaoItemLocal {
        uniqueidentifier Id PK
        uniqueidentifier ComposicaoId FK
        uniqueidentifier LocalId FK
        uniqueidentifier RequisicaoItemId FK
    }
    SaldoEstoque {
        uniqueidentifier Id PK
        uniqueidentifier AlmoxarifadoId FK
        uniqueidentifier ProdutoId FK
    }
    TransferenciaItem {
        uniqueidentifier Id PK
        uniqueidentifier LocalId FK
        uniqueidentifier ProdutoId FK
        uniqueidentifier RecebimentoItemId FK
        uniqueidentifier TransferenciaId FK
    }
    TransferenciaMaterial {
        uniqueidentifier Id PK
        uniqueidentifier DestinoAlmoxarifadoId FK
        uniqueidentifier ObraId FK
        uniqueidentifier OrigemAlmoxarifadoId FK
        uniqueidentifier OrigemEstabelecimentoId FK
        uniqueidentifier PedidoId FK
        uniqueidentifier RecebimentoId FK
        uniqueidentifier SpeId FK
    }
    UsoContrato {
        uniqueidentifier Id PK
        uniqueidentifier ContratoId FK
        uniqueidentifier ContratoItemId FK
        uniqueidentifier ObraId FK
    }
    Contrato ||--o{ Aditivo : "ContratoId"
    Aditivo ||--o{ AditivoItem : "AditivoId"
    CarteiraComprador ||--o{ AusenciaComprador : "CarteiraId"
    CarteiraComprador ||--o{ CarteiraCategoria : "CarteiraId"
    Contrato ||--o{ CompetenciaDocumental : "ContratoId"
    Cotacao ||--o{ Contrato : "CotacaoId"
    Requisicao ||--o{ Contrato : "RequisicaoId"
    Contrato ||--o{ ContratoItem : "ContratoId"
    ContratoItem ||--o{ ContratoItemLocal : "ContratoItemId"
    Contrato ||--o{ ContratoServico : "ContratoId"
    Requisicao ||--o{ Cotacao : "RequisicaoId"
    Cotacao ||--o{ CotacaoFornecedor : "CotacaoId"
    Cotacao ||--o{ CotacaoItem : "CotacaoId"
    RequisicaoItem ||--o{ CotacaoItem : "RequisicaoItemId"
    Almoxarifado ||--o{ Equipamento : "AlmoxarifadoId"
    Contrato ||--o{ Equipamento : "ContratoId"
    ContratoItem ||--o{ Equipamento : "ContratoItemId"
    FamiliaEquipamento ||--o{ Equipamento : "FamiliaId"
    Equipamento ||--o{ EquipamentoDocumento : "EquipamentoId"
    Almoxarifado ||--o{ InventarioEstoque : "AlmoxarifadoId"
    InventarioEstoque ||--o{ InventarioItem : "InventarioId"
    CompetenciaDocumental ||--o{ ItemCompetencia : "CompetenciaId"
    KitMaterial ||--o{ KitMaterialItem : "KitId"
    Contrato ||--o{ LiberacaoPagamento : "ContratoId"
    Medicao ||--o{ LiberacaoPagamento : "MedicaoId"
    Pedido ||--o{ LiberacaoPagamento : "PedidoId"
    Requisicao ||--o{ LoteCompra : "RequisicaoId"
    Contrato ||--o{ Medicao : "ContratoId"
    PlanoAcaoFornecedor ||--o{ Medicao : "PlanoAcaoId"
    ContratoItem ||--o{ MedicaoItem : "ContratoItemId"
    Medicao ||--o{ MedicaoItem : "MedicaoId"
    Almoxarifado ||--o{ MovimentoEquipamento : "DeAlmoxarifadoId"
    Equipamento ||--o{ MovimentoEquipamento : "EquipamentoId"
    Almoxarifado ||--o{ MovimentoEquipamento : "ParaAlmoxarifadoId"
    Almoxarifado ||--o{ MovimentoEstoque : "AlmoxarifadoId"
    KitMaterial ||--o{ MovimentoEstoque : "KitId"
    Contrato ||--o{ OrdemServico : "ContratoId"
    Equipamento ||--o{ OrdemServico : "EquipamentoId"
    PlanoManutencao ||--o{ OrdemServico : "PlanoId"
    Requisicao ||--o{ OrdemServico : "RequisicaoId"
    Contrato ||--o{ ParcelaContrato : "ContratoId"
    OrdemServico ||--o{ ParcelaContrato : "OrdemServicoId"
    Contrato ||--o{ Pedido : "ContratoId"
    Cotacao ||--o{ Pedido : "CotacaoId"
    Requisicao ||--o{ Pedido : "RequisicaoId"
    Pedido ||--o{ PedidoItem : "PedidoId"
    RequisicaoItem ||--o{ PedidoItem : "RequisicaoItemId"
    Contrato ||--o{ PlanoManutencao : "ContratoId"
    Equipamento ||--o{ PlanoManutencao : "EquipamentoId"
    CotacaoFornecedor ||--o{ PropostaVersao : "CotacaoFornecedorId"
    Almoxarifado ||--o{ Recebimento : "AlmoxarifadoId"
    Contrato ||--o{ Recebimento : "ContratoId"
    Pedido ||--o{ Recebimento : "PedidoId"
    PedidoItem ||--o{ RecebimentoItem : "PedidoItemId"
    Recebimento ||--o{ RecebimentoItem : "RecebimentoId"
    Requisicao ||--o{ RequisicaoItem : "RequisicaoId"
    RequisicaoItem ||--o{ RequisicaoItemLocal : "RequisicaoItemId"
    Almoxarifado ||--o{ SaldoEstoque : "AlmoxarifadoId"
    RecebimentoItem ||--o{ TransferenciaItem : "RecebimentoItemId"
    TransferenciaMaterial ||--o{ TransferenciaItem : "TransferenciaId"
    Almoxarifado ||--o{ TransferenciaMaterial : "DestinoAlmoxarifadoId"
    Almoxarifado ||--o{ TransferenciaMaterial : "OrigemAlmoxarifadoId"
    Pedido ||--o{ TransferenciaMaterial : "PedidoId"
    Recebimento ||--o{ TransferenciaMaterial : "RecebimentoId"
    Contrato ||--o{ UsoContrato : "ContratoId"
    ContratoItem ||--o{ UsoContrato : "ContratoItemId"
```

| Tabela | Colunas | Arquivo da classe | Telas que usam |
|---|---|---|---|
| `Aditivos` ⏱ | 27 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | `(início)`, `contratos`, `engenharia/obras`, `engenharia/tipologias` |
| `AditivoItens` | 11 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | _rotinas internas_ |
| `Almoxarifados` | 17 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `engenharia/diario`, `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes`, `sst/colaboradores`, `sst/documentos` +7 |
| `AusenciasComprador` | 6 | `src/Sigeo.Domain/Suprimentos/Atendimento.cs` | _rotinas internas_ |
| `AvaliacoesSemestrais` ⏱ | 26 | `src/Sigeo.Domain/Suprimentos/Medicao.cs` | `fornecedores/avaliacao`, `fornecedores/qualificacao`, `medicoes` |
| `CarteirasCategoria` | 6 | `src/Sigeo.Domain/Suprimentos/Atendimento.cs` | _rotinas internas_ |
| `CarteirasComprador` | 16 | `src/Sigeo.Domain/Suprimentos/Atendimento.cs` | `(início)`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras`, `engenharia/tipologias`, `requisicoes`, `suprimentos/compradores`, `suprimentos/fila` +1 |
| `CompetenciasDocumentais` | 20 | `src/Sigeo.Domain/Suprimentos/Portal.cs` | `(início)`, `cotacao/{Token:guid}`, `fornecedor/area`, `fornecedores/documentos`, `medicoes`, `mensagens`, `portal/{Token}`, `suprimentos/documentacao-mensal` |
| `Contratos` ⏱ | 45 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | `(início)`, `cadastros/centros-custo`, `contratos`, `contratos/parcelas`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras` +15 |
| `ContratoItens` | 10 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | `contratos`, `contratos/parcelas`, `cotacoes`, `fornecedores`, `fornecedores/exigencias`, `requisicoes` |
| `ContratoItemLocais` | 4 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | `contratos`, `engenharia/obras`, `engenharia/tipologias` |
| `ContratoServicos` | 3 | `src/Sigeo.Domain/Suprimentos/Contrato.cs` | _rotinas internas_ |
| `Cotacoes` ⏱ | 36 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `(início)`, `cadastros/produtos`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras`, `engenharia/tipologias`, `fornecedor` +17 |
| `CotacaoFornecedores` | 17 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `(início)`, `cotacoes`, `cotacoes/mapa`, `fornecedores`, `fornecedores/avaliacao`, `fornecedores/exigencias`, `fornecedores/qualificacao`, `requisicoes` +3 |
| `CotacaoItens` | 17 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `(início)`, `cotacoes`, `fornecedores`, `fornecedores/avaliacao`, `fornecedores/exigencias`, `fornecedores/qualificacao`, `requisicoes`, `sistema/parametros` +2 |
| `EquipamentosPatrimonio` ⏱ | 36 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | `contratos/parcelas`, `engenharia/diario`, `engenharia/diario/imprimir`, `equipamentos`, `obra/equipamentos/etiquetas`, `qr/f/{Token}` |
| `EquipamentoDocumentos` | 5 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | _rotinas internas_ |
| `FamiliasEquipamento` | 16 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | `engenharia/diario`, `equipamentos`, `obra/equipamentos/etiquetas`, `plataforma/auditoria`, `plataforma/construtoras`, `plataforma/usuarios` |
| `InventariosEstoque` ⏱ | 20 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes`, `sst/colaboradores`, `sst/documentos`, `sst/epi` +6 |
| `InventarioItens` | 7 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes`, `sst/colaboradores`, `sst/documentos`, `sst/epi` +6 |
| `ItensCompetencia` | 19 | `src/Sigeo.Domain/Suprimentos/Portal.cs` | `medicoes`, `suprimentos/documentacao-mensal` |
| `KitsMaterial` | 17 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes`, `sst/colaboradores`, `sst/documentos`, `sst/epi` +6 |
| `KitMaterialItens` | 4 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos` |
| `Pagamentos` ⏱ | 32 | `src/Sigeo.Domain/Suprimentos/Medicao.cs` | `(início)`, `engenharia/importar-erp`, `engenharia/obras`, `engenharia/tipologias`, `financeiro/bancos`, `financeiro/cadastros`, `financeiro/fluxo`, `financeiro/pagar` +4 |
| `LotesCompra` | 22 | `src/Sigeo.Domain/Suprimentos/Atendimento.cs` | `(início)`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras`, `engenharia/tipologias`, `gestao/metas`, `requisicoes`, `suprimentos/compradores` +4 |
| `Medicoes` ⏱ | 40 | `src/Sigeo.Domain/Suprimentos/Medicao.cs` | `(início)`, `contratos`, `cotacoes`, `engenharia/custo-realizado`, `engenharia/importar-erp`, `engenharia/obras`, `engenharia/tipologias`, `financeiro/bancos` +14 |
| `MedicaoItens` | 11 | `src/Sigeo.Domain/Suprimentos/Medicao.cs` | `engenharia/obras`, `engenharia/tipologias` |
| `MovimentosEquipamento` | 17 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | `contratos/parcelas`, `engenharia/diario`, `equipamentos` |
| `MovimentosEstoque` | 30 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes` +9 |
| `OrdensServico` ⏱ | 36 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | `engenharia/diario`, `equipamentos`, `qr/f/{Token}` |
| `ParcelasContrato` ⏱ | 35 | `src/Sigeo.Domain/Suprimentos/ContratoRecorrente.cs` | `contratos/parcelas`, `engenharia/diario`, `equipamentos` |
| `Pedidos` ⏱ | 40 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `(início)`, `cadastros/centros-custo`, `cadastros/marcas`, `cadastros/produtos`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/custo-realizado` +23 |
| `PedidoItens` | 15 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/marcas`, `cadastros/produtos`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras` +17 |
| `PlanosAcao` ⏱ | 25 | `src/Sigeo.Domain/Suprimentos/Medicao.cs` | `(início)`, `cotacoes`, `fornecedores`, `fornecedores/avaliacao`, `fornecedores/exigencias`, `fornecedores/planos`, `fornecedores/qualificacao`, `medicoes` +3 |
| `PlanosManutencao` | 22 | `src/Sigeo.Domain/Suprimentos/Equipamentos.cs` | `engenharia/diario`, `equipamentos` |
| `PropostasVersao` | 11 | `src/Sigeo.Domain/Suprimentos/Cotacao.cs` | `cotacao/{Token:guid}`, `fornecedor`, `fornecedor/area`, `fornecedor/cotacao/{CotacaoId:guid}`, `fornecedores/documentos`, `portal/{Token}` |
| `Recebimentos` ⏱ | 41 | `src/Sigeo.Domain/Suprimentos/Recebimento.cs` | `(início)`, `campo/fvs/{Id:guid}`, `campo/local/{Qr}`, `cotacoes`, `cotacoes/mapa`, `engenharia/obras`, `engenharia/tipologias`, `fornecedores` +19 |
| `RecebimentoItens` | 13 | `src/Sigeo.Domain/Suprimentos/Recebimento.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos` |
| `Requisicoes` ⏱ | 38 | `src/Sigeo.Domain/Suprimentos/Requisicao.cs` | `(início)`, `cadastros/centros-custo`, `cotacao/{Token:guid}`, `cotacoes`, `cotacoes/mapa`, `engenharia/composicoes`, `engenharia/obras`, `engenharia/tipologias` +11 |
| `RequisicaoItens` | 22 | `src/Sigeo.Domain/Suprimentos/Requisicao.cs` | `(início)`, `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `cotacoes`, `cotacoes/mapa`, `engenharia/composicoes`, `engenharia/obras` +15 |
| `RequisicaoItemLocais` | 5 | `src/Sigeo.Domain/Suprimentos/Requisicao.cs` | `engenharia/composicoes`, `engenharia/obras`, `engenharia/tipologias`, `requisicoes` |
| `SaldosEstoque` | 19 | `src/Sigeo.Domain/Suprimentos/Estoque.cs` | `cadastros/categorias`, `cadastros/controlados`, `cadastros/produtos`, `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes` +9 |
| `TransferenciaItens` | 11 | `src/Sigeo.Domain/Suprimentos/Transferencia.cs` | `engenharia/obras`, `engenharia/tipologias`, `recebimentos`, `transferencias` |
| `Transferencias` ⏱ | 25 | `src/Sigeo.Domain/Suprimentos/Transferencia.cs` | `engenharia/obras`, `engenharia/tipologias`, `equipamentos`, `estoque`, `estoque/kits`, `recebimentos`, `requisicoes`, `sst/colaboradores` +8 |
| `UsosContrato` | 17 | `src/Sigeo.Domain/Suprimentos/ContratoRecorrente.cs` | `contratos/parcelas` |

## Telas → tabelas

| Tela (rota) | Arquivo | Serviços | Tabelas |
|---|---|---|---|
| `(início)` | `src/Sigeo.Web/Components/Pages/Home.razor` | AprovacaoServico, MensagensServico, SistemaServico | 63 |
| `Error` | `src/Sigeo.Web/Components/Pages/Error.razor` | — | 0 |
| `cadastros/categorias` | `src/Sigeo.Web/Components/Pages/Cadastro/Categorias.razor` | CadastrosServico, CodigosProdutoServico, PadraoCatalogoServico | 18 |
| `cadastros/centros-custo` | `src/Sigeo.Web/Components/Pages/Cadastro/CentrosCusto.razor` | EstabelecimentosServico | 11 |
| `cadastros/controlados` | `src/Sigeo.Web/Components/Pages/Cadastro/Controlados.razor` | CadastrosServico | 20 |
| `cadastros/estabelecimentos` | `src/Sigeo.Web/Components/Pages/Cadastro/Estabelecimentos.razor` | EstabelecimentosServico | 8 |
| `cadastros/marcas` | `src/Sigeo.Web/Components/Pages/Cadastro/Marcas.razor` | MarcasServico | 8 |
| `cadastros/obras` | `src/Sigeo.Web/Components/Pages/Cadastro/ObrasRedireciona.razor` | — | 0 |
| `cadastros/produtos` | `src/Sigeo.Web/Components/Pages/Cadastro/Produtos.razor` | AnalisesIaServico, CadastrosServico, CodigosProdutoServico, InsumosAntigosServico, MarcasServico | 24 |
| `campo/fvs/{Id:guid}` | `src/Sigeo.Web/Components/Pages/Campo/FvsCampo.razor` | QualidadeObraServico | 22 |
| `campo/local/{Qr}` | `src/Sigeo.Web/Components/Pages/Campo/LocalCampo.razor` | QualidadeObraServico | 21 |
| `configuracao-inicial` | `src/Sigeo.Web/Components/Pages/Publico/ConfiguracaoInicial.razor` | — | 0 |
| `conta/construtora` | `src/Sigeo.Web/Components/Pages/Publico/EscolherConstrutora.razor` | ContaServico | 3 |
| `conta/primeiro-acesso` | `src/Sigeo.Web/Components/Pages/Publico/PrimeiroAcesso.razor` | — | 0 |
| `conta/trocar-senha` | `src/Sigeo.Web/Components/Pages/Publico/TrocarSenha.razor` | — | 0 |
| `contratos` | `src/Sigeo.Web/Components/Pages/Suprimentos/Contratos.razor` | ContratosServico | 12 |
| `contratos/parcelas` | `src/Sigeo.Web/Components/Pages/Suprimentos/Parcelas.razor` | ContratosRecorrentesServico | 7 |
| `cotacao/{Token:guid}` | `src/Sigeo.Web/Components/Pages/Publico/PortalCotacao.razor` | PortalServico | 32 |
| `cotacoes` | `src/Sigeo.Web/Components/Pages/Suprimentos/Cotacoes.razor` | CotacoesServico, FornecedoresServico | 29 |
| `cotacoes/mapa` | `src/Sigeo.Web/Components/Pages/Suprimentos/MapaCotacao.razor` | CotacoesServico | 21 |
| `engenharia/composicoes` | `src/Sigeo.Web/Components/Pages/Engenharia/Composicoes.razor` | OrcamentoServico | 14 |
| `engenharia/cronograma` | `src/Sigeo.Web/Components/Pages/Engenharia/Cronograma.razor` | PlanejamentoServico | 19 |
| `engenharia/custo-realizado` | `src/Sigeo.Web/Components/Pages/Engenharia/CustoRealizado.razor` | CustoRealizadoServico | 10 |
| `engenharia/diario` | `src/Sigeo.Web/Components/Pages/Engenharia/DiarioDeObra.razor` | DiarioObraServico | 18 |
| `engenharia/diario/imprimir` | `src/Sigeo.Web/Components/Pages/Engenharia/DiarioImprimir.razor` | — | 5 |
| `engenharia/importar-erp` | `src/Sigeo.Web/Components/Pages/Engenharia/ImportarHistoricoErp.razor` | CustoRealizadoServico, FinanceiroServico | 30 |
| `engenharia/last-planner` | `src/Sigeo.Web/Components/Pages/Engenharia/LastPlanner.razor` | — | 1 |
| `engenharia/mao-de-obra` | `src/Sigeo.Web/Components/Pages/Engenharia/MaoObra.razor` | MaoObraServico | 12 |
| `engenharia/obras` | `src/Sigeo.Web/Components/Pages/Engenharia/Empreendimento.razor` | EngenhariaServico, QualidadeObraServico | 58 |
| `engenharia/planejamento` | `src/Sigeo.Web/Components/Pages/Engenharia/Planejamento.razor` | — | 19 |
| `engenharia/planejamento/imprimir` | `src/Sigeo.Web/Components/Pages/Engenharia/PlanejamentoImprimir.razor` | — | 19 |
| `engenharia/planejamento/imprimir-programacao` | `src/Sigeo.Web/Components/Pages/Engenharia/ProgramacaoImprimir.razor` | AcompanhamentoServico | 26 |
| `engenharia/tipologias` | `src/Sigeo.Web/Components/Pages/Engenharia/Tipologias.razor` | EngenhariaServico, KitTipologiaServico, OrcamentoServico | 51 |
| `equipamentos` | `src/Sigeo.Web/Components/Pages/Suprimentos/Equipamentos.razor` | EquipamentosServico, EstoqueServico | 27 |
| `estoque` | `src/Sigeo.Web/Components/Pages/Suprimentos/Estoque.razor` | EstoqueServico | 20 |
| `estoque/kits` | `src/Sigeo.Web/Components/Pages/Suprimentos/Kits.razor` | EstoqueServico | 21 |
| `financeiro/bancos` | `src/Sigeo.Web/Components/Pages/Financeiro/Bancos.razor` | FinanceiroServico | 24 |
| `financeiro/cadastros` | `src/Sigeo.Web/Components/Pages/Financeiro/CadastrosFinanceiros.razor` | FinanceiroServico | 24 |
| `financeiro/fluxo` | `src/Sigeo.Web/Components/Pages/Financeiro/Fluxo.razor` | FinanceiroServico | 24 |
| `financeiro/pagar` | `src/Sigeo.Web/Components/Pages/Financeiro/Titulos.razor` | FinanceiroServico | 24 |
| `fornecedor` | `src/Sigeo.Web/Components/Pages/AreaFornecedor/Entrar.razor` | AcessoFornecedorServico | 14 |
| `fornecedor/area` | `src/Sigeo.Web/Components/Pages/AreaFornecedor/Area.razor` | AcessoFornecedorServico, PortalServico | 33 |
| `fornecedor/cotacao/{CotacaoId:guid}` | `src/Sigeo.Web/Components/Pages/AreaFornecedor/Cotacao.razor` | AcessoFornecedorServico | 14 |
| `fornecedores` | `src/Sigeo.Web/Components/Pages/Cadastro/Fornecedores.razor` | FornecedoresServico | 23 |
| `fornecedores/avaliacao` | `src/Sigeo.Web/Components/Pages/Qualidade/AvaliacaoFornecedores.razor` | AvaliacaoFornecedoresServico | 19 |
| `fornecedores/documentos` | `src/Sigeo.Web/Components/Pages/Cadastro/DocumentosFornecedor.razor` | PortalServico | 32 |
| `fornecedores/exigencias` | `src/Sigeo.Web/Components/Pages/Cadastro/MatrizDocumentos.razor` | FornecedoresServico | 22 |
| `fornecedores/planos` | `src/Sigeo.Web/Components/Pages/Qualidade/PlanosAcao.razor` | PlanosAcaoServico | 6 |
| `fornecedores/qualificacao` | `src/Sigeo.Web/Components/Pages/Cadastro/Qualificacao.razor` | AvaliacaoFornecedoresServico | 19 |
| `gestao/metas` | `src/Sigeo.Web/Components/Pages/Gestao/Metas.razor` | ColaboracaoServico, MetasServico | 20 |
| `incorporacao/carteira` | `src/Sigeo.Web/Components/Pages/Incorporadora/CarteiraRecebiveis.razor` | FinanceiroServico, IncorporacaoServico | 24 |
| `incorporacao/empreendimento` | `src/Sigeo.Web/Components/Pages/Incorporadora/DadosIncorporacao.razor` | IncorporacaoServico | 18 |
| `incorporacao/espelho` | `src/Sigeo.Web/Components/Pages/Incorporadora/EspelhoVendas.razor` | IncorporacaoServico | 18 |
| `incorporacao/ret` | `src/Sigeo.Web/Components/Pages/Incorporadora/ApuracaoRetTela.razor` | IncorporacaoServico | 18 |
| `incorporacao/vendas` | `src/Sigeo.Web/Components/Pages/Incorporadora/VendasContratos.razor` | FinanceiroServico, IncorporacaoServico | 24 |
| `login` | `src/Sigeo.Web/Components/Pages/Publico/Login.razor` | — | 0 |
| `medicoes` | `src/Sigeo.Web/Components/Pages/Suprimentos/Medicoes.razor` | MedicoesServico | 23 |
| `mensagens` | `src/Sigeo.Web/Components/Pages/Mensagens.razor` | CanalAvisos, MensagensServico | 33 |
| `not-found` | `src/Sigeo.Web/Components/Pages/NotFound.razor` | — | 0 |
| `notas-fiscais` | `src/Sigeo.Web/Components/Pages/Suprimentos/NotasFiscais.razor` | NotasFiscaisServico | 8 |
| `obra/acoes` | `src/Sigeo.Web/Components/Pages/QualidadeObra/AcoesCorretivas.razor` | QualidadeObraServico | 21 |
| `obra/auditorias` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Auditorias.razor` | QualidadeObraServico | 21 |
| `obra/equipamentos` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Equipamentos.razor` | QualidadeObraServico | 21 |
| `obra/equipamentos/etiquetas` | `src/Sigeo.Web/Components/Pages/Suprimentos/Etiquetas.razor` | — | 9 |
| `obra/fvs` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Fvss.razor` | QualidadeObraServico | 22 |
| `obra/mapa` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Mapa.razor` | — | 7 |
| `obra/modelos/{TipoRota}` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Modelos.razor` | KitModelosServico, QualidadeObraServico | 25 |
| `obra/rnc` | `src/Sigeo.Web/Components/Pages/QualidadeObra/Rncs.razor` | AnalisesIaServico, QualidadeObraServico | 27 |
| `pagamentos` | `src/Sigeo.Web/Components/Pages/Suprimentos/Pagamentos.razor` | PagamentosServico | 10 |
| `pedidos` | `src/Sigeo.Web/Components/Pages/Suprimentos/Pedidos.razor` | PedidosServico | 3 |
| `plataforma/ativar` | `src/Sigeo.Web/Components/Pages/Publico/AtivarPlataforma.razor` | — | 0 |
| `plataforma/auditoria` | `src/Sigeo.Web/Components/Pages/Plataforma/AuditoriaPlataforma.razor` | PlataformaServico | 24 |
| `plataforma/construtoras` | `src/Sigeo.Web/Components/Pages/Plataforma/Construtoras.razor` | PlataformaServico | 24 |
| `plataforma/usuarios` | `src/Sigeo.Web/Components/Pages/Plataforma/UsuariosPlataforma.razor` | PlataformaServico | 24 |
| `portal/{Token}` | `src/Sigeo.Web/Components/Pages/Publico/PortalFornecedor.razor` | PortalServico | 32 |
| `qr/f/{Token}` | `src/Sigeo.Web/Components/Pages/Publico/QrEquipamento.razor` | — | 4 |
| `qr/{Token}` | `src/Sigeo.Web/Components/Pages/Publico/Qr.razor` | — | 2 |
| `recebimentos` | `src/Sigeo.Web/Components/Pages/Suprimentos/Recebimentos.razor` | AnexosServico, EstoqueServico, LeituraNotaServico, QualidadeObraServico, RecebimentosServico, TransferenciasServico | 43 |
| `requisicoes` | `src/Sigeo.Web/Components/Pages/Suprimentos/Requisicoes.razor` | CotacoesServico, EstoqueServico, FornecedoresServico, OrcamentoServico, RequisicoesServico | 46 |
| `sgq/copias` | `src/Sigeo.Web/Components/Pages/Qualidade/Copias.razor` | — | 3 |
| `sgq/documentos` | `src/Sigeo.Web/Components/Pages/Qualidade/DocumentosSgq.razor` | — | 7 |
| `sgq/externos` | `src/Sigeo.Web/Components/Pages/Qualidade/Externos.razor` | — | 6 |
| `sgq/grd` | `src/Sigeo.Web/Components/Pages/Qualidade/Grds.razor` | — | 4 |
| `sgq/leituras` | `src/Sigeo.Web/Components/Pages/Qualidade/Leituras.razor` | — | 1 |
| `sgq/lista-mestra` | `src/Sigeo.Web/Components/Pages/Qualidade/ListaMestra.razor` | — | 2 |
| `sgq/registros` | `src/Sigeo.Web/Components/Pages/Qualidade/Registros.razor` | — | 1 |
| `sistema/log` | `src/Sigeo.Web/Components/Pages/Sistema/Log.razor` | — | 3 |
| `sistema/parametros` | `src/Sigeo.Web/Components/Pages/Sistema/ParametrosListas.razor` | SistemaServico | 21 |
| `sistema/perfis` | `src/Sigeo.Web/Components/Pages/Sistema/PerfisAcesso.razor` | AcessoServico | 5 |
| `sistema/permissoes` | `src/Sigeo.Web/Components/Pages/Sistema/Permissoes.razor` | SistemaServico | 19 |
| `sistema/usuarios` | `src/Sigeo.Web/Components/Pages/Sistema/Usuarios.razor` | ContaServico, SistemaServico | 19 |
| `sst/colaboradores` | `src/Sigeo.Web/Components/Pages/Sst/Colaboradores.razor` | SstServico | 33 |
| `sst/documentos` | `src/Sigeo.Web/Components/Pages/Sst/DocumentosColaborador.razor` | SstServico | 34 |
| `sst/epi` | `src/Sigeo.Web/Components/Pages/Sst/Epis.razor` | SstServico | 34 |
| `sst/exigencias` | `src/Sigeo.Web/Components/Pages/Sst/Exigencias.razor` | SstServico | 34 |
| `sst/inspecoes` | `src/Sigeo.Web/Components/Pages/Sst/Inspecoes.razor` | SstServico | 33 |
| `sst/ocorrencias` | `src/Sigeo.Web/Components/Pages/Sst/Ocorrencias.razor` | QualidadeObraServico, SstServico | 46 |
| `sst/pt` | `src/Sigeo.Web/Components/Pages/Sst/PermissoesTrabalho.razor` | SstServico | 33 |
| `sst/treinamentos` | `src/Sigeo.Web/Components/Pages/Sst/Treinamentos.razor` | SstServico | 33 |
| `suprimentos/compradores` | `src/Sigeo.Web/Components/Pages/Suprimentos/Compradores.razor` | AtendimentoServico | 18 |
| `suprimentos/documentacao-mensal` | `src/Sigeo.Web/Components/Pages/Suprimentos/DocumentacaoMensal.razor` | CompetenciasServico | 15 |
| `suprimentos/economia` | `src/Sigeo.Web/Components/Pages/Suprimentos/Economia.razor` | EconomiaServico | 14 |
| `suprimentos/fila` | `src/Sigeo.Web/Components/Pages/Suprimentos/FilaCompras.razor` | AtendimentoServico | 18 |
| `suprimentos/painel` | `src/Sigeo.Web/Components/Pages/Suprimentos/PainelSuprimentos.razor` | PainelSuprimentosServico | 20 |
| `suprimentos/precos` | `src/Sigeo.Web/Components/Pages/Suprimentos/HistoricoPrecos.razor` | EconomiaServico | 14 |
| `tarefas` | `src/Sigeo.Web/Components/Pages/Tarefas.razor` | ColaboracaoServico | 9 |
| `transferencias` | `src/Sigeo.Web/Components/Pages/Suprimentos/Transferencias.razor` | EstoqueServico, TransferenciasServico | 22 |
