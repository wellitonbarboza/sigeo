using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Importacao;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Sst;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Abstracoes;

/// <summary>
/// Banco visto pelos casos de uso: os conjuntos (DbSet = repositório) e a unidade de trabalho (SaveChanges).
/// A Infrastructure implementa com o DbContext do SQL Server; a aplicação não conhece provedor, conexão nem migração.
/// Filtros globais (empresa, obra, excluído) e auditoria continuam valendo: são configurados na implementação.
/// </summary>
public interface ISigeoDb : IDisposable, IAsyncDisposable
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
    DbSet<RegistroPlataforma> RegistrosPlataforma { get; }
    DbSet<PerfilAcesso> PerfisAcesso { get; }
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
    DbSet<Marca> Marcas { get; }
    DbSet<Requisicao> Requisicoes { get; }
    DbSet<RequisicaoItem> RequisicaoItens { get; }
    DbSet<Cotacao> Cotacoes { get; }
    DbSet<CotacaoFornecedor> CotacaoFornecedores { get; }
    DbSet<PropostaVersao> PropostasVersao { get; }
    DbSet<LoteCompra> LotesCompra { get; }
    DbSet<CarteiraComprador> CarteirasComprador { get; }
    DbSet<CompetenciaDocumental> CompetenciasDocumentais { get; }
    DbSet<ItemCompetencia> ItensCompetencia { get; }
    DbSet<Domain.Colaboracao.Meta> Metas { get; }
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
    DbSet<CustoRealizado> CustosRealizados { get; }
    DbSet<Estabelecimento> Estabelecimentos { get; }
    DbSet<FluxoEstabelecimento> FluxosEstabelecimento { get; }
    DbSet<FornecedorGrupo> FornecedorGrupos { get; }
    DbSet<FornecedorServico> FornecedorServicos { get; }
    DbSet<FornecedorExigencia> FornecedorExigencias { get; }
    DbSet<RequisicaoItemLocal> RequisicaoItemLocais { get; }
    DbSet<ContratoItemLocal> ContratoItemLocais { get; }
    DbSet<TransferenciaMaterial> Transferencias { get; }
    DbSet<TransferenciaItem> TransferenciaItens { get; }
    DbSet<Almoxarifado> Almoxarifados { get; }
    DbSet<SaldoEstoque> SaldosEstoque { get; }
    DbSet<MovimentoEstoque> MovimentosEstoque { get; }
    DbSet<InventarioEstoque> Inventarios { get; }
    DbSet<InventarioItem> InventarioItens { get; }
    DbSet<KitMaterial> KitsMaterial { get; }
    DbSet<KitMaterialItem> KitMaterialItens { get; }
    DbSet<FamiliaEquipamento> FamiliasEquipamento { get; }
    DbSet<Domain.Suprimentos.Equipamento> Ativos { get; }
    DbSet<EquipamentoDocumento> AtivoDocumentos { get; }
    DbSet<MovimentoEquipamento> MovimentosEquipamento { get; }
    DbSet<PlanoManutencao> PlanosManutencao { get; }
    DbSet<OrdemServico> OrdensServico { get; }
    DbSet<ParcelaContrato> ParcelasContrato { get; }
    DbSet<UsoContrato> UsosContrato { get; }
    DbSet<TipoEquipamentoMedicao> TiposEquipamentoMedicao { get; }
    DbSet<CautelaFerramenta> CautelasFerramenta { get; }
    DbSet<Domain.Planejamento.DiarioEquipamento> DiariosObraEquipamentos { get; }
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
    DbSet<VinculoOrigem> VinculosOrigem { get; }
    DbSet<Domain.Financeiro.ContaGerencial> ContasGerenciais { get; }
    DbSet<Domain.Financeiro.ContaBancaria> ContasBancarias { get; }
    DbSet<Domain.Financeiro.CondicaoPagamento> CondicoesPagamento { get; }
    DbSet<Domain.Financeiro.Titulo> Titulos { get; }
    DbSet<Domain.Financeiro.TituloParcela> TituloParcelas { get; }
    DbSet<Domain.Financeiro.MovimentoBancario> MovimentosBancarios { get; }
    DbSet<Domain.Incorporacao.IncorporacaoObra> Incorporacoes { get; }
    DbSet<Domain.Incorporacao.AliquotaRegime> AliquotasRegime { get; }
    DbSet<Domain.Incorporacao.IndiceEconomico> IndicesEconomicos { get; }
    DbSet<Domain.Incorporacao.UnidadeComercial> UnidadesComerciais { get; }
    DbSet<Domain.Incorporacao.Comprador> Compradores { get; }
    DbSet<Domain.Incorporacao.ContratoVenda> ContratosVenda { get; }
    DbSet<Domain.Incorporacao.BlocoCondicaoVenda> BlocosCondicaoVenda { get; }
    DbSet<Domain.Incorporacao.ComissaoVenda> ComissoesVenda { get; }
    DbSet<Domain.Incorporacao.ApuracaoRet> ApuracoesRet { get; }
    DbSet<LinhaImportacao> LinhasImportacao { get; }
    DbSet<RegistroAuditoria> Auditoria { get; }
    DbSet<CodigoAcessoFornecedor> CodigosAcessoFornecedor { get; }
    DbSet<Domain.Fiscal.NotaFiscalEntrada> NotasFiscais { get; }
    DbSet<Domain.Fiscal.NotaFiscalItem> NotaFiscalItens { get; }
    DbSet<Domain.Fiscal.ImportacaoNota> ImportacoesNota { get; }
    DbSet<Domain.Fiscal.UsoIa> UsosIa { get; }
    DbSet<Domain.Planejamento.VersaoCronograma> VersoesCronograma { get; }
    DbSet<Domain.Planejamento.AtividadeCronograma> AtividadesCronograma { get; }
    DbSet<Domain.Planejamento.VinculoAtividade> VinculosAtividade { get; }
    DbSet<Domain.Planejamento.DesembolsoVersao> DesembolsosVersao { get; }
    DbSet<Domain.Planejamento.Feriado> Feriados { get; }
    DbSet<Domain.Planejamento.RedePlanejamento> RedesPlanejamento { get; }
    DbSet<Domain.Planejamento.RedeLocal> RedeLocais { get; }
    DbSet<Domain.Planejamento.RedePacote> RedePacotes { get; }
    DbSet<Domain.Planejamento.RedePacoteLocal> RedePacoteLocais { get; }
    DbSet<Domain.Planejamento.VinculoRede> VinculosRede { get; }
    DbSet<Domain.Planejamento.Restricao> Restricoes { get; }
    DbSet<Domain.Planejamento.PlanoSemanal> PlanosSemanais { get; }
    DbSet<Domain.Planejamento.MedicaoSemanal> MedicoesSemanais { get; }
    DbSet<Domain.Planejamento.DiarioObra> DiariosObra { get; }
    DbSet<Domain.Planejamento.DiarioAtividade> DiariosObraAtividades { get; }
    DbSet<Domain.Planejamento.CompromissoSemanal> CompromissosSemanais { get; }
    DbSet<Domain.Colaboracao.Conversa> Conversas { get; }
    DbSet<Domain.Colaboracao.ParticipanteConversa> ParticipantesConversa { get; }
    DbSet<Domain.Colaboracao.MensagemConversa> MensagensConversa { get; }
    DbSet<Domain.Colaboracao.LeituraMensagem> LeiturasMensagem { get; }
    DbSet<Domain.Colaboracao.AvisoEmail> AvisosEmail { get; }
}
