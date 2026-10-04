using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sst;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Abstracoes;

/// <summary>
/// Banco visto pelos casos de uso: os conjuntos (DbSet = repositório) e a unidade de trabalho (SaveChanges).
/// A Infrastructure implementa com o DbContext do SQL Server; a aplicação não conhece provedor, conexão nem migração.
/// Filtros globais (empresa, obra, excluído) e auditoria continuam valendo: são configurados na implementação.
/// </summary>
public interface IQualyHubDb : IDisposable, IAsyncDisposable
{
    /// <summary>Empresa do usuário atual (filtro global e carimbo de novos registros).</summary>
    Guid EmpresaAtualId { get; }
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    IModel Model { get; }

    DbSet<T> Set<T>() where T : class;
    EntityEntry<T> Entry<T>(T entidade) where T : class;
    EntityEntry Entry(object entidade);
    EntityEntry<T> Add<T>(T entidade) where T : class;
    EntityEntry<T> Remove<T>(T entidade) where T : class;
    EntityEntry Remove(object entidade);
    EntityEntry<T> Update<T>(T entidade) where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    DbSet<Empresa> Empresas { get; }
    DbSet<Obra> Obras { get; }
    DbSet<Local> Locais { get; }
    DbSet<CentroCusto> CentrosCusto { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<CredencialUsuario> Credenciais { get; }
    DbSet<UsuarioObra> UsuariosObras { get; }
    DbSet<Alcada> Alcadas { get; }
    DbSet<Parametro> Parametros { get; }
    DbSet<ItemLista> Listas { get; }
    DbSet<Anexo> Anexos { get; }
    DbSet<Sequencia> Sequencias { get; }
    DbSet<TipoDocumentoExigido> TiposDocumento { get; }
    DbSet<Fornecedor> Fornecedores { get; }
    DbSet<FornecedorQualificacao> Qualificacoes { get; }
    DbSet<FornecedorDocumento> FornecedorDocumentos { get; }
    DbSet<LinkPortalFornecedor> LinksPortal { get; }
    DbSet<ServicoControlado> ServicosControlados { get; }
    DbSet<MaterialControlado> MateriaisControlados { get; }
    DbSet<Produto> Produtos { get; }
    DbSet<Requisicao> Requisicoes { get; }
    DbSet<RequisicaoItem> RequisicaoItens { get; }
    DbSet<Cotacao> Cotacoes { get; }
    DbSet<CotacaoFornecedor> CotacaoFornecedores { get; }
    DbSet<CotacaoItem> CotacaoItens { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<PedidoItem> PedidoItens { get; }
    DbSet<Contrato> Contratos { get; }
    DbSet<ContratoItem> ContratoItens { get; }
    DbSet<ContratoServico> ContratoServicos { get; }
    DbSet<Recebimento> Recebimentos { get; }
    DbSet<RecebimentoItem> RecebimentoItens { get; }
    DbSet<Medicao> Medicoes { get; }
    DbSet<MedicaoItem> MedicaoItens { get; }
    DbSet<PlanoAcaoFornecedor> PlanosAcao { get; }
    DbSet<AvaliacaoSemestral> AvaliacoesSemestrais { get; }
    DbSet<LiberacaoPagamento> Pagamentos { get; }
    DbSet<Pasta> Pastas { get; }
    DbSet<Documento> Documentos { get; }
    DbSet<DocumentoRelacionado> DocumentosRelacionados { get; }
    DbSet<DocumentoRevisao> Revisoes { get; }
    DbSet<EtapaRevisao> EtapasRevisao { get; }
    DbSet<CopiaControlada> Copias { get; }
    DbSet<SolicitacaoCopia> SolicitacoesCopia { get; }
    DbSet<SolicitacaoCopiaItem> SolicitacaoCopiaItens { get; }
    DbSet<Ciencia> Ciencias { get; }
    DbSet<PermissaoDocumento> Permissoes { get; }
    DbSet<CheckoutDocumento> Checkouts { get; }
    DbSet<EventoDocumento> EventosDocumento { get; }
    DbSet<DocumentoExterno> DocumentosExternos { get; }
    DbSet<RegraRetencao> RegrasRetencao { get; }
    DbSet<Grd> Grds { get; }
    DbSet<GrdItem> GrdItens { get; }
    DbSet<ModeloInspecao> ModelosInspecao { get; }
    DbSet<ItemVerificacao> ItensVerificacao { get; }
    DbSet<EnsaioModelo> EnsaiosModelo { get; }
    DbSet<Fvs> Fvss { get; }
    DbSet<FvsItem> FvsItens { get; }
    DbSet<Rnc> Rncs { get; }
    DbSet<AcaoCorretiva> AcoesCorretivas { get; }
    DbSet<AcaoCorretivaItem> AcaoCorretivaItens { get; }
    DbSet<EquipamentoMedicao> Equipamentos { get; }
    DbSet<Calibracao> Calibracoes { get; }
    DbSet<AuditoriaInterna> AuditoriasInternas { get; }
    DbSet<Constatacao> Constatacoes { get; }
    DbSet<FvmItem> FvmItens { get; }
    DbSet<EnsaioRecebimento> EnsaiosRecebimento { get; }
    DbSet<AplicacaoLote> AplicacoesLote { get; }
    DbSet<FuncaoObra> Funcoes { get; }
    DbSet<Exigencia> Exigencias { get; }
    DbSet<FuncaoExigencia> FuncaoExigencias { get; }
    DbSet<Colaborador> Colaboradores { get; }
    DbSet<DocumentoColaborador> DocumentosColaborador { get; }
    DbSet<Treinamento> Treinamentos { get; }
    DbSet<Turma> Turmas { get; }
    DbSet<Participante> Participantes { get; }
    DbSet<Epi> Epis { get; }
    DbSet<EntregaEpi> EntregasEpi { get; }
    DbSet<InspecaoSeguranca> InspecoesSeguranca { get; }
    DbSet<InspecaoSegurancaItem> InspecaoSegurancaItens { get; }
    DbSet<PermissaoTrabalho> PermissoesTrabalho { get; }
    DbSet<ExecutantePt> ExecutantesPt { get; }
    DbSet<Ocorrencia> Ocorrencias { get; }
    DbSet<Nota> Notas { get; }
    DbSet<FornecedorContato> FornecedorContatos { get; }
    DbSet<CategoriaProduto> CategoriasProduto { get; }
    DbSet<Spe> Spes { get; }
    DbSet<Quadra> Quadras { get; }
    DbSet<Tipologia> Tipologias { get; }
    DbSet<TipologiaAmbiente> TipologiaAmbientes { get; }
    DbSet<TipologiaMix> TipologiaMix { get; }
    DbSet<Composicao> Composicoes { get; }
    DbSet<ComposicaoItem> ComposicaoItens { get; }
    DbSet<ServicoAplicacao> ServicoAplicacoes { get; }
    DbSet<ServicoLocal> ServicosLocais { get; }
    DbSet<Equipe> Equipes { get; }
    DbSet<EquipeMembro> EquipeMembros { get; }
    DbSet<CustoMaoObraMes> CustosMaoObra { get; }
    DbSet<Apropriacao> Apropriacoes { get; }
    DbSet<Estabelecimento> Estabelecimentos { get; }
    DbSet<FluxoEstabelecimento> FluxosEstabelecimento { get; }
    DbSet<FornecedorGrupo> FornecedorGrupos { get; }
    DbSet<FornecedorServico> FornecedorServicos { get; }
    DbSet<FornecedorExigencia> FornecedorExigencias { get; }
    DbSet<RequisicaoItemLocal> RequisicaoItemLocais { get; }
    DbSet<ContratoItemLocal> ContratoItemLocais { get; }
    DbSet<TransferenciaMaterial> Transferencias { get; }
    DbSet<TransferenciaItem> TransferenciaItens { get; }
    DbSet<Tarefa> Tarefas { get; }
    DbSet<TarefaProrrogacao> TarefaProrrogacoes { get; }
    DbSet<TarefaLeitura> TarefaLeituras { get; }
    DbSet<Aditivo> Aditivos { get; }
    DbSet<AditivoItem> AditivoItens { get; }
    DbSet<RegraNotificacao> RegrasNotificacao { get; }
    DbSet<SolicitacaoAprovacao> SolicitacoesAprovacao { get; }
    DbSet<EtapaAprovacao> EtapasAprovacao { get; }
    DbSet<LimiteAprovacao> LimitesAprovacao { get; }
    DbSet<DelegacaoAprovacao> DelegacoesAprovacao { get; }
    DbSet<LayoutImportacao> LayoutsImportacao { get; }
    DbSet<LoteImportacao> LotesImportacao { get; }
    DbSet<LinhaImportacao> LinhasImportacao { get; }
    DbSet<RegistroAuditoria> Auditoria { get; }
}
